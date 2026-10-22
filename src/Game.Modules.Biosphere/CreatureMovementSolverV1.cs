using Game.Contracts;

namespace Game.Modules.Biosphere;

/// <summary>
/// Resolves locomotion along a caller-sampled route. Environmental fields are observations supplied
/// by their owners; biological limits remain explicit request values until profile calibration exists.
/// </summary>
public sealed class CreatureMovementSolverV1 : ICreatureMovementSolverV1
{
    private const double Epsilon = 1e-10;

    public CreatureMovementResultV1 ResolveMovement(CreatureMovementRequestV1 request)
    {
        ArgumentNullException.ThrowIfNull(request.RouteSamples);
        var requiredFamily = request.Habitat switch
            {
                CreatureMovementHabitatV1.Terrestrial => CreatureFamilyV1.Quadruped,
                CreatureMovementHabitatV1.Freshwater => CreatureFamilyV1.Fish,
                _ => throw new ArgumentOutOfRangeException(nameof(request), "Movement habitat is unsupported.")
        };
        if (request.Family != requiredFamily)
            throw new ArgumentException("Body family is incompatible with the requested locomotion habitat.", nameof(request));
        if (request.RouteSamples.Count < 2)
            throw new ArgumentException("Movement requires a route with start and target samples.", nameof(request));
        if (!double.IsFinite(request.MaximumTravelDistanceM) || request.MaximumTravelDistanceM < 0
            || !double.IsFinite(request.MaximumSlopeGrade) || request.MaximumSlopeGrade < 0
            || !double.IsFinite(request.MaximumWadeDepthM) || request.MaximumWadeDepthM < 0
            || !double.IsFinite(request.MinimumSwimDepthM) || request.MinimumSwimDepthM < 0
            || !double.IsFinite(request.MaximumSampleSpacingM) || request.MaximumSampleSpacingM <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Movement limits must be finite and non-negative; sample spacing must be positive.");

        var samples = request.RouteSamples.ToArray();
        for (var index = 0; index < samples.Length; index++)
        {
            var sample = samples[index];
            if (!double.IsFinite(sample.XPositionM) || !double.IsFinite(sample.ZPositionM)
                || !double.IsFinite(sample.GroundElevationM)
                || !double.IsFinite(sample.SurfaceWaterDepthM) || sample.SurfaceWaterDepthM < 0)
                throw new ArgumentException($"Route sample {index} contains an invalid environmental value.", nameof(request));
            if (index == 0) continue;
            var segmentLength = HorizontalDistance(samples[index - 1], sample);
            if (!double.IsFinite(segmentLength) || segmentLength <= Epsilon)
                throw new ArgumentException($"Route segment {index - 1} has no horizontal length.", nameof(request));
            if (segmentLength > request.MaximumSampleSpacingM + Epsilon)
                throw new ArgumentException($"Route segment {index - 1} exceeds the maximum sample spacing.", nameof(request));
        }

        var start = samples[0];
        if (!IsHabitatValid(start, request))
            return ResultAt(start, 0, 1, false, CreatureMovementStopReasonV1.HabitatBoundary, request.Habitat);
        if (request.MaximumTravelDistanceM <= Epsilon)
            return ResultAt(start, 0, 1, false, CreatureMovementStopReasonV1.DistanceLimit, request.Habitat);

        var traveled = 0.0;
        for (var index = 1; index < samples.Length; index++)
        {
            var from = samples[index - 1];
            var to = samples[index];
            var segmentLength = HorizontalDistance(from, to);
            var grade = Math.Abs(to.GroundElevationM - from.GroundElevationM) / segmentLength;
            if (request.Habitat == CreatureMovementHabitatV1.Terrestrial
                && grade > request.MaximumSlopeGrade + Epsilon)
                return ResultAt(from, traveled, index, false,
                    CreatureMovementStopReasonV1.SlopeLimit, request.Habitat);

            var nextIsInHabitat = IsHabitatValid(to, request);
            if (!nextIsInHabitat)
            {
                var boundaryFraction = HabitatBoundaryFraction(from, to, request);
                var distanceToBoundary = segmentLength * boundaryFraction;
                var remainingDistance = request.MaximumTravelDistanceM - traveled;
                if (distanceToBoundary > remainingDistance + Epsilon)
                {
                    var travelFraction = Math.Clamp(remainingDistance / segmentLength, 0, 1);
                    var limited = Interpolate(from, to, travelFraction);
                    return ResultAt(limited, request.MaximumTravelDistanceM, index, false,
                        CreatureMovementStopReasonV1.DistanceLimit, request.Habitat);
                }

                var boundary = Interpolate(from, to, boundaryFraction);
                return ResultAt(boundary, traveled + distanceToBoundary, index, false,
                    CreatureMovementStopReasonV1.HabitatBoundary, request.Habitat);
            }

            var remaining = request.MaximumTravelDistanceM - traveled;
            if (segmentLength > remaining + Epsilon)
            {
                var fraction = Math.Clamp(remaining / segmentLength, 0, 1);
                var limited = Interpolate(from, to, fraction);
                return ResultAt(limited, request.MaximumTravelDistanceM, index, false,
                    CreatureMovementStopReasonV1.DistanceLimit, request.Habitat);
            }

            traveled += segmentLength;
        }

        return ResultAt(samples[^1], traveled, samples.Length, true,
            CreatureMovementStopReasonV1.TargetReached, request.Habitat);
    }

    private static bool IsHabitatValid(CreatureMovementRouteSampleV1 sample, CreatureMovementRequestV1 request)
        => request.Habitat switch
        {
            CreatureMovementHabitatV1.Terrestrial => sample.SurfaceWaterDepthM <= request.MaximumWadeDepthM + Epsilon,
            CreatureMovementHabitatV1.Freshwater => sample.SurfaceWaterDepthM > Epsilon
                && sample.SurfaceWaterDepthM + Epsilon >= request.MinimumSwimDepthM,
            _ => false
        };

    private static double HabitatBoundaryFraction(
        CreatureMovementRouteSampleV1 from,
        CreatureMovementRouteSampleV1 to,
        CreatureMovementRequestV1 request)
    {
        var deltaDepth = to.SurfaceWaterDepthM - from.SurfaceWaterDepthM;
        if (request.Habitat == CreatureMovementHabitatV1.Terrestrial)
        {
            if (deltaDepth <= 0) return 1;
            return Math.Clamp((request.MaximumWadeDepthM - from.SurfaceWaterDepthM) / deltaDepth, 0, 1);
        }

        if (deltaDepth >= 0) return 1;
        return Math.Clamp((request.MinimumSwimDepthM - from.SurfaceWaterDepthM) / deltaDepth, 0, 1);
    }

    private static CreatureMovementRouteSampleV1 Interpolate(
        CreatureMovementRouteSampleV1 from,
        CreatureMovementRouteSampleV1 to,
        double fraction)
        => new(
            from.XPositionM + (to.XPositionM - from.XPositionM) * fraction,
            from.ZPositionM + (to.ZPositionM - from.ZPositionM) * fraction,
            from.GroundElevationM + (to.GroundElevationM - from.GroundElevationM) * fraction,
            from.SurfaceWaterDepthM + (to.SurfaceWaterDepthM - from.SurfaceWaterDepthM) * fraction);

    private static CreatureMovementResultV1 ResultAt(
        CreatureMovementRouteSampleV1 sample,
        double distance,
        int acceptedSamples,
        bool targetReached,
        CreatureMovementStopReasonV1 reason,
        CreatureMovementHabitatV1 habitat)
    {
        var verticalPosition = habitat == CreatureMovementHabitatV1.Freshwater
            ? sample.GroundElevationM + sample.SurfaceWaterDepthM * 0.5
            : sample.GroundElevationM;
        return new(new(sample.XPositionM, verticalPosition, sample.ZPositionM), distance,
            acceptedSamples, targetReached, reason);
    }

    private static double HorizontalDistance(CreatureMovementRouteSampleV1 first, CreatureMovementRouteSampleV1 second)
    {
        var dx = second.XPositionM - first.XPositionM;
        var dz = second.ZPositionM - first.ZPositionM;
        return Math.Sqrt(dx * dx + dz * dz);
    }
}
