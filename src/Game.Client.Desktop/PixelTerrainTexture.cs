using Game.Contracts;
using Game.Procedural;
using Microsoft.Xna.Framework;

namespace Game.Client.Desktop;

/// <summary>Builds a presentation-only terrain albedo from the authoritative L0 grid.</summary>
internal static class PixelTerrainTexture
{
    private static readonly (int X, int Y)[] GrassNeighbors =
    [
        (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1)
    ];

    public static Color[] Generate(WorldSnapshot world, int pixelsPerCell, int overlayMode, Season visualSeason,
        bool naturalMaterial = false, bool bakeLighting = true)
    {
        if (pixelsPerCell < 1) throw new ArgumentOutOfRangeException(nameof(pixelsPerCell));
        var width = world.Width * pixelsPerCell;
        var height = world.Height * pixelsPerCell;
        var pixels = new Color[width * height];
        var cellCount = world.Width * world.Height;
        var elevationGrid = new double[cellCount];
        var sandy = new double[cellCount];
        var grass = new double[cellCount];
        var soilWater = new double[cellCount];
        var soilCapacity = new double[cellCount];
        var nitrogen = new double[cellCount];
        var woody = new double[cellCount];
        var drySurface = new double[cellCount];
        var marshBiome = new double[cellCount];
        var visualBiomes = naturalMaterial ? VisualBiomeField.Build(world) : null;
        for (var i = 0; i < cellCount; i++)
        {
            var cell = world.Cells[i];
            elevationGrid[i] = cell.ElevationM;
            sandy[i] = cell.SoilKind == SoilKind.Sandy ? 1 : 0;
            grass[i] = cell.Grass.Present ? Math.Clamp(1 - Math.Exp(-cell.Grass.BiomassGPerM2 / 175), 0, 0.82) : 0;
            soilWater[i] = cell.SoilWaterMm;
            soilCapacity[i] = cell.SoilCapacityMm;
            nitrogen[i] = cell.MineralNitrogenGPerM2;
            woody[i] = cell.Woody.Present ? cell.Woody.BiomassGPerM2 : 0;
            if (visualBiomes is not null)
            {
                drySurface[i] = visualBiomes[i].DrySurface;
                marshBiome[i] = visualBiomes[i].Marsh;
            }
        }
        if (naturalMaterial)
        {
            var flatlandLevel = (elevationGrid.Min() + elevationGrid.Max()) * 0.5 - 12;
            for (var y = 0; y < world.Height; y++)
            for (var x = 0; x < world.Width; x++)
            {
                var index = y * world.Width + x;
                elevationGrid[index] = VisualBiomeField.FlattenForPresentation(world.Seed,
                    x + 0.5, y + 0.5, world.Width, world.Height, elevationGrid[index], flatlandLevel);
            }
        }
        grass = SmoothCoverage(grass, world.Width, world.Height);
        var grassAutotile = naturalMaterial
            ? (GrassAutotileField?)null
            : BuildGrassAutotile(world, detail: Math.Min(8, pixelsPerCell));
        var minimumElevation = elevationGrid.Min();
        var elevationRange = Math.Max(1, elevationGrid.Max() - minimumElevation);
        var surfaceTone = new double[cellCount];
        for (var i = 0; i < cellCount; i++)
        {
            var relativeElevation = (elevationGrid[i] - minimumElevation) / elevationRange;
            surfaceTone[i] = sandy[i] * 0.72 + relativeElevation * 0.28;
        }
        surfaceTone = SmoothSurfaceTone(surfaceTone, world.Width, world.Height);
        var landformElevation = naturalMaterial
            ? SmoothSurfaceTone((double[])elevationGrid.Clone(), world.Width, world.Height)
            : elevationGrid;
        var naturalPalette = naturalMaterial
            ? BuildNaturalSurfacePalette(elevationGrid, landformElevation, grass, soilWater, soilCapacity, woody, sandy,
                drySurface, marshBiome, world.Width, world.Height, minimumElevation, elevationRange)
            : null;
        var lighting = bakeLighting ? CalculateLighting(elevationGrid, world.Width, world.Height, 10) : null;

        var macro = new ValueNoise(world.Seed, width, height, Math.Max(1, (int)Math.Round(12.8 * pixelsPerCell)), 0x4D4143524FUL);
        var middle = new ValueNoise(world.Seed, width, height, Math.Max(1, (int)Math.Round(3.8 * pixelsPerCell)), 0x4D4944444CUL);
        var grain = new ValueNoise(world.Seed, width, height, Math.Max(1, (int)Math.Round(0.4 * pixelsPerCell)), 0x475241494EUL);
        Parallel.For(0, height, py =>
        {
        for (var px = 0; px < width; px++)
        {
            var gx = Math.Clamp(px / (double)pixelsPerCell - 0.5, 0, world.Width - 1);
            var gy = Math.Clamp(py / (double)pixelsPerCell - 0.5, 0, world.Height - 1);
            var heightM = Sample(elevationGrid, world.Width, world.Height, gx, gy);
            var eastDelta = (Sample(elevationGrid, world.Width, world.Height, gx + 0.55, gy)
                - Sample(elevationGrid, world.Width, world.Height, gx - 0.55, gy)) / 1.1;
            var northDelta = (Sample(elevationGrid, world.Width, world.Height, gx, gy + 0.55)
                - Sample(elevationGrid, world.Width, world.Height, gx, gy - 0.55)) / 1.1;
            var tone = Sample(surfaceTone, world.Width, world.Height, gx, gy);
            var grassValue = Sample(grass, world.Width, world.Height, gx, gy);
            var soilWaterValue = Sample(soilWater, world.Width, world.Height, gx, gy);
            var soilCapacityValue = Sample(soilCapacity, world.Width, world.Height, gx, gy);
            var drySurfaceValue = naturalMaterial ? Sample(drySurface, world.Width, world.Height, gx, gy) : 0;
            var nitrogenValue = Sample(nitrogen, world.Width, world.Height, gx, gy);
            var marshValue = naturalMaterial ? Sample(marshBiome, world.Width, world.Height, gx, gy) : 0;

            var broad = macro.Sample(px, py);
            var medium = middle.Sample(px, py);
            var fine = grain.Sample(px, py);
            var plantNoise = (float)((broad - 0.5) * 0.36 + (medium - 0.5) * 0.30 + (fine - 0.5) * 0.14);
            tone = Math.Clamp(tone + (broad - 0.5) * 0.12, 0, 1);
            var fleck = fine < 0.18 ? -0.07 : fine > 0.88 ? 0.06 : 0;
            var texture = naturalMaterial
                ? (broad - 0.5) * 0.28 + (medium - 0.5) * 0.22 + (fine - 0.5) * 0.08 + fleck * 1.15
                : (broad - 0.5) * 0.20 + (medium - 0.5) * 0.14 + fleck;
            var hillshade = lighting is null
                ? 1.0
                : 0.58 + 0.42 * Sample(lighting, world.Width, world.Height, gx, gy);
            var elevation = Math.Clamp((heightM - minimumElevation) / elevationRange, 0, 1);
            var slope = Math.Sqrt(eastDelta * eastDelta + northDelta * northDelta) / 10;
            var rockThreshold = 0.25 + (broad - 0.5) * 0.12 + (medium - 0.5) * 0.10;
            var rockExposure = SmoothStep(rockThreshold, rockThreshold + 0.38, slope) * 0.56;
            var steepRockExposure = SmoothStep(0.20, 0.68, slope);
            if (naturalMaterial) rockExposure = Math.Max(rockExposure, steepRockExposure * 0.86);
            var soilMoisture = (float)Math.Clamp(soilWaterValue / Math.Max(1, soilCapacityValue), 0, 1);

            var soil = naturalPalette is not null
                ? Sample(naturalPalette, world.Width, world.Height, gx, gy)
                : Mix(new Color(86, 108, 64), new Color(142, 133, 94), (float)Math.Clamp(tone, 0, 1));
            if (naturalMaterial)
            {
                soil = Mix(soil, new Color(178, 157, 116), (float)(drySurfaceValue * 0.20));
                soil = Mix(soil, new Color(69, 105, 73), (float)(marshValue * 0.62));
            }
            var rockColor = naturalMaterial
                ? Mix(new Color(151, 142, 119), new Color(115, 120, 112),
                    (float)Math.Clamp(medium * 0.56 + fine * 0.44, 0, 1))
                : new Color(116, 112, 96);
            soil = Mix(soil, rockColor, (float)rockExposure);
            soil = Mix(soil, new Color(66, 86, 57), soilMoisture * 0.18f);
            soil = Modulate(soil, hillshade + texture);

            var biomassStrength = (float)Math.Clamp(grassValue / 0.82, 0, 1);
            float grassCover;
            if (naturalMaterial)
            {
                // Break up coarse biomass into seed-stable plant patches at several scales.
                // Noise is gated by measured grass biomass, so it changes the material boundary
                // without painting vegetation into cells where the snapshot has none.
                var patternedGrass = Math.Clamp(grassValue + plantNoise * biomassStrength * 0.58f, 0, 0.82);
                grassCover = (float)(SmoothStep(0.10, 0.66, patternedGrass) * (0.70 + biomassStrength * 0.14));
                grassCover *= 1 - (float)steepRockExposure * 0.72f;
            }
            else
            {
                var autotile = grassAutotile!.Value;
                var grassX = Math.Clamp(px * autotile.Detail / pixelsPerCell, 0, autotile.Width - 1);
                var grassY = Math.Clamp(py * autotile.Detail / pixelsPerCell, 0, autotile.Height - 1);
                var grassIndex = grassY * autotile.Width + grassX;
                var neighborCount = CountNeighbors(autotile.NeighborMasks[grassIndex]);
                grassCover = autotile.States[grassIndex] == 0
                    ? 0
                    : (0.48f + neighborCount * 0.045f) * (0.78f + biomassStrength * 0.22f);
                if (grassCover > 0)
                {
                    var mask = autotile.NeighborMasks[grassIndex];
                    var pixelStartX = grassX * pixelsPerCell / autotile.Detail;
                    var pixelEndX = (grassX + 1) * pixelsPerCell / autotile.Detail;
                    var pixelStartY = grassY * pixelsPerCell / autotile.Detail;
                    var pixelEndY = (grassY + 1) * pixelsPerCell / autotile.Detail;
                    var localX = px - pixelStartX;
                    var localY = py - pixelStartY;
                    if ((localX == 0 && localY == 0 && NeedsCornerCut(mask, 0, 6, 7))
                        || (px == pixelEndX - 1 && localY == 0 && NeedsCornerCut(mask, 0, 2, 1))
                        || (localX == 0 && py == pixelEndY - 1 && NeedsCornerCut(mask, 4, 6, 5))
                        || (px == pixelEndX - 1 && py == pixelEndY - 1 && NeedsCornerCut(mask, 4, 2, 3)))
                        grassCover = 0;
                }
            }
            var grassColor = new Color(76, 128, 58);
            // The autotile supplies living blades; this softer pigment pass only blends ground
            // material between adjacent biomass cells and does not create extra plant cohorts.
            var cover = (float)(grassCover * (1 - elevation * 0.16));
            var surface = Mix(soil, Modulate(grassColor, hillshade + texture * 0.65f), cover);
            var plantMottle = Math.Clamp(0.50f + plantNoise, 0, 1);
            var groundPigment = biomassStrength * (naturalMaterial ? 0.18f + plantMottle * 0.16f : 0.14f)
                * (naturalMaterial ? 1 - (float)steepRockExposure * 0.78f : 1f);
            var groundPigmentColor = naturalMaterial ? new Color(74, 105, 52) : new Color(82, 108, 57);
            surface = Mix(surface, Modulate(groundPigmentColor, hillshade + texture * 0.35f), groundPigment);

            if (overlayMode == 1)
            {
                var wetness = (float)Math.Clamp(soilWaterValue / Math.Max(1, soilCapacityValue), 0, 1);
                surface = Mix(new Color(113, 91, 56), new Color(50, 113, 135), wetness * 0.78f);
            }
            else if (overlayMode == 2)
            {
                var biomass = (float)Math.Clamp((grassValue * 450 + Sample(woody, world.Width, world.Height, gx, gy)) / 900, 0, 1);
                surface = Mix(new Color(56, 70, 48), new Color(100, 167, 73), biomass);
            }
            else if (nitrogenValue > 0 && grassValue > 0.02 && fine > 0.82)
                surface = Mix(surface, new Color(133, 149, 83), (float)Math.Min(0.16, nitrogenValue * 0.015));

            pixels[py * width + px] = surface;
        }
        });

        return pixels;
    }

