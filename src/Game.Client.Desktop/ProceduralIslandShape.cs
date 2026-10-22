using Game.Procedural;

namespace Game.Client.Desktop;

internal readonly record struct CoastlinePoint(
    double EastCell,
    double NorthCell,
    double NormalEast,
    double NormalNorth,
    double Phase);

/// <summary>Render-only, seed-stable coastline for the island silhouette.</summary>
internal static class ProceduralIslandShape
{
    private const ulong CoastNoiseTag = 0x49534C414E44434FUL;
    private const ulong CoastPhaseTag = 0x53484F5245504841UL;

    public static double SignedDistanceCells(ulong seed, double eastCell, double northCell,
        int worldWidthCells, int worldHeightCells)
    {
        GetShapeParameters(seed, worldWidthCells, worldHeightCells,
            out var centerEast, out var centerNorth, out var radiusEast, out var radiusNorth, out var phase);
        var dx = eastCell - centerEast;
        var dz = northCell - centerNorth;
        var angle = Math.Atan2(dz / radiusNorth, dx / radiusEast);
        var normalizedRadius = Math.Sqrt(dx * dx / (radiusEast * radiusEast)
            + dz * dz / (radiusNorth * radiusNorth)) / RadialScale(angle, phase);
        var baseDistance = (1 - normalizedRadius) * Math.Min(radiusEast, radiusNorth);

        // Seeded broad lobes establish headlands and coves; coherent noise adds smaller
        // shoreline detail without leaving straight map edges or changing scientific fields.
        var sampleX = (int)Math.Round((eastCell + 512) * 4);
        var sampleY = (int)Math.Round((northCell + 512) * 4);
        var broad = CounterRandom.SpatialUnit(seed, CoastNoiseTag, sampleX, sampleY, 64, 0, 1);
        var middle = CounterRandom.SpatialUnit(seed, CoastNoiseTag, sampleX, sampleY, 24, 0, 2);
        var fine = CounterRandom.SpatialUnit(seed, CoastNoiseTag, sampleX, sampleY, 10, 0, 3);
        var shorelineDisplacement = (broad - 0.5) * 7.0
            + (middle - 0.5) * 3.0
            + (fine - 0.5) * 1.2;
        return baseDistance + shorelineDisplacement;
    }

    public static CoastlinePoint[] BuildCoastline(ulong seed, int worldWidthCells, int worldHeightCells,
        int segments = 512)
    {
        if (worldWidthCells < 4 || worldHeightCells < 4 || segments < 64)
            throw new ArgumentOutOfRangeException(nameof(segments), "Island dimensions and coastline resolution must be valid.");

        GetShapeParameters(seed, worldWidthCells, worldHeightCells,
            out var centerEast, out var centerNorth, out var radiusEast, out var radiusNorth, out var phase);
        var points = new CoastlinePoint[segments];
        for (var segment = 0; segment < segments; segment++)
        {
            var angle = Math.Tau * segment / segments;
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            var low = 0.75;
            var high = 1.30;
            for (var iteration = 0; iteration < 24; iteration++)
            {
                var middle = (low + high) * 0.5;
                var east = centerEast + cos * radiusEast * middle;
                var north = centerNorth + sin * radiusNorth * middle;
                if (SignedDistanceCells(seed, east, north, worldWidthCells, worldHeightCells) > 0)
                    low = middle;
                else
                    high = middle;
            }

            var ratio = (low + high) * 0.5;
            var coastEast = centerEast + cos * radiusEast * ratio;
            var coastNorth = centerNorth + sin * radiusNorth * ratio;
            var wavePhase = CounterRandom.Unit(seed, CoastPhaseTag, (ulong)segment, 0, 0, 0) * Math.Tau;
            points[segment] = new CoastlinePoint(coastEast, coastNorth, 0, 0, wavePhase);
        }

        // Use the actual coastline tangent so beach and foam follow the irregular bays.
        for (var segment = 0; segment < segments; segment++)
        {
            var previous = points[(segment + segments - 1) % segments];
            var next = points[(segment + 1) % segments];
            var tangentEast = next.EastCell - previous.EastCell;
            var tangentNorth = next.NorthCell - previous.NorthCell;
            var normalEast = tangentNorth;
            var normalNorth = -tangentEast;
            var normalLength = Math.Sqrt(normalEast * normalEast + normalNorth * normalNorth);
            points[segment] = points[segment] with
            {
                NormalEast = normalEast / normalLength,
                NormalNorth = normalNorth / normalLength
            };
        }

        return points;
    }

    private static void GetShapeParameters(ulong seed, int worldWidthCells, int worldHeightCells,
        out double centerEast, out double centerNorth, out double radiusEast, out double radiusNorth,
        out double phase)
    {
        centerEast = (worldWidthCells - 1) * 0.5
            + (CounterRandom.Unit(seed, CoastPhaseTag, 0x43454E544552584FUL, 0, 0, 0) - 0.5) * 6;
        centerNorth = (worldHeightCells - 1) * 0.5
            + (CounterRandom.Unit(seed, CoastPhaseTag, 0x43454E5445525AUL, 0, 0, 0) - 0.5) * 6;
        radiusEast = worldWidthCells * 0.390;
        radiusNorth = worldHeightCells * 0.375;
        phase = CounterRandom.Unit(seed, CoastPhaseTag, 0x4C4F424550484153UL, 0, 0, 0) * Math.Tau;
    }

    private static double RadialScale(double angle, double phase)
        => 1
            + 0.052 * Math.Sin(3 * angle + phase)
            + 0.034 * Math.Sin(5 * angle - phase * 0.71)
            + 0.022 * Math.Sin(2 * angle + phase * 1.83);
}
