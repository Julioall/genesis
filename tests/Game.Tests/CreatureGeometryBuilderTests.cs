using Game.Contracts;
using Game.Core;
using Game.Modules.Biosphere;
using Game.Procedural;

namespace Game.Tests;

public sealed class CreatureGeometryBuilderTests
{
    private readonly record struct SurfaceTriangle(
        int A,
        int B,
        int C,
        CreatureVector3V1 First,
        CreatureVector3V1 Second,
        CreatureVector3V1 Third,
        double MinX,
        double MaxX,
        double MinY,
        double MaxY,
        double MinZ,
        double MaxZ);

    private readonly CreatureDevelopmentModelV1 _development = new();
    private readonly CreatureGeometryBuilderV1 _builder = new();

    [Fact]
    public void Body_parts_produce_deterministic_bounded_renderer_neutral_ellipsoid_meshes()
    {
        foreach (var species in CreatureSpeciesCatalogV1.All)
        {
            var genome = _development.CreateGenome(species, 71, 91);
            var developed = _development.Develop(species, genome,
                new(species.MaturityDays * 2, 1, 1));
            var first = _builder.BuildGeometry(developed.Body, developed.VisualRecipe, 6, 8);
            var repeated = _builder.BuildGeometry(developed.Body, developed.VisualRecipe, 6, 8);

            Assert.Equal(first.Vertices, repeated.Vertices);
            Assert.Equal(first.Indices, repeated.Indices);
            Assert.Equal(developed.Body.StructuralSignature, first.StructuralSignature);
            Assert.Equal(developed.Body.Parts.Count, first.PartCount);
            Assert.Equal(developed.Body.Sensors.Count(sensor => sensor.Kind is SensoryKindV1.Vision or SensoryKindV1.Olfaction),
                first.FeatureCount);
            var surfaceCount = first.PartCount + first.FeatureCount;
            Assert.Equal(CreatureGeometryBuilderV1.GeometryVersion, first.GeometryVersion);
            Assert.Equal(surfaceCount, first.Surfaces.Count);
            var blendedJoints = developed.Body.Parts.Count(part => part.ParentPartIndex >= 0
                && part.Kind is AnatomicalPartKindV1.LimbProximal or AnatomicalPartKindV1.LimbDistal
                    or AnatomicalPartKindV1.AxialSegment or AnatomicalPartKindV1.Fin);
            Assert.Equal(surfaceCount * 7 * 9 + blendedJoints * 5 * 7, first.Vertices.Count);
            Assert.Equal(surfaceCount * 6 * 8 * 6 + blendedJoints * 4 * 6 * 6, first.Indices.Count);
            Assert.All(first.Indices, index => Assert.InRange(index, 0, first.Vertices.Count - 1));
            AssertSurfaceRangesCoverGeometry(first);
            AssertHierarchicalPartCenters(developed.Body, first, 6, 8);

            foreach (var vertex in first.Vertices)
            {
                AssertVectorFinite(vertex.PositionM);
                AssertVectorFinite(vertex.Normal);
                Assert.InRange(Length(vertex.Normal), 0.999999, 1.000001);
                Assert.InRange(vertex.U, 0, 1);
                Assert.InRange(vertex.V, 0, 1);
                Assert.Equal(0xFFu, vertex.Rgba & 0xFFu);
            }

            Assert.Throws<NotSupportedException>(() => ((IList<CreatureGeometryVertexV1>)first.Vertices)
                .Add(default));
            Assert.Throws<NotSupportedException>(() => ((IList<int>)first.Indices).Add(0));
        }
    }

    [Fact]
    public void Pigment_recipe_changes_vertex_color_without_changing_mesh_positions_or_topology()
    {
        var species = CreatureSpeciesCatalogV1.Quadruped;
        var firstGenome = _development.CreateGenome(species, 111, 222);
        var secondGenome = _development.CreateGenome(species, 111, 333);
        var context = new CreatureDevelopmentContextV1(species.MaturityDays, 1, 1);
        var firstBody = _development.Develop(species, firstGenome, context);
        var secondBody = _development.Develop(species, secondGenome, context);

        var first = _builder.BuildGeometry(firstBody.Body, firstBody.VisualRecipe);
        var second = _builder.BuildGeometry(secondBody.Body, secondBody.VisualRecipe);

        Assert.Equal(firstBody.Body.StructuralSignature, secondBody.Body.StructuralSignature);
        Assert.Equal(first.Indices, second.Indices);
        Assert.Equal(first.Vertices.Select(vertex => vertex.PositionM),
            second.Vertices.Select(vertex => vertex.PositionM));
        Assert.NotEqual(first.Vertices.Select(vertex => vertex.Rgba),
            second.Vertices.Select(vertex => vertex.Rgba));
    }

