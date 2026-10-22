using Game.Contracts;
using Game.Modules.Geosphere;
using Game.Procedural;

namespace Game.Client.Desktop;

/// <summary>
/// Builds seed-stable, presentation-only relief patterns over the published terrain field.
/// Nested height details follow local contours, while wet channels, standing water, beaches,
/// and authored outcrop rims keep the surface they already define.
/// </summary>
internal static class ProceduralReliefDetailField
{
    private const ulong DomainWarpTag = 0x52454C4945465750UL;
    private const ulong BroadLandformTag = 0x52454C4945464231UL;
    private const ulong MesoLandformTag = 0x52454C4945464D31UL;
    private const ulong ContourRidgeTag = 0x52454C4945465231UL;

    public static double[] Build(WorldSnapshot world, double[]? channelIncision, double cellSizeM)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!double.IsFinite(cellSizeM) || cellSizeM <= 0)
            throw new ArgumentOutOfRangeException(nameof(cellSizeM));
        if (channelIncision is not null && channelIncision.Length != world.Cells.Count)
            throw new ArgumentException("Channel incision must contain one value per world cell.", nameof(channelIncision));

        var heights = new double[world.Cells.Count];
        var wet = new double[world.Cells.Count];
        for (var index = 0; index < world.Cells.Count; index++)
        {
            heights[index] = world.Cells[index].ElevationM;
            wet[index] = SmoothStep(48, 380, world.Cells[index].SurfaceWaterMm);
        }
        wet = SmoothWaterMask(wet, world.Width, world.Height);
        var orientationHeights = SmoothHeightField(heights, world.Width, world.Height);

        var underlying = TerrainModel.CreateUnderlyingElevationSampler(world.Seed);
        var detail = new double[world.Cells.Count];
        for (var y = 0; y < world.Height; y++)
        for (var x = 0; x < world.Width; x++)
        {
            var index = y * world.Width + x;
            var cell = world.Cells[index];
            var west = orientationHeights[y * world.Width + Math.Max(0, x - 1)];
            var east = orientationHeights[y * world.Width + Math.Min(world.Width - 1, x + 1)];
            var south = orientationHeights[Math.Max(0, y - 1) * world.Width + x];
            var north = orientationHeights[Math.Min(world.Height - 1, y + 1) * world.Width + x];
            var eastRun = Math.Max(1, Math.Min(world.Width - 1, x + 1) - Math.Max(0, x - 1)) * cellSizeM;
            var northRun = Math.Max(1, Math.Min(world.Height - 1, y + 1) - Math.Max(0, y - 1)) * cellSizeM;
            var gradientEast = (east - west) / eastRun;
            var gradientNorth = (north - south) / northRun;
            var slope = Math.Sqrt(gradientEast * gradientEast + gradientNorth * gradientNorth);
            var slopeWeight = SmoothStep(0.025, 0.17, slope);

            // Contour-parallel strokes break into irregular sections under a broad domain warp.
            // Their long axis follows the height gradient's tangent rather than the map grid.
            var gridX = (int)Math.Round((x + 0.5) * 4);
            var gridY = (int)Math.Round((y + 0.5) * 4);
            var warpEast = CounterRandom.SpatialUnit(world.Seed, DomainWarpTag,
                gridX, gridY, 144, 0, 1);
            var warpNorth = CounterRandom.SpatialUnit(world.Seed, DomainWarpTag,
                gridX, gridY, 144, 0, 2);
            var sampleEast = x + 0.5 + (warpEast - 0.5) * 8;
            var sampleNorth = y + 0.5 + (warpNorth - 0.5) * 8;

            var normalEast = slope > 1e-5 ? gradientEast / slope : 0;
            var normalNorth = slope > 1e-5 ? gradientNorth / slope : 1;
            var tangentEast = -normalNorth;
            var tangentNorth = normalEast;
            var along = (sampleEast + 512) * tangentEast + (sampleNorth + 512) * tangentNorth;
            var across = (sampleEast + 512) * normalEast + (sampleNorth + 512) * normalNorth;
            var ridgeX = (int)Math.Round(along * 4);
            var ridgeY = (int)Math.Round(across * 12);
            var ridgeNoise = CounterRandom.SpatialUnit(world.Seed, ContourRidgeTag,
                ridgeX, ridgeY, 96, 0, 1);
            var ridgeShape = 1 - Math.Abs(2 * ridgeNoise - 1);
            var ridgeBreaks = CounterRandom.SpatialUnit(world.Seed, ContourRidgeTag,
                gridX, gridY, 256, 0, 2);

            var warpedX = (int)Math.Round((sampleEast + 512) * 4);
            var warpedY = (int)Math.Round((sampleNorth + 512) * 4);
            var broad = CounterRandom.SpatialUnit(world.Seed, BroadLandformTag,
                warpedX, warpedY, 320, 0, 1);
            var meso = CounterRandom.SpatialUnit(world.Seed, MesoLandformTag,
                warpedX, warpedY, 104, 0, 1);

            var waterProtection = 1 - SmoothStep(0.06, 0.72,
                Sample(wet, world.Width, world.Height, x, y));
            var channelProtection = channelIncision is null
                ? 1
                : 1 - SmoothStep(0.08, 1.6, channelIncision[index]);
            var coastProtection = SmoothStep(1.5, 6.0,
                ProceduralIslandShape.SignedDistanceCells(world.Seed, x + 0.5, y + 0.5,
                    world.Width, world.Height));
            var outcropDifference = Math.Abs(cell.ElevationM - underlying(x + 0.5, y + 0.5));
            var outcropProtection = 1 - SmoothStep(2, 15, outcropDifference);
            var mask = waterProtection * channelProtection * coastProtection * outcropProtection;

            // Broad lowland rolls need enough vertical range to read at the reference camera
            // distance; the smaller band breaks them into overlapping natural shoulders.
            var broadRoll = (broad - 0.5) * (60 + slopeWeight * 16);
            var mesoRoll = (meso - 0.5) * (28 + slopeWeight * 8);
            var contourRelief = (ridgeShape - 0.5) * 2
                * (1.5 + slopeWeight * 2.5) * (0.58 + ridgeBreaks * 0.42);
            detail[index] = (broadRoll + mesoRoll + contourRelief) * mask;
        }

        return SmoothHeightField(detail, world.Width, world.Height);
    }

    private static double[] SmoothHeightField(double[] source, int width, int height)
    {
        ReadOnlySpan<int> weights = [1, 4, 6, 4, 1];
        var smoothed = new double[source.Length];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var value = 0.0;
            for (var oy = -2; oy <= 2; oy++)
            for (var ox = -2; ox <= 2; ox++)
            {
                var sampleX = Math.Clamp(x + ox, 0, width - 1);
                var sampleY = Math.Clamp(y + oy, 0, height - 1);
                value += source[sampleY * width + sampleX] * weights[oy + 2] * weights[ox + 2];
            }
            smoothed[y * width + x] = value / 256;
        }
        return smoothed;
    }

    private static double[] SmoothWaterMask(double[] source, int width, int height)
    {
        for (var pass = 0; pass < 2; pass++)
        {
            var result = new double[source.Length];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var value = source[y * width + x] * 0.40;
                value += source[y * width + Math.Max(0, x - 1)] * 0.10;
                value += source[y * width + Math.Min(width - 1, x + 1)] * 0.10;
                value += source[Math.Max(0, y - 1) * width + x] * 0.10;
                value += source[Math.Min(height - 1, y + 1) * width + x] * 0.10;
                value += source[Math.Max(0, y - 1) * width + Math.Max(0, x - 1)] * 0.05;
                value += source[Math.Max(0, y - 1) * width + Math.Min(width - 1, x + 1)] * 0.05;
                value += source[Math.Min(height - 1, y + 1) * width + Math.Max(0, x - 1)] * 0.05;
                value += source[Math.Min(height - 1, y + 1) * width + Math.Min(width - 1, x + 1)] * 0.05;
                result[y * width + x] = value;
            }
            source = result;
        }
        return source;
    }

    private static double Sample(double[] field, int width, int height, double x, double y)
    {
        x = Math.Clamp(x, 0, width - 1);
        y = Math.Clamp(y, 0, height - 1);
        var x0 = (int)x;
        var y0 = (int)y;
        var x1 = Math.Min(width - 1, x0 + 1);
        var y1 = Math.Min(height - 1, y0 + 1);
        var tx = x - x0;
        var ty = y - y0;
        var top = field[y0 * width + x0] + (field[y0 * width + x1] - field[y0 * width + x0]) * tx;
        var bottom = field[y1 * width + x0] + (field[y1 * width + x1] - field[y1 * width + x0]) * tx;
        return top + (bottom - top) * ty;
    }

    private static double SmoothStep(double edge0, double edge1, double value)
    {
        var t = Math.Clamp((value - edge0) / (edge1 - edge0), 0, 1);
        return t * t * (3 - 2 * t);
    }
}
