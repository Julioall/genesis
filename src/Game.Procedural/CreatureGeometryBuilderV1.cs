using Game.Contracts;

namespace Game.Procedural;

/// <summary>
/// Builds renderer-neutral ellipsoid primitives from anatomical segment data. Selected articulated
/// joints receive link-aligned blend volumes; the result does not claim a watertight surface or collider.
/// </summary>
public sealed partial class CreatureGeometryBuilderV1 : ICreatureGeometryBuilderV1
{
    public const int GeometryVersion = 8;
    // Display-only silhouette corrections; these do not change DevelopedBody or claim calibration.
    private const double CaudalFinDisplayLengthScale = 1.6;
    private const double JointBlendLengthRatio = 1.40;
    private const ulong PatternPhaseTag = 0x5A34504154504841UL;
    private const uint Alpha = 0xFF;

    public CreatureGeometryV1 BuildGeometry(
        DevelopedBodyV1 anatomy,
        CreatureVisualRecipeV1 visualRecipe,
        int longitudinalSegments = 6,
        int circumferentialSegments = 8)
    {
        ArgumentNullException.ThrowIfNull(anatomy);
        if (anatomy.Parts.Count == 0)
            throw new ArgumentException("Developed anatomy must have at least one part.", nameof(anatomy));
        if (visualRecipe.RecipeVersion != 1 || visualRecipe.SpeciesId != anatomy.SpeciesId
            || visualRecipe.StructuralSignature != anatomy.StructuralSignature)
            throw new ArgumentException("Visual recipe identity/version does not match the developed body.", nameof(visualRecipe));
        if (!double.IsFinite(visualRecipe.BaseHueDegrees) || visualRecipe.BaseHueDegrees is < 0 or > 360
            || !double.IsFinite(visualRecipe.PatternFrequencyPerM) || visualRecipe.PatternFrequencyPerM < 0
            || !double.IsFinite(visualRecipe.PatternContrast01) || visualRecipe.PatternContrast01 is < 0 or > 1)
            throw new ArgumentException("Visual recipe contains invalid pigment values.", nameof(visualRecipe));
        if (longitudinalSegments is < 2 or > 24 || circumferentialSegments is < 3 or > 32)
            throw new ArgumentOutOfRangeException(nameof(longitudinalSegments),
                "Geometry tessellation requires 2–24 longitudinal and 3–32 circumferential segments.");

        var verticesPerPart = checked((longitudinalSegments + 1) * (circumferentialSegments + 1));
        var trianglesPerPart = checked(longitudinalSegments * circumferentialSegments * 2);
        var vertices = new List<CreatureGeometryVertexV1>(checked(anatomy.Parts.Count * verticesPerPart));
        var indices = new List<int>(checked(anatomy.Parts.Count * trianglesPerPart * 3));
        var surfaces = new List<CreatureGeometrySurfaceV1>(anatomy.Parts.Count + anatomy.Sensors.Count);
        var baseColor = HueToRgba(visualRecipe.BaseHueDegrees);
        var bodyLocalCenters = new CreatureVector3V1[anatomy.Parts.Count];

        for (var partIndex = 0; partIndex < anatomy.Parts.Count; partIndex++)
        {
            var part = anatomy.Parts[partIndex];
            if (!double.IsFinite(part.LengthM) || part.LengthM <= 0
                || !double.IsFinite(part.RadiusM) || part.RadiusM <= 0
                || !double.IsFinite(part.LocalOffsetXM) || !double.IsFinite(part.LocalOffsetYM)
                || !double.IsFinite(part.LocalOffsetZM))
                throw new ArgumentException($"Anatomical part '{part.PartId}' has invalid geometry.", nameof(anatomy));
            if (part.ParentPartIndex < -1 || part.ParentPartIndex >= partIndex)
                throw new ArgumentException($"Anatomical part '{part.PartId}' has an invalid parent index.", nameof(anatomy));

            var localOffset = new CreatureVector3V1(part.LocalOffsetXM, part.LocalOffsetYM, part.LocalOffsetZM);
            bodyLocalCenters[partIndex] = part.ParentPartIndex < 0
                ? localOffset
                : Add(bodyLocalCenters[part.ParentPartIndex], localOffset);
            var tangentRadiusScale = part.Kind == AnatomicalPartKindV1.Fin
                && (part.Side != AnatomicalSideV1.Center || part.PartId == "fin.caudal") ? 0.32 : 1.0;
            var bitangentRadiusScale = part.Kind == AnatomicalPartKindV1.Fin
                && part.Side == AnatomicalSideV1.Center && part.PartId != "fin.caudal" ? 0.22 : 1.0;
            var displayLengthM = part.PartId == "fin.caudal"
                ? part.LengthM * CaudalFinDisplayLengthScale
                : part.LengthM;
            var firstVertex = vertices.Count;
            var firstIndex = indices.Count;
            AddEllipsoid(partIndex, part.PartId, part.Kind, part.Side, displayLengthM, part.RadiusM,
                bodyLocalCenters[partIndex], visualRecipe, baseColor,
                longitudinalSegments, circumferentialSegments, vertices, indices,
                tangentRadiusScale: tangentRadiusScale, bitangentRadiusScale: bitangentRadiusScale);
            if (part.ParentPartIndex >= 0 && JointBlendRadiusRatio(part.Kind) > 0)
            {
                var parent = anatomy.Parts[part.ParentPartIndex];
                var parentCenter = bodyLocalCenters[part.ParentPartIndex];
                var childCenter = bodyLocalCenters[partIndex];
                var link = Subtract(childCenter, parentCenter);
                var linkLength = Length(link);
                var jointRadius = Math.Max(parent.RadiusM, part.RadiusM) * JointBlendRadiusRatio(part.Kind);
                var jointCenter = Scale(Add(parentCenter, childCenter), 0.5);
                var jointLength = Math.Max(jointRadius * 1.5, linkLength * JointBlendLengthRatio);
                var jointLongitudinalSegments = Math.Min(longitudinalSegments, 4);
                var jointCircumferentialSegments = Math.Min(circumferentialSegments, 6);
                AddEllipsoid(partIndex, part.PartId, part.Kind, part.Side, jointLength, jointRadius,
                    jointCenter, visualRecipe, baseColor,
                    jointLongitudinalSegments, jointCircumferentialSegments, vertices, indices,
                    tangentRadiusScale: tangentRadiusScale, bitangentRadiusScale: bitangentRadiusScale,
                    axisOverride: linkLength > 1e-12 ? Scale(link, 1 / linkLength) : null);
            }
            surfaces.Add(new(part.PartId, firstVertex, vertices.Count - firstVertex,
                firstIndex, indices.Count - firstIndex));
        }

        var featureCount = 0;
        for (var sensorIndex = 0; sensorIndex < anatomy.Sensors.Count; sensorIndex++)
        {
            var sensor = anatomy.Sensors[sensorIndex];
            if (sensor.Kind is not (SensoryKindV1.Vision or SensoryKindV1.Olfaction)) continue;
            if (sensor.PartIndex < 0 || sensor.PartIndex >= anatomy.Parts.Count
                || !double.IsFinite(sensor.LocalOffsetXM) || !double.IsFinite(sensor.LocalOffsetYM)
                || !double.IsFinite(sensor.LocalOffsetZM))
                throw new ArgumentException($"Sensory marker '{sensor.SensorId}' has invalid coordinates.", nameof(anatomy));

            var eyeRadiusRatio = anatomy.Family == CreatureFamilyV1.Fish ? 0.020 : 0.012;
            var radius = anatomy.BodyLengthM * (sensor.Kind == SensoryKindV1.Vision
                ? eyeRadiusRatio
                : 0.018);
            if (!double.IsFinite(radius) || radius <= 0)
                throw new ArgumentException("Body length is invalid for sensory markers.", nameof(anatomy));
            var center = Add(bodyLocalCenters[sensor.PartIndex],
                new(sensor.LocalOffsetXM, sensor.LocalOffsetYM, sensor.LocalOffsetZM));
            var side = sensor.LocalOffsetXM < 0 ? AnatomicalSideV1.Left
                : sensor.LocalOffsetXM > 0 ? AnatomicalSideV1.Right : AnatomicalSideV1.Center;
            var markerColor = sensor.Kind == SensoryKindV1.Vision ? 0x171A18FFu : 0x38271EFFu;
            var firstVertex = vertices.Count;
            var firstIndex = indices.Count;
            AddEllipsoid(anatomy.Parts.Count + featureCount, "sensor." + sensor.SensorId,
                AnatomicalPartKindV1.Head, side, radius * 2, radius, center,
                visualRecipe with { PatternFrequencyPerM = 0, PatternContrast01 = 0 }, markerColor,
                longitudinalSegments, circumferentialSegments, vertices, indices, markerColor);
            surfaces.Add(new("sensor." + sensor.SensorId, firstVertex, vertices.Count - firstVertex,
                firstIndex, indices.Count - firstIndex));
            featureCount++;
        }

        return new(GeometryVersion, anatomy.StructuralSignature, anatomy.Parts.Count,
            featureCount, vertices, indices, surfaces);
    }

