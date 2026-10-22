using System.Numerics;
using Game.Procedural;

namespace Game.Modules.Biosphere;

public enum TreeArchitecture : byte
{
    SpreadingBroadleaf,
    ConicalConifer,
    UprightBroadleaf,
    WeepingBroadleaf
}

/// <summary>Qualitative Weber–Penn crown envelope used to shape primary-bough length.</summary>
public enum TreeCrownShape : byte
{
    Conical,
    Spherical,
    Hemispherical,
    Cylindrical,
    TaperedCylindrical,
    Flame,
    InverseConical,
    TendFlame
}

public enum FoliageMorphology : byte
{
    Broadleaf,
    Needle
}

public enum LeafHabit : byte
{
    Deciduous,
    Evergreen
}

/// <summary>Qualitative crown-depth pattern; renderer magnitude is a stylized proxy, not a measured coefficient.</summary>
public enum LeafVerticalPattern : byte
{
    Uniform,
    LargerTowardCrownBase,
    MidCrownMaximum
}

/// <summary>Tree-form recipe for a deterministic skeleton; the current entries are visual archetypes, not taxa.</summary>
public sealed record TreeRenderProfile(
    int RenderRow,
    string ProfileId,
    TreeArchitecture Architecture,
    FoliageMorphology Foliage,
    LeafHabit LeafHabit,
    float CrownBase,
    float CrownTop,
    float CrownWidth,
    float TrunkRadius,
    float PrimaryBranchUp,
    float LateralBranchUp,
    float BranchDroop,
    int PrimaryBranchCount,
    int SecondaryBranches,
    int TerminalBranches,
    int FoliageSitesPerTerminal,
    int MaximumBranchOrder,
    LeafVerticalPattern LeafVerticalPattern = LeafVerticalPattern.Uniform,
    float PrimaryBranchRadiusRatio = 0.60f,
    TreeCrownShape CrownShape = TreeCrownShape.Spherical,
    float SecondaryLengthRatio = 0.60f,
    float TertiaryLengthRatio = 0.50f,
    float TerminalLengthRatio = 0.43f,
    float GravitySagRatio = 0.07f,
    float SecondaryDivergenceDegrees = 44f,
    float TerminalDivergenceDegrees = 38f);

/// <summary>A tapered, connected segment in the tree's camera-independent branch graph.</summary>
public readonly record struct TreeBranchSegment(
    int ParentIndex,
    int Order,
    Vector3 Start,
    Vector3 End,
    float StartRadius,
    float EndRadius);

/// <summary>A foliage attachment placed on a branch, rather than sampled from an arbitrary crown volume.</summary>
public readonly record struct TreeFoliageSite(
    int BranchIndex,
    Vector3 Position,
    Vector3 Direction,
    Vector3 Outward,
    float Scale);

public sealed class ProceduralTreeStructure
{
    internal ProceduralTreeStructure(
        TreeRenderProfile profile,
        float height,
        float maturity,
        TreeBranchSegment[] branches,
        TreeFoliageSite[] foliage)
    {
        Profile = profile;
        Height = height;
        Maturity = maturity;
        Branches = branches;
        Foliage = foliage;
    }

    public TreeRenderProfile Profile { get; }
    public float Height { get; }
    public float Maturity { get; }
    public IReadOnlyList<TreeBranchSegment> Branches { get; }
    public IReadOnlyList<TreeFoliageSite> Foliage { get; }
}

