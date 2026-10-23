using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Contracts;
using Game.Client.Desktop;
using Game.Core;
using Game.Modules.Atmosphere;
using Game.Modules.Biosphere;
using Game.Modules.Cosmos;
using Game.Modules.Geosphere;
using Game.Modules.Hydrosphere;
using Game.Procedural;
using Microsoft.Xna.Framework;

namespace Game.Tests;

public sealed class L0WorldTests
{
    [Fact]
    public void Counter_random_matches_the_documented_L0_vector()
    {
        Assert.Equal(0x7BD5A964748A48D8UL, CounterRandom.Bits(1, 3, 0, 0, 0, 0));
        Assert.Equal(0.48372896864433657, CounterRandom.Unit(1, 3, 0, 0, 0, 0), 15);
        Assert.Equal(0xE44005EC9AA4A02CUL, CounterRandom.Bits(1, 3, 0, 0, 0, 1));
        var spatialValue = CounterRandom.SpatialUnit(1, 2, 9, 12, 10, 1, 1);
        Assert.Equal(spatialValue, CounterRandom.SpatialUnit(1, 2, 9, 12, 10, 1, 1));
        Assert.NotEqual(spatialValue, CounterRandom.SpatialUnit(2, 2, 9, 12, 10, 1, 1));
        Assert.InRange(spatialValue, 0, 1);
        Assert.True(Math.Abs(spatialValue - CounterRandom.SpatialUnit(1, 2, 8, 12, 10, 1, 1)) < 0.2);
    }

    [Fact]
    public void Same_seed_and_tick_partitions_produce_the_same_state()
    {
        var first = TestWorldFactory.Create(1);
        var second = TestWorldFactory.Create(1);
        first.AdvanceTicks(60);
        second.AdvanceTicks(20);
        second.AdvanceTicks(40);
        Assert.Equal(first.CanonicalHash(), second.CanonicalHash());
        Assert.NotEqual(first.CanonicalHash(), TestWorldFactory.Create(2).CanonicalHash());
    }

    [Fact]
    public void Procedural_terrain_has_seeded_relief_and_repeatable_geography()
    {
        var first = TerrainModel.GenerateElevationField(1);
        var repeat = TerrainModel.GenerateElevationField(1);
        var otherSeed = TerrainModel.GenerateElevationField(2);
        Assert.Equal(first, repeat);
        Assert.NotEqual(first, otherSeed);
        Assert.True(first.Max() - first.Min() > 45);
        Assert.True(first.Max() > 220, $"Expected a high mountain crest in the raised v16 terrain, got {first.Max():F1} m.");

        var rowsWithMountainCrests = Enumerable.Range(0, SimulationState.Height)
            .Count(y => Enumerable.Range(0, SimulationState.Width)
                .Count(x => first[y * SimulationState.Width + x] >= 120) >= 2);
        Assert.True(rowsWithMountainCrests >= 20,
            $"Expected a connected mountain chain across multiple rows, got {rowsWithMountainCrests} rows.");

        var steepestCardinalDrop = 0.0;
        for (var y = 0; y < SimulationState.Height; y++)
        for (var x = 0; x < SimulationState.Width; x++)
        {
            var current = first[y * SimulationState.Width + x];
            if (x + 1 < SimulationState.Width)
                steepestCardinalDrop = Math.Max(steepestCardinalDrop,
                    Math.Abs(current - first[y * SimulationState.Width + x + 1]));
            if (y + 1 < SimulationState.Height)
                steepestCardinalDrop = Math.Max(steepestCardinalDrop,
                    Math.Abs(current - first[(y + 1) * SimulationState.Width + x]));
        }
        Assert.True(steepestCardinalDrop >= 16);

    }

