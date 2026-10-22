namespace Game.Contracts;

/// <summary>Body-local pose coordinates: X is lateral, Y is vertical, and +Z is forward.</summary>
public readonly record struct CreatureVector3V1(double X, double Y, double Z);

public enum QuadrupedLimbV1 : byte
{
    FrontLeft = 1,
    FrontRight = 2,
    HindLeft = 3,
    HindRight = 4
}

/// <summary>A renderer-neutral two-segment limb solution. The contact may be clamped to reachable space.</summary>
public readonly record struct QuadrupedLimbPoseV1(
    QuadrupedLimbV1 Limb,
    CreatureVector3V1 HipAnchorM,
    CreatureVector3V1 KneePositionM,
    CreatureVector3V1 FootPositionM,
    double UpperSegmentLengthM,
    double LowerSegmentLengthM,
    double MinimumReachM,
    double MaximumReachM,
    bool ContactTargetReached);

public readonly record struct CreatureAxialJointPoseV1(
    string ChildPartId,
    int ParentPartIndex,
    int ChildPartIndex,
    double AngleDegrees);

public enum CreatureMovementHabitatV1 : byte
{
    Terrestrial = 1,
    Freshwater = 2
}

public enum CreatureMovementStopReasonV1 : byte
{
    TargetReached = 1,
    DistanceLimit = 2,
    SlopeLimit = 3,
    HabitatBoundary = 4
}

/// <summary>Ground elevation and surface-water depth sampled by their owning world modules.</summary>
public readonly record struct CreatureMovementRouteSampleV1(
    double XPositionM,
    double ZPositionM,
    double GroundElevationM,
    double SurfaceWaterDepthM);

/// <summary>
/// A caller-sampled route and explicit locomotion limits. Slope/wading apply to terrestrial travel;
/// minimum water depth applies to freshwater travel. Values remain inputs pending profile calibration.
/// </summary>
public readonly record struct CreatureMovementRequestV1(
    CreatureFamilyV1 Family,
    CreatureMovementHabitatV1 Habitat,
    IReadOnlyList<CreatureMovementRouteSampleV1> RouteSamples,
    double MaximumTravelDistanceM,
    double MaximumSlopeGrade,
    double MaximumWadeDepthM,
    double MinimumSwimDepthM,
    double MaximumSampleSpacingM);

public readonly record struct CreatureMovementResultV1(
    CreatureVector3V1 PositionM,
    double HorizontalDistanceM,
    int AcceptedRouteSamples,
    bool TargetReached,
    CreatureMovementStopReasonV1 StopReason);

/// <summary>Visual pose only; it does not imply thrust, displacement, or energy expenditure.</summary>
public sealed class CreatureFishSwimmingPoseV1
{
    private readonly IReadOnlyList<CreatureAxialJointPoseV1> _axialJoints;

    public CreatureFishSwimmingPoseV1(
        int poseVersion,
        ulong structuralSignature,
        double phase01,
        IEnumerable<CreatureAxialJointPoseV1> axialJoints)
    {
        ArgumentNullException.ThrowIfNull(axialJoints);
        PoseVersion = poseVersion;
        StructuralSignature = structuralSignature;
        Phase01 = phase01;
        _axialJoints = Array.AsReadOnly(axialJoints.ToArray());
    }

    public int PoseVersion { get; }
    public ulong StructuralSignature { get; }
    public double Phase01 { get; }
    public IReadOnlyList<CreatureAxialJointPoseV1> AxialJoints => _axialJoints;
}

public readonly record struct CreatureGeometryVertexV1(
    CreatureVector3V1 PositionM,
    CreatureVector3V1 Normal,
    double U,
    double V,
    uint Rgba);

/// <summary>Index ranges for one independently transformable generated surface.</summary>
public readonly record struct CreatureGeometrySurfaceV1(
    string SurfaceId,
    int FirstVertex,
    int VertexCount,
    int FirstIndex,
    int IndexCount);

/// <summary>Per-part surfaces plus sensory markers. The result is not a watertight organism mesh.</summary>
public sealed class CreatureGeometryV1
{
    private readonly IReadOnlyList<CreatureGeometryVertexV1> _vertices;
    private readonly IReadOnlyList<int> _indices;
    private readonly IReadOnlyList<CreatureGeometrySurfaceV1> _surfaces;

    public CreatureGeometryV1(
        int geometryVersion,
        ulong structuralSignature,
        int partCount,
        int featureCount,
        IEnumerable<CreatureGeometryVertexV1> vertices,
        IEnumerable<int> indices,
        IEnumerable<CreatureGeometrySurfaceV1>? surfaces = null)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(indices);
        GeometryVersion = geometryVersion;
        StructuralSignature = structuralSignature;
        PartCount = partCount;
        FeatureCount = featureCount;
        _vertices = Array.AsReadOnly(vertices.ToArray());
        _indices = Array.AsReadOnly(indices.ToArray());
        _surfaces = Array.AsReadOnly(surfaces?.ToArray() ?? []);
    }

    public int GeometryVersion { get; }
    public ulong StructuralSignature { get; }
    public int PartCount { get; }
    public int FeatureCount { get; }
    public IReadOnlyList<CreatureGeometryVertexV1> Vertices => _vertices;
    public IReadOnlyList<int> Indices => _indices;
    public IReadOnlyList<CreatureGeometrySurfaceV1> Surfaces => _surfaces;
}

/// <summary>Pure pose construction. It does not read or mutate world, agent, or resource state.</summary>
public interface ICreaturePoseSolverV1
{
    QuadrupedLimbPoseV1 SolveQuadrupedLimb(
        DevelopedBodyV1 anatomy,
        QuadrupedLimbV1 limb,
        CreatureVector3V1 footContactTargetM,
        double bendSign);

    CreatureFishSwimmingPoseV1 BuildFishSwimmingPose(
        DevelopedBodyV1 anatomy,
        double phase01,
        double amplitude01);
}

/// <summary>
/// Resolves a sampled, straight locomotion route against owner-provided terrain and water samples.
/// It does not apply force, energy cost, pathfinding, or persistent agent state.
/// </summary>
public interface ICreatureMovementSolverV1
{
    CreatureMovementResultV1 ResolveMovement(CreatureMovementRequestV1 request);
}

/// <summary>Renderer-neutral static surface generation from immutable developed anatomy and pigment recipe.</summary>
public interface ICreatureGeometryBuilderV1
{
    CreatureGeometryV1 BuildGeometry(
        DevelopedBodyV1 anatomy,
        CreatureVisualRecipeV1 visualRecipe,
        int longitudinalSegments = 6,
        int circumferentialSegments = 8);
}
