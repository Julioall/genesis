using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Game.Procedural;

namespace Game.Client.Desktop;

internal readonly record struct ProceduralRockInstance(
    Vector3 Ground,
    float WidthM,
    float HeightM,
    ulong Seed,
    int Variant);

/// <summary>Builds deterministic, faceted boulder meshes for the renderer evaluation scene.</summary>
internal static class ProceduralRockMeshBuilder
{
    private static readonly float[] RingHeights = [0f, 0.16f, 0.54f, 0.86f, 1f];
    private static readonly float[][] RingRadiusProfiles =
    [
        [0.42f, 0.70f, 0.76f, 0.60f, 0.28f],
        [0.48f, 0.76f, 0.82f, 0.72f, 0.34f],
        [0.30f, 0.55f, 0.74f, 0.76f, 0.26f],
        [0.45f, 0.68f, 0.60f, 0.78f, 0.38f]
    ];
    private static readonly float[] HeightProfiles = [0.78f, 0.68f, 1.08f, 1.20f];
    private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(-0.42f, 0.82f, 0.38f));

    public static VertexPositionColor[] Build(IReadOnlyList<ProceduralRockInstance> rocks)
    {
        var vertices = new List<VertexPositionColor>(rocks.Count * 10 * (RingHeights.Length - 1) * 6);
        foreach (var rock in rocks) AddRock(vertices, rock);
        return vertices.ToArray();
    }

    private static void AddRock(List<VertexPositionColor> vertices, ProceduralRockInstance rock)
    {
        var profile = Math.Abs(rock.Variant) % RingRadiusProfiles.Length;
        var ringRadii = RingRadiusProfiles[profile];
        var sides = 7 + profile * 2;
        var points = new Vector3[RingHeights.Length, sides];
        for (var ring = 0; ring < RingHeights.Length; ring++)
        for (var side = 0; side < sides; side++)
        {
            var angle = MathF.Tau * side / sides
                + (Unit(rock.Seed, 0x524F434B50484153UL, (ulong)ring) - 0.5f) * 0.18f;
            var irregularity = 0.88f + Unit(rock.Seed, 0x524F434B53484150UL,
                (ulong)(ring * sides + side)) * 0.24f;
            var radius = rock.WidthM * 0.5f * ringRadii[ring] * irregularity;
            var height = rock.HeightM * HeightProfiles[profile] * RingHeights[ring];
            var leanX = (Unit(rock.Seed, 0x524F434B4C45414EUL, 0) - 0.5f) * rock.WidthM * 0.12f * RingHeights[ring];
            var leanZ = (Unit(rock.Seed, 0x524F434B4C45414EUL, 1) - 0.5f) * rock.WidthM * 0.12f * RingHeights[ring];
            points[ring, side] = rock.Ground + new Vector3(
                leanX + MathF.Cos(angle) * radius,
                height,
                leanZ + MathF.Sin(angle) * radius * 0.82f);
        }

        var baseColor = (rock.Variant % 4) switch
        {
            0 => new Color(105, 103, 92),
            1 => new Color(125, 112, 92),
            2 => new Color(91, 105, 105),
            _ => new Color(132, 126, 111)
        };
        for (var ring = 0; ring < RingHeights.Length - 1; ring++)
        for (var side = 0; side < sides; side++)
        {
            var next = (side + 1) % sides;
            var a = points[ring, side];
            var b = points[ring, next];
            var c = points[ring + 1, side];
            var d = points[ring + 1, next];
            var normal = Vector3.Normalize(Vector3.Cross(c - a, b - a));
            var light = 0.65f + MathF.Max(0, Vector3.Dot(normal, LightDirection)) * 0.34f;
            var facet = (Unit(rock.Seed, 0x524F434B46414345UL,
                (ulong)(ring * sides + side)) - 0.5f) * 0.15f;
            var color = Shade(baseColor, light + facet + (ring == 0 ? -0.08f : 0));
            AddTriangle(vertices, a, c, b, color);
            AddTriangle(vertices, b, c, d, color);
        }

        var finalRing = RingHeights.Length - 1;
        var crown = points[finalRing, 0];
        for (var side = 0; side < sides; side++)
        {
            var next = (side + 1) % sides;
            var color = Shade(baseColor, 0.92f + Unit(rock.Seed, 0x524F434B43524F57UL, (ulong)side) * 0.18f);
            AddTriangle(vertices, crown, points[finalRing, side], points[finalRing, next], color);
        }
    }

    private static float Unit(ulong seed, ulong salt, ulong index)
        => (float)CounterRandom.Unit(seed, salt, index, 0, 0, 0);

    private static Color Shade(Color color, float factor) => new(
        (byte)Math.Clamp((int)MathF.Round(color.R * factor), 0, 255),
        (byte)Math.Clamp((int)MathF.Round(color.G * factor), 0, 255),
        (byte)Math.Clamp((int)MathF.Round(color.B * factor), 0, 255));

    private static void AddTriangle(List<VertexPositionColor> vertices, Vector3 a, Vector3 b, Vector3 c, Color color)
    {
        vertices.Add(new VertexPositionColor(a, color));
        vertices.Add(new VertexPositionColor(b, color));
        vertices.Add(new VertexPositionColor(c, color));
    }
}