    internal static GrassAutotileField BuildGrassAutotile(WorldSnapshot world, int detail)
    {
        if (detail < 1 || detail > 8) throw new ArgumentOutOfRangeException(nameof(detail));
        var width = world.Width * detail;
        var height = world.Height * detail;
        var states = new byte[width * height];
        var masks = new byte[states.Length];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var cell = world.Cells[(y / detail) * world.Width + x / detail];
            if (!cell.Grass.Present || cell.Grass.BiomassGPerM2 <= 1) continue;
            var cover = Math.Clamp(1 - Math.Exp(-cell.Grass.BiomassGPerM2 / 125), 0, 1);
            var broad = CounterRandom.SpatialUnit(world.Seed, 0x4752415353415554UL,
                x, y, detail * 6, 0, 1);
            var middle = CounterRandom.SpatialUnit(world.Seed, 0x4752415353415554UL,
                x, y, detail * 2, 0, 2);
            var fine = CounterRandom.SpatialUnit(world.Seed, 0x4752415353415554UL,
                x, y, detail, 0, 3);
            var threshold = 0.10 + 0.48 * broad + 0.28 * middle + 0.14 * fine;
            if (cover >= threshold) states[y * width + x] = 1;
        }

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var index = y * width + x;
            if (states[index] == 0) continue;
            byte mask = 0;
            for (var bit = 0; bit < GrassNeighbors.Length; bit++)
            {
                var (offsetX, offsetY) = GrassNeighbors[bit];
                var neighborX = x + offsetX;
                var neighborY = y + offsetY;
                if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height
                    && states[neighborY * width + neighborX] != 0)
                    mask |= (byte)(1 << bit);
            }
            masks[index] = mask;
        }

        return new GrassAutotileField(width, height, detail, states, masks);
    }

    private static int CountNeighbors(byte mask)
    {
        var count = 0;
        for (var bit = 0; bit < GrassNeighbors.Length; bit++) count += (mask >> bit) & 1;
        return count;
    }

    private static bool NeedsCornerCut(byte mask, int firstCardinal, int secondCardinal, int diagonal)
        => (mask & (1 << firstCardinal)) == 0
            && (mask & (1 << secondCardinal)) == 0
            && (mask & (1 << diagonal)) == 0;

    private static double Sample(double[] values, int width, int height, double x, double y)
    {
        x = Math.Clamp(x, 0, width - 1);
        y = Math.Clamp(y, 0, height - 1);
        var x0 = (int)x;
        var y0 = (int)y;
        var x1 = Math.Min(width - 1, x0 + 1);
        var y1 = Math.Min(height - 1, y0 + 1);
        var tx = x - x0;
        var ty = y - y0;
        var a = values[y0 * width + x0];
        var b = values[y0 * width + x1];
        var c = values[y1 * width + x0];
        var d = values[y1 * width + x1];
        var top = a + (b - a) * tx;
        var bottom = c + (d - c) * tx;
        return top + (bottom - top) * ty;
    }

    private static Color Sample(Color[] values, int width, int height, double x, double y)
    {
        x = Math.Clamp(x, 0, width - 1);
        y = Math.Clamp(y, 0, height - 1);
        var x0 = (int)x;
        var y0 = (int)y;
        var x1 = Math.Min(width - 1, x0 + 1);
        var y1 = Math.Min(height - 1, y0 + 1);
        var top = Color.Lerp(values[y0 * width + x0], values[y0 * width + x1], (float)(x - x0));
        var bottom = Color.Lerp(values[y1 * width + x0], values[y1 * width + x1], (float)(x - x0));
        return Color.Lerp(top, bottom, (float)(y - y0));
    }

    private static double[] SmoothCoverage(double[] source, int width, int height)
    {
        for (var pass = 0; pass < 2; pass++)
        {
            var smoothed = new double[source.Length];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var index = y * width + x;
                var value = source[index] * 0.52;
                value += source[y * width + Math.Max(0, x - 1)] * 0.09;
                value += source[y * width + Math.Min(width - 1, x + 1)] * 0.09;
                value += source[Math.Max(0, y - 1) * width + x] * 0.09;
                value += source[Math.Min(height - 1, y + 1) * width + x] * 0.09;
                value += source[Math.Max(0, y - 1) * width + Math.Max(0, x - 1)] * 0.03;
                value += source[Math.Max(0, y - 1) * width + Math.Min(width - 1, x + 1)] * 0.03;
                value += source[Math.Min(height - 1, y + 1) * width + Math.Max(0, x - 1)] * 0.03;
                value += source[Math.Min(height - 1, y + 1) * width + Math.Min(width - 1, x + 1)] * 0.03;
                smoothed[index] = value;
            }
            source = smoothed;
        }
        return source;
    }

    private static double[] SmoothSurfaceTone(double[] source, int width, int height)
    {
        ReadOnlySpan<int> weights = [1, 4, 6, 4, 1];
        var smoothed = new double[source.Length];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var weighted = 0.0;
            for (var oy = -2; oy <= 2; oy++)
            for (var ox = -2; ox <= 2; ox++)
            {
                var sampleX = Math.Clamp(x + ox, 0, width - 1);
                var sampleY = Math.Clamp(y + oy, 0, height - 1);
                weighted += source[sampleY * width + sampleX] * weights[oy + 2] * weights[ox + 2];
            }
            var index = y * width + x;
            smoothed[index] = source[index] * 0.25 + weighted / 256 * 0.75;
        }
        return smoothed;
    }

    private static Color[] BuildNaturalSurfacePalette(
        double[] elevations,
        double[] landformElevations,
        double[] grass,
        double[] soilWater,
        double[] soilCapacity,
        double[] woody,
        double[] sandy,
        double[] drySurface,
        double[] marshBiome,
        int width,
        int height,
        double minimumElevation,
        double elevationRange)
    {
        var colors = new Color[elevations.Length];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var index = y * width + x;
            var west = elevations[y * width + Math.Max(0, x - 1)];
            var east = elevations[y * width + Math.Min(width - 1, x + 1)];
            var north = elevations[Math.Max(0, y - 1) * width + x];
            var south = elevations[Math.Min(height - 1, y + 1) * width + x];
            var slope = Math.Sqrt(Math.Pow((east - west) * 0.5, 2) + Math.Pow((south - north) * 0.5, 2)) / 10;
            var relativeElevation = (landformElevations[index] - minimumElevation) / elevationRange;
            var upland = SmoothStep(0.64, 0.82, relativeElevation);
            var highland = SmoothStep(0.79, 0.96, relativeElevation);
            var exposed = SmoothStep(0.22, 0.64, slope);
            var wetness = Math.Clamp(soilWater[index] / Math.Max(1, soilCapacity[index]), 0, 1);
            var woodland = 1 - Math.Exp(-Math.Max(0, woody[index]) / 420);
            var meadow = SmoothStep(0.08, 0.60, grass[index]);

            var color = Mix(new Color(105, 125, 69), new Color(156, 143, 105),
                (float)Math.Clamp(sandy[index] * 0.58 + relativeElevation * 0.18, 0, 0.76));
            color = Mix(color, new Color(90, 116, 66), (float)(SmoothStep(0.10, 0.72, woodland) * 0.28));
            color = Mix(color, new Color(98, 136, 68), (float)(meadow * 0.20));
            color = Mix(color, new Color(87, 112, 79), (float)(wetness * (1 - exposed * 0.45) * 0.20));
            color = Mix(color, new Color(143, 138, 113), (float)(upland * 0.20 + highland * 0.27));
            color = Mix(color, new Color(137, 132, 111), (float)(exposed * 0.40));
            color = Mix(color, new Color(178, 157, 116), (float)(drySurface[index] * 0.24));
            color = Mix(color, new Color(69, 105, 73), (float)(marshBiome[index] * 0.64));
            colors[index] = color;
        }

        return SmoothSurfacePalette(colors, width, height);
    }

    private static Color[] SmoothSurfacePalette(Color[] source, int width, int height)
    {
        ReadOnlySpan<int> weights = [1, 4, 6, 4, 1];
        var smoothed = new Color[source.Length];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var red = 0.0;
            var green = 0.0;
            var blue = 0.0;
            for (var oy = -2; oy <= 2; oy++)
            for (var ox = -2; ox <= 2; ox++)
            {
                var sampleX = Math.Clamp(x + ox, 0, width - 1);
                var sampleY = Math.Clamp(y + oy, 0, height - 1);
                var color = source[sampleY * width + sampleX];
                var weight = weights[oy + 2] * weights[ox + 2];
                red += color.R * weight;
                green += color.G * weight;
                blue += color.B * weight;
            }

            var index = y * width + x;
            var blurred = new Color((int)Math.Round(red / 256), (int)Math.Round(green / 256), (int)Math.Round(blue / 256));
            smoothed[index] = Color.Lerp(source[index], blurred, 0.42f);
        }

        return smoothed;
    }

    private static double[] CalculateLighting(double[] elevations, int width, int height, double cellSizeM)
    {
        const double lightX = -0.7071067811865476;
        const double lightY = -0.7071067811865476;
        var factors = new double[elevations.Length];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var westX = Math.Max(0, x - 1);
            var eastX = Math.Min(width - 1, x + 1);
            var northY = Math.Max(0, y - 1);
            var southY = Math.Min(height - 1, y + 1);
            var dx = (elevations[y * width + eastX] - elevations[y * width + westX])
                / (Math.Max(1, eastX - westX) * cellSizeM);
            var dy = (elevations[southY * width + x] - elevations[northY * width + x])
                / (Math.Max(1, southY - northY) * cellSizeM);
            var intensity = Math.Clamp(-(dx * lightX + dy * lightY) * 3 * 2.2, -0.52, 0.48);
            var current = elevations[y * width + x];
            var occlusion = 0.0;
            var lastIndex = y * width + x;
            for (var step = 1; step <= 4; step++)
            {
                var sampleX = (int)Math.Round(x + lightX * step);
                var sampleY = (int)Math.Round(y + lightY * step);
                if (sampleX < 0 || sampleX >= width || sampleY < 0 || sampleY >= height) continue;
                var index = sampleY * width + sampleX;
                if (index == lastIndex) continue;
                lastIndex = index;
                var distance = Math.Max(1, Math.Sqrt((sampleX - x) * (sampleX - x) + (sampleY - y) * (sampleY - y)));
                var horizon = elevations[index] - current;
                occlusion = Math.Max(occlusion, Math.Clamp(horizon / (cellSizeM * distance) * 3 * 0.37, 0, 0.17));
            }
            factors[y * width + x] = Math.Clamp(1.02 + intensity - occlusion, 0.62, 1.38);
        }
        return SmoothLighting(factors, width, height);
    }

    private static double[] SmoothLighting(double[] source, int width, int height)
    {
        ReadOnlySpan<int> weights = [1, 2, 3, 2, 1];
        var smoothed = new double[source.Length];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var weighted = 0.0;
            for (var oy = -2; oy <= 2; oy++)
            for (var ox = -2; ox <= 2; ox++)
            {
                var sampleX = Math.Clamp(x + ox, 0, width - 1);
                var sampleY = Math.Clamp(y + oy, 0, height - 1);
                weighted += source[sampleY * width + sampleX] * weights[oy + 2] * weights[ox + 2];
            }
            var index = y * width + x;
            smoothed[index] = source[index] * 0.25 + weighted / 81 * 0.75;
        }
        return smoothed;
    }

    private static double SmoothStep(double edge0, double edge1, double value)
    {
        var t = Math.Clamp((value - edge0) / (edge1 - edge0), 0, 1);
        return t * t * (3 - 2 * t);
    }

    private static Color Mix(Color a, Color b, float amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return new Color(
            (byte)(a.R + (b.R - a.R) * amount),
            (byte)(a.G + (b.G - a.G) * amount),
            (byte)(a.B + (b.B - a.B) * amount));
    }

    private static Color Modulate(Color color, double factor)
        => new((byte)Math.Clamp(color.R * factor, 0, 255),
            (byte)Math.Clamp(color.G * factor, 0, 255),
            (byte)Math.Clamp(color.B * factor, 0, 255));

    private sealed class ValueNoise
    {
        private readonly double[,] _values;
        private readonly int _spacing;

        public ValueNoise(ulong seed, int width, int height, int spacing, ulong salt)
        {
            _spacing = spacing;
            var columns = (int)Math.Ceiling(width / (double)spacing) + 1;
            var rows = (int)Math.Ceiling(height / (double)spacing) + 1;
            _values = new double[columns, rows];
            for (var y = 0; y < rows; y++)
            for (var x = 0; x < columns; x++)
                _values[x, y] = CounterRandom.Unit(seed, salt, (ulong)x, (ulong)y, 0, 0);
        }

        public double Sample(int x, int y)
        {
            var fx = Math.Clamp(x / (double)_spacing, 0, _values.GetLength(0) - 1.000001);
            var fy = Math.Clamp(y / (double)_spacing, 0, _values.GetLength(1) - 1.000001);
            var x0 = (int)fx;
            var y0 = (int)fy;
            var tx = Fade(fx - x0);
            var ty = Fade(fy - y0);
            var top = _values[x0, y0] + (_values[x0 + 1, y0] - _values[x0, y0]) * tx;
            var bottom = _values[x0, y0 + 1] + (_values[x0 + 1, y0 + 1] - _values[x0, y0 + 1]) * tx;
            return top + (bottom - top) * ty;
        }

        private static double Fade(double t) => t * t * (3 - 2 * t);
    }

    internal readonly record struct GrassAutotileField(
        int Width, int Height, int Detail, byte[] States, byte[] NeighborMasks);
}
