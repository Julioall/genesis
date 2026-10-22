using Game.Client.Desktop;
using Game.Contracts;
using Game.Core;
using Game.Modules.Hydrosphere;

namespace Game.Tests;

public sealed class ProceduralWaterPoolSurfaceTests
{
    [Fact]
    public void Rendered_depression_pool_uses_one_stage_for_uneven_snapshot_depths()
    {
        var terrain = CreateSingleBasin();
        var topology = SurfaceWaterModel.AnalyzeHydrology(terrain);
        var basin = Assert.Single(topology.Basins);
        terrain[32 * SimulationState.Width + 32] = terrain[32 * SimulationState.Width + 32]
            with { SurfaceWaterMm = 3_000 };
        terrain[32 * SimulationState.Width + 31] = terrain[32 * SimulationState.Width + 31]
            with { SurfaceWaterMm = 1_000 };

        var surface = ProceduralWaterPoolSurfaceBuilder.Build(
            terrain, topology.Basins, topology.DepressionId);

        var wetCells = new[] { 32 * SimulationState.Width + 32, 32 * SimulationState.Width + 31 };
        Assert.Equal(2, surface.PoolStageMByCell[wetCells[0]], 8);
        Assert.Equal(2, surface.PoolStageMByCell[wetCells[1]], 8);
        Assert.Equal(400, wetCells.Sum(index =>
            Math.Max(0, surface.PoolStageMByCell[index] - terrain[index].ElevationM)
                * SimulationState.CellAreaM2), 6);
        Assert.All(wetCells, index =>
        {
            Assert.Equal(2, surface.SurfaceStageMByCell[index], 8);
            Assert.Equal(1, surface.CoverageByCell[index], 8);
        });
        Assert.All(basin.Cells.Except(wetCells), index => Assert.Equal(0, surface.CoverageByCell[index]));

        Assert.True(surface.TryGetNearestPoolStage(30.5, 32.5, out var shorelineStageM));
        Assert.Equal(surface.PoolStageMByCell[wetCells[1]], shorelineStageM, 8);
    }

    [Fact]
    public void Rendered_depression_pool_is_capped_at_the_displayed_spill_stage()
    {
        var terrain = CreateSingleBasin();
        var topology = SurfaceWaterModel.AnalyzeHydrology(terrain);
        var basin = Assert.Single(topology.Basins);
        var centerIndex = 32 * SimulationState.Width + 32;
        terrain[centerIndex] = terrain[centerIndex] with { SurfaceWaterMm = 50_000 };

        var surface = ProceduralWaterPoolSurfaceBuilder.Build(
            terrain, topology.Basins, topology.DepressionId);

        Assert.Equal(basin.SpillElevationM, surface.PoolStageMByCell[centerIndex], 8);
        Assert.Equal(1, surface.CoverageByCell[centerIndex], 8);
        Assert.All(basin.Cells.Where(index => index != centerIndex), index => Assert.Equal(0, surface.CoverageByCell[index]));
    }

    [Fact]
    public void Uneven_snapshot_patch_keeps_its_stored_surface_when_levelization_would_remove_it()
    {
        var terrain = CreateSingleBasin();
        var topology = SurfaceWaterModel.AnalyzeHydrology(terrain);
        var firstIndex = 32 * SimulationState.Width + 32;
        var secondIndex = 32 * SimulationState.Width + 31;
        terrain[firstIndex] = terrain[firstIndex] with { SurfaceWaterMm = 200 };
        terrain[secondIndex] = terrain[secondIndex] with { SurfaceWaterMm = 200 };
        var displayBedM = terrain.Select(cell => cell.ElevationM).ToArray();
        displayBedM[secondIndex] = 4.5;

        var surface = ProceduralWaterPoolSurfaceBuilder.Build(
            terrain, topology.Basins, topology.DepressionId,
            displayBedMByCell: displayBedM);

        Assert.False(surface.TryGetNearestPoolStage(32.5, 32.5, out _));
        Assert.Equal(0.2, surface.SurfaceStageMByCell[firstIndex], 8);
        Assert.Equal(4.7, surface.SurfaceStageMByCell[secondIndex], 8);
        Assert.All(new[] { firstIndex, secondIndex }, index =>
            Assert.True(surface.CoverageByCell[index] > 0));
    }

    private static SurfaceWaterCellV1[] CreateSingleBasin()
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

        return terrain;
    }
}
