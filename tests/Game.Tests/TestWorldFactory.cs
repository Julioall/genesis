using Game.Core;
using Game.Contracts;
using Game.Modules.Atmosphere;
using Game.Modules.Biosphere;
using Game.Modules.Cosmos;
using Game.Modules.Geosphere;
using Game.Modules.Hydrosphere;

namespace Game.Tests;

internal static class TestWorldFactory
{
    public static SurfaceWaterCellV1[] ToSurfaceWaterCells(IEnumerable<WorldCell> cells)
        => cells.Select(cell => new SurfaceWaterCellV1(
            cell.Index, cell.X, cell.Y, cell.ElevationM, cell.SurfaceWaterMm, cell.GroundwaterMm)).ToArray();

    public static BotanyCellStateV1 ToBotanyState(WorldCell cell)
        => new(new PlantPatchPair(cell.Grass, cell.Woody), new BotanyDailyFactorsV1(
            cell.GrassWaterFactorSum, cell.GrassNitrogenFactorSum, cell.GrassTemperatureFactorSum,
            cell.GrassOutOfRangeHours, cell.WoodyWaterFactorSum, cell.WoodyNitrogenFactorSum,
            cell.WoodyTemperatureFactorSum, cell.WoodyOutOfRangeHours));

    public static EcologyDailyInputV1 ToEcologyInput(WorldCell cell, double meanAirTemperatureC)
        => new(ToBotanyState(cell), cell.DetritusDryGPerM2, cell.DetritusNitrogenGPerM2,
            new SoilMoistureContextV1(cell.SoilWaterMm, cell.SoilCapacityMm, cell.SoilWiltMm),
            new SoilBiogeochemistryStateV1(cell.SoilOrganicMatterGPerM2,
                cell.SoilOrganicNitrogenGPerM2, cell.MineralNitrogenGPerM2), meanAirTemperatureC);

    public static WorldSimulation Create(ulong seed = 1, InitialAnimalPopulationV1 initialPopulation = default)
    {
        var cosmos = new CosmosModuleV1();
        var atmosphere = new AtmosphereModuleV1();
        var geosphere = new GeosphereModuleV1();
        var hydrosphere = new HydrosphereModuleV1();
        var biosphere = new BiosphereModuleV1();
        return WorldSimulation.Create(seed, cosmos, atmosphere, atmosphere, geosphere, geosphere, geosphere,
            hydrosphere, biosphere, biosphere, biosphere, initialPopulation);
    }

    public static WorldSimulation Load(string json)
    {
        var cosmos = new CosmosModuleV1();
        var atmosphere = new AtmosphereModuleV1();
        var geosphere = new GeosphereModuleV1();
        var hydrosphere = new HydrosphereModuleV1();
        var biosphere = new BiosphereModuleV1();
        return WorldSimulation.Load(json, cosmos, atmosphere, atmosphere, geosphere, geosphere, geosphere,
            hydrosphere, biosphere, biosphere, biosphere);
    }
}
