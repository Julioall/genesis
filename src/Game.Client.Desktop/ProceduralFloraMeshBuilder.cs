using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Game.Contracts;
using Game.Modules.Biosphere;

namespace Game.Client.Desktop;

internal readonly record struct ProceduralTreeInstance(
    Vector3 Ground,
    float Height,
    int RenderProfileRow,
    float Maturity,
    ulong Seed,
    Season Season);

/// <summary>Converts camera-independent botanical structures into the current MonoGame representation.</summary>
internal static class ProceduralFloraMeshBuilder
{
    private const float Tau = MathF.PI * 2;
    private const float CanopySpread = 2.40f;
    private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(-0.38f, 0.82f, 0.43f));
    private static readonly float IcoPhi = (1 + MathF.Sqrt(5)) * 0.5f;
    private static readonly Vector3[] CanopyVertices = CreateCanopyVertices();
    private static readonly int[] CanopyFaces =
    [
        0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
        1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
        3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
        4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
    ];
    private static readonly (Vector3[] Vertices, int[] Faces) SmoothCanopy = SubdivideCanopy();

    public static VertexPositionColorTexture[] Build(
        IReadOnlyList<ProceduralTreeInstance> trees,
        float projectionPixelScale,
        bool perspectiveProjection,
        Vector3 cameraPosition,
        bool volumeCanopy = false,
        bool distanceAwareLod = false,
        bool branchesOnly = false)
    {
        // A conservative initial capacity avoids the previous multi-thousand-vertex reservation per tree.
        var vertices = new List<VertexPositionColorTexture>(Math.Min(trees.Count * 1200, 1_500_000));
        foreach (var tree in trees)
            AddTree(vertices, tree, projectionPixelScale, perspectiveProjection, cameraPosition,
                volumeCanopy, distanceAwareLod, branchesOnly);
        return vertices.ToArray();
    }

    private static void AddTree(
        List<VertexPositionColorTexture> vertices,
        ProceduralTreeInstance tree,
        float projectionPixelScale,
        bool perspectiveProjection,
        Vector3 cameraPosition,
        bool volumeCanopy,
        bool distanceAwareLod,
        bool branchesOnly)
    {
        var maturity = Math.Clamp(tree.Maturity, 0.12f, 1f);
        var crownCenter = tree.Ground + Vector3.Up * (tree.Height * 0.5f);
        var distanceToCamera = Math.Max(1f, Vector3.Distance(cameraPosition, crownCenter));
        var projectedHeight = perspectiveProjection
            ? tree.Height * projectionPixelScale / distanceToCamera
            : tree.Height * projectionPixelScale;
        var detailLevel = distanceAwareLod
            ? projectedHeight < 48 ? 0 : projectedHeight < 128 ? 1 : 2
            : projectedHeight < 35 ? 0 : projectedHeight < 60 ? 1 : 2;
        var structure = ProceduralTreeGenerator.Generate(tree.RenderProfileRow, tree.Height, maturity, tree.Seed, detailLevel);
        var profile = structure.Profile;
        var bark = BarkColor(profile.RenderRow);
        var mapScaleLod = distanceAwareLod && detailLevel == 0;
        var lastTrunkSegment = -1;
        var sameOrderSuccessors = new int[structure.Branches.Count];
        var branchJunctionChildren = new List<int>?[structure.Branches.Count];
        Array.Fill(sameOrderSuccessors, -1);
        for (var index = 0; index < structure.Branches.Count; index++)
        {
            var parentIndex = structure.Branches[index].ParentIndex;
            if (parentIndex >= 0 && structure.Branches[parentIndex].Order == structure.Branches[index].Order)
                sameOrderSuccessors[parentIndex] = index;
            else if (parentIndex >= 0 && detailLevel > 0 && (structure.Branches[parentIndex].Order <= 1
                || detailLevel == 2 && structure.Branches[parentIndex].Order == 2))
            {
                // Close LOD includes smaller collars at secondary-branch forks; coarser
                // LODs keep plain tubes to bound the extra geometry in dense forests.
                (branchJunctionChildren[parentIndex] ??= []).Add(index);
            }
        }

        if (mapScaleLod)
        {
            for (var index = 0; index < structure.Branches.Count; index++)
                if (structure.Branches[index].Order == 0) lastTrunkSegment = index;
        }

        for (var index = 0; index < structure.Branches.Count; index++)
        {
            var branch = structure.Branches[index];
            var parentIndex = branch.ParentIndex;
            if (parentIndex >= 0 && structure.Branches[parentIndex].Order == branch.Order) continue;
            if (mapScaleLod && branch.Order > 1) continue;
            if (mapScaleLod && branch.Order == 0)
            {
                if (index != 0 || lastTrunkSegment < 0) continue;
                var trunkTop = structure.Branches[lastTrunkSegment];
                AddTube(vertices, tree.Ground + ToXna(branch.Start), tree.Ground + ToXna(trunkTop.End),
                    branch.StartRadius, trunkTop.EndRadius, bark, tree.Seed, 5, tree.RenderProfileRow);
                continue;
            }
            var branchColor = branch.Order == 0 ? bark : Shade(bark, Math.Clamp(0.92f - branch.Order * 0.045f, 0.72f, 0.92f));
            var sides = detailLevel == 0
                ? branch.Order == 0 ? 6 : 3
                : branch.Order switch { 0 => 10, 1 => 8, 2 => 6, 3 => 5, _ => 4 };
            var hasBranchJunction = branch.Order > 0 && parentIndex >= 0;
            var parentRadius = hasBranchJunction
                ? RadiusAtAttachment(structure.Branches[parentIndex], branch.Start)
                : 0f;
            AddBranchChain(vertices, tree.Ground, structure.Branches, sameOrderSuccessors, index,
                branchColor, sides, tree.RenderProfileRow, hasBranchJunction, parentRadius,
                branchJunctionChildren, detailLevel == 0 ? 2 : 1);
        }

        // Small exposed roots anchor the trunk visually to uneven ground.
        if (detailLevel > 0) AddRootFlare(vertices, tree, structure, bark);
        if (branchesOnly) return;

        if (tree.Season == Season.Winter && profile.LeafHabit == LeafHabit.Deciduous) return;
        var palette = HealthyFoliageColor(profile.RenderRow);
        if (distanceAwareLod && detailLevel == 0)
        {
            AddDistantCanopy(vertices, tree, structure, palette);
            return;
        }

        for (var index = 0; index < structure.Foliage.Count; index++)
        {
            var site = structure.Foliage[index];
            var center = tree.Ground + ToXna(site.Position);
            var direction = Vector3.Normalize(ToXna(site.Direction));
            var outward = Vector3.Normalize(ToXna(site.Outward));
            var leafSizeFactor = LeafSizeFactor(profile, site.Position.Y, tree.Height);
            AddFoliageCluster(vertices, center, direction, outward, site.Scale,
                palette, tree.Seed ^ (ulong)(index + 31), profile.Foliage == FoliageMorphology.Needle,
                profile.Architecture, detailLevel, volumeCanopy, profile.RenderRow,
                tree.Height, leafSizeFactor);
        }
    }

    private static float LeafSizeFactor(TreeRenderProfile profile, float siteHeight, float treeHeight)
    {
        if (profile.LeafVerticalPattern == LeafVerticalPattern.Uniform) return 1f;

        var crownSpan = Math.Max(0.001f, profile.CrownTop - profile.CrownBase);
        var positionInCrown = Math.Clamp(
            (siteHeight / Math.Max(0.001f, treeHeight) - profile.CrownBase) / crownSpan,
            0f,
            1f);
        var depthFromTop = 1f - positionInCrown;

        // The amplitudes are deliberately small illustrative values, not coefficients
        // fitted to the observed species or to the fictional ecosystem.
        return profile.LeafVerticalPattern switch
        {
            LeafVerticalPattern.LargerTowardCrownBase => 0.90f + depthFromTop * 0.20f,
            LeafVerticalPattern.MidCrownMaximum => 0.90f + (1f - MathF.Abs(positionInCrown * 2f - 1f)) * 0.20f,
            _ => 1f
        };
    }

    private static void AddDistantCanopy(
        List<VertexPositionColorTexture> vertices,
        ProceduralTreeInstance tree,
        ProceduralTreeStructure structure,
        Color palette)
    {
        // At map scale, replace individual leaf cards with a small, volumetric crown mesh.
        // The species profile supplies the crown bounds and architecture; the stable tree seed
        // only varies the shape within those biological constraints.
        var profile = structure.Profile;
        var crownHeight = tree.Height * (profile.CrownTop - profile.CrownBase);
        var crownWidth = tree.Height * profile.CrownWidth;
        var crownCenter = tree.Ground
            + Vector3.Up * (tree.Height * (profile.CrownBase + profile.CrownTop) * 0.5f);
        var yaw = Unit(tree.Seed, 0x4449535443524F57UL, 0) * Tau;

        if (profile.Architecture == TreeArchitecture.ConicalConifer)
        {
            AddDistantConiferCrown(vertices, crownCenter, crownWidth, crownHeight,
                palette, tree.Seed, profile.RenderRow, yaw);
            return;
        }

        // The coarse crown is reduced from actual terminal-shoot attachments instead of
        // replacing every broadleaf with a fixed handful of ellipsoids. Farthest-point
        // sampling keeps a small, stable set that spans the branch scaffold at map scale.
        var sites = structure.Foliage;
        if (sites.Count == 0) return;
        var clusterCount = Math.Min(sites.Count, Math.Clamp((int)MathF.Round(MathF.Sqrt(sites.Count) * 1.35f), 4, 10));
        var selectedSites = SelectDistantFoliageSites(sites,
            tree.Height * (profile.CrownBase + profile.CrownTop) * 0.5f,
            crownHeight, crownWidth, clusterCount);
        for (var index = 0; index < selectedSites.Length; index++)
        {
            var site = sites[selectedSites[index]];
            var center = tree.Ground + ToXna(site.Position);
            var direction = NormalizeOr(ToXna(site.Direction), Vector3.Up);
            var outward = NormalizeOr(ToXna(site.Outward), Vector3.Right);
            var scale = site.Scale * (1.22f + Unit(tree.Seed, 0x44495354434C5553UL, (ulong)index) * 0.24f);
            // Keep coarse crowns made of branch-oriented card fans and leaf blades rather
            // than substituting the terminal shoots with spheres. This is a bounded foliage
            // budget sampled from the same attachment sites used by the close LOD.
            AddFoliageCluster(vertices, center, direction, outward, scale, palette,
                tree.Seed ^ (ulong)(index + 31), false, profile.Architecture,
                1, false, profile.RenderRow, tree.Height,
                LeafSizeFactor(profile, site.Position.Y, tree.Height));
        }
    }

    private static int[] SelectDistantFoliageSites(
        IReadOnlyList<TreeFoliageSite> sites,
        float crownCenterY,
        float crownHeight,
        float crownWidth,
        int count)
    {
        var widthSquared = Math.Max(0.001f, crownWidth * crownWidth);
        var heightSquared = Math.Max(0.001f, crownHeight * crownHeight);
        var positions = new Vector3[sites.Count];
        for (var index = 0; index < sites.Count; index++)
            positions[index] = ToXna(sites[index].Position);

        float CrownDistanceSquared(Vector3 first, Vector3 second)
        {
            var delta = first - second;
            return (delta.X * delta.X + delta.Z * delta.Z) / widthSquared
                + 1.35f * delta.Y * delta.Y / heightSquared;
        }

        var selected = new int[count];
        var minimumDistances = new float[sites.Count];
        Array.Fill(minimumDistances, float.PositiveInfinity);
        var firstIndex = 0;
        var largestEnvelopeDistance = float.NegativeInfinity;
        for (var index = 0; index < positions.Length; index++)
        {
            var score = CrownDistanceSquared(positions[index], new Vector3(0f, crownCenterY, 0f));
            if (score <= largestEnvelopeDistance) continue;
            largestEnvelopeDistance = score;
            firstIndex = index;
        }

        selected[0] = firstIndex;
        minimumDistances[firstIndex] = -1f;
        for (var sample = 1; sample < selected.Length; sample++)
        {
            var previousPosition = positions[selected[sample - 1]];
            var nextIndex = -1;
            var greatestSpacing = float.NegativeInfinity;
            for (var index = 0; index < positions.Length; index++)
            {
                if (minimumDistances[index] < 0) continue;
                minimumDistances[index] = Math.Min(minimumDistances[index],
                    CrownDistanceSquared(positions[index], previousPosition));
                if (minimumDistances[index] <= greatestSpacing) continue;
                greatestSpacing = minimumDistances[index];
                nextIndex = index;
            }
            if (nextIndex < 0) return selected[..sample];
            selected[sample] = nextIndex;
            minimumDistances[nextIndex] = -1f;
        }
        return selected;
    }

    private static void AddDistantConiferCrown(
        List<VertexPositionColorTexture> vertices,
        Vector3 center,
        float crownWidth,
        float crownHeight,
        Color palette,
        ulong seed,
        int speciesRow,
        float yaw)
    {
        const int sides = 8;
        const int rings = 7;
        var ringPoints = new Vector3[rings, sides];
        for (var ring = 0; ring < rings; ring++)
        {
            var t = ring / (float)(rings - 1);
            var y = crownHeight * (t - 0.5f);
            var tier = 0.94f + 0.06f * MathF.Cos(t * MathF.PI * 8f + 0.35f);
            var radius = crownWidth * (0.025f + MathF.Pow(1f - t, 0.76f) * 0.88f) * tier;
            for (var side = 0; side < sides; side++)
            {
                var index = (ulong)(ring * sides + side);
                var angle = yaw + Tau * side / sides
                    + (Unit(seed, 0x434F4E4946455241UL, index) - 0.5f) * 0.16f;
                var irregularity = 0.94f + Unit(seed, 0x434F4E4946455252UL, index) * 0.12f;
                ringPoints[ring, side] = center + new Vector3(MathF.Cos(angle) * radius * irregularity,
                    y, MathF.Sin(angle) * radius * irregularity);
            }
        }

        var canopyTexel = AtlasUv(speciesRow, 1, 0.5f, 0.5f);
        for (var ring = 0; ring < rings - 1; ring++)
        for (var side = 0; side < sides; side++)
        {
            var next = (side + 1) % sides;
            var lowerLeft = ringPoints[ring, side];
            var lowerRight = ringPoints[ring, next];
            var upperLeft = ringPoints[ring + 1, side];
            var upperRight = ringPoints[ring + 1, next];
            AddLitConiferTriangle(vertices, lowerLeft, upperLeft, lowerRight,
                center, palette, canopyTexel);
            AddLitConiferTriangle(vertices, lowerRight, upperLeft, upperRight,
                center, palette, canopyTexel);
        }
    }

    private static void AddLitConiferTriangle(
        List<VertexPositionColorTexture> vertices,
        Vector3 first,
        Vector3 second,
        Vector3 third,
        Vector3 center,
        Color palette,
        Vector2 canopyTexel)
    {
        var normal = Vector3.Normalize(Vector3.Cross(second - first, third - first));
        var centroid = (first + second + third) / 3f;
        if (Vector3.Dot(normal, centroid - center) < 0f) normal = -normal;
        var sunlight = Math.Max(0, Vector3.Dot(normal, LightDirection));
        var color = Shade(palette, 0.83f + sunlight * 0.24f);
        AddTexturedTriangle(vertices, first, second, third, color,
            canopyTexel, canopyTexel, canopyTexel);
    }

    private static void AddRootFlare(
        List<VertexPositionColorTexture> vertices,
        ProceduralTreeInstance tree,
        ProceduralTreeStructure structure,
        Color bark)
    {
        var rootRadius = structure.Branches[0].StartRadius;
        var rootCount = structure.Profile.Architecture == TreeArchitecture.UprightBroadleaf ? 3 : 5;
        var phase = Unit(tree.Seed, 0x524F4F54464C4152UL, 0) * Tau;
        for (var root = 0; root < rootCount; root++)
        {
            var angle = phase + Tau * root / rootCount;
            var radial = new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle));
            var reach = rootRadius * (2.4f + Unit(tree.Seed, 0x524F4F544C454E47UL, (ulong)root) * 1.1f);
            var start = tree.Ground + Vector3.Up * rootRadius * 0.72f;
            var end = tree.Ground + radial * reach + Vector3.Up * rootRadius * 0.08f;
            AddTube(vertices, start, end, rootRadius * 0.38f, rootRadius * 0.045f,
                Shade(bark, 0.84f + Unit(tree.Seed, 0x524F4F54434F4C52UL, (ulong)root) * 0.18f),
                tree.Seed ^ (ulong)root, 4, tree.RenderProfileRow);
        }
    }

    private static void AddFoliageCluster(
        List<VertexPositionColorTexture> vertices,
        Vector3 center,
        Vector3 stemDirection,
        Vector3 outward,
        float scale,
        Color baseColor,
        ulong seed,
        bool needles,
        TreeArchitecture architecture,
        int detailLevel,
        bool volumeCanopy,
        int speciesRow,
        float treeHeight,
        float leafSizeFactor)
    {
        stemDirection = NormalizeOr(stemDirection, Vector3.Up);
        outward = NormalizeOr(outward, Vector3.Right);
        var lateral = Vector3.Cross(outward, Vector3.Up);
        if (lateral.LengthSquared() < 0.01f) lateral = Vector3.Cross(outward, Vector3.Right);
        lateral = Vector3.Normalize(lateral);
        var phase = Unit(seed, 0x43414E4F5059535AUL, 0) * Tau;
        var spread = scale * CanopySpread * (volumeCanopy ? 1.18f : 1f)
            * (0.94f + Unit(seed, 0x43414E4F50535052UL, 0) * 0.20f);

        if (detailLevel == 0)
        {
            AddCanopyCard(vertices, center + stemDirection * spread * 0.06f, stemDirection,
                outward, spread, baseColor, seed, architecture, speciesRow);
            return;
        }

        if (needles)
        {
            // Individual pine needles are thinner than a screen pixel at ordinary zoom. A
            // small cross-card of procedurally drawn needle sprays preserves the branch
            // silhouette there; real needle blades remain layered on top at close range.
            var needleMassScale = detailLevel == 1 ? 0.24f : volumeCanopy ? 0.30f : 0.28f;
            AddCanopyCard(vertices, center + stemDirection * spread * 0.025f,
                stemDirection, outward, spread * needleMassScale, baseColor, seed,
                architecture, speciesRow, detailLevel == 1 ? 3 : volumeCanopy ? 5 : 4);

            // Radial needle sprays add fine detail without a dense cloud of individual
            // triangles. At map scale the full branch structure collapses to the crown LOD.
            var count = detailLevel == 0 ? 8 : detailLevel == 1 ? 12 : 20;
            for (var index = 0; index < count; index++)
            {
                var key = (ulong)index;
                var angle = phase + Tau * index / count
                    + (Unit(seed, 0x4E4545444C455048UL, key) - 0.5f) * 0.34f;
                var radial = lateral * MathF.Cos(angle) + Vector3.Up * MathF.Sin(angle) * 0.68f
                    + outward * 0.32f;
                var direction = NormalizeOr(stemDirection * 0.28f + radial * 0.72f + outward * 0.26f, radial);
                // Approximate individual needle scale from the tree height. The current
                // profile is an illustrative conifer archetype, not a measured pine taxon.
                var length = treeHeight * 0.0045f
                    * (0.88f + Unit(seed, 0x4E4545444C454E47UL, key) * 0.24f)
                    * leafSizeFactor;
                var origin = center + radial * spread * 0.09f;
                AddLeafBlade(vertices, origin, direction, outward, length, length * 0.075f,
                    baseColor, seed, key, speciesRow);
            }
            return;
        }

        // Each terminal shoot carries a small fan of pointed, folded leaf blades. The
        // overlapping fans make a broken canopy edge and keep visible gaps for branch depth.
        var coreColor = baseColor;
        // Use overlapping, alpha-cut leaf-mass cards behind the individual blades.
        // A smooth sphere at every terminal shoot reads as a string of low-poly balls
        // at close range, while crossed textured cards keep a broken leafy silhouette
        // and cost fewer vertices than subdivided canopy lobes.
        var massScale = detailLevel == 1 ? 0.32f : volumeCanopy ? 0.30f : 0.26f;
        var massPlanes = detailLevel == 1 ? 3 : volumeCanopy ? 5 : 4;
        AddCanopyCard(vertices, center + stemDirection * spread * 0.025f,
            stemDirection, outward, spread * massScale, coreColor, seed,
            architecture, speciesRow, massPlanes);

        var leafCount = volumeCanopy
            ? detailLevel == 1 ? 9 : 32
            : detailLevel == 1 ? 9 : 24;
        var leafLengthFraction = speciesRow switch
        {
            0 => 0.0036f, // broad, lobed leaves
            1 => 0.0045f, // conifer needles
            2 => 0.0024f, // smaller serrated leaves
            3 => 0.0038f, // palmate leaves
            4 => 0.0030f, // narrow, lanceolate leaves
            _ => 0.0034f
        };
        var leafWidthScale = speciesRow switch
        {
            1 => 0.075f,
            2 => 0.36f,
            4 => 0.235f,
            3 => 0.42f,
            _ => 0.41f
        };
        for (var index = 0; index < leafCount; index++)
        {
            var key = (ulong)index;
            var angle = phase + Tau * index / leafCount
                + (Unit(seed, 0x4C45414646414E50UL, key) - 0.5f) * 0.30f;
            var fan = lateral * MathF.Cos(angle) + Vector3.Up * MathF.Sin(angle) * 0.52f;
            var direction = NormalizeOr(outward * 0.48f + stemDirection * 0.28f + fan * 0.46f, outward);
            var origin = center + fan * spread * (0.12f + Unit(seed, 0x4C4541464F524947UL, key) * 0.10f);
            // Keep leaf dimensions tied to the tree's world scale, not to the crown
            // cluster radius. The older crown-relative values produced oversized blades.
            var length = treeHeight * leafLengthFraction
                * (0.88f + Unit(seed, 0x4C4541464C454E47UL, key) * 0.24f)
                * leafSizeFactor;
            AddLeafBlade(vertices, origin, direction, fan, length, length * leafWidthScale,
                baseColor, seed, key, speciesRow);
        }
    }

    private static void AddCanopyCard(
        List<VertexPositionColorTexture> vertices,
        Vector3 center,
        Vector3 stemDirection,
        Vector3 outward,
        float spread,
        Color baseColor,
        ulong seed,
        TreeArchitecture architecture,
        int speciesRow,
        int planeCount = 2)
    {
        var halfWidth = spread * (architecture switch
        {
            TreeArchitecture.ConicalConifer => 0.92f,
            TreeArchitecture.UprightBroadleaf => 1.12f,
            TreeArchitecture.WeepingBroadleaf => 0.90f,
            _ => 1.34f
        });
        var halfHeight = spread * (architecture switch
        {
            TreeArchitecture.ConicalConifer => 1.10f,
            TreeArchitecture.UprightBroadleaf => 1.22f,
            TreeArchitecture.WeepingBroadleaf => 1.02f,
            _ => 0.94f
        });
        var vertical = Vector3.Up;
        var facing = NormalizeOr(outward + stemDirection * 0.18f, Vector3.Forward);
        for (var plane = 0; plane < planeCount; plane++)
        {
            var angle = Unit(seed, 0x43415244414E474CUL, (ulong)plane) * Tau + plane * Tau / planeCount;
            var horizontal = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
            if (Vector3.Dot(horizontal, facing) < 0f) horizontal = -horizontal;
            var bottom = center - vertical * (halfHeight * 0.56f);
            var top = center + vertical * (halfHeight * 0.58f);
            var lowerLeft = bottom - horizontal * halfWidth;
            var lowerRight = bottom + horizontal * halfWidth;
            var upperLeft = top - horizontal * (halfWidth * 0.78f);
            var upperRight = top + horizontal * (halfWidth * 0.78f);
            var cardNormal = Vector3.Normalize(Vector3.Cross(horizontal, vertical));
            var color = Shade(baseColor, 0.84f + Math.Max(0, Vector3.Dot(cardNormal, LightDirection)) * 0.24f);
            var lowerLeftUv = AtlasUv(speciesRow, 1, 0f, 1f);
            var lowerRightUv = AtlasUv(speciesRow, 1, 1f, 1f);
            var upperLeftUv = AtlasUv(speciesRow, 1, 0.11f, 0f);
            var upperRightUv = AtlasUv(speciesRow, 1, 0.89f, 0f);
            AddTexturedTriangle(vertices, lowerLeft, upperLeft, lowerRight, color,
                lowerLeftUv, upperLeftUv, lowerRightUv);
            AddTexturedTriangle(vertices, lowerRight, upperLeft, upperRight, color,
                lowerRightUv, upperLeftUv, upperRightUv);
        }
    }

    private static void AddLeafBlade(
        List<VertexPositionColorTexture> vertices,
        Vector3 basePoint,
        Vector3 direction,
        Vector3 orientation,
        float length,
        float halfWidth,
        Color color,
        ulong seed,
        ulong key,
        int speciesRow)
    {
        var side = Vector3.Cross(direction, orientation);
        if (side.LengthSquared() < 0.01f) side = Vector3.Cross(direction, Vector3.Right);
        side = Vector3.Normalize(side);
        var normal = NormalizeOr(Vector3.Cross(direction, side), Vector3.Up);
        var bend = (Unit(seed, 0x4C45414642454E44UL, key) - 0.5f) * length * 0.12f;
        var tip = basePoint + direction * length + side * bend + normal * (length * 0.025f);
        var middle = basePoint + direction * (length * 0.51f) + side * (bend * 0.28f);
        var left = middle - side * halfWidth - normal * (length * 0.018f);
        var right = middle + side * halfWidth - normal * (length * 0.018f);
        var light = Math.Max(0, Vector3.Dot(normal, LightDirection));
        var face = Shade(color, 0.82f + light * 0.30f);
        var petioleUv = AtlasUv(speciesRow, 2, 0.5f, 1f);
        var leftUv = AtlasUv(speciesRow, 2, 0f, 0.49f);
        var tipUv = AtlasUv(speciesRow, 2, 0.5f, 0f);
        var rightUv = AtlasUv(speciesRow, 2, 1f, 0.49f);
        AddTexturedTriangle(vertices, basePoint, left, tip, face, petioleUv, leftUv, tipUv);
        AddTexturedTriangle(vertices, basePoint, tip, right, face, petioleUv, tipUv, rightUv);
    }

    private static Vector3 NormalizeOr(Vector3 value, Vector3 fallback)
        => value.LengthSquared() < 0.0001f ? Vector3.Normalize(fallback) : Vector3.Normalize(value);

    private static void AddCanopyLobe(
        List<VertexPositionColor> vertices,
        Vector3 center,
        Vector3 radii,
        Color baseColor,
        ulong seed,
        int lobe,
        int detailLevel)
    {
        var key = (ulong)lobe;
        var rotation = Matrix.CreateFromYawPitchRoll(
            Unit(seed, 0x43414E4F50595259UL, key) * Tau,
            (Unit(seed, 0x43414E4F50525043UL, key) - 0.5f) * 0.62f,
            (Unit(seed, 0x43414E4F50524F4CUL, key) - 0.5f) * 0.62f);
        var canopyVertices = detailLevel >= 1 ? SmoothCanopy.Vertices : CanopyVertices;
        var canopyFaces = detailLevel >= 1 ? SmoothCanopy.Faces : CanopyFaces;
        var transformed = new Vector3[canopyVertices.Length];
        for (var index = 0; index < canopyVertices.Length; index++)
        {
            var unit = Vector3.Transform(canopyVertices[index], rotation);
            var irregularity = detailLevel >= 1
                ? 0.96f + Unit(seed, 0x43414E4F50495252UL, key * 31 + (ulong)index) * 0.08f
                : 0.92f + Unit(seed, 0x43414E4F50495252UL, key * 31 + (ulong)index) * 0.16f;
            transformed[index] = center + new Vector3(unit.X * radii.X, unit.Y * radii.Y, unit.Z * radii.Z) * irregularity;
        }

        for (var face = 0; face < canopyFaces.Length; face += 3)
        {
            var first = transformed[canopyFaces[face]];
            var second = transformed[canopyFaces[face + 1]];
            var third = transformed[canopyFaces[face + 2]];
            var normal = Vector3.Normalize(Vector3.Cross(second - first, third - first));
            var centroid = (first + second + third) / 3;
            if (Vector3.Dot(normal, centroid - center) < 0) normal = -normal;
            var sunlight = Math.Max(0, Vector3.Dot(normal, LightDirection));
            // Lighting follows the generated surface normal; pigment remains species/state data.
            var facet = detailLevel >= 1 ? 0.86f + sunlight * 0.18f : 0.83f + sunlight * 0.24f;
            var color = Shade(baseColor, facet);
            AddTriangle(vertices, first, second, third, color);
        }
    }

    private static void AddCanopyLobe(
        List<VertexPositionColorTexture> vertices,
        Vector3 center,
        Vector3 radii,
        Color baseColor,
        ulong seed,
        int lobe,
        int detailLevel,
        int speciesRow)
    {
        var key = (ulong)lobe;
        var rotation = Matrix.CreateFromYawPitchRoll(
            Unit(seed, 0x43414E4F50595259UL, key) * Tau,
            (Unit(seed, 0x43414E4F50525043UL, key) - 0.5f) * 0.62f,
            (Unit(seed, 0x43414E4F50524F4CUL, key) - 0.5f) * 0.62f);
        var canopyVertices = detailLevel >= 1 ? SmoothCanopy.Vertices : CanopyVertices;
        var canopyFaces = detailLevel >= 1 ? SmoothCanopy.Faces : CanopyFaces;
        var transformed = new Vector3[canopyVertices.Length];
        var textureCoordinates = new Vector2[canopyVertices.Length];
        for (var index = 0; index < canopyVertices.Length; index++)
        {
            var unit = Vector3.Transform(canopyVertices[index], rotation);
            var irregularity = detailLevel >= 1
                ? 0.96f + Unit(seed, 0x43414E4F50595252UL, key * 31 + (ulong)index) * 0.08f
                : 0.92f + Unit(seed, 0x43414E4F50595252UL, key * 31 + (ulong)index) * 0.16f;
            transformed[index] = center + new Vector3(unit.X * radii.X, unit.Y * radii.Y, unit.Z * radii.Z) * irregularity;
            textureCoordinates[index] = new Vector2(unit.X * 0.5f + 0.5f, 0.5f - unit.Z * 0.5f);
        }

        for (var face = 0; face < canopyFaces.Length; face += 3)
        {
            var firstIndex = canopyFaces[face];
            var secondIndex = canopyFaces[face + 1];
            var thirdIndex = canopyFaces[face + 2];
            var first = transformed[firstIndex];
            var second = transformed[secondIndex];
            var third = transformed[thirdIndex];
            var normal = Vector3.Normalize(Vector3.Cross(second - first, third - first));
            var centroid = (first + second + third) / 3;
            if (Vector3.Dot(normal, centroid - center) < 0) normal = -normal;
            var sunlight = Math.Max(0, Vector3.Dot(normal, LightDirection));
            var facet = detailLevel >= 1 ? 0.86f + sunlight * 0.18f : 0.83f + sunlight * 0.24f;
            AddTexturedTriangle(vertices, first, second, third, Shade(baseColor, facet),
                AtlasUv(speciesRow, 1, textureCoordinates[firstIndex]),
                AtlasUv(speciesRow, 1, textureCoordinates[secondIndex]),
                AtlasUv(speciesRow, 1, textureCoordinates[thirdIndex]));
        }
    }

    internal static void AddShrubLobe(List<VertexPositionColor> vertices, Vector3 center,
        Vector3 radii, Color color, ulong seed, int lobe)
        => AddCanopyLobe(vertices, center, radii, color, seed, lobe, detailLevel: 0);

    private static Vector3[] CreateCanopyVertices()
    {
        var phi = IcoPhi;
        Vector3[] vertices =
        [
            new(-1, phi, 0), new(1, phi, 0), new(-1, -phi, 0), new(1, -phi, 0),
            new(0, -1, phi), new(0, 1, phi), new(0, -1, -phi), new(0, 1, -phi),
            new(phi, 0, -1), new(phi, 0, 1), new(-phi, 0, -1), new(-phi, 0, 1)
        ];
        for (var index = 0; index < vertices.Length; index++) vertices[index] = Vector3.Normalize(vertices[index]);
        return vertices;
    }

    private static (Vector3[] Vertices, int[] Faces) SubdivideCanopy()
    {
        var vertices = CanopyVertices.ToList();
        var midpointByEdge = new Dictionary<(int A, int B), int>();
        var faces = new List<int>(CanopyFaces.Length * 4);

        int Midpoint(int first, int second)
        {
            var edge = first < second ? (first, second) : (second, first);
            if (midpointByEdge.TryGetValue(edge, out var existing)) return existing;
            var midpoint = Vector3.Normalize((vertices[first] + vertices[second]) * 0.5f);
            var index = vertices.Count;
            vertices.Add(midpoint);
            midpointByEdge.Add(edge, index);
            return index;
        }

        for (var face = 0; face < CanopyFaces.Length; face += 3)
        {
            var first = CanopyFaces[face];
            var second = CanopyFaces[face + 1];
            var third = CanopyFaces[face + 2];
            var firstSecond = Midpoint(first, second);
            var secondThird = Midpoint(second, third);
            var thirdFirst = Midpoint(third, first);
            faces.AddRange([first, firstSecond, thirdFirst,
                second, secondThird, firstSecond,
                third, thirdFirst, secondThird,
                firstSecond, secondThird, thirdFirst]);
        }
        return (vertices.ToArray(), faces.ToArray());
    }

    private static void AddBranchChain(
        List<VertexPositionColorTexture> vertices,
        Vector3 ground,
        IReadOnlyList<TreeBranchSegment> branches,
        IReadOnlyList<int> sameOrderSuccessors,
        int firstBranchIndex,
        Color color,
        int sides,
        int speciesRow,
        bool hasBranchJunction,
        float parentRadius,
        IReadOnlyList<int>?[] branchJunctionChildren,
        int centerlineStride)
    {
        var points = new List<Vector3>(8);
        var radii = new List<float>(8);
        var current = firstBranchIndex;
        var first = branches[current];
        if (hasBranchJunction)
        {
            var junctionRadius = Math.Min(parentRadius * 0.76f, first.StartRadius * 1.35f);
            points.Add(ground + ToXna(first.Start));
            radii.Add(junctionRadius);
        }
        else
        {
            points.Add(ground + ToXna(first.Start));
            radii.Add(first.StartRadius);
        }
        var centerlineSegment = 0;
        while (current >= 0)
        {
            var segment = branches[current];
            // Shape the supporting trunk and primary boughs around larger daughter shoots.
            // Fine twig junctions stay as plain tubes to bound close-range mesh growth.
            if (branchJunctionChildren[current] is { Count: > 0 } children)
                AddBranchJunctionBulges(points, radii, ground, segment, children, branches);
            centerlineSegment++;
            if (centerlineSegment % centerlineStride == 0 || sameOrderSuccessors[current] < 0)
            {
                points.Add(ground + ToXna(segment.End));
                radii.Add(segment.EndRadius);
            }
            current = sameOrderSuccessors[current];
        }

        if (points.Count < 2) return;
        var cumulative = new float[points.Count];
        for (var pointIndex = 1; pointIndex < points.Count; pointIndex++)
            cumulative[pointIndex] = cumulative[pointIndex - 1]
                + Vector3.Distance(points[pointIndex - 1], points[pointIndex]);
        var totalLength = Math.Max(0.001f, cumulative[^1]);
        var tangents = new Vector3[points.Count];
        var normals = new Vector3[points.Count];
        var binormals = new Vector3[points.Count];

        for (var pointIndex = 0; pointIndex < points.Count; pointIndex++)
        {
            var tangent = pointIndex == 0
                ? points[1] - points[0]
                : pointIndex == points.Count - 1
                    ? points[^1] - points[^2]
                    : points[pointIndex + 1] - points[pointIndex - 1];
            if (tangent.LengthSquared() < 0.000001f) tangent = pointIndex == 0 ? Vector3.Up : tangents[pointIndex - 1];
            tangents[pointIndex] = Vector3.Normalize(tangent);

            if (pointIndex == 0)
            {
                var reference = MathF.Abs(Vector3.Dot(tangents[pointIndex], Vector3.Up)) > 0.92f
                    ? Vector3.Right
                    : Vector3.Up;
                normals[pointIndex] = Vector3.Normalize(Vector3.Cross(tangents[pointIndex], reference));
            }
            else
            {
                // Projecting the previous frame onto the new tangent plane is a stable,
                // low-cost parallel-transport approximation; it avoids the twisting that
                // appeared when every short branch segment chose a fresh reference axis.
                var transported = normals[pointIndex - 1]
                    - tangents[pointIndex] * Vector3.Dot(normals[pointIndex - 1], tangents[pointIndex]);
                if (transported.LengthSquared() < 0.0001f)
                {
                    var reference = MathF.Abs(Vector3.Dot(tangents[pointIndex], Vector3.Up)) > 0.92f
                        ? Vector3.Right
                        : Vector3.Up;
                    transported = Vector3.Cross(tangents[pointIndex], reference);
                }
                normals[pointIndex] = Vector3.Normalize(transported);
            }
            binormals[pointIndex] = Vector3.Normalize(Vector3.Cross(tangents[pointIndex], normals[pointIndex]));
        }

        var angleStep = Tau / sides;
        for (var pointIndex = 0; pointIndex < points.Count - 1; pointIndex++)
        {
            var startRadius = radii[pointIndex];
            var endRadius = radii[pointIndex + 1];
            // A small natural tip taper removes the blunt cut left by the final cylinder.
            if (pointIndex == points.Count - 2) endRadius *= 0.02f;
            var v0 = 1f - cumulative[pointIndex] / totalLength;
            var v1 = 1f - cumulative[pointIndex + 1] / totalLength;
            for (var side = 0; side < sides; side++)
            {
                var angle0 = side * angleStep;
                var angle1 = (side + 1) * angleStep;
                var radial00 = normals[pointIndex] * MathF.Cos(angle0) + binormals[pointIndex] * MathF.Sin(angle0);
                var radial01 = normals[pointIndex] * MathF.Cos(angle1) + binormals[pointIndex] * MathF.Sin(angle1);
                var radial10 = normals[pointIndex + 1] * MathF.Cos(angle0) + binormals[pointIndex + 1] * MathF.Sin(angle0);
                var radial11 = normals[pointIndex + 1] * MathF.Cos(angle1) + binormals[pointIndex + 1] * MathF.Sin(angle1);
                var p0 = points[pointIndex] + radial00 * startRadius;
                var p1 = points[pointIndex] + radial01 * startRadius;
                var p2 = points[pointIndex + 1] + radial11 * endRadius;
                var p3 = points[pointIndex + 1] + radial10 * endRadius;
                var u0 = side / (float)sides;
                var u1 = (side + 1) / (float)sides;
                var c0 = Shade(color, 0.76f + 0.42f * Math.Max(0, Vector3.Dot(radial00, LightDirection)));
                var c1 = Shade(color, 0.76f + 0.42f * Math.Max(0, Vector3.Dot(radial01, LightDirection)));
                var c2 = Shade(color, 0.76f + 0.42f * Math.Max(0, Vector3.Dot(radial11, LightDirection)));
                var c3 = Shade(color, 0.76f + 0.42f * Math.Max(0, Vector3.Dot(radial10, LightDirection)));
                AddTexturedTriangle(vertices, p0, p1, p2, c0, c1, c2,
                    AtlasUv(speciesRow, 0, u0, v0), AtlasUv(speciesRow, 0, u1, v0), AtlasUv(speciesRow, 0, u1, v1));
                AddTexturedTriangle(vertices, p0, p2, p3, c0, c2, c3,
                    AtlasUv(speciesRow, 0, u0, v0), AtlasUv(speciesRow, 0, u1, v1), AtlasUv(speciesRow, 0, u0, v1));
            }
        }
    }

    private static float RadiusAtAttachment(TreeBranchSegment parent, System.Numerics.Vector3 position)
    {
        var segment = parent.End - parent.Start;
        var lengthSquared = segment.LengthSquared();
        if (lengthSquared < 0.000001f) return parent.EndRadius;
        var along = Math.Clamp(System.Numerics.Vector3.Dot(position - parent.Start, segment) / lengthSquared, 0f, 1f);
        return parent.StartRadius + (parent.EndRadius - parent.StartRadius) * along;
    }

    private static void AddBranchJunctionBulges(
        List<Vector3> points,
        List<float> radii,
        Vector3 ground,
        TreeBranchSegment trunkSegment,
        IReadOnlyList<int> childIndices,
        IReadOnlyList<TreeBranchSegment> branches)
    {
        var axis = trunkSegment.End - trunkSegment.Start;
        var lengthSquared = axis.LengthSquared();
        if (lengthSquared < 0.000001f) return;
        var length = MathF.Sqrt(lengthSquared);
        var previousT = 0f;
        foreach (var childIndex in childIndices)
        {
            var child = branches[childIndex];
            var fraction = Math.Clamp(System.Numerics.Vector3.Dot(child.Start - trunkSegment.Start, axis)
                / lengthSquared, 0f, 1f);
            var parentRadius = trunkSegment.StartRadius
                + (trunkSegment.EndRadius - trunkSegment.StartRadius) * fraction;
            var isSecondaryBranch = trunkSegment.Order >= 2;
            var halfWidth = isSecondaryBranch
                ? Math.Clamp(parentRadius * 2.0f / length, 0.035f, 0.18f)
                : Math.Clamp(parentRadius * 2.0f / length, 0.06f, 0.28f);
            halfWidth = Math.Min(halfWidth, Math.Min(fraction * 0.48f, (1f - fraction) * 0.48f));
            var before = fraction - halfWidth;
            var after = fraction + halfWidth;
            if (halfWidth < 0.012f || before <= previousT + 0.01f || after >= 0.99f) continue;

            points.Add(ground + ToXna(System.Numerics.Vector3.Lerp(trunkSegment.Start, trunkSegment.End, before)));
            radii.Add((trunkSegment.StartRadius + (trunkSegment.EndRadius - trunkSegment.StartRadius) * before) * 1.06f);
            points.Add(ground + ToXna(child.Start));
            radii.Add(parentRadius * 1.25f);
            points.Add(ground + ToXna(System.Numerics.Vector3.Lerp(trunkSegment.Start, trunkSegment.End, after)));
            radii.Add((trunkSegment.StartRadius + (trunkSegment.EndRadius - trunkSegment.StartRadius) * after) * 1.06f);
            previousT = after;
        }
    }

    private static void AddTube(
        List<VertexPositionColorTexture> vertices,
        Vector3 start,
        Vector3 end,
        float startRadius,
        float endRadius,
        Color color,
        ulong seed,
        int sides,
        int speciesRow)
    {
        var axis = end - start;
        var length = axis.Length();
        if (length < 0.001f || startRadius <= 0 || endRadius < 0) return;
        axis /= length;
        var reference = MathF.Abs(Vector3.Dot(axis, Vector3.Up)) > 0.92f ? Vector3.Right : Vector3.Up;
        var first = Vector3.Normalize(Vector3.Cross(axis, reference));
        var second = Vector3.Normalize(Vector3.Cross(axis, first));
        var step = Tau / sides;

        for (var side = 0; side < sides; side++)
        {
            var a0 = side * step;
            var a1 = (side + 1) * step;
            var radial0 = first * MathF.Cos(a0) + second * MathF.Sin(a0);
            var radial1 = first * MathF.Cos(a1) + second * MathF.Sin(a1);
            var p0 = start + radial0 * startRadius;
            var p1 = start + radial1 * startRadius;
            var p2 = end + radial1 * endRadius;
            var p3 = end + radial0 * endRadius;
            var face = Shade(color, 0.76f + 0.42f * Math.Max(0, Vector3.Dot(radial0, LightDirection)));
            var u0 = side / (float)sides;
            var u1 = (side + 1) / (float)sides;
            AddTexturedTriangle(vertices, p0, p1, p2, face,
                AtlasUv(speciesRow, 0, u0, 1), AtlasUv(speciesRow, 0, u1, 1), AtlasUv(speciesRow, 0, u1, 0));
            AddTexturedTriangle(vertices, p0, p2, p3, face,
                AtlasUv(speciesRow, 0, u0, 1), AtlasUv(speciesRow, 0, u1, 0), AtlasUv(speciesRow, 0, u0, 0));
        }
    }

    private static Vector3 ToXna(System.Numerics.Vector3 value) => new(value.X, value.Y, value.Z);

    private static Color BarkColor(int species) => species switch
    {
        1 => new Color(143, 101, 69),
        2 => new Color(226, 224, 210),
        3 => new Color(139, 121, 99),
        4 => new Color(111, 99, 82),
        _ => new Color(112, 100, 83)
    };

    private static Vector2 AtlasUv(int speciesRow, int materialRow, float localU, float localV)
    {
        const float cell = 256f;
        const float gutter = 2f;
        const float step = cell + gutter * 2f;
        const float width = 5 * step;
        const float height = 3 * step;
        return new Vector2(
            (Math.Clamp(speciesRow, 0, 4) * step + gutter + Math.Clamp(localU, 0, 1) * cell) / width,
            (Math.Clamp(materialRow, 0, 2) * step + gutter + Math.Clamp(localV, 0, 1) * cell) / height);
    }

    private static Vector2 AtlasUv(int speciesRow, int materialRow, Vector2 local)
        => AtlasUv(speciesRow, materialRow, local.X, local.Y);

    private static void AddTexturedTriangle(
        List<VertexPositionColorTexture> vertices,
        Vector3 first,
        Vector3 second,
        Vector3 third,
        Color color,
        Vector2 firstUv,
        Vector2 secondUv,
        Vector2 thirdUv)
    {
        AddTexturedTriangle(vertices, first, second, third, color, color, color,
            firstUv, secondUv, thirdUv);
    }

    private static void AddTexturedTriangle(
        List<VertexPositionColorTexture> vertices,
        Vector3 first,
        Vector3 second,
        Vector3 third,
        Color firstColor,
        Color secondColor,
        Color thirdColor,
        Vector2 firstUv,
        Vector2 secondUv,
        Vector2 thirdUv)
    {
        vertices.Add(new VertexPositionColorTexture(first, firstColor, firstUv));
        vertices.Add(new VertexPositionColorTexture(second, secondColor, secondUv));
        vertices.Add(new VertexPositionColorTexture(third, thirdColor, thirdUv));
    }

    private static Color HealthyFoliageColor(int species) => species switch
    {
        1 => new Color(64, 109, 82),  // blue-green needle foliage
        2 => new Color(104, 140, 67), // light-green birch leaves
        3 => new Color(68, 112, 53),  // dark-green maple leaves
        4 => new Color(70, 115, 67),  // green willow foliage
        _ => new Color(76, 128, 58)   // green oak foliage
    };

    private static void AddTriangle(List<VertexPositionColor> vertices, Vector3 a, Vector3 b, Vector3 c, Color color)
    {
        vertices.Add(new VertexPositionColor(a, color));
        vertices.Add(new VertexPositionColor(b, color));
        vertices.Add(new VertexPositionColor(c, color));
    }

    private static Color Shade(Color color, float factor) => new(
        (byte)Math.Clamp((int)MathF.Round(color.R * factor), 0, 255),
        (byte)Math.Clamp((int)MathF.Round(color.G * factor), 0, 255),
        (byte)Math.Clamp((int)MathF.Round(color.B * factor), 0, 255),
        color.A);

    private static float Unit(ulong seed, ulong salt, ulong index)
        => (float)Game.Procedural.CounterRandom.Unit(seed, salt, index, 0, 0, 0);
}
