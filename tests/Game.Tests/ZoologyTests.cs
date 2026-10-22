using Game.Contracts;
using Game.Core;
using Game.Modules.Biosphere;
using Game.Procedural;

namespace Game.Tests;

public sealed class ZoologyTests
{
    private const int WorldCellCount = SimulationState.Width * SimulationState.Height;
    private static readonly double CellSizeM = Math.Sqrt(SimulationState.CellAreaM2);

    [Fact]
    public void Initial_animals_use_forage_cells_and_seeded_predators_start_near_herbivores()
    {
        var cells = EmptyCells();
        var grass = new PlantPatch(true, 100, 2, 0, 0, 0, 0);
        for (var index = 0; index < cells.Length; index++) cells[index] = cells[index] with { Grass = grass };

        var animals = ZoologyModel.CreateInitialPopulation(17, cells, new(40, 4));
        var herbivores = animals.Where(animal => animal.FunctionalProfileId == AnimalFunctionalProfileV1.Herbivore).ToArray();
        var predators = animals.Where(animal => animal.FunctionalProfileId == AnimalFunctionalProfileV1.Predator).ToArray();

        Assert.Equal(44, animals.Length);
        Assert.Equal(Enumerable.Range(1, 44).Select(id => (ulong)id), animals.Select(animal => animal.AnimalId));
        Assert.All(herbivores, animal => Assert.True(cells[CellIndex(animal.PositionXM, animal.PositionYM)].Grass.Present));
        for (var index = 0; index < predators.Length; index++)
        {
            var assignedHerbivore = herbivores[index * herbivores.Length / predators.Length];
            Assert.True(Distance(predators[index], assignedHerbivore) <= Math.Sqrt(2) + 1e-9);
        }
    }

    [Fact]
    public void Herbivore_only_eats_a_patch_in_its_starting_cell()
    {
        var cells = EmptyCells();
        const int patchX = 10;
        const int patchY = 10;
        var patchIndex = patchY * SimulationState.Width + patchX;
        var patch = new PlantPatch(true, 100, 2, 0, 0, 0, 0);
        cells[patchIndex] = cells[patchIndex] with { Grass = patch };
        var animals = new[]
        {
            Herbivore(1, (patchX - 1) * CellSizeM + CellSizeM - 0.1, (patchY + 0.5) * CellSizeM),
            Herbivore(2, (patchX + 0.5) * CellSizeM, (patchY + 0.5) * CellSizeM)
        };

        var result = new BiosphereModuleV1().AdvanceTick(new(1, 1, cells, animals, 3));

        var updatedPatch = result.CellChanges.Single(cell => cell.Index == patchIndex).Grass;
        var requestedDryG = 0.025 * (5000 + 500) / SimulationState.TicksPerDay;
        Assert.Equal(requestedDryG * CreatureEnergyAccountingV1.JoulesPerDryGram * (1 - 0.45),
            result.FecalFoodEnergyJ, 8);
        Assert.Equal(result.AnimalRespirationDryGrams * CreatureEnergyAccountingV1.JoulesPerDryGram,
            result.AnimalEnergyConsumedJ, 8);
        Assert.Equal(patch.BiomassGPerM2 - requestedDryG / SimulationState.CellAreaM2,
            updatedPatch.BiomassGPerM2, 10);
        Assert.Equal(patch.NitrogenGPerM2 * updatedPatch.BiomassGPerM2 / patch.BiomassGPerM2,
            updatedPatch.NitrogenGPerM2, 10);
    }

    [Fact]
    public void N3_metabolism_uses_energy_budget_for_reserve_consumption()
    {
        var animal = Herbivore(1, 105, 105);
        var result = new BiosphereModuleV1().AdvanceTick(
            new ZoologyTickInputV1(1, 1, EmptyCells(), [animal], 2));

        var updated = Assert.Single(result.Animals);
        var expectedRespirationG = 0.30 * Math.Pow(5.5, 0.75)
            / SimulationState.TicksPerDay / 0.018;
        Assert.Equal(expectedRespirationG, result.AnimalRespirationDryGrams, 10);
        Assert.Equal(expectedRespirationG * CreatureEnergyAccountingV1.JoulesPerDryGram,
            result.AnimalEnergyConsumedJ, 8);
        Assert.Equal(animal.ReserveDryG - expectedRespirationG, updated.ReserveDryG, 10);
        Assert.Equal(0, result.FecalFoodEnergyJ);
        Assert.Equal(0, result.UnmetEnergyDemandJ);
    }

