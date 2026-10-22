using Game.Contracts;

namespace Game.Modules.Cosmos;

public static class SolarGeometry
{
    public static SolarForcingSampleV1 At(int dayOfYear, double solarHour, double latitudeDegrees = 45.0)
    {
        if (dayOfYear is < 1 or > 365)
            throw new ArgumentOutOfRangeException(nameof(dayOfYear), "Day of year must be in [1, 365].");
        if (!double.IsFinite(solarHour) || solarHour is < 0 or >= 24)
            throw new ArgumentOutOfRangeException(nameof(solarHour), "Solar hour must be finite and in [0, 24).");
        if (!double.IsFinite(latitudeDegrees) || latitudeDegrees is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitudeDegrees), "Latitude must be finite and in [-90, 90] degrees.");

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
        return new(dayOfYear, solarHour, latitudeDegrees, east, north, up, top);
    }
}
