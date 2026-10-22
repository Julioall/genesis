using Game.Contracts;

namespace Game.Procedural;

/// <summary>
/// Applies renderer-neutral quadruped limb solutions to the matching independently generated
/// surfaces. It produces a visual stance only and does not move or update a simulation agent.
/// </summary>
public static class CreaturePoseGeometryAdapterV1
{
    private const double Epsilon = 1e-12;
    private static readonly CreatureVector3V1 RestAxis = new(0, -1, 0);

    public static CreatureGeometryV1 ApplyStaticQuadrupedStance(
        DevelopedBodyV1 anatomy,
        CreatureGeometryV1 geometry,
        ICreaturePoseSolverV1 poseSolver)
    {
        ArgumentNullException.ThrowIfNull(anatomy);
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(poseSolver);
        if (anatomy.Family != CreatureFamilyV1.Quadruped)
            throw new ArgumentException("A static support stance requires a quadruped body plan.", nameof(anatomy));

        var supportPlaneY = geometry.Vertices.Min(vertex => vertex.PositionM.Y);
        var poses = Enum.GetValues<QuadrupedLimbV1>().Select(limb =>
        {
            var upperPartId = limb switch
            {
                QuadrupedLimbV1.FrontLeft => "front.left.upper",
                QuadrupedLimbV1.FrontRight => "front.right.upper",
                QuadrupedLimbV1.HindLeft => "hind.left.upper",
                QuadrupedLimbV1.HindRight => "hind.right.upper",
                _ => throw new ArgumentOutOfRangeException(nameof(limb))
            };
            var upperPart = anatomy.Parts.FirstOrDefault(part => part.PartId == upperPartId);
            if (string.IsNullOrEmpty(upperPart.PartId))
                throw new ArgumentException($"Quadruped anatomy is missing limb part '{upperPartId}'.", nameof(anatomy));
            var hip = new CreatureVector3V1(upperPart.LocalOffsetXM,
                upperPart.LocalOffsetYM, upperPart.LocalOffsetZM);
            var footContact = new CreatureVector3V1(hip.X, supportPlaneY, hip.Z);
            var bendSign = limb is QuadrupedLimbV1.FrontLeft or QuadrupedLimbV1.FrontRight ? -1 : 1;
            return poseSolver.SolveQuadrupedLimb(anatomy, limb, footContact, bendSign);
        }).ToArray();

        return ApplyQuadrupedLimbPoses(anatomy, geometry, poses);
    }

    public static CreatureGeometryV1 ApplyQuadrupedLimbPoses(
        DevelopedBodyV1 anatomy,
        CreatureGeometryV1 geometry,
        IEnumerable<QuadrupedLimbPoseV1> limbPoses)
    {
        ArgumentNullException.ThrowIfNull(anatomy);
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(limbPoses);
        if (anatomy.Family != CreatureFamilyV1.Quadruped)
            throw new ArgumentException("Quadruped limb poses require a quadruped body plan.", nameof(anatomy));
        if (anatomy.StructuralSignature != geometry.StructuralSignature
            || anatomy.Parts.Count != geometry.PartCount)
            throw new ArgumentException("Geometry does not match the developed quadruped anatomy.", nameof(geometry));
        if (geometry.Surfaces.Count != geometry.PartCount + geometry.FeatureCount)
            throw new ArgumentException("Geometry must identify every part and feature surface.", nameof(geometry));

        var poses = limbPoses.ToArray();
        if (poses.Length != 4 || poses.Select(pose => pose.Limb).Distinct().Count() != 4)
            throw new ArgumentException("Exactly one pose for each quadruped limb is required.", nameof(limbPoses));

        var surfaces = geometry.Surfaces.ToDictionary(surface => surface.SurfaceId, StringComparer.Ordinal);
        var partIndices = anatomy.Parts.Select((part, index) => (part.PartId, index))
            .ToDictionary(pair => pair.PartId, pair => pair.index, StringComparer.Ordinal);
        var centers = GetPartCenters(anatomy);
        var output = geometry.Vertices.ToArray();

        foreach (var pose in poses)
        {
            var (upperId, lowerId) = pose.Limb switch
            {
                QuadrupedLimbV1.FrontLeft => ("front.left.upper", "front.left.lower"),
                QuadrupedLimbV1.FrontRight => ("front.right.upper", "front.right.lower"),
                QuadrupedLimbV1.HindLeft => ("hind.left.upper", "hind.left.lower"),
                QuadrupedLimbV1.HindRight => ("hind.right.upper", "hind.right.lower"),
                _ => throw new ArgumentOutOfRangeException(nameof(limbPoses), "Unknown quadruped limb.")
            };

            var upperIndex = RequirePart(partIndices, upperId);
            var lowerIndex = RequirePart(partIndices, lowerId);
            var upperPart = anatomy.Parts[upperIndex];
            var lowerPart = anatomy.Parts[lowerIndex];
            RequireLength(pose.UpperSegmentLengthM, upperPart.LengthM, upperId);
            RequireLength(pose.LowerSegmentLengthM, lowerPart.LengthM, lowerId);

            PoseSurface(surfaces[upperId], centers[upperIndex], pose.HipAnchorM,
                pose.KneePositionM, output);
            PoseSurface(surfaces[lowerId], centers[lowerIndex], pose.KneePositionM,
                pose.FootPositionM, output);
        }

        return new(geometry.GeometryVersion, geometry.StructuralSignature, geometry.PartCount,
            geometry.FeatureCount, output, geometry.Indices, geometry.Surfaces);
    }

