namespace Game.Contracts;

public readonly record struct PlantPatchPair(PlantPatch Grass, PlantPatch Woody);
public readonly record struct PlantCoverV1(double Grass, double Woody)
{
    public double Total => 1 - (1 - Grass) * (1 - Woody);
}

public readonly record struct BotanyDailyFactorsV1(
    double GrassWaterFactorSum,
    double GrassNitrogenFactorSum,
    double GrassTemperatureFactorSum,
    double GrassOutOfRangeHours,
    double WoodyWaterFactorSum,
    double WoodyNitrogenFactorSum,
    double WoodyTemperatureFactorSum,
    double WoodyOutOfRangeHours);

public readonly record struct BotanyCellStateV1(PlantPatchPair Plants, BotanyDailyFactorsV1 DailyFactors);

public readonly record struct BotanyHourInputV1(
    BotanyCellStateV1 State,
    double MineralNitrogenGPerM2,
    double AirTemperatureC,
    double SurfaceShortwaveMjPerM2,
    double PotentialEtMm,
    double AvailableWaterMm,
    double WaterAvailabilityFactor,
    double PlantWaterDemandMultiplier);

public readonly record struct BotanyHourResultV1(
    BotanyCellStateV1 State,
    double TranspirationMm,
    double MineralNitrogenAfterGrowthProposalGPerM2,
    double NewBiomassGPerM2,
    double GrassSeedProducedGPerM2,
    double WoodySeedProducedGPerM2);

public readonly record struct EcologyDailyInputV1(
    BotanyCellStateV1 Botany,
    double DetritusDryGPerM2,
    double DetritusNitrogenGPerM2,
    SoilMoistureContextV1 SoilMoisture,
    SoilBiogeochemistryStateV1 SoilBiogeochemistry,
    double MeanAirTemperatureC);

public readonly record struct EcologyDailyResultV1(
    BotanyCellStateV1 Botany,
    double DetritusDryGPerM2,
    double DetritusNitrogenGPerM2,
    SoilBiogeochemistryProposalV1 SoilProposal,
    double DetritusRespirationGPerM2,
    double SoilOrganicMatterRespirationGPerM2);

public readonly record struct EcologyDetritusStateV1(double DryGPerM2, double NitrogenGPerM2);

public enum AnimalFunctionalProfileV1 : byte { Herbivore, Predator }

/// <summary>Persistent L0 animal state. Nitrogen is derived from structural mass and its profile.</summary>
public readonly record struct AnimalAgentV1(
    ulong AnimalId,
    AnimalFunctionalProfileV1 FunctionalProfileId,
    double PositionXM,
    double PositionYM,
    double AgeDays,
    double StructuralDryG,
    double ReserveDryG,
    double HydrationL,
    double MaintenanceDeficitDays,
    double DehydrationDeficitDays,
    double ReproductionCooldownDays,
    long LastCaptureDay,
    CreatureGenomeV1? Genome = null);

public readonly record struct InitialAnimalPopulationV1(int Herbivores, int Predators);

/// <summary>Zoology stocks and read-only terrain/water observations supplied by their owning modules.</summary>
public readonly record struct ZoologyCellStateV1(
    int Index,
    int X,
    int Y,
    PlantPatch Grass,
    PlantPatch Woody,
    double MineralNitrogenGPerM2,
    double DetritusDryGPerM2,
    double DetritusNitrogenGPerM2,
    double GroundElevationM = 0,
    double SurfaceWaterMm = 0);

public readonly record struct HerbivoryRequestV1(ulong AnimalId, double RequestedDryG);
public readonly record struct HerbivoryGrantV1(ulong AnimalId, double GrantedDryG, double GrantedNitrogenG);
public readonly record struct HerbivoryResolutionV1(PlantPatch UpdatedPatch, HerbivoryGrantV1[] Grants);

public readonly record struct ZoologyWaterPreparationV1(
    AnimalAgentV1[] AnimalsAfterExcretion,
    SurfaceWaterDemandV1[] WaterRequests,
    double ExcretionWaterLiters);

public readonly record struct ZoologyTickInputV1(
    ulong Seed,
    long TickIndex,
    ZoologyCellStateV1[] Cells,
    AnimalAgentV1[] Animals,
    ulong NextAnimalId);

public readonly record struct ZoologyTickResultV1(
    ZoologyCellStateV1[] CellChanges,
    AnimalAgentV1[] Animals,
    ulong NextAnimalId,
    double AnimalRespirationDryGrams,
    SurfaceWaterDepositV1[] WaterDeposits,
    double AnimalEnergyConsumedJ = 0,
    double FecalFoodEnergyJ = 0,
    double UnmetEnergyDemandJ = 0);

public readonly record struct ZoologyDailyInputV1(
    ulong Seed,
    long TickIndex,
    long AbsoluteDay,
    ZoologyCellStateV1[] Cells,
    AnimalAgentV1[] Animals,
    SurfaceWaterGrantV1[] WaterGrants,
    ulong NextAnimalId);

public readonly record struct ZoologyDailyResultV1(
    ZoologyCellStateV1[] CellChanges,
    AnimalAgentV1[] Animals,
    ulong NextAnimalId,
    SurfaceWaterDepositV1[] WaterDeposits);

/// <summary>Biosphere v1 Botany capabilities for the L0 plant-patch runtime.</summary>
public interface IBotanyV1
{
    double TotalCover(PlantPatchPair plants);
    PlantCoverV1 CoverComponents(PlantPatchPair plants);
    BotanyCellStateV1[] DisperseSeeds(BotanyCellStateV1[] states, BotanyHourResultV1[] hourlyResults);
    BotanyHourResultV1 GrowHour(BotanyHourInputV1 input);
    BotanyHourResultV1 GrowHour(BotanyHourInputV1 input, PlantCoverV1 cover);
    HerbivoryResolutionV1 ResolveHerbivory(PlantPatch patch, PlantKind kind, HerbivoryRequestV1[] requests);

    PlantPatchPair[] CreateInitialPatches(
        ulong seed,
        double[] elevationsM,
        double[] surfaceWaterMm,
        double[] terrainSlopeMagnitude);
}

/// <summary>Biosphere v1 Ecology daily-close capability for plant patches and decomposition.</summary>
public interface IEcologyV1
{
    EcologyDailyResultV1 CloseDay(EcologyDailyInputV1 input);
    EcologyDetritusStateV1 ConfirmDetritusDeposit(
        EcologyDetritusStateV1 current,
        EcologyDetritusStateV1 proposedDeposit);
}

/// <summary>Biosphere v1 Zoology capabilities for persistent L0 consumer agents.</summary>
public interface IZoologyV1
{
    AnimalAgentV1[] CreateInitialPopulation(ulong seed, ZoologyCellStateV1[] cells, InitialAnimalPopulationV1 population);
    ZoologyTickResultV1 AdvanceTick(ZoologyTickInputV1 input);
    ZoologyWaterPreparationV1 PrepareDailyWater(AnimalAgentV1[] animals);
    ZoologyDailyResultV1 CloseDay(ZoologyDailyInputV1 input);
}

/// <summary>Optional migration and persistence support for genotypes attached to N3 animals.</summary>
public interface IZoologyGeneticsV1
{
    AnimalAgentV1[] EnsureGenomes(ulong seed, AnimalAgentV1[] animals);
}
