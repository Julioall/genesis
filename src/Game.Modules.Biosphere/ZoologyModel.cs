using Game.Contracts;
using Game.Procedural;

namespace Game.Modules.Biosphere;

public static class ZoologyModel
{
    private const double TickDays = 1.0 / SimulationState.TicksPerDay;
    private static readonly double CellSizeM = Math.Sqrt(SimulationState.CellAreaM2);
    private const double PerceptionRadiusM = 30;
    // L0 movement proxies until profiles are calibrated against a selected real-world species.
    private const double PrototypeMaximumSlopeGrade = 0.75;
    private const double PrototypeMaximumWadeDepthM = 0.25;
    private const double PrototypeActivityEnergyJPerMeter = 10.0;
    private const double RouteSampleSpacingM = 5;
    private const ulong GeneticMutationTag = 0x5A354D5554415445UL;
    private static readonly CreatureDevelopmentModelV1 CreatureDevelopment = new();

    private readonly record struct Profile(
        double InitialStructuralDryG,
        double MaximumStructuralDryG,
        double IntakePerBodyDay,
        double DryAssimilation,
        double NitrogenAssimilation,
        double StructuralNitrogenFraction,
        double SpeedMPerSecond,
        double MaturityDays,
        double ReproductionCooldownDays,
        double ChildFraction);

    private readonly record struct Food(double DryG, double NitrogenG);
    private readonly record struct ForageTarget(int CellIndex, PlantKind Kind, double X, double Y, double Score);
    private readonly record struct ForageRequest(ulong AnimalId, double RequestedDryG);

    public static AnimalAgentV1[] CreateInitialPopulation(
        ulong seed,
        ZoologyCellStateV1[] cells,
        InitialAnimalPopulationV1 population)
    {
        ArgumentNullException.ThrowIfNull(cells);
        if (cells.Length != SimulationState.Width * SimulationState.Height)
            throw new ArgumentException("Animal initialization requires a complete world cell field.", nameof(cells));
        if (population.Herbivores < 0 || population.Predators < 0)
            throw new ArgumentOutOfRangeException(nameof(population), "Initial population counts cannot be negative.");

        var animals = new AnimalAgentV1[checked(population.Herbivores + population.Predators)];
        var forageCells = cells.Where(cell =>
            cell.Grass.BiomassGPerM2 * 0.80 + cell.Woody.BiomassGPerM2 * 0.15 > 0).ToArray();
        if (forageCells.Length == 0) forageCells = cells;
        var index = 0;
        ulong id = 1;
        for (var i = 0; i < population.Herbivores; i++)
            animals[index++] = CreateInitialAnimal(seed, forageCells, id++, AnimalFunctionalProfileV1.Herbivore);
        for (var i = 0; i < population.Predators; i++)
        {
            var nearbyHerbivore = population.Herbivores == 0
                ? (AnimalAgentV1?)null
                : animals[(int)((long)i * population.Herbivores / Math.Max(1, population.Predators))];
            animals[index++] = CreateInitialAnimal(seed, cells, id++, AnimalFunctionalProfileV1.Predator,
                nearbyHerbivore);
        }
        return EnsureGenomes(seed, animals);
    }

    public static AnimalAgentV1[] EnsureGenomes(ulong seed, AnimalAgentV1[] animals)
    {
        ArgumentNullException.ThrowIfNull(animals);
        var result = (AnimalAgentV1[])animals.Clone();
        for (var index = 0; index < result.Length; index++)
        {
            var animal = result[index];
            var species = SpeciesFor(animal.FunctionalProfileId);
            if (animal.Genome is { } genome)
            {
                _ = CreatureDevelopmentModelV1.ExportGenome(species, genome);
                continue;
            }

            result[index] = animal with
            {
                Genome = CreatureDevelopment.CreateGenomeFromWorldSeed(species, seed, animal.AnimalId)
            };
        }
        return result;
    }

