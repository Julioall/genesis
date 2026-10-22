using Game.Contracts;
using Game.Modules.Biosphere;

namespace Game.Tests;

public sealed class CreaturePoseSolverTests
{
    private readonly CreatureDevelopmentModelV1 _development = new();
    private readonly CreaturePoseSolverV1 _solver = new();

    [Fact]
    public void Quadruped_ik_preserves_both_segment_lengths_for_all_limbs_and_reachable_contacts()
    {
        var species = CreatureSpeciesCatalogV1.Quadruped;
        var body = _development.Develop(species,
            _development.CreateGenome(species, 71, 91),
            new(species.MaturityDays * 2, 1, 1)).Body;

        foreach (var (limb, upperId) in LimbParts())
        {
            var upper = body.Parts.Single(part => part.PartId == upperId);
            var anchor = new CreatureVector3V1(upper.LocalOffsetXM, upper.LocalOffsetYM, upper.LocalOffsetZM);
            var targetDistance = body.Parts.Single(part => part.PartId == LowerPartId(upperId)).LengthM
                                 + upper.LengthM * 0.45;
            var target = new CreatureVector3V1(anchor.X, anchor.Y - targetDistance, anchor.Z);

            var pose = _solver.SolveQuadrupedLimb(body, limb, target, 1);

            Assert.True(pose.ContactTargetReached);
            AssertVectorNear(target, pose.FootPositionM, 1e-9);
            Assert.InRange(Distance(pose.HipAnchorM, pose.KneePositionM),
                pose.UpperSegmentLengthM - 1e-10, pose.UpperSegmentLengthM + 1e-10);
            Assert.InRange(Distance(pose.KneePositionM, pose.FootPositionM),
                pose.LowerSegmentLengthM - 1e-10, pose.LowerSegmentLengthM + 1e-10);
        }
    }

    [Fact]
    public void Quadruped_ik_reports_contacts_outside_minimum_and_maximum_reach()
    {
        var body = DevelopQuadruped();
        var upper = body.Parts.Single(part => part.PartId == "front.left.upper");
        var lower = body.Parts.Single(part => part.PartId == "front.left.lower");
        var anchor = new CreatureVector3V1(upper.LocalOffsetXM, upper.LocalOffsetYM, upper.LocalOffsetZM);

        var tooFar = _solver.SolveQuadrupedLimb(body, QuadrupedLimbV1.FrontLeft,
            new(anchor.X, anchor.Y - 10, anchor.Z), -1);
        var tooClose = _solver.SolveQuadrupedLimb(body, QuadrupedLimbV1.FrontLeft,
            anchor, 1);

        Assert.False(tooFar.ContactTargetReached);
        Assert.False(tooClose.ContactTargetReached);
        Assert.InRange(Distance(tooFar.HipAnchorM, tooFar.FootPositionM),
            tooFar.MaximumReachM - 1e-10, tooFar.MaximumReachM + 1e-10);
        Assert.InRange(Distance(tooClose.HipAnchorM, tooClose.FootPositionM),
            tooClose.MinimumReachM - 1e-10, tooClose.MinimumReachM + 1e-10);
        AssertLimbLengths(tooFar);
        AssertLimbLengths(tooClose);

        Assert.Equal(upper.LengthM, tooFar.UpperSegmentLengthM);
        Assert.Equal(lower.LengthM, tooFar.LowerSegmentLengthM);
    }

    [Fact]
    public void Quadruped_ik_handles_bend_plane_degeneracy_with_finite_length_preserving_output()
    {
        var body = DevelopQuadruped();
        var upper = body.Parts.Single(part => part.PartId == "hind.right.upper");
        var anchor = new CreatureVector3V1(upper.LocalOffsetXM, upper.LocalOffsetYM, upper.LocalOffsetZM);
        var targetAlongPlaneNormal = new CreatureVector3V1(anchor.X + 10, anchor.Y, anchor.Z);

        var pose = _solver.SolveQuadrupedLimb(body, QuadrupedLimbV1.HindRight,
            targetAlongPlaneNormal, -1);

        Assert.False(pose.ContactTargetReached);
        AssertVectorFinite(pose.KneePositionM);
        AssertVectorFinite(pose.FootPositionM);
        AssertLimbLengths(pose);
    }

