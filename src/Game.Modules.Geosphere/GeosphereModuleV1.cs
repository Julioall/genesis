using Game.Contracts;

namespace Game.Modules.Geosphere;

public sealed class GeosphereModuleV1 : IGeosphereTerrainV1, ISoilWaterV1, ISoilBiogeochemistryV1
{
    public double[] GenerateElevationField(ulong seed) => TerrainModel.GenerateElevationField(seed);

    public double SlopeMagnitude(double[] elevations, int x, int y)
        => TerrainModel.SlopeMagnitude(elevations, x, y);

    public double TerrainSolarFactor(double[] elevationsM, int x, int y, double sunEast, double sunNorth, double sunUp)
        => TerrainModel.TerrainSolarFactor(elevationsM, x, y, sunEast, sunNorth, sunUp);

    public TerrainSolarNormalV1[] CreateTerrainSolarNormals(double[] elevationsM)
        => TerrainModel.CreateTerrainSolarNormals(elevationsM);

    public double[] TerrainSolarFactors(TerrainSolarNormalV1[] normals,
        double sunEast, double sunNorth, double sunUp)
        => TerrainModel.TerrainSolarFactors(normals, sunEast, sunNorth, sunUp);

    public SoilProfileV1 Profile(SoilKind kind)
    {
        var profile = SoilModel.Profile(kind);
        return new(profile.CapacityMm, profile.WiltMm, profile.KsatMmPerHour,
            profile.InitialWaterMm, profile.InitialN, profile.InitialSom, profile.InitialSomN);
    }

    public double MaximumInfiltrationMm(SoilHydraulicStateV1 soil)
        => SoilModel.MaximumInfiltrationMm(soil);

    public SoilWaterResultV1 ApplyInfiltrationAndEvaporate(
        SoilHydraulicStateV1 soil,
        double infiltrationMm,
        double plantCover,
        double potentialEtMm,
        double plantWaterDemandMultiplier)
        => SoilModel.ApplyInfiltrationAndEvaporate(soil, infiltrationMm, plantCover,
            potentialEtMm, plantWaterDemandMultiplier);

    public SoilTranspirationResultV1 ApplyPlantTranspiration(
        SoilHydraulicStateV1 soil, double proposedTranspirationMm)
        => SoilModel.ApplyPlantTranspiration(soil, proposedTranspirationMm);

    public double ConfirmMineralNitrogenAfterGrowth(
        double availableMineralNitrogenGPerM2, double proposedRemainingMineralNitrogenGPerM2)
        => SoilModel.ConfirmMineralNitrogenAfterGrowth(availableMineralNitrogenGPerM2,
            proposedRemainingMineralNitrogenGPerM2);

    public double DepositMineralNitrogen(double currentMineralNitrogenGPerM2, double proposedDepositGPerM2)
        => SoilModel.DepositMineralNitrogen(currentMineralNitrogenGPerM2, proposedDepositGPerM2);

    public SoilBiogeochemistryStateV1 ConfirmEcologyProposal(
        SoilBiogeochemistryStateV1 current, SoilBiogeochemistryProposalV1 proposal)
        => SoilModel.ConfirmEcologyProposal(current, proposal);
}
