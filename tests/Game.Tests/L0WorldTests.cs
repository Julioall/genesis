using Game.Contracts;
using Game.Core;
using Game.Modules.Biosphere;
using Game.Modules.Cosmos;
using Game.Modules.Geosphere;
using Game.Modules.Hydrosphere;
using Game.Procedural;

namespace Game.Tests;

public sealed class L0WorldTests
{
    [Fact]
    public void Counter_random_matches_the_documented_L0_vector()
    {
        Assert.Equal(0x7BD5A964748A48D8UL, CounterRandom.Bits(1, 3, 0, 0, 0, 0));
        Assert.Equal(0.48372896864433657, CounterRandom.Unit(1, 3, 0, 0, 0, 0), 15);
        Assert.Equal(0xE44005EC9AA4A02CUL, CounterRandom.Bits(1, 3, 0, 0, 0, 1));
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
        cells[32 * 64 + 32] = cells[32 * 64 + 32] with { SurfaceWaterMm = 100 };
        var beforeLiters = cells.Sum(c => c.SurfaceWaterMm * SimulationState.CellAreaM2);
        var routed = SurfaceWaterModel.Route(cells);
        var afterLiters = routed.Cells.Sum(c => c.SurfaceWaterMm * SimulationState.CellAreaM2);
        Assert.InRange(Math.Abs(beforeLiters - afterLiters - routed.OutflowLiters), 0, 1e-8);
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
        world.AdvanceTicks((int)SimulationState.TicksPerDay);
        var final = world.Inventory();
        var ledger = world.Snapshot().Ledger;
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
        Assert.Contains(world.Snapshot().Cells, c => c.Grass.SeedBankGPerM2 > 0 || c.Woody.SeedBankGPerM2 > 0);
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