    private static void AddEllipsoid(
        int surfaceIndex,
        string surfaceId,
        AnatomicalPartKindV1 kind,
        AnatomicalSideV1 side,
        double lengthM,
        double radiusM,
        CreatureVector3V1 bodyLocalCenter,
        CreatureVisualRecipeV1 recipe,
        uint baseColor,
        int longitudinalSegments,
        int circumferentialSegments,
        List<CreatureGeometryVertexV1> vertices,
        List<int> indices,
        uint? solidColor = null,
        double tangentRadiusScale = 1,
        double bitangentRadiusScale = 1,
        CreatureVector3V1? axisOverride = null)
    {
        var axis = axisOverride ?? PartAxis(surfaceId, kind, side);
        var helper = Math.Abs(axis.Y) < 0.9 ? new CreatureVector3V1(0, 1, 0) : new(1, 0, 0);
        var tangent = Normalize(Cross(axis, helper));
        var bitangent = Normalize(Cross(axis, tangent));
        var center = bodyLocalCenter;
        var halfLength = lengthM * 0.5;
        var tangentRadius = radiusM * tangentRadiusScale;
        var bitangentRadius = radiusM * bitangentRadiusScale;
        if (!double.IsFinite(tangentRadius) || tangentRadius <= 0
            || !double.IsFinite(bitangentRadius) || bitangentRadius <= 0)
            throw new ArgumentOutOfRangeException(nameof(tangentRadiusScale),
                "Ellipsoid radial scale factors must produce positive finite radii.");
        var firstVertex = vertices.Count;
        var phase = CounterRandom.Unit(recipe.PatternSeed, PatternPhaseTag, (ulong)surfaceIndex,
            (ulong)recipe.RecipeVersion, 0, 0) * Math.PI * 2;

        for (var longitudinal = 0; longitudinal <= longitudinalSegments; longitudinal++)
        {
            var latitude = -Math.PI * 0.5 + Math.PI * longitudinal / longitudinalSegments;
            var along = Math.Sin(latitude);
            var radial = Math.Cos(latitude);
            for (var circumferential = 0; circumferential <= circumferentialSegments; circumferential++)
            {
                var angle = Math.PI * 2 * circumferential / circumferentialSegments;
                var radialTangent = radial * Math.Cos(angle);
                var radialBitangent = radial * Math.Sin(angle);
                var position = Add(center, Add(Scale(axis, halfLength * along),
                    Add(Scale(tangent, tangentRadius * radialTangent),
                        Scale(bitangent, bitangentRadius * radialBitangent))));
                var normal = Normalize(Add(Scale(axis, along / halfLength),
                    Add(Scale(tangent, radialTangent / tangentRadius),
                        Scale(bitangent, radialBitangent / bitangentRadius))));
                var stripe = Math.Cos((along + 1) * 0.5 * lengthM
                                      * recipe.PatternFrequencyPerM * Math.PI * 2 + phase);
                var color = solidColor ?? (stripe > 0.45
                    ? ShadeRgba(baseColor, 1 - recipe.PatternContrast01 * 0.52)
                    : baseColor);
                var u = (double)longitudinal / longitudinalSegments;
                var v = (double)circumferential / circumferentialSegments;
                vertices.Add(new(position, normal, u, v, color));
            }
        }

        var stride = circumferentialSegments + 1;
        for (var longitudinal = 0; longitudinal < longitudinalSegments; longitudinal++)
        for (var circumferential = 0; circumferential < circumferentialSegments; circumferential++)
        {
            var first = firstVertex + longitudinal * stride + circumferential;
            var nextAround = first + 1;
            var nextRing = first + stride;
            var nextAroundRing = nextRing + 1;
            // Outward winding follows the ellipsoid parameterization.
            indices.Add(first);
            indices.Add(nextAround);
            indices.Add(nextRing);
            indices.Add(nextAround);
            indices.Add(nextAroundRing);
            indices.Add(nextRing);
        }
    }

