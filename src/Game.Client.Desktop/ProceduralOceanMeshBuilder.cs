using Game.Procedural;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game.Client.Desktop;

internal readonly record struct ProceduralOceanMeshData(VertexPositionColor[] Vertices, int[] Indices)
{
    public int PrimitiveCount => Indices.Length / 3;
}

/// <summary>Builds a calm, render-only sea plane around the simulated island.</summary>
internal static class ProceduralOceanMeshBuilder
{
    private const ulong OceanPatternTag = 0x4F4345414E504154UL;

    public static ProceduralOceanMeshData Build(
        ulong seed,
        double centerEastM,
        double centerNorthM,
        double halfExtentM,
        double seaLevelM,
        double cellSizeM,
        int subdivisions)
    {
        if (halfExtentM <= 0 || cellSizeM <= 0 || subdivisions < 2
            || !double.IsFinite(centerEastM) || !double.IsFinite(centerNorthM)
            || !double.IsFinite(halfExtentM) || !double.IsFinite(seaLevelM)
            || !double.IsFinite(cellSizeM))
            throw new ArgumentOutOfRangeException(nameof(halfExtentM), "Ocean geometry dimensions and levels must be finite and positive.");

        var stride = subdivisions + 1;
        var vertices = new VertexPositionColor[checked(stride * stride)];
        for (var z = 0; z <= subdivisions; z++)
        for (var x = 0; x <= subdivisions; x++)
        {
            var u = x / (double)subdivisions;
            var v = z / (double)subdivisions;
            var east = centerEastM + (u * 2 - 1) * halfExtentM;
            var north = centerNorthM + (v * 2 - 1) * halfExtentM;
            var noiseX = (int)Math.Round((east / cellSizeM + 1024) * 4);
            var noiseY = (int)Math.Round((north / cellSizeM + 1024) * 4);
            var broad = CounterRandom.SpatialUnit(seed, OceanPatternTag, noiseX, noiseY, 224, 0, 1);
            var middle = CounterRandom.SpatialUnit(seed, OceanPatternTag, noiseX, noiseY, 80, 0, 2);
            var tone = Math.Clamp(0.38 + broad * 0.42 + (middle - 0.5) * 0.14, 0, 1);
            var color = Color.Lerp(new Color(18, 65, 75), new Color(47, 119, 128), (float)tone);
            var position = new Vector3((float)east, (float)seaLevelM, (float)north);
            vertices[z * stride + x] = new VertexPositionColor(position, color);
        }

        var indices = new int[checked(subdivisions * subdivisions * 6)];
        var index = 0;
        for (var z = 0; z < subdivisions; z++)
        for (var x = 0; x < subdivisions; x++)
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

        return new ProceduralOceanMeshData(vertices, indices);
    }
}
