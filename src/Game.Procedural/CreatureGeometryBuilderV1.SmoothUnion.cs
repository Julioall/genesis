using Game.Contracts;

namespace Game.Procedural;

public sealed partial class CreatureGeometryBuilderV1
{
    private readonly record struct FusedEllipsoid(
        int PartIndex,
        CreatureVector3V1 Center,
        CreatureVector3V1 Axis,
        CreatureVector3V1 Tangent,
        CreatureVector3V1 Bitangent,
        double AxialRadius,
        double TangentRadius,
        double BitangentRadius);

    private readonly record struct FusedLink(
        CreatureVector3V1 Start,
        CreatureVector3V1 End,
        double StartRadius,
        double EndRadius);

    private static readonly (int X, int Y, int Z)[] LatticeCornerOffsets =
    [
        (0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0),
        (0, 0, 1), (1, 0, 1), (1, 1, 1), (0, 1, 1)
    ];

    private static readonly (int A, int B, int C, int D)[] LatticeTetrahedra =
    [
        (0, 5, 1, 6), (0, 1, 2, 6), (0, 2, 3, 6),
        (0, 3, 7, 6), (0, 7, 4, 6), (0, 4, 5, 6)
    ];

    /// <summary>
    /// Fuses the anatomical ellipsoids into a single renderer mesh using a smooth signed-distance
    /// union. The input may already contain a static IK pose. Sensory markers remain separate.
    /// This is a display mesh; it does not replace anatomy, collision geometry, or simulation state.
    /// </summary>
    public CreatureGeometryV1 BuildUnifiedRenderGeometry(
        DevelopedBodyV1 anatomy,
        CreatureVisualRecipeV1 visualRecipe,
        CreatureGeometryV1 sourceGeometry,
        int longitudinalSegments = 6,
        int circumferentialSegments = 8)
    {
        ArgumentNullException.ThrowIfNull(anatomy);
        ArgumentNullException.ThrowIfNull(sourceGeometry);
        if (sourceGeometry.StructuralSignature != anatomy.StructuralSignature
            || sourceGeometry.PartCount != anatomy.Parts.Count
            || visualRecipe.StructuralSignature != anatomy.StructuralSignature
            || visualRecipe.SpeciesId != anatomy.SpeciesId)
            throw new ArgumentException("Source geometry, anatomy and pigment recipe must identify the same body.");
        if (longitudinalSegments is < 2 or > 24 || longitudinalSegments % 2 != 0
            || circumferentialSegments is < 4 or > 32 || circumferentialSegments % 4 != 0)
            throw new ArgumentOutOfRangeException(nameof(longitudinalSegments),
                "Unified geometry requires an even longitudinal count and circumferential count divisible by four.");

        var primaryVertexCount = checked((longitudinalSegments + 1) * (circumferentialSegments + 1));
        var shapeById = sourceGeometry.Surfaces
            .Where(surface => !surface.SurfaceId.StartsWith("sensor.", StringComparison.Ordinal))
            .ToDictionary(surface => surface.SurfaceId, StringComparer.Ordinal);
        if (shapeById.Count != anatomy.Parts.Count)
            throw new ArgumentException("Source geometry must contain one primary surface per anatomical part.", nameof(sourceGeometry));

        var stride = circumferentialSegments + 1;
        var quarterCircumferential = circumferentialSegments / 4;
        var equatorRing = longitudinalSegments / 2;
        var shapes = new FusedEllipsoid[anatomy.Parts.Count];
        for (var partIndex = 0; partIndex < anatomy.Parts.Count; partIndex++)
        {
            var part = anatomy.Parts[partIndex];
            var surface = shapeById[part.PartId];
            if (surface.VertexCount < primaryVertexCount)
                throw new ArgumentException($"Surface '{part.PartId}' does not contain a complete primary ellipsoid.", nameof(sourceGeometry));

            var negativePole = sourceGeometry.Vertices[surface.FirstVertex].PositionM;
            var positivePole = sourceGeometry.Vertices[surface.FirstVertex + longitudinalSegments * stride].PositionM;
            var axisVector = Subtract(positivePole, negativePole);
            var axialRadius = Length(axisVector) * 0.5;
            var center = Scale(Add(negativePole, positivePole), 0.5);
            var axis = Normalize(axisVector);
            var equator = surface.FirstVertex + equatorRing * stride;
            var tangentVector = Subtract(sourceGeometry.Vertices[equator].PositionM, center);
            var bitangentVector = Subtract(sourceGeometry.Vertices[equator + quarterCircumferential].PositionM, center);
            var tangentRadius = Length(tangentVector);
            var bitangentRadius = Length(bitangentVector);
            if (!FinitePositive(axialRadius) || !FinitePositive(tangentRadius) || !FinitePositive(bitangentRadius))
                throw new ArgumentException($"Surface '{part.PartId}' cannot define a finite ellipsoid.", nameof(sourceGeometry));

            shapes[partIndex] = new(partIndex, center, axis, Normalize(tangentVector),
                Normalize(bitangentVector), axialRadius, tangentRadius, bitangentRadius);
        }

        var gridStep = Math.Clamp(anatomy.BodyLengthM / 36, 0.006, 0.05);
        var blendRadius = Math.Max(gridStep * 1.2, anatomy.BodyLengthM * 0.045);
        var links = new List<FusedLink>();
        for (var partIndex = 0; partIndex < anatomy.Parts.Count; partIndex++)
        {
            var parentIndex = anatomy.Parts[partIndex].ParentPartIndex;
            if (parentIndex < 0) continue;
            var parent = shapes[parentIndex];
            var child = shapes[partIndex];
            var parentRadius = Math.Min(parent.TangentRadius, parent.BitangentRadius);
            var childRadius = Math.Min(child.TangentRadius, child.BitangentRadius);
            links.Add(new(parent.Center, child.Center,
                Math.Max(gridStep * 1.1, parentRadius * 0.52),
                Math.Max(gridStep * 1.1, childRadius * 0.52)));
        }
        var margin = gridStep * 2;
        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var minZ = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;
        var maxZ = double.NegativeInfinity;
        foreach (var shape in shapes)
        {
            var extentX = EllipsoidExtent(shape.Axis.X, shape.Tangent.X, shape.Bitangent.X,
                shape.AxialRadius, shape.TangentRadius, shape.BitangentRadius);
            var extentY = EllipsoidExtent(shape.Axis.Y, shape.Tangent.Y, shape.Bitangent.Y,
                shape.AxialRadius, shape.TangentRadius, shape.BitangentRadius);
            var extentZ = EllipsoidExtent(shape.Axis.Z, shape.Tangent.Z, shape.Bitangent.Z,
                shape.AxialRadius, shape.TangentRadius, shape.BitangentRadius);
            minX = Math.Min(minX, shape.Center.X - extentX);
            minY = Math.Min(minY, shape.Center.Y - extentY);
            minZ = Math.Min(minZ, shape.Center.Z - extentZ);
            maxX = Math.Max(maxX, shape.Center.X + extentX);
            maxY = Math.Max(maxY, shape.Center.Y + extentY);
            maxZ = Math.Max(maxZ, shape.Center.Z + extentZ);
        }

        minX -= margin; minY -= margin; minZ -= margin;
        maxX += margin; maxY += margin; maxZ += margin;
        var countX = GridDimension(minX, maxX, gridStep);
        var countY = GridDimension(minY, maxY, gridStep);
        var countZ = GridDimension(minZ, maxZ, gridStep);
        var gridCount = checked(countX * countY * countZ);
        if (gridCount > 2_000_000)
            throw new ArgumentException("Anatomical envelope exceeds the unified render-mesh grid budget.", nameof(anatomy));

        var field = new double[gridCount];
        var positions = new CreatureVector3V1[gridCount];
        for (var z = 0; z < countZ; z++)
        for (var y = 0; y < countY; y++)
        for (var x = 0; x < countX; x++)
        {
            var index = GridIndex(x, y, z, countX, countY);
            var position = new CreatureVector3V1(minX + x * gridStep, minY + y * gridStep,
                minZ + z * gridStep);
            positions[index] = position;
            field[index] = SmoothUnionDistance(position, shapes, links, blendRadius);
        }

        var gradients = BuildGridGradients(field, countX, countY, countZ, gridStep);
        var vertices = new List<CreatureGeometryVertexV1>();
        var indices = new List<int>();
        var edgeVertices = new Dictionary<long, int>();
        var cellCorners = new int[8];

        for (var z = 0; z < countZ - 1; z++)
        for (var y = 0; y < countY - 1; y++)
        for (var x = 0; x < countX - 1; x++)
        {
            for (var corner = 0; corner < LatticeCornerOffsets.Length; corner++)
            {
                var offset = LatticeCornerOffsets[corner];
                cellCorners[corner] = GridIndex(x + offset.X, y + offset.Y, z + offset.Z, countX, countY);
            }

            foreach (var tetrahedron in LatticeTetrahedra)
                ProcessTetrahedron(cellCorners[tetrahedron.A], cellCorners[tetrahedron.B],
                    cellCorners[tetrahedron.C], cellCorners[tetrahedron.D]);
        }

        if (indices.Count == 0)
            throw new InvalidOperationException("The unified anatomy field did not produce an isosurface.");

        var surfaces = new List<CreatureGeometrySurfaceV1>
        {
            new("anatomy.unified", 0, vertices.Count, 0, indices.Count)
        };
        AppendSensorySurfaces(sourceGeometry, vertices, indices, surfaces);
        var featureCount = surfaces.Count - 1;
        return new(GeometryVersion, anatomy.StructuralSignature, anatomy.Parts.Count,
            featureCount, vertices, indices, surfaces);

        void ProcessTetrahedron(int a, int b, int c, int d)
        {
            Span<int> inside = stackalloc int[4];
            Span<int> outside = stackalloc int[4];
            var insideCount = 0;
            var outsideCount = 0;
            if (field[a] <= 0) inside[insideCount++] = a; else outside[outsideCount++] = a;
            if (field[b] <= 0) inside[insideCount++] = b; else outside[outsideCount++] = b;
            if (field[c] <= 0) inside[insideCount++] = c; else outside[outsideCount++] = c;
            if (field[d] <= 0) inside[insideCount++] = d; else outside[outsideCount++] = d;

            if (insideCount == 0 || insideCount == 4) return;
            var outwardGradient = TetrahedronGradient(a, b, c, d);
            if (insideCount == 1)
            {
                AddTriangle(GetIsoVertex(inside[0], outside[0]),
                    GetIsoVertex(inside[0], outside[1]), GetIsoVertex(inside[0], outside[2]), outwardGradient);
                return;
            }
            if (insideCount == 3)
            {
                AddTriangle(GetIsoVertex(outside[0], inside[0]),
                    GetIsoVertex(outside[0], inside[1]), GetIsoVertex(outside[0], inside[2]), outwardGradient);
                return;
            }

            var first = GetIsoVertex(inside[0], outside[0]);
            var second = GetIsoVertex(inside[0], outside[1]);
            var third = GetIsoVertex(inside[1], outside[0]);
            var fourth = GetIsoVertex(inside[1], outside[1]);
            AddTriangle(first, second, fourth, outwardGradient);
            AddTriangle(first, fourth, third, outwardGradient);
        }

        CreatureVector3V1 TetrahedronGradient(int a, int b, int c, int d)
        {
            var edgeA = Subtract(positions[b], positions[a]);
            var edgeB = Subtract(positions[c], positions[a]);
            var edgeC = Subtract(positions[d], positions[a]);
            var reciprocalA = Cross(edgeB, edgeC);
            var reciprocalB = Cross(edgeC, edgeA);
            var reciprocalC = Cross(edgeA, edgeB);
            var determinant = Dot(edgeA, reciprocalA);
            if (Math.Abs(determinant) <= 1e-18)
                throw new InvalidOperationException("The unified isosurface lattice contains a degenerate tetrahedron.");

            var gradient = Scale(Add(
                Add(Scale(reciprocalA, field[b] - field[a]),
                    Scale(reciprocalB, field[c] - field[a])),
                Scale(reciprocalC, field[d] - field[a])), 1 / determinant);
            if (!double.IsFinite(gradient.X) || !double.IsFinite(gradient.Y) || !double.IsFinite(gradient.Z))
                throw new InvalidOperationException("The unified isosurface contains a non-finite orientation gradient.");
            return gradient;
        }

        int GetIsoVertex(int firstGridIndex, int secondGridIndex)
        {
            const double ZeroTolerance = 1e-12;
            var firstValue = field[firstGridIndex];
            var secondValue = field[secondGridIndex];
            var firstIsZero = Math.Abs(firstValue) <= ZeroTolerance;
            var secondIsZero = Math.Abs(secondValue) <= ZeroTolerance;
            var keyFirst = firstIsZero ? firstGridIndex : secondGridIndex;
            var keySecond = firstIsZero ? firstGridIndex : secondIsZero ? secondGridIndex : firstGridIndex;
            var low = Math.Min(keyFirst, keySecond);
            var high = Math.Max(keyFirst, keySecond);
            var key = ((long)low << 32) | (uint)high;
            if (edgeVertices.TryGetValue(key, out var existing)) return existing;

            var amount = firstIsZero ? 0
                : secondIsZero ? 1
                : Math.Abs(firstValue - secondValue) <= ZeroTolerance ? 0.5
                : Math.Clamp(firstValue / (firstValue - secondValue), 0, 1);
            var firstPosition = positions[firstGridIndex];
            var secondPosition = positions[secondGridIndex];
            var position = Add(firstPosition, Scale(Subtract(secondPosition, firstPosition), amount));
            var normal = Normalize(Add(Scale(gradients[firstGridIndex], 1 - amount),
                Scale(gradients[secondGridIndex], amount)));
            var nearestShapeIndex = NearestShape(position, shapes);
            var shape = shapes[nearestShapeIndex];
            var delta = Subtract(position, shape.Center);
            var axialFraction = Math.Clamp(Dot(delta, shape.Axis) / shape.AxialRadius, -1, 1);
            var tangentCoordinate = Dot(delta, shape.Tangent) / shape.TangentRadius;
            var bitangentCoordinate = Dot(delta, shape.Bitangent) / shape.BitangentRadius;
            var textureV = Math.Atan2(bitangentCoordinate, tangentCoordinate) / Math.Tau;
            if (textureV < 0) textureV += 1;
            var phase = CounterRandom.Unit(visualRecipe.PatternSeed, PatternPhaseTag,
                (ulong)shape.PartIndex, (ulong)visualRecipe.RecipeVersion, 0, 0) * Math.Tau;
            var stripe = Math.Cos((axialFraction + 1) * shape.AxialRadius
                                  * visualRecipe.PatternFrequencyPerM * Math.PI * 2 + phase);
            var baseColor = HueToRgba(visualRecipe.BaseHueDegrees);
            var color = stripe > 0.45
                ? ShadeRgba(baseColor, 1 - visualRecipe.PatternContrast01 * 0.52)
                : baseColor;
            var vertexIndex = vertices.Count;
            vertices.Add(new(position, normal, (axialFraction + 1) * 0.5, textureV, color));
            edgeVertices.Add(key, vertexIndex);
            return vertexIndex;
        }

        void AddTriangle(int first, int second, int third, CreatureVector3V1 outwardGradient)
        {
            var a = vertices[first].PositionM;
            var b = vertices[second].PositionM;
            var c = vertices[third].PositionM;
            var face = Cross(Subtract(b, a), Subtract(c, a));
            if (Length(face) <= 1e-14) return;
            if (Dot(face, outwardGradient) < 0) (second, third) = (third, second);
            indices.Add(first);
            indices.Add(second);
            indices.Add(third);
        }
    }

