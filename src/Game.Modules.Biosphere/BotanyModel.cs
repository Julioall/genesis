using Game.Contracts;

namespace Game.Modules.Biosphere;

public readonly record struct BotanyHourResult(
    WorldCell Cell, double TranspirationMm, double NewBiomassGPerM2,
    double GrassSeedProducedGPerM2, double WoodySeedProducedGPerM2);

public static class BotanyModel
{
    private const double GrassBmax = 600, GrassB50 = 200, GrassRue = 0.80, GrassQN = 0.018, GrassKN = 0.50;
    private const double WoodyBmax = 1800, WoodyB50 = 500, WoodyRue = 0.55, WoodyQN = 0.012, WoodyKN = 0.30;

    public static double Cover(PlantPatch patch, PlantKind kind)
    {
        if (patch.BiomassGPerM2 <= 0) return 0;
        var b50 = kind == PlantKind.Grass ? GrassB50 : WoodyB50;
        return 1 - Math.Exp(-patch.BiomassGPerM2 / b50);
    }

    public static double TotalCover(WorldCell cell)
        => 1 - (1 - Cover(cell.Grass, PlantKind.Grass)) * (1 - Cover(cell.Woody, PlantKind.Woody));

    public static WorldCell[] DisperseSeeds(WorldCell[] cells, BotanyHourResult[] hourlyResults)
    {
        var result = (WorldCell[])cells.Clone();
        var grassMatter = new double[cells.Length];
        var grassNitrogen = new double[cells.Length];
        var woodyMatter = new double[cells.Length];
        var woodyNitrogen = new double[cells.Length];
        for (var i = 0; i < hourlyResults.Length; i++)
        {
            var grassSeed = hourlyResults[i].GrassSeedProducedGPerM2 * 0.20;
            var woodySeed = hourlyResults[i].WoodySeedProducedGPerM2 * 0.20;
            var grass = result[i].Grass with
            {
                SeedBankGPerM2 = result[i].Grass.SeedBankGPerM2 - grassSeed,
                SeedBankNitrogenGPerM2 = result[i].Grass.SeedBankNitrogenGPerM2 - grassSeed * GrassQN
            };
            var woody = result[i].Woody with
            {
                SeedBankGPerM2 = result[i].Woody.SeedBankGPerM2 - woodySeed,
                SeedBankNitrogenGPerM2 = result[i].Woody.SeedBankNitrogenGPerM2 - woodySeed * WoodyQN
            };
            result[i] = result[i] with { Grass = grass, Woody = woody };
            SpreadFrom(i, grassSeed, grassSeed * GrassQN, grassMatter, grassNitrogen);
            SpreadFrom(i, woodySeed, woodySeed * WoodyQN, woodyMatter, woodyNitrogen);
        }

        ApplyReceipts(result, grassMatter, grassNitrogen, PlantKind.Grass);
        ApplyReceipts(result, woodyMatter, woodyNitrogen, PlantKind.Woody);
        return result;
    }

    private static void SpreadFrom(int origin, double matter, double nitrogen, double[] matterByCell, double[] nitrogenByCell)
    {
        if (matter <= 0) return;
        var x = origin % SimulationState.Width;
        var y = origin / SimulationState.Width;
        Span<int> neighbors = stackalloc int[8];
        var count = 0;
        for (var dy = -1; dy <= 1; dy++)
        for (var dx = -1; dx <= 1; dx++)
        {
            if (dx == 0 && dy == 0) continue;
            var nx = x + dx;
            var ny = y + dy;
            if (nx < 0 || nx >= SimulationState.Width || ny < 0 || ny >= SimulationState.Height) continue;
            neighbors[count++] = ny * SimulationState.Width + nx;
        }
        if (count == 0) return;
        for (var n = 0; n < count; n++)
        {
            var index = neighbors[n];
            matterByCell[index] += matter / count;
            nitrogenByCell[index] += nitrogen / count;
        }
    }