    [Fact]
    public void Quadruped_vision_and_olfaction_markers_intersect_the_head_surface()
    {
        var species = CreatureSpeciesCatalogV1.Quadruped;
        var developed = _development.Develop(species,
            _development.CreateGenome(species, 1171, 2281),
            new(species.MaturityDays * 2, 1, 1));
        var geometry = _builder.BuildGeometry(developed.Body, developed.VisualRecipe, 8, 12);
        var head = Assert.Single(developed.Body.Parts.Where(part => part.PartId == "head"));
        var visionMarkers = developed.Body.Sensors.Where(sensor => sensor.Kind == SensoryKindV1.Vision).ToArray();
        Assert.Equal(2, visionMarkers.Length);

        foreach (var eye in visionMarkers)
        {
            Assert.Contains(geometry.Surfaces,
                surface => surface.SurfaceId == "sensor." + eye.SensorId);
            var headCrossSection = head.RadiusM * Math.Sqrt(Math.Max(0,
                1 - Math.Pow(eye.LocalOffsetZM / (head.LengthM * 0.5), 2)));
            var eyeCenterRadius = Math.Sqrt(eye.LocalOffsetXM * eye.LocalOffsetXM
                + eye.LocalOffsetYM * eye.LocalOffsetYM);
            var eyeRadius = developed.Body.BodyLengthM * 0.012;
            Assert.True(eyeCenterRadius - eyeRadius < headCrossSection,
                $"'{eye.SensorId}' should intersect the head instead of floating away from it.");
            Assert.True(eyeCenterRadius + eyeRadius > headCrossSection,
                $"'{eye.SensorId}' should protrude from the head instead of being buried inside it.");
        }

        var nose = Assert.Single(developed.Body.Sensors.Where(sensor => sensor.SensorId == "nose"));
        Assert.Contains(geometry.Surfaces, surface => surface.SurfaceId == "sensor.nose");
        var noseRadius = developed.Body.BodyLengthM * 0.018;
        Assert.True(nose.LocalOffsetZM - noseRadius < head.LengthM * 0.5,
            "The nose marker should intersect the forward head surface instead of floating away.");
        Assert.True(nose.LocalOffsetZM + noseRadius > head.LengthM * 0.5,
            "The nose marker should extend beyond the head instead of being buried inside it.");
    }

    [Fact]
    public void Joint_blend_volumes_follow_the_anatomical_link_and_reach_both_part_centers()
    {
        var species = CreatureSpeciesCatalogV1.Quadruped;
        var developed = _development.Develop(species,
            _development.CreateGenome(species, 711, 811),
            new(species.MaturityDays * 2, 1, 1));
        var geometry = _builder.BuildGeometry(developed.Body, developed.VisualRecipe, 6, 8);
        const int primaryPartVertices = 7 * 9;
        const int blendLongitudinalSegments = 4;
        const int blendCircumferentialSegments = 6;
        var centers = new CreatureVector3V1[developed.Body.Parts.Count];
        var blendedPartCount = 0;

        for (var partIndex = 0; partIndex < developed.Body.Parts.Count; partIndex++)
        {
            var part = developed.Body.Parts[partIndex];
            var offset = new CreatureVector3V1(part.LocalOffsetXM, part.LocalOffsetYM, part.LocalOffsetZM);
            centers[partIndex] = part.ParentPartIndex < 0
                ? offset
                : Add(centers[part.ParentPartIndex], offset);
            if (part.ParentPartIndex < 0
                || part.Kind is not (AnatomicalPartKindV1.LimbProximal or AnatomicalPartKindV1.LimbDistal
                    or AnatomicalPartKindV1.AxialSegment or AnatomicalPartKindV1.Fin))
                continue;

            var surface = geometry.Surfaces.Single(value => value.SurfaceId == part.PartId);
            var blendStart = surface.FirstVertex + primaryPartVertices;
            var blendStride = blendCircumferentialSegments + 1;
            var negativePole = geometry.Vertices[blendStart].PositionM;
            var positivePole = geometry.Vertices[blendStart + blendLongitudinalSegments * blendStride].PositionM;
            var blendAxis = Normalize(Subtract(positivePole, negativePole));
            var anatomicalLink = Normalize(Subtract(centers[partIndex], centers[part.ParentPartIndex]));
            var centerDistance = Length(Subtract(centers[partIndex], centers[part.ParentPartIndex]));
            var blendHalfLength = Length(Subtract(positivePole, negativePole)) * 0.5;

            Assert.InRange(Dot(blendAxis, anatomicalLink), 0.999999, 1.000001);
            Assert.True(blendHalfLength > centerDistance * 0.5,
                $"The '{part.PartId}' blend should extend across both anatomical part centers.");
            blendedPartCount++;
        }

        Assert.True(blendedPartCount > 0);
    }