    private static void AppendSensorySurfaces(
        CreatureGeometryV1 sourceGeometry,
        List<CreatureGeometryVertexV1> vertices,
        List<int> indices,
        List<CreatureGeometrySurfaceV1> surfaces)
    {
        foreach (var surface in sourceGeometry.Surfaces
                     .Where(value => value.SurfaceId.StartsWith("sensor.", StringComparison.Ordinal)))
        {
            var firstVertex = vertices.Count;
            var firstIndex = indices.Count;
            for (var vertex = surface.FirstVertex; vertex < surface.FirstVertex + surface.VertexCount; vertex++)
                vertices.Add(sourceGeometry.Vertices[vertex]);
            for (var index = surface.FirstIndex; index < surface.FirstIndex + surface.IndexCount; index++)
            {
                var localVertex = sourceGeometry.Indices[index] - surface.FirstVertex;
                if (localVertex < 0 || localVertex >= surface.VertexCount)
                    throw new ArgumentException($"Sensory surface '{surface.SurfaceId}' has an invalid index.", nameof(sourceGeometry));
                indices.Add(firstVertex + localVertex);
            }
            surfaces.Add(new(surface.SurfaceId, firstVertex, surface.VertexCount,
                firstIndex, surface.IndexCount));
        }
    }

    private static CreatureVector3V1[] BuildGridGradients(
        double[] field,
        int countX,
        int countY,
        int countZ,
        double step)
    {
        var gradients = new CreatureVector3V1[field.Length];
        for (var z = 0; z < countZ; z++)
        for (var y = 0; y < countY; y++)
        for (var x = 0; x < countX; x++)
        {
            var x0 = Math.Max(0, x - 1); var x1 = Math.Min(countX - 1, x + 1);
            var y0 = Math.Max(0, y - 1); var y1 = Math.Min(countY - 1, y + 1);
            var z0 = Math.Max(0, z - 1); var z1 = Math.Min(countZ - 1, z + 1);
            var dx = (field[GridIndex(x1, y, z, countX, countY)]
                      - field[GridIndex(x0, y, z, countX, countY)]) / ((x1 - x0) * step);
            var dy = (field[GridIndex(x, y1, z, countX, countY)]
                      - field[GridIndex(x, y0, z, countX, countY)]) / ((y1 - y0) * step);
            var dz = (field[GridIndex(x, y, z1, countX, countY)]
                      - field[GridIndex(x, y, z0, countX, countY)]) / ((z1 - z0) * step);
            gradients[GridIndex(x, y, z, countX, countY)] = Normalize(new(dx, dy, dz));
        }
        return gradients;
    }