    private static void ApplyReceipts(WorldCell[] cells, double[] matter, double[] nitrogen, PlantKind kind)
    {
        for (var i = 0; i < cells.Length; i++)
        {
            if (matter[i] <= 0) continue;
            if (kind == PlantKind.Grass)
            {
                var patch = cells[i].Grass;
                cells[i] = cells[i] with { Grass = patch with
                {
                    Present = true,
                    SeedBankGPerM2 = patch.SeedBankGPerM2 + matter[i],
                    SeedBankNitrogenGPerM2 = patch.SeedBankNitrogenGPerM2 + nitrogen[i]
                } };
            }
            else
            {
                var patch = cells[i].Woody;
                cells[i] = cells[i] with { Woody = patch with
                {
                    Present = true,
                    SeedBankGPerM2 = patch.SeedBankGPerM2 + matter[i],
                    SeedBankNitrogenGPerM2 = patch.SeedBankNitrogenGPerM2 + nitrogen[i]
                } };
            }
        }
    }

    public static BotanyHourResult GrowHour(
        WorldCell cell,
        double airTemperatureC,
        double surfaceShortwaveMjPerM2,
        double potentialEtMm,
        double availableWaterMm,
        double waterAvailabilityFactor,
        double plantWaterMultiplier)
    {
        var grassCover = Cover(cell.Grass, PlantKind.Grass);
        var woodyCover = Cover(cell.Woody, PlantKind.Woody);
        var waterCover = Math.Clamp(cell.SurfaceWaterMm / 10, 0, 1);
        var grassDemand = potentialEtMm * grassCover * (1 - woodyCover) * plantWaterMultiplier;
        var woodyDemand = potentialEtMm * woodyCover * plantWaterMultiplier;
        var totalDemand = grassDemand + woodyDemand;
        var waterGrantScale = totalDemand <= 0 ? 1 : Math.Min(1, availableWaterMm / totalDemand);
        var grassWaterGrant = grassDemand * waterGrantScale;
        var woodyWaterGrant = woodyDemand * waterGrantScale;
        var grassWaterGrantFactor = grassDemand == 0 ? 1 : Math.Min(1, grassWaterGrant / grassDemand);
        var woodyWaterGrantFactor = woodyDemand == 0 ? 1 : Math.Min(1, woodyWaterGrant / woodyDemand);

        var grassFTemp = TemperatureSuitability(airTemperatureC, -2, 20, 38);
        var woodyFTemp = TemperatureSuitability(airTemperatureC, -10, 18, 36);
        var nitrogenAvailability = cell.MineralNitrogenGPerM2 <= 0 ? 0
            : cell.MineralNitrogenGPerM2 / (cell.MineralNitrogenGPerM2 + GrassKN);
        var woodyNitrogenAvailability = cell.MineralNitrogenGPerM2 <= 0 ? 0
            : cell.MineralNitrogenGPerM2 / (cell.MineralNitrogenGPerM2 + WoodyKN);
        var availableFactor = Math.Clamp(waterAvailabilityFactor, 0, 1);
        var grassFW = Math.Min(availableFactor, grassWaterGrantFactor);
        var woodyFW = Math.Min(availableFactor, woodyWaterGrantFactor);
        var grassFn = Math.Clamp(nitrogenAvailability, 0, 1);
        var woodyFn = Math.Clamp(woodyNitrogenAvailability, 0, 1);

        var parWoody = 0.48 * surfaceShortwaveMjPerM2 * woodyCover;
        var parGrass = 0.48 * surfaceShortwaveMjPerM2 * grassCover * (1 - woodyCover);
        var grassPotential = cell.Grass.Present
            ? Math.Min(GrassBmax - cell.Grass.BiomassGPerM2,
                GrassRue * parGrass * grassFTemp * grassFW * Math.Clamp(1 - cell.Grass.BiomassGPerM2 / GrassBmax, 0, 1))
            : 0;
        var woodyPotential = cell.Woody.Present
            ? Math.Min(WoodyBmax - cell.Woody.BiomassGPerM2,
                WoodyRue * parWoody * woodyFTemp * woodyFW * Math.Clamp(1 - cell.Woody.BiomassGPerM2 / WoodyBmax, 0, 1))
            : 0;
        var grassNRequest = GrassQN * grassPotential * grassFn;
        var woodyNRequest = WoodyQN * woodyPotential * woodyFn;
        var nRequest = grassNRequest + woodyNRequest;
        var nGrantScale = nRequest <= 0 ? 0 : Math.Min(1, cell.MineralNitrogenGPerM2 / nRequest);
        var grassNGrant = grassNRequest * nGrantScale;
        var woodyNGrant = woodyNRequest * nGrantScale;
        var grassGrowth = Math.Min(grassPotential * grassFn, grassNGrant / GrassQN);
        var woodyGrowth = Math.Min(woodyPotential * woodyFn, woodyNGrant / WoodyQN);
        var grassUsedN = GrassQN * grassGrowth;
        var woodyUsedN = WoodyQN * woodyGrowth;
        var returnedN = Math.Max(0, grassNGrant - grassUsedN) + Math.Max(0, woodyNGrant - woodyUsedN);
        var mineralN = cell.MineralNitrogenGPerM2 - grassNGrant - woodyNGrant + returnedN;

        var grass = AddProduction(cell.Grass, grassGrowth, 0.02, GrassQN);
        var woody = AddProduction(cell.Woody, woodyGrowth, 0.01, WoodyQN);
        var grassOut = grassFTemp == 0 ? 1 : 0;
        var woodyOut = woodyFTemp == 0 ? 1 : 0;
        var transpiration = grassWaterGrant + woodyWaterGrant;
        var updated = cell with
        {
            SoilWaterMm = Math.Max(cell.SoilWiltMm, cell.SoilWaterMm - transpiration),
            MineralNitrogenGPerM2 = mineralN,
            Grass = grass,
            Woody = woody,
            GrassWaterFactorSum = cell.GrassWaterFactorSum + (cell.Grass.Present ? grassFW : 0),
            GrassNitrogenFactorSum = cell.GrassNitrogenFactorSum + (cell.Grass.Present ? grassFn : 0),
            GrassTemperatureFactorSum = cell.GrassTemperatureFactorSum + (cell.Grass.Present ? grassFTemp : 0),
            GrassOutOfRangeHours = cell.GrassOutOfRangeHours + (cell.Grass.Present ? grassOut : 0),
            WoodyWaterFactorSum = cell.WoodyWaterFactorSum + (cell.Woody.Present ? woodyFW : 0),
            WoodyNitrogenFactorSum = cell.WoodyNitrogenFactorSum + (cell.Woody.Present ? woodyFn : 0),
            WoodyTemperatureFactorSum = cell.WoodyTemperatureFactorSum + (cell.Woody.Present ? woodyFTemp : 0),
            WoodyOutOfRangeHours = cell.WoodyOutOfRangeHours + (cell.Woody.Present ? woodyOut : 0)
        };

        return new(updated, transpiration, grassGrowth + woodyGrowth,
            grassGrowth * 0.02, woodyGrowth * 0.01);
    }

    private static PlantPatch AddProduction(PlantPatch patch, double growth, double seedFraction, double qN)
    {
        if (!patch.Present) return patch;
        var seedMatter = growth * seedFraction;
        var seedN = qN * seedMatter;
        return patch with
        {
            BiomassGPerM2 = patch.BiomassGPerM2 + growth - seedMatter,
            NitrogenGPerM2 = patch.NitrogenGPerM2 + qN * (growth - seedMatter),
            SeedBankGPerM2 = patch.SeedBankGPerM2 + seedMatter,
            SeedBankNitrogenGPerM2 = patch.SeedBankNitrogenGPerM2 + seedN
        };
    }

    public static double TemperatureSuitability(double temperature, double minimum, double optimum, double maximum)
    {
        if (temperature <= minimum || temperature >= maximum) return 0;
        return temperature <= optimum
            ? (temperature - minimum) / (optimum - minimum)
            : (maximum - temperature) / (maximum - optimum);
    }
}