    [Fact]
    public void Unified_render_geometry_fuses_both_body_plans_into_one_connected_anatomical_surface()
    {
        foreach (var species in CreatureSpeciesCatalogV1.All)
        {
            var genome = _development.CreateGenome(species, 1771, 2881);
            var developed = _development.Develop(species, genome,
                new(species.MaturityDays * 2, 1, 1));
            var source = _builder.BuildGeometry(developed.Body, developed.VisualRecipe, 6, 8);
            if (species.Family == CreatureFamilyV1.Quadruped)
                source = CreaturePoseGeometryAdapterV1.ApplyStaticQuadrupedStance(
                    developed.Body, source, new CreaturePoseSolverV1());

            var fused = _builder.BuildUnifiedRenderGeometry(developed.Body,
                developed.VisualRecipe, source, 6, 8);
            var repeated = _builder.BuildUnifiedRenderGeometry(developed.Body,
                developed.VisualRecipe, source, 6, 8);
            var bodySurface = Assert.Single(fused.Surfaces.Where(surface => surface.SurfaceId == "anatomy.unified"));

            Assert.Equal(CreatureGeometryBuilderV1.GeometryVersion, fused.GeometryVersion);
            Assert.Equal(developed.Body.Parts.Count, fused.PartCount);
            Assert.Equal(source.FeatureCount, fused.FeatureCount);
            Assert.Equal(1 + fused.FeatureCount, fused.Surfaces.Count);
            Assert.Equal(fused.Vertices, repeated.Vertices);
            Assert.Equal(fused.Indices, repeated.Indices);
            Assert.True(CountConnectedComponents(fused, bodySurface) == 1,
                $"'{species.SpeciesId}' produced multiple anatomical components.");
            var edgeIncidences = CountEdgeIncidences(fused, bodySurface);
            Assert.NotEmpty(edgeIncidences);
            Assert.All(edgeIncidences.Values, incidence => Assert.Equal(2, incidence));
            AssertConsistentEdgeOrientation(fused, bodySurface, species.SpeciesId);
            AssertVertexLinksAreSingleCycles(fused, bodySurface, species.SpeciesId);
            AssertNoUnsharedTriangleIntersections(fused, bodySurface, species.SpeciesId);
            Assert.All(fused.Indices, index => Assert.InRange(index, 0, fused.Vertices.Count - 1));
            for (var index = bodySurface.FirstIndex;
                 index < bodySurface.FirstIndex + bodySurface.IndexCount;
                 index += 3)
            {
                var first = fused.Vertices[fused.Indices[index]].PositionM;
                var second = fused.Vertices[fused.Indices[index + 1]].PositionM;
                var third = fused.Vertices[fused.Indices[index + 2]].PositionM;
                Assert.True(Length(Cross(Subtract(second, first), Subtract(third, first))) > 1e-14,
                    $"'{species.SpeciesId}' produced a degenerate anatomical triangle.");
            }
            Assert.All(fused.Vertices, vertex =>
            {
                AssertVectorFinite(vertex.PositionM);
                AssertVectorFinite(vertex.Normal);
                Assert.InRange(Length(vertex.Normal), 0.999, 1.001);
            });
        }
    }

    [Fact]
    public void Unified_render_geometry_remains_manifold_across_genomes_and_tessellations()
    {
        var genomeCases = new (ulong GenomeSeed, ulong PigmentSeed)[]
        {
            (13, 29), (1771, 2881), (90210, 17321)
        };
        var tessellations = new (int Longitudinal, int Circumferential)[] { (6, 8), (8, 12) };

        foreach (var species in CreatureSpeciesCatalogV1.All)
        foreach (var (genomeSeed, pigmentSeed) in genomeCases)
        foreach (var (longitudinal, circumferential) in tessellations)
        {
            var genome = _development.CreateGenome(species, genomeSeed, pigmentSeed);
            var developed = _development.Develop(species, genome,
                new(species.MaturityDays * 2, 1, 1));
            var source = _builder.BuildGeometry(developed.Body, developed.VisualRecipe,
                longitudinal, circumferential);
            if (species.Family == CreatureFamilyV1.Quadruped)
                source = CreaturePoseGeometryAdapterV1.ApplyStaticQuadrupedStance(
                    developed.Body, source, new CreaturePoseSolverV1());

            var fused = _builder.BuildUnifiedRenderGeometry(developed.Body,
                developed.VisualRecipe, source, longitudinal, circumferential);
            var bodySurface = Assert.Single(fused.Surfaces.Where(surface => surface.SurfaceId == "anatomy.unified"));
            var identity = $"'{species.SpeciesId}' genome ({genomeSeed},{pigmentSeed}) at "
                + $"{longitudinal}x{circumferential}";

            Assert.True(CountConnectedComponents(fused, bodySurface) == 1,
                $"{identity} produced multiple anatomical components.");
            Assert.All(CountEdgeIncidences(fused, bodySurface).Values,
                incidence => Assert.Equal(2, incidence));
            AssertConsistentEdgeOrientation(fused, bodySurface, identity);
            AssertVertexLinksAreSingleCycles(fused, bodySurface, identity);
            AssertNoUnsharedTriangleIntersections(fused, bodySurface, identity);

            for (var index = bodySurface.FirstIndex; index < bodySurface.FirstIndex + bodySurface.IndexCount; index += 3)
            {
                var first = fused.Vertices[fused.Indices[index]].PositionM;
                var second = fused.Vertices[fused.Indices[index + 1]].PositionM;
                var third = fused.Vertices[fused.Indices[index + 2]].PositionM;
                Assert.True(Length(Cross(Subtract(second, first), Subtract(third, first))) > 1e-14,
                    $"{identity} produced a degenerate anatomical triangle.");
            }
        }
    }

