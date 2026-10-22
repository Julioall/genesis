using Game.Contracts;
using Game.Procedural;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game.Client.Desktop;

internal readonly record struct GroundCoverBuildResult(VertexPositionColor[] Vertices, int Clusters,
    int UnderstoryShrubs);

/// <summary>Builds renderer-only grass and understory geometry from published flora snapshot fields.</summary>
internal static class ProceduralGroundCoverMeshBuilder
{
    private const float Tau = MathF.PI * 2;
    private const ulong UnderstoryMosaicTag = 0x554E4445524D4F53UL;

    public static GroundCoverBuildResult Build(
        WorldSnapshot snapshot,
        Func<double, double, double> elevationSampler,
        float cellSizeM,
        VisualBiomeWeights[]? visualBiomes,
        int minimumCellX,
        int minimumCellY,
        int maximumCellX,
        int maximumCellY)
    {
        visualBiomes ??= VisualBiomeField.Build(snapshot);
        if (visualBiomes.Length != snapshot.Cells.Count)
            throw new ArgumentException("Visual biome weights must match the published snapshot cells.", nameof(visualBiomes));

        minimumCellX = Math.Clamp(minimumCellX, 0, snapshot.Width - 1);
        minimumCellY = Math.Clamp(minimumCellY, 0, snapshot.Height - 1);
        maximumCellX = Math.Clamp(maximumCellX, minimumCellX, snapshot.Width - 1);
        maximumCellY = Math.Clamp(maximumCellY, minimumCellY, snapshot.Height - 1);

        var vertices = new List<VertexPositionColor>();
        var nearbyWoodland = BuildNearbyWoodland(snapshot, minimumCellX, minimumCellY,
            maximumCellX, maximumCellY);
        var clusters = 0;
        var understoryShrubs = 0;
        var grassPalette = new Color(76, 128, 58);

        for (var gridCellY = minimumCellY; gridCellY <= maximumCellY; gridCellY++)
        for (var gridCellX = minimumCellX; gridCellX <= maximumCellX; gridCellX++)
        {
            var cell = snapshot.Cells[gridCellY * snapshot.Width + gridCellX];
            if (!cell.Grass.Present || cell.Grass.BiomassGPerM2 <= 1 || cell.SurfaceWaterMm >= 450) continue;
            var biome = visualBiomes[cell.Index];
            if (biome.Marsh > 0.48f
                && Unit(snapshot.Seed, 0x4D41525348524545UL, (ulong)cell.Index, 0, 0, 0) < biome.Marsh * 0.34f)
            {
                AddMarshReeds(vertices, snapshot.Seed, cell.Index, cell.X, cell.Y, cellSizeM,
                    elevationSampler, biome.Marsh);
                clusters++;
            }
            var cover = Math.Clamp(1 - Math.Exp(-cell.Grass.BiomassGPerM2 / 145), 0, 1);
            var moisture = Math.Clamp(cell.SoilWaterMm / Math.Max(1, cell.SoilCapacityMm), 0, 1);
            if (AddUnderstoryShrub(vertices, snapshot, cell, cellSizeM, elevationSampler, biome,
                    (float)cover, (float)moisture, nearbyWoodland[cell.Index], TerrainSlope(snapshot, cell, cellSizeM)))
                understoryShrubs++;
            var clusterCount = 3 + (int)Math.Round(cover * 2.0) + (biome.Marsh > 0.56f ? 1 : 0);
            for (var cluster = 0; cluster < clusterCount; cluster++)
            {
                var placement = Unit(snapshot.Seed, 0x47524F554E44504CUL, (ulong)cell.Index, (ulong)cluster, 0, 0);
                var chance = (0.18 + cover * 0.55 + moisture * 0.10)
                    * (1 + biome.Marsh * 0.08);
                if (placement > chance) continue;

                var xFraction = 0.10 + Unit(snapshot.Seed, 0x47524F554E445858UL, (ulong)cell.Index, (ulong)cluster, 0, 0) * 0.80;
                var zFraction = 0.10 + Unit(snapshot.Seed, 0x47524F554E445A5AUL, (ulong)cell.Index, (ulong)cluster, 0, 0) * 0.80;
                var cellX = cell.X + xFraction;
                var cellZ = cell.Y + zFraction;
                var worldX = cellX * cellSizeM;
                var worldZ = cellZ * cellSizeM;
                var ground = new Vector3((float)worldX, (float)elevationSampler(cellX, cellZ) + 0.06f, (float)worldZ);
                var bladeCount = 8 + (int)(Unit(snapshot.Seed, 0x4752415353424C44UL,
                    (ulong)cell.Index, (ulong)cluster, 0, 0) * 5);
                // Wider, lower tufts survive the full-map projection as small vegetation patches;
                // instance count and snapshot biomass remain unchanged.
                var clusterWidth = 1.90f + (float)cover * 2.00f;
                var clusterHeight = (1.20f + (float)cover * 1.00f) * (0.82f + (float)moisture * 0.32f)
                    * (1 + biome.Marsh * 0.30f);
                var phase = (float)Unit(snapshot.Seed, 0x4752415353504841UL,
                    (ulong)cell.Index, (ulong)cluster, 0, 0) * Tau;

                for (var blade = 0; blade < bladeCount; blade++)
                {
                    var key = (ulong)(cluster * 16 + blade);
                    var angle = phase + Tau * blade / bladeCount
                        + ((float)Unit(snapshot.Seed, 0x47524153534A4954UL, (ulong)cell.Index, key, 0, 0) - 0.5f) * 0.6f;
                    var radial = new System.Numerics.Vector2(MathF.Cos(angle), MathF.Sin(angle));
                    var lean = 0.16f + (float)Unit(snapshot.Seed, 0x47524153534C4541UL,
                        (ulong)cell.Index, key, 0, 0) * 0.30f;
                    var length = clusterHeight * (0.63f + (float)Unit(snapshot.Seed, 0x4752415353424C4CUL,
                        (ulong)cell.Index, key, 0, 0) * 0.55f);
                    var width = clusterWidth * (0.18f + (float)Unit(snapshot.Seed, 0x4752415353574944UL,
                        (ulong)cell.Index, key, 0, 0) * 0.15f);
                    var bladeDirection = Vector3.Normalize(new Vector3(radial.X * lean, 1, radial.Y * lean));
                    var side = Vector3.Normalize(new Vector3(-radial.Y, 0, radial.X));
                    var offset = new Vector3(radial.X, 0, radial.Y) * clusterWidth * 0.12f;
                    var bladeBase = ground + offset;
                    var middle = bladeBase + bladeDirection * length * 0.55f;
                    var tip = bladeBase + bladeDirection * length;
                    var halfWidth = width * 0.5f;
                    var color = grassPalette;
                    AddTriangle(vertices, bladeBase - side * halfWidth, middle + side * halfWidth * 0.46f, tip, color);
                    AddTriangle(vertices, tip, middle - side * halfWidth * 0.46f, bladeBase + side * halfWidth, Shade(color, 0.88f));
                }

                clusters++;
                // Flowering is omitted until species and reproductive state are exposed by the snapshot.
            }
        }

        return new GroundCoverBuildResult(vertices.ToArray(), clusters, understoryShrubs);
    }

