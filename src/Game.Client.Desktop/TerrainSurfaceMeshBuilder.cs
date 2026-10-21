using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game.Client.Desktop;

public readonly record struct TerrainMeshData(VertexPositionNormalTexture[] Vertices, int[] Indices)
{
    public int PrimitiveCount => Indices.Length / 3;
}

/// <summary>Builds deterministic render chunks from the Geosphere elevation field.</summary>
public static class TerrainSurfaceMeshBuilder
{
    public static TerrainMeshData BuildChunk(
        Func<double, double, double> elevationSampler,
        int startXCells,
        int startYCells,
        int cellsWide,
        int cellsHigh,
        int subdivisionsPerCell,
        int worldWidthCells,
        int worldHeightCells,
        double cellSizeM)
    {
        ArgumentNullException.ThrowIfNull(elevationSampler);
        if (startXCells < 0 || startYCells < 0 || cellsWide <= 0 || cellsHigh <= 0
            || startXCells + cellsWide > worldWidthCells || startYCells + cellsHigh > worldHeightCells
            || subdivisionsPerCell <= 0 || worldWidthCells <= 0 || worldHeightCells <= 0
            || !double.IsFinite(cellSizeM) || cellSizeM <= 0)
            throw new ArgumentOutOfRangeException(nameof(cellsWide), "Terrain chunk bounds, subdivision, and scale must be positive and inside the world.");

        var columns = checked(cellsWide * subdivisionsPerCell);
        var rows = checked(cellsHigh * subdivisionsPerCell);
        var stride = columns + 1;
        var vertices = new VertexPositionNormalTexture[checked(stride * (rows + 1))];
        var sampleStep = 0.5 / subdivisionsPerCell;

        for (var z = 0; z <= rows; z++)
        for (var x = 0; x <= columns; x++)
        {
            var gridX = startXCells + x / (double)subdivisionsPerCell;
            var gridY = startYCells + z / (double)subdivisionsPerCell;
            var derivativeEast = (elevationSampler(gridX + sampleStep, gridY)
                - elevationSampler(gridX - sampleStep, gridY)) / (cellSizeM * sampleStep * 2);
            var derivativeNorth = (elevationSampler(gridX, gridY + sampleStep)
                - elevationSampler(gridX, gridY - sampleStep)) / (cellSizeM * sampleStep * 2);
            var normal = Vector3.Normalize(new Vector3((float)-derivativeEast, 1, (float)-derivativeNorth));
            var position = new Vector3((float)(gridX * cellSizeM),
                (float)elevationSampler(gridX, gridY), (float)(gridY * cellSizeM));
            var uv = new Vector2((float)(gridX / worldWidthCells), (float)(gridY / worldHeightCells));
            vertices[z * stride + x] = new VertexPositionNormalTexture(position, normal, uv);
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

        return new TerrainMeshData(vertices, indices);
    }
}