    [Fact]
    public void Triangle_intersection_predicate_detects_coplanar_and_spatial_crossings()
    {
        var first = CreateSurfaceTriangle(0,
            new(0, 0, 0), new(2, 0, 0), new(0, 2, 0));
        var coplanarOverlap = CreateSurfaceTriangle(3,
            new(1, 0.25, 0), new(2.25, 0.25, 0), new(1, 1.5, 0));
        var spatialCrossing = CreateSurfaceTriangle(6,
            new(0.5, 0.5, -1), new(0.5, 0.5, 1), new(0.5, 1.5, 0));
        var coplanarSeparate = CreateSurfaceTriangle(9,
            new(3, 3, 0), new(4, 3, 0), new(3, 4, 0));
        var spatialSeparate = CreateSurfaceTriangle(12,
            new(3, 0.5, -1), new(3, 0.5, 1), new(3, 1.5, 0));

        Assert.True(TrianglesOverlap(first, coplanarOverlap));
        Assert.True(TrianglesOverlap(first, spatialCrossing));
        Assert.False(TrianglesOverlap(first, coplanarSeparate));
        Assert.False(TrianglesOverlap(first, spatialSeparate));
    }

    [Fact]
    public void Quadruped_pose_repositions_only_the_named_limb_surfaces()
    {
        var species = CreatureSpeciesCatalogV1.Quadruped;
        var developed = _development.Develop(species,
            _development.CreateGenome(species, 171, 272),
            new(species.MaturityDays * 2, 1, 1));
        var geometry = _builder.BuildGeometry(developed.Body, developed.VisualRecipe, 6, 8);
        var stanceFloorY = geometry.Vertices.Min(vertex => vertex.PositionM.Y);
        var solver = new CreaturePoseSolverV1();
        var poses = Enum.GetValues<QuadrupedLimbV1>().Select(limb =>
        {
            var upperPartId = limb switch
            {
                QuadrupedLimbV1.FrontLeft => "front.left.upper",
                QuadrupedLimbV1.FrontRight => "front.right.upper",
                QuadrupedLimbV1.HindLeft => "hind.left.upper",
                QuadrupedLimbV1.HindRight => "hind.right.upper",
                _ => throw new ArgumentOutOfRangeException(nameof(limb))
            };
            var upper = developed.Body.Parts.First(part => part.PartId == upperPartId);
            var hip = new CreatureVector3V1(upper.LocalOffsetXM, upper.LocalOffsetYM, upper.LocalOffsetZM);
            var foot = new CreatureVector3V1(hip.X, stanceFloorY, hip.Z);
            var bendSign = limb is QuadrupedLimbV1.FrontLeft or QuadrupedLimbV1.FrontRight ? -1 : 1;
            return solver.SolveQuadrupedLimb(developed.Body, limb, foot, bendSign);
        }).ToArray();

        var originalVertices = geometry.Vertices.ToArray();
        var posed = CreaturePoseGeometryAdapterV1.ApplyQuadrupedLimbPoses(developed.Body, geometry, poses);
        var repeatedPose = CreaturePoseGeometryAdapterV1.ApplyQuadrupedLimbPoses(developed.Body, geometry, poses);
        Assert.Equal(originalVertices, geometry.Vertices);
        Assert.Equal(posed.Vertices, repeatedPose.Vertices);
        var originalSurfaces = geometry.Surfaces.ToDictionary(surface => surface.SurfaceId);
        var posedSurfaces = posed.Surfaces.ToDictionary(surface => surface.SurfaceId);
        var torso = originalSurfaces["torso"];
        Assert.Equal(geometry.Vertices.Skip(torso.FirstVertex).Take(torso.VertexCount),
            posed.Vertices.Skip(torso.FirstVertex).Take(torso.VertexCount));
        Assert.Equal(geometry.Indices, posed.Indices);
        Assert.Equal(geometry.Surfaces, posed.Surfaces);

        foreach (var pose in poses)
        {
            var (upperId, lowerId) = pose.Limb switch
            {
                QuadrupedLimbV1.FrontLeft => ("front.left.upper", "front.left.lower"),
                QuadrupedLimbV1.FrontRight => ("front.right.upper", "front.right.lower"),
                QuadrupedLimbV1.HindLeft => ("hind.left.upper", "hind.left.lower"),
                QuadrupedLimbV1.HindRight => ("hind.right.upper", "hind.right.lower"),
                _ => throw new ArgumentOutOfRangeException(nameof(pose))
            };
            var upperSurface = posedSurfaces[upperId];
            var lowerSurface = posedSurfaces[lowerId];
            var oldUpper = originalSurfaces[upperId];
            Assert.NotEqual(geometry.Vertices.Skip(oldUpper.FirstVertex).Take(oldUpper.VertexCount)
                    .Select(vertex => vertex.PositionM),
                posed.Vertices.Skip(upperSurface.FirstVertex).Take(upperSurface.VertexCount)
                    .Select(vertex => vertex.PositionM));
            AssertSurfaceCenter(posed, upperSurface, pose.HipAnchorM, pose.KneePositionM, 6, 8);
            AssertSurfaceCenter(posed, lowerSurface, pose.KneePositionM, pose.FootPositionM, 6, 8);
        }
    }

