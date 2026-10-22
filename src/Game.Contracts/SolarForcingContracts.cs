namespace Game.Contracts;

/// <summary>
/// Instantaneous solar forcing published by Cosmos for a one-based day of year,
/// solar hour in [0, 24), and latitude in degrees north. Direction components
/// are unitless east/north/up components; irradiance is W/m² above the atmosphere.
/// This sample is an instant, not an accumulation over a simulation window.
/// </summary>
public readonly record struct SolarForcingSampleV1(
    int DayOfYear,
    double SolarHour,
    double LatitudeDegrees,
    double EastDirection,
    double NorthDirection,
    double UpDirection,
    double TopOfAtmosphereIrradianceWPerM2);
