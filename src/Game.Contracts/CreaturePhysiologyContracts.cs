namespace Game.Contracts;

/// <summary>L0 conversion for the dry-matter energy proxy already used by the N3 metabolism model.</summary>
public static class CreatureEnergyAccountingV1
{
    public const double JoulesPerDryGram = 18_000;
}

/// <summary>
/// Energy terms for one accounting interval. All fields are joules; structural dry matter,
/// nitrogen, and body water are accounted for by separate ledgers.
/// </summary>
public readonly record struct CreatureEnergyBudgetInputV1(
    double EnergyReserveBeforeJ,
    double GrossDietEnergyJ,
    double AssimilationEfficiency01,
    double BasalMaintenanceDemandJ,
    double ActivityDemandJ,
    double DigestiveProcessingDemandJ,
    double ThermoregulationDemandJ,
    double ExcretoryEnergyLossJ,
    double GrowthAllocationDemandJ,
    double ReproductionAllocationDemandJ,
    double OtherEnergyDemandJ);

/// <summary>Closed single-interval energy accounting; this does not predict demands or convert energy to mass.</summary>
public readonly record struct CreatureEnergyBudgetOutcomeV1(
    double AssimilatedFoodEnergyJ,
    double FecalFoodEnergyJ,
    double TotalDemandJ,
    double EnergyConsumedJ,
    double EnergyReserveAfterJ,
    double UnmetEnergyDemandJ,
    double UnmetBasalMaintenanceDemandJ);

/// <summary>Deterministic energy accounting with all physiology demands supplied by a profile.</summary>
public interface ICreatureEnergyBudgetV1
{
    CreatureEnergyBudgetOutcomeV1 ResolveEnergyBudget(CreatureEnergyBudgetInputV1 input);
}