    public static ZoologyTickResultV1 AdvanceTick(
        ZoologyTickInputV1 input,
        IBotanyV1 botany,
        ICreatureMovementSolverV1? movementSolver = null,
        ICreatureEnergyBudgetV1? energyBudget = null)
    {
        ArgumentNullException.ThrowIfNull(botany);
        movementSolver ??= new CreatureMovementSolverV1();
        energyBudget ??= new CreatureEnergyBudgetModelV1();
        ArgumentNullException.ThrowIfNull(input.Cells);
        ArgumentNullException.ThrowIfNull(input.Animals);
        if (input.Cells.Length != SimulationState.Width * SimulationState.Height || input.TickIndex <= 0)
            throw new ArgumentException("Zoology received an invalid world or tick.", nameof(input));

        var cellChanges = new Dictionary<int, ZoologyCellStateV1>();
        var ordered = EnsureGenomes(input.Seed, input.Animals);
        for (var index = 1; index < ordered.Length; index++)
        {
            if (ordered[index - 1].AnimalId <= ordered[index].AnimalId) continue;
            ordered = ordered.OrderBy(animal => animal.AnimalId).ToArray();
            break;
        }
        var hasPredators = false;
        foreach (var animal in ordered)
            if (animal.FunctionalProfileId == AnimalFunctionalProfileV1.Predator)
            {
                hasPredators = true;
                break;
            }
        var animalsById = hasPredators ? ordered.ToDictionary(animal => animal.AnimalId) : null;
        var spatialIndex = hasPredators ? BuildAnimalSpatialIndex(ordered) : null;
        var forageTargets = new Dictionary<ulong, ForageTarget>();
        var movementTargets = new Dictionary<ulong, (double X, double Y)>();
        var predatorTargets = new Dictionary<ulong, ulong>();
        var captures = new Dictionary<ulong, AnimalAgentV1>();
        var capturedPreyIds = new HashSet<ulong>();
        var waterDeposits = new List<SurfaceWaterDepositV1>();

        foreach (var animal in ordered)
        {
            if (animal.FunctionalProfileId == AnimalFunctionalProfileV1.Herbivore)
            {
                var target = FindBestPlantTarget(input.Cells, animal);
                if (target is { } found)
                {
                    forageTargets[animal.AnimalId] = found;
                    movementTargets[animal.AnimalId] = (found.X, found.Y);
                }
                continue;
            }

            var prey = FindNearestPrey(animal, spatialIndex!);
            if (prey is not { } candidate) continue;
            predatorTargets[animal.AnimalId] = candidate.AnimalId;
            movementTargets[animal.AnimalId] = (candidate.PositionXM, candidate.PositionYM);
        }

        var absoluteDay = (input.TickIndex - 1) / SimulationState.TicksPerDay;
        foreach (var predator in ordered)
        {
            if (predator.FunctionalProfileId != AnimalFunctionalProfileV1.Predator) continue;
            if (predator.LastCaptureDay == absoluteDay || !predatorTargets.TryGetValue(predator.AnimalId, out var preyId))
                continue;
            var prey = animalsById![preyId];
            if (capturedPreyIds.Contains(preyId) || Distance(predator.PositionXM, predator.PositionYM, prey.PositionXM, prey.PositionYM) > 10)
                continue;
            var captureProbability = 1 - Math.Exp(-0.80 * TickDays);
            var roll = CounterRandom.Unit(input.Seed, CounterRandom.PredationTag,
                predator.AnimalId, prey.AnimalId, (ulong)input.TickIndex, 0);
            if (roll >= captureProbability) continue;
            captures[predator.AnimalId] = prey;
            capturedPreyIds.Add(prey.AnimalId);
        }

        var foodByAnimal = new Dictionary<ulong, Food>();
        foreach (var (predatorId, prey) in captures.OrderBy(pair => pair.Key))
        {
            var predator = animalsById![predatorId];
            var preyProfile = GetProfile(prey.FunctionalProfileId);
            var predatorProfile = GetProfile(predator.FunctionalProfileId);
            var preyDryG = prey.StructuralDryG + prey.ReserveDryG;
            if (prey.HydrationL > 0)
                waterDeposits.Add(new(CellIndex(prey.PositionXM, prey.PositionYM), prey.HydrationL));
            var intakeDryG = Math.Min(predatorProfile.IntakePerBodyDay
                * (predator.StructuralDryG + predator.ReserveDryG), preyDryG);
            if (intakeDryG <= 0) continue;
            var removedFraction = intakeDryG / preyDryG;
            var preyNitrogenG = preyProfile.StructuralNitrogenFraction * prey.StructuralDryG;
            AddDetritus(input.Cells, cellChanges, CellIndex(prey.PositionXM, prey.PositionYM), preyDryG - intakeDryG,
                preyNitrogenG * (1 - removedFraction));
            foodByAnimal[predatorId] = new(intakeDryG, preyNitrogenG * removedFraction);
        }

        var forageRequests = new Dictionary<(int CellIndex, PlantKind Kind), List<ForageRequest>>();
        foreach (var animal in ordered)
        {
            if (animal.FunctionalProfileId != AnimalFunctionalProfileV1.Herbivore || capturedPreyIds.Contains(animal.AnimalId)
                || !forageTargets.TryGetValue(animal.AnimalId, out var target)
                || CellIndex(animal.PositionXM, animal.PositionYM) != target.CellIndex)
                continue;
            var profile = GetProfile(animal.FunctionalProfileId);
            var request = profile.IntakePerBodyDay * (animal.StructuralDryG + animal.ReserveDryG) * TickDays;
            if (request <= 0) continue;
            var key = (target.CellIndex, target.Kind);
            if (!forageRequests.TryGetValue(key, out var list)) forageRequests[key] = list = [];
            list.Add(new(animal.AnimalId, request));
        }

        foreach (var (key, requests) in forageRequests.OrderBy(pair => pair.Key.CellIndex).ThenBy(pair => pair.Key.Kind))
        {
            requests.Sort((left, right) => left.AnimalId.CompareTo(right.AnimalId));
            var cell = GetCell(input.Cells, cellChanges, key.CellIndex);
            var patch = key.Kind == PlantKind.Grass ? cell.Grass : cell.Woody;
            var resolution = botany.ResolveHerbivory(patch, key.Kind,
                requests.Select(request => new HerbivoryRequestV1(request.AnimalId, request.RequestedDryG)).ToArray());
            if (resolution.Grants is null || resolution.Grants.Length != requests.Count)
                throw new InvalidOperationException("Botany returned invalid herbivory grants.");
            var requestsById = requests.ToDictionary(request => request.AnimalId, request => request.RequestedDryG);
            var grantedIds = new HashSet<ulong>();
            foreach (var grant in resolution.Grants)
            {
                if (!requestsById.TryGetValue(grant.AnimalId, out var requestedDryG)
                    || !grantedIds.Add(grant.AnimalId) || grant.GrantedDryG > requestedDryG + 1e-9
                    || !double.IsFinite(grant.GrantedDryG) || grant.GrantedDryG < 0
                    || !double.IsFinite(grant.GrantedNitrogenG) || grant.GrantedNitrogenG < 0)
                    throw new InvalidOperationException("Botany returned a grant outside the herbivory request set.");
                AddFood(foodByAnimal, grant.AnimalId, grant.GrantedDryG, grant.GrantedNitrogenG);
            }
            var updatedPatch = resolution.UpdatedPatch;
            cellChanges[key.CellIndex] = key.Kind == PlantKind.Grass
                ? cell with { Grass = updatedPatch }
                : cell with { Woody = updatedPatch };
        }

        var surviving = new List<AnimalAgentV1>(ordered.Length);
        var respirationDryG = 0.0;
        var energyConsumedJ = 0.0;
        var fecalFoodEnergyJ = 0.0;
        var unmetEnergyDemandJ = 0.0;
        CreatureMovementRouteSampleV1[]? routeEnvironmentGrid = null;
        foreach (var animal in ordered)
        {
            if (capturedPreyIds.Contains(animal.AnimalId)) continue;
            var profile = GetProfile(animal.FunctionalProfileId);
            var food = foodByAnimal.GetValueOrDefault(animal.AnimalId);
            (double X, double Y) target = movementTargets.GetValueOrDefault(
                animal.AnimalId, (animal.PositionXM, animal.PositionYM));
            var totalBodyG = animal.StructuralDryG + animal.ReserveDryG;
            var maintenanceJ = 0.30 * Math.Pow(totalBodyG / 1000, 0.75) * TickDays * 1_000_000;
            var availableEnergyJ = animal.ReserveDryG * CreatureEnergyAccountingV1.JoulesPerDryGram
                + food.DryG * CreatureEnergyAccountingV1.JoulesPerDryGram * profile.DryAssimilation;
            // The prototype funds basal maintenance before locomotor activity. These limits remain
            // L0 engineering proxies until each functional profile has a calibrated energy policy.
            var energyLimitedTravelM = Math.Max(0, availableEnergyJ - maintenanceJ)
                / PrototypeActivityEnergyJPerMeter;
            var maximumTravelM = Math.Min(profile.SpeedMPerSecond * 600, energyLimitedTravelM);
            var movement = captures.ContainsKey(animal.AnimalId)
                ? null
                : ResolveTerrestrialMovement(input.Cells, movementSolver, animal, target,
                    maximumTravelM, ref routeEnvironmentGrid);
            var movementDistance = movement?.HorizontalDistanceM ?? 0;
            var activityJ = PrototypeActivityEnergyJPerMeter * movementDistance;
            var energy = energyBudget.ResolveEnergyBudget(new CreatureEnergyBudgetInputV1(
                animal.ReserveDryG * CreatureEnergyAccountingV1.JoulesPerDryGram,
                food.DryG * CreatureEnergyAccountingV1.JoulesPerDryGram,
                profile.DryAssimilation,
                maintenanceJ,
                activityJ,
                0, 0, 0, 0, 0, 0));
            var assimilatedDryG = energy.AssimilatedFoodEnergyJ / CreatureEnergyAccountingV1.JoulesPerDryGram;
            var assimilatedNitrogenG = profile.NitrogenAssimilation * food.NitrogenG;
            var respirationActualG = energy.EnergyConsumedJ / CreatureEnergyAccountingV1.JoulesPerDryGram;
            var foodRespirationG = Math.Min(respirationActualG, assimilatedDryG);
            var dryAvailableG = Math.Max(0, assimilatedDryG - foodRespirationG);
            var newStructuralDryG = animal.StructuralDryG;
            var newReserveDryG = Math.Max(0,
                energy.EnergyReserveAfterJ / CreatureEnergyAccountingV1.JoulesPerDryGram - dryAvailableG);
            var structuralGrowthG = Math.Min(dryAvailableG, Math.Min(
                Math.Max(0, profile.MaximumStructuralDryG - animal.StructuralDryG),
                assimilatedNitrogenG / profile.StructuralNitrogenFraction));
            dryAvailableG -= structuralGrowthG;
            newStructuralDryG += structuralGrowthG;
            var reserveGainG = Math.Min(dryAvailableG,
                Math.Max(0, 0.20 * newStructuralDryG - newReserveDryG));
            newReserveDryG += reserveGainG;
            var sourceCellIndex = CellIndex(animal.PositionXM, animal.PositionYM);
            AddDetritus(input.Cells, cellChanges, sourceCellIndex,
                (1 - profile.DryAssimilation) * food.DryG + Math.Max(0, dryAvailableG - reserveGainG),
                (1 - profile.NitrogenAssimilation) * food.NitrogenG);
            var excessNitrogenG = Math.Max(0, assimilatedNitrogenG - profile.StructuralNitrogenFraction * structuralGrowthG);
            if (excessNitrogenG > 0)
            {
                var sourceCell = GetCell(input.Cells, cellChanges, sourceCellIndex);
                cellChanges[sourceCellIndex] = sourceCell with
                {
                    MineralNitrogenGPerM2 = sourceCell.MineralNitrogenGPerM2
                        + excessNitrogenG / SimulationState.CellAreaM2
                };
            }
            respirationDryG += respirationActualG;
            energyConsumedJ += energy.EnergyConsumedJ;
            fecalFoodEnergyJ += energy.FecalFoodEnergyJ;
            unmetEnergyDemandJ += energy.UnmetEnergyDemandJ;

            var deficit = energy.UnmetBasalMaintenanceDemandJ
                > 1e-12 * CreatureEnergyAccountingV1.JoulesPerDryGram
                ? animal.MaintenanceDeficitDays + TickDays
                : 0;
            var moved = movement is { } acceptedMovement
                ? (X: acceptedMovement.PositionM.X, Y: acceptedMovement.PositionM.Z)
                : (X: animal.PositionXM, Y: animal.PositionYM);
            surviving.Add(animal with
            {
                PositionXM = moved.X,
                PositionYM = moved.Y,
                AgeDays = animal.AgeDays + TickDays,
                StructuralDryG = newStructuralDryG,
                ReserveDryG = newReserveDryG,
                MaintenanceDeficitDays = deficit,
                ReproductionCooldownDays = Math.Max(0, animal.ReproductionCooldownDays - TickDays),
                LastCaptureDay = captures.ContainsKey(animal.AnimalId) ? absoluteDay : animal.LastCaptureDay
            });
        }

        return new(cellChanges.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray(),
            surviving.OrderBy(animal => animal.AnimalId).ToArray(), input.NextAnimalId, respirationDryG,
            waterDeposits.ToArray(), energyConsumedJ, fecalFoodEnergyJ, unmetEnergyDemandJ);
    }