    private static double SmoothUnionDistance(
        CreatureVector3V1 point,
        IReadOnlyList<FusedEllipsoid> shapes,
        IReadOnlyList<FusedLink> links,
        double blendRadius)
    {
        var distance = DistanceToEllipsoid(point, shapes[0]);
        for (var index = 1; index < shapes.Count; index++)
            distance = SmoothMinimum(distance, DistanceToEllipsoid(point, shapes[index]), blendRadius);
        foreach (var link in links)
            distance = SmoothMinimum(distance, DistanceToLink(point, link), blendRadius);
        return distance;
    }

    private static double DistanceToLink(CreatureVector3V1 point, FusedLink link)
    {
        var segment = Subtract(link.End, link.Start);
        var lengthSquared = Dot(segment, segment);
        if (lengthSquared <= 1e-18) return Length(Subtract(point, link.Start)) - link.StartRadius;
        var amount = Math.Clamp(Dot(Subtract(point, link.Start), segment) / lengthSquared, 0, 1);
        var closestPoint = Add(link.Start, Scale(segment, amount));
        var radius = link.StartRadius + (link.EndRadius - link.StartRadius) * amount;
        return Length(Subtract(point, closestPoint)) - radius;
    }

    private static double DistanceToEllipsoid(CreatureVector3V1 point, FusedEllipsoid shape)
    {
        var delta = Subtract(point, shape.Center);
        var axial = Dot(delta, shape.Axis);
        var tangent = Dot(delta, shape.Tangent);
        var bitangent = Dot(delta, shape.Bitangent);
        var k0 = Math.Sqrt(axial * axial / (shape.AxialRadius * shape.AxialRadius)
                           + tangent * tangent / (shape.TangentRadius * shape.TangentRadius)
                           + bitangent * bitangent / (shape.BitangentRadius * shape.BitangentRadius));
        if (k0 <= 1e-12) return -Math.Min(shape.AxialRadius,
            Math.Min(shape.TangentRadius, shape.BitangentRadius));
        var k1 = Math.Sqrt(axial * axial / Math.Pow(shape.AxialRadius, 4)
                           + tangent * tangent / Math.Pow(shape.TangentRadius, 4)
                           + bitangent * bitangent / Math.Pow(shape.BitangentRadius, 4));
        return k1 > 1e-12 ? k0 * (k0 - 1) / k1 : -Math.Min(shape.AxialRadius,
            Math.Min(shape.TangentRadius, shape.BitangentRadius));
    }

