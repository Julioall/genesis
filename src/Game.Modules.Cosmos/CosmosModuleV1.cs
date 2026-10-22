using Game.Contracts;

namespace Game.Modules.Cosmos;

public sealed class CosmosModuleV1 : ISolarForcingV1
{
    public SolarForcingSampleV1 At(int dayOfYear, double solarHour, double latitudeDegrees = 45.0)
        => SolarGeometry.At(dayOfYear, solarHour, latitudeDegrees);
}