    private static bool AddUnderstoryShrub(List<VertexPositionColor> vertices, WorldSnapshot snapshot,
        WorldCell cell, float cellSizeM, Func<double, double, double> elevationSampler,
        VisualBiomeWeights biome, float grassCover, float moisture, float nearbyWoodland, float slope)
    {
        var meadow = SmoothStep(0.18, 0.72, grassCover);
        var slopeExposure = SmoothStep(0.35, 0.95, slope);
        var habitat = meadow
            * (0.42f + moisture * 0.58f)
            * (1 - nearbyWoodland * 0.48f)
            * (1 - biome.Marsh * 0.72f)
            * (1 - slopeExposure * 0.38f);
        // Keep the expected shrub frequency close to the previous recipe while correlating
        // occurrence at 30 m and 150 m scales. These renderer-only scales make patches rather
        // than a uniformly sprinkled layer; they do not create or move snapshot organisms.
        var neighborhoodPatch = CounterRandom.SpatialUnit(snapshot.Seed, UnderstoryMosaicTag,
            cell.X, cell.Y, 3, 0, 1);
        var landscapePatch = CounterRandom.SpatialUnit(snapshot.Seed, UnderstoryMosaicTag,
            cell.X, cell.Y, 15, 0, 2);
        var patchIntensity = (float)(neighborhoodPatch * 0.64 + landscapePatch * 0.36);
        var patchFactor = 0.22f + patchIntensity * 1.56f;
        var chance = Math.Clamp(0.48f * habitat * patchFactor, 0, 0.72f);
        if (Unit(snapshot.Seed, 0x554E444552535452UL, (ulong)cell.Index, 0, 0, 0) >= chance)
            return false;

        var xFraction = 0.14 + Unit(snapshot.Seed, 0x5348525542504F58UL, (ulong)cell.Index, 0, 0, 0) * 0.72;
        var zFraction = 0.14 + Unit(snapshot.Seed, 0x5348525542504F5AUL, (ulong)cell.Index, 0, 0, 0) * 0.72;
        var cellX = cell.X + xFraction;
        var cellZ = cell.Y + zFraction;
        var basePoint = new Vector3((float)(cellX * cellSizeM),
            (float)elevationSampler(cellX, cellZ) + 0.04f, (float)(cellZ * cellSizeM));
        var radius = (2.60f + grassCover * 1.50f + moisture * 0.50f)
            * (0.84f + patchIntensity * 0.68f);
        var height = (1.15f + grassCover * 0.85f + moisture * 0.40f)
            * (0.88f + patchIntensity * 0.36f);
        var phase = (float)Unit(snapshot.Seed, 0x5348525542504841UL, (ulong)cell.Index, 0, 0, 0) * Tau;
        var baseColor = UnderstoryColor();

        var groundMassColor = Color.Lerp(baseColor, new Color(48, 65, 39), 0.48f);
        ProceduralFloraMeshBuilder.AddShrubLobe(vertices,
            basePoint + Vector3.Up * 0.08f,
            new Vector3(radius * 0.84f, 0.10f, radius * 0.84f), groundMassColor,
            snapshot.Seed ^ (ulong)cell.Index ^ 0x534852554247524FUL, 4);

        for (var lobe = 0; lobe < 3; lobe++)
        {
            var angle = phase + Tau * lobe / 3;
            var scale = lobe == 0 ? 1f : 0.78f;
            var offset = lobe == 0 ? 0f : radius * 0.28f;
            var center = basePoint + new Vector3(MathF.Cos(angle) * offset,
                height * (lobe == 0 ? 0.60f : 0.46f), MathF.Sin(angle) * offset);
            var radii = new Vector3(radius * (lobe == 0 ? 0.76f : 0.62f) * scale,
                height * (lobe == 0 ? 0.62f : 0.52f), radius * (lobe == 0 ? 0.74f : 0.62f) * scale);
            ProceduralFloraMeshBuilder.AddShrubLobe(vertices, center, radii,
                baseColor, snapshot.Seed ^ (ulong)(cell.Index * 3 + lobe), lobe);
        }
        return true;
    }

