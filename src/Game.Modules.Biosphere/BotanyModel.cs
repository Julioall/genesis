using Game.Contracts;
using Game.Procedural;

namespace Game.Modules.Biosphere;

public static class BotanyModel
{
    private const double GrassBmax = 600, GrassB50 = 200, GrassRue = 0.80, GrassQN = 0.018, GrassKN = 0.50;
    private const double WoodyBmax = 1800, WoodyB50 = 500, WoodyRue = 0.55, WoodyQN = 0.012, WoodyKN = 0.30;
    private static readonly int[] SeedNeighborIndices = CreateSeedNeighborIndices();
    private static readonly byte[] SeedNeighborCounts = CreateSeedNeighborCounts();

    public static double Cover(PlantPatch patch, PlantKind kind)
    {
        if (patch.BiomassGPerM2 <= 0) return 0;
        var b50 = kind == PlantKind.Grass ? GrassB50 : WoodyB50;
        return 1 - Math.Exp(-patch.BiomassGPerM2 / b50);
    }

    public static double TotalCover(PlantPatchPair plants)
        => CoverComponents(plants).Total;

    public static PlantCoverV1 CoverComponents(PlantPatchPair plants)
        => new(Cover(plants.Grass, PlantKind.Grass), Cover(plants.Woody, PlantKind.Woody));

    public static PlantPatchPair[] CreateInitialPatches(
        ulong seed,
        double[] elevationsM,
        double[] surfaceWaterMm,
        double[] terrainSlopeMagnitude)
    {
        ArgumentNullException.ThrowIfNull(elevationsM);
        ArgumentNullException.ThrowIfNull(surfaceWaterMm);
        ArgumentNullException.ThrowIfNull(terrainSlopeMagnitude);
        var cellCount = SimulationState.Width * SimulationState.Height;
        if (elevationsM.Length != cellCount || surfaceWaterMm.Length != cellCount
            || terrainSlopeMagnitude.Length != cellCount)
            throw new ArgumentException("Initial plant placement fields must match the L0 world grid.");

        var grassPresence = CreateClusteredPlantPlacements(seed, elevationsM, surfaceWaterMm,
            terrainSlopeMagnitude, 1, [0.55, 0.38, 0.12], 1.0, 150);
        var woodyPresence = CreateClusteredPlantPlacements(seed, elevationsM, surfaceWaterMm,
            terrainSlopeMagnitude, 2, [0.30, 0.62, 0.40], 1.0, 0);
        var initialPatches = new PlantPatchPair[cellCount];
        for (var i = 0; i < cellCount; i++)
        {
            var grass = grassPresence[i] ? new PlantPatch(true, 100, 1.8, 0, 0, 0, 0) : default;
            var woody = woodyPresence[i] ? new PlantPatch(true, 250, 3.0, 0, 0, 0, 0) : default;
            initialPatches[i] = new PlantPatchPair(grass, woody);
        }
        return initialPatches;
    }