    [Fact]
    public void Herbivore_route_stops_at_owner_sampled_deep_water_and_persists_only_accepted_position()
    {
        var cells = EmptyCells();
        const int patchX = 13;
        const int patchY = 10;
        var patchIndex = patchY * SimulationState.Width + patchX;
        cells[patchIndex] = cells[patchIndex] with
        {
            Grass = new PlantPatch(true, 100, 2, 0, 0, 0, 0)
        };
        for (var index = 0; index < cells.Length; index++)
            if (cells[index].X >= 11)
                cells[index] = cells[index] with { SurfaceWaterMm = 1000 };
        var animal = Herbivore(1, 105, 105);

        var result = new BiosphereModuleV1().AdvanceTick(new(1, 1, cells, [animal], 2));

        var moved = Assert.Single(result.Animals);
        Assert.InRange(moved.PositionXM, 107.49, 107.51);
        Assert.Equal(animal.PositionYM, moved.PositionYM);
        Assert.All(cells.Where(cell => cell.X >= 11), cell => Assert.Equal(1000, cell.SurfaceWaterMm));
        Assert.All(result.CellChanges, cell => Assert.Equal(cells[cell.Index].SurfaceWaterMm, cell.SurfaceWaterMm));
    }

    [Fact]
    public void Herbivore_locomotion_uses_only_energy_remaining_after_basal_maintenance()
    {
        var cells = EmptyCells();
        const int targetIndex = 10 * SimulationState.Width + 12;
        cells[targetIndex] = cells[targetIndex] with
        {
            Grass = new PlantPatch(true, 0.0108, 0.000216, 0, 0, 0, 0)
        };
        var animal = Herbivore(1, 105, 105) with { ReserveDryG = 0.388 };

        var result = new BiosphereModuleV1().AdvanceTick(
            new(1, 1, cells, [animal], 2));

        var moved = Assert.Single(result.Animals);
        var distance = Distance(animal, moved);
        Assert.InRange(distance, 0.1, 5);
        Assert.InRange(result.UnmetEnergyDemandJ, 0, 1e-6);
    }

    [Fact]
    public void Herbivore_with_unmet_basal_maintenance_does_not_spend_energy_on_locomotion()
    {
        var cells = EmptyCells();
        const int targetIndex = 10 * SimulationState.Width + 12;
        cells[targetIndex] = cells[targetIndex] with
        {
            Grass = new PlantPatch(true, 0.001, 0.00002, 0, 0, 0, 0)
        };
        var animal = Herbivore(1, 105, 105) with { ReserveDryG = 0 };

        var result = new BiosphereModuleV1().AdvanceTick(
            new(1, 1, cells, [animal], 2));

        var updated = Assert.Single(result.Animals);
        Assert.Equal(animal.PositionXM, updated.PositionXM);
        Assert.Equal(animal.PositionYM, updated.PositionYM);
        Assert.True(result.UnmetEnergyDemandJ > 0);
        Assert.True(updated.MaintenanceDeficitDays > animal.MaintenanceDeficitDays);
    }

    [Fact]
    public void Herbivore_route_stops_at_owner_sampled_excessive_slope()
    {
        var cells = EmptyCells();
        const int patchX = 13;
        const int patchY = 10;
        var patchIndex = patchY * SimulationState.Width + patchX;
        cells[patchIndex] = cells[patchIndex] with
        {
            Grass = new PlantPatch(true, 100, 2, 0, 0, 0, 0)
        };
        for (var index = 0; index < cells.Length; index++)
            if (cells[index].X >= 11)
                cells[index] = cells[index] with { GroundElevationM = 10 };
        var animal = Herbivore(1, 105, 105);

        var result = new BiosphereModuleV1().AdvanceTick(new(1, 1, cells, [animal], 2));

        var moved = Assert.Single(result.Animals);
        Assert.Equal(animal.PositionXM, moved.PositionXM);
        Assert.Equal(animal.PositionYM, moved.PositionYM);
        Assert.All(result.CellChanges, cell => Assert.Equal(cells[cell.Index].GroundElevationM, cell.GroundElevationM));
    }