    [Fact]
    public void Main_ridge_axes_wander_independently_across_the_terrain()
    {
        var elevation = TerrainModel.GenerateElevationField(1);
        var westCrests = new List<int>();
        var eastCrests = new List<int>();
        for (var y = 8; y < SimulationState.Height - 8; y += 2)
        {
            westCrests.Add(Enumerable.Range(0, SimulationState.Width / 2)
                .MaxBy(x => elevation[y * SimulationState.Width + x]));
            eastCrests.Add(Enumerable.Range(SimulationState.Width / 2, SimulationState.Width / 2)
                .MaxBy(x => elevation[y * SimulationState.Width + x]));
        }

        Assert.True(westCrests.Distinct().Count() >= 4,
            $"The western uplift axis should bend across rows, but only occupied {westCrests.Distinct().Count()} columns.");
        Assert.True(eastCrests.Distinct().Count() >= 4,
            $"The eastern uplift axis should bend across rows, but only occupied {eastCrests.Distinct().Count()} columns.");
        Assert.True(westCrests.Max() - westCrests.Min() >= 4,
            "The western mountain crest should not be a straight line parallel to the river.");
        Assert.True(eastCrests.Max() - eastCrests.Min() >= 4,
            "The eastern mountain crest should not be a straight line parallel to the river.");
    }

    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(3UL)]
    public void Common_terrain_differences_are_reduced_without_losing_mountain_relief(ulong seed)
    {
        var elevation = TerrainModel.GenerateElevationField(seed);
        var surface = TerrainModel.CreateElevationSampler(seed);
        var underlying = TerrainModel.CreateUnderlyingElevationSampler(seed);
        var commonDrops = new List<double>();
        for (var y = 0; y < SimulationState.Height; y++)
        for (var x = 0; x < SimulationState.Width; x++)
        {
            var index = y * SimulationState.Width + x;
            AddNeighbor(x + 1 < SimulationState.Width, x + 1, y);
            AddNeighbor(y + 1 < SimulationState.Height, x, y + 1);

            void AddNeighbor(bool exists, int neighborX, int neighborY)
            {
                if (!exists) return;
                var neighborIndex = neighborY * SimulationState.Width + neighborX;
                var difference = Math.Abs(elevation[index] - elevation[neighborIndex]);
                var x0 = x + 0.5;
                var y0 = y + 0.5;
                var x1 = neighborX + 0.5;
                var y1 = neighborY + 0.5;
                if (Math.Abs(surface(x0, y0) - underlying(x0, y0)) <= 2
                    && Math.Abs(surface(x1, y1) - underlying(x1, y1)) <= 2)
                    commonDrops.Add(difference);
            }
        }

        commonDrops.Sort();
        var p95 = commonDrops[(int)Math.Ceiling(commonDrops.Count * 0.95) - 1];
        var baselineP95 = seed switch { 1 => 16.44, 2 => 18.37, _ => 16.84 };
        var baselineMaximum = seed switch { 1 => 39.96, 2 => 41.18, _ => 38.82 };
        Assert.True(p95 <= baselineP95 * 0.90, $"Seed {seed} common P95 {p95:F2} m did not improve by 10% from {baselineP95:F2} m.");
        Assert.True(commonDrops[^1] <= baselineMaximum, $"Seed {seed} maximum common physical drop {commonDrops[^1]:F2} m exceeds baseline {baselineMaximum:F2} m.");
        Assert.True(elevation.Max() > 180, $"Seed {seed} lost its mountain relief.");
        var outcrops = TerrainModel.GenerateOutcropGeometries(seed);
        Assert.Equal(7, outcrops.Count);
        foreach (var outcrop in outcrops)
        foreach (var point in outcrop.Rim)
            Assert.InRange(point.CrownElevationM - point.ToeElevationM, 18, 40);
        for (var first = 0; first < outcrops.Count; first++)
        for (var second = first + 1; second < outcrops.Count; second++)
            Assert.True(Math.Sqrt(Math.Pow(outcrops[first].CenterEastCell - outcrops[second].CenterEastCell, 2)
                + Math.Pow(outcrops[first].CenterNorthCell - outcrops[second].CenterNorthCell, 2)) > 12);
    }

    [Fact]
    public void Plateau_has_a_seeded_flat_crown_and_a_real_elevation_break()
    {
        var plateau = TerrainModel.GeneratePlateauGeometry(1);
        var repeated = TerrainModel.GeneratePlateauGeometry(1);
        var otherSeed = TerrainModel.GeneratePlateauGeometry(2);
        Assert.Equal(plateau.CenterEastCell, repeated.CenterEastCell);
        Assert.Equal(plateau.CenterNorthCell, repeated.CenterNorthCell);
        Assert.Equal(plateau.Rim, repeated.Rim);
        Assert.NotEqual(plateau.CenterEastCell, otherSeed.CenterEastCell);
        Assert.Equal(160, plateau.Rim.Count);

        var elevation = TerrainModel.CreateElevationSampler(1);
        var underlyingElevation = TerrainModel.CreateUnderlyingElevationSampler(1);
        var minimumRimRise = double.PositiveInfinity;
        var maximumRimRise = 0.0;
        var maximumRimSegmentCells = 0.0;
        var maximumRimRadius = 0.0;
        var minimumRimRadius = double.PositiveInfinity;
        for (var index = 0; index < plateau.Rim.Count; index++)
        {
            var point = plateau.Rim[index];
            var dx = point.EastCell - plateau.CenterEastCell;
            var dy = point.NorthCell - plateau.CenterNorthCell;
            var radius = Math.Sqrt(dx * dx + dy * dy);
            maximumRimRadius = Math.Max(maximumRimRadius, radius);
            minimumRimRadius = Math.Min(minimumRimRadius, radius);
            var inside = elevation(plateau.CenterEastCell + dx * 0.98, plateau.CenterNorthCell + dy * 0.98);
            var outside = elevation(plateau.CenterEastCell + dx * 1.02, plateau.CenterNorthCell + dy * 1.02);
            Assert.Equal(point.CrownElevationM, inside, 5);
            Assert.Equal(point.ToeElevationM, underlyingElevation(point.EastCell, point.NorthCell), 5);
            var rise = point.CrownElevationM - point.ToeElevationM;
            minimumRimRise = Math.Min(minimumRimRise, rise);
            maximumRimRise = Math.Max(maximumRimRise, rise);
            Assert.True(point.CrownElevationM > outside);

            var next = plateau.Rim[(index + 1) % plateau.Rim.Count];
            maximumRimSegmentCells = Math.Max(maximumRimSegmentCells,
                Math.Sqrt(Math.Pow(next.EastCell - point.EastCell, 2) + Math.Pow(next.NorthCell - point.NorthCell, 2)));
        }

        Assert.True(minimumRimRise >= 17.9);
        Assert.True(maximumRimRise >= 18);
        var mapScale = SimulationState.Width / 64.0;
        Assert.True(maximumRimRadius < 5.3 * mapScale,
            $"The weathered outcrop should remain local, but its radius is {maximumRimRadius:F2} cells.");
        Assert.True(maximumRimRadius - minimumRimRadius > 0.2, "The outcrop rim should retain irregular, non-elliptical relief after fitting its walls to the slope.");
        Assert.True(maximumRimSegmentCells < 1);
    }

    [Fact]
    public void Seeded_outcrop_cluster_has_three_separate_crowns_and_real_cliff_faces()
    {
        var features = TerrainModel.GenerateOutcropGeometries(1);
        var repeated = TerrainModel.GenerateOutcropGeometries(1);
        Assert.Equal(7, features.Count);
        Assert.Equal(features.Select(feature => feature.FeatureIndex), repeated.Select(feature => feature.FeatureIndex));
        Assert.Equal(features.SelectMany(feature => feature.Rim), repeated.SelectMany(feature => feature.Rim));

        var elevation = TerrainModel.CreateElevationSampler(1);
        var underlyingElevation = TerrainModel.CreateUnderlyingElevationSampler(1);
        foreach (var feature in features)
        {
            Assert.Equal(feature.Rim[0].CrownElevationM, elevation(feature.CenterEastCell, feature.CenterNorthCell), 5);
            var minimumRise = double.PositiveInfinity;
            var maximumRadius = 0.0;
            foreach (var point in feature.Rim)
            {
                var dx = point.EastCell - feature.CenterEastCell;
                var dy = point.NorthCell - feature.CenterNorthCell;
                maximumRadius = Math.Max(maximumRadius, Math.Sqrt(dx * dx + dy * dy));
                minimumRise = Math.Min(minimumRise, point.CrownElevationM - point.ToeElevationM);
                Assert.Equal(point.ToeElevationM, underlyingElevation(point.EastCell, point.NorthCell), 5);
                Assert.True(point.CrownElevationM > elevation(point.EastCell + dx * 0.02, point.NorthCell + dy * 0.02));
                Assert.InRange(point.CrownElevationM - point.ToeElevationM, 18, 40);
            }

            Assert.True(minimumRise >= 17.9, $"Outcrop {feature.FeatureIndex} has only {minimumRise:F1} m of physical relief.");
            if (feature.FeatureIndex > 0)
                Assert.True(maximumRadius < 3.1 * (SimulationState.Width / 64.0),
                    $"Secondary outcrop {feature.FeatureIndex} grew to {maximumRadius:F2} cells.");
        }

        for (var first = 0; first < features.Count; first++)
        for (var second = first + 1; second < features.Count; second++)
            Assert.True(Math.Sqrt(Math.Pow(features[first].CenterEastCell - features[second].CenterEastCell, 2)
                + Math.Pow(features[first].CenterNorthCell - features[second].CenterNorthCell, 2)) > 12,
                "Seeded outcrop crowns must remain separate physical features.");
    }

    [Fact]
    public void Desktop_terrain_snapshots_reject_a_field_from_another_generator_version()
    {
        var snapshot = TestWorldFactory.Create(1).Snapshot();
        TerrainSnapshotCompatibility.Validate(snapshot);

        var cells = snapshot.Cells.ToArray();
        cells[0] = cells[0] with { ElevationM = cells[0].ElevationM + 1 };
        var olderTerrain = new WorldSnapshot(snapshot.Seed, snapshot.TickIndex, cells, snapshot.Animals.ToArray(), new FluxLedgerState());
        Assert.Throws<InvalidDataException>(() => TerrainSnapshotCompatibility.Validate(olderTerrain));
    }

    [Fact]
    public void Snapshot_altitudes_and_D8_receivers_follow_the_physical_terrain_field()
    {
        var world = TestWorldFactory.Create(1).Snapshot();
        var elevations = TerrainModel.GenerateElevationField(world.Seed);
        var cells = world.Cells.ToArray();
        for (var index = 0; index < cells.Length; index++)
            Assert.Equal(elevations[index], cells[index].ElevationM, 8);

        var waterCells = TestWorldFactory.ToSurfaceWaterCells(cells);
        var drainage = SurfaceWaterModel.AnalyzeDrainage(waterCells);
        var drawableCascadeEdges = 0;
        for (var from = 0; from < drainage.Receiver.Length; from++)
        {
            var to = drainage.Receiver[from];
            if (to < 0) continue;
            Assert.True(waterCells[to].ElevationM < waterCells[from].ElevationM,
                $"D8 receiver {to} must be lower than source cell {from}.");
            if (drainage.Accumulation[from] >= SurfaceWaterModel.ChannelThresholdCells
                && (waterCells[from].SurfaceWaterMm >= 20 || waterCells[to].SurfaceWaterMm >= 20)
                && !(waterCells[from].SurfaceWaterMm >= 250 && waterCells[to].SurfaceWaterMm >= 250)
                && waterCells[from].ElevationM - waterCells[to].ElevationM >= 6)
                drawableCascadeEdges++;
        }
        Assert.True(drawableCascadeEdges > 0, "The canonical seed should route water over at least one real elevation break.");
    }

    [Fact]
    public void Priority_flood_preserves_the_bed_and_measures_level_pool_volume_and_area()
    {
        var terrain = TestWorldFactory.ToSurfaceWaterCells(TestWorldFactory.Create(1).Snapshot().Cells)
            .Select(cell => cell with { ElevationM = 0, SurfaceWaterMm = 0, GroundwaterMm = 0 })
            .ToArray();
        for (var y = 30; y <= 34; y++)
        for (var x = 30; x <= 34; x++)
        {
            if (x != 30 && x != 34 && y != 30 && y != 34) continue;
            var index = y * SimulationState.Width + x;
            terrain[index] = terrain[index] with { ElevationM = 5 };
        }

        var topology = SurfaceWaterModel.AnalyzeHydrology(terrain);
        var basinIndex = topology.DepressionId[32 * SimulationState.Width + 32];
        Assert.True(basinIndex >= 0);
        var basin = Assert.Single(topology.Basins);
        Assert.Equal(5, basin.SpillElevationM);
        Assert.Equal(9, basin.Cells.Length);
        Assert.Equal(4_500, basin.CapacityM3);
        Assert.Equal(1_800, SurfaceWaterModel.DepressionVolumeAtStageM3(terrain, basin, 2));
        Assert.Equal(900, SurfaceWaterModel.DepressionAreaAtStageM2(terrain, basin, 2));
        Assert.Equal(basin.CapacityM3, SurfaceWaterModel.DepressionVolumeAtStageM3(terrain, basin, 8));
        Assert.Equal(0, terrain[32 * SimulationState.Width + 32].ElevationM);
        Assert.Equal(5, topology.FilledElevationM[32 * SimulationState.Width + 32]);
    }

    [Fact]
    public void Priority_flood_receiver_tree_is_deterministic_and_routes_all_cells_to_the_open_edge()
    {
        var terrain = TestWorldFactory.ToSurfaceWaterCells(TestWorldFactory.Create(2).Snapshot().Cells);
        var first = SurfaceWaterModel.AnalyzeHydrology(terrain);
        var second = SurfaceWaterModel.AnalyzeHydrology(terrain);

        Assert.Equal(first.FilledElevationM, second.FilledElevationM);
        Assert.Equal(first.Receiver, second.Receiver);
        Assert.Equal(first.Accumulation, second.Accumulation);
        for (var index = 0; index < terrain.Length; index++)
        {
            var receiver = first.Receiver[index];
            if (receiver < 0) continue;
            Assert.True(first.FilledElevationM[receiver] <= first.FilledElevationM[index]);
        }

        Assert.Equal(terrain.Length, first.Accumulation
            .Where((_, index) => first.Receiver[index] < 0).Sum());
    }

    [Fact]
    public void Terrain_chunks_have_valid_triangles_and_exact_shared_edges()
    {
        const int subdivisions = 8;
        const int chunkCells = 16;
        var sampler = TerrainModel.CreateUnderlyingElevationSampler(1);
        var ridgeChunkX = (int)Math.Round(SimulationState.Width * 0.20);
        var west = TerrainSurfaceMeshBuilder.BuildChunk(sampler, ridgeChunkX, 0, chunkCells, chunkCells,
            subdivisions, SimulationState.Width, SimulationState.Height, 10);
        var east = TerrainSurfaceMeshBuilder.BuildChunk(sampler, ridgeChunkX + chunkCells, 0, chunkCells, chunkCells,
            subdivisions, SimulationState.Width, SimulationState.Height, 10);

        var verticesPerRow = chunkCells * subdivisions + 1;
        Assert.Equal(verticesPerRow * verticesPerRow, west.Vertices.Length);
        Assert.Equal(chunkCells * subdivisions * chunkCells * subdivisions * 6, west.Indices.Length);
        Assert.True(west.Vertices.Max(vertex => vertex.Position.Y) - west.Vertices.Min(vertex => vertex.Position.Y) > 20,
            "The rendered terrain chunk must contain geometric relief, not a flat surface with a shaded heightmap.");
        foreach (var mesh in new[] { west, east })
        {
            Assert.All(mesh.Indices, index => Assert.InRange(index, 0, mesh.Vertices.Length - 1));
            for (var index = 0; index < mesh.Indices.Length; index += 3)
            {
                var a = mesh.Vertices[mesh.Indices[index]].Position;
                var b = mesh.Vertices[mesh.Indices[index + 1]].Position;
                var c = mesh.Vertices[mesh.Indices[index + 2]].Position;
                Assert.True(Vector3.Cross(b - a, c - a).LengthSquared() > 0);
            }
        }

        for (var row = 0; row < verticesPerRow; row++)
        {
            var westEdge = west.Vertices[row * verticesPerRow + verticesPerRow - 1];
            var eastEdge = east.Vertices[row * verticesPerRow];
            Assert.Equal(westEdge.Position, eastEdge.Position);
            Assert.Equal(westEdge.Normal, eastEdge.Normal);
            Assert.Equal(westEdge.TextureCoordinate, eastEdge.TextureCoordinate);
        }
    }

    [Fact]
    public void Initial_vegetation_keeps_altitude_fractions_and_forms_spatial_patches()
    {
        var cells = TestWorldFactory.Create(1).Snapshot().Cells.ToArray();
        var elevations = cells.Select(cell => cell.ElevationM).ToArray();
        var grassFractions = new[] { 0.55, 0.38, 0.12 };
        var woodyFractions = new[] { 0.30, 0.62, 0.40 };
        var grassCandidates = new List<WorldCell>[] { [], [], [] };
        var woodyCandidates = new List<WorldCell>[] { [], [], [] };
        foreach (var cell in cells)
        {
            var zone = cell.ElevationM < 75 ? 0 : cell.ElevationM < 125 ? 1 : 2;
            var slope = TerrainModel.SlopeMagnitude(elevations, cell.X, cell.Y);
            if (cell.SurfaceWaterMm <= 150 && slope <= 1.0) grassCandidates[zone].Add(cell);
            if (cell.SurfaceWaterMm == 0 && slope <= 1.0) woodyCandidates[zone].Add(cell);
            if (cell.Grass.Present) Assert.True(slope <= 1.0, $"Grass crossed a bare-rock slope at ({cell.X}, {cell.Y}).");
            if (cell.Woody.Present) Assert.True(slope <= 1.0, $"Woody cover crossed a bare-rock slope at ({cell.X}, {cell.Y}).");
            if (cell.Grass.Present) Assert.True(cell.SurfaceWaterMm <= 150, $"Grass occupied deep surface water at ({cell.X}, {cell.Y}).");
            if (cell.Woody.Present) Assert.Equal(0, cell.SurfaceWaterMm);
        }

        for (var zone = 0; zone < 3; zone++)
        {
            Assert.Equal((int)Math.Round(grassCandidates[zone].Count * grassFractions[zone], MidpointRounding.AwayFromZero),
                grassCandidates[zone].Count(cell => cell.Grass.Present));
            Assert.Equal((int)Math.Round(woodyCandidates[zone].Count * woodyFractions[zone], MidpointRounding.AwayFromZero),
                woodyCandidates[zone].Count(cell => cell.Woody.Present));
        }
        Assert.NotEmpty(grassCandidates[2]);
        Assert.NotEmpty(woodyCandidates[2]);

        var expectedAdjacentGrassPairs = 0.0;
        var actualAdjacentGrassPairs = 0;
        for (var y = 0; y < SimulationState.Height; y++)
        for (var x = 0; x < SimulationState.Width; x++)
        {
            var index = y * SimulationState.Width + x;
            if (x + 1 < SimulationState.Width)
                CountPair(index, index + 1);
            if (y + 1 < SimulationState.Height)
                CountPair(index, index + SimulationState.Width);
        }
        Assert.True(actualAdjacentGrassPairs > expectedAdjacentGrassPairs * 1.15,
            $"Expected spatially clustered grass: observed {actualAdjacentGrassPairs} adjacent pairs, independent expectation {expectedAdjacentGrassPairs:F1}.");

        void CountPair(int firstIndex, int secondIndex)
        {
            var first = cells[firstIndex];
            var second = cells[secondIndex];
            var firstProbability = PlacementProbability(first);
            var secondProbability = PlacementProbability(second);
            expectedAdjacentGrassPairs += firstProbability * secondProbability;
            if (first.Grass.Present && second.Grass.Present) actualAdjacentGrassPairs++;
        }

        double PlacementProbability(WorldCell cell)
        {
            if (cell.SurfaceWaterMm > 150 || TerrainModel.SlopeMagnitude(elevations, cell.X, cell.Y) > 1.0) return 0;
            var zone = cell.ElevationM < 75 ? 0 : cell.ElevationM < 125 ? 1 : 2;
            return grassFractions[zone];
        }
    }

    [Fact]
    public void Initial_surface_water_follows_drainage_and_includes_lake_storage()
    {
        var cells = TestWorldFactory.Create(1).Snapshot().Cells;
        var repeated = TestWorldFactory.Create(1).Snapshot().Cells;
        var wetCells = cells.Where(cell => cell.SurfaceWaterMm > 0).ToArray();
        Assert.True(wetCells.Length >= 12);
        Assert.True(wetCells.Sum(cell => cell.SurfaceWaterMm) > 0);
        Assert.True(wetCells.Sum(cell => cell.GroundwaterMm) > 0);
        Assert.True(wetCells.Max(cell => cell.SurfaceWaterMm) > 220);
        Assert.Equal(cells.Select(cell => (cell.SurfaceWaterMm, cell.GroundwaterMm)),
            repeated.Select(cell => (cell.SurfaceWaterMm, cell.GroundwaterMm)));
    }

    [Fact]
    public void Tick_zero_is_a_real_initial_state_and_render_snapshot_is_read_only()
    {
        var world = TestWorldFactory.Create(1);
        var before = world.Snapshot();
        world.AdvanceTicks(5);
        var after = world.Snapshot();
        Assert.Equal(0, before.TickIndex);
        Assert.Equal(5, after.TickIndex);
        Assert.Equal(before.Cells[0], after.Cells[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<WorldCell>)before.Cells)[0] = after.Cells[0]);
    }

    [Fact]
    public void Solar_geometry_has_zero_night_and_positive_midday_radiation()
    {
        var night = SolarGeometry.At(1, 0);
        var noon = SolarGeometry.At(172, 12);
        var summerSolsticeAtEquator = SolarGeometry.At(172, 12, 0);
        var midnightSunAtNorthPole = SolarGeometry.At(172, 0, 90);
        var polarWinter = SolarGeometry.At(355, 12, 90);
        Assert.Equal(0, night.TopOfAtmosphereIrradianceWPerM2);
        Assert.True(noon.TopOfAtmosphereIrradianceWPerM2 > 0);
        Assert.Equal(Math.Cos(23.44 * Math.PI / 180), summerSolsticeAtEquator.UpDirection, 3);
        Assert.True(midnightSunAtNorthPole.TopOfAtmosphereIrradianceWPerM2 > 0);
        Assert.Equal(Math.Sin(23.44 * Math.PI / 180), midnightSunAtNorthPole.UpDirection, 3);
        Assert.Equal(0, polarWinter.TopOfAtmosphereIrradianceWPerM2);
        Assert.Equal(172, noon.DayOfYear);
        Assert.Equal(12, noon.SolarHour);
        Assert.Equal(45, noon.LatitudeDegrees);
        Assert.Equal(1, Math.Sqrt(noon.EastDirection * noon.EastDirection
            + noon.NorthDirection * noon.NorthDirection + noon.UpDirection * noon.UpDirection), 10);
        Assert.Equal(0.0, BotanyModel.TemperatureSuitability(-2, -2, 20, 38));
    }

    [Fact]
    public void Seasonal_and_daily_temperature_are_owned_by_atmosphere()
    {
        var seaLevel = TemperatureModel.SeasonalMeanTemperatureC(200, 0);
        var highland = TemperatureModel.SeasonalMeanTemperatureC(200, 1_000);

        Assert.Equal(23, seaLevel, 10);
        Assert.Equal(16.5, highland, 10);
        Assert.Equal(seaLevel + 5, TemperatureModel.AirTemperatureC(200, 15, 0), 10);
        Assert.Equal(seaLevel - 5, TemperatureModel.AirTemperatureC(200, 3, 0), 10);
        Assert.Throws<ArgumentOutOfRangeException>(() => TemperatureModel.AirTemperatureC(200, double.PositiveInfinity, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemperatureModel.SeasonalMeanTemperatureC(366, 0));
    }

    [Fact]
    public void Atmosphere_forecast_publishes_a_repeatable_read_only_hourly_tile_field()
    {
        var atmosphere = new AtmosphereModuleV1();
        var rainAge = Enumerable.Repeat(24, 16).ToArray();
        var forecast = atmosphere.ForecastDay(1, 20, 48, rainAge);
        var repeated = atmosphere.ForecastDay(1, 20, 48, rainAge);

        Assert.Equal(24 * 16, forecast.Count);
        Assert.Equal(forecast, repeated);
        Assert.All(forecast, hour =>
        {
            Assert.True(double.IsFinite(hour.PrecipitationMm));
            Assert.InRange(hour.PrecipitationMm, 0, 12);
            Assert.InRange(hour.CloudCover, 0, 0.85);
        });
        Assert.All(rainAge, age => Assert.Equal(24, age));
        Assert.Throws<NotSupportedException>(() => ((IList<ClimateHour>)forecast)[0] = default);
    }

    [Fact]
    public void Solar_geometry_rejects_inputs_outside_its_contract()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SolarGeometry.At(0, 12));
        Assert.Throws<ArgumentOutOfRangeException>(() => SolarGeometry.At(1, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => SolarGeometry.At(1, 12, 91));
    }

    [Fact]
    public void Hourly_air_temperature_is_daily_mean_plus_diurnal_offset()
    {
        var atmosphere = new AtmosphereModuleV1();
        for (var day = 1; day <= 365; day += 47)
        for (var hour = 0.5; hour < 24; hour += 5.5)
        {
            var dailyMean = atmosphere.SeasonalMeanTemperatureC(day, 137.25);
            var diurnalOffset = atmosphere.DiurnalTemperatureOffsetC(hour);
            Assert.Equal(atmosphere.AirTemperatureC(day, hour, 137.25), dailyMean + diurnalOffset);
        }
    }

    [Fact]
    public void Terrain_solar_factor_batch_matches_cell_scoped_calculation()
    {
        var geosphere = new GeosphereModuleV1();
        var elevations = geosphere.GenerateElevationField(7);
        var normals = geosphere.CreateTerrainSolarNormals(elevations);
        var factors = geosphere.TerrainSolarFactors(normals, 0.31, -0.42, 0.71);

        Assert.Equal(elevations.Length, normals.Length);
        Assert.Equal(elevations.Length, factors.Length);
        for (var index = 0; index < elevations.Length; index++)
            Assert.Equal(geosphere.TerrainSolarFactor(elevations, index % SimulationState.Width,
                index / SimulationState.Width, 0.31, -0.42, 0.71), factors[index], 14);
    }

    [Fact]
    public void Core_consumes_substitutable_domain_capabilities()
    {
        var solar = new CountingSolarForcing(new CosmosModuleV1());
        var atmosphere = new AtmosphereModuleV1();
        var geosphere = new CountingGeosphere(new GeosphereModuleV1());
        var hydrosphere = new CountingHydrosphere(new HydrosphereModuleV1());
        var biosphere = new CountingBiosphere(new BiosphereModuleV1());
        var world = WorldSimulation.Create(1, solar, atmosphere, atmosphere, geosphere, geosphere,
            geosphere, hydrosphere, biosphere, biosphere, biosphere);
        var callsAfterInitialization = solar.CallCount;

        Assert.True(geosphere.ElevationFieldCallCount > 0);
        Assert.Equal(1, geosphere.SolarNormalFieldCallCount);
        Assert.True(geosphere.SoilProfileCallCount > 0);
        Assert.True(hydrosphere.SeedCallCount > 0);
        Assert.Equal(1, hydrosphere.RoutingPlanCallCount);
        Assert.True(biosphere.InitialPatchCallCount > 0);
        world.AdvanceTicks((int)SimulationState.TicksPerHour);

        Assert.True(callsAfterInitialization > 0);
        Assert.True(solar.CallCount > callsAfterInitialization);
        Assert.Equal(24, geosphere.SolarFactorFieldCallCount);
        Assert.True(geosphere.SoilWaterCallCount > 0);
        Assert.True(hydrosphere.PrecipitationCallCount > 0);
        Assert.True(hydrosphere.RouteCallCount > 0);
        Assert.True(biosphere.GrowthCallCount > 0);
        Assert.Equal(0, biosphere.SeedDispersalCallCount);
        Assert.True(geosphere.PlantTranspirationCallCount > 0);
        Assert.True(geosphere.MineralNitrogenConfirmationCallCount > 0);

        world.AdvanceTicks((int)(SimulationState.TicksPerDay - SimulationState.TicksPerHour));
        Assert.True(geosphere.EcologyProposalConfirmationCallCount > 0);
    }

    [Fact]
    public void Saturated_soil_routes_rain_to_surface_storage()
    {
        var soilModule = new GeosphereModuleV1();
        var hydroModule = new HydrosphereModuleV1();
        var soil = new SoilHydraulicStateV1(100, 100, 20, 20);
        var infiltrationCapacity = soilModule.MaximumInfiltrationMm(soil);
        var surface = hydroModule.ReceivePrecipitation(new SurfaceWaterStateV1(0, 0, 0), 12,
            0, infiltrationCapacity, 0);
        var result = soilModule.ApplyInfiltrationAndEvaporate(soil, surface.InfiltrationMm,
            0, 0, surface.PlantWaterDemandMultiplier);

        Assert.Equal(100, result.SoilWaterMm);
        Assert.Equal(12, surface.SurfaceWaterMm);
        Assert.Equal(0, surface.InfiltrationMm);
        Assert.Equal(0, surface.InterceptionMm);
    }

    [Fact]
    public void Existing_puddle_infiltrates_without_new_rain()
    {
        var soilModule = new GeosphereModuleV1();
        var hydroModule = new HydrosphereModuleV1();
        var soil = new SoilHydraulicStateV1(70, 100, 20, 20);
        var water = new SurfaceWaterStateV1(0, 2, 0);
        var infiltrationCapacity = soilModule.MaximumInfiltrationMm(soil);

        var surface = hydroModule.ReceivePrecipitation(water, 0, 0, infiltrationCapacity, 0);
        var soilResult = soilModule.ApplyInfiltrationAndEvaporate(soil, surface.InfiltrationMm,
            0, 0, surface.PlantWaterDemandMultiplier);

        Assert.Equal(2, surface.InfiltrationMm);
        Assert.Equal(0, surface.SurfaceWaterMm);
        Assert.Equal(72, soilResult.SoilWaterMm);
        var storageChangeLiters = soilResult.SoilWaterMm * SimulationState.CellAreaM2
            + surface.SurfaceWaterMm * SimulationState.CellAreaM2
            - (soil.SoilWaterMm + water.SurfaceWaterMm) * SimulationState.CellAreaM2;
        Assert.InRange(Math.Abs(storageChangeLiters), 0, 1e-12);
    }

    [Fact]
    public void Geosphere_hydrosphere_hourly_exchange_closes_the_local_water_budget()
    {
        var soilModule = new GeosphereModuleV1();
        var hydroModule = new HydrosphereModuleV1();
        var soil = new SoilHydraulicStateV1(72, 100, 20, 18);
        var surface = new SurfaceWaterStateV1(7, 5, 17);
        const double precipitationMm = 12;
        const double plantCover = 0.4;
        const double potentialEtMm = 1.5;

        var infiltrationCapacity = soilModule.MaximumInfiltrationMm(soil);
        var surfaceResult = hydroModule.ReceivePrecipitation(surface, precipitationMm, plantCover,
            infiltrationCapacity, potentialEtMm);
        var soilResult = soilModule.ApplyInfiltrationAndEvaporate(soil, surfaceResult.InfiltrationMm,
            plantCover, potentialEtMm, surfaceResult.PlantWaterDemandMultiplier);

        var initialStorageMm = soil.SoilWaterMm + surface.SurfaceWaterMm;
        var finalStorageMm = soilResult.SoilWaterMm + surfaceResult.SurfaceWaterMm;
        var expectedFinalMm = initialStorageMm + precipitationMm - surfaceResult.InterceptionMm
            - surfaceResult.OpenWaterEvaporationMm - soilResult.SoilEvaporationMm;

        Assert.InRange(Math.Abs(finalStorageMm - expectedFinalMm), 0, 1e-12);
        Assert.InRange(surfaceResult.InfiltrationMm, 0, infiltrationCapacity);
        Assert.InRange(surfaceResult.SurfaceWaterMm, 0, double.MaxValue);
        Assert.InRange(soilResult.SoilWaterMm, soil.WiltMm, soil.CapacityMm);
    }

    [Fact]
    public void Surface_water_routing_conserves_internal_flow_and_accounts_boundary_loss()
    {
        var cells = TestWorldFactory.ToSurfaceWaterCells(TestWorldFactory.Create(1).Snapshot().Cells);
        var centerIndex = (SimulationState.Height / 2) * SimulationState.Width + SimulationState.Width / 2;
        cells[centerIndex] = cells[centerIndex] with { SurfaceWaterMm = 100 };
        var beforeLiters = cells.Sum(c => (c.SurfaceWaterMm + c.GroundwaterMm) * SimulationState.CellAreaM2);
        var routingPlan = SurfaceWaterModel.CreateRoutingPlan(cells);
        var cachedRoute = SurfaceWaterModel.Route(cells, routingPlan);
        var routed = SurfaceWaterModel.Route(cells);
        Assert.Equal(routed.OutflowLiters, cachedRoute.OutflowLiters, 10);
        Assert.Equal(routed.Cells, cachedRoute.Cells);
        var afterLiters = routed.Cells.Sum(c => (c.SurfaceWaterMm + c.GroundwaterMm) * SimulationState.CellAreaM2);
        var roundoffToleranceLiters = Math.Max(1e-3, beforeLiters * 1e-9);
        Assert.InRange(Math.Abs(beforeLiters - afterLiters - routed.OutflowLiters), 0, roundoffToleranceLiters);
        Assert.Contains(routed.Cells, c => c.SurfaceWaterMm > 0);
    }

    [Fact]
    public void Edge_routing_conserves_internal_transfer_and_reports_boundary_outflow()
    {
        var cells = Enumerable.Range(0, SimulationState.Width * SimulationState.Height)
            .Select(index => new SurfaceWaterCellV1(index, index % SimulationState.Width,
                index / SimulationState.Width, 0, 0, 0)).ToArray();
        var edgeIndex = (SimulationState.Height / 2) * SimulationState.Width;
        cells[edgeIndex] = cells[edgeIndex] with { SurfaceWaterMm = 100 };
        var beforeLiters = cells.Sum(cell => cell.SurfaceWaterMm * SimulationState.CellAreaM2);

        var routed = SurfaceWaterModel.Route(cells);
        var afterLiters = routed.Cells.Sum(cell => cell.SurfaceWaterMm * SimulationState.CellAreaM2);

        Assert.True(routed.OutflowLiters > 0);
        Assert.InRange(Math.Abs(beforeLiters - afterLiters - routed.OutflowLiters), 0, 1e-10);
        Assert.True(routed.Cells[edgeIndex].SurfaceWaterMm < 100);
        Assert.Contains(routed.Cells, cell => cell.SurfaceWaterMm > 0);
    }

    [Fact]
    public void Growth_responds_to_light_water_and_mineral_nitrogen()
    {
        var state = new BotanyCellStateV1(
            new PlantPatchPair(new PlantPatch(true, 100, 1.8, 0, 0, 0, 0), default), default);
        var sun = BotanyModel.GrowHour(Input(state, 4, 20, 8, 20));
        var night = BotanyModel.GrowHour(Input(state, 4, 20, 0, 20));
        var dry = BotanyModel.GrowHour(Input(state, 4, 20, 8, 0));
        var nitrogenFree = BotanyModel.GrowHour(Input(state, 0, 20, 8, 20));
        Assert.True(sun.NewBiomassGPerM2 > 0);
        Assert.Equal(0, night.NewBiomassGPerM2);
        Assert.Equal(0, dry.NewBiomassGPerM2);
        Assert.Equal(0, nitrogenFree.NewBiomassGPerM2);
        Assert.InRange(sun.MineralNitrogenAfterGrowthProposalGPerM2, 0, 4);
        Assert.Equal(0, nitrogenFree.MineralNitrogenAfterGrowthProposalGPerM2);

        var fullSun = BotanyModel.GrowHour(Input(state, 100, 20, 8, 20));
        var halfSun = BotanyModel.GrowHour(Input(state, 100, 20, 4, 20));
        Assert.True(fullSun.NewBiomassGPerM2 > 0);
        Assert.Equal(fullSun.NewBiomassGPerM2 * 0.5, halfSun.NewBiomassGPerM2, 12);
        Assert.Equal(fullSun, BotanyModel.GrowHour(Input(state, 100, 20, 8, 20),
            BotanyModel.CoverComponents(state.Plants)));

        static BotanyHourInputV1 Input(BotanyCellStateV1 state, double nitrogen, double temperature,
            double shortwave, double availableWater)
            => new(state, nitrogen, temperature, shortwave, 1, availableWater, 1, 1);
    }

    [Fact]
    public void Seed_dispersal_skips_empty_hours_and_conserves_produced_matter_and_nitrogen()
    {
        var states = new BotanyCellStateV1[SimulationState.Width * SimulationState.Height];
        var origin = SimulationState.Width * 10 + 10;
        var grass = new PlantPatch(true, 100, 1.8, 10, 0.18, 0, 0);
        states[origin] = new(new PlantPatchPair(grass, default), default);
        var noSeeds = new BotanyHourResultV1[states.Length];
        Assert.Same(states, BotanyModel.DisperseSeeds(states, noSeeds));

        var production = new BotanyHourResultV1[states.Length];
        production[origin] = new(states[origin], 0, 0, 0, 10, 0);
        var dispersed = BotanyModel.DisperseSeeds(states, production);
        var dryMatter = dispersed.Sum(state => state.Plants.Grass.SeedBankGPerM2);
        var nitrogen = dispersed.Sum(state => state.Plants.Grass.SeedBankNitrogenGPerM2);

        Assert.Equal(8, dispersed[origin].Plants.Grass.SeedBankGPerM2, 12);
        Assert.Equal(10, dryMatter, 12);
        Assert.Equal(0.18, nitrogen, 12);
        Assert.True(dispersed[origin + 1].Plants.Grass.Present);
    }

    [Fact]
    public void Drought_from_saturated_soil_limits_both_plants_without_crossing_wilt()
    {
        var geosphere = new GeosphereModuleV1();
        var hydrosphere = new HydrosphereModuleV1();
        var biosphere = new BiosphereModuleV1();
        var soil = new SoilHydraulicStateV1(100, 100, 20, 20);
        var surface = new SurfaceWaterStateV1(0, 0, 0);
        var botany = new BotanyCellStateV1(new PlantPatchPair(
            new PlantPatch(true, 100, 1.8, 0, 0, 0, 0),
            new PlantPatch(true, 250, 3.0, 0, 0, 0, 0)), default);
        var mineralNitrogen = 1_000.0;
        var totalTranspirationMm = 0.0;

        for (var hour = 0; hour < 48; hour++)
        {
            var soilState = soil;
            var cover = biosphere.TotalCover(botany.Plants);
            var infiltrationCapacity = geosphere.MaximumInfiltrationMm(soilState);
            var surfaceExchange = hydrosphere.ReceivePrecipitation(surface, 0, cover,
                infiltrationCapacity, 0);
            var soilWater = geosphere.ApplyInfiltrationAndEvaporate(soilState,
                surfaceExchange.InfiltrationMm, cover, 0, surfaceExchange.PlantWaterDemandMultiplier);
            var growth = biosphere.GrowHour(new BotanyHourInputV1(botany, mineralNitrogen,
                20, 8, 5, soilWater.PlantAvailableWaterMm, soilWater.WaterFactorAvailable,
                surfaceExchange.PlantWaterDemandMultiplier));
            var transpiration = geosphere.ApplyPlantTranspiration(new SoilHydraulicStateV1(
                soilWater.SoilWaterMm, soil.CapacityMm, soil.WiltMm, soil.KsatMmPerHour),
                growth.TranspirationMm);
            mineralNitrogen = geosphere.ConfirmMineralNitrogenAfterGrowth(mineralNitrogen,
                growth.MineralNitrogenAfterGrowthProposalGPerM2);
            soil = soil with { SoilWaterMm = transpiration.SoilWaterMm };
            botany = growth.State;
            totalTranspirationMm += transpiration.ConfirmedTranspirationMm;

            Assert.Equal(0, surfaceExchange.InfiltrationMm);
            Assert.InRange(transpiration.ConfirmedTranspirationMm, 0,
                soilWater.PlantAvailableWaterMm);
            Assert.InRange(soil.SoilWaterMm, soil.WiltMm, soil.CapacityMm);
        }

        Assert.True(totalTranspirationMm > 0);
        Assert.Equal(soil.WiltMm, soil.SoilWaterMm, 10);
        Assert.InRange(mineralNitrogen, 0, 1_000);
        Assert.Equal(0, biosphere.GrowHour(new BotanyHourInputV1(botany, mineralNitrogen,
            20, 8, 5, 0, 0, 1)).TranspirationMm);
    }

    [Fact]
    public void Daily_litter_decomposition_conserves_dry_matter_and_nitrogen()
    {
        var cell = TestWorldFactory.Create(1).Snapshot().Cells[31 * 64 + 31] with
        {
            DetritusDryGPerM2 = 120,
            DetritusNitrogenGPerM2 = 1.2,
            SoilWaterMm = 90,
            Grass = default,
            Woody = default
        };
        var matterBefore = cell.DetritusDryGPerM2 + cell.SoilOrganicMatterGPerM2;
        var nitrogenBefore = cell.DetritusNitrogenGPerM2 + cell.SoilOrganicNitrogenGPerM2 + cell.MineralNitrogenGPerM2;
        var result = EcologyModel.CloseDay(TestWorldFactory.ToEcologyInput(cell, 20));
        var matterAfter = result.DetritusDryGPerM2 + result.SoilProposal.SoilOrganicMatterGPerM2
            + result.DetritusRespirationGPerM2 + result.SoilOrganicMatterRespirationGPerM2;
        var nitrogenAfter = result.DetritusNitrogenGPerM2 + result.SoilProposal.SoilOrganicNitrogenGPerM2
            + result.SoilProposal.MineralNitrogenGPerM2;
        Assert.InRange(Math.Abs(matterBefore - matterAfter), 0, 1e-10);
        Assert.InRange(Math.Abs(nitrogenBefore - nitrogenAfter), 0, 1e-10);

        var dryResult = EcologyModel.CloseDay(TestWorldFactory.ToEcologyInput(
            cell with { SoilWaterMm = cell.SoilWiltMm }, 20));
        var coldResult = EcologyModel.CloseDay(TestWorldFactory.ToEcologyInput(cell, 0));
        Assert.True(result.DetritusDryGPerM2 < dryResult.DetritusDryGPerM2);
        Assert.True(result.DetritusRespirationGPerM2 > dryResult.DetritusRespirationGPerM2);
        Assert.True(result.DetritusRespirationGPerM2 > coldResult.DetritusRespirationGPerM2);
    }

    [Fact]
    public void Ecology_returns_more_plant_material_from_a_stressed_habitat()
    {
        var grass = new PlantPatch(true, 100, 1.8, 0, 0, 0, 0);
        var favorable = new BotanyCellStateV1(new PlantPatchPair(grass, default),
            new BotanyDailyFactorsV1(24, 24, 24, 0, 0, 0, 0, 0));
        var stressed = new BotanyCellStateV1(new PlantPatchPair(grass, default),
            new BotanyDailyFactorsV1(0, 0, 0, 24, 0, 0, 0, 0));
        var soilMoisture = new SoilMoistureContextV1(90, 180, 35);
        var soil = new SoilBiogeochemistryStateV1(1500, 22.5, 4);

        var favorableResult = EcologyModel.CloseDay(new EcologyDailyInputV1(
            favorable, 0, 0, soilMoisture, soil, 20));
        var stressedResult = EcologyModel.CloseDay(new EcologyDailyInputV1(
            stressed, 0, 0, soilMoisture, soil, 20));

        Assert.True(stressedResult.Botany.Plants.Grass.BiomassGPerM2
            < favorableResult.Botany.Plants.Grass.BiomassGPerM2);
        Assert.True(stressedResult.DetritusDryGPerM2 > favorableResult.DetritusDryGPerM2);
        Assert.Equal(default, stressedResult.Botany.DailyFactors);
    }

    [Fact]
    public void Save_schema_two_migrates_missing_genomes_deterministically()
    {
        var original = TestWorldFactory.Create(42, new InitialAnimalPopulationV1(4, 2));
        var legacy = JsonNode.Parse(original.SaveToJson())!.AsObject();
        legacy["SchemaVersion"] = 2;
        foreach (var animal in legacy["Animals"]!.AsArray()) animal!.AsObject().Remove("Genome");
        var legacyJson = legacy.ToJsonString();

        var migrated = TestWorldFactory.Load(legacyJson);
        var repeated = TestWorldFactory.Load(legacyJson);
        var migratedAnimals = migrated.Snapshot().Animals;
        Assert.Equal(6, migratedAnimals.Count);
        Assert.All(migratedAnimals, animal => Assert.True(animal.Genome.HasValue));
        Assert.Equal(migrated.Snapshot().Animals.Select(animal => animal.Genome!.Value.GenomeId),
            repeated.Snapshot().Animals.Select(animal => animal.Genome!.Value.GenomeId));
        Assert.Equal(3, JsonDocument.Parse(migrated.SaveToJson()).RootElement
            .GetProperty("SchemaVersion").GetInt32());
    }

    [Fact]
    public void Save_load_preserves_ecological_stocks_and_scheduler_continuation()
    {
        var uninterrupted = TestWorldFactory.Create(1);
        uninterrupted.AdvanceTicks((int)SimulationState.TicksPerDay - 1);
        var resumedAtDayBoundary = TestWorldFactory.Load(uninterrupted.SaveToJson());
        uninterrupted.AdvanceTicks(1);
        resumedAtDayBoundary.AdvanceTicks(1);
        Assert.Equal(2, resumedAtDayBoundary.Snapshot().DayOfYear);
        Assert.Equal(uninterrupted.CanonicalHash(), resumedAtDayBoundary.CanonicalHash());

        uninterrupted.AdvanceTicks(37);
        resumedAtDayBoundary.AdvanceTicks(37);
        var beforeSave = uninterrupted.Snapshot();
        Assert.Contains(beforeSave.Cells, cell => cell.Grass.SeedBankGPerM2 > 0
            || cell.Woody.SeedBankGPerM2 > 0);
        Assert.Contains(beforeSave.Cells, cell => cell.DetritusDryGPerM2 > 0);
        var resumedWithEcology = TestWorldFactory.Load(resumedAtDayBoundary.SaveToJson());
        uninterrupted.AdvanceTicks(205);
        resumedWithEcology.AdvanceTicks(205);
        Assert.Equal(uninterrupted.CanonicalHash(), resumedWithEcology.CanonicalHash());
    }

    [Fact]
    public void One_day_water_dry_matter_and_nitrogen_ledgers_close()
    {
        var world = TestWorldFactory.Create(1);
        var initial = world.Inventory();
        var initialGroundwater = world.Snapshot().Cells.Sum(cell => cell.GroundwaterMm);
        world.AdvanceTicks((int)SimulationState.TicksPerDay);
        var final = world.Inventory();
        var finalSnapshot = world.Snapshot();
        var ledger = finalSnapshot.Ledger;
        var waterResidual = final.WaterLiters - initial.WaterLiters
            - ledger.PrecipitationLiters + ledger.InterceptionLiters + ledger.OpenWaterEvaporationLiters
            + ledger.SoilEvaporationLiters + ledger.PlantTranspirationLiters + ledger.WaterOutflowLiters;
        var dryResidual = final.DryMatterGrams - initial.DryMatterGrams
            - ledger.PhotosynthesisDryGrams + ledger.DetritusRespirationDryGrams
            + ledger.SoilOrganicMatterRespirationDryGrams;
        var nitrogenResidual = final.NitrogenGrams - initial.NitrogenGrams;
        Assert.InRange(Math.Abs(waterResidual), 0, Math.Max(1e-6, ledger.PrecipitationLiters * 1e-9));
        Assert.InRange(Math.Abs(dryResidual), 0, Math.Max(1e-6, ledger.PhotosynthesisDryGrams * 1e-9));
        Assert.InRange(Math.Abs(nitrogenResidual), 0, Math.Max(1e-8, initial.NitrogenGrams * 1e-9));
        Assert.True(finalSnapshot.Cells.Sum(cell => cell.GroundwaterMm) < initialGroundwater);
        Assert.True(finalSnapshot.Cells.Count(cell => cell.SurfaceWaterMm > 0) >= 100);
        Assert.Contains(finalSnapshot.Cells, c => c.Grass.SeedBankGPerM2 > 0 || c.Woody.SeedBankGPerM2 > 0);
    }

    [Fact]
    public void Day_calendar_advances_on_the_144th_tick()
    {
        var world = TestWorldFactory.Create(1);
        world.AdvanceTicks(143);
        Assert.Equal(1, world.Snapshot().DayOfYear);
        world.AdvanceTicks(1);
        Assert.Equal(2, world.Snapshot().DayOfYear);
    }

    private sealed class CountingSolarForcing(ISolarForcingV1 inner) : ISolarForcingV1
    {
        public int CallCount { get; private set; }

        public SolarForcingSampleV1 At(int dayOfYear, double solarHour, double latitudeDegrees = 45.0)
        {
            CallCount++;
            return inner.At(dayOfYear, solarHour, latitudeDegrees);
        }
    }

    private sealed class CountingGeosphere(GeosphereModuleV1 inner)
        : IGeosphereTerrainV1, ISoilWaterV1, ISoilBiogeochemistryV1
    {
        public int ElevationFieldCallCount { get; private set; }
        public int SoilProfileCallCount { get; private set; }
        public int SoilWaterCallCount { get; private set; }
        public int PlantTranspirationCallCount { get; private set; }
        public int MineralNitrogenConfirmationCallCount { get; private set; }
        public int EcologyProposalConfirmationCallCount { get; private set; }
        public int SolarNormalFieldCallCount { get; private set; }
        public int SolarFactorFieldCallCount { get; private set; }

        public double[] GenerateElevationField(ulong seed)
        {
            ElevationFieldCallCount++;
            return inner.GenerateElevationField(seed);
        }

        public double SlopeMagnitude(double[] elevations, int x, int y) => inner.SlopeMagnitude(elevations, x, y);

        public double TerrainSolarFactor(double[] elevationsM, int x, int y, double sunEast, double sunNorth, double sunUp)
            => inner.TerrainSolarFactor(elevationsM, x, y, sunEast, sunNorth, sunUp);

        public TerrainSolarNormalV1[] CreateTerrainSolarNormals(double[] elevationsM)
        {
            SolarNormalFieldCallCount++;
            return inner.CreateTerrainSolarNormals(elevationsM);
        }

        public double[] TerrainSolarFactors(TerrainSolarNormalV1[] normals,
            double sunEast, double sunNorth, double sunUp)
        {
            SolarFactorFieldCallCount++;
            return inner.TerrainSolarFactors(normals, sunEast, sunNorth, sunUp);
        }

        public SoilProfileV1 Profile(SoilKind kind)
        {
            SoilProfileCallCount++;
            return inner.Profile(kind);
        }

        public double MaximumInfiltrationMm(SoilHydraulicStateV1 soil)
        {
            SoilWaterCallCount++;
            return inner.MaximumInfiltrationMm(soil);
        }

        public SoilWaterResultV1 ApplyInfiltrationAndEvaporate(
            SoilHydraulicStateV1 soil,
            double infiltrationMm,
            double plantCover,
            double potentialEtMm,
            double plantWaterDemandMultiplier)
        {
            SoilWaterCallCount++;
            return inner.ApplyInfiltrationAndEvaporate(soil, infiltrationMm, plantCover,
                potentialEtMm, plantWaterDemandMultiplier);
        }

        public SoilTranspirationResultV1 ApplyPlantTranspiration(
            SoilHydraulicStateV1 soil, double proposedTranspirationMm)
        {
            PlantTranspirationCallCount++;
            return inner.ApplyPlantTranspiration(soil, proposedTranspirationMm);
        }

        public double ConfirmMineralNitrogenAfterGrowth(
            double availableMineralNitrogenGPerM2, double proposedRemainingMineralNitrogenGPerM2)
        {
            MineralNitrogenConfirmationCallCount++;
            return inner.ConfirmMineralNitrogenAfterGrowth(availableMineralNitrogenGPerM2,
                proposedRemainingMineralNitrogenGPerM2);
        }

        public SoilBiogeochemistryStateV1 ConfirmEcologyProposal(
            SoilBiogeochemistryStateV1 current, SoilBiogeochemistryProposalV1 proposal)
        {
            EcologyProposalConfirmationCallCount++;
            return inner.ConfirmEcologyProposal(current, proposal);
        }

        public double DepositMineralNitrogen(double currentMineralNitrogenGPerM2, double proposedDepositGPerM2)
            => inner.DepositMineralNitrogen(currentMineralNitrogenGPerM2, proposedDepositGPerM2);
    }

    private sealed class CountingHydrosphere(HydrosphereModuleV1 inner) : ISurfaceWaterV1
    {
        public int SeedCallCount { get; private set; }
        public int PrecipitationCallCount { get; private set; }
        public int RouteCallCount { get; private set; }
        public int RoutingPlanCallCount { get; private set; }

        public SurfaceWaterStateV1[] SeedInitialWater(SurfaceWaterCellV1[] terrain)
        {
            SeedCallCount++;
            return inner.SeedInitialWater(terrain);
        }

        public SurfaceWaterRoutingPlanV1 CreateRoutingPlan(SurfaceWaterCellV1[] terrain)
        {
            RoutingPlanCallCount++;
            return inner.CreateRoutingPlan(terrain);
        }

        public SurfaceWaterExchangeResultV1 ReceivePrecipitation(
            SurfaceWaterStateV1 water,
            double precipitationMm,
            double plantCover,
            double soilInfiltrationCapacityMm,
            double potentialEtMm)
        {
            PrecipitationCallCount++;
            return inner.ReceivePrecipitation(water, precipitationMm, plantCover,
                soilInfiltrationCapacityMm, potentialEtMm);
        }

        public SurfaceFlowResultV1 Route(SurfaceWaterCellV1[] source)
        {
            RouteCallCount++;
            return inner.Route(source);
        }

        public SurfaceFlowResultV1 Route(SurfaceWaterCellV1[] source, SurfaceWaterRoutingPlanV1 routingPlan)
        {
            RouteCallCount++;
            return inner.Route(source, routingPlan);
        }

        public SurfaceWaterWithdrawalResultV1 AllocateAnimalWater(
            SurfaceWaterCellV1[] source,
            SurfaceWaterDemandV1[] demands)
            => inner.AllocateAnimalWater(source, demands);

        public SurfaceWaterStateV1[] DepositAnimalWater(
            SurfaceWaterCellV1[] source,
            SurfaceWaterDepositV1[] deposits)
            => inner.DepositAnimalWater(source, deposits);
    }

    private sealed class CountingBiosphere(BiosphereModuleV1 inner) : IBotanyV1, IEcologyV1, IZoologyV1,
        IZoologyGeneticsV1
    {
        public int InitialPatchCallCount { get; private set; }
        public int GrowthCallCount { get; private set; }
        public int SeedDispersalCallCount { get; private set; }

        public double TotalCover(PlantPatchPair plants) => inner.TotalCover(plants);

        public PlantCoverV1 CoverComponents(PlantPatchPair plants) => inner.CoverComponents(plants);

        public BotanyCellStateV1[] DisperseSeeds(
            BotanyCellStateV1[] states, BotanyHourResultV1[] hourlyResults)
        {
            SeedDispersalCallCount++;
            return inner.DisperseSeeds(states, hourlyResults);
        }

        public BotanyHourResultV1 GrowHour(BotanyHourInputV1 input)
        {
            GrowthCallCount++;
            return inner.GrowHour(input);
        }

        public BotanyHourResultV1 GrowHour(BotanyHourInputV1 input, PlantCoverV1 cover)
        {
            GrowthCallCount++;
            return inner.GrowHour(input, cover);
        }

        public HerbivoryResolutionV1 ResolveHerbivory(PlantPatch patch, PlantKind kind, HerbivoryRequestV1[] requests)
            => inner.ResolveHerbivory(patch, kind, requests);

        public PlantPatchPair[] CreateInitialPatches(
            ulong seed, double[] elevationsM, double[] surfaceWaterMm, double[] terrainSlopeMagnitude)
        {
            InitialPatchCallCount++;
            return inner.CreateInitialPatches(seed, elevationsM, surfaceWaterMm, terrainSlopeMagnitude);
        }

        public EcologyDailyResultV1 CloseDay(EcologyDailyInputV1 input) => inner.CloseDay(input);

        public EcologyDetritusStateV1 ConfirmDetritusDeposit(
            EcologyDetritusStateV1 current,
            EcologyDetritusStateV1 proposedDeposit)
            => inner.ConfirmDetritusDeposit(current, proposedDeposit);

        public AnimalAgentV1[] CreateInitialPopulation(ulong seed, ZoologyCellStateV1[] cells, InitialAnimalPopulationV1 population)
            => inner.CreateInitialPopulation(seed, cells, population);

        public AnimalAgentV1[] EnsureGenomes(ulong seed, AnimalAgentV1[] animals)
            => inner.EnsureGenomes(seed, animals);

        public ZoologyTickResultV1 AdvanceTick(ZoologyTickInputV1 input) => inner.AdvanceTick(input);

        public ZoologyWaterPreparationV1 PrepareDailyWater(AnimalAgentV1[] animals)
            => inner.PrepareDailyWater(animals);

        public ZoologyDailyResultV1 CloseDay(ZoologyDailyInputV1 input) => inner.CloseDay(input);
    }
}