    private static float[] BuildNearbyWoodland(WorldSnapshot snapshot,
        int minimumCellX, int minimumCellY, int maximumCellX, int maximumCellY)
    {
        var density = new float[snapshot.Cells.Count];
        var startX = Math.Max(0, minimumCellX - 1);
        var startY = Math.Max(0, minimumCellY - 1);
        var endX = Math.Min(snapshot.Width - 1, maximumCellX + 1);
        var endY = Math.Min(snapshot.Height - 1, maximumCellY + 1);
        for (var y = startY; y <= endY; y++)
        for (var x = startX; x <= endX; x++)
        {
            var weightedCover = 0f;
            var totalWeight = 0f;
            for (var offsetY = -1; offsetY <= 1; offsetY++)
            for (var offsetX = -1; offsetX <= 1; offsetX++)
            {
                var sampleX = x + offsetX;
                var sampleY = y + offsetY;
                if (sampleX < 0 || sampleY < 0 || sampleX >= snapshot.Width || sampleY >= snapshot.Height) continue;
                var sample = snapshot.Cells[sampleY * snapshot.Width + sampleX];
                var weight = offsetX == 0 && offsetY == 0 ? 2f : 1f;
                var woodyCover = sample.Woody.Present
                    ? (float)(1 - Math.Exp(-Math.Max(0, sample.Woody.BiomassGPerM2) / 420))
                    : 0;
                weightedCover += woodyCover * weight;
                totalWeight += weight;
            }
            density[y * snapshot.Width + x] = totalWeight > 0 ? weightedCover / totalWeight : 0;
        }
        return density;
    }

