using Game.Contracts;

namespace Game.Modules.Biosphere;

public readonly record struct DailyEcologyResult(
    WorldCell Cell, double DetritusRespirationGPerM2, double SoilOrganicMatterRespirationGPerM2);

public static class EcologyModel
{
    public static DailyEcologyResult CloseDay(WorldCell cell, double meanAirTemperatureC)
    {
        var grass = ClosePlantDay(cell.Grass, cell.GrassWaterFactorSum / 24,
            cell.GrassNitrogenFactorSum / 24, cell.GrassTemperatureFactorSum / 24,
            cell.GrassOutOfRangeHours / 24, 600);
        var woody = ClosePlantDay(cell.Woody, cell.WoodyWaterFactorSum / 24,
            cell.WoodyNitrogenFactorSum / 24, cell.WoodyTemperatureFactorSum / 24,
            cell.WoodyOutOfRangeHours / 24, 1800);

        var deadDry = grass.DeadDry + woody.DeadDry;
        var deadN = grass.DeadN + woody.DeadN;
        var soil = cell with
        {
            Grass = grass.Patch,
            Woody = woody.Patch,
            DetritusDryGPerM2 = cell.DetritusDryGPerM2 + deadDry,
            DetritusNitrogenGPerM2 = cell.DetritusNitrogenGPerM2 + deadN
        };

        var fTemperature = Math.Clamp(Math.Pow(2, (meanAirTemperatureC - 20) / 10), 0.25, 2.50);
        var fWater = Math.Clamp((soil.SoilWaterMm - soil.SoilWiltMm)
            / (0.5 * (soil.SoilCapacityMm - soil.SoilWiltMm)), 0, 1);
        var litterRate = 0.008 * fTemperature * fWater;
        var litterFraction = 1 - Math.Exp(-litterRate);
        var decayedDry = soil.DetritusDryGPerM2 * litterFraction;
        var decayedN = soil.DetritusNitrogenGPerM2 * litterFraction;
        var soilMatterRate = 0.0004 * fTemperature * fWater;
        var soilMatterFraction = 1 - Math.Exp(-soilMatterRate);
        var soilMatterLoss = soil.SoilOrganicMatterGPerM2 * soilMatterFraction;
        var soilNitrogenRelease = soil.SoilOrganicNitrogenGPerM2 * soilMatterFraction;

        var result = soil with
        {
            DetritusDryGPerM2 = soil.DetritusDryGPerM2 - decayedDry,
            DetritusNitrogenGPerM2 = soil.DetritusNitrogenGPerM2 - decayedN,
            SoilOrganicMatterGPerM2 = soil.SoilOrganicMatterGPerM2 + 0.25 * decayedDry - soilMatterLoss,
            SoilOrganicNitrogenGPerM2 = soil.SoilOrganicNitrogenGPerM2 + 0.70 * decayedN - soilNitrogenRelease,
            MineralNitrogenGPerM2 = soil.MineralNitrogenGPerM2 + 0.30 * decayedN + soilNitrogenRelease,
            GrassWaterFactorSum = 0,
            GrassNitrogenFactorSum = 0,
            GrassTemperatureFactorSum = 0,
            GrassOutOfRangeHours = 0,
            WoodyWaterFactorSum = 0,
            WoodyNitrogenFactorSum = 0,
            WoodyTemperatureFactorSum = 0,
            WoodyOutOfRangeHours = 0
        };

        return new(result, 0.75 * decayedDry, soilMatterLoss);
    }

    private static (PlantPatch Patch, double DeadDry, double DeadN) ClosePlantDay(
        PlantPatch patch, double meanWaterFactor, double meanNitrogenFactor,
        double meanTemperatureFactor, double outOfRangeFraction, double maxBiomass)
    {
        if (!patch.Present) return (patch, 0, 0);
        var germinationFraction = 1 - Math.Exp(-0.005 * meanTemperatureFactor * meanWaterFactor);
        var germination = Math.Min(patch.GerminationEligibleGPerM2 * germinationFraction,
            Math.Max(0, maxBiomass - patch.BiomassGPerM2));
        var eligibleRatio = patch.GerminationEligibleGPerM2 <= 0 ? 0
            : patch.GerminationEligibleNGPerM2 / patch.GerminationEligibleGPerM2;
        patch = patch with
        {
            BiomassGPerM2 = patch.BiomassGPerM2 + germination,
            NitrogenGPerM2 = patch.NitrogenGPerM2 + germination * eligibleRatio,
            SeedBankGPerM2 = patch.SeedBankGPerM2 - germination,
            SeedBankNitrogenGPerM2 = patch.SeedBankNitrogenGPerM2 - germination * eligibleRatio
        };

        var stressWater = Math.Max(0, 0.25 - meanWaterFactor) / 0.25;
        var stressNitrogen = Math.Max(0, 0.25 - meanNitrogenFactor) / 0.25;
        var mortalityRate = 0.001 + 0.03 * stressWater + 0.03 * stressNitrogen + 0.05 * outOfRangeFraction;
        var deadFraction = 1 - Math.Exp(-mortalityRate);
        var death = patch.BiomassGPerM2 * deadFraction;
        var afterDeath = patch.BiomassGPerM2 - death;
        var senescence = afterDeath * (1 - Math.Exp(-0.0005));
        var removed = death + senescence;
        var biomassBefore = patch.BiomassGPerM2;
        var nitrogenRemoved = biomassBefore <= 0 ? 0 : patch.NitrogenGPerM2 * (removed / biomassBefore);
        var viabilityFraction = 1 - Math.Exp(-0.001);
        var seedLoss = patch.SeedBankGPerM2 * viabilityFraction;
        var seedLossN = patch.SeedBankNitrogenGPerM2 * viabilityFraction;
        var updated = patch with
        {
            BiomassGPerM2 = Math.Max(0, biomassBefore - removed),
            NitrogenGPerM2 = Math.Max(0, patch.NitrogenGPerM2 - nitrogenRemoved),
            SeedBankGPerM2 = Math.Max(0, patch.SeedBankGPerM2 - seedLoss),
            SeedBankNitrogenGPerM2 = Math.Max(0, patch.SeedBankNitrogenGPerM2 - seedLossN),
            GerminationEligibleGPerM2 = Math.Max(0, patch.SeedBankGPerM2 - germination - seedLoss),
            GerminationEligibleNGPerM2 = Math.Max(0, patch.SeedBankNitrogenGPerM2 - germination * eligibleRatio - seedLossN)
        };
        return (updated, removed + seedLoss, nitrogenRemoved + seedLossN);
    }
}