    [Fact]
    public void Herbivore_uses_an_alternative_route_around_a_local_steep_barrier()
    {
        var cells = EmptyCells();
        const int startX = 10;
        const int row = 10;
        const int barrierX = 11;
        const int targetX = 13;
        cells[row * SimulationState.Width + barrierX] =
            cells[row * SimulationState.Width + barrierX] with { GroundElevationM = 20 };
        var targetIndex = row * SimulationState.Width + targetX;
        cells[targetIndex] = cells[targetIndex] with
        {
            Grass = new PlantPatch(true, 100, 2, 0, 0, 0, 0)
        };
        var animal = Herbivore(1, (startX + 0.5) * CellSizeM, (row + 0.5) * CellSizeM);

        var result = ZoologyModel.AdvanceTick(new(1, 1, cells, [animal], 2), new BiosphereModuleV1());

        var moved = Assert.Single(result.Animals);
        Assert.NotEqual(animal.PositionYM, moved.PositionYM);
        Assert.InRange(Distance(animal, moved), 0, 0.02 * 600 + 1e-9);
        Assert.True(Distance(animal.PositionXM, animal.PositionYM,
            (targetX + 0.5) * CellSizeM, (row + 0.5) * CellSizeM)
            > Distance(moved.PositionXM, moved.PositionYM,
                (targetX + 0.5) * CellSizeM, (row + 0.5) * CellSizeM));
    }

    [Fact]
    public void Concurrent_herbivory_is_capped_by_edible_stock_and_order_independent()
    {
        var cells = EmptyCells();
        const int patchIndex = 10 * SimulationState.Width + 10;
        var patch = new PlantPatch(true, 0.01, 0.0002, 0, 0, 0, 0);
        cells[patchIndex] = cells[patchIndex] with { Grass = patch };
        var first = Herbivore(1, 105, 105);
        var second = Herbivore(2, 105, 105);
        var model = new BiosphereModuleV1();

        var forward = ZoologyModel.AdvanceTick(new(1, 1, cells, [first, second], 3), model);
        var reversed = ZoologyModel.AdvanceTick(new(1, 1, cells, [second, first], 3), model);
        var forwardPatch = forward.CellChanges.Single(cell => cell.Index == patchIndex).Grass;
        var reversedPatch = reversed.CellChanges.Single(cell => cell.Index == patchIndex).Grass;

        Assert.Equal(0.002, forwardPatch.BiomassGPerM2, 10);
        Assert.Equal(0.00004, forwardPatch.NitrogenGPerM2, 10);
        Assert.Equal(forwardPatch, reversedPatch);
        Assert.Equal(forward.Animals, reversed.Animals);
    }

