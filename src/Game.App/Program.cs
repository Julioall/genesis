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
Console.WriteLine($"scenario=temperate-valley-l0-v2 seed={snapshot.Seed} tick={snapshot.TickIndex} day={snapshot.DayOfYear}");
Console.WriteLine($"cells={snapshot.Cells.Count} grass-patches={grass} woody-patches={woody}");
Console.WriteLine($"water-L={inventory.WaterLiters:F6} dry-matter-g={inventory.DryMatterGrams:F6} nitrogen-g={inventory.NitrogenGrams:F6}");
Console.WriteLine($"balance-error water-L={waterResidual:E3} dry-matter-g={dryResidual:E3} nitrogen-g={nitrogenResidual:E3}");
Console.WriteLine($"state-sha256={world.CanonicalHash()}");