    private static bool[] CreateClusteredPlantPlacements(
        ulong seed,
        double[] elevationsM,
        double[] surfaceWaterMm,
        double[] terrainSlopeMagnitude,
        ulong profileId,
        double[] elevationFractions,
        double maximumSlope,
        double maximumSurfaceWaterMm)
    {
        if (elevationFractions.Length != 3 || elevationFractions.Any(fraction => fraction is < 0 or > 1))
            throw new ArgumentException("Plant placement needs lowland, upland, and alpine fractions.", nameof(elevationFractions));
        if (!double.IsFinite(maximumSlope) || maximumSlope < 0
            || !double.IsFinite(maximumSurfaceWaterMm) || maximumSurfaceWaterMm < 0)
            throw new ArgumentException("Plant placement limits must be finite and non-negative.");

        var scores = new double[elevationsM.Length];
        var candidates = new[] { new List<int>(), new List<int>(), new List<int>() };
        for (var index = 0; index < elevationsM.Length; index++)
        {
            var x = index % SimulationState.Width;
            var y = index / SimulationState.Width;
            var broadSpacing = profileId == 2 ? 12 : 10;
            const int regionalSpacing = 4;
            var broad = CounterRandom.SpatialUnit(seed, CounterRandom.PlantPlacementTag, x, y, broadSpacing, profileId, 1);
            var regional = CounterRandom.SpatialUnit(seed, CounterRandom.PlantPlacementTag, x, y, regionalSpacing, profileId, 2);
            var local = CounterRandom.Unit(seed, CounterRandom.PlantPlacementTag, (ulong)index, profileId, 0, 0);
            scores[index] = profileId == 2
                ? broad * 0.52 + regional * 0.32 + local * 0.16
                : broad * 0.58 + regional * 0.27 + local * 0.15;
            if (surfaceWaterMm[index] > maximumSurfaceWaterMm
                || terrainSlopeMagnitude[index] > maximumSlope) continue;
            var zone = elevationsM[index] < 75 ? 0 : elevationsM[index] < 125 ? 1 : 2;
            candidates[zone].Add(index);
        }

        var present = new bool[elevationsM.Length];
        for (var zone = 0; zone < candidates.Length; zone++) Select(candidates[zone], elevationFractions[zone]);
        return present;

        void Select(List<int> zoneCandidates, double fraction)
        {
            var targetCount = (int)Math.Round(zoneCandidates.Count * fraction, MidpointRounding.AwayFromZero);
            foreach (var index in zoneCandidates.OrderByDescending(candidate => scores[candidate])
                         .ThenBy(candidate => candidate).Take(targetCount))
                present[index] = true;
        }
    }

    public static BotanyCellStateV1[] DisperseSeeds(
        BotanyCellStateV1[] states, BotanyHourResultV1[] hourlyResults)
    {
        if (states.Length != hourlyResults.Length)
            throw new ArgumentException("Botany state and hourly result fields must have matching dimensions.");

        var anySeeds = false;
        for (var i = 0; i < hourlyResults.Length; i++)
        {
            if (hourlyResults[i].GrassSeedProducedGPerM2 != 0
                || hourlyResults[i].WoodySeedProducedGPerM2 != 0)
            {
                anySeeds = true;
                break;
            }
        }
        if (!anySeeds) return states;

        var result = (BotanyCellStateV1[])states.Clone();
        var grassMatter = new double[states.Length];
        var grassNitrogen = new double[states.Length];
        var woodyMatter = new double[states.Length];
        var woodyNitrogen = new double[states.Length];
        for (var i = 0; i < hourlyResults.Length; i++)
        {
            var grassSeed = hourlyResults[i].GrassSeedProducedGPerM2 * 0.20;
            var woodySeed = hourlyResults[i].WoodySeedProducedGPerM2 * 0.20;
            if (grassSeed == 0 && woodySeed == 0) continue;
            var plants = result[i].Plants;
            if (grassSeed != 0)
            {
                var grass = plants.Grass with
                {
                    SeedBankGPerM2 = plants.Grass.SeedBankGPerM2 - grassSeed,
                    SeedBankNitrogenGPerM2 = plants.Grass.SeedBankNitrogenGPerM2 - grassSeed * GrassQN
                };
                plants = plants with { Grass = grass };
                SpreadFrom(i, grassSeed, grassSeed * GrassQN, grassMatter, grassNitrogen);
            }
            if (woodySeed != 0)
            {
                var woody = plants.Woody with
                {
                    SeedBankGPerM2 = plants.Woody.SeedBankGPerM2 - woodySeed,
                    SeedBankNitrogenGPerM2 = plants.Woody.SeedBankNitrogenGPerM2 - woodySeed * WoodyQN
                };
                plants = plants with { Woody = woody };
                SpreadFrom(i, woodySeed, woodySeed * WoodyQN, woodyMatter, woodyNitrogen);
            }
            result[i] = result[i] with { Plants = plants };
        }

        ApplyReceipts(result, grassMatter, grassNitrogen, PlantKind.Grass);
        ApplyReceipts(result, woodyMatter, woodyNitrogen, PlantKind.Woody);
        return result;
    }

