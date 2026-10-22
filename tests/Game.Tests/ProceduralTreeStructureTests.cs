using Game.Modules.Biosphere;

namespace Game.Tests;

public sealed class ProceduralTreeStructureTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Generated_tree_has_connected_hierarchy_and_supported_foliage(int profileRow)
    {
        var tree = ProceduralTreeGenerator.Generate(profileRow, 8f, 1f, 0x5EEDUL, detailLevel: 2);

        Assert.NotEmpty(tree.Branches);
        Assert.NotEmpty(tree.Foliage);
        for (var branchIndex = 0; branchIndex < tree.Branches.Count; branchIndex++)
        {
            var branch = tree.Branches[branchIndex];
            Assert.InRange(branch.ParentIndex, -1, tree.Branches.Count - 1);
            Assert.True(branch.ParentIndex < branchIndex);
            Assert.True(branch.StartRadius > 0);
            Assert.True(branch.EndRadius > 0);
            Assert.NotEqual(branch.Start, branch.End);

            if (branch.ParentIndex < 0) continue;
            var parent = tree.Branches[branch.ParentIndex];
            if (parent.Order == branch.Order)
                Assert.True(System.Numerics.Vector3.DistanceSquared(parent.End, branch.Start) < 0.000001f);
        }
        Assert.All(tree.Foliage, site =>
        {
            Assert.InRange(site.BranchIndex, 0, tree.Branches.Count - 1);
            Assert.True(site.Scale > 0);
            Assert.True(site.Direction.LengthSquared() > 0.9f);
        });
        Assert.Contains(tree.Branches, branch => branch.Order >= 3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Different_order_branches_attach_to_their_parent_wood(int profileRow)
    {
        var tree = ProceduralTreeGenerator.Generate(profileRow, 8f, 1f, 0xA77AC4UL, detailLevel: 2);
        var differentOrderBranches = tree.Branches
            .Where(branch => branch.ParentIndex >= 0
                && tree.Branches[branch.ParentIndex].Order != branch.Order)
            .ToArray();

        Assert.NotEmpty(differentOrderBranches);
        foreach (var branch in differentOrderBranches)
        {
            var parent = tree.Branches[branch.ParentIndex];
            Assert.True(parent.Order < branch.Order);
            Assert.InRange(DistanceToSegment(branch.Start, parent.Start, parent.End), 0f, 0.05f);
            Assert.True(branch.StartRadius <= RadiusAlongParent(parent, branch.Start) * 1.05f);
        }
    }

    [Fact]
    public void Same_seed_reproduces_identical_tree_structure()
    {
        var first = ProceduralTreeGenerator.Generate(0, 7.5f, 0.83f, 0xA57E22UL, detailLevel: 2);
        var second = ProceduralTreeGenerator.Generate(0, 7.5f, 0.83f, 0xA57E22UL, detailLevel: 2);

        Assert.Equal(first.Branches.ToArray(), second.Branches.ToArray());
        Assert.Equal(first.Foliage.ToArray(), second.Foliage.ToArray());
    }

    [Fact]
    public void Seed_changes_individual_branch_placement_without_changing_species_profile()
    {
        var first = ProceduralTreeGenerator.Generate(0, 8f, 1f, 101UL, detailLevel: 2);
        var second = ProceduralTreeGenerator.Generate(0, 8f, 1f, 202UL, detailLevel: 2);

        Assert.Equal(first.Profile, second.Profile);
        Assert.Equal(first.Branches.Count, second.Branches.Count);
        Assert.NotEqual(first.Branches[10].Start, second.Branches[10].Start);
    }

    [Fact]
    public void Conifer_uses_tiered_primary_branch_architecture()
    {
        var conifer = ProceduralTreeGenerator.Generate(1, 8f, 1f, 0xC01FE2UL, detailLevel: 2);
        var broadleaf = ProceduralTreeGenerator.Generate(0, 8f, 1f, 0xC01FE2UL, detailLevel: 2);
        var coniferPrimarySegments = conifer.Branches.Count(branch => branch.Order == 1);
        var broadleafPrimarySegments = broadleaf.Branches.Count(branch => branch.Order == 1);

        Assert.True(coniferPrimarySegments > broadleafPrimarySegments);
    }

    [Fact]
    public void Primary_branch_scaffold_keeps_its_positions_across_geometry_lods()
    {
        var coarse = ProceduralTreeGenerator.Generate(1, 8f, 1f, 0x10DUL, detailLevel: 0);
        var detailed = ProceduralTreeGenerator.Generate(1, 8f, 1f, 0x10DUL, detailLevel: 2);
        var coarsePrimaryRoots = PrimaryRoots(coarse);
        var detailedPrimaryRoots = PrimaryRoots(detailed);

        Assert.Equal(detailedPrimaryRoots.Length, coarsePrimaryRoots.Length);
        Assert.Equal(detailedPrimaryRoots.Select(branch => branch.Start),
            coarsePrimaryRoots.Select(branch => branch.Start));
    }

    [Fact]
    public void Primary_bough_lengths_follow_the_species_crown_envelope()
    {
        var sphericalLow = new List<float>();
        var sphericalMiddle = new List<float>();
        var sphericalHigh = new List<float>();
        var conicalLow = new List<float>();
        var conicalHigh = new List<float>();

        for (ulong seed = 1; seed <= 40; seed++)
        {
            CollectPrimaryBoughs(ProceduralTreeGenerator.Generate(0, 8f, 1f, seed, detailLevel: 2),
                sphericalLow, sphericalMiddle, sphericalHigh);
            CollectPrimaryBoughs(ProceduralTreeGenerator.Generate(1, 8f, 1f, seed, detailLevel: 2),
                conicalLow, null, conicalHigh);
        }

        Assert.True(sphericalLow.Count > 20 && sphericalMiddle.Count > 20 && sphericalHigh.Count > 20);
        Assert.True(conicalLow.Count > 20 && conicalHigh.Count > 20);
        Assert.True(Average(sphericalMiddle) > Average(sphericalLow) * 1.35f);
        Assert.True(Average(sphericalMiddle) > Average(sphericalHigh) * 1.35f);
        Assert.True(Average(conicalLow) > Average(conicalHigh) * 1.45f);
    }

    [Fact]
    public void Crown_colonization_extends_from_primary_bough_tips()
    {
        var tree = ProceduralTreeGenerator.Generate(0, 8f, 1f, 0xC01FE2UL, detailLevel: 2);
        var sameOrderSuccessors = new int[tree.Branches.Count];
        Array.Fill(sameOrderSuccessors, -1);
        for (var index = 0; index < tree.Branches.Count; index++)
        {
            var parentIndex = tree.Branches[index].ParentIndex;
            if (parentIndex >= 0 && tree.Branches[parentIndex].Order == tree.Branches[index].Order)
                sameOrderSuccessors[parentIndex] = index;
        }

        var crownExtensions = Enumerable.Range(0, tree.Branches.Count)
            .Where(index =>
            {
                var branch = tree.Branches[index];
                if (branch.Order != 2 || branch.ParentIndex < 0) return false;
                var parent = tree.Branches[branch.ParentIndex];
                return parent.Order == 1
                    && sameOrderSuccessors[branch.ParentIndex] < 0
                    && System.Numerics.Vector3.DistanceSquared(parent.End, branch.Start) < 0.000001f;
            })
            .ToArray();

        Assert.NotEmpty(crownExtensions);
    }

    private static TreeBranchSegment[] PrimaryRoots(ProceduralTreeStructure tree)
        => tree.Branches
            .Where(branch => branch.Order == 1 && branch.ParentIndex >= 0
                && tree.Branches[branch.ParentIndex].Order == 0)
            .ToArray();

    private static void CollectPrimaryBoughs(
        ProceduralTreeStructure tree,
        List<float> low,
        List<float>? middle,
        List<float> high)
    {
        var sameOrderSuccessors = new int[tree.Branches.Count];
        Array.Fill(sameOrderSuccessors, -1);
        for (var index = 0; index < tree.Branches.Count; index++)
        {
            var parentIndex = tree.Branches[index].ParentIndex;
            if (parentIndex >= 0 && tree.Branches[parentIndex].Order == tree.Branches[index].Order)
                sameOrderSuccessors[parentIndex] = index;
        }

        foreach (var rootIndex in Enumerable.Range(0, tree.Branches.Count)
                     .Where(index => tree.Branches[index].Order == 1
                         && tree.Branches[index].ParentIndex >= 0
                         && tree.Branches[tree.Branches[index].ParentIndex].Order == 0))
        {
            var root = tree.Branches[rootIndex];
            var progress = Math.Clamp(
                (root.Start.Y / tree.Height - tree.Profile.CrownBase)
                    / Math.Max(0.001f, tree.Profile.CrownTop - tree.Profile.CrownBase),
                0f,
                1f);
            var length = 0f;
            for (var current = rootIndex; current >= 0; current = sameOrderSuccessors[current])
            {
                var branch = tree.Branches[current];
                length += System.Numerics.Vector3.Distance(branch.Start, branch.End);
            }

            if (progress < 0.35f) low.Add(length);
            else if (progress > 0.65f) high.Add(length);
            else if (middle is not null) middle.Add(length);
        }
    }

    private static float Average(IReadOnlyCollection<float> values) => values.Average();

    private static float DistanceToSegment(
        System.Numerics.Vector3 point,
        System.Numerics.Vector3 start,
        System.Numerics.Vector3 end)
    {
        var segment = end - start;
        var lengthSquared = segment.LengthSquared();
        if (lengthSquared < 0.000001f) return System.Numerics.Vector3.Distance(point, start);
        var fraction = Math.Clamp(System.Numerics.Vector3.Dot(point - start, segment) / lengthSquared, 0f, 1f);
        return System.Numerics.Vector3.Distance(point, start + segment * fraction);
    }

    private static float RadiusAlongParent(TreeBranchSegment parent, System.Numerics.Vector3 point)
    {
        var segment = parent.End - parent.Start;
        var lengthSquared = segment.LengthSquared();
        if (lengthSquared < 0.000001f) return parent.EndRadius;
        var fraction = Math.Clamp(System.Numerics.Vector3.Dot(point - parent.Start, segment) / lengthSquared, 0f, 1f);
        return parent.StartRadius + (parent.EndRadius - parent.StartRadius) * fraction;
    }
}
