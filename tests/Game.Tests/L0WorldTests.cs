using Game.Contracts;
using Game.Client.Desktop;
using Game.Core;
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
        var first = WorldSimulation.Create(1);
        var second = WorldSimulation.Create(1);
        first.AdvanceTicks(60);
        second.AdvanceTicks(20);
        second.AdvanceTicks(40);
        Assert.Equal(first.CanonicalHash(), second.CanonicalHash());
        Assert.NotEqual(first.CanonicalHash(), WorldSimulation.Create(2).CanonicalHash());
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
        Assert.True(maximumRimRadius - minimumRimRadius > 0.8, "The outcrop rim should have irregular, non-elliptical relief.");
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
    public void Snapshot_altitudes_and_D8_receivers_follow_the_physical_terrain_field()
    {
        var world = WorldSimulation.Create(1).Snapshot();
        var elevations = TerrainModel.GenerateElevationField(world.Seed);
        var cells = world.Cells.ToArray();
        for (var index = 0; index < cells.Length; index++)
            Assert.Equal(elevations[index], cells[index].ElevationM, 8);

        var drainage = SurfaceWaterModel.AnalyzeDrainage(cells);
        var drawableCascadeEdges = 0;
        for (var from = 0; from < drainage.Receiver.Length; from++)
        {
            var to = drainage.Receiver[from];
            if (to < 0) continue;
            Assert.True(cells[to].ElevationM < cells[from].ElevationM,
                $"D8 receiver {to} must be lower than source cell {from}.");
            if (drainage.Accumulation[from] >= SurfaceWaterModel.ChannelThresholdCells
                && (cells[from].SurfaceWaterMm >= 20 || cells[to].SurfaceWaterMm >= 20)
                && !(cells[from].SurfaceWaterMm >= 250 && cells[to].SurfaceWaterMm >= 250)
                && cells[from].ElevationM - cells[to].ElevationM >= 6)
                drawableCascadeEdges++;
        }
        Assert.True(drawableCascadeEdges > 0, "The canonical seed should route water over at least one real elevation break.");
    }

    [Fact]
    public void Priority_flood_preserves_the_bed_and_measures_level_pool_volume_and_area()
    {
        var terrain = WorldSimulation.Create(1).Snapshot().Cells
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
        var terrain = WorldSimulation.Create(2).Snapshot().Cells.ToArray();
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
        var cells = WorldSimulation.Create(1).Snapshot().Cells.ToArray();
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
        var cells = WorldSimulation.Create(1).Snapshot().Cells;
        var repeated = WorldSimulation.Create(1).Snapshot().Cells;
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
        var world = WorldSimulation.Create(1);
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
        Assert.Equal(0, SolarGeometry.At(1, 0).TopOfAtmosphereWPerM2);
        Assert.True(SolarGeometry.At(172, 12).TopOfAtmosphereWPerM2 > 0);
        Assert.Equal(0.0, BotanyModel.TemperatureSuitability(-2, -2, 20, 38));
    }

    [Fact]
    public void Saturated_soil_routes_rain_to_surface_storage()
    {
        var cell = WorldSimulation.Create(1).Snapshot().Cells[0] with
        {
            SoilWaterMm = 100,
            SoilCapacityMm = 100,
            SoilWiltMm = 20,
            SoilKsatMmPerHour = 20,
            SurfaceWaterMm = 0,
            Grass = default,
            Woody = default
        };
        var result = SoilModel.ReceiveRainAndEvaporate(cell, 12, 0, 0);
        Assert.Equal(100, result.SoilWaterMm);
        Assert.Equal(12, result.SurfaceWaterMm);
        Assert.Equal(0, result.InterceptionMm);
    }

    [Fact]
    public void Surface_water_routing_conserves_internal_flow_and_accounts_boundary_loss()
    {
        var cells = WorldSimulation.Create(1).Snapshot().Cells.ToArray();
        var centerIndex = (SimulationState.Height / 2) * SimulationState.Width + SimulationState.Width / 2;
        cells[centerIndex] = cells[centerIndex] with { SurfaceWaterMm = 100 };
        var beforeLiters = cells.Sum(c => (c.SurfaceWaterMm + c.GroundwaterMm) * SimulationState.CellAreaM2);
        var routed = SurfaceWaterModel.Route(cells);
        var afterLiters = routed.Cells.Sum(c => (c.SurfaceWaterMm + c.GroundwaterMm) * SimulationState.CellAreaM2);
        var roundoffToleranceLiters = Math.Max(1e-3, beforeLiters * 1e-9);
        Assert.InRange(Math.Abs(beforeLiters - afterLiters - routed.OutflowLiters), 0, roundoffToleranceLiters);
        Assert.Contains(routed.Cells, c => c.SurfaceWaterMm > 0);
    }

    [Fact]
    public void Growth_responds_to_light_water_and_mineral_nitrogen()
    {
        var source = WorldSimulation.Create(1).Snapshot().Cells[31 * 64 + 31] with
        {
            Grass = new PlantPatch(true, 100, 1.8, 0, 0, 0, 0),
            Woody = default,
            MineralNitrogenGPerM2 = 4
        };
        var sun = BotanyModel.GrowHour(source, 20, 8, 1, 20, 1, 1);
        var night = BotanyModel.GrowHour(source, 20, 0, 1, 20, 1, 1);
        var dry = BotanyModel.GrowHour(source, 20, 8, 1, 0, 0, 1);
        var nitrogenFree = BotanyModel.GrowHour(source with { MineralNitrogenGPerM2 = 0 }, 20, 8, 1, 20, 1, 1);
        Assert.True(sun.NewBiomassGPerM2 > 0);
        Assert.Equal(0, night.NewBiomassGPerM2);
        Assert.Equal(0, dry.NewBiomassGPerM2);
        Assert.Equal(0, nitrogenFree.NewBiomassGPerM2);
    }

    [Fact]
    public void Daily_litter_decomposition_conserves_dry_matter_and_nitrogen()
    {
        var cell = WorldSimulation.Create(1).Snapshot().Cells[31 * 64 + 31] with
        {
            DetritusDryGPerM2 = 120,
            DetritusNitrogenGPerM2 = 1.2,
            SoilWaterMm = 90,
            Grass = default,
            Woody = default
        };
        var matterBefore = cell.DetritusDryGPerM2 + cell.SoilOrganicMatterGPerM2;
        var nitrogenBefore = cell.DetritusNitrogenGPerM2 + cell.SoilOrganicNitrogenGPerM2 + cell.MineralNitrogenGPerM2;
        var result = EcologyModel.CloseDay(cell, 20);
        var matterAfter = result.Cell.DetritusDryGPerM2 + result.Cell.SoilOrganicMatterGPerM2
            + result.DetritusRespirationGPerM2 + result.SoilOrganicMatterRespirationGPerM2;
        var nitrogenAfter = result.Cell.DetritusNitrogenGPerM2 + result.Cell.SoilOrganicNitrogenGPerM2 + result.Cell.MineralNitrogenGPerM2;
        Assert.InRange(Math.Abs(matterBefore - matterAfter), 0, 1e-10);
        Assert.InRange(Math.Abs(nitrogenBefore - nitrogenAfter), 0, 1e-10);
    }

    [Fact]
    public void Save_load_preserves_midday_weather_scheduler_and_continuation()
    {
        var uninterrupted = WorldSimulation.Create(1);
        uninterrupted.AdvanceTicks(37);
        var save = uninterrupted.SaveToJson();
        var resumed = WorldSimulation.Load(save);
        uninterrupted.AdvanceTicks(205);
        resumed.AdvanceTicks(205);
        Assert.Equal(uninterrupted.CanonicalHash(), resumed.CanonicalHash());
    }

    [Fact]
    public void One_day_water_dry_matter_and_nitrogen_ledgers_close()
    {
        var world = WorldSimulation.Create(1);
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
        var world = WorldSimulation.Create(1);
        world.AdvanceTicks(143);
        Assert.Equal(1, world.Snapshot().DayOfYear);
        world.AdvanceTicks(1);
        Assert.Equal(2, world.Snapshot().DayOfYear);
    }
}