    private static void SpreadFrom(int origin, double matter, double nitrogen, double[] matterByCell, double[] nitrogenByCell)
    {
        if (matter <= 0) return;
        var count = SeedNeighborCounts[origin];
        if (count == 0) return;
        var neighborOffset = origin * 8;
        for (var n = 0; n < count; n++)
        {
            var index = SeedNeighborIndices[neighborOffset + n];
            matterByCell[index] += matter / count;
            nitrogenByCell[index] += nitrogen / count;
        }
    }

    private static int[] CreateSeedNeighborIndices()
    {
        var neighbors = new int[SimulationState.Width * SimulationState.Height * 8];
        var cellCount = SimulationState.Width * SimulationState.Height;
        for (var origin = 0; origin < cellCount; origin++)
        {
            var x = origin % SimulationState.Width;
            var y = origin / SimulationState.Width;
            var offset = origin * 8;
            var count = 0;
            for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                var nx = x + dx;
                var ny = y + dy;
                if (nx < 0 || nx >= SimulationState.Width || ny < 0 || ny >= SimulationState.Height) continue;
                neighbors[offset + count++] = ny * SimulationState.Width + nx;
            }
        }
        return neighbors;
    }

    private static byte[] CreateSeedNeighborCounts()
    {
        var counts = new byte[SimulationState.Width * SimulationState.Height];
        for (var origin = 0; origin < counts.Length; origin++)
        {
            var x = origin % SimulationState.Width;
            var y = origin / SimulationState.Width;
            counts[origin] = (byte)((x == 0 || x == SimulationState.Width - 1 ? 5 : 8)
                - (y == 0 || y == SimulationState.Height - 1 ? (x == 0 || x == SimulationState.Width - 1 ? 2 : 3) : 0));
        }
        return counts;
    }

    private static void ApplyReceipts(BotanyCellStateV1[] states, double[] matter, double[] nitrogen, PlantKind kind)
    {
        for (var i = 0; i < states.Length; i++)
        {
            if (matter[i] <= 0) continue;
            var plants = states[i].Plants;
            if (kind == PlantKind.Grass)
            {
                var patch = plants.Grass;
                states[i] = states[i] with { Plants = plants with { Grass = patch with
                {
                    Present = true,
                    SeedBankGPerM2 = patch.SeedBankGPerM2 + matter[i],
                    SeedBankNitrogenGPerM2 = patch.SeedBankNitrogenGPerM2 + nitrogen[i]
                } } };
            }
            else
            {
                var patch = plants.Woody;
                states[i] = states[i] with { Plants = plants with { Woody = patch with
                {
                    Present = true,
                    SeedBankGPerM2 = patch.SeedBankGPerM2 + matter[i],
                    SeedBankNitrogenGPerM2 = patch.SeedBankNitrogenGPerM2 + nitrogen[i]
                } } };
            }
        }
    }

    public static BotanyHourResultV1 GrowHour(BotanyHourInputV1 input)
        => GrowHour(input, CoverComponents(input.State.Plants));

    public static BotanyHourResultV1 GrowHour(BotanyHourInputV1 input, PlantCoverV1 cover)
    {
        if (!double.IsFinite(cover.Grass) || cover.Grass is < 0 or > 1
            || !double.IsFinite(cover.Woody) || cover.Woody is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(cover), "Plant covers must be finite fractions in [0, 1].");
        var plants = input.State.Plants;
        var factors = input.State.DailyFactors;
        var grassCover = cover.Grass;
        var woodyCover = cover.Woody;
        var grassDemand = input.PotentialEtMm * grassCover * (1 - woodyCover) * input.PlantWaterDemandMultiplier;
        var woodyDemand = input.PotentialEtMm * woodyCover * input.PlantWaterDemandMultiplier;
        var totalDemand = grassDemand + woodyDemand;
        var waterGrantScale = totalDemand <= 0 ? 1 : Math.Min(1, input.AvailableWaterMm / totalDemand);
        var grassWaterGrant = grassDemand * waterGrantScale;
        var woodyWaterGrant = woodyDemand * waterGrantScale;
        var grassWaterGrantFactor = grassDemand == 0 ? 1 : Math.Min(1, grassWaterGrant / grassDemand);
        var woodyWaterGrantFactor = woodyDemand == 0 ? 1 : Math.Min(1, woodyWaterGrant / woodyDemand);

        var grassFTemp = TemperatureSuitability(input.AirTemperatureC, -2, 20, 38);
        var woodyFTemp = TemperatureSuitability(input.AirTemperatureC, -10, 18, 36);
        var nitrogenAvailability = input.MineralNitrogenGPerM2 <= 0 ? 0
            : input.MineralNitrogenGPerM2 / (input.MineralNitrogenGPerM2 + GrassKN);
        var woodyNitrogenAvailability = input.MineralNitrogenGPerM2 <= 0 ? 0
            : input.MineralNitrogenGPerM2 / (input.MineralNitrogenGPerM2 + WoodyKN);
        var availableFactor = Math.Clamp(input.WaterAvailabilityFactor, 0, 1);
        var grassFW = Math.Min(availableFactor, grassWaterGrantFactor);
        var woodyFW = Math.Min(availableFactor, woodyWaterGrantFactor);
        var grassFn = Math.Clamp(nitrogenAvailability, 0, 1);
        var woodyFn = Math.Clamp(woodyNitrogenAvailability, 0, 1);

        var parWoody = 0.48 * input.SurfaceShortwaveMjPerM2 * woodyCover;
        var parGrass = 0.48 * input.SurfaceShortwaveMjPerM2 * grassCover * (1 - woodyCover);
        var grassPotential = plants.Grass.Present
            ? Math.Min(GrassBmax - plants.Grass.BiomassGPerM2,
                GrassRue * parGrass * grassFTemp * grassFW * Math.Clamp(1 - plants.Grass.BiomassGPerM2 / GrassBmax, 0, 1))
            : 0;
        var woodyPotential = plants.Woody.Present
            ? Math.Min(WoodyBmax - plants.Woody.BiomassGPerM2,
                WoodyRue * parWoody * woodyFTemp * woodyFW * Math.Clamp(1 - plants.Woody.BiomassGPerM2 / WoodyBmax, 0, 1))
            : 0;
        var grassNRequest = GrassQN * grassPotential * grassFn;
        var woodyNRequest = WoodyQN * woodyPotential * woodyFn;
        var nRequest = grassNRequest + woodyNRequest;
        var nGrantScale = nRequest <= 0 ? 0 : Math.Min(1, input.MineralNitrogenGPerM2 / nRequest);
        var grassNGrant = grassNRequest * nGrantScale;
        var woodyNGrant = woodyNRequest * nGrantScale;
        var grassGrowth = Math.Min(grassPotential * grassFn, grassNGrant / GrassQN);
        var woodyGrowth = Math.Min(woodyPotential * woodyFn, woodyNGrant / WoodyQN);
        var grassUsedN = GrassQN * grassGrowth;
        var woodyUsedN = WoodyQN * woodyGrowth;
        var returnedN = Math.Max(0, grassNGrant - grassUsedN) + Math.Max(0, woodyNGrant - woodyUsedN);
        var mineralN = input.MineralNitrogenGPerM2 - grassNGrant - woodyNGrant + returnedN;

        var grass = AddProduction(plants.Grass, grassGrowth, 0.02, GrassQN);
        var woody = AddProduction(plants.Woody, woodyGrowth, 0.01, WoodyQN);
        var grassOut = grassFTemp == 0 ? 1 : 0;
        var woodyOut = woodyFTemp == 0 ? 1 : 0;
        var transpiration = grassWaterGrant + woodyWaterGrant;
        var updated = input.State with
        {
            Plants = new PlantPatchPair(grass, woody),
            DailyFactors = new BotanyDailyFactorsV1(
                factors.GrassWaterFactorSum + (plants.Grass.Present ? grassFW : 0),
                factors.GrassNitrogenFactorSum + (plants.Grass.Present ? grassFn : 0),
                factors.GrassTemperatureFactorSum + (plants.Grass.Present ? grassFTemp : 0),
                factors.GrassOutOfRangeHours + (plants.Grass.Present ? grassOut : 0),
                factors.WoodyWaterFactorSum + (plants.Woody.Present ? woodyFW : 0),
                factors.WoodyNitrogenFactorSum + (plants.Woody.Present ? woodyFn : 0),
                factors.WoodyTemperatureFactorSum + (plants.Woody.Present ? woodyFTemp : 0),
                factors.WoodyOutOfRangeHours + (plants.Woody.Present ? woodyOut : 0))
        };

        return new(updated, transpiration, mineralN, grassGrowth + woodyGrowth,
            grassGrowth * 0.02, woodyGrowth * 0.01);
    }

    public static HerbivoryResolutionV1 ResolveHerbivory(
        PlantPatch patch,
        PlantKind kind,
        HerbivoryRequestV1[] requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        if (kind is not (PlantKind.Grass or PlantKind.Woody))
            throw new ArgumentOutOfRangeException(nameof(kind));
        if (!double.IsFinite(patch.BiomassGPerM2) || patch.BiomassGPerM2 < 0
            || !double.IsFinite(patch.NitrogenGPerM2) || patch.NitrogenGPerM2 < 0)
            throw new ArgumentException("The plant patch has invalid biomass or nitrogen stocks.", nameof(patch));
        var ordered = requests.OrderBy(request => request.AnimalId).ToArray();
        if (ordered.Any(request => request.AnimalId == 0 || !double.IsFinite(request.RequestedDryG)
                || request.RequestedDryG < 0)
            || ordered.Select(request => request.AnimalId).Distinct().Count() != ordered.Length)
            throw new ArgumentException("Herbivory requests require unique animal IDs and finite non-negative amounts.", nameof(requests));
        var totalRequested = ordered.Sum(request => request.RequestedDryG);
        if (!double.IsFinite(totalRequested))
            throw new ArgumentException("Combined herbivory demand is not finite.", nameof(requests));
        var edibleFraction = kind == PlantKind.Grass ? 0.80 : 0.15;
        var biomassCellG = patch.BiomassGPerM2 * SimulationState.CellAreaM2;
        var nitrogenCellG = patch.NitrogenGPerM2 * SimulationState.CellAreaM2;
        var totalConsumed = Math.Min(totalRequested, biomassCellG * edibleFraction);
        var grantScale = totalRequested <= 0 ? 0 : totalConsumed / totalRequested;
        var nitrogenPerDryG = biomassCellG <= 0 ? 0 : nitrogenCellG / biomassCellG;
        var grants = ordered.Select(request => new HerbivoryGrantV1(
            request.AnimalId,
            request.RequestedDryG * grantScale,
            request.RequestedDryG * grantScale * nitrogenPerDryG)).ToArray();
        var consumedNitrogenG = totalConsumed * nitrogenPerDryG;
        var updatedPatch = patch with
        {
            BiomassGPerM2 = Math.Max(0, patch.BiomassGPerM2 - totalConsumed / SimulationState.CellAreaM2),
            NitrogenGPerM2 = Math.Max(0, patch.NitrogenGPerM2 - consumedNitrogenG / SimulationState.CellAreaM2)
        };
        return new(updatedPatch, grants);
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