    [Fact]
    public void Predation_conflict_kills_one_prey_and_deposits_the_unconsumed_carcass()
    {
        var predator1 = Predator(1, 325, 325);
        var predator2 = Predator(2, 325, 325);
        var prey = Herbivore(3, 325, 325);
        var probability = 1 - Math.Exp(-0.80 / SimulationState.TicksPerDay);
        var seed = Enumerable.Range(1, 100_000).Select(value => (ulong)value).First(candidate =>
            CounterRandom.Unit(candidate, CounterRandom.PredationTag, predator1.AnimalId, prey.AnimalId, 1, 0) < probability
            && CounterRandom.Unit(candidate, CounterRandom.PredationTag, predator2.AnimalId, prey.AnimalId, 1, 0) < probability);

        var cells = EmptyCells();
        var result = ZoologyModel.AdvanceTick(new(seed, 1, cells, [predator1, predator2, prey], 4), new BiosphereModuleV1());

        Assert.Equal([1UL, 2UL], result.Animals.Select(animal => animal.AnimalId));
        Assert.Equal(0, result.Animals.Single(animal => animal.AnimalId == 1).LastCaptureDay);
        Assert.Equal(-1, result.Animals.Single(animal => animal.AnimalId == 2).LastCaptureDay);
        Assert.Equal(3.575, Assert.Single(result.WaterDeposits).Liters, 10);
        var carcassCell = result.CellChanges.Single(cell => cell.Index == CellIndex(prey.PositionXM, prey.PositionYM));
        Assert.True(carcassCell.DetritusDryGPerM2 >= (5500 - 0.015 * 13200) / SimulationState.CellAreaM2);
        Assert.True(carcassCell.DetritusNitrogenGPerM2 >= 0.020 * 5000
            * (1 - 0.015 * 13200 / 5500) / SimulationState.CellAreaM2);
        var animalNitrogenG = result.Animals.Sum(animal =>
            (animal.FunctionalProfileId == AnimalFunctionalProfileV1.Herbivore ? 0.020 : 0.025)
            * animal.StructuralDryG);
        var cellNitrogenG = (carcassCell.DetritusNitrogenGPerM2 + carcassCell.MineralNitrogenGPerM2)
            * SimulationState.CellAreaM2;
        Assert.Equal(700, animalNitrogenG + cellNitrogenG, 8);
        var animalDryMatterG = result.Animals.Sum(animal => animal.StructuralDryG + animal.ReserveDryG);
        var detritusDryMatterG = carcassCell.DetritusDryGPerM2 * SimulationState.CellAreaM2;
        Assert.Equal(31_900, animalDryMatterG + detritusDryMatterG + result.AnimalRespirationDryGrams, 8);
        Assert.Equal(20.735, result.Animals.Sum(animal => animal.HydrationL)
            + result.WaterDeposits.Sum(deposit => deposit.Liters), 10);
    }

    [Fact]
    public void Predator_capture_quota_allows_one_prey_per_predator_per_day_and_resets_at_midnight()
    {
        var probability = 1 - Math.Exp(-0.80 / SimulationState.TicksPerDay);
        var seed = Enumerable.Range(1, 100_000).Select(value => (ulong)value).First(candidate =>
            HasTwoPotentialCaptures(candidate, 1, 3, 4, 1, 144, probability)
            && HasTwoPotentialCaptures(candidate, 2, 6, 7, 1, 144, probability)
            && HasTwoPotentialCaptures(candidate, 1, 4, 5, 145, 288, probability)
            && HasTwoPotentialCaptures(candidate, 2, 7, 8, 145, 288, probability));
        var cells = EmptyCells();
        var animals = new[]
        {
            Predator(1, 105, 105), Predator(2, 205, 105),
            Herbivore(3, 105, 105), Herbivore(4, 105, 105), Herbivore(5, 105, 105),
            Herbivore(6, 205, 105), Herbivore(7, 205, 105), Herbivore(8, 205, 105)
        };
        var preyDeathsByDay = new Dictionary<long, List<ulong>>();
        var module = new BiosphereModuleV1();

        for (var tickIndex = 1; tickIndex <= 2 * SimulationState.TicksPerDay; tickIndex++)
        {
            var priorIds = animals.Select(animal => animal.AnimalId).ToHashSet();
            var result = ZoologyModel.AdvanceTick(new(seed, tickIndex, cells, animals, 9), module);
            var absoluteDay = (tickIndex - 1) / SimulationState.TicksPerDay;
            var killedPrey = priorIds.Except(result.Animals.Select(animal => animal.AnimalId))
                .Where(animalId => animalId >= 3).ToArray();
            if (killedPrey.Length > 0)
            {
                if (!preyDeathsByDay.TryGetValue(absoluteDay, out var list)) preyDeathsByDay[absoluteDay] = list = [];
                list.AddRange(killedPrey);
            }
            animals = result.Animals;
            foreach (var changedCell in result.CellChanges) cells[changedCell.Index] = changedCell;
        }

        Assert.Equal(new ulong[] { 3, 6 }, preyDeathsByDay[0].Order().ToArray());
        Assert.Equal(new ulong[] { 4, 7 }, preyDeathsByDay[1].Order().ToArray());
        Assert.Equal(new ulong[] { 5, 8 }, animals.Where(animal => animal.FunctionalProfileId == AnimalFunctionalProfileV1.Herbivore)
            .Select(animal => animal.AnimalId).Order().ToArray());
        Assert.All(animals.Where(animal => animal.FunctionalProfileId == AnimalFunctionalProfileV1.Predator),
            predator => Assert.Equal(1, predator.LastCaptureDay));
    }

