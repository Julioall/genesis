using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game.Client.Desktop;

internal static class ProceduralBeachMeshBuilder
{
    public static VertexPositionColor[] BuildBeach(CoastlinePoint[] coastline,
        Func<double, double, double> elevationSampler, double cellSizeM, double seaLevelM)
    {
        ArgumentNullException.ThrowIfNull(coastline);
        ArgumentNullException.ThrowIfNull(elevationSampler);
        if (coastline.Length < 3 || cellSizeM <= 0 || !double.IsFinite(cellSizeM))
            throw new ArgumentOutOfRangeException(nameof(cellSizeM), "Beach geometry dimensions must be valid.");

        const double beachWidthCells = 3.0;
        var vertices = new VertexPositionColor[coastline.Length * 6];
        var offset = 0;
        for (var index = 0; index < coastline.Length; index++)
        {
            var current = coastline[index];
            var next = coastline[(index + 1) % coastline.Length];
            var currentInner = Inward(current, beachWidthCells);
            var nextInner = Inward(next, beachWidthCells);
            var currentOuterPosition = Position(current.EastCell, current.NorthCell, 0.16);
            var nextOuterPosition = Position(next.EastCell, next.NorthCell, 0.16);
            var currentInnerPosition = Position(currentInner.East, currentInner.North, 0.16);
            var nextInnerPosition = Position(nextInner.East, nextInner.North, 0.16);

            var alongshore = 0.5 + 0.5 * Math.Sin(index * 0.19 + current.Phase);
            var wetSand = new Color(139, 148, 113);
            var drySand = Color.Lerp(new Color(183, 163, 115), new Color(207, 190, 144), (float)alongshore);
            vertices[offset++] = new VertexPositionColor(currentOuterPosition, wetSand);
            vertices[offset++] = new VertexPositionColor(currentInnerPosition, drySand);
            vertices[offset++] = new VertexPositionColor(nextOuterPosition, wetSand);
            vertices[offset++] = new VertexPositionColor(nextOuterPosition, wetSand);
            vertices[offset++] = new VertexPositionColor(currentInnerPosition, drySand);
            vertices[offset++] = new VertexPositionColor(nextInnerPosition, drySand);
        }

        return vertices;

        Vector3 Position(double east, double north, double lift)
        {
            var height = Math.Max(seaLevelM + 0.035, elevationSampler(east, north) + lift);
            return new Vector3((float)(east * cellSizeM), (float)height, (float)(north * cellSizeM));
        }

        static (double East, double North) Inward(CoastlinePoint point, double amount)
            => (point.EastCell - point.NormalEast * amount,
                point.NorthCell - point.NormalNorth * amount);
    }

    public static VertexPositionColor[] BuildWaveFoam(CoastlinePoint[] coastline,
        Func<double, double, double> elevationSampler, double cellSizeM, double seaLevelM,
        double elapsedSeconds)
    {
        ArgumentNullException.ThrowIfNull(coastline);
        ArgumentNullException.ThrowIfNull(elevationSampler);
        var vertices = new VertexPositionColor[coastline.Length * 6];
        var offset = 0;
        for (var index = 0; index < coastline.Length; index++)
        {
            var current = coastline[index];
            var next = coastline[(index + 1) % coastline.Length];
            var currentWave = Wave(index);
            var nextWave = Wave((index + 1) % coastline.Length);
            var currentInner = Offset(current, currentWave.Ripple - currentWave.Width * 0.5);
            var currentOuter = Offset(current, currentWave.Ripple + currentWave.Width * 0.5);
            var nextInner = Offset(next, nextWave.Ripple - nextWave.Width * 0.5);
            var nextOuter = Offset(next, nextWave.Ripple + nextWave.Width * 0.5);
            var innerPosition = Position(currentInner.East, currentInner.North);
            var outerPosition = Position(currentOuter.East, currentOuter.North);
            var nextInnerPosition = Position(nextInner.East, nextInner.North);
            var nextOuterPosition = Position(nextOuter.East, nextOuter.North);
            var pulse = (float)(0.72 + 0.28 * (0.5 + 0.5 * Math.Sin(elapsedSeconds * 1.4 + currentWave.Phase)));
            var transparent = new Color((byte)218, (byte)240, (byte)226, (byte)12);
            var crest = new Color((byte)229, (byte)243, (byte)226,
                (byte)Math.Clamp((int)Math.Round(118 * pulse * currentWave.Mask), 0, 255));
            vertices[offset++] = new VertexPositionColor(innerPosition, transparent);
            vertices[offset++] = new VertexPositionColor(outerPosition, crest);
            vertices[offset++] = new VertexPositionColor(nextInnerPosition, transparent);
            vertices[offset++] = new VertexPositionColor(nextInnerPosition, transparent);
            vertices[offset++] = new VertexPositionColor(outerPosition, crest);
            vertices[offset++] = new VertexPositionColor(nextOuterPosition, crest);
        }

        return vertices;

        Vector3 Position(double east, double north)
        {
            var ground = elevationSampler(east, north);
            var height = Math.Max(seaLevelM + 0.12, ground + 0.12);
            return new Vector3((float)(east * cellSizeM), (float)height, (float)(north * cellSizeM));
        }

        static (double East, double North) Offset(CoastlinePoint point, double amount)
            => (point.EastCell + point.NormalEast * amount,
                point.NorthCell + point.NormalNorth * amount);

        static double SmoothStep(double edge0, double edge1, double value)
        {
            var t = Math.Clamp((value - edge0) / (edge1 - edge0), 0, 1);
            return t * t * (3 - 2 * t);
        }

        (double Ripple, double Width, double Mask, double Phase) Wave(int pointIndex)
        {
            var alongshore = pointIndex / (double)coastline.Length * Math.Tau;
            var phase = elapsedSeconds * 1.15 + alongshore * 7.0;
            var crestSignal = Math.Sin(phase) + 0.38 * Math.Sin(elapsedSeconds * 1.72 - alongshore * 11.0);
            var mask = SmoothStep(0.34, 1.12, crestSignal);
            var ripple = 0.12 * Math.Sin(phase) + 0.06 * Math.Sin(elapsedSeconds * 1.72 - alongshore * 11.0);
            return (ripple, 0.10 + 0.40 * mask, mask, phase);
        }
    }
}
