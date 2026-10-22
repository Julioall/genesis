using Game.Contracts;

namespace Game.Modules.Biosphere;

/// <summary>
/// Renderer-neutral kinematics for the Z1 prototype plans. Outputs pose only; movement, contact
/// confirmation, thrust, and energy remain owned by their simulation systems.
/// </summary>
public sealed class CreaturePoseSolverV1 : ICreaturePoseSolverV1
{
    public const int FishPoseVersion = 1;

    private const double ReachEpsilonM = 1e-12;
    private const double ContactToleranceM = 1e-9;
    private const double TwoPi = 2 * Math.PI;

    public QuadrupedLimbPoseV1 SolveQuadrupedLimb(
        DevelopedBodyV1 anatomy,
        QuadrupedLimbV1 limb,
        CreatureVector3V1 footContactTargetM,
        double bendSign)
    {
        ArgumentNullException.ThrowIfNull(anatomy);
        if (anatomy.Family != CreatureFamilyV1.Quadruped)
            throw new ArgumentException("Two-segment limb IK requires a quadruped body plan.", nameof(anatomy));
        if (bendSign is not (-1 or 1))
            throw new ArgumentOutOfRangeException(nameof(bendSign), "Bend sign must be -1 or +1.");
        RequireFinite(footContactTargetM, nameof(footContactTargetM));

        var (upperPartId, lowerPartId) = limb switch
        {
            QuadrupedLimbV1.FrontLeft => ("front.left.upper", "front.left.lower"),
            QuadrupedLimbV1.FrontRight => ("front.right.upper", "front.right.lower"),
            QuadrupedLimbV1.HindLeft => ("hind.left.upper", "hind.left.lower"),
            QuadrupedLimbV1.HindRight => ("hind.right.upper", "hind.right.lower"),
            _ => throw new ArgumentOutOfRangeException(nameof(limb))
        };

        var upper = FindPart(anatomy, upperPartId);
        var lower = FindPart(anatomy, lowerPartId);
        if (!FinitePositive(upper.LengthM) || !FinitePositive(lower.LengthM))
            throw new ArgumentException("Limb segment lengths must be finite and positive.", nameof(anatomy));

        var anchor = new CreatureVector3V1(upper.LocalOffsetXM, upper.LocalOffsetYM, upper.LocalOffsetZM);
        RequireFinite(anchor, nameof(anatomy));
        var targetDelta = Subtract(footContactTargetM, anchor);
        var targetDistance = Length(targetDelta);
        if (!double.IsFinite(targetDistance))
            throw new ArgumentOutOfRangeException(nameof(footContactTargetM), "Contact distance must be finite.");
        var minimumReach = Math.Abs(upper.LengthM - lower.LengthM);
        var maximumReach = upper.LengthM + lower.LengthM;

        if (targetDistance <= ReachEpsilonM && minimumReach <= ReachEpsilonM)
        {
            var perpendicular = SelectBendDirection(new(1, 0, 0), new(0, -1, 0));
            var knee = Add(anchor, Scale(perpendicular, bendSign * upper.LengthM));
            return new(limb, anchor, knee, anchor, upper.LengthM, lower.LengthM,
                minimumReach, maximumReach, true);
        }

        var direction = targetDistance > ReachEpsilonM
            ? Scale(targetDelta, 1 / targetDistance)
            : new CreatureVector3V1(0, -1, 0);
        var solvedDistance = Math.Clamp(targetDistance, minimumReach, maximumReach);
        var solvedTarget = Add(anchor, Scale(direction, solvedDistance));
        var alongDirection = (upper.LengthM * upper.LengthM - lower.LengthM * lower.LengthM
                              + solvedDistance * solvedDistance) / (2 * solvedDistance);
        alongDirection = Math.Clamp(alongDirection, 0, upper.LengthM);
        var perpendicularDistance = Math.Sqrt(Math.Max(0,
            upper.LengthM * upper.LengthM - alongDirection * alongDirection));
        var bendDirection = SelectBendDirection(new(1, 0, 0), direction);
        var kneePosition = Add(anchor,
            Add(Scale(direction, alongDirection), Scale(bendDirection, bendSign * perpendicularDistance)));
        var contactReached = Math.Abs(targetDistance - solvedDistance) <= ContactToleranceM;

        return new(limb, anchor, kneePosition, solvedTarget, upper.LengthM, lower.LengthM,
            minimumReach, maximumReach, contactReached);
    }

