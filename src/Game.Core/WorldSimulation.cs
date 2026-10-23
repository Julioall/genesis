using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Game.Contracts;

namespace Game.Core;

public readonly record struct WorldInventory(double WaterLiters, double DryMatterGrams, double NitrogenGrams);

public sealed class WorldSimulation
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    private sealed record DailyRadiationCache(
        double[] RadiationByCell,
        SolarForcingSampleV1[] SolarByHour,
        double[][] TerrainFactorByHour);

    private SimulationState _state;
    private WorldCell[]? _alternateCells;
    private readonly ISolarForcingV1 _solarForcing;
    private readonly IClimateForcingV1 _climateForcing;
    private readonly IAtmosphericTemperatureV1 _atmosphericTemperature;
    private readonly IGeosphereTerrainV1 _terrain;
    private readonly ISoilWaterV1 _soilWater;
    private readonly ISoilBiogeochemistryV1 _soilBiogeochemistry;
    private readonly ISurfaceWaterV1 _surfaceWater;
    private readonly IBotanyV1 _botany;
    private readonly IEcologyV1 _ecology;
    private readonly IZoologyV1 _zoology;
    private readonly double[] _elevationsM;
    private readonly TerrainSolarNormalV1[] _terrainSolarNormals;
    private readonly int[] _climateTileByCell;
    private readonly SurfaceWaterRoutingPlanV1 _surfaceWaterRoutingPlan;
    private ZoologyCellStateV1[] _zoologyCells;
    private DailyRadiationCache? _dailyRadiation;
    private int _dailyRadiationDay = -1;
    private double[]? _dailySeasonTemperature;
    private int _dailySeasonTemperatureDay = -1;
    private double[]? _dailyPotentialEt;
    private int _dailyPotentialEtDay = -1;

    private WorldSimulation(
        SimulationState state,
        ISolarForcingV1 solarForcing,
        IClimateForcingV1 climateForcing,
        IAtmosphericTemperatureV1 atmosphericTemperature,
        IGeosphereTerrainV1 terrain,
        ISoilWaterV1 soilWater,
        ISoilBiogeochemistryV1 soilBiogeochemistry,
        ISurfaceWaterV1 surfaceWater,
        IBotanyV1 botany,
        IEcologyV1 ecology,
        IZoologyV1 zoology)
    {
        _state = state;
        _alternateCells = new WorldCell[state.Cells.Length];
        _solarForcing = solarForcing;
        _climateForcing = climateForcing;
        _atmosphericTemperature = atmosphericTemperature;
        _terrain = terrain;
        _soilWater = soilWater;
        _soilBiogeochemistry = soilBiogeochemistry;
        _surfaceWater = surfaceWater;
        _botany = botany;
        _ecology = ecology;
        _zoology = zoology;
        _elevationsM = state.Cells.Select(cell => cell.ElevationM).ToArray();
        _terrainSolarNormals = _terrain.CreateTerrainSolarNormals(_elevationsM);
        if (_terrainSolarNormals is null || _terrainSolarNormals.Length != state.Cells.Length)
            throw new InvalidOperationException("Geosphere returned an invalid terrain-solar normal field.");
        _climateTileByCell = state.Cells.Select(ClimateTile).ToArray();
        _surfaceWaterRoutingPlan = _surfaceWater.CreateRoutingPlan(ToSurfaceWaterCells(state.Cells));
        _zoologyCells = ToZoologyCells(state.Cells);
    }

    public static WorldSimulation Create(
        ulong seed,
        ISolarForcingV1 solarForcing,
        IClimateForcingV1 climateForcing,
        IAtmosphericTemperatureV1 atmosphericTemperature,
        IGeosphereTerrainV1 terrain,
        ISoilWaterV1 soilWater,
        ISoilBiogeochemistryV1 soilBiogeochemistry,
        ISurfaceWaterV1 surfaceWater,
        IBotanyV1 botany,
        IEcologyV1 ecology,
        IZoologyV1 zoology,
        InitialAnimalPopulationV1 initialPopulation = default)
    {
        ArgumentNullException.ThrowIfNull(solarForcing);
        ArgumentNullException.ThrowIfNull(climateForcing);
        ArgumentNullException.ThrowIfNull(atmosphericTemperature);
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentNullException.ThrowIfNull(soilWater);
        ArgumentNullException.ThrowIfNull(soilBiogeochemistry);
        ArgumentNullException.ThrowIfNull(surfaceWater);
        ArgumentNullException.ThrowIfNull(botany);
        ArgumentNullException.ThrowIfNull(ecology);
        ArgumentNullException.ThrowIfNull(zoology);
        if (initialPopulation.Herbivores < 0 || initialPopulation.Predators < 0)
            throw new ArgumentOutOfRangeException(nameof(initialPopulation), "Initial animal counts cannot be negative.");
        var state = CreateInitialState(seed, solarForcing, climateForcing, atmosphericTemperature,
            terrain, soilWater, surfaceWater, botany, zoology, initialPopulation);
        return new(state, solarForcing, climateForcing, atmosphericTemperature, terrain, soilWater,
            soilBiogeochemistry, surfaceWater, botany, ecology, zoology);
    }

    public static WorldSimulation Load(
        string json,
        ISolarForcingV1 solarForcing,
        IClimateForcingV1 climateForcing,
        IAtmosphericTemperatureV1 atmosphericTemperature,
        IGeosphereTerrainV1 terrain,
        ISoilWaterV1 soilWater,
        ISoilBiogeochemistryV1 soilBiogeochemistry,
        ISurfaceWaterV1 surfaceWater,
        IBotanyV1 botany,
        IEcologyV1 ecology,
        IZoologyV1 zoology)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(solarForcing);
        ArgumentNullException.ThrowIfNull(climateForcing);
        ArgumentNullException.ThrowIfNull(atmosphericTemperature);
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentNullException.ThrowIfNull(soilWater);
        ArgumentNullException.ThrowIfNull(soilBiogeochemistry);
        ArgumentNullException.ThrowIfNull(surfaceWater);
        ArgumentNullException.ThrowIfNull(botany);
        ArgumentNullException.ThrowIfNull(ecology);
        ArgumentNullException.ThrowIfNull(zoology);
        var state = JsonSerializer.Deserialize<SimulationState>(json, JsonOptions)
            ?? throw new InvalidDataException("Save contains no world state.");
        Validate(state);
        if (zoology is IZoologyGeneticsV1 geneticZoology)
            state.Animals = geneticZoology.EnsureGenomes(state.Seed, state.Animals);
        ValidateAnimalPopulation(state.Animals, state.NextAnimalId);
        return new(state, solarForcing, climateForcing, atmosphericTemperature, terrain, soilWater,
            soilBiogeochemistry, surfaceWater, botany, ecology, zoology);
    }

    public WorldSnapshot Snapshot() => new(_state.Seed, _state.TickIndex, _state.Cells, _state.Animals, _state.Ledger);

    public string SaveToJson() => JsonSerializer.Serialize(_state, JsonOptions);

    public string CanonicalHash() => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(SaveToJson())));

    public WorldInventory Inventory()
    {
        var water = 0.0;
        var dry = 0.0;
        var nitrogen = 0.0;
        foreach (var cell in _state.Cells)
        {
            water += (cell.SoilWaterMm + cell.SurfaceWaterMm + cell.GroundwaterMm) * SimulationState.CellAreaM2;
            dry += (cell.Grass.BiomassGPerM2 + cell.Grass.SeedBankGPerM2
                + cell.Woody.BiomassGPerM2 + cell.Woody.SeedBankGPerM2
                + cell.DetritusDryGPerM2 + cell.SoilOrganicMatterGPerM2) * SimulationState.CellAreaM2;
            nitrogen += (cell.MineralNitrogenGPerM2 + cell.SoilOrganicNitrogenGPerM2
                + cell.Grass.NitrogenGPerM2 + cell.Grass.SeedBankNitrogenGPerM2
                + cell.Woody.NitrogenGPerM2 + cell.Woody.SeedBankNitrogenGPerM2
                + cell.DetritusNitrogenGPerM2) * SimulationState.CellAreaM2;
        }
        foreach (var animal in _state.Animals)
        {
            water += animal.HydrationL;
            dry += animal.StructuralDryG + animal.ReserveDryG;
            nitrogen += animal.StructuralDryG * (animal.FunctionalProfileId == AnimalFunctionalProfileV1.Herbivore ? 0.020 : 0.025);
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
        if (_state.Animals.Length > 0)
        {
            var zoology = _zoology.AdvanceTick(new ZoologyTickInputV1(
                _state.Seed, _state.TickIndex, _zoologyCells,
                _state.Animals, _state.NextAnimalId));
            if (zoology.CellChanges is null || zoology.CellChanges.Length > _state.Cells.Length || zoology.Animals is null
                || zoology.WaterDeposits is null
                || !double.IsFinite(zoology.AnimalRespirationDryGrams) || zoology.AnimalRespirationDryGrams < 0
                || !double.IsFinite(zoology.AnimalEnergyConsumedJ) || zoology.AnimalEnergyConsumedJ < 0
                || !double.IsFinite(zoology.FecalFoodEnergyJ) || zoology.FecalFoodEnergyJ < 0
                || !double.IsFinite(zoology.UnmetEnergyDemandJ) || zoology.UnmetEnergyDemandJ < 0
                || Math.Abs(zoology.AnimalEnergyConsumedJ
                    - zoology.AnimalRespirationDryGrams * CreatureEnergyAccountingV1.JoulesPerDryGram) > 1e-5)
                throw new InvalidOperationException("Biosphere returned an invalid zoological tick state.");
            ValidateAnimalPopulation(zoology.Animals, zoology.NextAnimalId);
            if (zoology.WaterDeposits.Any(deposit => deposit.CellIndex < 0 || deposit.CellIndex >= _state.Cells.Length
                    || !double.IsFinite(deposit.Liters) || deposit.Liters < 0))
                throw new InvalidOperationException("Biosphere returned invalid water from captured animals.");
            ApplyZoologyCells(_state.Cells, zoology.CellChanges);
            _state.Animals = zoology.Animals;
            _state.NextAnimalId = zoology.NextAnimalId;
            _state.Ledger.AnimalRespirationDryGrams += zoology.AnimalRespirationDryGrams;
            if (zoology.WaterDeposits.Length > 0)
            {
                var depositedWater = _surfaceWater.DepositAnimalWater(ToSurfaceWaterCells(_state.Cells), zoology.WaterDeposits);
                if (depositedWater is null || depositedWater.Length != _state.Cells.Length)
                    throw new InvalidOperationException("Hydrosphere returned an invalid captured-animal water deposit.");
                ApplySurfaceWaterStates(_state.Cells, depositedWater);
                RefreshZoologyCellCache();
            }
        }
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
            _state.CurrentDayWeather = _climateForcing.ForecastDay(_state.Seed, dayOfYear,
                absoluteHour - hour, (int[])_state.ClimateHoursSinceRain.Clone()).ToArray();
            _state.CurrentWeatherDay = dayOfYear;
        }

        var old = _state.Cells;
        if (_dailyRadiationDay != dayOfYear || _dailyRadiation is null)
        {
            _dailyRadiation = CalculateDailyRadiation(old, dayOfYear, _state.CurrentDayWeather,
                _terrainSolarNormals, _climateTileByCell, _solarForcing, _terrain);
            _dailyRadiationDay = dayOfYear;
        }
        if (_dailySeasonTemperatureDay != dayOfYear || _dailySeasonTemperature is null)
        {
            _dailySeasonTemperature = new double[old.Length];
            for (var i = 0; i < old.Length; i++)
                _dailySeasonTemperature[i] = _atmosphericTemperature.SeasonalMeanTemperatureC(dayOfYear, old[i].ElevationM);
            _dailySeasonTemperatureDay = dayOfYear;
        }
        if (_dailyPotentialEtDay != dayOfYear || _dailyPotentialEt is null)
        {
            _dailyPotentialEt = new double[old.Length];
            for (var i = 0; i < old.Length; i++)
                _dailyPotentialEt[i] = Math.Clamp(2.5 * Math.Max(0, _dailySeasonTemperature[i] + 5) / 20
                    * _dailyRadiation.RadiationByCell[i] / 12, 0, 5);
            _dailyPotentialEtDay = dayOfYear;
        }
        var dailyRadiation = _dailyRadiation;
        var hDay = dailyRadiation.RadiationByCell;
        var seasonTemperature = _dailySeasonTemperature;
        var potentialEtDayByCell = _dailyPotentialEt;
        var sun = dailyRadiation.SolarByHour[hour];
        var terrainFactors = dailyRadiation.TerrainFactorByHour[hour];
        var diurnalTemperatureOffsetC = _atmosphericTemperature.DiurnalTemperatureOffsetC(hour + 0.5);

        var updated = _alternateCells;
        if (updated is null || updated.Length != old.Length || ReferenceEquals(updated, old))
            updated = new WorldCell[old.Length];
        var botanyResults = new BotanyHourResultV1[old.Length];
        var ledger = _state.Ledger;
        for (var i = 0; i < old.Length; i++)
        {
            var cell = old[i];
            var tile = _climateTileByCell[i];
            var weather = _state.CurrentDayWeather[hour * 16 + tile];
            var terrainFactor = terrainFactors[i];
            var surfaceShortwave = sun.TopOfAtmosphereIrradianceWPerM2
                * (1 - 0.65 * weather.CloudCover) * terrainFactor;
            var hourlyEnergy = surfaceShortwave * 0.0036;
            var etDay = potentialEtDayByCell[i];
            var etHour = hDay[i] <= 0 ? 0 : etDay * hourlyEnergy / hDay[i];
            var airTemperature = seasonTemperature[i] + diurnalTemperatureOffsetC;
            var botanyState = ToBotanyState(cell);
            var plantCover = _botany.CoverComponents(botanyState.Plants);
            var cover = plantCover.Total;
            var soilState = new SoilHydraulicStateV1(cell.SoilWaterMm, cell.SoilCapacityMm,
                cell.SoilWiltMm, cell.SoilKsatMmPerHour);
            var infiltrationCapacityMm = _soilWater.MaximumInfiltrationMm(soilState);
            var surfaceWaterState = new SurfaceWaterStateV1(cell.Index, cell.SurfaceWaterMm, cell.GroundwaterMm);
            var surfaceExchange = _surfaceWater.ReceivePrecipitation(surfaceWaterState,
                weather.PrecipitationMm, cover, infiltrationCapacityMm, etHour);
            var soilWater = _soilWater.ApplyInfiltrationAndEvaporate(soilState,
                surfaceExchange.InfiltrationMm, cover, etHour, surfaceExchange.PlantWaterDemandMultiplier);
            var botany = _botany.GrowHour(new BotanyHourInputV1(botanyState,
                cell.MineralNitrogenGPerM2, airTemperature, hourlyEnergy, etHour,
                soilWater.PlantAvailableWaterMm, soilWater.WaterFactorAvailable,
                surfaceExchange.PlantWaterDemandMultiplier), plantCover);
            var transpiration = _soilWater.ApplyPlantTranspiration(
                new SoilHydraulicStateV1(soilWater.SoilWaterMm, cell.SoilCapacityMm,
                    cell.SoilWiltMm, cell.SoilKsatMmPerHour), botany.TranspirationMm);
            var mineralNitrogen = _soilBiogeochemistry.ConfirmMineralNitrogenAfterGrowth(
                cell.MineralNitrogenGPerM2, botany.MineralNitrogenAfterGrowthProposalGPerM2);
            var grownState = botany.State;
            var dailyFactors = grownState.DailyFactors;
            var climate = new ClimateSample(airTemperature,
                _climateForcing.RelativeHumidityProxy(weather.CloudCover, transpiration.SoilWaterMm, cell.SoilCapacityMm),
                weather.PrecipitationMm, weather.CloudCover, surfaceShortwave);
            updated[i] = cell with
            {
                SoilWaterMm = transpiration.SoilWaterMm,
                SurfaceWaterMm = surfaceExchange.SurfaceWaterMm,
                MineralNitrogenGPerM2 = mineralNitrogen,
                Grass = grownState.Plants.Grass,
                Woody = grownState.Plants.Woody,
                GrassWaterFactorSum = dailyFactors.GrassWaterFactorSum,
                GrassNitrogenFactorSum = dailyFactors.GrassNitrogenFactorSum,
                GrassTemperatureFactorSum = dailyFactors.GrassTemperatureFactorSum,
                GrassOutOfRangeHours = dailyFactors.GrassOutOfRangeHours,
                WoodyWaterFactorSum = dailyFactors.WoodyWaterFactorSum,
                WoodyNitrogenFactorSum = dailyFactors.WoodyNitrogenFactorSum,
                WoodyTemperatureFactorSum = dailyFactors.WoodyTemperatureFactorSum,
                WoodyOutOfRangeHours = dailyFactors.WoodyOutOfRangeHours,
                Climate = climate
            };
            botanyResults[i] = botany;

            var cellLiters = SimulationState.CellAreaM2;
            ledger.PrecipitationLiters += weather.PrecipitationMm * cellLiters;
            ledger.InterceptionLiters += surfaceExchange.InterceptionMm * cellLiters;
            ledger.OpenWaterEvaporationLiters += surfaceExchange.OpenWaterEvaporationMm * cellLiters;
            ledger.SoilEvaporationLiters += soilWater.SoilEvaporationMm * cellLiters;
            ledger.PlantTranspirationLiters += transpiration.ConfirmedTranspirationMm * cellLiters;
            ledger.PhotosynthesisDryGrams += botany.NewBiomassGPerM2 * cellLiters;

        }

        var hasSeeds = false;
        for (var index = 0; index < botanyResults.Length; index++)
        {
            if (botanyResults[index].GrassSeedProducedGPerM2 != 0
                || botanyResults[index].WoodySeedProducedGPerM2 != 0)
            {
                hasSeeds = true;
                break;
            }
        }
        if (hasSeeds)
        {
            var botanyStates = ToBotanyStates(updated);
            var dispersedBotany = _botany.DisperseSeeds(botanyStates, botanyResults);
            if (dispersedBotany is null || dispersedBotany.Length != updated.Length)
                throw new InvalidOperationException("Biosphere returned a botanical field with invalid dimensions.");
            for (var index = 0; index < updated.Length; index++)
                updated[index] = ApplyBotanyState(updated[index], dispersedBotany[index]);
        }

        var surfaceCells = ToSurfaceWaterCells(updated);
        var flow = _surfaceWater.Route(surfaceCells, _surfaceWaterRoutingPlan);
        ApplySurfaceWaterStates(updated, flow.Cells);
        _state.Cells = updated;
        _alternateCells = old;
        if (_state.Animals.Length > 0) RefreshZoologyCellCache();
        ledger.WaterOutflowLiters += flow.OutflowLiters;
        for (var tile = 0; tile < 16; tile++)
        {
            var rain = _state.CurrentDayWeather[hour * 16 + tile].PrecipitationMm;
            _state.ClimateHoursSinceRain[tile] = rain > 0 ? 0 : _state.ClimateHoursSinceRain[tile] + 1;
        }
    }

    private void CloseDay(int dayOfYear)
    {
        if (_state.Animals.Length > 0)
        {
            var waterPreparation = _zoology.PrepareDailyWater((AnimalAgentV1[])_state.Animals.Clone());
            if (waterPreparation.AnimalsAfterExcretion is null || waterPreparation.WaterRequests is null
                || !double.IsFinite(waterPreparation.ExcretionWaterLiters) || waterPreparation.ExcretionWaterLiters < 0)
                throw new InvalidOperationException("Biosphere returned an invalid daily drinking request.");
            if (waterPreparation.AnimalsAfterExcretion.Length != _state.Animals.Length)
                throw new InvalidOperationException("Biosphere changed the animal population during water preparation.");
            ValidateAnimalPopulation(waterPreparation.AnimalsAfterExcretion, _state.NextAnimalId);
            var animalsById = waterPreparation.AnimalsAfterExcretion.ToDictionary(animal => animal.AnimalId);
            if (waterPreparation.WaterRequests.Any(request => !animalsById.TryGetValue(request.ConsumerId, out var animal)
                    || request.CellIndex != (int)(animal.PositionYM / Math.Sqrt(SimulationState.CellAreaM2)) * SimulationState.Width
                        + (int)(animal.PositionXM / Math.Sqrt(SimulationState.CellAreaM2))))
                throw new InvalidOperationException("Biosphere requested water for an unknown animal or cell.");
            var surfaceWaterBeforeDrink = ToSurfaceWaterCells(_state.Cells);
            var waterAllocation = _surfaceWater.AllocateAnimalWater(surfaceWaterBeforeDrink, waterPreparation.WaterRequests);
            if (waterAllocation.Cells is null || waterAllocation.Cells.Length != _state.Cells.Length || waterAllocation.Grants is null)
                throw new InvalidOperationException("Hydrosphere returned an invalid animal water allocation.");
            var cellsAfterDrinking = (WorldCell[])_state.Cells.Clone();
            ApplySurfaceWaterStates(cellsAfterDrinking, waterAllocation.Cells);

            var animalDay = _zoology.CloseDay(new ZoologyDailyInputV1(
                _state.Seed, _state.TickIndex, _state.TickIndex / SimulationState.TicksPerDay - 1,
                ToZoologyCells(cellsAfterDrinking), waterPreparation.AnimalsAfterExcretion,
                waterAllocation.Grants, _state.NextAnimalId));
            if (animalDay.CellChanges is null || animalDay.CellChanges.Length > _state.Cells.Length || animalDay.Animals is null
                || animalDay.WaterDeposits is null)
                throw new InvalidOperationException("Biosphere returned an invalid zoological daily state.");
            ValidateAnimalPopulation(animalDay.Animals, animalDay.NextAnimalId);
            var updatedCells = (WorldCell[])cellsAfterDrinking.Clone();
            ApplyZoologyCells(updatedCells, animalDay.CellChanges);
            _state.Cells = updatedCells;
            _state.Animals = animalDay.Animals;
            _state.NextAnimalId = animalDay.NextAnimalId;
            _state.Ledger.AnimalExcretionWaterLiters += waterPreparation.ExcretionWaterLiters;
            if (animalDay.WaterDeposits.Length > 0)
            {
                var depositedWater = _surfaceWater.DepositAnimalWater(ToSurfaceWaterCells(_state.Cells), animalDay.WaterDeposits);
                if (depositedWater is null || depositedWater.Length != _state.Cells.Length)
                    throw new InvalidOperationException("Hydrosphere returned an invalid animal water deposit.");
                var cellsAfterDeath = (WorldCell[])_state.Cells.Clone();
                ApplySurfaceWaterStates(cellsAfterDeath, depositedWater);
                _state.Cells = cellsAfterDeath;
            }
        }

        var cells = (WorldCell[])_state.Cells.Clone();
        for (var i = 0; i < cells.Length; i++)
        {
            var cell = cells[i];
            var meanTemperature = _atmosphericTemperature.SeasonalMeanTemperatureC(dayOfYear, cell.ElevationM);
            var ecology = _ecology.CloseDay(new EcologyDailyInputV1(
                ToBotanyState(cell), cell.DetritusDryGPerM2, cell.DetritusNitrogenGPerM2,
                new SoilMoistureContextV1(cell.SoilWaterMm, cell.SoilCapacityMm, cell.SoilWiltMm),
                new SoilBiogeochemistryStateV1(cell.SoilOrganicMatterGPerM2,
                    cell.SoilOrganicNitrogenGPerM2, cell.MineralNitrogenGPerM2), meanTemperature));
            var soilBiogeochemistry = _soilBiogeochemistry.ConfirmEcologyProposal(
                new SoilBiogeochemistryStateV1(cell.SoilOrganicMatterGPerM2,
                    cell.SoilOrganicNitrogenGPerM2, cell.MineralNitrogenGPerM2), ecology.SoilProposal);
            cells[i] = ApplyBotanyState(cell, ecology.Botany) with
            {
                DetritusDryGPerM2 = ecology.DetritusDryGPerM2,
                DetritusNitrogenGPerM2 = ecology.DetritusNitrogenGPerM2,
                SoilOrganicMatterGPerM2 = soilBiogeochemistry.SoilOrganicMatterGPerM2,
                SoilOrganicNitrogenGPerM2 = soilBiogeochemistry.SoilOrganicNitrogenGPerM2,
                MineralNitrogenGPerM2 = soilBiogeochemistry.MineralNitrogenGPerM2
            };
            _state.Ledger.DetritusRespirationDryGrams += ecology.DetritusRespirationGPerM2 * SimulationState.CellAreaM2;
            _state.Ledger.SoilOrganicMatterRespirationDryGrams += ecology.SoilOrganicMatterRespirationGPerM2 * SimulationState.CellAreaM2;
        }
        _state.Cells = cells;
        if (_state.Animals.Length > 0) RefreshZoologyCellCache();
    }

    private static DailyRadiationCache CalculateDailyRadiation(
        WorldCell[] cells,
        int dayOfYear,
        ClimateHour[] forecast,
        TerrainSolarNormalV1[] terrainSolarNormals,
        int[] climateTileByCell,
        ISolarForcingV1 solarForcing,
        IGeosphereTerrainV1 terrain)
    {
        var totals = new double[cells.Length];
        var solarByHour = new SolarForcingSampleV1[24];
        var terrainFactorByHour = new double[24][];
        for (var hour = 0; hour < 24; hour++)
        {
            var sun = solarForcing.At(dayOfYear, hour + 0.5);
            solarByHour[hour] = sun;
            var terrainFactors = terrain.TerrainSolarFactors(terrainSolarNormals,
                sun.EastDirection, sun.NorthDirection, sun.UpDirection);
            if (terrainFactors is null || terrainFactors.Length != cells.Length)
                throw new InvalidOperationException("Geosphere returned an invalid hourly terrain-solar field.");
            terrainFactorByHour[hour] = terrainFactors;
            for (var i = 0; i < cells.Length; i++)
            {
                var cell = cells[i];
                var terrainFactor = terrainFactors[i];
                var tile = climateTileByCell[i];
                var cloud = forecast[hour * 16 + tile].CloudCover;
                totals[i] += sun.TopOfAtmosphereIrradianceWPerM2 * (1 - 0.65 * cloud)
                    * terrainFactor * 0.0036;
            }
        }
        return new(totals, solarByHour, terrainFactorByHour);
    }

    private static int ClimateTile(WorldCell cell)
    {
        var tileWidth = SimulationState.Width / 4;
        var tileHeight = SimulationState.Height / 4;
        return (cell.Y / tileHeight) * 4 + cell.X / tileWidth;
    }

    private static BotanyCellStateV1 ToBotanyState(WorldCell cell)
        => new(new PlantPatchPair(cell.Grass, cell.Woody), new BotanyDailyFactorsV1(
            cell.GrassWaterFactorSum, cell.GrassNitrogenFactorSum, cell.GrassTemperatureFactorSum,
            cell.GrassOutOfRangeHours, cell.WoodyWaterFactorSum, cell.WoodyNitrogenFactorSum,
            cell.WoodyTemperatureFactorSum, cell.WoodyOutOfRangeHours));

    private static WorldCell ApplyBotanyState(WorldCell cell, BotanyCellStateV1 state)
        => cell with
        {
            Grass = state.Plants.Grass,
            Woody = state.Plants.Woody,
            GrassWaterFactorSum = state.DailyFactors.GrassWaterFactorSum,
            GrassNitrogenFactorSum = state.DailyFactors.GrassNitrogenFactorSum,
            GrassTemperatureFactorSum = state.DailyFactors.GrassTemperatureFactorSum,
            GrassOutOfRangeHours = state.DailyFactors.GrassOutOfRangeHours,
            WoodyWaterFactorSum = state.DailyFactors.WoodyWaterFactorSum,
            WoodyNitrogenFactorSum = state.DailyFactors.WoodyNitrogenFactorSum,
            WoodyTemperatureFactorSum = state.DailyFactors.WoodyTemperatureFactorSum,
            WoodyOutOfRangeHours = state.DailyFactors.WoodyOutOfRangeHours
        };

    private static SimulationState CreateInitialState(
        ulong seed,
        ISolarForcingV1 solarForcing,
        IClimateForcingV1 climateForcing,
        IAtmosphericTemperatureV1 atmosphericTemperature,
        IGeosphereTerrainV1 terrain,
        ISoilWaterV1 soilWater,
        ISurfaceWaterV1 surfaceWater,
        IBotanyV1 botany,
        IZoologyV1 zoology,
        InitialAnimalPopulationV1 initialPopulation)
    {
        var cells = new WorldCell[SimulationState.Width * SimulationState.Height];
        var elevationField = terrain.GenerateElevationField(seed);
        var emptyPatch = new PlantPatch(false, 0, 0, 0, 0, 0, 0);
        for (var y = 0; y < SimulationState.Height; y++)
        for (var x = 0; x < SimulationState.Width; x++)
        {
            var index = y * SimulationState.Width + x;
            var elevation = elevationField[index];
            var kind = elevation < 75 ? SoilKind.Loam : SoilKind.Sandy;
            var profile = soilWater.Profile(kind);
            cells[index] = new WorldCell(index, x, y, elevation, kind,
                profile.InitialWaterMm, profile.CapacityMm, profile.WiltMm, profile.KsatMmPerHour,
                0, 0, profile.InitialMineralNitrogenGPerM2, profile.InitialSoilOrganicMatterGPerM2,
                profile.InitialSoilOrganicNitrogenGPerM2, emptyPatch, emptyPatch,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, new ClimateSample(0, 0, 0, 0, 0));
        }
        var surfaceWaterTerrain = cells.Select(cell => new SurfaceWaterCellV1(
            cell.Index, cell.X, cell.Y, cell.ElevationM, cell.SurfaceWaterMm, cell.GroundwaterMm)).ToArray();
        ApplySurfaceWaterStates(cells, surfaceWater.SeedInitialWater(surfaceWaterTerrain));
        var initialSurfaceWater = cells.Select(cell => cell.SurfaceWaterMm).ToArray();
        var terrainSlopeMagnitude = new double[elevationField.Length];
        for (var index = 0; index < terrainSlopeMagnitude.Length; index++)
            terrainSlopeMagnitude[index] = terrain.SlopeMagnitude(elevationField,
                index % SimulationState.Width, index / SimulationState.Width);
        var initialPatches = botany.CreateInitialPatches(seed, elevationField, initialSurfaceWater,
            terrainSlopeMagnitude);
        for (var index = 0; index < cells.Length; index++)
        {
            cells[index] = cells[index] with
            {
                Grass = initialPatches[index].Grass,
                Woody = initialPatches[index].Woody
            };
        }

        var initialDay = 1;
        var initialSun = solarForcing.At(initialDay, 0);
        for (var i = 0; i < cells.Length; i++)
        {
            var cell = cells[i];
            var cloud = Math.Clamp(0.20 + 0.65 * Math.Exp(-24 / 4.0), 0, 0.85);
            var terrainFactor = terrain.TerrainSolarFactor(elevationField, cell.X, cell.Y,
                initialSun.EastDirection, initialSun.NorthDirection, initialSun.UpDirection);
            var shortwave = initialSun.TopOfAtmosphereIrradianceWPerM2 * (1 - 0.65 * cloud) * terrainFactor;
            cells[i] = cell with { Climate = new ClimateSample(
                atmosphericTemperature.AirTemperatureC(initialDay, 0, cell.ElevationM),
                climateForcing.RelativeHumidityProxy(cloud, cell.SoilWaterMm, cell.SoilCapacityMm), 0, cloud, shortwave) };
        }

        var animals = zoology.CreateInitialPopulation(seed, ToZoologyCells(cells), initialPopulation);
        var state = new SimulationState
        {
            Seed = seed,
            TickIndex = 0,
            Cells = cells,
            Animals = animals,
            NextAnimalId = (ulong)animals.Length + 1,
            ClimateHoursSinceRain = Enumerable.Repeat(24, 16).ToArray(),
            CurrentDayWeather = [],
            CurrentWeatherDay = -1,
            Ledger = new FluxLedgerState()
        };
        Validate(state);
        return state;
    }

    private static void ApplySurfaceWaterStates(WorldCell[] cells, SurfaceWaterStateV1[] states)
    {
        if (states.Length != cells.Length)
            throw new InvalidOperationException("Hydrosphere returned a water field with invalid dimensions.");

        for (var index = 0; index < cells.Length; index++)
        {
            if (states[index].CellIndex != cells[index].Index)
                throw new InvalidOperationException("Hydrosphere returned water states in an invalid cell order.");
            if (!double.IsFinite(states[index].SurfaceWaterMm) || states[index].SurfaceWaterMm < 0
                || !double.IsFinite(states[index].GroundwaterMm) || states[index].GroundwaterMm < 0)
                throw new InvalidOperationException($"Hydrosphere returned an invalid water stock for cell {index}.");
            cells[index] = cells[index] with
            {
                SurfaceWaterMm = states[index].SurfaceWaterMm,
                GroundwaterMm = states[index].GroundwaterMm
            };
        }
    }

    private static ZoologyCellStateV1[] ToZoologyCells(WorldCell[] cells)
    {
        var result = new ZoologyCellStateV1[cells.Length];
        for (var index = 0; index < cells.Length; index++)
        {
            var cell = cells[index];
            result[index] = new(cell.Index, cell.X, cell.Y, cell.Grass, cell.Woody,
                cell.MineralNitrogenGPerM2, cell.DetritusDryGPerM2, cell.DetritusNitrogenGPerM2,
                cell.ElevationM, cell.SurfaceWaterMm);
        }
        return result;
    }

    private static BotanyCellStateV1[] ToBotanyStates(WorldCell[] cells)
    {
        var result = new BotanyCellStateV1[cells.Length];
        for (var index = 0; index < cells.Length; index++) result[index] = ToBotanyState(cells[index]);
        return result;
    }

    private void RefreshZoologyCellCache() => _zoologyCells = ToZoologyCells(_state.Cells);

    private static SurfaceWaterCellV1[] ToSurfaceWaterCells(WorldCell[] cells)
    {
        var result = new SurfaceWaterCellV1[cells.Length];
        for (var index = 0; index < cells.Length; index++)
        {
            var cell = cells[index];
            result[index] = new(cell.Index, cell.X, cell.Y, cell.ElevationM,
                cell.SurfaceWaterMm, cell.GroundwaterMm);
        }
        return result;
    }

    private void ApplyZoologyCells(WorldCell[] cells, ZoologyCellStateV1[] zoologyCells)
    {
        if (zoologyCells.Length == 0) return;
        var changedIndices = zoologyCells.Length > 1 ? new HashSet<int>(zoologyCells.Length) : null;
        foreach (var zoologyCell in zoologyCells)
        {
            var index = zoologyCell.Index;
            if (index < 0 || index >= cells.Length || changedIndices is not null && !changedIndices.Add(index)
                || zoologyCell.X != cells[index].X || zoologyCell.Y != cells[index].Y)
                throw new InvalidOperationException("Biosphere returned zoological states in an invalid cell order.");
            var current = cells[index];
            if (zoologyCell.GroundElevationM != current.ElevationM
                || zoologyCell.SurfaceWaterMm != current.SurfaceWaterMm)
                throw new InvalidOperationException($"Biosphere changed owner-controlled terrain or water observations for cell {index}.");
            var grass = zoologyCell.Grass;
            var woody = zoologyCell.Woody;
            if (!double.IsFinite(zoologyCell.MineralNitrogenGPerM2) || zoologyCell.MineralNitrogenGPerM2 < 0
                || !double.IsFinite(zoologyCell.DetritusDryGPerM2) || zoologyCell.DetritusDryGPerM2 < 0
                || !double.IsFinite(zoologyCell.DetritusNitrogenGPerM2) || zoologyCell.DetritusNitrogenGPerM2 < 0
                || !double.IsFinite(grass.BiomassGPerM2) || grass.BiomassGPerM2 < 0
                || !double.IsFinite(grass.NitrogenGPerM2) || grass.NitrogenGPerM2 < 0
                || !double.IsFinite(grass.SeedBankGPerM2) || grass.SeedBankGPerM2 < 0
                || !double.IsFinite(grass.SeedBankNitrogenGPerM2) || grass.SeedBankNitrogenGPerM2 < 0
                || !double.IsFinite(woody.BiomassGPerM2) || woody.BiomassGPerM2 < 0
                || !double.IsFinite(woody.NitrogenGPerM2) || woody.NitrogenGPerM2 < 0
                || !double.IsFinite(woody.SeedBankGPerM2) || woody.SeedBankGPerM2 < 0
                || !double.IsFinite(woody.SeedBankNitrogenGPerM2) || woody.SeedBankNitrogenGPerM2 < 0)
                throw new InvalidOperationException($"Biosphere returned a negative or non-finite zoological stock for cell {index}.");
            var mineralNitrogenDeposit = zoologyCell.MineralNitrogenGPerM2 - current.MineralNitrogenGPerM2;
            var detritusDryDeposit = zoologyCell.DetritusDryGPerM2 - current.DetritusDryGPerM2;
            var detritusNitrogenDeposit = zoologyCell.DetritusNitrogenGPerM2 - current.DetritusNitrogenGPerM2;
            if (mineralNitrogenDeposit < -1e-12 || detritusDryDeposit < -1e-12 || detritusNitrogenDeposit < -1e-12)
                throw new InvalidOperationException($"Zoology returned a withdrawal from a stock it does not own in cell {index}.");
            var mineralNitrogen = mineralNitrogenDeposit > 0
                ? _soilBiogeochemistry.DepositMineralNitrogen(current.MineralNitrogenGPerM2, mineralNitrogenDeposit)
                : current.MineralNitrogenGPerM2;
            var detritus = detritusDryDeposit > 0 || detritusNitrogenDeposit > 0
                ? _ecology.ConfirmDetritusDeposit(
                    new EcologyDetritusStateV1(current.DetritusDryGPerM2, current.DetritusNitrogenGPerM2),
                    new EcologyDetritusStateV1(Math.Max(0, detritusDryDeposit), Math.Max(0, detritusNitrogenDeposit)))
                : new EcologyDetritusStateV1(current.DetritusDryGPerM2, current.DetritusNitrogenGPerM2);
            cells[index] = cells[index] with
            {
                Grass = zoologyCell.Grass,
                Woody = zoologyCell.Woody,
                MineralNitrogenGPerM2 = mineralNitrogen,
                DetritusDryGPerM2 = detritus.DryGPerM2,
                DetritusNitrogenGPerM2 = detritus.NitrogenGPerM2
            };
            var committed = cells[index];
            _zoologyCells[index] = new ZoologyCellStateV1(committed.Index, committed.X, committed.Y,
                committed.Grass, committed.Woody, committed.MineralNitrogenGPerM2,
                committed.DetritusDryGPerM2, committed.DetritusNitrogenGPerM2,
                committed.ElevationM, committed.SurfaceWaterMm);
        }
    }

    private static void Validate(SimulationState state)
    {
        if (state.SchemaVersion == 1)
        {
            state.Animals ??= [];
            state.NextAnimalId = state.Animals.Length == 0 ? 1 : state.Animals.Max(animal => animal.AnimalId) + 1;
            state.SchemaVersion = 2;
        }
        if (state.SchemaVersion == 2) state.SchemaVersion = 3;
        if (state.SchemaVersion != SimulationState.CurrentSchemaVersion)
            throw new InvalidDataException($"Unsupported save schema {state.SchemaVersion}.");
        if (state.TickIndex < 0 || state.Cells.Length != SimulationState.Width * SimulationState.Height
            || state.Animals is null || state.NextAnimalId == 0
            || state.ClimateHoursSinceRain.Length != 16
            || (state.CurrentDayWeather.Length != 0 && state.CurrentDayWeather.Length != 24 * 16))
            throw new InvalidDataException("Save has invalid world dimensions or scheduler state.");
        foreach (var cell in state.Cells)
        {
            var values = new[] { cell.ElevationM, cell.SoilWaterMm, cell.SurfaceWaterMm, cell.GroundwaterMm, cell.MineralNitrogenGPerM2,
                cell.SoilOrganicMatterGPerM2, cell.SoilOrganicNitrogenGPerM2, cell.DetritusDryGPerM2, cell.DetritusNitrogenGPerM2,
                cell.Grass.BiomassGPerM2, cell.Grass.NitrogenGPerM2, cell.Grass.SeedBankGPerM2, cell.Grass.SeedBankNitrogenGPerM2,
                cell.Woody.BiomassGPerM2, cell.Woody.NitrogenGPerM2, cell.Woody.SeedBankGPerM2, cell.Woody.SeedBankNitrogenGPerM2 };
            if (values.Any(v => !double.IsFinite(v)) || values.Skip(1).Any(v => v < 0))
                throw new InvalidDataException($"Cell {cell.Index} contains a non-finite or negative stock.");
            if (cell.Index < 0 || cell.Index >= state.Cells.Length || cell.X != cell.Index % SimulationState.Width
                || cell.Y != cell.Index / SimulationState.Width)
                throw new InvalidDataException("Save contains invalid cell identity.");
        }
        ValidateAnimalPopulation(state.Animals, state.NextAnimalId);
    }

    private static void ValidateAnimalPopulation(AnimalAgentV1[] animals, ulong nextAnimalId)
    {
        if (animals is null || nextAnimalId == 0)
            throw new InvalidDataException("Animal state is missing or its next identity is invalid.");
        var strictlyAscendingIds = true;
        var previousId = 0UL;
        var maximumId = 0UL;
        for (var index = 0; index < animals.Length; index++)
        {
            var animal = animals[index];
            var profile = animal.FunctionalProfileId switch
            {
                AnimalFunctionalProfileV1.Herbivore => (MaximumStructuralDryG: 8000.0, NitrogenFraction: 0.020),
                AnimalFunctionalProfileV1.Predator => (MaximumStructuralDryG: 20000.0, NitrogenFraction: 0.025),
                _ => (MaximumStructuralDryG: 0.0, NitrogenFraction: 0.0)
            };
            if (index > 0 && animal.AnimalId <= previousId) strictlyAscendingIds = false;
            previousId = animal.AnimalId;
            maximumId = Math.Max(maximumId, animal.AnimalId);
            if (animal.AnimalId == 0
                || !double.IsFinite(animal.PositionXM) || !double.IsFinite(animal.PositionYM)
                || !double.IsFinite(animal.AgeDays) || !double.IsFinite(animal.StructuralDryG)
                || !double.IsFinite(animal.ReserveDryG) || !double.IsFinite(animal.HydrationL)
                || !double.IsFinite(animal.MaintenanceDeficitDays) || !double.IsFinite(animal.DehydrationDeficitDays)
                || !double.IsFinite(animal.ReproductionCooldownDays)
                || animal.AgeDays < 0 || animal.ReserveDryG < 0 || animal.HydrationL < 0
                || animal.MaintenanceDeficitDays < 0 || animal.DehydrationDeficitDays < 0
                || animal.ReproductionCooldownDays < 0
                || animal.StructuralDryG <= 0 || animal.StructuralDryG > profile.MaximumStructuralDryG
                || animal.ReserveDryG > 0.20 * animal.StructuralDryG + 1e-9
                || animal.PositionXM < 0 || animal.PositionYM < 0
                || animal.PositionXM >= SimulationState.Width * Math.Sqrt(SimulationState.CellAreaM2)
                || animal.PositionYM >= SimulationState.Height * Math.Sqrt(SimulationState.CellAreaM2)
                || animal.LastCaptureDay < -1
                || profile.NitrogenFraction <= 0)
                throw new InvalidDataException($"Animal {animal.AnimalId} contains an invalid identity, profile, position, or stock.");
        }
        if (!strictlyAscendingIds)
        {
            var animalIds = new HashSet<ulong>();
            foreach (var animal in animals)
                if (!animalIds.Add(animal.AnimalId))
                    throw new InvalidDataException($"Animal {animal.AnimalId} contains a duplicate identity.");
        }
        if (animals.Length > 0 && nextAnimalId <= maximumId)
            throw new InvalidDataException("Animal state contains an invalid next identity.");
    }
}
