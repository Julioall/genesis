using Game.Contracts;
using Game.Procedural;

namespace Game.Modules.Atmosphere;

public sealed record ClimateDay(ClimateHour[] ByHourAndTile);

public static class ClimateModel
{
    public static ClimateDay ForecastDay(ulong seed, int dayOfYear, long absoluteHourStart, int[] hoursSinceRain)
    {
        if (hoursSinceRain.Length != 16) throw new ArgumentException("L0 expects 16 climate tiles.", nameof(hoursSinceRain));
        var forecast = new ClimateHour[24 * 16];
        var ages = (int[])hoursSinceRain.Clone();
        var seasonalWetness = 1 + 0.40 * Math.Cos(2 * Math.PI * (dayOfYear - 20) / 365.0);
        var wetProbability = 0.045 * seasonalWetness;

        for (var hour = 0; hour < 24; hour++)
        for (var tile = 0; tile < 16; tile++)
        {
            var absoluteHour = (ulong)(absoluteHourStart + hour);
            var cloud = Math.Clamp(0.20 + 0.65 * Math.Exp(-ages[tile] / 4.0), 0, 0.85);
            var wet = CounterRandom.Unit(seed, CounterRandom.ClimateTag, (ulong)tile, 0, absoluteHour, 0);
            var draw = CounterRandom.Unit(seed, CounterRandom.ClimateTag, (ulong)tile, 0, absoluteHour, 1);
            var rain = wet >= wetProbability ? 0 : Math.Min(12, -1.5 * Math.Log(1 - draw));
            forecast[hour * 16 + tile] = new(rain, cloud);
            ages[tile] = rain > 0 ? 0 : ages[tile] + 1;
        }

        return new(forecast);
    }

    public static double RelativeHumidityProxy(double cloudCover, double soilWaterMm, double soilCapacityMm)
        => Math.Clamp(0.35 + 0.45 * cloudCover + 0.15 * soilWaterMm / soilCapacityMm, 0.20, 0.95);
}