    [Fact]
    public void Creature_geometry_and_static_stance_generation_do_not_change_simulation_state()
    {
        const ulong seed = 4927;
        const int ticks = 24;
        var population = new InitialAnimalPopulationV1(4, 2);
        var simulationWithoutVisuals = TestWorldFactory.Create(seed, population);
        var simulationWithVisuals = TestWorldFactory.Create(seed, population);
        simulationWithoutVisuals.AdvanceTicks(ticks);
        simulationWithVisuals.AdvanceTicks(ticks);

        var stateBeforeVisualGeneration = simulationWithVisuals.SaveToJson();
        var poseSolver = new CreaturePoseSolverV1();
        foreach (var animal in simulationWithVisuals.Snapshot().Animals)
        {
            var genome = animal.Genome ?? _development.CreateGenomeFromWorldSeed(
                CreatureSpeciesCatalogV1.Quadruped, seed, animal.AnimalId);
            var species = CreatureSpeciesCatalogV1.Resolve(genome.SpeciesId);
            if (species.Family != CreatureFamilyV1.Quadruped) continue;

            var developmentStep = Math.Clamp(
                (int)Math.Floor(Math.Max(0, animal.AgeDays) / species.MaturityDays * 12), 0, 12);
            var developed = _development.Develop(species, genome,
                new(species.MaturityDays * developmentStep / 12, 1, 1));
            var geometry = _builder.BuildGeometry(developed.Body, developed.VisualRecipe, 6, 8);
            _ = CreaturePoseGeometryAdapterV1.ApplyStaticQuadrupedStance(
                developed.Body, geometry, poseSolver);
        }

        var stateAfterVisualGeneration = simulationWithVisuals.SaveToJson();
        Assert.Equal(stateBeforeVisualGeneration, stateAfterVisualGeneration);
        Assert.Equal(simulationWithoutVisuals.SaveToJson(), stateAfterVisualGeneration);
    }

    [Fact]
    public void Fish_fins_are_flattened_across_the_body_depth()
    {
        var species = CreatureSpeciesCatalogV1.FreshwaterFish;
        var developed = _development.Develop(species,
            _development.CreateGenome(species, 901, 902),
            new(species.MaturityDays * 2, 1, 1));
        var geometry = _builder.BuildGeometry(developed.Body, developed.VisualRecipe);
        var caudalFinIndex = Array.FindIndex(developed.Body.Parts.ToArray(), part => part.PartId == "fin.caudal");

        Assert.True(caudalFinIndex >= 0);
        Assert.Equal(CreatureGeometryBuilderV1.GeometryVersion, geometry.GeometryVersion);
        var caudalSurface = geometry.Surfaces.Single(surface => surface.SurfaceId == "fin.caudal");
        var finVertices = geometry.Vertices.Skip(caudalSurface.FirstVertex).Take(7 * 9).ToArray();
        var heightY = finVertices.Max(vertex => vertex.PositionM.Y) - finVertices.Min(vertex => vertex.PositionM.Y);
        var widthX = finVertices.Max(vertex => vertex.PositionM.X) - finVertices.Min(vertex => vertex.PositionM.X);
        var thicknessZ = finVertices.Max(vertex => vertex.PositionM.Z) - finVertices.Min(vertex => vertex.PositionM.Z);

        Assert.InRange(heightY / widthX, 1.59, 1.61);
        Assert.InRange(thicknessZ / widthX, 0.31, 0.33);
    }

