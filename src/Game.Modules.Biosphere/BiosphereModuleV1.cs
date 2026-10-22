using Game.Contracts;

namespace Game.Modules.Biosphere;

public sealed class BiosphereModuleV1 : IBotanyV1, IEcologyV1, IZoologyV1, IZoologyGeneticsV1, ICreatureDevelopmentV1,
    ICreaturePoseSolverV1, ICreatureMovementSolverV1, ICreatureEnergyBudgetV1
{
    private readonly CreatureDevelopmentModelV1 _creatureDevelopment = new();
    private readonly CreaturePoseSolverV1 _creaturePoseSolver = new();
    private readonly CreatureMovementSolverV1 _creatureMovementSolver = new();
    private readonly CreatureEnergyBudgetModelV1 _creatureEnergyBudget = new();

    public double TotalCover(PlantPatchPair plants) => BotanyModel.TotalCover(plants);

    public PlantCoverV1 CoverComponents(PlantPatchPair plants) => BotanyModel.CoverComponents(plants);

    public BotanyCellStateV1[] DisperseSeeds(BotanyCellStateV1[] states, BotanyHourResultV1[] hourlyResults)
        => BotanyModel.DisperseSeeds(states, hourlyResults);

    public BotanyHourResultV1 GrowHour(BotanyHourInputV1 input) => BotanyModel.GrowHour(input);

    public BotanyHourResultV1 GrowHour(BotanyHourInputV1 input, PlantCoverV1 cover)
        => BotanyModel.GrowHour(input, cover);

    public HerbivoryResolutionV1 ResolveHerbivory(PlantPatch patch, PlantKind kind, HerbivoryRequestV1[] requests)
        => BotanyModel.ResolveHerbivory(patch, kind, requests);

    public PlantPatchPair[] CreateInitialPatches(
        ulong seed,
        double[] elevationsM,
        double[] surfaceWaterMm,
        double[] terrainSlopeMagnitude)
        => BotanyModel.CreateInitialPatches(seed, elevationsM, surfaceWaterMm, terrainSlopeMagnitude);

    public EcologyDailyResultV1 CloseDay(EcologyDailyInputV1 input) => EcologyModel.CloseDay(input);

    public EcologyDetritusStateV1 ConfirmDetritusDeposit(
        EcologyDetritusStateV1 current,
        EcologyDetritusStateV1 proposedDeposit)
        => EcologyModel.ConfirmDetritusDeposit(current, proposedDeposit);

    public AnimalAgentV1[] CreateInitialPopulation(ulong seed, ZoologyCellStateV1[] cells, InitialAnimalPopulationV1 population)
        => ZoologyModel.CreateInitialPopulation(seed, cells, population);

    public AnimalAgentV1[] EnsureGenomes(ulong seed, AnimalAgentV1[] animals)
        => ZoologyModel.EnsureGenomes(seed, animals);

    public ZoologyTickResultV1 AdvanceTick(ZoologyTickInputV1 input)
        => ZoologyModel.AdvanceTick(input, this, _creatureMovementSolver, _creatureEnergyBudget);

    public ZoologyWaterPreparationV1 PrepareDailyWater(AnimalAgentV1[] animals)
        => ZoologyModel.PrepareDailyWater(animals);

    public ZoologyDailyResultV1 CloseDay(ZoologyDailyInputV1 input) => ZoologyModel.CloseDay(input);

    public CreatureGenomeV1 CreateGenome(
        CreatureSpeciesDefinitionV1 species,
        ulong anatomySeed,
        ulong pigmentSeed)
        => _creatureDevelopment.CreateGenome(species, anatomySeed, pigmentSeed);

    public CreatureGenomeV1 CreateGenomeFromWorldSeed(
        CreatureSpeciesDefinitionV1 species,
        ulong worldSeed,
        ulong individualId)
        => _creatureDevelopment.CreateGenomeFromWorldSeed(species, worldSeed, individualId);

    public CreatureGenomeV1 MutateGenome(
        CreatureSpeciesDefinitionV1 species,
        CreatureGenomeV1 parent,
        ulong mutationSeed)
        => _creatureDevelopment.MutateGenome(species, parent, mutationSeed);

    public CreatureDevelopmentResultV1 Develop(
        CreatureSpeciesDefinitionV1 species,
        CreatureGenomeV1 genome,
        CreatureDevelopmentContextV1 context)
        => _creatureDevelopment.Develop(species, genome, context);

    public QuadrupedLimbPoseV1 SolveQuadrupedLimb(
        DevelopedBodyV1 anatomy,
        QuadrupedLimbV1 limb,
        CreatureVector3V1 footContactTargetM,
        double bendSign)
        => _creaturePoseSolver.SolveQuadrupedLimb(anatomy, limb, footContactTargetM, bendSign);

    public CreatureFishSwimmingPoseV1 BuildFishSwimmingPose(
        DevelopedBodyV1 anatomy,
        double phase01,
        double amplitude01)
        => _creaturePoseSolver.BuildFishSwimmingPose(anatomy, phase01, amplitude01);

    public CreatureMovementResultV1 ResolveMovement(CreatureMovementRequestV1 request)
        => _creatureMovementSolver.ResolveMovement(request);

    public CreatureEnergyBudgetOutcomeV1 ResolveEnergyBudget(CreatureEnergyBudgetInputV1 input)
        => _creatureEnergyBudget.ResolveEnergyBudget(input);
}