    public CreatureFishSwimmingPoseV1 BuildFishSwimmingPose(
        DevelopedBodyV1 anatomy,
        double phase01,
        double amplitude01)
    {
        ArgumentNullException.ThrowIfNull(anatomy);
        if (anatomy.Family != CreatureFamilyV1.Fish)
            throw new ArgumentException("Axial swimming pose requires a fish body plan.", nameof(anatomy));
        if (!InUnitInterval(phase01))
            throw new ArgumentOutOfRangeException(nameof(phase01));
        if (!InUnitInterval(amplitude01))
            throw new ArgumentOutOfRangeException(nameof(amplitude01));

        var joints = anatomy.Joints
            .Where(joint => joint.Kind == AnatomicalJointKindV1.FlexibleAxial
                            && joint.ChildPartIndex >= 0
                            && joint.ChildPartIndex < anatomy.Parts.Count
                            && IsSwimmingAxisPart(anatomy.Parts[joint.ChildPartIndex]))
            .OrderBy(joint => joint.ChildPartIndex)
            .ToArray();
        if (joints.Length == 0)
            throw new ArgumentException("Fish body plan has no flexible axial joints.", nameof(anatomy));

        var pose = new CreatureAxialJointPoseV1[joints.Length];
        for (var index = 0; index < joints.Length; index++)
        {
            var joint = joints[index];
            var child = anatomy.Parts[joint.ChildPartIndex];
            if (!double.IsFinite(joint.MinimumAngleDegrees) || !double.IsFinite(joint.MaximumAngleDegrees)
                || joint.MinimumAngleDegrees >= joint.MaximumAngleDegrees)
                throw new ArgumentException("Fish axial joints must have finite, non-empty angle limits.", nameof(anatomy));
            var center = (joint.MinimumAngleDegrees + joint.MaximumAngleDegrees) * 0.5;
            var halfRange = (joint.MaximumAngleDegrees - joint.MinimumAngleDegrees) * 0.5;
            var progress = joints.Length == 1 ? 0 : (double)index / (joints.Length - 1);
            var wavePhase = TwoPi * phase01 - progress * Math.PI * 0.5;
            var angle = center + Math.Sin(wavePhase) * halfRange * amplitude01;
            pose[index] = new(child.PartId, joint.ParentPartIndex, joint.ChildPartIndex, angle);
        }

        return new(FishPoseVersion, anatomy.StructuralSignature, phase01, pose);
    }

    private static bool IsSwimmingAxisPart(AnatomicalPartV1 part)
        => part.Kind == AnatomicalPartKindV1.AxialSegment
           || part.Kind == AnatomicalPartKindV1.Fin && part.PartId == "fin.caudal";

    private static AnatomicalPartV1 FindPart(DevelopedBodyV1 anatomy, string partId)
    {
        var part = anatomy.Parts.FirstOrDefault(candidate => candidate.PartId == partId);
        return !string.IsNullOrEmpty(part.PartId)
            ? part
            : throw new ArgumentException($"Body plan is missing required limb part '{partId}'.", nameof(anatomy));
    }

    private static CreatureVector3V1 SelectBendDirection(
        CreatureVector3V1 planeNormal,
        CreatureVector3V1 limbDirection)
    {
        var perpendicular = Normalize(Cross(planeNormal, limbDirection));
        if (Length(perpendicular) > ReachEpsilonM) return perpendicular;
        perpendicular = Normalize(Cross(new(0, 1, 0), limbDirection));
        if (Length(perpendicular) > ReachEpsilonM) return perpendicular;
        return Normalize(Cross(new(0, 0, 1), limbDirection));
    }

    private static CreatureVector3V1 Cross(CreatureVector3V1 first, CreatureVector3V1 second)
        => new(first.Y * second.Z - first.Z * second.Y,
            first.Z * second.X - first.X * second.Z,
            first.X * second.Y - first.Y * second.X);

    private static CreatureVector3V1 Add(CreatureVector3V1 first, CreatureVector3V1 second)
        => new(first.X + second.X, first.Y + second.Y, first.Z + second.Z);

    private static CreatureVector3V1 Subtract(CreatureVector3V1 first, CreatureVector3V1 second)
        => new(first.X - second.X, first.Y - second.Y, first.Z - second.Z);

    private static CreatureVector3V1 Scale(CreatureVector3V1 value, double scalar)
        => new(value.X * scalar, value.Y * scalar, value.Z * scalar);

    private static CreatureVector3V1 Normalize(CreatureVector3V1 value)
    {
        var length = Length(value);
        return length > ReachEpsilonM ? Scale(value, 1 / length) : new(0, 0, 0);
    }

    private static double Length(CreatureVector3V1 value)
        => Math.Sqrt(value.X * value.X + value.Y * value.Y + value.Z * value.Z);

    private static bool InUnitInterval(double value)
        => double.IsFinite(value) && value is >= 0 and <= 1;

    private static bool FinitePositive(double value)
        => double.IsFinite(value) && value > 0;

    private static void RequireFinite(CreatureVector3V1 value, string parameterName)
    {
        if (!double.IsFinite(value.X) || !double.IsFinite(value.Y) || !double.IsFinite(value.Z))
            throw new ArgumentOutOfRangeException(parameterName, "Vector components must be finite.");
    }
}
