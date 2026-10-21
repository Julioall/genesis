namespace Game.Modules.Cosmos;

public readonly record struct SolarSample(double SinAltitude, double East, double North, double Up, double TopOfAtmosphereWPerM2);

public static class SolarGeometry
{
    public static SolarSample At(int dayOfYear, double solarHour, double latitudeDegrees = 45.0)
    {
        var radians = Math.PI / 180.0;
        var phi = latitudeDegrees * radians;
        var declination = 23.44 * radians * Math.Sin(2 * Math.PI * (dayOfYear - 80) / 365.0);
        var hourAngle = 15.0 * radians * (solarHour - 12.0);
        var sinAltitude = Math.Sin(phi) * Math.Sin(declination)
                          + Math.Cos(phi) * Math.Cos(declination) * Math.Cos(hourAngle);
        var east = -Math.Cos(declination) * Math.Sin(hourAngle);
        var north = Math.Cos(phi) * Math.Sin(declination)
                    - Math.Sin(phi) * Math.Cos(declination) * Math.Cos(hourAngle);
        var up = sinAltitude;
        var distanceFactor = 1 + 0.033 * Math.Cos(2 * Math.PI * (dayOfYear - 3) / 365.0);
        var top = 1361 * distanceFactor * Math.Max(0, sinAltitude);
        return new(sinAltitude, east, north, up, top);
    }

    public static double SeasonTemperatureC(int dayOfYear, double elevationM)
        => 11 + 12 * Math.Cos(2 * Math.PI * (dayOfYear - 200) / 365.0) - 0.0065 * elevationM;

    public static double AirTemperatureC(int dayOfYear, double solarHour, double elevationM)
        => SeasonTemperatureC(dayOfYear, elevationM) + 5 * Math.Cos(2 * Math.PI * (solarHour - 15) / 24.0);
}
