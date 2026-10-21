using Game.Contracts;

namespace Game.Modules.Geosphere;

public readonly record struct SoilWaterResult(
    double SoilWaterMm,
    double SurfaceWaterMm,
    double PlantAvailableWaterMm,
    double WaterFactorAvailable,
    double OpenWaterEvaporationMm,
    double SoilEvaporationMm,
    double InterceptionMm,
    double PlantWaterDemandMultiplier);

public static class SoilModel
{
    public static (double CapacityMm, double WiltMm, double KsatMmPerHour, double InitialWaterMm, double InitialN, double InitialSom, double InitialSomN)
        Profile(SoilKind kind) => kind switch
        {
            SoilKind.Sandy => (100, 20, 20, 55, 2, 500, 5),
            _ => (180, 35, 10, 90, 4, 1500, 22.5)
        };

    public static SoilWaterResult ReceiveRainAndEvaporate(
        WorldCell cell, double precipitationMm, double plantCover, double potentialEtMm)
    {
        var intercepted = 0.15 * plantCover * precipitationMm;
        var availableSurface = cell.SurfaceWaterMm + precipitationMm - intercepted;
        var infiltration = Math.Min(availableSurface,
            Math.Min(cell.SoilKsatMmPerHour, cell.SoilCapacityMm - cell.SoilWaterMm));
        var soilBeforeEvaporation = cell.SoilWaterMm + infiltration;
        var surfaceBeforeEvaporation = availableSurface - infiltration;

        var waterCover = Math.Clamp(surfaceBeforeEvaporation / 10, 0, 1);
        var plantMultiplier = 1 - waterCover;
        var soilFraction = (1 - plantCover) * plantMultiplier;
        var waterFraction = waterCover;
        var waterEvaporation = Math.Min(surfaceBeforeEvaporation, potentialEtMm * waterFraction);
        var soilEvaporation = Math.Min(Math.Max(0, soilBeforeEvaporation - cell.SoilWiltMm), potentialEtMm * soilFraction);
        var availableForPlants = Math.Max(0, soilBeforeEvaporation - soilEvaporation - cell.SoilWiltMm);
        var availableFactor = Math.Clamp(availableForPlants / (0.25 * (cell.SoilCapacityMm - cell.SoilWiltMm)), 0, 1);

        return new(
            soilBeforeEvaporation - soilEvaporation,
            surfaceBeforeEvaporation - waterEvaporation,
            availableForPlants,
            availableFactor,
            waterEvaporation,
            soilEvaporation,
            intercepted,
            plantMultiplier);
    }
}
