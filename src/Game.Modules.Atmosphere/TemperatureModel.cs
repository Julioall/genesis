using Game.Contracts;

namespace Game.Modules.Atmosphere;

public static class TemperatureModel
{
    public static double SeasonalMeanTemperatureC(int dayOfYear, double elevationM)
    {
        ValidateDayAndElevation(dayOfYear, elevationM);
        return 11 + 12 * Math.Cos(2 * Math.PI * (dayOfYear - 200) / 365.0) - 0.0065 * elevationM;
    }

    public static double AirTemperatureC(int dayOfYear, double solarHour, double elevationM)
    {
        ValidateDayAndElevation(dayOfYear, elevationM);
        return SeasonalMeanTemperatureC(dayOfYear, elevationM) + DiurnalTemperatureOffsetC(solarHour);
    }

    public static double DiurnalTemperatureOffsetC(double solarHour)
    {
        if (!double.IsFinite(solarHour) || solarHour is < 0 or >= 24)
            throw new ArgumentOutOfRangeException(nameof(solarHour), "Solar hour must be finite and in [0, 24).");
        return 5 * Math.Cos(2 * Math.PI * (solarHour - 15) / 24.0);
    }

    private static void ValidateDayAndElevation(int dayOfYear, double elevationM)
    {
        if (dayOfYear is < 1 or > 365)
            throw new ArgumentOutOfRangeException(nameof(dayOfYear), "Day of year must be in [1, 365].");
        if (!double.IsFinite(elevationM))
            throw new ArgumentOutOfRangeException(nameof(elevationM), "Elevation must be finite and expressed in metres.");
    }
}