/// <summary>
/// Deterministic species-profile generator. It creates a hierarchical woody skeleton first;
/// renderers can then choose their own mesh, impostor, or other representation.
/// </summary>
public static class ProceduralTreeGenerator
{
    private const float Tau = MathF.PI * 2;
    private const float GoldenAngle = 2.39996323f;
    // These named profiles are visual archetypes until species-level measurements are supplied.
    private static readonly TreeRenderProfile[] Profiles =
    [
        new(0, "temperate-oak", TreeArchitecture.SpreadingBroadleaf, FoliageMorphology.Broadleaf, LeafHabit.Deciduous,
            0.43f, 0.96f, 0.39f, 0.026f, 0.36f, 0.32f, 0.02f, 7, 3, 3, 4, 4,
            LeafVerticalPattern.MidCrownMaximum, PrimaryBranchRadiusRatio: 0.66f,
            CrownShape: TreeCrownShape.Spherical, SecondaryLengthRatio: 0.65f,
            TertiaryLengthRatio: 0.54f, TerminalLengthRatio: 0.45f, GravitySagRatio: 0.13f,
            SecondaryDivergenceDegrees: 49f, TerminalDivergenceDegrees: 43f),
        new(1, "temperate-pine", TreeArchitecture.ConicalConifer, FoliageMorphology.Needle, LeafHabit.Evergreen,
            0.12f, 0.98f, 0.34f, 0.018f, -0.07f, -0.10f, 0.00f, 20, 2, 2, 2, 4,
            PrimaryBranchRadiusRatio: 0.48f, CrownShape: TreeCrownShape.Conical, SecondaryLengthRatio: 0.58f,
            TertiaryLengthRatio: 0.48f, TerminalLengthRatio: 0.40f, GravitySagRatio: 0.10f,
            SecondaryDivergenceDegrees: 37f, TerminalDivergenceDegrees: 31f),
        new(2, "temperate-birch", TreeArchitecture.UprightBroadleaf, FoliageMorphology.Broadleaf, LeafHabit.Deciduous,
            0.46f, 0.97f, 0.27f, 0.014f, 0.28f, 0.38f, 0.00f, 6, 3, 3, 4, 4,
            LeafVerticalPattern.LargerTowardCrownBase, PrimaryBranchRadiusRatio: 0.67f,
            CrownShape: TreeCrownShape.TaperedCylindrical, SecondaryLengthRatio: 0.60f,
            TertiaryLengthRatio: 0.47f, TerminalLengthRatio: 0.38f, GravitySagRatio: 0.055f,
            SecondaryDivergenceDegrees: 42f, TerminalDivergenceDegrees: 35f),
        new(3, "temperate-maple", TreeArchitecture.SpreadingBroadleaf, FoliageMorphology.Broadleaf, LeafHabit.Deciduous,
            0.48f, 0.96f, 0.37f, 0.023f, 0.34f, 0.28f, 0.00f, 7, 3, 3, 4, 4,
            PrimaryBranchRadiusRatio: 0.64f, CrownShape: TreeCrownShape.Spherical, SecondaryLengthRatio: 0.66f,
            TertiaryLengthRatio: 0.55f, TerminalLengthRatio: 0.46f, GravitySagRatio: 0.12f,
            SecondaryDivergenceDegrees: 51f, TerminalDivergenceDegrees: 45f),
        new(4, "temperate-willow", TreeArchitecture.WeepingBroadleaf, FoliageMorphology.Broadleaf, LeafHabit.Deciduous,
            0.36f, 0.86f, 0.43f, 0.020f, 0.20f, 0.10f, 0.58f, 8, 3, 3, 4, 4,
            PrimaryBranchRadiusRatio: 0.62f, CrownShape: TreeCrownShape.Hemispherical, SecondaryLengthRatio: 0.68f,
            TertiaryLengthRatio: 0.59f, TerminalLengthRatio: 0.50f, GravitySagRatio: 0.11f,
            SecondaryDivergenceDegrees: 54f, TerminalDivergenceDegrees: 49f)
    ];

    public static IReadOnlyList<TreeRenderProfile> RenderProfiles { get; } = Array.AsReadOnly(Profiles);

    public static TreeRenderProfile ProfileForRenderRow(int row)
        => Profiles[Math.Clamp(row, 0, Profiles.Length - 1)];

    private static float CrownLengthFactor(TreeCrownShape shape, float progressFromCrownBase)
    {
        var progress = Math.Clamp(progressFromCrownBase, 0f, 1f);
        var distanceFromCrownTop = 1f - progress;
        var factor = shape switch
        {
            TreeCrownShape.Conical => 0.2f + 0.8f * distanceFromCrownTop,
            TreeCrownShape.Spherical => 0.2f + 0.8f * MathF.Sin(MathF.PI * distanceFromCrownTop),
            TreeCrownShape.Hemispherical => 0.2f + 0.8f * MathF.Sin(0.5f * MathF.PI * distanceFromCrownTop),
            TreeCrownShape.Cylindrical => 1f,
            TreeCrownShape.TaperedCylindrical => 0.5f + 0.5f * distanceFromCrownTop,
            TreeCrownShape.Flame => distanceFromCrownTop <= 0.7f
                ? distanceFromCrownTop / 0.7f
                : (1f - distanceFromCrownTop) / 0.3f,
            TreeCrownShape.InverseConical => 0.2f + 0.8f * progress,
            TreeCrownShape.TendFlame => distanceFromCrownTop <= 0.7f
                ? 0.5f + 0.5f * distanceFromCrownTop / 0.7f
                : 0.5f + 0.5f * (1f - distanceFromCrownTop) / 0.3f,
            _ => 1f
        };

        // Keep a little branch volume at the crown edges so terminal shoots can bridge
        // neighboring boughs instead of ending as isolated stubs.
        return Math.Clamp(factor, 0.18f, 1f);
    }

