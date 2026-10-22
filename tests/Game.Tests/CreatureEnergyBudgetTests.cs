using Game.Contracts;
using Game.Modules.Biosphere;

namespace Game.Tests;

public sealed class CreatureEnergyBudgetTests
{
    private readonly CreatureEnergyBudgetModelV1 _budget = new();

    [Fact]
    public void Energy_budget_assimilates_food_and_closes_the_energy_balance_with_surplus()
    {
        var outcome = _budget.ResolveEnergyBudget(new(
            EnergyReserveBeforeJ: 20,
            GrossDietEnergyJ: 100,
            AssimilationEfficiency01: 0.75,
            BasalMaintenanceDemandJ: 10,
            ActivityDemandJ: 20,
            DigestiveProcessingDemandJ: 5,
            ThermoregulationDemandJ: 3,
            ExcretoryEnergyLossJ: 2,
            GrowthAllocationDemandJ: 4,
            ReproductionAllocationDemandJ: 1,
            OtherEnergyDemandJ: 0));

        Assert.Equal(75, outcome.AssimilatedFoodEnergyJ);
        Assert.Equal(25, outcome.FecalFoodEnergyJ);
        Assert.Equal(45, outcome.TotalDemandJ);
        Assert.Equal(45, outcome.EnergyConsumedJ);
        Assert.Equal(50, outcome.EnergyReserveAfterJ);
        Assert.Equal(0, outcome.UnmetEnergyDemandJ);
        Assert.Equal(0, outcome.UnmetBasalMaintenanceDemandJ);
        AssertEnergyConservation(20, 100, outcome);
    }

    [Fact]
    public void Energy_budget_reports_unmet_demand_without_creating_negative_reserves()
    {
        var outcome = _budget.ResolveEnergyBudget(new(
            5, 10, 0.5,
            BasalMaintenanceDemandJ: 7,
            ActivityDemandJ: 4,
            DigestiveProcessingDemandJ: 1,
            ThermoregulationDemandJ: 0,
            ExcretoryEnergyLossJ: 0,
            GrowthAllocationDemandJ: 2,
            ReproductionAllocationDemandJ: 1,
            OtherEnergyDemandJ: 0));

        Assert.Equal(5, outcome.AssimilatedFoodEnergyJ);
        Assert.Equal(5, outcome.FecalFoodEnergyJ);
        Assert.Equal(15, outcome.TotalDemandJ);
        Assert.Equal(10, outcome.EnergyConsumedJ);
        Assert.Equal(0, outcome.EnergyReserveAfterJ);
        Assert.Equal(5, outcome.UnmetEnergyDemandJ);
        Assert.Equal(0, outcome.UnmetBasalMaintenanceDemandJ);
        AssertEnergyConservation(5, 10, outcome);
    }

    [Fact]
    public void Energy_budget_reports_basal_maintenance_shortfall_separately()
    {
        var outcome = _budget.ResolveEnergyBudget(new(
            EnergyReserveBeforeJ: 3,
            GrossDietEnergyJ: 0,
            AssimilationEfficiency01: 0.5,
            BasalMaintenanceDemandJ: 10,
            ActivityDemandJ: 4,
            DigestiveProcessingDemandJ: 0,
            ThermoregulationDemandJ: 0,
            ExcretoryEnergyLossJ: 0,
            GrowthAllocationDemandJ: 0,
            ReproductionAllocationDemandJ: 0,
            OtherEnergyDemandJ: 0));

        Assert.Equal(3, outcome.EnergyConsumedJ);
        Assert.Equal(11, outcome.UnmetEnergyDemandJ);
        Assert.Equal(7, outcome.UnmetBasalMaintenanceDemandJ);
    }

    [Fact]
    public void Assimilation_efficiency_endpoints_preserve_food_energy_as_assimilated_or_fecal()
    {
        var zero = _budget.ResolveEnergyBudget(EmptyDemand(4, 10, 0));
        var complete = _budget.ResolveEnergyBudget(EmptyDemand(4, 10, 1));

        Assert.Equal(0, zero.AssimilatedFoodEnergyJ);
        Assert.Equal(10, zero.FecalFoodEnergyJ);
        Assert.Equal(10, complete.AssimilatedFoodEnergyJ);
        Assert.Equal(0, complete.FecalFoodEnergyJ);
        AssertEnergyConservation(4, 10, zero);
        AssertEnergyConservation(4, 10, complete);
    }

    [Fact]
    public void Energy_budget_rejects_invalid_terms_and_non_finite_combined_totals()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _budget.ResolveEnergyBudget(
            EmptyDemand(double.NaN, 0, 0.5)));
        Assert.Throws<ArgumentOutOfRangeException>(() => _budget.ResolveEnergyBudget(
            EmptyDemand(0, 1, 1.01)));
        Assert.Throws<ArgumentOutOfRangeException>(() => _budget.ResolveEnergyBudget(
            EmptyDemand(0, 0, 0.5) with { ActivityDemandJ = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => _budget.ResolveEnergyBudget(
            EmptyDemand(double.MaxValue, double.MaxValue, 0.5)));
    }

    private static CreatureEnergyBudgetInputV1 EmptyDemand(
        double reserveJ,
        double grossDietEnergyJ,
        double assimilationEfficiency01)
        => new(reserveJ, grossDietEnergyJ, assimilationEfficiency01,
            0, 0, 0, 0, 0, 0, 0, 0);

    private static void AssertEnergyConservation(
        double reserveBeforeJ,
        double grossFoodEnergyJ,
        CreatureEnergyBudgetOutcomeV1 outcome)
    {
        var energyBefore = reserveBeforeJ + grossFoodEnergyJ;
        var energyAccounted = outcome.EnergyReserveAfterJ + outcome.EnergyConsumedJ + outcome.FecalFoodEnergyJ;
        Assert.InRange(Math.Abs(energyBefore - energyAccounted), 0, 1e-9 * Math.Max(1, energyBefore));
    }
}