    private static CreatureVector3V1 PartAxis(string partId, AnatomicalPartKindV1 kind, AnatomicalSideV1 side)
    {
        if (kind == AnatomicalPartKindV1.Torso)
            return new(0, 0, 1);
        if (kind is AnatomicalPartKindV1.Neck or AnatomicalPartKindV1.Head)
            return new(0, 0, 1);
        if (kind == AnatomicalPartKindV1.Tail || kind == AnatomicalPartKindV1.AxialSegment)
            return new(0, 0, -1);
        if (kind is AnatomicalPartKindV1.LimbProximal or AnatomicalPartKindV1.LimbDistal)
            return new(0, -1, 0);
        if (partId == "fin.caudal") return new(0, 1, 0);
        if (partId == "fin.dorsal") return new(0, 1, 0);
        if (partId == "fin.anal") return new(0, -1, 0);
        if (side == AnatomicalSideV1.Left) return new(-1, 0, 0);
        if (side == AnatomicalSideV1.Right) return new(1, 0, 0);
        return new(0, 1, 0);
    }

    private static double JointBlendRadiusRatio(AnatomicalPartKindV1 kind) => kind switch
    {
        AnatomicalPartKindV1.LimbProximal => 0.72,
        AnatomicalPartKindV1.LimbDistal => 0.72,
        AnatomicalPartKindV1.AxialSegment => 0.55,
        AnatomicalPartKindV1.Fin => 0.38,
        _ => 0
    };