    private static CreatureVector3V1[] GetPartCenters(DevelopedBodyV1 anatomy)
    {
        var centers = new CreatureVector3V1[anatomy.Parts.Count];
        for (var index = 0; index < anatomy.Parts.Count; index++)
        {
            var part = anatomy.Parts[index];
            var offset = new CreatureVector3V1(part.LocalOffsetXM, part.LocalOffsetYM, part.LocalOffsetZM);
            centers[index] = part.ParentPartIndex < 0
                ? offset
                : Add(centers[part.ParentPartIndex], offset);
        }
        return centers;
    }

    private static void PoseSurface(
        CreatureGeometrySurfaceV1 surface,
        CreatureVector3V1 originalCenter,
        CreatureVector3V1 start,
        CreatureVector3V1 end,
        CreatureGeometryVertexV1[] vertices)
    {
        if (surface.FirstVertex < 0 || surface.VertexCount <= 0
            || surface.FirstVertex > vertices.Length - surface.VertexCount)
            throw new ArgumentException($"Surface '{surface.SurfaceId}' has an invalid vertex range.");

        var direction = Subtract(end, start);
        var length = Length(direction);
        if (!double.IsFinite(length) || length <= Epsilon)
            throw new ArgumentException($"Pose segment '{surface.SurfaceId}' has no finite length.");
        var targetCenter = Scale(Add(start, end), 0.5);
        var unitDirection = Scale(direction, 1 / length);

        for (var index = surface.FirstVertex; index < surface.FirstVertex + surface.VertexCount; index++)
        {
            var source = vertices[index];
            var relativePosition = Subtract(source.PositionM, originalCenter);
            var relativeNormal = source.Normal;
            vertices[index] = source with
            {
                PositionM = Add(targetCenter, RotateFromRest(relativePosition, unitDirection)),
                Normal = Normalize(RotateFromRest(relativeNormal, unitDirection))
            };
        }
    }

    private static CreatureVector3V1 RotateFromRest(CreatureVector3V1 value, CreatureVector3V1 targetAxis)
    {
        var cross = Cross(RestAxis, targetAxis);
        var cosine = Math.Clamp(Dot(RestAxis, targetAxis), -1, 1);
        if (cosine > 1 - Epsilon) return value;
        if (cosine < -1 + Epsilon)
        {
            var rotationAxis = Normalize(Cross(RestAxis, new CreatureVector3V1(1, 0, 0)));
            return Subtract(Scale(rotationAxis, 2 * Dot(rotationAxis, value)), value);
        }

        return Add(value, Add(Cross(cross, value),
            Scale(Cross(cross, Cross(cross, value)), 1 / (1 + cosine))));
    }

    private static int RequirePart(IReadOnlyDictionary<string, int> parts, string partId)
        => parts.TryGetValue(partId, out var index)
            ? index
            : throw new ArgumentException($"Quadruped anatomy is missing limb part '{partId}'.");

    private static void RequireLength(double poseLength, double anatomyLength, string partId)
    {
        if (!double.IsFinite(poseLength) || poseLength <= 0
            || Math.Abs(poseLength - anatomyLength) > Math.Max(Epsilon, anatomyLength * 1e-9))
            throw new ArgumentException($"Pose segment length does not match anatomy part '{partId}'.");
    }

    private static CreatureVector3V1 Add(CreatureVector3V1 first, CreatureVector3V1 second)
        => new(first.X + second.X, first.Y + second.Y, first.Z + second.Z);

    private static CreatureVector3V1 Subtract(CreatureVector3V1 first, CreatureVector3V1 second)
        => new(first.X - second.X, first.Y - second.Y, first.Z - second.Z);

    private static CreatureVector3V1 Scale(CreatureVector3V1 value, double amount)
        => new(value.X * amount, value.Y * amount, value.Z * amount);

    private static CreatureVector3V1 Cross(CreatureVector3V1 first, CreatureVector3V1 second)
        => new(first.Y * second.Z - first.Z * second.Y,
            first.Z * second.X - first.X * second.Z,
            first.X * second.Y - first.Y * second.X);

    private static double Dot(CreatureVector3V1 first, CreatureVector3V1 second)
        => first.X * second.X + first.Y * second.Y + first.Z * second.Z;

    private static double Length(CreatureVector3V1 value)
        => Math.Sqrt(Dot(value, value));

    private static CreatureVector3V1 Normalize(CreatureVector3V1 value)
    {
        var length = Length(value);
        return length > Epsilon ? Scale(value, 1 / length) : new(0, 1, 0);
    }
}
