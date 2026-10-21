using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Game.Contracts;
using Game.Modules.Atmosphere;
using Game.Modules.Biosphere;
using Game.Modules.Cosmos;
using Game.Modules.Geosphere;
using Game.Modules.Hydrosphere;
using Game.Procedural;

namespace Game.Core;

public readonly record struct WorldInventory(double WaterLiters, double DryMatterGrams, double NitrogenGrams);

public sealed class WorldSimulation
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    private SimulationState _state;
    private double[]? _dailyRadiation;
    private int _dailyRadiationDay = -1;

    private WorldSimulation(SimulationState state) => _state = state;

    public static WorldSimulation Create(ulong seed = 1) => new(CreateInitialState(seed));

    public static WorldSimulation Load(string json)
    {
        var state = JsonSerializer.Deserialize<SimulationState>(json, JsonOptions)
            ?? throw new InvalidDataException("Save contains no world state.");
        Validate(state);
        return new(state);
    }

    public WorldSnapshot Snapshot() => new(_state.Seed, _state.TickIndex, _state.Cells, _state.Ledger);

    public string SaveToJson() => JsonSerializer.Serialize(_state, JsonOptions);

    public string CanonicalHash() => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(SaveToJson())));

    public WorldInventory Inventory()
    {
        var water = 0.0;
        var dry = 0.0;
        var nitrogen = 0.0;
        foreach (var cell in _state.Cells)
        {
            water += (cell.SoilWaterMm + cell.SurfaceWaterMm) * SimulationState.CellAreaM2;
            dry += (cell.Grass.BiomassGPerM2 + cell.Grass.SeedBankGPerM2
                + cell.Woody.BiomassGPerM2 + cell.Woody.SeedBankGPerM2
                + cell.DetritusDryGPerM2 + cell.SoilOrganicMatterGPerM2) * SimulationState.CellAreaM2;
            nitrogen += (cell.MineralNitrogenGPerM2 + cell.SoilOrganicNitrogenGPerM2
                + cell.Grass.NitrogenGPerM2 + cell.Grass.SeedBankNitrogenGPerM2
                + cell.Woody.NitrogenGPerM2 + cell.Woody.SeedBankNitrogenGPerM2
                + cell.DetritusNitrogenGPerM2) * SimulationState.CellAreaM2;
        }
        return new(water, dry, nitrogen);
    }

    public void AdvanceTicks(int ticks)
    {
        if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
        for (var i = 0; i < ticks; i++) AdvanceOneTick();
    }

    private void AdvanceOneTick()
    {
        _state.TickIndex++;
        if (_state.TickIndex % SimulationState.TicksPerHour == 0)
            AdvanceHour(_state.TickIndex / SimulationState.TicksPerHour - 1);
        if (_state.TickIndex % SimulationState.TicksPerDay == 0)
            CloseDay((int)((_state.TickIndex / SimulationState.TicksPerDay - 1) % 365) + 1);
    }

    private void AdvanceHour(long absoluteHour)
    {
        var dayOfYear = (int)(absoluteHour / 24 % 365) + 1;
        var hour = (int)(absoluteHour % 24);
        if (_state.CurrentWeatherDay != dayOfYear || _state.CurrentDayWeather.Length != 24 * 16)
        {
            _state.CurrentDayWeather = ClimateModel.ForecastDay(_state.Seed, dayOfYear, absoluteHour - hour,
                _state.ClimateHoursSinceRain).ByHourAndTile;
            _state.CurrentWeatherDay = dayOfYear;
        }

        var old = _state.Cells;
        if (_dailyRadiationDay != dayOfYear || _dailyRadiation is null)
        {
            _dailyRadiation = CalculateDailyRadiation(old, dayOfYear, _state.CurrentDayWeather);
            _dailyRadiationDay = dayOfYear;
        }
        var hDay = _dailyRadiation;
        var seasonTemperature = new double[old.Length];
        for (var i = 0; i < old.Length; i++)
        {
            var cell = old[i];
            seasonTemperature[i] = SolarGeometry.SeasonTemperatureC(dayOfYear, cell.ElevationM);
        }

        var updated = (WorldCell[])old.Clone();
        var botanyResults = new BotanyHourResult[old.Length];
        var ledger = _state.Ledger;
        for (var i = 0; i < old.Length; i++)
        {
            var cell = old[i];
            var tile = ClimateTile(cell);
            var weather = _state.CurrentDayWeather[hour * 16 + tile];
            var sun = SolarGeometry.At(dayOfYear, hour + 0.5);
            var terrainFactor = TerrainModel.TerrainSolarFactor(old, cell, sun.East, sun.North, sun.Up);
            var surfaceShortwave = sun.TopOfAtmosphereWPerM2 * (1 - 0.65 * weather.CloudCover) * terrainFactor;
            var hourlyEnergy = surfaceShortwave * 0.0036;
            var tMean = seasonTemperature[i];
            var etDay = Math.Clamp(2.5 * Math.Max(0, tMean + 5) / 20 * hDay[i] / 12, 0, 5);
            var etHour = hDay[i] <= 0 ? 0 : etDay * hourlyEnergy / hDay[i];
            var airTemperature = SolarGeometry.AirTemperatureC(dayOfYear, hour + 0.5, cell.ElevationM);
            var cover = BotanyModel.TotalCover(cell);
            var water = SoilModel.ReceiveRainAndEvaporate(cell, weather.PrecipitationMm, cover, etHour);
            var prepared = cell with { SoilWaterMm = water.SoilWaterMm, SurfaceWaterMm = water.SurfaceWaterMm };
            var botany = BotanyModel.GrowHour(prepared, airTemperature, hourlyEnergy, etHour,
                water.PlantAvailableWaterMm, water.WaterFactorAvailable, water.PlantWaterDemandMultiplier);
            var climate = new ClimateSample(airTemperature,
                ClimateModel.RelativeHumidityProxy(weather.CloudCover, botany.Cell.SoilWaterMm, cell.SoilCapacityMm),
                weather.PrecipitationMm, weather.CloudCover, surfaceShortwave);
            updated[i] = botany.Cell with { Climate = climate };
            botanyResults[i] = botany;

            var cellLiters = SimulationState.CellAreaM2;
            ledger.PrecipitationLiters += weather.PrecipitationMm * cellLiters;
            ledger.InterceptionLiters += water.InterceptionMm * cellLiters;
            ledger.OpenWaterEvaporationLiters += water.OpenWaterEvaporationMm * cellLiters;
            ledger.SoilEvaporationLiters += water.SoilEvaporationMm * cellLiters;
            ledger.PlantTranspirationLiters += botany.TranspirationMm * cellLiters;
            ledger.PhotosynthesisDryGrams += botany.NewBiomassGPerM2 * cellLiters;

        }

        updated = BotanyModel.DisperseSeeds(updated, botanyResults);
        var flow = SurfaceWaterModel.Route(updated);
        _state.Cells = flow.Cells;
        ledger.WaterOutflowLiters += flow.OutflowLiters;
        for (var tile = 0; tile < 16; tile++)
        {
            var rain = _state.CurrentDayWeather[hour * 16 + tile].PrecipitationMm;
            _state.ClimateHoursSinceRain[tile] = rain > 0 ? 0 : _state.ClimateHoursSinceRain[tile] + 1;
        }
    }

    private void CloseDay(int dayOfYear)
    {
        var cells = (WorldCell[])_state.Cells.Clone();
        for (var i = 0; i < cells.Length; i++)
        {
            var cell = cells[i];
            var meanTemperature = SolarGeometry.SeasonTemperatureC(dayOfYear, cell.ElevationM);
            var ecology = EcologyModel.CloseDay(cell, meanTemperature);
            cells[i] = ecology.Cell;
            _state.Ledger.DetritusRespirationDryGrams += ecology.DetritusRespirationGPerM2 * SimulationState.CellAreaM2;
            _state.Ledger.SoilOrganicMatterRespirationDryGrams += ecology.SoilOrganicMatterRespirationGPerM2 * SimulationState.CellAreaM2;
        }
        _state.Cells = cells;
    }

    private static double[] CalculateDailyRadiation(WorldCell[] cells, int dayOfYear, ClimateHour[] forecast)
    {
        var totals = new double[cells.Length];
        for (var i = 0; i < cells.Length; i++)
        {
            var cell = cells[i];
            var tile = ClimateTile(cell);
            for (var hour = 0; hour < 24; hour++)
            {
                var sun = SolarGeometry.At(dayOfYear, hour + 0.5);
                var terrainFactor = TerrainModel.TerrainSolarFactor(cells, cell, sun.East, sun.North, sun.Up);
                var cloud = forecast[hour * 16 + tile].CloudCover;
                totals[i] += sun.TopOfAtmosphereWPerM2 * (1 - 0.65 * cloud) * terrainFactor * 0.0036;
            }
        }
        return totals;
    }

    private static int ClimateTile(WorldCell cell) => (cell.Y / 16) * 4 + cell.X / 16;

    private static SimulationState CreateInitialState(ulong seed)
    {
        var cells = new WorldCell[SimulationState.Width * SimulationState.Height];
        for (var y = 0; y < SimulationState.Height; y++)
        for (var x = 0; x < SimulationState.Width; x++)
        {
            var index = y * SimulationState.Width + x;
            var elevation = TerrainModel.Elevation(x, y, seed);
            var kind = elevation < 75 ? SoilKind.Loam : SoilKind.Sandy;
            var profile = SoilModel.Profile(kind);
            var empty = new PlantPatch(false, 0, 0, 0, 0, 0, 0);
            var low = elevation < 75;
            var grassProbability = low ? 0.55 : 0.30;
            var woodyProbability = low ? 0.08 : 0.20;
            var grassPresent = CounterRandom.Unit(seed, CounterRandom.PlantPlacementTag, (ulong)index, 1, 0, 0) < grassProbability;
            var woodyPresent = CounterRandom.Unit(seed, CounterRandom.PlantPlacementTag, (ulong)index, 2, 0, 0) < woodyProbability;
            var grass = grassPresent ? new PlantPatch(true, 100, 1.8, 0, 0, 0, 0) : empty;
            var woody = woodyPresent ? new PlantPatch(true, 250, 3.0, 0, 0, 0, 0) : empty;
            cells[index] = new WorldCell(index, x, y, elevation, kind,
                profile.InitialWaterMm, profile.CapacityMm, profile.WiltMm, profile.KsatMmPerHour,
                0, profile.InitialN, profile.InitialSom, profile.InitialSomN, grass, woody,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, new ClimateSample(0, 0, 0, 0, 0));
        }

        var initialDay = 1;
        var initialSun = SolarGeometry.At(initialDay, 0);
        for (var i = 0; i < cells.Length; i++)
        {
            var cell = cells[i];
            var cloud = Math.Clamp(0.20 + 0.65 * Math.Exp(-24 / 4.0), 0, 0.85);
            var terrainFactor = TerrainModel.TerrainSolarFactor(cells, cell, initialSun.East, initialSun.North, initialSun.Up);
            var shortwave = initialSun.TopOfAtmosphereWPerM2 * (1 - 0.65 * cloud) * terrainFactor;
            cells[i] = cell with { Climate = new ClimateSample(
                SolarGeometry.AirTemperatureC(initialDay, 0, cell.ElevationM),
                ClimateModel.RelativeHumidityProxy(cloud, cell.SoilWaterMm, cell.SoilCapacityMm), 0, cloud, shortwave) };
        }

        return new SimulationState
        {
            Seed = seed,
            TickIndex = 0,
            Cells = cells,
            ClimateHoursSinceRain = Enumerable.Repeat(24, 16).ToArray(),
            CurrentDayWeather = [],
            CurrentWeatherDay = -1,
            Ledger = new FluxLedgerState()
        };
    }

    private static void Validate(SimulationState state)
    {
        if (state.SchemaVersion != 1) throw new InvalidDataException($"Unsupported save schema {state.SchemaVersion}.");
        if (state.TickIndex < 0 || state.Cells.Length != SimulationState.Width * SimulationState.Height
            || state.ClimateHoursSinceRain.Length != 16
            || (state.CurrentDayWeather.Length != 0 && state.CurrentDayWeather.Length != 24 * 16))
            throw new InvalidDataException("Save has invalid world dimensions or scheduler state.");
        foreach (var cell in state.Cells)
        {
            var values = new[] { cell.ElevationM, cell.SoilWaterMm, cell.SurfaceWaterMm, cell.MineralNitrogenGPerM2,
                cell.SoilOrganicMatterGPerM2, cell.SoilOrganicNitrogenGPerM2, cell.DetritusDryGPerM2, cell.DetritusNitrogenGPerM2,
                cell.Grass.BiomassGPerM2, cell.Grass.NitrogenGPerM2, cell.Grass.SeedBankGPerM2, cell.Grass.SeedBankNitrogenGPerM2,
                cell.Woody.BiomassGPerM2, cell.Woody.NitrogenGPerM2, cell.Woody.SeedBankGPerM2, cell.Woody.SeedBankNitrogenGPerM2 };
            if (values.Any(v => !double.IsFinite(v)) || values.Skip(1).Any(v => v < 0))
                throw new InvalidDataException($"Cell {cell.Index} contains a non-finite or negative stock.");
            if (cell.Index < 0 || cell.Index >= state.Cells.Length || cell.X != cell.Index % SimulationState.Width
                || cell.Y != cell.Index / SimulationState.Width)
                throw new InvalidDataException("Save contains invalid cell identity.");
        }
    }
}
