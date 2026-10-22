using Game.Contracts;

namespace Game.Modules.Atmosphere;

public sealed class AtmosphereModuleV1 : IClimateForcingV1, IAtmosphericTemperatureV1
{
    public IReadOnlyList<ClimateHour> ForecastDay(ulong seed, int dayOfYear, long absoluteHourStart, int[] hoursSinceRain)
        => Array.AsReadOnly(ClimateModel.ForecastDay(seed, dayOfYear, absoluteHourStart, hoursSinceRain).ByHourAndTile);

    public double RelativeHumidityProxy(double cloudCover, double soilWaterMm, double soilCapacityMm)
        => ClimateModel.RelativeHumidityProxy(cloudCover, soilWaterMm, soilCapacityMm);

    public double SeasonalMeanTemperatureC(int dayOfYear, double elevationM)
        => TemperatureModel.SeasonalMeanTemperatureC(dayOfYear, elevationM);

    public double DiurnalTemperatureOffsetC(double solarHour)
        => TemperatureModel.DiurnalTemperatureOffsetC(solarHour);

    public double AirTemperatureC(int dayOfYear, double solarHour, double elevationM)
        => TemperatureModel.AirTemperatureC(dayOfYear, solarHour, elevationM);
}