    [Fact]
    public void Fish_pose_is_deterministic_bounded_and_renderer_neutral()
    {
        var species = CreatureSpeciesCatalogV1.FreshwaterFish;
        var body = _development.Develop(species,
            _development.CreateGenome(species, 101, 202),
            new(species.MaturityDays * 2, 1, 1)).Body;

        var neutral = _solver.BuildFishSwimmingPose(body, 0.25, 0);
        var first = _solver.BuildFishSwimmingPose(body, 0.25, 1);
        var repeated = _solver.BuildFishSwimmingPose(body, 0.25, 1);
        var otherPhase = _solver.BuildFishSwimmingPose(body, 0.75, 1);

        Assert.Equal(first.AxialJoints, repeated.AxialJoints);
        Assert.Equal(body.StructuralSignature, first.StructuralSignature);
        Assert.Equal(CreaturePoseSolverV1.FishPoseVersion, first.PoseVersion);
        Assert.NotEqual(first.AxialJoints, otherPhase.AxialJoints);
        Assert.All(neutral.AxialJoints, joint => Assert.Equal(0, joint.AngleDegrees, 10));
        Assert.NotEmpty(first.AxialJoints);
        Assert.Throws<NotSupportedException>(() => ((IList<CreatureAxialJointPoseV1>)first.AxialJoints)
            .Add(default));

        foreach (var poseJoint in first.AxialJoints)
        {
            var bodyJoint = body.Joints.Single(joint => joint.ChildPartIndex == poseJoint.ChildPartIndex);
            Assert.InRange(poseJoint.AngleDegrees, bodyJoint.MinimumAngleDegrees, bodyJoint.MaximumAngleDegrees);
            Assert.True(double.IsFinite(poseJoint.AngleDegrees));
        }
    }

    [Fact]
    public void Pose_api_rejects_wrong_family_and_invalid_inputs_without_accepting_world_state()
    {
        var quadruped = DevelopQuadruped();
        var fish = DevelopFish();
        var upper = quadruped.Parts.Single(part => part.PartId == "front.left.upper");
        var anchor = new CreatureVector3V1(upper.LocalOffsetXM, upper.LocalOffsetYM, upper.LocalOffsetZM);

        Assert.Throws<ArgumentException>(() => _solver.SolveQuadrupedLimb(fish,
            QuadrupedLimbV1.FrontLeft, anchor, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _solver.SolveQuadrupedLimb(quadruped,
            QuadrupedLimbV1.FrontLeft, anchor, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => _solver.BuildFishSwimmingPose(fish, double.NaN, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _solver.BuildFishSwimmingPose(fish, 0.5, 1.01));

        foreach (var method in typeof(ICreaturePoseSolverV1).GetMethods())
        foreach (var parameter in method.GetParameters())
        {
            Assert.NotEqual(typeof(AnimalAgentV1), parameter.ParameterType);
            Assert.NotEqual(typeof(WorldCell), parameter.ParameterType);
            Assert.DoesNotContain("SimulationState", parameter.ParameterType.Name, StringComparison.Ordinal);
        }
    }

    private DevelopedBodyV1 DevelopQuadruped()
    {
        var species = CreatureSpeciesCatalogV1.Quadruped;
        return _development.Develop(species, _development.CreateGenome(species, 71, 91),
            new(species.MaturityDays * 2, 1, 1)).Body;
    }

    private DevelopedBodyV1 DevelopFish()
    {
        var species = CreatureSpeciesCatalogV1.FreshwaterFish;
        return _development.Develop(species, _development.CreateGenome(species, 101, 202),
            new(species.MaturityDays * 2, 1, 1)).Body;
    }

    private static IEnumerable<(QuadrupedLimbV1 Limb, string UpperPartId)> LimbParts()
    {
        yield return (QuadrupedLimbV1.FrontLeft, "front.left.upper");
        yield return (QuadrupedLimbV1.FrontRight, "front.right.upper");
        yield return (QuadrupedLimbV1.HindLeft, "hind.left.upper");
        yield return (QuadrupedLimbV1.HindRight, "hind.right.upper");
    }

    private static string LowerPartId(string upperPartId) => upperPartId.Replace("upper", "lower");

    private static void AssertLimbLengths(QuadrupedLimbPoseV1 pose)
    {
        Assert.InRange(Distance(pose.HipAnchorM, pose.KneePositionM),
            pose.UpperSegmentLengthM - 1e-9, pose.UpperSegmentLengthM + 1e-9);
        Assert.InRange(Distance(pose.KneePositionM, pose.FootPositionM),
            pose.LowerSegmentLengthM - 1e-9, pose.LowerSegmentLengthM + 1e-9);
    }

    private static void AssertVectorNear(CreatureVector3V1 expected, CreatureVector3V1 actual, double tolerance)
        => Assert.InRange(Distance(expected, actual), 0, tolerance);

    private static void AssertVectorFinite(CreatureVector3V1 value)
    {
        Assert.True(double.IsFinite(value.X));
        Assert.True(double.IsFinite(value.Y));
        Assert.True(double.IsFinite(value.Z));
    }

    private static double Distance(CreatureVector3V1 first, CreatureVector3V1 second)
    {
        var x = first.X - second.X;
        var y = first.Y - second.Y;
        var z = first.Z - second.Z;
        return Math.Sqrt(x * x + y * y + z * z);
    }
}
