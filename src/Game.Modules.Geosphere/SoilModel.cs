using Game.Contracts;

namespace Game.Modules.Geosphere;

public static class SoilModel
{
    public static (double CapacityMm, double WiltMm, double KsatMmPerHour, double InitialWaterMm, double InitialN, double InitialSom, double InitialSomN)
        Profile(SoilKind kind) => kind switch
        {
            SoilKind.Sandy => (100, 20, 20, 55, 2, 500, 5),
            _ => (180, 35, 10, 90, 4, 1500, 22.5)
        };

    public static double MaximumInfiltrationMm(SoilHydraulicStateV1 soil)
    {
        ValidateHydraulicState(soil);
        return Math.Max(0, Math.Min(soil.KsatMmPerHour, soil.CapacityMm - soil.SoilWaterMm));
    }

    public static SoilWaterResultV1 ApplyInfiltrationAndEvaporate(
        SoilHydraulicStateV1 soil,
        double infiltrationMm,
        double plantCover,
        double potentialEtMm,
        double plantWaterDemandMultiplier)
    {
        ValidateHydraulicState(soil);
        ArgumentOutOfRangeException.ThrowIfNegative(infiltrationMm);
        ArgumentOutOfRangeException.ThrowIfNegative(potentialEtMm);
        if (!double.IsFinite(infiltrationMm) || !double.IsFinite(potentialEtMm)
            || !double.IsFinite(plantCover) || plantCover is < 0 or > 1
            || !double.IsFinite(plantWaterDemandMultiplier) || plantWaterDemandMultiplier is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(infiltrationMm), "Soil water exchange inputs must be finite and within their physical ranges.");
        var maximumInfiltrationMm = Math.Max(0, Math.Min(soil.KsatMmPerHour, soil.CapacityMm - soil.SoilWaterMm));
        if (infiltrationMm > maximumInfiltrationMm)
            throw new ArgumentOutOfRangeException(nameof(infiltrationMm), "Accepted infiltration cannot exceed the soil owner's hydraulic capacity.");

        var soilBeforeEvaporation = soil.SoilWaterMm + infiltrationMm;
        var soilFraction = (1 - plantCover) * plantWaterDemandMultiplier;
        var soilEvaporation = Math.Min(Math.Max(0, soilBeforeEvaporation - soil.WiltMm), potentialEtMm * soilFraction);
        var availableForPlants = Math.Max(0, soilBeforeEvaporation - soilEvaporation - soil.WiltMm);
        var availableFactor = Math.Clamp(availableForPlants / (0.25 * (soil.CapacityMm - soil.WiltMm)), 0, 1);

        return new(
            soilBeforeEvaporation - soilEvaporation,
            availableForPlants,
            availableFactor,
            soilEvaporation);
    }

    public static SoilTranspirationResultV1 ApplyPlantTranspiration(
        SoilHydraulicStateV1 soil, double proposedTranspirationMm)
    {
        ValidateHydraulicState(soil);
        if (!double.IsFinite(proposedTranspirationMm) || proposedTranspirationMm < 0)
            throw new ArgumentOutOfRangeException(nameof(proposedTranspirationMm),
                "Proposed plant transpiration must be finite and non-negative.");

        var confirmed = Math.Min(proposedTranspirationMm, Math.Max(0, soil.SoilWaterMm - soil.WiltMm));
        return new(soil.SoilWaterMm - confirmed, confirmed);
    }

    public static double ConfirmMineralNitrogenAfterGrowth(
        double availableMineralNitrogenGPerM2, double proposedRemainingMineralNitrogenGPerM2)
    {
        if (!double.IsFinite(availableMineralNitrogenGPerM2) || availableMineralNitrogenGPerM2 < 0
            || !double.IsFinite(proposedRemainingMineralNitrogenGPerM2)
            || proposedRemainingMineralNitrogenGPerM2 < 0
            || proposedRemainingMineralNitrogenGPerM2 > availableMineralNitrogenGPerM2)
            throw new ArgumentOutOfRangeException(nameof(proposedRemainingMineralNitrogenGPerM2),
                "The soil owner only accepts a finite, non-negative nitrogen proposal no greater than the available stock.");
        return proposedRemainingMineralNitrogenGPerM2;
    }

    public static double DepositMineralNitrogen(double currentMineralNitrogenGPerM2, double proposedDepositGPerM2)
    {
        if (!double.IsFinite(currentMineralNitrogenGPerM2) || currentMineralNitrogenGPerM2 < 0
            || !double.IsFinite(proposedDepositGPerM2) || proposedDepositGPerM2 < 0
            || !double.IsFinite(currentMineralNitrogenGPerM2 + proposedDepositGPerM2))
            throw new ArgumentOutOfRangeException(nameof(proposedDepositGPerM2),
                "Mineral-nitrogen deposits must be finite and non-negative.");
        return currentMineralNitrogenGPerM2 + proposedDepositGPerM2;
    }

    public static SoilBiogeochemistryStateV1 ConfirmEcologyProposal(
        SoilBiogeochemistryStateV1 current, SoilBiogeochemistryProposalV1 proposal)
    {
        ValidateBiogeochemistry(current);
        var accepted = new SoilBiogeochemistryStateV1(
            proposal.SoilOrganicMatterGPerM2,
            proposal.SoilOrganicNitrogenGPerM2,
            proposal.MineralNitrogenGPerM2);
        ValidateBiogeochemistry(accepted);
        return accepted;
    }

    private static void ValidateHydraulicState(SoilHydraulicStateV1 soil)
    {
        if (!double.IsFinite(soil.SoilWaterMm) || soil.SoilWaterMm < 0
            || !double.IsFinite(soil.CapacityMm) || soil.CapacityMm <= 0
            || !double.IsFinite(soil.WiltMm) || soil.WiltMm < 0 || soil.WiltMm >= soil.CapacityMm
            || !double.IsFinite(soil.KsatMmPerHour) || soil.KsatMmPerHour < 0
            || soil.SoilWaterMm > soil.CapacityMm)
            throw new ArgumentOutOfRangeException(nameof(soil), "Soil hydraulic state violates the soil profile bounds.");
    }

    private static void ValidateBiogeochemistry(SoilBiogeochemistryStateV1 state)
    {
        if (!double.IsFinite(state.SoilOrganicMatterGPerM2) || state.SoilOrganicMatterGPerM2 < 0
            || !double.IsFinite(state.SoilOrganicNitrogenGPerM2) || state.SoilOrganicNitrogenGPerM2 < 0
            || !double.IsFinite(state.MineralNitrogenGPerM2) || state.MineralNitrogenGPerM2 < 0)
            throw new ArgumentOutOfRangeException(nameof(state), "Soil biogeochemistry stocks must be finite and non-negative.");
    }
}