    public static ZoologyWaterPreparationV1 PrepareDailyWater(AnimalAgentV1[] animals)
    {
        ArgumentNullException.ThrowIfNull(animals);
        var ordered = animals.OrderBy(animal => animal.AnimalId).ToArray();
        var afterExcretion = new AnimalAgentV1[ordered.Length];
        var requests = new List<SurfaceWaterDemandV1>(ordered.Length);
        var excretionWaterL = 0.0;
        for (var index = 0; index < ordered.Length; index++)
        {
            var animal = ordered[index];
            var totalBodyKg = (animal.StructuralDryG + animal.ReserveDryG) / 1000;
            var excretionL = Math.Min(animal.HydrationL, 0.08 * totalBodyKg);
            excretionWaterL += excretionL;
            var hydration = animal.HydrationL - excretionL;
            var targetWaterL = 0.65 * totalBodyKg;
            var requestL = Math.Max(0, targetWaterL - hydration);
            afterExcretion[index] = animal with { HydrationL = hydration };
            if (requestL > 0)
                requests.Add(new(CellIndex(animal.PositionXM, animal.PositionYM), animal.AnimalId, requestL));
        }
        return new(afterExcretion, requests.ToArray(), excretionWaterL);
    }

    public static ZoologyDailyResultV1 CloseDay(ZoologyDailyInputV1 input)
    {
        ArgumentNullException.ThrowIfNull(input.Cells);
        ArgumentNullException.ThrowIfNull(input.Animals);
        ArgumentNullException.ThrowIfNull(input.WaterGrants);
        if (input.Cells.Length != SimulationState.Width * SimulationState.Height || input.AbsoluteDay < 0)
            throw new ArgumentException("Zoology received an invalid daily state.", nameof(input));

        var cellChanges = new Dictionary<int, ZoologyCellStateV1>();
        var ordered = EnsureGenomes(input.Seed, input.Animals).OrderBy(animal => animal.AnimalId).ToArray();
        var animalsById = ordered.ToDictionary(animal => animal.AnimalId);
        var grantedById = new Dictionary<ulong, double>(input.WaterGrants.Length);
        foreach (var grant in input.WaterGrants)
        {
            if (!animalsById.TryGetValue(grant.ConsumerId, out var animal)
                || !double.IsFinite(grant.GrantedLiters) || grant.GrantedLiters < 0
                || !grantedById.TryAdd(grant.ConsumerId, grant.GrantedLiters))
                throw new ArgumentException("Daily water grants must reference unique animals and finite non-negative amounts.", nameof(input));
            var targetWaterL = 0.65 * (animal.StructuralDryG + animal.ReserveDryG) / 1000;
            if (grant.GrantedLiters > Math.Max(0, targetWaterL - animal.HydrationL) + 1e-9)
                throw new ArgumentException("Daily water grant exceeds the animal's request.", nameof(input));
        }

        var survivors = new List<AnimalAgentV1>(ordered.Length);
        var waterDeposits = new List<SurfaceWaterDepositV1>();
        foreach (var animal in ordered)
        {
            var updated = animal with { HydrationL = animal.HydrationL + grantedById.GetValueOrDefault(animal.AnimalId) };
            var profile = GetProfile(updated.FunctionalProfileId);
            var targetWaterL = 0.65 * (updated.StructuralDryG + updated.ReserveDryG) / 1000;
            var dehydratedDays = updated.HydrationL < 0.40 * targetWaterL
                ? updated.DehydrationDeficitDays + 1
                : 0;
            updated = updated with { DehydrationDeficitDays = dehydratedDays };
            var died = updated.MaintenanceDeficitDays >= 3
                || updated.DehydrationDeficitDays >= 2
                || updated.AgeDays >= 3650;
            if (died)
            {
                var cellIndex = CellIndex(updated.PositionXM, updated.PositionYM);
                AddDetritus(input.Cells, cellChanges, cellIndex,
                    updated.StructuralDryG + updated.ReserveDryG,
                    profile.StructuralNitrogenFraction * updated.StructuralDryG);
                if (updated.HydrationL > 0) waterDeposits.Add(new(cellIndex, updated.HydrationL));
                continue;
            }
            survivors.Add(updated);
        }

        var newborns = new List<AnimalAgentV1>();
        var nextId = input.NextAnimalId;
        foreach (var animal in survivors.OrderBy(animal => animal.AnimalId).ToArray())
        {
            var profile = GetProfile(animal.FunctionalProfileId);
            if (animal.AgeDays < profile.MaturityDays || animal.ReproductionCooldownDays > 0
                || animal.ReserveDryG < 0.10 * animal.StructuralDryG
                || CounterRandom.Unit(input.Seed, CounterRandom.ReproductionTag,
                    animal.AnimalId, 0, (ulong)input.AbsoluteDay, 0) >= 1 - Math.Exp(-0.01))
                continue;

            var childStructuralG = profile.ChildFraction * animal.StructuralDryG;
            var childReserveG = profile.ChildFraction * animal.ReserveDryG;
            var childWaterL = 0.65 * (childStructuralG + childReserveG) / 1000;
            var parentStructuralG = animal.StructuralDryG - childStructuralG;
            var parentReserveG = animal.ReserveDryG - childReserveG;
            var parentHydrationL = animal.HydrationL - childWaterL;
            var parentTargetWaterL = 0.65 * (parentStructuralG + parentReserveG) / 1000;
            if (nextId == 0 || parentStructuralG < 0 || parentReserveG < 0 || parentHydrationL < 0
                || parentHydrationL < 0.40 * parentTargetWaterL)
                continue;

            var parentIndex = survivors.FindIndex(candidate => candidate.AnimalId == animal.AnimalId);
            if (parentIndex < 0) continue;
            survivors[parentIndex] = animal with
            {
                StructuralDryG = parentStructuralG,
                ReserveDryG = parentReserveG,
                HydrationL = parentHydrationL,
                ReproductionCooldownDays = profile.ReproductionCooldownDays
            };
            var parentGenome = animal.Genome!.Value;
            var childGenome = CreatureDevelopment.MutateGenome(
                SpeciesFor(animal.FunctionalProfileId), parentGenome,
                CounterRandom.Bits(input.Seed, GeneticMutationTag, animal.AnimalId, nextId,
                    unchecked((ulong)input.AbsoluteDay), 0));
            newborns.Add(new(nextId++, animal.FunctionalProfileId, animal.PositionXM, animal.PositionYM, 0,
                childStructuralG, childReserveG, childWaterL, 0, 0, 0, -1, childGenome));
        }
        survivors.AddRange(newborns);
        return new(cellChanges.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray(),
            survivors.OrderBy(animal => animal.AnimalId).ToArray(), nextId, waterDeposits.ToArray());
    }