    [Fact]
    public void Daily_water_and_reproduction_transfer_only_their_declared_stocks()
    {
        var parent = Herbivore(1, 325, 325);
        var water = ZoologyModel.PrepareDailyWater([parent]);
        Assert.Equal(0.44, water.ExcretionWaterLiters, 10);
        Assert.Equal(3.135, water.AnimalsAfterExcretion[0].HydrationL, 10);
        Assert.Equal(0.44, Assert.Single(water.WaterRequests).RequestedLiters, 10);

        var reproductionThreshold = 1 - Math.Exp(-0.01);
        var seed = Enumerable.Range(1, 100_000).Select(value => (ulong)value).First(candidate =>
            CounterRandom.Unit(candidate, CounterRandom.ReproductionTag, parent.AnimalId, 0, 0, 0) < reproductionThreshold);
        var result = ZoologyModel.CloseDay(new(seed, SimulationState.TicksPerDay, 0, EmptyCells(), [parent], [], 2));

        Assert.Equal(2, result.Animals.Length);
        var child = result.Animals.Single(animal => animal.AnimalId == 2);
        var updatedParent = result.Animals.Single(animal => animal.AnimalId == 1);
        Assert.Equal(0, child.AgeDays);
        Assert.Equal(250, child.StructuralDryG, 10);
        Assert.Equal(25, child.ReserveDryG, 10);
        Assert.Equal(4750, updatedParent.StructuralDryG, 10);
        Assert.Equal(475, updatedParent.ReserveDryG, 10);
        Assert.Equal(5000, result.Animals.Sum(animal => animal.StructuralDryG), 10);
        Assert.Equal(500, result.Animals.Sum(animal => animal.ReserveDryG), 10);
        Assert.Equal(3.575, result.Animals.Sum(animal => animal.HydrationL), 10);
        var parentGenome = updatedParent.Genome!.Value;
        var childGenome = child.Genome!.Value;
        Assert.Equal(CreatureSpeciesCatalogV1.Quadruped.SpeciesId, childGenome.SpeciesId);
        Assert.Equal(parentGenome.GenomeId, childGenome.ParentAId);
        Assert.Null(childGenome.ParentBId);
        Assert.NotEqual(parentGenome.GenomeId, childGenome.GenomeId);
        var repeated = ZoologyModel.CloseDay(new(seed, SimulationState.TicksPerDay, 0,
            EmptyCells(), [parent], [], 2));
        Assert.Equal(childGenome, repeated.Animals.Single(animal => animal.AnimalId == 2).Genome!.Value);

        var underhydratedParent = parent with { HydrationL = 1.5 };
        var rejected = ZoologyModel.CloseDay(new(seed, SimulationState.TicksPerDay, 0, EmptyCells(),
            [underhydratedParent], [], 2));
        Assert.Single(rejected.Animals);
        Assert.Equal(parent.StructuralDryG, rejected.Animals[0].StructuralDryG);
        Assert.Equal(parent.ReserveDryG, rejected.Animals[0].ReserveDryG);
        Assert.Equal(underhydratedParent.HydrationL, rejected.Animals[0].HydrationL);
    }

