namespace Game.Contracts;

public readonly record struct SoilProfileV1(
    double CapacityMm,
    double WiltMm,
    double KsatMmPerHour,
    double InitialWaterMm,
    double InitialMineralNitrogenGPerM2,
    double InitialSoilOrganicMatterGPerM2,
    double InitialSoilOrganicNitrogenGPerM2);

/// <summary>Soil-owned hydraulic state required to evaluate one hourly water exchange.</summary>
public readonly record struct SoilHydraulicStateV1(
    double SoilWaterMm,
    double CapacityMm,
    double WiltMm,
    double KsatMmPerHour);

/// <summary>Geosphere-owned soil moisture observations used by daily ecological processes.</summary>
public readonly record struct SoilMoistureContextV1(double SoilWaterMm, double CapacityMm, double WiltMm);

/// <summary>Geosphere-owned soil organic matter and mineral nitrogen stocks.</summary>
public readonly record struct SoilBiogeochemistryStateV1(
    double SoilOrganicMatterGPerM2,
    double SoilOrganicNitrogenGPerM2,
    double MineralNitrogenGPerM2);

/// <summary>Biosphere proposal for Geosphere-owned soil biogeochemistry stocks.</summary>
public readonly record struct SoilBiogeochemistryProposalV1(
    double SoilOrganicMatterGPerM2,
    double SoilOrganicNitrogenGPerM2,
    double MineralNitrogenGPerM2);

/// <summary>Soil-owned state and availability returned after accepted infiltration and evaporation.</summary>
public readonly record struct SoilWaterResultV1(
    double SoilWaterMm,
    double PlantAvailableWaterMm,
    double WaterFactorAvailable,
    double SoilEvaporationMm);

public readonly record struct SoilTranspirationResultV1(double SoilWaterMm, double ConfirmedTranspirationMm);

/// <summary>Terrain and water observations Hydrosphere needs without borrowing the aggregate world cell.</summary>
public readonly record struct SurfaceWaterCellV1(
    int Index,
    int X,
    int Y,
    double ElevationM,
    double SurfaceWaterMm,
    double GroundwaterMm);

/// <summary>Hydrosphere-owned water stores returned for a single cell.</summary>
public readonly record struct SurfaceWaterStateV1(
    int CellIndex,
    double SurfaceWaterMm,
    double GroundwaterMm);

/// <summary>Unit terrain normal at one cell, in east, north, up coordinates.</summary>
public readonly record struct TerrainSolarNormalV1(double East, double North, double Up);

/// <summary>Surface-water changes and boundary fluxes proposed for one precipitation step.</summary>
public readonly record struct SurfaceWaterExchangeResultV1(
    double SurfaceWaterMm,
    double InfiltrationMm,
    double OpenWaterEvaporationMm,
    double InterceptionMm,
    double PlantWaterDemandMultiplier);

public readonly record struct SurfaceFlowResultV1(SurfaceWaterStateV1[] Cells, double OutflowLiters);
public readonly record struct SurfaceWaterRoutingPlanV1(double[] ElevationM, double[] FlowAccumulation);

/// <summary>A cell-scoped surface-water request from a stable consumer.</summary>
public readonly record struct SurfaceWaterDemandV1(int CellIndex, ulong ConsumerId, double RequestedLiters);
public readonly record struct SurfaceWaterGrantV1(ulong ConsumerId, double GrantedLiters);
public readonly record struct SurfaceWaterDepositV1(int CellIndex, double Liters);
public readonly record struct SurfaceWaterWithdrawalResultV1(
    SurfaceWaterStateV1[] Cells,
    SurfaceWaterGrantV1[] Grants);

/// <summary>Geosphere v1 terrain capabilities used by the current L0 simulation.</summary>
public interface IGeosphereTerrainV1
{
    double[] GenerateElevationField(ulong seed);
    double SlopeMagnitude(double[] elevations, int x, int y);
    double TerrainSolarFactor(double[] elevationsM, int x, int y, double sunEast, double sunNorth, double sunUp);
    TerrainSolarNormalV1[] CreateTerrainSolarNormals(double[] elevationsM);
    double[] TerrainSolarFactors(TerrainSolarNormalV1[] normals, double sunEast, double sunNorth, double sunUp);
}

/// <summary>Geosphere v1 owner of soil profiles, infiltration, and evaporation.</summary>
public interface ISoilWaterV1
{
    SoilProfileV1 Profile(SoilKind kind);
    double MaximumInfiltrationMm(SoilHydraulicStateV1 soil);
    SoilWaterResultV1 ApplyInfiltrationAndEvaporate(
        SoilHydraulicStateV1 soil,
        double infiltrationMm,
        double plantCover,
        double potentialEtMm,
        double plantWaterDemandMultiplier);
    SoilTranspirationResultV1 ApplyPlantTranspiration(SoilHydraulicStateV1 soil, double proposedTranspirationMm);
}

/// <summary>Geosphere owner that confirms Biosphere proposals against soil nitrogen and organic-matter stocks.</summary>
public interface ISoilBiogeochemistryV1
{
    double ConfirmMineralNitrogenAfterGrowth(double availableMineralNitrogenGPerM2,
        double proposedRemainingMineralNitrogenGPerM2);
    double DepositMineralNitrogen(double currentMineralNitrogenGPerM2, double proposedDepositGPerM2);
    SoilBiogeochemistryStateV1 ConfirmEcologyProposal(
        SoilBiogeochemistryStateV1 current, SoilBiogeochemistryProposalV1 proposal);
}

/// <summary>Hydrosphere v1 owner of surface-water seeding and routing.</summary>
public interface ISurfaceWaterV1
{
    SurfaceWaterStateV1[] SeedInitialWater(SurfaceWaterCellV1[] terrain);
    SurfaceWaterRoutingPlanV1 CreateRoutingPlan(SurfaceWaterCellV1[] terrain);
    SurfaceWaterExchangeResultV1 ReceivePrecipitation(
        SurfaceWaterStateV1 water,
        double precipitationMm,
        double plantCover,
        double soilInfiltrationCapacityMm,
        double potentialEtMm);
    SurfaceFlowResultV1 Route(SurfaceWaterCellV1[] source);
    SurfaceFlowResultV1 Route(SurfaceWaterCellV1[] source, SurfaceWaterRoutingPlanV1 routingPlan);
    SurfaceWaterWithdrawalResultV1 AllocateAnimalWater(
        SurfaceWaterCellV1[] source,
        SurfaceWaterDemandV1[] demands);
    SurfaceWaterStateV1[] DepositAnimalWater(
        SurfaceWaterCellV1[] source,
        SurfaceWaterDepositV1[] deposits);
}