    private static AnimalAgentV1 CreateInitialAnimal(
        ulong seed,
        ZoologyCellStateV1[] cells,
        ulong id,
        AnimalFunctionalProfileV1 profileId,
        AnimalAgentV1? nearbyHerbivore = null)
    {
        var profile = GetProfile(profileId);
        var offsetX = CounterRandom.Unit(seed, CounterRandom.AnimalPlacementTag, id, (ulong)profileId, 0, 1);
        var offsetY = CounterRandom.Unit(seed, CounterRandom.AnimalPlacementTag, id, (ulong)profileId, 0, 2);
        double positionX;
        double positionY;
        if (nearbyHerbivore is { } prey)
        {
            var cell = cells[CellIndex(prey.PositionXM, prey.PositionYM)];
            var cellMinX = cell.X * CellSizeM;
            var cellMinY = cell.Y * CellSizeM;
            positionX = Math.Clamp(prey.PositionXM + (offsetX - 0.5) * 2,
                cellMinX, Math.BitDecrement(cellMinX + CellSizeM));
            positionY = Math.Clamp(prey.PositionYM + (offsetY - 0.5) * 2,
                cellMinY, Math.BitDecrement(cellMinY + CellSizeM));
        }
        else
        {
            var cellRoll = CounterRandom.Unit(seed, CounterRandom.AnimalPlacementTag, id, (ulong)profileId, 0, 0);
            var cell = cells[Math.Min(cells.Length - 1, (int)(cellRoll * cells.Length))];
            positionX = cell.X * CellSizeM + offsetX * CellSizeM;
            positionY = cell.Y * CellSizeM + offsetY * CellSizeM;
        }
        var structuralG = profile.InitialStructuralDryG;
        var reserveG = 0.10 * structuralG;
        return new(id, profileId, positionX, positionY, profile.MaturityDays, structuralG,
            reserveG, 0.65 * (structuralG + reserveG) / 1000, 0, 0, 0, -1);
    }

