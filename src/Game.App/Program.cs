using Game.Core;
using Game.Contracts;
using Game.Modules.Atmosphere;
using Game.Modules.Biosphere;
using Game.Modules.Cosmos;
using Game.Modules.Geosphere;
using Game.Modules.Hydrosphere;
using System.Globalization;

ulong seed = 1;
var ticks = 1000;
string? loadPath = null;
string? savePath = null;
var n3Scenario = false;
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--seed" when i + 1 < args.Length: seed = ulong.Parse(args[++i]); break;
        case "--ticks" when i + 1 < args.Length: ticks = int.Parse(args[++i]); break;
        case "--load" when i + 1 < args.Length: loadPath = args[++i]; break;
        case "--save" when i + 1 < args.Length: savePath = args[++i]; break;
        case "--n3-scenario": n3Scenario = true; break;
        case "--help":
            Console.WriteLine("Usage: Game.App [--seed N] [--ticks N] [--n3-scenario] [--load save.json] [--save save.json]");
            return;
        default: throw new ArgumentException($"Unknown or incomplete argument: {args[i]}");
    }
}
if (n3Scenario && loadPath is not null)
    throw new ArgumentException("--n3-scenario can only be used when creating a new world.");

var cosmos = new CosmosModuleV1();
var atmosphere = new AtmosphereModuleV1();
var geosphere = new GeosphereModuleV1();
var hydrosphere = new HydrosphereModuleV1();
var biosphere = new BiosphereModuleV1();
var world = loadPath is null
    ? WorldSimulation.Create(seed, cosmos, atmosphere, atmosphere, geosphere, geosphere, geosphere, hydrosphere,
        biosphere, biosphere, biosphere,
        n3Scenario ? new InitialAnimalPopulationV1(40, 4) : default)
    : WorldSimulation.Load(File.ReadAllText(loadPath), cosmos, atmosphere, atmosphere,
        geosphere, geosphere, geosphere, hydrosphere, biosphere, biosphere, biosphere);
var initialInventory = world.Inventory();
var initialLedger = world.Snapshot().Ledger;
var simulationTimer = System.Diagnostics.Stopwatch.StartNew();
world.AdvanceTicks(ticks);
simulationTimer.Stop();
var simulationSeconds = simulationTimer.Elapsed.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture);
if (savePath is not null) File.WriteAllText(savePath, world.SaveToJson());
var snapshot = world.Snapshot();
var inventory = world.Inventory();
var ledger = snapshot.Ledger;
var scenarioName = n3Scenario ? "temperate-valley-n3-v1" : loadPath is not null ? "loaded-save" : "temperate-valley-l0-v17";
var waterResidual = inventory.WaterLiters - initialInventory.WaterLiters
    - (ledger.PrecipitationLiters - initialLedger.PrecipitationLiters)
    + (ledger.InterceptionLiters - initialLedger.InterceptionLiters)
    + (ledger.OpenWaterEvaporationLiters - initialLedger.OpenWaterEvaporationLiters)
    + (ledger.SoilEvaporationLiters - initialLedger.SoilEvaporationLiters)
    + (ledger.PlantTranspirationLiters - initialLedger.PlantTranspirationLiters)
    + (ledger.AnimalExcretionWaterLiters - initialLedger.AnimalExcretionWaterLiters)
    + (ledger.WaterOutflowLiters - initialLedger.WaterOutflowLiters);
var dryResidual = inventory.DryMatterGrams - initialInventory.DryMatterGrams
    - (ledger.PhotosynthesisDryGrams - initialLedger.PhotosynthesisDryGrams)
    + (ledger.AnimalRespirationDryGrams - initialLedger.AnimalRespirationDryGrams)
    + (ledger.DetritusRespirationDryGrams - initialLedger.DetritusRespirationDryGrams)
    + (ledger.SoilOrganicMatterRespirationDryGrams - initialLedger.SoilOrganicMatterRespirationDryGrams);
var nitrogenResidual = inventory.NitrogenGrams - initialInventory.NitrogenGrams;
var grass = snapshot.Cells.Count(c => c.Grass.Present);
var woody = snapshot.Cells.Count(c => c.Woody.Present);
var predators = snapshot.Animals.Where(animal => animal.FunctionalProfileId == AnimalFunctionalProfileV1.Predator).ToArray();
var predatorsWithCaptureHistory = predators.Count(animal => animal.LastCaptureDay >= 0);
var latestCaptureDay = predators.Length == 0 ? -1 : predators.Max(animal => animal.LastCaptureDay);
var detritusDryGrams = snapshot.Cells.Sum(cell => cell.DetritusDryGPerM2 * SimulationState.CellAreaM2);
var detritusNitrogenGrams = snapshot.Cells.Sum(cell => cell.DetritusNitrogenGPerM2 * SimulationState.CellAreaM2);
var wetCells = snapshot.Cells.Count(c => c.SurfaceWaterMm > 0);
var wetBands = new[] { 20, 100, 250, 500, 1_000 }
    .Select(threshold => $">={threshold}:{snapshot.Cells.Count(c => c.SurfaceWaterMm >= threshold)}");
var minimumElevation = snapshot.Cells.Min(c => c.ElevationM);
var maximumElevation = snapshot.Cells.Max(c => c.ElevationM);
var maximumSurfaceWater = snapshot.Cells.Max(c => c.SurfaceWaterMm);
Console.WriteLine($"scenario={scenarioName} seed={snapshot.Seed} tick={snapshot.TickIndex} day={snapshot.DayOfYear}");
Console.WriteLine($"simulation-seconds={simulationSeconds}");
Console.WriteLine($"cells={snapshot.Cells.Count} grass-patches={grass} woody-patches={woody}");
Console.WriteLine($"animals={snapshot.Animals.Count} herbivores={snapshot.Animals.Count(a => a.FunctionalProfileId == AnimalFunctionalProfileV1.Herbivore)} predators={snapshot.Animals.Count(a => a.FunctionalProfileId == AnimalFunctionalProfileV1.Predator)}");
Console.WriteLine($"predators-with-capture-history={predatorsWithCaptureHistory} latest-capture-day={latestCaptureDay}");
Console.WriteLine($"detritus-dry-matter-g={detritusDryGrams:F6} detritus-nitrogen-g={detritusNitrogenGrams:F6}");
Console.WriteLine($"elevation-range-m={minimumElevation:F1}..{maximumElevation:F1} wet-cells={wetCells} max-surface-water-mm={maximumSurfaceWater:F1}");
Console.WriteLine($"water-depth-bands-mm={string.Join(' ', wetBands)}");
Console.WriteLine($"water-L={inventory.WaterLiters:F6} dry-matter-g={inventory.DryMatterGrams:F6} nitrogen-g={inventory.NitrogenGrams:F6}");
Console.WriteLine($"balance-error water-L={waterResidual:E3} dry-matter-g={dryResidual:E3} nitrogen-g={nitrogenResidual:E3}");
Console.WriteLine($"state-sha256={world.CanonicalHash()}");