    [Fact]
    public void Geometry_builder_rejects_mismatched_recipes_and_unsupported_tessellation()
    {
        var species = CreatureSpeciesCatalogV1.FreshwaterFish;
        var developed = _development.Develop(species,
            _development.CreateGenome(species, 55, 66),
            new(species.MaturityDays, 1, 1));

        Assert.Throws<ArgumentException>(() => _builder.BuildGeometry(developed.Body,
            developed.VisualRecipe with { StructuralSignature = developed.Body.StructuralSignature + 1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => _builder.BuildGeometry(developed.Body,
            developed.VisualRecipe, longitudinalSegments: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _builder.BuildGeometry(developed.Body,
            developed.VisualRecipe, circumferentialSegments: 33));
    }

    private static double Length(CreatureVector3V1 value)
        => Math.Sqrt(value.X * value.X + value.Y * value.Y + value.Z * value.Z);

    private static void AssertHierarchicalPartCenters(
        DevelopedBodyV1 anatomy,
        CreatureGeometryV1 geometry,
        int longitudinalSegments,
        int circumferentialSegments)
    {
        var verticesPerPart = (longitudinalSegments + 1) * (circumferentialSegments + 1);
        var centers = new CreatureVector3V1[anatomy.Parts.Count];
        for (var partIndex = 0; partIndex < anatomy.Parts.Count; partIndex++)
        {
            var part = anatomy.Parts[partIndex];
            var offset = new CreatureVector3V1(part.LocalOffsetXM, part.LocalOffsetYM, part.LocalOffsetZM);
            centers[partIndex] = part.ParentPartIndex < 0
                ? offset
                : Add(centers[part.ParentPartIndex], offset);
            var first = geometry.Surfaces.Single(surface => surface.SurfaceId == part.PartId).FirstVertex;
            var oppositePole = first + longitudinalSegments * (circumferentialSegments + 1);
            var actualCenter = Scale(Add(geometry.Vertices[first].PositionM,
                geometry.Vertices[oppositePole].PositionM), 0.5);
            AssertVectorNear(centers[partIndex], actualCenter, 1e-10);
        }
    }

    private static void AssertSurfaceRangesCoverGeometry(CreatureGeometryV1 geometry)
    {
        var nextVertex = 0;
        var nextIndex = 0;
        foreach (var surface in geometry.Surfaces)
        {
            Assert.False(string.IsNullOrWhiteSpace(surface.SurfaceId));
            Assert.Equal(nextVertex, surface.FirstVertex);
            Assert.Equal(nextIndex, surface.FirstIndex);
            Assert.True(surface.VertexCount > 0);
            Assert.True(surface.IndexCount > 0);
            nextVertex += surface.VertexCount;
            nextIndex += surface.IndexCount;
        }
        Assert.Equal(geometry.Vertices.Count, nextVertex);
        Assert.Equal(geometry.Indices.Count, nextIndex);
    }

    private static int CountConnectedComponents(
        CreatureGeometryV1 geometry,
        CreatureGeometrySurfaceV1 surface)
    {
        var parents = Enumerable.Range(0, surface.VertexCount).ToArray();
        var used = new HashSet<int>();

        int Find(int value)
        {
            var root = value;
            while (parents[root] != root) root = parents[root];
            while (parents[value] != value)
            {
                var next = parents[value];
                parents[value] = root;
                value = next;
            }
            return root;
        }

        void Union(int first, int second)
        {
            var firstRoot = Find(first);
            var secondRoot = Find(second);
            if (firstRoot != secondRoot) parents[secondRoot] = firstRoot;
        }

        for (var index = surface.FirstIndex; index < surface.FirstIndex + surface.IndexCount; index += 3)
        {
            var first = geometry.Indices[index] - surface.FirstVertex;
            var second = geometry.Indices[index + 1] - surface.FirstVertex;
            var third = geometry.Indices[index + 2] - surface.FirstVertex;
            used.Add(first);
            used.Add(second);
            used.Add(third);
            Union(first, second);
            Union(second, third);
            Union(third, first);
        }

        return used.Select(Find).Distinct().Count();
    }

    private static Dictionary<(int First, int Second), int> CountEdgeIncidences(
        CreatureGeometryV1 geometry,
        CreatureGeometrySurfaceV1 surface)
    {
        var edgeCounts = new Dictionary<(int First, int Second), int>();
        for (var index = surface.FirstIndex; index < surface.FirstIndex + surface.IndexCount; index += 3)
        {
            AddEdge(geometry.Indices[index], geometry.Indices[index + 1]);
            AddEdge(geometry.Indices[index + 1], geometry.Indices[index + 2]);
            AddEdge(geometry.Indices[index + 2], geometry.Indices[index]);
        }
        return edgeCounts;

        void AddEdge(int first, int second)
        {
            var edge = first < second ? (first, second) : (second, first);
            edgeCounts.TryGetValue(edge, out var incidence);
            edgeCounts[edge] = incidence + 1;
        }
    }

    private static void AssertConsistentEdgeOrientation(
        CreatureGeometryV1 geometry,
        CreatureGeometrySurfaceV1 surface,
        string bodyPlanId)
    {
        var edges = new Dictionary<(int First, int Second), (int Count, int DirectionSum)>();
        for (var index = surface.FirstIndex; index < surface.FirstIndex + surface.IndexCount; index += 3)
        {
            AddEdge(geometry.Indices[index], geometry.Indices[index + 1]);
            AddEdge(geometry.Indices[index + 1], geometry.Indices[index + 2]);
            AddEdge(geometry.Indices[index + 2], geometry.Indices[index]);
        }

        Assert.All(edges, pair => Assert.True(pair.Value.Count == 2 && pair.Value.DirectionSum == 0,
            $"'{bodyPlanId}' edge {pair.Key} is not shared with opposite triangle winding."));

        void AddEdge(int first, int second)
        {
            var key = first < second ? (first, second) : (second, first);
            var direction = first < second ? 1 : -1;
            edges.TryGetValue(key, out var incidence);
            edges[key] = (incidence.Count + 1, incidence.DirectionSum + direction);
        }
    }

    private static void AssertVertexLinksAreSingleCycles(
        CreatureGeometryV1 geometry,
        CreatureGeometrySurfaceV1 surface,
        string bodyPlanId)
    {
        var links = new Dictionary<int, Dictionary<int, HashSet<int>>>();
        for (var index = surface.FirstIndex; index < surface.FirstIndex + surface.IndexCount; index += 3)
        {
            var first = geometry.Indices[index];
            var second = geometry.Indices[index + 1];
            var third = geometry.Indices[index + 2];
            AddLink(first, second, third);
            AddLink(second, third, first);
            AddLink(third, first, second);
        }

        Assert.Equal(surface.VertexCount, links.Count);
        foreach (var (vertex, link) in links)
        {
            Assert.All(link.Values, adjacent => Assert.Equal(2, adjacent.Count));
            var reached = new HashSet<int>();
            var pending = new Queue<int>();
            pending.Enqueue(link.Keys.First());
            while (pending.TryDequeue(out var neighbor))
            {
                if (!reached.Add(neighbor)) continue;
                foreach (var adjacent in link[neighbor])
                    pending.Enqueue(adjacent);
            }

            Assert.True(reached.Count == link.Count,
                $"'{bodyPlanId}' vertex {vertex} has a disconnected link, so it is not a 2-manifold vertex.");
        }

        void AddLink(int vertex, int firstNeighbor, int secondNeighbor)
        {
            if (!links.TryGetValue(vertex, out var link))
                links.Add(vertex, link = new Dictionary<int, HashSet<int>>());
            if (!link.TryGetValue(firstNeighbor, out var firstAdjacents))
                link.Add(firstNeighbor, firstAdjacents = new HashSet<int>());
            if (!link.TryGetValue(secondNeighbor, out var secondAdjacents))
                link.Add(secondNeighbor, secondAdjacents = new HashSet<int>());
            firstAdjacents.Add(secondNeighbor);
            secondAdjacents.Add(firstNeighbor);
        }
    }

    private static void AssertNoUnsharedTriangleIntersections(
        CreatureGeometryV1 geometry,
        CreatureGeometrySurfaceV1 surface,
        string bodyPlanId)
    {
        const double tolerance = 1e-9;
        var triangles = new List<SurfaceTriangle>(surface.IndexCount / 3);
        for (var index = surface.FirstIndex; index < surface.FirstIndex + surface.IndexCount; index += 3)
        {
            var a = geometry.Indices[index];
            var b = geometry.Indices[index + 1];
            var c = geometry.Indices[index + 2];
            var first = geometry.Vertices[a].PositionM;
            var second = geometry.Vertices[b].PositionM;
            var third = geometry.Vertices[c].PositionM;
            triangles.Add(new(a, b, c, first, second, third,
                Math.Min(first.X, Math.Min(second.X, third.X)), Math.Max(first.X, Math.Max(second.X, third.X)),
                Math.Min(first.Y, Math.Min(second.Y, third.Y)), Math.Max(first.Y, Math.Max(second.Y, third.Y)),
                Math.Min(first.Z, Math.Min(second.Z, third.Z)), Math.Max(first.Z, Math.Max(second.Z, third.Z))));
        }

        triangles.Sort((first, second) => first.MinX.CompareTo(second.MinX));
        for (var firstIndex = 0; firstIndex < triangles.Count; firstIndex++)
        {
            var first = triangles[firstIndex];
            for (var secondIndex = firstIndex + 1;
                 secondIndex < triangles.Count && triangles[secondIndex].MinX <= first.MaxX + tolerance;
                 secondIndex++)
            {
                var second = triangles[secondIndex];
                if (ShareVertex(first, second)
                    || first.MaxY < second.MinY - tolerance || second.MaxY < first.MinY - tolerance
                    || first.MaxZ < second.MinZ - tolerance || second.MaxZ < first.MinZ - tolerance)
                    continue;

                Assert.False(TrianglesOverlap(first, second),
                    $"'{bodyPlanId}' has an intersection between unshared mesh triangles "
                    + $"({first.A}, {first.B}, {first.C}) and ({second.A}, {second.B}, {second.C}).");
            }
        }
    }

    private static SurfaceTriangle CreateSurfaceTriangle(
        int firstIndex,
        CreatureVector3V1 first,
        CreatureVector3V1 second,
        CreatureVector3V1 third)
        => new(firstIndex, firstIndex + 1, firstIndex + 2, first, second, third,
            Math.Min(first.X, Math.Min(second.X, third.X)), Math.Max(first.X, Math.Max(second.X, third.X)),
            Math.Min(first.Y, Math.Min(second.Y, third.Y)), Math.Max(first.Y, Math.Max(second.Y, third.Y)),
            Math.Min(first.Z, Math.Min(second.Z, third.Z)), Math.Max(first.Z, Math.Max(second.Z, third.Z)));

    private static bool ShareVertex(SurfaceTriangle first, SurfaceTriangle second)
        => first.A == second.A || first.A == second.B || first.A == second.C
            || first.B == second.A || first.B == second.B || first.B == second.C
            || first.C == second.A || first.C == second.B || first.C == second.C;

    private static bool TrianglesOverlap(SurfaceTriangle first, SurfaceTriangle second)
    {
        var firstEdge0 = Subtract(first.Second, first.First);
        var firstEdge1 = Subtract(first.Third, first.Second);
        var firstEdge2 = Subtract(first.First, first.Third);
        var secondEdge0 = Subtract(second.Second, second.First);
        var secondEdge1 = Subtract(second.Third, second.Second);
        var secondEdge2 = Subtract(second.First, second.Third);
        var firstNormal = Cross(firstEdge0, Subtract(first.Third, first.First));
        var secondNormal = Cross(secondEdge0, Subtract(second.Third, second.First));

        if (Separates(firstNormal, first, second) || Separates(secondNormal, first, second)
            || Separates(Cross(firstNormal, firstEdge0), first, second)
            || Separates(Cross(firstNormal, firstEdge1), first, second)
            || Separates(Cross(firstNormal, firstEdge2), first, second)
            || Separates(Cross(secondNormal, secondEdge0), first, second)
            || Separates(Cross(secondNormal, secondEdge1), first, second)
            || Separates(Cross(secondNormal, secondEdge2), first, second))
            return false;

        var firstEdges = new[] { firstEdge0, firstEdge1, firstEdge2 };
        var secondEdges = new[] { secondEdge0, secondEdge1, secondEdge2 };
        foreach (var firstEdge in firstEdges)
        foreach (var secondEdge in secondEdges)
            if (Separates(Cross(firstEdge, secondEdge), first, second))
                return false;

        return true;
    }

    private static bool Separates(
        CreatureVector3V1 axis,
        SurfaceTriangle first,
        SurfaceTriangle second)
    {
        var axisLength = Length(axis);
        if (axisLength <= 1e-12) return false;
        var first0 = Dot(first.First, axis);
        var first1 = Dot(first.Second, axis);
        var first2 = Dot(first.Third, axis);
        var second0 = Dot(second.First, axis);
        var second1 = Dot(second.Second, axis);
        var second2 = Dot(second.Third, axis);
        var firstMin = Math.Min(first0, Math.Min(first1, first2));
        var firstMax = Math.Max(first0, Math.Max(first1, first2));
        var secondMin = Math.Min(second0, Math.Min(second1, second2));
        var secondMax = Math.Max(second0, Math.Max(second1, second2));
        var tolerance = 1e-10 * axisLength;
        return firstMax < secondMin - tolerance || secondMax < firstMin - tolerance;
    }

    private static void AssertSurfaceCenter(
        CreatureGeometryV1 geometry,
        CreatureGeometrySurfaceV1 surface,
        CreatureVector3V1 start,
        CreatureVector3V1 end,
        int longitudinalSegments,
        int circumferentialSegments)
    {
        var oppositePole = surface.FirstVertex + longitudinalSegments * (circumferentialSegments + 1);
        var center = Scale(Add(geometry.Vertices[surface.FirstVertex].PositionM,
            geometry.Vertices[oppositePole].PositionM), 0.5);
        var expected = Scale(Add(start, end), 0.5);
        AssertVectorNear(expected, center, 1e-10);
    }

    private static CreatureVector3V1 Add(CreatureVector3V1 first, CreatureVector3V1 second)
        => new(first.X + second.X, first.Y + second.Y, first.Z + second.Z);

    private static CreatureVector3V1 Scale(CreatureVector3V1 value, double scale)
        => new(value.X * scale, value.Y * scale, value.Z * scale);

    private static CreatureVector3V1 Subtract(CreatureVector3V1 first, CreatureVector3V1 second)
        => new(first.X - second.X, first.Y - second.Y, first.Z - second.Z);

    private static CreatureVector3V1 Cross(CreatureVector3V1 first, CreatureVector3V1 second)
        => new(first.Y * second.Z - first.Z * second.Y,
            first.Z * second.X - first.X * second.Z,
            first.X * second.Y - first.Y * second.X);

    private static double Dot(CreatureVector3V1 first, CreatureVector3V1 second)
        => first.X * second.X + first.Y * second.Y + first.Z * second.Z;

    private static CreatureVector3V1 Normalize(CreatureVector3V1 value)
    {
        var length = Length(value);
        return length > 1e-12 ? Scale(value, 1 / length) : new(0, 1, 0);
    }

    private static void AssertVectorNear(CreatureVector3V1 expected, CreatureVector3V1 actual, double tolerance)
        => Assert.InRange(Length(new(expected.X - actual.X, expected.Y - actual.Y, expected.Z - actual.Z)), 0, tolerance);

    private static void AssertVectorFinite(CreatureVector3V1 value)
    {
        Assert.True(double.IsFinite(value.X));
        Assert.True(double.IsFinite(value.Y));
        Assert.True(double.IsFinite(value.Z));
    }
}