    [Fact]
    public void Starvation_requires_three_days_of_unmet_maintenance_while_fed_animals_remain_nonnegative()
    {
        var cells = EmptyCells();
        var starving = Herbivore(1, 325, 325) with { ReserveDryG = 0, HydrationL = 3.25 };
        var animal = starving;
        ZoologyDailyResultV1? death = null;
        for (var tickIndex = 1; tickIndex <= 3 * SimulationState.TicksPerDay; tickIndex++)
        {
            var tick = ZoologyModel.AdvanceTick(new(1, tickIndex, cells, [animal], 2), new BiosphereModuleV1());
            animal = Assert.Single(tick.Animals);
            Assert.True(animal.ReserveDryG >= 0);
            if (tickIndex % SimulationState.TicksPerDay == 0)
            {
                var daily = ZoologyModel.CloseDay(new(1, tickIndex, tickIndex / SimulationState.TicksPerDay - 1,
                    cells, [animal], [], 2));
                if (tickIndex == 3 * SimulationState.TicksPerDay) death = daily;
                else animal = Assert.Single(daily.Animals);
            }
        }
        Assert.NotNull(death);
        Assert.Empty(death.Value.Animals);
        Assert.Equal(50, Assert.Single(death.Value.CellChanges).DetritusDryGPerM2, 10);

        var fedCells = EmptyCells();
        const int forageIndex = 10 * SimulationState.Width + 10;
        fedCells[forageIndex] = fedCells[forageIndex] with
        {
            Grass = new PlantPatch(true, 100, 2, 0, 0, 0, 0)
        };
        var fed = Herbivore(1, 105, 105);
        for (var tickIndex = 1; tickIndex <= SimulationState.TicksPerDay; tickIndex++)
        {
            var tick = ZoologyModel.AdvanceTick(new(1, tickIndex, fedCells, [fed], 2), new BiosphereModuleV1());
            fed = Assert.Single(tick.Animals);
            Assert.True(fed.StructuralDryG >= 0);
            Assert.True(fed.ReserveDryG >= 0);
            foreach (var changedCell in tick.CellChanges) fedCells[changedCell.Index] = changedCell;
        }
        var fedClose = ZoologyModel.CloseDay(new(1, SimulationState.TicksPerDay, 0, fedCells, [fed], [], 2));
        Assert.Single(fedClose.Animals);
        Assert.Equal(0, fedClose.Animals[0].MaintenanceDeficitDays);
    }

    [Fact]
    public void Dehydration_kills_after_two_daily_checks_and_returns_body_stocks()
    {
        var animal = Herbivore(1, 325, 325) with { ReserveDryG = 0, HydrationL = 0 };
        var cells = EmptyCells();

        var firstDay = ZoologyModel.CloseDay(new(1, SimulationState.TicksPerDay, 0,
            cells, [animal], [], 2));
        Assert.Single(firstDay.Animals);
        Assert.Equal(1, firstDay.Animals[0].DehydrationDeficitDays);
        Assert.Empty(firstDay.CellChanges);

        var secondDay = ZoologyModel.CloseDay(new(1, 2 * SimulationState.TicksPerDay, 1,
            cells, firstDay.Animals, [], 2));
        Assert.Empty(secondDay.Animals);
        Assert.Equal(50, Assert.Single(secondDay.CellChanges).DetritusDryGPerM2, 10);
        Assert.Empty(secondDay.WaterDeposits);
    }

    [Fact]
    public void Animal_at_maximum_age_dies_and_returns_dry_matter_nitrogen_and_water()
    {
        var animal = Herbivore(1, 325, 325) with { AgeDays = 3650 };

        var result = ZoologyModel.CloseDay(new(1, SimulationState.TicksPerDay, 0,
            EmptyCells(), [animal], [], 2));

        Assert.Empty(result.Animals);
        var cell = Assert.Single(result.CellChanges);
        Assert.Equal(55, cell.DetritusDryGPerM2, 10);
        Assert.Equal(1, cell.DetritusNitrogenGPerM2, 10);
        Assert.Equal(3.575, Assert.Single(result.WaterDeposits).Liters, 10);
    }

    [Fact]
    public void Animal_population_save_load_continuation_has_the_same_state_hash()
    {
        var uninterrupted = TestWorldFactory.Create(1, new(40, 4));
        uninterrupted.AdvanceTicks((int)SimulationState.TicksPerDay - 1);
        var resumed = TestWorldFactory.Load(uninterrupted.SaveToJson());

        uninterrupted.AdvanceTicks(1);
        resumed.AdvanceTicks(1);
        Assert.Equal(uninterrupted.CanonicalHash(), resumed.CanonicalHash());

        uninterrupted.AdvanceTicks(143);
        resumed.AdvanceTicks(143);
        Assert.Equal(uninterrupted.CanonicalHash(), resumed.CanonicalHash());
        Assert.Equal(uninterrupted.Snapshot().Animals, resumed.Snapshot().Animals);
    }