    private static CreatureSpeciesDefinitionV1 SpeciesFor(AnimalFunctionalProfileV1 profileId)
        => profileId switch
        {
            AnimalFunctionalProfileV1.Herbivore or AnimalFunctionalProfileV1.Predator
                => CreatureSpeciesCatalogV1.Quadruped,
            _ => throw new ArgumentOutOfRangeException(nameof(profileId), profileId,
                "No prototype genome mapping exists for this functional profile.")
        };

    private static Dictionary<int, List<AnimalAgentV1>> BuildAnimalSpatialIndex(AnimalAgentV1[] animals)
    {
        var index = new Dictionary<int, List<AnimalAgentV1>>();
        foreach (var animal in animals)
        {
            var cellIndex = CellIndex(animal.PositionXM, animal.PositionYM);
            if (!index.TryGetValue(cellIndex, out var list)) index[cellIndex] = list = [];
            list.Add(animal);
        }
        return index;
    }

    private static AnimalAgentV1? FindNearestPrey(
        AnimalAgentV1 predator,
        Dictionary<int, List<AnimalAgentV1>> spatialIndex)
    {
        var nearestDistance = double.PositiveInfinity;
        AnimalAgentV1? nearest = null;
        foreach (var prey in NearbyAnimals(predator.PositionXM, predator.PositionYM, spatialIndex))
        {
            if (prey.FunctionalProfileId != AnimalFunctionalProfileV1.Herbivore) continue;
            var distance = Distance(predator.PositionXM, predator.PositionYM, prey.PositionXM, prey.PositionYM);
            if (distance > PerceptionRadiusM) continue;
            if (distance < nearestDistance || (distance == nearestDistance
                && (nearest is null || prey.AnimalId < nearest.Value.AnimalId)))
            {
                nearestDistance = distance;
                nearest = prey;
            }
        }
        return nearest;
    }