    private static float TerrainSlope(WorldSnapshot snapshot, WorldCell cell, float cellSizeM)
    {
        var west = snapshot.Cells[cell.Y * snapshot.Width + Math.Max(0, cell.X - 1)].ElevationM;
        var east = snapshot.Cells[cell.Y * snapshot.Width + Math.Min(snapshot.Width - 1, cell.X + 1)].ElevationM;
        var north = snapshot.Cells[Math.Max(0, cell.Y - 1) * snapshot.Width + cell.X].ElevationM;
        var south = snapshot.Cells[Math.Min(snapshot.Height - 1, cell.Y + 1) * snapshot.Width + cell.X].ElevationM;
        var dx = (east - west) / (2 * cellSizeM);
        var dz = (south - north) / (2 * cellSizeM);
        return (float)Math.Sqrt(dx * dx + dz * dz);
    }

    private static Color UnderstoryColor() => new(76, 128, 58);

    private static double SmoothStep(double edge0, double edge1, double value)
    {
        var t = Math.Clamp((value - edge0) / (edge1 - edge0), 0, 1);
        return t * t * (3 - 2 * t);
    }

    private static void AddMarshReeds(List<VertexPositionColor> vertices, ulong seed, int cellIndex,
        int cellX, int cellY, float cellSizeM, Func<double, double, double> elevationSampler, float marsh)
    {
        var centerX = cellX + 0.18 + Unit(seed, 0x4D41525348504F53UL, (ulong)cellIndex, 0, 0, 0) * 0.64;
        var centerZ = cellY + 0.18 + Unit(seed, 0x4D41525348504F53UL, (ulong)cellIndex, 1, 0, 0) * 0.64;
        var ground = new Vector3((float)(centerX * cellSizeM),
            (float)elevationSampler(centerX, centerZ) + 0.08f, (float)(centerZ * cellSizeM));
        var stems = 5 + (int)(Unit(seed, 0x4D4152534853544DUL, (ulong)cellIndex, 0, 0, 0) * 4);
        var baseHeight = 1.75f + marsh * 1.10f;
        for (var stem = 0; stem < stems; stem++)
        {
            var key = (ulong)stem;
            var angle = (float)(Unit(seed, 0x4D41525348414E47UL, (ulong)cellIndex, key, 0, 0) * Tau);
            var radius = 0.22f + (float)Unit(seed, 0x4D41525348524144UL, (ulong)cellIndex, key, 0, 0) * 0.52f;
            var basePoint = ground + new Vector3(MathF.Cos(angle) * radius, 0, MathF.Sin(angle) * radius);
            var height = baseHeight * (0.72f + (float)Unit(seed, 0x4D415253484854UL, (ulong)cellIndex, key, 0, 0) * 0.48f);
            var lean = new Vector3(MathF.Cos(angle + 0.7f), 0, MathF.Sin(angle + 0.7f))
                * (0.12f + (float)Unit(seed, 0x4D415253484C4541UL, (ulong)cellIndex, key, 0, 0) * 0.22f);
            var tip = basePoint + Vector3.Up * height + lean;
            var side = Vector3.Normalize(new Vector3(-MathF.Sin(angle), 0.1f, MathF.Cos(angle))) * 0.09f;
            var color = new Color(76, 128, 58);
            AddTriangle(vertices, basePoint - side, basePoint + side, tip, color);
            if ((stem + cellIndex) % 3 == 0)
            {
                var head = tip - Vector3.Up * 0.16f;
                var headSide = side * 1.1f;
                AddTriangle(vertices, head - headSide, head + headSide, tip + Vector3.Up * 0.10f,
                    color);
            }
        }
    }

    private static double Unit(ulong seed, ulong salt, ulong a, ulong b, ulong c, ulong d)
        => CounterRandom.Unit(seed, salt, a, b, c, d);

    private static Color Shade(Color color, float factor) => new(
        (byte)Math.Clamp((int)MathF.Round(color.R * factor), 0, 255),
        (byte)Math.Clamp((int)MathF.Round(color.G * factor), 0, 255),
        (byte)Math.Clamp((int)MathF.Round(color.B * factor), 0, 255));

    private static void AddTriangle(List<VertexPositionColor> vertices, Vector3 first, Vector3 second, Vector3 third, Color color)
    {
        vertices.Add(new VertexPositionColor(first, color));
        vertices.Add(new VertexPositionColor(second, color));
        vertices.Add(new VertexPositionColor(third, color));
    }
}
