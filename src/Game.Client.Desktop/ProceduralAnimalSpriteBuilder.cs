using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Game.Contracts;
using Game.Procedural;
using Microsoft.Xna.Framework;

namespace Game.Client.Desktop;

/// <summary>Builds small render-only pixel silhouettes from published animal agents.</summary>
internal static class ProceduralAnimalSpriteBuilder
{
    public const int Width = 24;
    public const int Height = 18;

    public static int VariationFor(ulong worldSeed, ulong animalId)
        => Math.Min(3, (int)(CounterRandom.Unit(worldSeed, 0x4641554E41535052UL, animalId, 0, 0, 0) * 4));

    public static Color[] Build(AnimalFunctionalProfileV1 profile, int variation)
    {
        if (profile is not (AnimalFunctionalProfileV1.Herbivore or AnimalFunctionalProfileV1.Predator))
            throw new ArgumentOutOfRangeException(nameof(profile));

        variation &= 3;
        var pixels = new Color[Width * Height];
        var outline = new Color(34, 40, 36);
        var fur = profile == AnimalFunctionalProfileV1.Herbivore
            ? variation switch
            {
                0 => new Color(145, 102, 67),
                1 => new Color(173, 132, 88),
                2 => new Color(117, 111, 96),
                _ => new Color(158, 94, 65)
            }
            : variation switch
            {
                0 => new Color(151, 106, 65),
                1 => new Color(120, 125, 118),
                2 => new Color(163, 133, 91),
                _ => new Color(101, 91, 80)
            };
        var lightFur = Shade(fur, 1.28f);
        var darkFur = Shade(fur, 0.68f);

        if (profile == AnimalFunctionalProfileV1.Herbivore)
        {
            FillEllipse(3, 9, 2, 2, outline);
            FillEllipse(4, 9, 1, 1, darkFur);
            FillEllipse(10, 10, 8, 4, outline);
            FillEllipse(10, 9, 7, 3, fur);
            FillEllipse(16, 9, 4, 3, outline);
            FillEllipse(17, 8, 3, 2, lightFur);
            FillEllipse(20, 8, 2, 2, outline);
            FillEllipse(20, 8, 1, 1, fur);
            Line(18, 6, variation % 2 == 0 ? 18 : 19, 1, outline);
            Line(20, 6, variation % 2 == 0 ? 21 : 20, 2, outline);
            Line(18, 6, variation % 2 == 0 ? 18 : 19, 2, lightFur);
            Line(20, 6, variation % 2 == 0 ? 21 : 20, 3, lightFur);
        }
        else
        {
            Line(4, 9, 1, variation % 2 == 0 ? 6 : 12, outline);
            Line(3, 9, 1, variation % 2 == 0 ? 6 : 12, darkFur);
            FillEllipse(10, 10, 9, 3, outline);
            FillEllipse(10, 9, 8, 2, fur);
            FillEllipse(16, 9, 4, 3, outline);
            FillEllipse(17, 8, 3, 2, lightFur);
            FillEllipse(20, 8, 3, 2, outline);
            FillEllipse(21, 8, 2, 1, fur);
            Line(17, 6, 17, 2, outline);
            Line(20, 6, 21, 2, outline);
            Line(18, 5, 18, 3, lightFur);
            Line(20, 5, 20, 3, lightFur);
        }

        // Four short legs and a belly highlight keep the creature legible at map scale.
        foreach (var x in new[] { 7, 11, 15, 18 })
        {
            Line(x, 12, x + (x % 2 == 0 ? 1 : 0), 16, outline);
            Put(x, 13, darkFur);
        }
        FillEllipse(11, 11, 4, 1, lightFur);
        Put(profile == AnimalFunctionalProfileV1.Herbivore ? 20 : 21, 7, Color.White);
        Put(profile == AnimalFunctionalProfileV1.Herbivore ? 20 : 21, 7, outline);
        Put(profile == AnimalFunctionalProfileV1.Herbivore ? 21 : 22, 8, darkFur);

        if ((variation & 1) != 0)
            MirrorHorizontally(pixels);
        return pixels;

        void Put(int x, int y, Color color)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
                pixels[y * Width + x] = color;
        }

        void FillEllipse(int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            for (var y = centerY - radiusY; y <= centerY + radiusY; y++)
            for (var x = centerX - radiusX; x <= centerX + radiusX; x++)
            {
                var dx = (x - centerX) / (double)radiusX;
                var dy = (y - centerY) / (double)radiusY;
                if (dx * dx + dy * dy <= 1.12) Put(x, y, color);
            }
        }

        void Line(int x0, int y0, int x1, int y1, Color color)
        {
            var dx = Math.Abs(x1 - x0);
            var sx = x0 < x1 ? 1 : -1;
            var dy = -Math.Abs(y1 - y0);
            var sy = y0 < y1 ? 1 : -1;
            var error = dx + dy;
            while (true)
            {
                Put(x0, y0, color);
                if (x0 == x1 && y0 == y1) break;
                var doubled = error * 2;
                if (doubled >= dy) { error += dy; x0 += sx; }
                if (doubled <= dx) { error += dx; y0 += sy; }
            }
        }
    }

    public static string LayoutHash(ulong worldSeed, IReadOnlyList<AnimalAgentV1> animals)
    {
        var canonical = new StringBuilder();
        canonical.Append(worldSeed.ToString(CultureInfo.InvariantCulture)).Append('\n');
        foreach (var animal in animals.OrderBy(value => value.AnimalId))
        {
            canonical.Append(animal.AnimalId.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(((byte)animal.FunctionalProfileId).ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(BitConverter.DoubleToInt64Bits(animal.PositionXM).ToString("X16", CultureInfo.InvariantCulture)).Append('|')
                .Append(BitConverter.DoubleToInt64Bits(animal.PositionYM).ToString("X16", CultureInfo.InvariantCulture)).Append('|')
                .Append(VariationFor(worldSeed, animal.AnimalId).ToString(CultureInfo.InvariantCulture)).Append('\n');
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }

    private static Color Shade(Color color, float scale)
        => new(Math.Clamp((int)(color.R * scale), 0, 255), Math.Clamp((int)(color.G * scale), 0, 255),
            Math.Clamp((int)(color.B * scale), 0, 255), color.A);

    private static void MirrorHorizontally(Color[] pixels)
    {
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width / 2; x++)
        {
            var left = y * Width + x;
            var right = y * Width + (Width - 1 - x);
            (pixels[left], pixels[right]) = (pixels[right], pixels[left]);
        }
    }
}