    private static IEnumerable<AnimalAgentV1> NearbyAnimals(
        double x,
        double y,
        Dictionary<int, List<AnimalAgentV1>> spatialIndex)
    {
        var minX = Math.Max(0, (int)Math.Floor((x - PerceptionRadiusM) / CellSizeM));
        var maxX = Math.Min(SimulationState.Width - 1, (int)Math.Floor((x + PerceptionRadiusM) / CellSizeM));
        var minY = Math.Max(0, (int)Math.Floor((y - PerceptionRadiusM) / CellSizeM));
        var maxY = Math.Min(SimulationState.Height - 1, (int)Math.Floor((y + PerceptionRadiusM) / CellSizeM));
        for (var cellY = minY; cellY <= maxY; cellY++)
        for (var cellX = minX; cellX <= maxX; cellX++)
        {
            var cellIndex = cellY * SimulationState.Width + cellX;
            if (spatialIndex.TryGetValue(cellIndex, out var occupants))
                foreach (var animal in occupants) yield return animal;
        }
    }

    private static ForageTarget? FindBestPlantTarget(ZoologyCellStateV1[] cells, AnimalAgentV1 animal)
    {
        var minX = Math.Max(0, (int)Math.Floor((animal.PositionXM - PerceptionRadiusM) / CellSizeM));
        var maxX = Math.Min(SimulationState.Width - 1, (int)Math.Floor((animal.PositionXM + PerceptionRadiusM) / CellSizeM));
        var minY = Math.Max(0, (int)Math.Floor((animal.PositionYM - PerceptionRadiusM) / CellSizeM));
        var maxY = Math.Min(SimulationState.Height - 1, (int)Math.Floor((animal.PositionYM + PerceptionRadiusM) / CellSizeM));
        ForageTarget? best = null;
        for (var cellY = minY; cellY <= maxY; cellY++)
        for (var cellX = minX; cellX <= maxX; cellX++)
        {
            var cell = cells[cellY * SimulationState.Width + cellX];
            ConsiderPlant(cell, PlantKind.Grass, cell.Grass, 0.80);
            ConsiderPlant(cell, PlantKind.Woody, cell.Woody, 0.15);
        }
        return best;

        void ConsiderPlant(ZoologyCellStateV1 cell, PlantKind kind, PlantPatch patch, double edibleFraction)
        {
            var edibleMass = patch.BiomassGPerM2 * SimulationState.CellAreaM2 * edibleFraction;
            if (edibleMass <= 0) return;
            var x = (cell.X + 0.5) * CellSizeM;
            var y = (cell.Y + 0.5) * CellSizeM;
            var distance = Distance(animal.PositionXM, animal.PositionYM, x, y);
            if (distance > PerceptionRadiusM) return;
            var score = edibleMass / (distance + 10);
            if (best is { } current && score <= current.Score) return;
            best = new(cell.Index, kind, x, y, score);
        }
    }

