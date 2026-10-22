using Game.Procedural;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game.Client.Desktop;

internal readonly record struct ProceduralBackdropMeshData(VertexPositionColor[] Vertices, int[] Indices)
{
    public int PrimitiveCount => Indices.Length / 3;
}

/// <summary>Extends the rendered island beyond the simulation edge and tapers it into the sea.</summary>
internal static class ProceduralBackdropMeshBuilder
{
    private const double CoastFalloffStartCells = -4.5;
    private const double CoastFalloffEndCells = 1.0;
    private const double UnderseaDepthM = 20.0;

    public static ProceduralBackdropMeshData Build(
        Func<double, double, double> elevationSampler,
        Color[] edgeTexture,
        int textureWidth,
        int textureHeight,
        ulong seed,
        int startXCells,
        int startYCells,
        int cellsWide,
        int cellsHigh,
        int subdivisionsPerCell,
        int worldWidthCells,
        int worldHeightCells,
        double cellSizeM,
        double seaLevelM)
    {
        ArgumentNullException.ThrowIfNull(elevationSampler);
        ArgumentNullException.ThrowIfNull(edgeTexture);
        if (textureWidth <= 0 || textureHeight <= 0 || edgeTexture.Length != textureWidth * textureHeight
            || cellsWide <= 0 || cellsHigh <= 0 || subdivisionsPerCell <= 0
            || worldWidthCells <= 0 || worldHeightCells <= 0
            || !double.IsFinite(cellSizeM) || cellSizeM <= 0 || !double.IsFinite(seaLevelM))
            throw new ArgumentOutOfRangeException(nameof(cellsWide), "Backdrop dimensions, texture, subdivision, and scale must be valid.");

        var columns = checked(cellsWide * subdivisionsPerCell);
        var rows = checked(cellsHigh * subdivisionsPerCell);
        var stride = columns + 1;
        var vertices = new VertexPositionColor[checked(stride * (rows + 1))];
        var edgeAverage = AveragePerimeter(edgeTexture, textureWidth, textureHeight);
        for (var z = 0; z <= rows; z++)
        for (var x = 0; x <= columns; x++)
        {
            var gridX = startXCells + x / (double)subdivisionsPerCell;
            var gridY = startYCells + z / (double)subdivisionsPerCell;
            var outsideX = Math.Max(0, Math.Max(-gridX, gridX - worldWidthCells));
            var outsideY = Math.Max(0, Math.Max(-gridY, gridY - worldHeightCells));
            var outsideDistance = Math.Sqrt(outsideX * outsideX + outsideY * outsideY);
            var coastNoiseX = (int)Math.Round((gridX + 512) * 4);
            var coastNoiseY = (int)Math.Round((gridY + 512) * 4);
            var coastlineNoise = CounterRandom.SpatialUnit(seed, 0x434F4153544C494EUL,
                coastNoiseX, coastNoiseY, 80, 0, 1);
            var coastExtensionCells = 6 + (coastlineNoise - 0.5) * 6;
            var distanceToCoast = outsideDistance - coastExtensionCells;
            var landElevation = elevationSampler(gridX, gridY);
            var underseaElevation = seaLevelM - UnderseaDepthM;
            var coastBlend = SmoothStep(CoastFalloffStartCells, CoastFalloffEndCells, distanceToCoast);
            var coastalElevation = landElevation + (underseaElevation - landElevation) * coastBlend;
            var position = new Vector3((float)(gridX * cellSizeM), (float)coastalElevation,
                (float)(gridY * cellSizeM));
            var color = SampleBackdropColor(gridX, gridY, outsideDistance, distanceToCoast);
            vertices[z * stride + x] = new VertexPositionColor(position, color);
        }

        var indices = new int[checked(columns * rows * 6)];
        var index = 0;
        for (var z = 0; z < rows; z++)
        for (var x = 0; x < columns; x++)
        {
            var northwest = z * stride + x;
            var northeast = northwest + 1;
            var southwest = northwest + stride;
            var southeast = southwest + 1;
            indices[index++] = northwest;
            indices[index++] = southwest;
            indices[index++] = northeast;
            indices[index++] = northeast;
            indices[index++] = southwest;
            indices[index++] = southeast;
        }

        return new ProceduralBackdropMeshData(vertices, indices);

        Color SampleBackdropColor(double gridX, double gridY, double outsideDistance, double distanceToCoast)
        {
            var u = Math.Clamp(gridX / worldWidthCells, 0, 1);
            var v = Math.Clamp(gridY / worldHeightCells, 0, 1);
            var pixelX = u * (textureWidth - 1);
            var pixelY = v * (textureHeight - 1);
            var x0 = (int)pixelX;
            var y0 = (int)pixelY;
            var x1 = Math.Min(textureWidth - 1, x0 + 1);
            var y1 = Math.Min(textureHeight - 1, y0 + 1);
            var edgeColor = Color.Lerp(
                Color.Lerp(edgeTexture[y0 * textureWidth + x0], edgeTexture[y0 * textureWidth + x1], (float)(pixelX - x0)),
                Color.Lerp(edgeTexture[y1 * textureWidth + x0], edgeTexture[y1 * textureWidth + x1], (float)(pixelX - x0)),
                (float)(pixelY - y0));

            var blend = SmoothStep(0.15, 6.0, outsideDistance);
            var color = Color.Lerp(edgeColor, edgeAverage, (float)blend);
            var noiseX = (int)Math.Round((gridX + 512) * 4);
            var noiseY = (int)Math.Round((gridY + 512) * 4);
            var broad = CounterRandom.SpatialUnit(seed, 0x4241434B44524F50UL, noiseX, noiseY, 120, 0, 1);
            var fine = CounterRandom.SpatialUnit(seed, 0x4241434B44524F50UL, noiseX, noiseY, 24, 0, 2);
            var textureVariation = (broad - 0.5) * 0.24 + (fine - 0.5) * 0.10;
            color = Shade(color, (float)(1 + blend * textureVariation));

            var beach = SmoothStep(-3.1, -2.3, distanceToCoast)
                * (1 - SmoothStep(-1.1, -0.25, distanceToCoast));
            return Color.Lerp(color, new Color(166, 151, 105), (float)(beach * 0.88));
        }
    }

    private static Color AveragePerimeter(Color[] pixels, int width, int height)
    {
        long red = 0;
        long green = 0;
        long blue = 0;
        var count = 0;
        void Add(Color color)
        {
            red += color.R;
            green += color.G;
            blue += color.B;
            count++;
        }

        for (var x = 0; x < width; x++)
        {
            Add(pixels[x]);
            if (height > 1) Add(pixels[(height - 1) * width + x]);
        }
        for (var y = 1; y < height - 1; y++)
        {
            Add(pixels[y * width]);
            if (width > 1) Add(pixels[y * width + width - 1]);
        }
        return new Color((int)(red / count), (int)(green / count), (int)(blue / count));
    }

    private static double SmoothStep(double edge0, double edge1, double value)
    {
        var t = Math.Clamp((value - edge0) / (edge1 - edge0), 0, 1);
        return t * t * (3 - 2 * t);
    }

    private static Color Shade(Color color, float factor) => new(
        (byte)Math.Clamp((int)MathF.Round(color.R * factor), 0, 255),
        (byte)Math.Clamp((int)MathF.Round(color.G * factor), 0, 255),
        (byte)Math.Clamp((int)MathF.Round(color.B * factor), 0, 255),
        color.A);
}
