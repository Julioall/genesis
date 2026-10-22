using Game.Contracts;

namespace Game.Modules.Biosphere;

public static class EcologyModel
{
    public static EcologyDetritusStateV1 ConfirmDetritusDeposit(
        EcologyDetritusStateV1 current,
        EcologyDetritusStateV1 proposedDeposit)
    {
        if (!double.IsFinite(current.DryGPerM2) || current.DryGPerM2 < 0
            || !double.IsFinite(current.NitrogenGPerM2) || current.NitrogenGPerM2 < 0
            || !double.IsFinite(proposedDeposit.DryGPerM2) || proposedDeposit.DryGPerM2 < 0
            || !double.IsFinite(proposedDeposit.NitrogenGPerM2) || proposedDeposit.NitrogenGPerM2 < 0
            || !double.IsFinite(current.DryGPerM2 + proposedDeposit.DryGPerM2)
            || !double.IsFinite(current.NitrogenGPerM2 + proposedDeposit.NitrogenGPerM2))
            throw new ArgumentOutOfRangeException(nameof(proposedDeposit),
                "Detritus deposits must be finite and non-negative.");
        return new(current.DryGPerM2 + proposedDeposit.DryGPerM2,
            current.NitrogenGPerM2 + proposedDeposit.NitrogenGPerM2);
    }

    public static EcologyDailyResultV1 CloseDay(EcologyDailyInputV1 input)
    {
        var plants = input.Botany.Plants;
        var factors = input.Botany.DailyFactors;
        var grass = ClosePlantDay(plants.Grass, factors.GrassWaterFactorSum / 24,
            factors.GrassNitrogenFactorSum / 24, factors.GrassTemperatureFactorSum / 24,
            factors.GrassOutOfRangeHours / 24, 600);
        var woody = ClosePlantDay(plants.Woody, factors.WoodyWaterFactorSum / 24,
            factors.WoodyNitrogenFactorSum / 24, factors.WoodyTemperatureFactorSum / 24,
            factors.WoodyOutOfRangeHours / 24, 1800);

        var deadDry = grass.DeadDry + woody.DeadDry;
        var deadN = grass.DeadN + woody.DeadN;
        var detritusDry = input.DetritusDryGPerM2 + deadDry;
        var detritusNitrogen = input.DetritusNitrogenGPerM2 + deadN;

        var fTemperature = Math.Clamp(Math.Pow(2, (input.MeanAirTemperatureC - 20) / 10), 0.25, 2.50);
        var fWater = Math.Clamp((input.SoilMoisture.SoilWaterMm - input.SoilMoisture.WiltMm)
            / (0.5 * (input.SoilMoisture.CapacityMm - input.SoilMoisture.WiltMm)), 0, 1);
        var litterRate = 0.008 * fTemperature * fWater;
        var litterFraction = 1 - Math.Exp(-litterRate);
        var decayedDry = detritusDry * litterFraction;
        var decayedN = detritusNitrogen * litterFraction;
        var soilMatterRate = 0.0004 * fTemperature * fWater;
        var soilMatterFraction = 1 - Math.Exp(-soilMatterRate);
        var soilMatterLoss = input.SoilBiogeochemistry.SoilOrganicMatterGPerM2 * soilMatterFraction;
        var soilNitrogenRelease = input.SoilBiogeochemistry.SoilOrganicNitrogenGPerM2 * soilMatterFraction;

        var updatedBotany = input.Botany with
        {
            Plants = new PlantPatchPair(grass.Patch, woody.Patch),
            DailyFactors = default
        };

        var soilProposal = new SoilBiogeochemistryProposalV1(
            input.SoilBiogeochemistry.SoilOrganicMatterGPerM2 + 0.25 * decayedDry - soilMatterLoss,
            input.SoilBiogeochemistry.SoilOrganicNitrogenGPerM2 + 0.70 * decayedN - soilNitrogenRelease,
            input.SoilBiogeochemistry.MineralNitrogenGPerM2 + 0.30 * decayedN + soilNitrogenRelease);
        return new(updatedBotany, detritusDry - decayedDry, detritusNitrogen - decayedN,
            soilProposal, 0.75 * decayedDry, soilMatterLoss);
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
