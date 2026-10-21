using Game.Core;

ulong seed = 1;
var ticks = 1000;
string? loadPath = null;
string? savePath = null;
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--seed" when i + 1 < args.Length: seed = ulong.Parse(args[++i]); break;
        case "--ticks" when i + 1 < args.Length: ticks = int.Parse(args[++i]); break;
        case "--load" when i + 1 < args.Length: loadPath = args[++i]; break;
        case "--save" when i + 1 < args.Length: savePath = args[++i]; break;
        case "--help":
            Console.WriteLine("Usage: Game.App [--seed N] [--ticks N] [--load save.json] [--save save.json]");
            return;
        default: throw new ArgumentException($"Unknown or incomplete argument: {args[i]}");
    }
}

var world = loadPath is null
    ? WorldSimulation.Create(seed)
    : WorldSimulation.Load(File.ReadAllText(loadPath));
var initialInventory = world.Inventory();
var initialLedger = world.Snapshot().Ledger;
world.AdvanceTicks(ticks);
if (savePath is not null) File.WriteAllText(savePath, world.SaveToJson());
var snapshot = world.Snapshot();
var inventory = world.Inventory();
var ledger = snapshot.Ledger;
var waterResidual = inventory.WaterLiters - initialInventory.WaterLiters
    - (ledger.PrecipitationLiters - initialLedger.PrecipitationLiters)
    + (ledger.InterceptionLiters - initialLedger.InterceptionLiters)
    + (ledger.OpenWaterEvaporationLiters - initialLedger.OpenWaterEvaporationLiters)
    + (ledger.SoilEvaporationLiters - initialLedger.SoilEvaporationLiters)
    + (ledger.PlantTranspirationLiters - initialLedger.PlantTranspirationLiters)
    + (ledger.WaterOutflowLiters - initialLedger.WaterOutflowLiters);
var dryResidual = inventory.DryMatterGrams - initialInventory.DryMatterGrams
    - (ledger.PhotosynthesisDryGrams - initialLedger.PhotosynthesisDryGrams)
    + (ledger.DetritusRespirationDryGrams - initialLedger.DetritusRespirationDryGrams)
    + (ledger.SoilOrganicMatterRespirationDryGrams - initialLedger.SoilOrganicMatterRespirationDryGrams);
var nitrogenResidual = inventory.NitrogenGrams - initialInventory.NitrogenGrams;
var grass = snapshot.Cells.Count(c => c.Grass.Present);
var woody = snapshot.Cells.Count(c => c.Woody.Present);
var wetCells = snapshot.Cells.Count(c => c.SurfaceWaterMm > 0);
var wetBands = new[] { 20, 100, 250, 500, 1_000 }
    .Select(threshold => $">={threshold}:{snapshot.Cells.Count(c => c.SurfaceWaterMm >= threshold)}");
var minimumElevation = snapshot.Cells.Min(c => c.ElevationM);
var maximumElevation = snapshot.Cells.Max(c => c.ElevationM);
var maximumSurfaceWater = snapshot.Cells.Max(c => c.SurfaceWaterMm);
Console.WriteLine($"scenario=temperate-valley-l0-v15 seed={snapshot.Seed} tick={snapshot.TickIndex} day={snapshot.DayOfYear}");
Console.WriteLine($"cells={snapshot.Cells.Count} grass-patches={grass} woody-patches={woody}");
Console.WriteLine($"elevation-range-m={minimumElevation:F1}..{maximumElevation:F1} wet-cells={wetCells} max-surface-water-mm={maximumSurfaceWater:F1}");
Console.WriteLine($"water-depth-bands-mm={string.Join(' ', wetBands)}");
Console.WriteLine($"water-L={inventory.WaterLiters:F6} dry-matter-g={inventory.DryMatterGrams:F6} nitrogen-g={inventory.NitrogenGrams:F6}");
Console.WriteLine($"balance-error water-L={waterResidual:E3} dry-matter-g={dryResidual:E3} nitrogen-g={nitrogenResidual:E3}");
Console.WriteLine($"state-sha256={world.CanonicalHash()}");
