using Game.Contracts;
using Game.Procedural;

namespace Game.Client.Desktop;

internal readonly record struct VisualBiomeWeights(float DrySurface, float Marsh);

/// <summary>
/// Renderer-only terrain cues derived from published snapshot values. They never change
/// published ecological presence/biomass, physical fields, or the simulation random streams.
/// </summary>
internal static class VisualBiomeField
{
    private const ulong FlatlandTag = 0x564953504C41494EUL;
    private const ulong FlatlandReliefTag = 0x564953504C414E52UL;

    public static VisualBiomeWeights[] Build(WorldSnapshot world)
    {
        var weights = new VisualBiomeWeights[world.Cells.Count];
        var rawMarsh = new float[weights.Length];
        for (var index = 0; index < weights.Length; index++)
        {
            var cell = world.Cells[index];
            var soilSaturation = Math.Clamp(cell.SoilWaterMm / Math.Max(1, cell.SoilCapacityMm), 0, 1.5);
            var surfaceWater = SmoothStep(24, 220, cell.SurfaceWaterMm);
            var moisture = Math.Max(SmoothStep(0.72, 1.12, soilSaturation) * 0.72, surfaceWater * 0.92);
            var belowBank = 1 - SmoothStep(105, 170, cell.ElevationM);
            rawMarsh[index] = (float)(moisture * (0.68 + belowBank * 0.32));

            // Surface dryness is a current snapshot cue, not a climatological desert
            // classification. Plant presence and pigments must continue to come from
            // published ecological state rather than this rendering field.
            var drySurface = (1 - SmoothStep(0.24, 0.62, soilSaturation))
                * (1 - surfaceWater * 0.92);

            weights[index] = new VisualBiomeWeights((float)Math.Clamp(drySurface, 0, 1), 0);
        }

        var marsh = Smooth(rawMarsh, world.Width, world.Height);
        for (var index = 0; index < weights.Length; index++)
            weights[index] = weights[index] with { Marsh = Math.Clamp(marsh[index], 0, 1) };
        return weights;
    }

    public static double FlatlandRegionWeight(ulong seed, double eastCell, double northCell,
        int width, int height)
        => RegionWeight(seed, FlatlandTag, eastCell, northCell, width, height,
            centerX: 0.47, centerY: 0.57, radiusX: 0.14, radiusY: 0.20);

    public static double FlattenForPresentation(ulong seed, double eastCell, double northCell,
        int width, int height, double elevationM, double flatlandLevelM)
    {
        var region = FlatlandRegionWeight(seed, eastCell, northCell, width, height);
        if (region <= 0) return elevationM;
        var noiseX = (int)Math.Round(eastCell * 4);
        var noiseY = (int)Math.Round(northCell * 4);
        var broadRelief = CounterRandom.SpatialUnit(seed, FlatlandReliefTag,
            noiseX, noiseY, 144, 0, 1);
        var rollingRelief = CounterRandom.SpatialUnit(seed, FlatlandReliefTag,
            noiseX, noiseY, 52, 0, 2);
        var microRelief = CounterRandom.SpatialUnit(seed, FlatlandReliefTag,
            noiseX, noiseY, 16, 0, 3);
        var gentleUndulation = (broadRelief - 0.5) * 56 + (rollingRelief - 0.5) * 20;
        var target = flatlandLevelM + gentleUndulation;
        // Keep recognizable lowland flats, but let more of the snapshot's surrounding relief
        // shape the display. All added detail is renderer-only; physical terrain, drainage,
        // and ecological fields remain untouched.
        var lowland = elevationM + (target - elevationM) * (region * 0.28);
        // Add smooth 360 m, 130 m, and 40 m bands across the rendered landform. They put
        // gentle rolls back into broad meadows and give the fine terrain mesh readable slopes.
        var rollingDetail = (broadRelief - 0.5) * 18
            + (rollingRelief - 0.5) * 8
            + (microRelief - 0.5) * 2.4;
        return lowland + rollingDetail * (0.72 + region * 0.28);
    }

    private static double RegionWeight(ulong seed, ulong tag, double eastCell, double northCell,
        int width, int height, double centerX, double centerY, double radiusX, double radiusY)
    {
        var dx = (eastCell - width * centerX) / (width * radiusX);
        var dy = (northCell - height * centerY) / (height * radiusY);
        var normalizedDistance = Math.Sqrt(dx * dx + dy * dy);
        var noiseX = (int)Math.Round(eastCell * 4);
        var noiseY = (int)Math.Round(northCell * 4);
        var broad = CounterRandom.SpatialUnit(seed, tag, noiseX, noiseY, 144, 0, 1);
        var middle = CounterRandom.SpatialUnit(seed, tag, noiseX, noiseY, 52, 0, 2);
        var displacedDistance = normalizedDistance + (broad - 0.5) * 0.22 + (middle - 0.5) * 0.10;
        return 1 - SmoothStep(0.74, 1.19, displacedDistance);
    }

    private static float[] Smooth(float[] source, int width, int height)
    {
        for (var pass = 0; pass < 2; pass++)
        {
            var result = new float[source.Length];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var index = y * width + x;
                var value = source[index] * 0.40f;
                value += source[y * width + Math.Max(0, x - 1)] * 0.10f;
                value += source[y * width + Math.Min(width - 1, x + 1)] * 0.10f;
                value += source[Math.Max(0, y - 1) * width + x] * 0.10f;
                value += source[Math.Min(height - 1, y + 1) * width + x] * 0.10f;
                value += source[Math.Max(0, y - 1) * width + Math.Max(0, x - 1)] * 0.05f;
                value += source[Math.Max(0, y - 1) * width + Math.Min(width - 1, x + 1)] * 0.05f;
                value += source[Math.Min(height - 1, y + 1) * width + Math.Max(0, x - 1)] * 0.05f;
                value += source[Math.Min(height - 1, y + 1) * width + Math.Min(width - 1, x + 1)] * 0.05f;
                result[index] = value;
            }
            source = result;
        }
        return source;
    }

    private static double SmoothStep(double edge0, double edge1, double value)
    {
        var t = Math.Clamp((value - edge0) / (edge1 - edge0), 0, 1);
        return t * t * (3 - 2 * t);
    }
}
