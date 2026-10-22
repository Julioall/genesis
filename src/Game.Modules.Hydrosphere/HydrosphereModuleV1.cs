using Game.Contracts;

namespace Game.Modules.Hydrosphere;

public sealed class HydrosphereModuleV1 : ISurfaceWaterV1
{
    public SurfaceWaterStateV1[] SeedInitialWater(SurfaceWaterCellV1[] terrain)
        => SurfaceWaterModel.SeedInitialWater(terrain)
            .Select(cell => new SurfaceWaterStateV1(cell.Index, cell.SurfaceWaterMm, cell.GroundwaterMm))
            .ToArray();

    public SurfaceWaterRoutingPlanV1 CreateRoutingPlan(SurfaceWaterCellV1[] terrain)
        => SurfaceWaterModel.CreateRoutingPlan(terrain);

    public SurfaceWaterExchangeResultV1 ReceivePrecipitation(
        SurfaceWaterStateV1 water,
        double precipitationMm,
        double plantCover,
        double soilInfiltrationCapacityMm,
        double potentialEtMm)
        => SurfaceWaterModel.ReceivePrecipitation(water, precipitationMm, plantCover,
            soilInfiltrationCapacityMm, potentialEtMm);

    public SurfaceFlowResultV1 Route(SurfaceWaterCellV1[] source)
        => SurfaceWaterModel.Route(source);

    public SurfaceFlowResultV1 Route(SurfaceWaterCellV1[] source, SurfaceWaterRoutingPlanV1 routingPlan)
        => SurfaceWaterModel.Route(source, routingPlan);

    public SurfaceWaterWithdrawalResultV1 AllocateAnimalWater(
        SurfaceWaterCellV1[] source,
        SurfaceWaterDemandV1[] demands)
        => SurfaceWaterModel.AllocateAnimalWater(source, demands);

    public SurfaceWaterStateV1[] DepositAnimalWater(
        SurfaceWaterCellV1[] source,
        SurfaceWaterDepositV1[] deposits)
        => SurfaceWaterModel.DepositAnimalWater(source, deposits);
}
