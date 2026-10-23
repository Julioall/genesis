namespace Game.Contracts;

public enum SoilKind : byte { Sandy, Loam }
public enum PlantKind : byte { Grass, Woody }
public enum Season : byte { Winter, Spring, Summer, Autumn }
public readonly record struct ClimateHour(double PrecipitationMm, double CloudCover);
public readonly record struct ClimateSample(
    double AirTemperatureC,
    double RelativeHumidityProxy,
    double PrecipitationLastHourMm,
    double CloudCoverFraction,
    double SurfaceShortwaveWPerM2);

public readonly record struct PlantPatch(
    bool Present,
    double BiomassGPerM2,
    double NitrogenGPerM2,
    double SeedBankGPerM2,
    double SeedBankNitrogenGPerM2,
    double GerminationEligibleGPerM2,
    double GerminationEligibleNGPerM2);

public readonly record struct WorldCell(
    int Index,
    int X,
    int Y,
    double ElevationM,
    SoilKind SoilKind,
    double SoilWaterMm,
    double SoilCapacityMm,
    double SoilWiltMm,
    double SoilKsatMmPerHour,
    double SurfaceWaterMm,
    double GroundwaterMm,
    double MineralNitrogenGPerM2,
    double SoilOrganicMatterGPerM2,
    double SoilOrganicNitrogenGPerM2,
    PlantPatch Grass,
    PlantPatch Woody,
    double DetritusDryGPerM2,
    double DetritusNitrogenGPerM2,
    double GrassWaterFactorSum,
    double GrassNitrogenFactorSum,
    double GrassTemperatureFactorSum,
    double GrassOutOfRangeHours,
    double WoodyWaterFactorSum,
    double WoodyNitrogenFactorSum,
    double WoodyTemperatureFactorSum,
    double WoodyOutOfRangeHours,
    ClimateSample Climate);

public sealed class FluxLedgerState
{
    public double PrecipitationLiters { get; set; }
    public double InterceptionLiters { get; set; }
    public double OpenWaterEvaporationLiters { get; set; }
    public double SoilEvaporationLiters { get; set; }
    public double PlantTranspirationLiters { get; set; }
    public double WaterOutflowLiters { get; set; }
    public double PhotosynthesisDryGrams { get; set; }
    public double AnimalRespirationDryGrams { get; set; }
    public double AnimalExcretionWaterLiters { get; set; }
    public double DetritusRespirationDryGrams { get; set; }
    public double SoilOrganicMatterRespirationDryGrams { get; set; }

    public FluxLedgerSnapshot Snapshot() => new(
        PrecipitationLiters, InterceptionLiters, OpenWaterEvaporationLiters, SoilEvaporationLiters,
        PlantTranspirationLiters, WaterOutflowLiters, PhotosynthesisDryGrams,
        AnimalRespirationDryGrams, AnimalExcretionWaterLiters,
        DetritusRespirationDryGrams, SoilOrganicMatterRespirationDryGrams);
}

public readonly record struct FluxLedgerSnapshot(
    double PrecipitationLiters,
    double InterceptionLiters,
    double OpenWaterEvaporationLiters,
    double SoilEvaporationLiters,
    double PlantTranspirationLiters,
    double WaterOutflowLiters,
    double PhotosynthesisDryGrams,
    double AnimalRespirationDryGrams,
    double AnimalExcretionWaterLiters,
    double DetritusRespirationDryGrams,
    double SoilOrganicMatterRespirationDryGrams);

public sealed class SimulationState
{
    public const int CurrentSchemaVersion = 3;
    public const int Width = 64;
    public const int Height = 64;
    public const double CellAreaM2 = 100.0;
    public const long TicksPerHour = 6;
    public const long TicksPerDay = 144;

    public ulong Seed { get; set; }
    public long TickIndex { get; set; }
    public WorldCell[] Cells { get; set; } = [];
    public AnimalAgentV1[] Animals { get; set; } = [];
    public ulong NextAnimalId { get; set; } = 1;
    public int[] ClimateHoursSinceRain { get; set; } = new int[16];
    public ClimateHour[] CurrentDayWeather { get; set; } = [];
    public int CurrentWeatherDay { get; set; } = -1;
    public FluxLedgerState Ledger { get; set; } = new();
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
}

public sealed class WorldSnapshot
{
    private readonly IReadOnlyList<WorldCell> _cells;
    private readonly IReadOnlyList<AnimalAgentV1> _animals;

    public WorldSnapshot(ulong seed, long tickIndex, WorldCell[] cells, AnimalAgentV1[] animals, FluxLedgerState ledger)
    {
        Seed = seed;
        TickIndex = tickIndex;
        _cells = Array.AsReadOnly((WorldCell[])cells.Clone());
        _animals = Array.AsReadOnly((AnimalAgentV1[])animals.Clone());
        Ledger = ledger.Snapshot();
    }

    public ulong Seed { get; }
    public long TickIndex { get; }
    public IReadOnlyList<WorldCell> Cells => _cells;
    public IReadOnlyList<AnimalAgentV1> Animals => _animals;
    public FluxLedgerSnapshot Ledger { get; }
    public int Width => SimulationState.Width;
    public int Height => SimulationState.Height;
    public int DayOfYear => (int)((TickIndex / SimulationState.TicksPerDay) % 365) + 1;
    public Season Season => Calendar.SeasonForDay(DayOfYear);
}

public static class Calendar
{
    public static Season SeasonForDay(int dayOfYear) => dayOfYear switch
    {
        >= 80 and < 172 => Season.Spring,
        >= 172 and < 264 => Season.Summer,
        >= 264 and < 355 => Season.Autumn,
        _ => Season.Winter
    };

    public static long NextSeasonStartTick(long tickIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tickIndex);

        var currentDayNumber = tickIndex / SimulationState.TicksPerDay;
        var currentSeason = SeasonForDay((int)(currentDayNumber % 365) + 1);
        for (var dayOffset = 1; dayOffset <= 365; dayOffset++)
        {
            var targetDayNumber = currentDayNumber + dayOffset;
            var targetDayOfYear = (int)(targetDayNumber % 365) + 1;
            if (SeasonForDay(targetDayOfYear) == currentSeason) continue;
            return checked(targetDayNumber * SimulationState.TicksPerDay);
        }

        throw new InvalidOperationException("No season boundary found in the calendar year.");
    }
}