    [Fact]
    public void Captured_animal_water_returns_to_surface_and_preserves_the_global_water_ledger()
    {
        var world = TestWorldFactory.Create(1, new(40, 4));
        var initialInventory = world.Inventory();
        var initialLedger = world.Snapshot().Ledger;

        world.AdvanceTicks((int)SimulationState.TicksPerDay);

        var snapshot = world.Snapshot();
        Assert.Equal(3, snapshot.Animals.Count(animal =>
            animal.FunctionalProfileId == AnimalFunctionalProfileV1.Predator && animal.LastCaptureDay == 0));
        var inventory = world.Inventory();
        var ledger = snapshot.Ledger;
        var waterResidual = inventory.WaterLiters - initialInventory.WaterLiters
            - (ledger.PrecipitationLiters - initialLedger.PrecipitationLiters)
            + (ledger.InterceptionLiters - initialLedger.InterceptionLiters)
            + (ledger.OpenWaterEvaporationLiters - initialLedger.OpenWaterEvaporationLiters)
            + (ledger.SoilEvaporationLiters - initialLedger.SoilEvaporationLiters)
            + (ledger.PlantTranspirationLiters - initialLedger.PlantTranspirationLiters)
            + (ledger.AnimalExcretionWaterLiters - initialLedger.AnimalExcretionWaterLiters)
            + (ledger.WaterOutflowLiters - initialLedger.WaterOutflowLiters);
        Assert.InRange(Math.Abs(waterResidual), 0, 1e-6);
    }

    private static ZoologyCellStateV1[] EmptyCells()
    {
        var cells = new ZoologyCellStateV1[WorldCellCount];
        for (var index = 0; index < cells.Length; index++)
            cells[index] = new(index, index % SimulationState.Width, index / SimulationState.Width,
                default, default, 0, 0, 0);
        return cells;
    }

    private static AnimalAgentV1 Herbivore(ulong id, double x, double y)
        => new(id, AnimalFunctionalProfileV1.Herbivore, x, y, 180, 5000, 500, 3.575, 0, 0, 0, -1);

    private static AnimalAgentV1 Predator(ulong id, double x, double y)
        => new(id, AnimalFunctionalProfileV1.Predator, x, y, 365, 12000, 1200, 8.58, 0, 0, 0, -1);

    private static int CellIndex(double x, double y)
        => (int)(y / CellSizeM) * SimulationState.Width + (int)(x / CellSizeM);

    private static double Distance(AnimalAgentV1 left, AnimalAgentV1 right)
        => Math.Sqrt(Math.Pow(left.PositionXM - right.PositionXM, 2) + Math.Pow(left.PositionYM - right.PositionYM, 2));

    private static double Distance(double firstX, double firstY, double secondX, double secondY)
        => Math.Sqrt(Math.Pow(firstX - secondX, 2) + Math.Pow(firstY - secondY, 2));

    private static bool HasTwoPotentialCaptures(
        ulong seed,
        ulong predatorId,
        ulong firstPreyId,
        ulong secondPreyId,
        int firstTick,
        int lastTick,
        double probability)
    {
        for (var firstCaptureTick = firstTick; firstCaptureTick <= lastTick; firstCaptureTick++)
        {
            if (CounterRandom.Unit(seed, CounterRandom.PredationTag, predatorId, firstPreyId,
                    (ulong)firstCaptureTick, 0) >= probability)
                continue;
            for (var secondCaptureTick = firstCaptureTick + 1; secondCaptureTick <= lastTick; secondCaptureTick++)
                if (CounterRandom.Unit(seed, CounterRandom.PredationTag, predatorId, secondPreyId,
                        (ulong)secondCaptureTick, 0) < probability)
                    return true;
        }
        return false;
    }
}