    private static CreatureMovementResultV1? ResolveTerrestrialMovement(
        ZoologyCellStateV1[] cells,
        ICreatureMovementSolverV1 movementSolver,
        AnimalAgentV1 animal,
        (double X, double Y) target,
        double maximumTravelDistanceM,
        ref CreatureMovementRouteSampleV1[]? routeEnvironmentGrid)
    {
        if (maximumTravelDistanceM <= 0) return null;
        var distance = Distance(animal.PositionXM, animal.PositionYM, target.X, target.Y);
        if (distance <= 1e-10) return null;
        var sampleCount = Math.Max(1, (int)Math.Ceiling(distance / RouteSampleSpacingM));
        var route = new CreatureMovementRouteSampleV1[sampleCount + 1];
        for (var sampleIndex = 0; sampleIndex <= sampleCount; sampleIndex++)
        {
            var fraction = (double)sampleIndex / sampleCount;
            var x = animal.PositionXM + (target.X - animal.PositionXM) * fraction;
            var z = animal.PositionYM + (target.Y - animal.PositionYM) * fraction;
            route[sampleIndex] = new(x, z,
                SampleObservation(cells, x, z, cell => cell.GroundElevationM),
                SampleObservation(cells, x, z, cell => cell.SurfaceWaterMm) / 1000);
        }

        var movementRequest = new CreatureMovementRequestV1(
            CreatureFamilyV1.Quadruped,
            CreatureMovementHabitatV1.Terrestrial,
            route,
            maximumTravelDistanceM,
            PrototypeMaximumSlopeGrade,
            PrototypeMaximumWadeDepthM,
            0,
            RouteSampleSpacingM);
        var directResult = movementSolver.ResolveMovement(movementRequest);
        if (directResult.TargetReached
            || directResult.StopReason is not (CreatureMovementStopReasonV1.SlopeLimit
                or CreatureMovementStopReasonV1.HabitatBoundary))
            return directResult;

        routeEnvironmentGrid ??= cells.Select(cell => new CreatureMovementRouteSampleV1(
                (cell.X + 0.5) * CellSizeM,
                (cell.Y + 0.5) * CellSizeM,
                cell.GroundElevationM,
                cell.SurfaceWaterMm / 1000))
            .ToArray();
        var alternativeRoute = CreatureTerrainRoutePlannerV1.PlanTerrestrialRoute(
            routeEnvironmentGrid,
            SimulationState.Width,
            SimulationState.Height,
            CellSizeM,
            route[0],
            route[^1],
            PrototypeMaximumSlopeGrade,
            PrototypeMaximumWadeDepthM,
            RouteSampleSpacingM);
        if (alternativeRoute is null) return directResult;

        return movementSolver.ResolveMovement(movementRequest with { RouteSamples = alternativeRoute });
    }