    private static double SmoothMinimum(double first, double second, double blendRadius)
    {
        var amount = Math.Max(blendRadius - Math.Abs(first - second), 0) / blendRadius;
        return Math.Min(first, second) - amount * amount * blendRadius * 0.25;
    }

    private static int NearestShape(CreatureVector3V1 point, IReadOnlyList<FusedEllipsoid> shapes)
    {
        var nearest = 0;
        var distance = DistanceToEllipsoid(point, shapes[0]);
        for (var index = 1; index < shapes.Count; index++)
        {
            var candidate = DistanceToEllipsoid(point, shapes[index]);
            if (candidate >= distance) continue;
            nearest = index;
            distance = candidate;
        }
        return nearest;
    }

    private static double EllipsoidExtent(
        double axis,
        double tangent,
        double bitangent,
        double axialRadius,
        double tangentRadius,
        double bitangentRadius)
        => Math.Sqrt(axis * axis * axialRadius * axialRadius
                     + tangent * tangent * tangentRadius * tangentRadius
                     + bitangent * bitangent * bitangentRadius * bitangentRadius);

    private static int GridDimension(double minimum, double maximum, double step)
        => Math.Max(2, checked((int)Math.Ceiling((maximum - minimum) / step) + 1));

    private static int GridIndex(int x, int y, int z, int countX, int countY)
        => checked((z * countY + y) * countX + x);

    private static double Dot(CreatureVector3V1 first, CreatureVector3V1 second)
        => first.X * second.X + first.Y * second.Y + first.Z * second.Z;

    private static bool FinitePositive(double value) => double.IsFinite(value) && value > 0;
}
