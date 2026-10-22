using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game.Client.Desktop;

/// <summary>Builds deterministic bark, leaf, and opaque distant-crown materials from pixel rules.</summary>
internal static class ProceduralTreeMaterialAtlasBuilder
{
    private const int CellSize = 256;
    private const int Gutter = 2;
    private const int Step = CellSize + Gutter * 2;
    private const int SpeciesCount = 5;
    private const int MaterialCount = 4;
    private const float Tau = MathF.PI * 2;
    private static readonly Vector2[] MapleVeinTips =
    [
        new(128, 7), new(34, 62), new(65, 35), new(190, 35), new(222, 62)
    ];
    private static readonly float[] VeinSides = [-1f, 1f];

    public static Texture2D Build(GraphicsDevice graphicsDevice)
    {
        const int width = SpeciesCount * Step;
        const int height = MaterialCount * Step;
        var pixels = new Color[width * height];

        for (var species = 0; species < SpeciesCount; species++)
        for (var material = 0; material < MaterialCount; material++)
        {
            var tile = BuildTile(species, material);
            CopyTileWithGutter(pixels, width, species * Step, material * Step, tile);
        }

        var atlas = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Color);
        atlas.SetData(pixels);
        return atlas;
    }

    private static Color[] BuildTile(int species, int material)
    {
        if (material == 1) return BuildCanopyTile(species);
        if (material == 3) return BuildCanopyMassTile(species);

        var pixels = new Color[CellSize * CellSize];
        for (var y = 0; y < CellSize; y++)
        for (var x = 0; x < CellSize; x++)
        {
            var u = x / (float)(CellSize - 1);
            var v = y / (float)(CellSize - 1);
            pixels[y * CellSize + x] = material switch
            {
                0 => BarkPixel(species, u, v),
                _ => LeafPixel(species, x, y, u, v)
            };
        }
        return pixels;
    }

    private static Color[] BuildCanopyMassTile(int species)
    {
        // Distant crown volumes need a solid, deterministic leaf material. Individual
        // leaf cutouts remain in material 1; this tile supplies opaque foliage at LOD0.
        var pixels = new Color[CellSize * CellSize];
        var phase = species * 0.731f;
        for (var y = 0; y < CellSize; y++)
        for (var x = 0; x < CellSize; x++)
        {
            var u = x / (float)(CellSize - 1);
            var v = y / (float)(CellSize - 1);
            var broadMottle = PeriodicNoise(u, v, 9, species + 127);
            var fineMottle = PeriodicNoise(u, v, 23, species + 193);
            var rib = MathF.Sin(Tau * (u * 3.2f + v * 2.1f + phase)) * 0.018f;
            var tone = Math.Clamp(0.91f + (broadMottle - 0.5f) * 0.12f
                + (fineMottle - 0.5f) * 0.055f + rib, 0.78f, 1f);
            var value = (byte)Math.Clamp((int)MathF.Round(tone * 255f), 0, 255);
            pixels[y * CellSize + x] = new Color(value, value, value, (byte)255);
        }
        return pixels;
    }

    private static Color[] BuildCanopyTile(int species)
    {
        // Procedural twig and leaf pigments are sampled on individual leaf blades and
        // branch-local 3D foliage masses; the tile never stores a complete tree impostor.
        var pixels = new Color[CellSize * CellSize];
        var twigCount = species == 1 ? 20 : species == 4 ? 14 : 16;
        var leavesPerTwig = species == 1 ? 20 : 18;
        for (var twig = 0; twig < twigCount; twig++)
        {
            var twigKey = (ulong)twig;
            var angle = Tau * twig / twigCount
                + (UnitHash(species, twigKey, 0) - 0.5f) * 0.32f;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var start = new Vector2(128f, 133f)
                + new Vector2((UnitHash(species, twigKey, 1) - 0.5f) * 16f,
                    (UnitHash(species, twigKey, 2) - 0.5f) * 16f);
            var reach = species == 1 ? 77f : species == 4 ? 68f : 72f;
            var end = start + direction * reach;
            var side = new Vector2(-direction.Y, direction.X);

            for (var leaf = 0; leaf < leavesPerTwig; leaf++)
            {
                var leafKey = (ulong)leaf;
                var along = 0.16f + 0.80f * (leaf + 0.5f) / leavesPerTwig;
                var attachment = Vector2.Lerp(start, end, along)
                    + side * ((UnitHash(species, twigKey, leafKey + 11) - 0.5f) * 11f);
                var sideSign = (leaf & 1) == 0 ? 1f : -1f;
                var leafAngle = angle + sideSign * (0.48f + UnitHash(species, twigKey, leafKey + 37) * 0.58f);
                if (species == 1)
                    leafAngle = angle + sideSign * (0.85f + UnitHash(species, twigKey, leafKey + 37) * 0.45f);
                if (species == 4) leafAngle += 0.34f;

            var length = (species == 1 ? 26f : species == 2 ? 18f : 22f)
                    * (0.82f + UnitHash(species, twigKey, leafKey + 61) * 0.38f);
                StampLeaf(pixels, species, attachment, length, leafAngle,
                    0.84f + UnitHash(species, twigKey, leafKey + 83) * 0.16f);
            }
        }

        return pixels;
    }

    private static void StampLeaf(Color[] pixels, int species, Vector2 basePoint,
        float length, float angle, float brightness)
    {
        var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        var side = new Vector2(-direction.Y, direction.X);
        var tip = basePoint + direction * length;
        var halfWidth = length * MaxLeafHalfWidth(species);
        var minX = Math.Clamp((int)MathF.Floor(Math.Min(basePoint.X, tip.X) - halfWidth - 1f), 0, CellSize - 1);
        var maxX = Math.Clamp((int)MathF.Ceiling(Math.Max(basePoint.X, tip.X) + halfWidth + 1f), 0, CellSize - 1);
        var minY = Math.Clamp((int)MathF.Floor(Math.Min(basePoint.Y, tip.Y) - halfWidth - 1f), 0, CellSize - 1);
        var maxY = Math.Clamp((int)MathF.Ceiling(Math.Max(basePoint.Y, tip.Y) + halfWidth + 1f), 0, CellSize - 1);

        for (var y = minY; y <= maxY; y++)
        for (var x = minX; x <= maxX; x++)
        {
            var offset = new Vector2(x + 0.5f, y + 0.5f) - basePoint;
            var t = Vector2.Dot(offset, direction) / length;
            if (t < 0f || t > 1f) continue;
            var u = 0.5f + Vector2.Dot(offset, side) / (length * MaxLeafHalfWidth(species) * 2f);
            var v = 1f - t;
            if (u < 0f || u > 1f) continue;
            var leaf = LeafPixel(species, (int)(u * (CellSize - 1)), (int)(v * (CellSize - 1)), u, v);
            if (leaf.A == 0) continue;

            var index = y * CellSize + x;
            var old = pixels[index];
            var sourceAlpha = leaf.A / 255f;
            var destinationAlpha = old.A / 255f;
            var alpha = sourceAlpha + destinationAlpha * (1f - sourceAlpha);
            var red = (byte)Math.Clamp((int)MathF.Round(leaf.R * brightness), 0, 255);
            var green = (byte)Math.Clamp((int)MathF.Round(leaf.G * brightness), 0, 255);
            var blue = (byte)Math.Clamp((int)MathF.Round(leaf.B * brightness), 0, 255);
            var inverse = destinationAlpha * (1f - sourceAlpha);
            pixels[index] = alpha <= 0f ? Color.Transparent : new Color(
                (byte)Math.Clamp((int)MathF.Round((red * sourceAlpha + old.R * inverse) / alpha), 0, 255),
                (byte)Math.Clamp((int)MathF.Round((green * sourceAlpha + old.G * inverse) / alpha), 0, 255),
                (byte)Math.Clamp((int)MathF.Round((blue * sourceAlpha + old.B * inverse) / alpha), 0, 255),
                (byte)Math.Clamp((int)MathF.Round(alpha * 255f), 0, 255));
        }
    }

    private static float MaxLeafHalfWidth(int species) => species switch
    {
        1 => 0.075f,
        2 => 0.39f,
        4 => 0.255f,
        3 => 0.46f,
        _ => 0.45f
    };

    private static float UnitHash(int species, ulong first, ulong second)
    {
        var x = unchecked((uint)(species * 0x1f123bb5 ^ (int)first * 0x5f356495 ^ (int)second * 0x6c8e9cf5));
        x ^= x >> 16;
        x *= 0x7feb352d;
        x ^= x >> 15;
        x *= 0x846ca68b;
        x ^= x >> 16;
        return (x & 0x00ffffff) / 16777215f;
    }

    private static Color BarkPixel(int species, float u, float v)
    {
        var phase = species * 0.731f;
        var grain = MathF.Sin(Tau * (u * 7.5f + 0.19f * MathF.Sin(Tau * (v * 1.7f + phase)))) * 0.055f
            + MathF.Sin(Tau * (u * 22f - v * 0.6f + phase * 1.7f)) * 0.025f
            + (PeriodicNoise(u, v, 11, species + 31) - 0.5f) * 0.075f;
        var tone = 0.91f + grain;

        if (species == 2) // Birch: pale bark with short horizontal lenticels.
        {
            var wave = MathF.Sin(Tau * (v * 7.4f + u * 0.27f + phase));
            var dash = MathF.Pow(MathF.Max(0, wave), 18f)
                * (0.24f + 0.76f * MathF.Pow(MathF.Max(0, MathF.Sin(Tau * (u * 2.3f + phase))), 2f));
            tone = 0.99f - dash * 0.48f + (PeriodicNoise(u, v, 16, species + 83) - 0.5f) * 0.035f;
        }
        else if (species == 0) // Oak: broken, irregular furrows.
        {
            var furrow = MathF.Pow(MathF.Max(0, MathF.Sin(Tau * (u * 5.8f + 0.17f * MathF.Sin(Tau * v * 2f + phase)))), 18f);
            tone -= furrow * 0.19f;
        }
        else if (species == 1) // Pine: long, narrow scales and shallow grooves.
        {
            tone -= MathF.Pow(MathF.Max(0, MathF.Sin(Tau * (u * 12f + v * 0.24f + phase))), 12f) * 0.10f;
        }
        else if (species == 3) // Field maple: short, broken flakes and corky ridges.
        {
            var flakes = MathF.Sin(Tau * (v * 8.5f + 0.14f * MathF.Sin(Tau * u * 3.1f + phase)));
            var breaks = MathF.Pow(MathF.Max(0, MathF.Sin(Tau * (u * 7.2f + v * 0.42f + phase))), 9f);
            tone -= MathF.Pow(MathF.Max(0, flakes), 8f) * (0.035f + breaks * 0.11f);
        }
        else if (species == 4) // Willow: fine, elongated fibrous grooves.
        {
            var fibres = MathF.Sin(Tau * (u * 13.5f + 0.12f * MathF.Sin(Tau * v * 1.4f + phase)));
            tone -= MathF.Pow(MathF.Max(0, fibres), 14f) * 0.075f;
            tone += MathF.Sin(Tau * (v * 2.0f + phase * 0.3f)) * 0.025f;
        }

        var value = (byte)Math.Clamp((int)MathF.Round(tone * 255f), 0, 255);
        return new Color(value, value, value, (byte)255);
    }

    private static Color LeafPixel(int species, int x, int y, float u, float v)
    {
        // Texture coordinates point from petiole (v=1) toward leaf tip (v=0).
        var t = 1f - v;
        var halfWidth = LeafHalfWidth(species, t);
        var edgePixels = (halfWidth - MathF.Abs(u - 0.5f)) * CellSize;
        if (edgePixels < -0.8f) return Color.Transparent;

        var alpha = (byte)Math.Clamp((int)MathF.Round((edgePixels + 0.8f) * 210f), 0, 255);
        if (alpha == 0) return Color.Transparent;

        var px = x + 0.5f;
        var py = y + 0.5f;
        var center = new Vector2(px, py);
        var veinDistance = float.MaxValue;
        var primaryWidth = species == 1 ? 0.85f : 1.45f;
        veinDistance = Math.Min(veinDistance,
            DistanceToSegment(center, new Vector2(128, 250), new Vector2(128, 5)));

        if (species == 3) // Maple: palmate veins spread from the petiole into five lobes.
        {
            foreach (var end in MapleVeinTips)
                veinDistance = Math.Min(veinDistance,
                    DistanceToSegment(center, new Vector2(128, 248), end));
            primaryWidth = 1.55f;
        }
        else
        {
            for (var branch = 0; branch < 7; branch++)
            {
                var veinT = 0.16f + branch * 0.105f;
                var endT = Math.Min(0.97f, veinT + (species == 1 ? 0.16f : 0.13f));
                var startY = CellSize * (1f - veinT);
                var endY = CellSize * (1f - endT);
                var endWidth = LeafHalfWidth(species, endT) * CellSize * 0.88f;
                foreach (var sign in VeinSides)
                {
                    var start = new Vector2(128f + sign * 1.3f, startY);
                    var end = new Vector2(128f + sign * endWidth, endY);
                    veinDistance = Math.Min(veinDistance, DistanceToSegment(center, start, end));
                }
            }
        }

        var vein = veinDistance < primaryWidth
            ? species == 2 ? 0.72f : 0.76f
            : veinDistance < primaryWidth + 1.0f ? 0.88f : 1.0f;
        var centerGlow = 0.97f + 0.035f * (1f - MathF.Abs(u - 0.5f) * 2f);
        var tone = Math.Clamp(vein * centerGlow, 0.55f, 1f);
        var value = (byte)Math.Clamp((int)MathF.Round(tone * 255f), 0, 255);
        return new Color(value, value, value, alpha);
    }

    private static float LeafHalfWidth(int species, float t)
    {
        if (t <= 0f || t >= 1f) return 0f;
        var oval = MathF.Pow(MathF.Sin(MathF.PI * t), 0.72f);
        return species switch
        {
            0 => 0.43f * oval * (0.61f + 0.39f * MathF.Abs(MathF.Sin(Tau * 2.5f * t + 0.35f))), // Oak, lobed.
            1 => 0.075f * MathF.Pow(MathF.Sin(MathF.PI * t), 0.55f), // Pine needles.
            2 => 0.36f * oval * (1f + 0.055f * MathF.Sin(Tau * 22f * t)), // Birch, fine serration.
            3 => 0.44f * oval * (0.38f + 0.62f * MathF.Abs(MathF.Sin(Tau * 2.15f * t + 0.48f))), // Maple, palmate lobes.
            _ => 0.235f * oval * (1f + 0.022f * MathF.Sin(Tau * 20f * t)) // Willow, lanceolate.
        };
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        var edge = end - start;
        var lengthSquared = edge.LengthSquared();
        var t = lengthSquared < 0.0001f ? 0f : Math.Clamp(Vector2.Dot(point - start, edge) / lengthSquared, 0f, 1f);
        return Vector2.Distance(point, start + edge * t);
    }

    private static float PeriodicNoise(float u, float v, int period, int seed)
    {
        var x = u * period;
        var y = v * period;
        var x0 = (int)MathF.Floor(x);
        var y0 = (int)MathF.Floor(y);
        var tx = Smooth(x - x0);
        var ty = Smooth(y - y0);
        var a = Hash(x0, y0, period, seed);
        var b = Hash(x0 + 1, y0, period, seed);
        var c = Hash(x0, y0 + 1, period, seed);
        var d = Hash(x0 + 1, y0 + 1, period, seed);
        return MathHelper.Lerp(MathHelper.Lerp(a, b, tx), MathHelper.Lerp(c, d, tx), ty);
    }

    private static float Hash(int x, int y, int period, int seed)
    {
        x = (x % period + period) % period;
        y = (y % period + period) % period;
        var value = unchecked((uint)(x * 0x1f123bb5 ^ y * 0x5f356495 ^ seed * 0x6c8e9cf5));
        value ^= value >> 16;
        value *= 0x7feb352d;
        value ^= value >> 15;
        value *= 0x846ca68b;
        value ^= value >> 16;
        return (value & 0x00ffffff) / 16777215f;
    }

    private static float Smooth(float value) => value * value * (3f - 2f * value);

    private static void CopyTileWithGutter(Color[] atlas, int atlasWidth, int originX, int originY, Color[] tile)
    {
        for (var y = -Gutter; y < CellSize + Gutter; y++)
        for (var x = -Gutter; x < CellSize + Gutter; x++)
        {
            var sourceX = Math.Clamp(x, 0, CellSize - 1);
            var sourceY = Math.Clamp(y, 0, CellSize - 1);
            var destinationX = originX + Gutter + x;
            var destinationY = originY + Gutter + y;
            atlas[destinationY * atlasWidth + destinationX] = tile[sourceY * CellSize + sourceX];
        }
    }
}