    private static uint HueToRgba(double hueDegrees)
    {
        var hue = hueDegrees % 360 / 60;
        const double saturation = 0.58;
        const double value = 0.78;
        var chroma = value * saturation;
        var x = chroma * (1 - Math.Abs(hue % 2 - 1));
        var (red, green, blue) = hue switch
        {
            < 1 => (chroma, x, 0.0),
            < 2 => (x, chroma, 0.0),
            < 3 => (0.0, chroma, x),
            < 4 => (0.0, x, chroma),
            < 5 => (x, 0.0, chroma),
            _ => (chroma, 0.0, x)
        };
        var match = value - chroma;
        var r = ToByte(red + match);
        var g = ToByte(green + match);
        var b = ToByte(blue + match);
        return ((uint)r << 24) | ((uint)g << 16) | ((uint)b << 8) | Alpha;
    }

    private static uint ShadeRgba(uint rgba, double factor)
    {
        var red = ToByte(((rgba >> 24) & 0xFF) / 255.0 * factor);
        var green = ToByte(((rgba >> 16) & 0xFF) / 255.0 * factor);
        var blue = ToByte(((rgba >> 8) & 0xFF) / 255.0 * factor);
        return ((uint)red << 24) | ((uint)green << 16) | ((uint)blue << 8) | Alpha;
    }

    private static byte ToByte(double value)
        => (byte)Math.Clamp((int)Math.Round(value * 255), 0, 255);

    private static CreatureVector3V1 Cross(CreatureVector3V1 first, CreatureVector3V1 second)
        => new(first.Y * second.Z - first.Z * second.Y,
            first.Z * second.X - first.X * second.Z,
            first.X * second.Y - first.Y * second.X);

    private static CreatureVector3V1 Add(CreatureVector3V1 first, CreatureVector3V1 second)
        => new(first.X + second.X, first.Y + second.Y, first.Z + second.Z);

    private static CreatureVector3V1 Subtract(CreatureVector3V1 first, CreatureVector3V1 second)
        => new(first.X - second.X, first.Y - second.Y, first.Z - second.Z);

    private static CreatureVector3V1 Scale(CreatureVector3V1 value, double amount)
        => new(value.X * amount, value.Y * amount, value.Z * amount);

    private static double Length(CreatureVector3V1 value)
        => Math.Sqrt(value.X * value.X + value.Y * value.Y + value.Z * value.Z);

    private static CreatureVector3V1 Normalize(CreatureVector3V1 value)
    {
        var length = Math.Sqrt(value.X * value.X + value.Y * value.Y + value.Z * value.Z);
        return length > 1e-12 ? Scale(value, 1 / length) : new(0, 0, 0);
    }
}