    public static ProceduralTreeStructure Generate(int renderProfileRow, float height, float maturity, ulong seed, int detailLevel = 2)
    {
        var profile = ProfileForRenderRow(renderProfileRow);
        height = Math.Max(2f, height);
        maturity = Math.Clamp(maturity, 0.12f, 1f);
        detailLevel = Math.Clamp(detailLevel, 0, 2);
        var branches = new List<TreeBranchSegment>(64);
        var foliage = new List<TreeFoliageSite>(32);
        var primaryTips = new List<CrownTip>(profile.PrimaryBranchCount);
        var trunkSegments = 10;
        var leanX = (Unit(seed, 0x545245454C45414EUL, 0) - 0.5f) * height * 0.055f;
        var leanZ = (Unit(seed, 0x545245454C45414EUL, 1) - 0.5f) * height * 0.055f;
        var trunkBendX = (Unit(seed, 0x5452454542454E44UL, 0) - 0.5f) * height * 0.032f;
        var trunkBendZ = (Unit(seed, 0x5452454542454E44UL, 1) - 0.5f) * height * 0.032f;

        Vector3 TrunkAt(float t)
        {
            var gentleSway = MathF.Sin(MathF.PI * t) + 0.22f * MathF.Sin(MathF.Tau * t);
            return new Vector3(leanX * t * t + trunkBendX * gentleSway,
                height * t,
                leanZ * t * t + trunkBendZ * gentleSway);
        }
        float TrunkRadiusAt(float t) => height * profile.TrunkRadius * (0.13f + 0.87f * MathF.Pow(1 - t, 1.12f));

        for (var index = 0; index < trunkSegments; index++)
        {
            var t0 = index / (float)trunkSegments;
            var t1 = (index + 1) / (float)trunkSegments;
            branches.Add(new TreeBranchSegment(index == 0 ? -1 : index - 1, 0,
                TrunkAt(t0), TrunkAt(t1), TrunkRadiusAt(t0), TrunkRadiusAt(t1)));
        }

        var maturityBranchScale = 0.60f + maturity * 0.40f;
        var primaryCount = Math.Max(detailLevel == 0 ? 3 : 4,
            (int)MathF.Round(profile.PrimaryBranchCount * maturityBranchScale));
        var maximumOrder = Math.Max(2, profile.MaximumBranchOrder - (detailLevel == 2 ? 0 : 1));
        var crownSpan = profile.CrownTop - profile.CrownBase;
        var phase = Unit(seed, 0x545245454252414EUL, 0) * Tau;
        var broadleafPositions = profile.Architecture == TreeArchitecture.ConicalConifer
            ? Array.Empty<float>()
            : Enumerable.Range(0, primaryCount)
                .Select(index => Unit(seed, 0x54524545504F5349UL, (ulong)index))
                .Order()
                .ToArray();
        for (var index = 0; index < primaryCount; index++)
        {
            float progress;
            float angle;
            if (profile.Architecture == TreeArchitecture.ConicalConifer)
            {
                // Conifers form recognizable branch tiers around a persistent leader.
                // Keep each tier distinct, then perturb its height and azimuth per individual.
                const int branchesPerWhorl = 4;
                var whorl = index / branchesPerWhorl;
                var member = index % branchesPerWhorl;
                var whorlCount = (primaryCount + branchesPerWhorl - 1) / branchesPerWhorl;
                var tierPosition = (whorl + 0.5f
                    + (Unit(seed, 0x50494E4554494552UL, (ulong)whorl) - 0.5f) * 0.28f)
                    / whorlCount;
                progress = Math.Clamp(tierPosition, 0.04f, 0.96f);
                angle = phase + Tau * member / branchesPerWhorl + whorl * GoldenAngle * 0.44f
                    + (Unit(seed, 0x50494E4550484153UL, (ulong)index) - 0.5f) * 0.30f;
            }
            else
            {
                // Broadleaf boughs emerge at irregular internodes instead of evenly spaced rings.
                progress = 0.06f + broadleafPositions[index] * 0.88f;
                angle = phase + index * GoldenAngle
                    + (Unit(seed, 0x42524F4144415A49UL, (ulong)index) - 0.5f) * 0.86f;
            }
            var t = profile.CrownBase + crownSpan * progress;
            var trunkSegment = Math.Clamp((int)(t * trunkSegments), 0, trunkSegments - 1);
            var outward = new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle));
            var branchStart = TrunkAt(t);
            var length = height * profile.CrownWidth * CrownLengthFactor(profile.CrownShape, progress)
                * (0.84f + Unit(seed, 0x545245454C454E47UL, (ulong)index) * 0.32f);
            var direction = Vector3.Normalize(outward + Vector3.UnitY * profile.PrimaryBranchUp);
            var branchRadius = TrunkRadiusAt(t) * profile.PrimaryBranchRadiusRatio;
            var primaryTipIndex = GrowBranch(branchStart, direction, outward, length, branchRadius, 1, trunkSegment,
                profile, height, maturity, seed, (ulong)index, maximumOrder, detailLevel, branches, foliage);
            var primaryTip = branches[primaryTipIndex];
            primaryTips.Add(new CrownTip(primaryTip.End, primaryTipIndex, primaryTip.EndRadius, outward, 1));
        }

        if (profile.Foliage == FoliageMorphology.Broadleaf)
            GrowTowardCrownAttractors(profile, height, maturity, seed, detailLevel, primaryTips, branches, foliage);

        ApplyBranchSupport(branches, foliage);

        return new ProceduralTreeStructure(profile, height, maturity, branches.ToArray(), foliage.ToArray());
    }

    private static void ApplyBranchSupport(
        List<TreeBranchSegment> branches,
        IReadOnlyList<TreeFoliageSite> foliage)
    {
        if (branches.Count == 0 || foliage.Count == 0) return;

        // Estimate the foliage carried by each segment, then accumulate that load toward
        // the trunk. The square-root ratio is a bounded, pipe-model-inspired presentation
        // rule: branch bases carrying a larger share of the crown get a stronger junction.
        var load = new float[branches.Count];
        foreach (var site in foliage)
            if ((uint)site.BranchIndex < (uint)load.Length)
                load[site.BranchIndex] += Math.Max(0.05f, site.Scale * site.Scale);
        for (var index = branches.Count - 1; index > 0; index--)
        {
            var parentIndex = branches[index].ParentIndex;
            if (parentIndex >= 0) load[parentIndex] += load[index];
        }

        var sameOrderSuccessor = new int[branches.Count];
        Array.Fill(sameOrderSuccessor, -1);
        for (var index = 0; index < branches.Count; index++)
        {
            var parentIndex = branches[index].ParentIndex;
            if (parentIndex >= 0 && branches[parentIndex].Order == branches[index].Order)
                sameOrderSuccessor[parentIndex] = index;
        }

        var maximumOrder = branches.Max(branch => branch.Order);
        var rootsByOrder = new List<int>?[maximumOrder + 1];
        for (var index = 1; index < branches.Count; index++)
        {
            var root = branches[index];
            // Primary bough radii come directly from the profile. Their load-based
            // adjustment would change the main silhouette when a lower LOD omits
            // fine descendants; pipe support remains active for finer branch orders.
            if (root.Order <= 1 || root.ParentIndex < 0
                || branches[root.ParentIndex].Order >= root.Order
                || load[index] <= 0 || load[root.ParentIndex] <= 0)
                continue;
            (rootsByOrder[root.Order] ??= []).Add(index);
        }

        for (var order = 1; order <= maximumOrder; order++)
        foreach (var rootIndex in rootsByOrder[order] ?? [])
        {
            var root = branches[rootIndex];
            var parent = branches[root.ParentIndex];
            var parentAxis = parent.End - parent.Start;
            var axisLengthSquared = parentAxis.LengthSquared();
            var attachment = axisLengthSquared < 0.000001f
                ? parent.Start
                : parent.Start + parentAxis * Math.Clamp(
                    Vector3.Dot(root.Start - parent.Start, parentAxis) / axisLengthSquared, 0f, 1f);
            var attachmentFraction = axisLengthSquared < 0.000001f ? 0f
                : Math.Clamp(Vector3.Dot(attachment - parent.Start, parentAxis) / axisLengthSquared, 0f, 1f);
            var parentRadius = parent.StartRadius + (parent.EndRadius - parent.StartRadius) * attachmentFraction;
            var loadShare = Math.Clamp(load[rootIndex] / load[root.ParentIndex], 0f, 1f);
            var supportedRadius = parentRadius * Math.Min(0.92f, 1.30f * MathF.Sqrt(loadShare));
            var radiusScale = root.StartRadius <= 0.000001f
                ? 1f
                : Math.Clamp(supportedRadius / root.StartRadius, 0.70f, 1.45f);
            if (Math.Abs(radiusScale - 1f) <= 0.001f) continue;

            var current = rootIndex;
            var remaining = branches.Count;
            while (current >= 0 && remaining-- > 0 && branches[current].Order == order)
            {
                var segment = branches[current];
                branches[current] = segment with
                {
                    StartRadius = segment.StartRadius * radiusScale,
                    EndRadius = segment.EndRadius * radiusScale
                };
                current = sameOrderSuccessor[current];
            }
        }
    }

    private static void GrowTowardCrownAttractors(
        TreeRenderProfile profile,
        float height,
        float maturity,
        ulong seed,
        int detailLevel,
        List<CrownTip> initialTips,
        List<TreeBranchSegment> branches,
        List<TreeFoliageSite> foliage)
    {
        // A compact, deterministic space-colonization pass fills crown gaps with branchlets.
        // The species profile still controls the crown envelope and its primary architecture.
        var attractionCount = detailLevel switch { 0 => 18, 1 => 56, _ => 144 };
        var attractors = new List<Vector3>(attractionCount);
        var radius = height * profile.CrownWidth;
        var centerY = height * (profile.CrownBase + profile.CrownTop) * 0.5f;
        var halfHeight = height * (profile.CrownTop - profile.CrownBase) * 0.5f;
        for (var sample = 0; sample < attractionCount * 3 && attractors.Count < attractionCount; sample++)
        {
            var x = Unit(seed, 0x43524F574E415458UL, (ulong)(sample * 3)) * 2f - 1f;
            var y = Unit(seed, 0x43524F574E415459UL, (ulong)(sample * 3 + 1)) * 2f - 1f;
            var z = Unit(seed, 0x43524F574E41545AUL, (ulong)(sample * 3 + 2)) * 2f - 1f;
            if (x * x + y * y + z * z > 1f) continue;
            // Leave room for the trunk and bias points toward the outer half of the crown.
            var radial = MathF.Sqrt(x * x + z * z);
            if (radial < 0.24f) continue;
            var position = new Vector3(x * radius, centerY + y * halfHeight, z * radius);
            if (position.Y <= height * profile.CrownBase * 0.88f) continue;
            attractors.Add(position);
        }

        if (attractors.Count == 0) return;
        var tips = new List<CrownTip>(initialTips);
        var maxNewSegments = detailLevel switch { 0 => 3, 1 => 16, _ => 64 };
        maxNewSegments = Math.Max(1, (int)MathF.Round(maxNewSegments * (0.72f + maturity * 0.28f)));
        var influenceRadius = radius * 0.82f;
        var killRadius = radius * 0.14f;
        var stepLength = height * profile.CrownWidth * 0.14f;
        var created = 0;
        var iteration = 0;

        while (attractors.Count > 0 && created < maxNewSegments && iteration < 7)
        {
            var directionSums = new Vector3[tips.Count];
            var assignments = new int[tips.Count];
            foreach (var target in attractors)
            {
                var nearest = -1;
                var nearestDistanceSquared = influenceRadius * influenceRadius;
                for (var tipIndex = 0; tipIndex < tips.Count; tipIndex++)
                {
                    var delta = target - tips[tipIndex].Position;
                    var distanceSquared = delta.LengthSquared();
                    if (distanceSquared >= nearestDistanceSquared) continue;
                    nearest = tipIndex;
                    nearestDistanceSquared = distanceSquared;
                }
                if (nearest < 0) continue;
                directionSums[nearest] += Vector3.Normalize(target - tips[nearest].Position);
                assignments[nearest]++;
            }

            var nextTips = new List<CrownTip>(tips.Count + assignments.Count(value => value > 0));
            for (var tipIndex = 0; tipIndex < tips.Count; tipIndex++)
            {
                var tip = tips[tipIndex];
                if (assignments[tipIndex] == 0 || created >= maxNewSegments)
                {
                    nextTips.Add(tip);
                    continue;
                }

                var directionSum = directionSums[tipIndex];
                if (directionSum.LengthSquared() < 0.0001f)
                {
                    nextTips.Add(tip);
                    continue;
                }
                var averagedDirection = Vector3.Normalize(directionSum);
                var direction = Vector3.Normalize(averagedDirection * 0.72f + tip.Outward * 0.22f
                    + Vector3.UnitY * profile.LateralBranchUp * 0.24f);
                var endpoint = tip.Position + direction * stepLength;
                endpoint = ClampToCrown(endpoint, radius, centerY, halfHeight);
                if (Vector3.DistanceSquared(endpoint, tip.Position) < stepLength * stepLength * 0.12f)
                {
                    nextTips.Add(tip);
                    continue;
                }

                var minimumRadius = height * 0.001f;
                var startRadius = Math.Max(tip.Radius, minimumRadius);
                var endRadius = Math.Max(startRadius * 0.54f, minimumRadius);
                var newIndex = branches.Count;
                branches.Add(new TreeBranchSegment(tip.BranchIndex, tip.Order + 1, tip.Position,
                    endpoint, startRadius, endRadius));
                var outwardVector = new Vector3(endpoint.X, 0, endpoint.Z);
                var newOutward = outwardVector.LengthSquared() < 0.0001f ? tip.Outward : Vector3.Normalize(outwardVector);
                nextTips.Add(new CrownTip(endpoint, newIndex, endRadius, newOutward, tip.Order + 1));
                foliage.Add(new TreeFoliageSite(newIndex, endpoint,
                    Vector3.Normalize(direction + newOutward * 0.38f + Vector3.UnitY * 0.16f),
                    newOutward, height * profile.CrownWidth * 0.20f));
                created++;
            }

            attractors.RemoveAll(target => nextTips.Any(tip => Vector3.DistanceSquared(target, tip.Position) < killRadius * killRadius));
            tips = nextTips;
            iteration++;
        }
    }

    private static Vector3 ClampToCrown(Vector3 point, float radius, float centerY, float halfHeight)
    {
        var normalized = new Vector3(point.X / radius, (point.Y - centerY) / halfHeight, point.Z / radius);
        var length = normalized.Length();
        if (length <= 0.96f) return point;
        normalized *= 0.96f / length;
        return new Vector3(normalized.X * radius, centerY + normalized.Y * halfHeight, normalized.Z * radius);
    }

    private static int GrowBranch(
        Vector3 start,
        Vector3 direction,
        Vector3 outward,
        float length,
        float startRadius,
        int order,
        int parentIndex,
        TreeRenderProfile profile,
        float height,
        float maturity,
        ulong seed,
        ulong branchKey,
        int maximumOrder,
        int detailLevel,
        List<TreeBranchSegment> branches,
        List<TreeFoliageSite> foliage)
    {
        startRadius = Math.Max(startRadius, height * 0.0010f);
        var droop = profile.BranchDroop * Math.Clamp(
            (order - 1) / (float)Math.Max(1, profile.MaximumBranchOrder - 1), 0, 1);
        direction = Vector3.Normalize(direction - Vector3.UnitY * droop);
        var side = Vector3.Cross(direction, Vector3.UnitY);
        if (side.LengthSquared() < 0.01f) side = Vector3.Cross(direction, Vector3.UnitX);
        side = Vector3.Normalize(side);
        var normal = Vector3.Normalize(Vector3.Cross(direction, side));
        var bendSide = (Unit(seed, 0x5452454543555256UL, branchKey) - 0.5f) * 0.24f;
        var bendDepth = (Unit(seed, 0x5452454543555244UL, branchKey) - 0.5f) * 0.22f;
        // Gravity is a presentation-level bending rule, not a force simulation. Its
        // effect scales with branch order so terminal growth curves more than the scaffold.
        // Geometry LOD may omit higher branch orders, but must not change the
        // curvature of woody scaffold that remains visible at every distance.
        var orderProgress = order / (float)Math.Max(1, profile.MaximumBranchOrder);
        var gravitySag = length * profile.GravitySagRatio * (0.40f + 0.60f * orderProgress);
        var controlA = start + direction * (length * 0.31f)
            + side * (length * bendSide) + normal * (length * bendDepth * 0.48f)
            + Vector3.UnitY * (length * profile.LateralBranchUp * 0.035f - gravitySag * 0.08f);
        var controlB = start + direction * (length * 0.72f)
            + side * (length * bendSide * 1.24f) - normal * (length * bendDepth * 0.32f)
            + Vector3.UnitY * (length * profile.LateralBranchUp * 0.11f - gravitySag * 0.58f);
        var endpoint = start + direction * length + side * (length * bendSide * 1.48f)
            + normal * (length * bendDepth * 0.30f)
            + Vector3.UnitY * (length * profile.LateralBranchUp * 0.18f)
            - Vector3.UnitY * (length * (droop * 0.34f + profile.GravitySagRatio * (0.40f + 0.60f * orderProgress)));
        var daughterTaper = order == 1 ? 0.50f : order == 2 ? 0.54f : 0.58f;
        var endRadius = Math.Max(startRadius * daughterTaper, height * 0.0010f);
        // Keep centerline samples stable across LODs. The renderer reduces radial
        // sides and branch orders; it does not redraw a different bough curve.
        var segmentCount = order == 1 ? 4 : order == 2 ? 3 : 2;
        Vector3 PointOnBranch(float t)
        {
            var inverse = 1f - t;
            return start * (inverse * inverse * inverse)
                + controlA * (3f * inverse * inverse * t)
                + controlB * (3f * inverse * t * t)
                + endpoint * (t * t * t);
        }

        var branchSegments = new int[segmentCount];
        var previousPoint = start;
        var previousRadius = startRadius;
        for (var segment = 0; segment < segmentCount; segment++)
        {
            var t = (segment + 1f) / segmentCount;
            var point = PointOnBranch(t);
            var radius = Math.Max(startRadius * MathF.Pow(endRadius / startRadius, t), height * 0.0010f);
            var segmentIndex = branches.Count;
            branches.Add(new TreeBranchSegment(segment == 0 ? parentIndex : branchSegments[segment - 1],
                order, previousPoint, point, previousRadius, radius));
            branchSegments[segment] = segmentIndex;
            previousPoint = point;
            previousRadius = radius;
        }

        var foliageOrder = order >= maximumOrder
            ? profile.FoliageSitesPerTerminal * (detailLevel == 0 ? 0.55f : 1f)
            : order == maximumOrder - 1 ? 1 : 0;
        if (foliageOrder > 0)
        {
            var siteCount = Math.Max(1, (int)MathF.Round(foliageOrder * (0.55f + 0.45f * maturity)));
            for (var site = 0; site < siteCount; site++)
            {
                var t = siteCount == 1 ? 0.72f : 0.40f + 0.50f * site / (siteCount - 1f);
                var position = PointOnBranch(t);
                var radialVariation = side * ((Unit(seed, 0x54524545464F4C49UL, branchKey * 3 + (ulong)site) - 0.5f) * 0.72f);
                var tangent = Vector3.Normalize(PointOnBranch(Math.Min(1f, t + 0.015f))
                    - PointOnBranch(Math.Max(0f, t - 0.015f)));
                var leafDirection = Vector3.Normalize(tangent + outward * 0.42f + Vector3.UnitY * 0.22f + radialVariation);
                var sizeVariation = 0.78f + Unit(seed, 0x545245454C454146UL, branchKey * 3 + (ulong)site) * 0.44f;
                // A foliage site represents a small branch tip cluster, not a single leaf.
                // Its radius is intentionally large enough to survive the game's elevated camera.
                var scale = height * profile.CrownWidth * (order == maximumOrder ? 0.205f : 0.23f) * sizeVariation;
                foliage.Add(new TreeFoliageSite(branchSegments[Math.Min(segmentCount - 1, (int)(t * segmentCount))],
                    position, leafDirection, outward, scale));
            }
        }

        if (order >= maximumOrder)
        {
            return branchSegments[^1];
        }

        var childBaseCount = order == 1 ? profile.SecondaryBranches : profile.TerminalBranches;
        var detailBranchScale = detailLevel switch { 0 => 0.55f, 1 => 0.72f, _ => 1.35f };
        var matureBranchScale = 0.58f + 0.42f * maturity;
        var fullDetailChildCount = Math.Max(1,
            (int)MathF.Round(childBaseCount * matureBranchScale * 1.35f));
        var childCount = Math.Max(1,
            (int)MathF.Round(childBaseCount * matureBranchScale * detailBranchScale));
        var branchPhase = Unit(seed, 0x5452454543485048UL, branchKey) * Tau;
        for (var child = 0; child < childCount; child++)
        {
            // Lower LODs select a spread-out subset of the full-detail bud slots.
            // A branch that survives the reduction keeps its height, azimuth and seed.
            var childSlot = childCount == 1
                ? fullDetailChildCount / 2
                : Math.Clamp((int)MathF.Round(
                    (child + 0.5f) * fullDetailChildCount / childCount - 0.5f),
                    0, fullDetailChildCount - 1);
            var jitter = (Unit(seed, 0x5452454541545441UL, branchKey * 7 + (ulong)childSlot) - 0.5f) * 0.12f;
            var positionT = Math.Clamp(0.18f + 0.66f * (childSlot + 0.5f) / fullDetailChildCount + jitter,
                0.16f, 0.90f);
            var segmentPosition = positionT * segmentCount;
            var attachSegment = Math.Min(segmentCount - 1, (int)segmentPosition);
            var attachFraction = Math.Clamp(segmentPosition - attachSegment, 0f, 1f);
            var attachParent = branchSegments[attachSegment];
            var supportingSegment = branches[attachParent];
            // Attach to the same sampled centerline that the renderer will mesh. Using
            // the unsampled Bezier point here can leave a child visibly detached from
            // the coarser chord when the parent has a strong bend or gravity sag.
            var attachPoint = Vector3.Lerp(supportingSegment.Start, supportingSegment.End, attachFraction);
            var tangent = Vector3.Normalize(PointOnBranch(Math.Min(1f, positionT + 0.015f))
                - PointOnBranch(Math.Max(0f, positionT - 0.015f)));
            var branchSideRaw = Vector3.Cross(tangent, Vector3.UnitY);
            var branchSide = branchSideRaw.LengthSquared() < 0.01f ? side : Vector3.Normalize(branchSideRaw);
            var branchNormal = Vector3.Normalize(Vector3.Cross(tangent, branchSide));
            var azimuthJitter = (Unit(seed, 0x54524545415A494DUL, branchKey * 13 + (ulong)childSlot) - 0.5f) * 0.64f;
            // Buds still occupy stratified positions along each axis, while successive
            // azimuths follow a phyllotactic sequence instead of forming tiny whorls.
            var azimuth = branchPhase + childSlot * GoldenAngle + azimuthJitter;
            var lateral = branchSide * MathF.Cos(azimuth) + branchNormal * MathF.Sin(azimuth);
            var outwardBias = profile.Architecture switch
            {
                TreeArchitecture.UprightBroadleaf => 0.18f,
                TreeArchitecture.WeepingBroadleaf => 0.28f,
                TreeArchitecture.ConicalConifer => 0.34f,
                _ => 0.24f
            };
            var childUp = profile.LateralBranchUp
                + (profile.Architecture == TreeArchitecture.ConicalConifer ? 0.20f : 0.04f);
            var divergenceDegrees = order == 1
                ? profile.SecondaryDivergenceDegrees
                : profile.TerminalDivergenceDegrees;
            var divergenceRadians = divergenceDegrees * (MathF.PI / 180f);
            var childDirection = Vector3.Normalize(tangent * MathF.Cos(divergenceRadians)
                + lateral * MathF.Sin(divergenceRadians) + outward * outwardBias
                + Vector3.UnitY * childUp);
            var orderLengthRatio = order switch
            {
                1 => profile.SecondaryLengthRatio,
                2 => profile.TertiaryLengthRatio,
                _ => profile.TerminalLengthRatio
            };
            var childLength = length * orderLengthRatio
                * (0.86f + Unit(seed, 0x545245454348494CUL, branchKey * 7 + (ulong)childSlot) * 0.30f);
            // A lateral branch takes its thickness from the parent where it actually joins.
            // Using the parent's tip radius for every insertion made proximal branches
            // implausibly thin and left the crown looking like a bundle of wires.
            var parentRadiusAtAttachment = supportingSegment.StartRadius
                + (supportingSegment.EndRadius - supportingSegment.StartRadius) * attachFraction;
            var daughterRadiusRatio = profile.Architecture == TreeArchitecture.ConicalConifer
                ? order == 1 ? 0.56f : 0.62f
                : order == 1 ? 0.60f : 0.66f;
            var daughterVariation = 0.90f
                + Unit(seed, 0x5452454543485241UL, branchKey * 17 + (ulong)childSlot) * 0.20f;
            var childRadius = parentRadiusAtAttachment * daughterRadiusRatio * daughterVariation;

            // Carry the local horizontal heading into the next order. Reusing the first
            // bough's radial vector at every depth makes every descendant fan away from
            // the trunk as if it were an independent primary branch.
            var childOutward = new Vector3(childDirection.X, 0f, childDirection.Z);
            childOutward = childOutward.LengthSquared() < 0.0001f
                ? outward
                : Vector3.Normalize(Vector3.Lerp(outward, Vector3.Normalize(childOutward), 0.72f));
            GrowBranch(attachPoint, childDirection, childOutward, childLength, childRadius, order + 1,
                attachParent, profile, height, maturity, seed, branchKey * 11 + (ulong)childSlot + 1,
                maximumOrder, detailLevel,
                branches, foliage);
        }

        // Pines keep needle sprays along the whorled branch skeleton as well as at branch tips.
        if (profile.Foliage == FoliageMorphology.Needle)
        {
            var position = PointOnBranch(0.70f);
            var tangent = Vector3.Normalize(PointOnBranch(0.72f) - PointOnBranch(0.68f));
            foliage.Add(new TreeFoliageSite(branchSegments[Math.Min(segmentCount - 1, (int)(0.70f * segmentCount))], position,
                Vector3.Normalize(tangent + outward * 0.55f), outward,
                height * profile.CrownWidth * 0.16f));
        }

        // Crown colonization must start at the actual end of this bough. Returning the
        // terminal segment avoids growing fill branches from the first quarter near the
        // trunk, which creates false junctions and leaves the outer crown unsupported.
        return branchSegments[^1];
    }

    private static float Unit(ulong seed, ulong salt, ulong index)
        => (float)CounterRandom.Unit(seed, salt, index, 0, 0, 0);

    private readonly record struct CrownTip(Vector3 Position, int BranchIndex, float Radius, Vector3 Outward, int Order);
}
