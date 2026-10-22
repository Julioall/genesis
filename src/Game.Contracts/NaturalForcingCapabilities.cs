namespace Game.Contracts;

/// <summary>Cosmos v1 capability that publishes an instantaneous solar forcing sample.</summary>
public interface ISolarForcingV1
{
    SolarForcingSampleV1 At(int dayOfYear, double solarHour, double latitudeDegrees = 45.0);
}

/// <summary>Atmosphere v1 capabilities used by the hourly L0 scheduler.</summary>
public interface IClimateForcingV1
{
    /// <summary>
    /// Returns 24 hourly precipitation accumulations in mm and cloud-cover fractions [0, 1]
    /// for each of the 16 climate tiles, in hour-major then tile-major order.
    /// </summary>
    IReadOnlyList<ClimateHour> ForecastDay(ulong seed, int dayOfYear, long absoluteHourStart, int[] hoursSinceRain);

    /// <summary>Returns the L0 relative-humidity proxy in [0, 1] from cloud and soil-water inputs.</summary>
    double RelativeHumidityProxy(double cloudCover, double soilWaterMm, double soilCapacityMm);
}

/// <summary>Atmosphere v1 capability that provides L0 daily mean and hourly air temperature.</summary>
public interface IAtmosphericTemperatureV1
{
    double SeasonalMeanTemperatureC(int dayOfYear, double elevationM);
    double DiurnalTemperatureOffsetC(double solarHour);
    double AirTemperatureC(int dayOfYear, double solarHour, double elevationM);
}
