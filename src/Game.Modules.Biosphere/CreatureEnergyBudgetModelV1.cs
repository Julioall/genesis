using Game.Contracts;

namespace Game.Modules.Biosphere;

/// <summary>
/// Closes a caller-supplied energy budget without choosing species parameters or converting energy
/// into structural matter. Intake and all demands are joules for one declared model interval.
/// Basal maintenance is reported as funded before activity, following the current explicit L0 policy.
/// </summary>
public sealed class CreatureEnergyBudgetModelV1 : ICreatureEnergyBudgetV1
{
    public CreatureEnergyBudgetOutcomeV1 ResolveEnergyBudget(CreatureEnergyBudgetInputV1 input)
    {
        RequireFiniteNonNegative(input.EnergyReserveBeforeJ, nameof(input.EnergyReserveBeforeJ));
        RequireFiniteNonNegative(input.GrossDietEnergyJ, nameof(input.GrossDietEnergyJ));
        RequireFiniteUnitInterval(input.AssimilationEfficiency01, nameof(input.AssimilationEfficiency01));
        RequireFiniteNonNegative(input.BasalMaintenanceDemandJ, nameof(input.BasalMaintenanceDemandJ));
        RequireFiniteNonNegative(input.ActivityDemandJ, nameof(input.ActivityDemandJ));
        RequireFiniteNonNegative(input.DigestiveProcessingDemandJ, nameof(input.DigestiveProcessingDemandJ));
        RequireFiniteNonNegative(input.ThermoregulationDemandJ, nameof(input.ThermoregulationDemandJ));
        RequireFiniteNonNegative(input.ExcretoryEnergyLossJ, nameof(input.ExcretoryEnergyLossJ));
        RequireFiniteNonNegative(input.GrowthAllocationDemandJ, nameof(input.GrowthAllocationDemandJ));
        RequireFiniteNonNegative(input.ReproductionAllocationDemandJ, nameof(input.ReproductionAllocationDemandJ));
        RequireFiniteNonNegative(input.OtherEnergyDemandJ, nameof(input.OtherEnergyDemandJ));

        var assimilatedEnergy = input.GrossDietEnergyJ * input.AssimilationEfficiency01;
        var fecalEnergy = input.GrossDietEnergyJ - assimilatedEnergy;
        var totalDemand = input.BasalMaintenanceDemandJ
                          + input.ActivityDemandJ
                          + input.DigestiveProcessingDemandJ
                          + input.ThermoregulationDemandJ
                          + input.ExcretoryEnergyLossJ
                          + input.GrowthAllocationDemandJ
                          + input.ReproductionAllocationDemandJ
                          + input.OtherEnergyDemandJ;
        var availableEnergy = input.EnergyReserveBeforeJ + assimilatedEnergy;
        if (!double.IsFinite(assimilatedEnergy) || !double.IsFinite(fecalEnergy)
            || !double.IsFinite(totalDemand) || !double.IsFinite(availableEnergy))
            throw new ArgumentOutOfRangeException(nameof(input), "Combined energy terms must remain finite.");

        var unmetBasalMaintenance = Math.Max(0, input.BasalMaintenanceDemandJ - availableEnergy);
        var consumed = Math.Min(availableEnergy, totalDemand);
        var reserveAfter = availableEnergy - consumed;
        var unmetDemand = totalDemand - consumed;
        return new(assimilatedEnergy, fecalEnergy, totalDemand, consumed,
            Math.Max(0, reserveAfter), Math.Max(0, unmetDemand), unmetBasalMaintenance);
    }

    private static void RequireFiniteNonNegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0)
            throw new ArgumentOutOfRangeException(name, "Energy values must be finite and non-negative joules.");
    }

    private static void RequireFiniteUnitInterval(double value, string name)
    {
        if (!double.IsFinite(value) || value is < 0 or > 1)
            throw new ArgumentOutOfRangeException(name, "Assimilation efficiency must be finite and between 0 and 1.");
    }
}