    private static double SampleObservation(
        ZoologyCellStateV1[] cells,
        double x,
        double z,
        Func<ZoologyCellStateV1, double> selector)
    {
        // World cells store point samples at their centers. Bilinear interpolation keeps route
        // segments smooth without making Zoology an owner of either environmental field.
        var gridX = Math.Clamp(x / CellSizeM - 0.5, 0, SimulationState.Width - 1);
        var gridY = Math.Clamp(z / CellSizeM - 0.5, 0, SimulationState.Height - 1);
        var x0 = (int)Math.Floor(gridX);
        var y0 = (int)Math.Floor(gridY);
        var x1 = Math.Min(x0 + 1, SimulationState.Width - 1);
        var y1 = Math.Min(y0 + 1, SimulationState.Height - 1);
        var tx = gridX - x0;
        var ty = gridY - y0;
        var top = Lerp(selector(cells[y0 * SimulationState.Width + x0]),
            selector(cells[y0 * SimulationState.Width + x1]), tx);
        var bottom = Lerp(selector(cells[y1 * SimulationState.Width + x0]),
            selector(cells[y1 * SimulationState.Width + x1]), tx);
        return Lerp(top, bottom, ty);
    }

    private static double Lerp(double first, double second, double fraction)
        => first + (second - first) * fraction;

    private static void AddFood(Dictionary<ulong, Food> food, ulong animalId, double dryG, double nitrogenG)
    {
        var existing = food.GetValueOrDefault(animalId);
        food[animalId] = new(existing.DryG + dryG, existing.NitrogenG + nitrogenG);
    }

    private static ZoologyCellStateV1 GetCell(
        ZoologyCellStateV1[] source,
        Dictionary<int, ZoologyCellStateV1> changes,
        int cellIndex)
        => changes.GetValueOrDefault(cellIndex, source[cellIndex]);

    private static void AddDetritus(
        ZoologyCellStateV1[] source,
        Dictionary<int, ZoologyCellStateV1> changes,
        int cellIndex,
        double dryG,
        double nitrogenG)
    {
        if (dryG <= 0 && nitrogenG <= 0) return;
        var cell = GetCell(source, changes, cellIndex);
        changes[cellIndex] = cell with
        {
            DetritusDryGPerM2 = cell.DetritusDryGPerM2 + Math.Max(0, dryG) / SimulationState.CellAreaM2,
            DetritusNitrogenGPerM2 = cell.DetritusNitrogenGPerM2 + Math.Max(0, nitrogenG) / SimulationState.CellAreaM2
        };
    }

    private static int CellIndex(double x, double y)
    {
        var cellX = Math.Clamp((int)(x / CellSizeM), 0, SimulationState.Width - 1);
        var cellY = Math.Clamp((int)(y / CellSizeM), 0, SimulationState.Height - 1);
        return cellY * SimulationState.Width + cellX;
    }

    private static double Distance(double x1, double y1, double x2, double y2)
        => Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));

    private static Profile GetProfile(AnimalFunctionalProfileV1 profile) => profile switch
    {
        AnimalFunctionalProfileV1.Herbivore => new(5000, 8000, 0.025, 0.45, 0.65, 0.020,
            0.02, 180, 30, 0.05),
        AnimalFunctionalProfileV1.Predator => new(12000, 20000, 0.015, 0.70, 0.80, 0.025,
            0.05, 365, 60, 0.05),
        _ => throw new ArgumentOutOfRangeException(nameof(profile))
    };
}
