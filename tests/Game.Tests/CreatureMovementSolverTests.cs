using Game.Contracts;
using Game.Modules.Biosphere;

namespace Game.Tests;

public sealed class CreatureMovementSolverTests
{
    private readonly CreatureMovementSolverV1 _solver = new();

    [Fact]
    public void Terrestrial_route_reaches_target_and_tracks_ground_elevation()
    {
        var route = new[]
        {
            new CreatureMovementRouteSampleV1(0, 0, 10, 0),
            new CreatureMovementRouteSampleV1(1, 0, 10.25, 0),
            new CreatureMovementRouteSampleV1(2, 0, 10.5, 0)
        };

        var result = _solver.ResolveMovement(Request(CreatureFamilyV1.Quadruped, CreatureMovementHabitatV1.Terrestrial,
            route, maximumTravel: 3, maximumGrade: 0.3, maxWadeDepth: 0.1, minSwimDepth: 0.5, spacing: 1));

        Assert.True(result.TargetReached);
        Assert.Equal(CreatureMovementStopReasonV1.TargetReached, result.StopReason);
        Assert.InRange(result.HorizontalDistanceM, 2 - 1e-10, 2 + 1e-10);
        Assert.Equal(new CreatureVector3V1(2, 10.5, 0), result.PositionM);
        Assert.Equal(3, result.AcceptedRouteSamples);
    }

    [Fact]
    public void Terrestrial_route_stops_before_a_slope_above_the_callers_limit()
    {
        var route = new[]
        {
            new CreatureMovementRouteSampleV1(0, 0, 10, 0),
            new CreatureMovementRouteSampleV1(1, 0, 10, 0),
            new CreatureMovementRouteSampleV1(2, 0, 12, 0)
        };

        var result = _solver.ResolveMovement(Request(CreatureFamilyV1.Quadruped, CreatureMovementHabitatV1.Terrestrial,
            route, maximumTravel: 3, maximumGrade: 0.5, maxWadeDepth: 0.1, minSwimDepth: 0.5, spacing: 1));

        Assert.False(result.TargetReached);
        Assert.Equal(CreatureMovementStopReasonV1.SlopeLimit, result.StopReason);
        Assert.Equal(new CreatureVector3V1(1, 10, 0), result.PositionM);
        Assert.Equal(1, result.HorizontalDistanceM);
    }

    [Fact]
    public void Terrestrial_route_stops_at_the_interpolated_wading_boundary()
    {
        var route = new[]
        {
            new CreatureMovementRouteSampleV1(0, 0, 3, 0),
            new CreatureMovementRouteSampleV1(1, 0, 3, 0.1),
            new CreatureMovementRouteSampleV1(2, 0, 3, 0.4)
        };

        var result = _solver.ResolveMovement(Request(CreatureFamilyV1.Quadruped, CreatureMovementHabitatV1.Terrestrial,
            route, maximumTravel: 3, maximumGrade: 1, maxWadeDepth: 0.2, minSwimDepth: 0.5, spacing: 1));

        Assert.False(result.TargetReached);
        Assert.Equal(CreatureMovementStopReasonV1.HabitatBoundary, result.StopReason);
        Assert.InRange(result.PositionM.X, 1.3333333332, 1.3333333334);
        Assert.Equal(3, result.PositionM.Y);
        Assert.InRange(result.HorizontalDistanceM, 1.3333333332, 1.3333333334);
    }

    [Fact]
    public void Freshwater_route_stops_at_the_shallow_water_boundary_and_tracks_mid_column_height()
    {
        var route = new[]
        {
            new CreatureMovementRouteSampleV1(0, 0, 4, 1),
            new CreatureMovementRouteSampleV1(1, 0, 4, 0.8),
            new CreatureMovementRouteSampleV1(2, 0, 4, 0.2)
        };

        var result = _solver.ResolveMovement(Request(CreatureFamilyV1.Fish, CreatureMovementHabitatV1.Freshwater,
            route, maximumTravel: 3, maximumGrade: 0.5, maxWadeDepth: 0, minSwimDepth: 0.5, spacing: 1));

        Assert.False(result.TargetReached);
        Assert.Equal(CreatureMovementStopReasonV1.HabitatBoundary, result.StopReason);
        Assert.InRange(result.PositionM.X, 1.499999999, 1.500000001);
        Assert.InRange(result.PositionM.Y, 4.249999999, 4.250000001);
        Assert.InRange(result.HorizontalDistanceM, 1.499999999, 1.500000001);
    }

    [Fact]
    public void Freshwater_route_uses_water_depth_instead_of_terrestrial_bottom_slope_limit()
    {
        var route = new[]
        {
            new CreatureMovementRouteSampleV1(0, 0, 0, 1),
            new CreatureMovementRouteSampleV1(1, 0, 10, 1),
            new CreatureMovementRouteSampleV1(2, 0, 24, 1)
        };

        var result = _solver.ResolveMovement(Request(CreatureFamilyV1.Fish, CreatureMovementHabitatV1.Freshwater,
            route, maximumTravel: 3, maximumGrade: 0, maxWadeDepth: 0, minSwimDepth: 0.5, spacing: 1));

        Assert.True(result.TargetReached);
        Assert.Equal(CreatureMovementStopReasonV1.TargetReached, result.StopReason);
        Assert.Equal(new CreatureVector3V1(2, 24.5, 0), result.PositionM);
        Assert.Equal(2, result.HorizontalDistanceM);
    }

    [Fact]
    public void Freshwater_route_does_not_accept_dry_cells_even_when_minimum_depth_is_zero()
    {
        var route = new[]
        {
            new CreatureMovementRouteSampleV1(0, 0, 2, 0),
            new CreatureMovementRouteSampleV1(1, 0, 2, 1)
        };

        var result = _solver.ResolveMovement(Request(CreatureFamilyV1.Fish, CreatureMovementHabitatV1.Freshwater,
            route, maximumTravel: 2, maximumGrade: 1, maxWadeDepth: 0, minSwimDepth: 0, spacing: 1));

        Assert.False(result.TargetReached);
        Assert.Equal(CreatureMovementStopReasonV1.HabitatBoundary, result.StopReason);
        Assert.Equal(0, result.HorizontalDistanceM);
    }

    [Fact]
    public void Travel_budget_stops_inside_a_valid_route_segment()
    {
        var route = new[]
        {
            new CreatureMovementRouteSampleV1(0, 0, 2, 0),
            new CreatureMovementRouteSampleV1(2, 0, 2, 0),
            new CreatureMovementRouteSampleV1(4, 0, 2, 0)
        };

        var result = _solver.ResolveMovement(Request(CreatureFamilyV1.Quadruped, CreatureMovementHabitatV1.Terrestrial,
            route, maximumTravel: 2.5, maximumGrade: 1, maxWadeDepth: 0, minSwimDepth: 0, spacing: 2));

        Assert.False(result.TargetReached);
        Assert.Equal(CreatureMovementStopReasonV1.DistanceLimit, result.StopReason);
        Assert.Equal(new CreatureVector3V1(2.5, 2, 0), result.PositionM);
        Assert.Equal(2.5, result.HorizontalDistanceM);
    }

    [Fact]
    public void Movement_rejects_sparse_samples_and_incompatible_body_habitat_pairs()
    {
        var sparseRoute = new[]
        {
            new CreatureMovementRouteSampleV1(0, 0, 0, 0),
            new CreatureMovementRouteSampleV1(2, 0, 0, 0)
        };
        Assert.Throws<ArgumentException>(() => _solver.ResolveMovement(Request(CreatureFamilyV1.Quadruped,
            CreatureMovementHabitatV1.Terrestrial, sparseRoute,
            maximumTravel: 2, maximumGrade: 1, maxWadeDepth: 0, minSwimDepth: 0, spacing: 1)));

        var validRoute = new[]
        {
            new CreatureMovementRouteSampleV1(0, 0, 0, 1),
            new CreatureMovementRouteSampleV1(1, 0, 0, 1)
        };
        Assert.Throws<ArgumentException>(() => _solver.ResolveMovement(Request(CreatureFamilyV1.Quadruped,
            CreatureMovementHabitatV1.Freshwater, validRoute,
            maximumTravel: 1, maximumGrade: 1, maxWadeDepth: 0, minSwimDepth: 0.5, spacing: 1)));
    }

    [Fact]
    public void Movement_is_deterministic_and_does_not_mutate_route_samples()
    {
        var route = new[]
        {
            new CreatureMovementRouteSampleV1(0, 0, 1, 0),
            new CreatureMovementRouteSampleV1(0, 1, 1.2, 0),
            new CreatureMovementRouteSampleV1(0, 2, 1.4, 0)
        };
        var original = route.ToArray();
        var request = Request(CreatureFamilyV1.Quadruped, CreatureMovementHabitatV1.Terrestrial,
            route, maximumTravel: 2, maximumGrade: 0.3, maxWadeDepth: 0.1, minSwimDepth: 0.5, spacing: 1);

        var first = _solver.ResolveMovement(request);
        var second = _solver.ResolveMovement(request);

        Assert.Equal(first, second);
        Assert.Equal(original, route);
    }

    private static CreatureMovementRequestV1 Request(
        CreatureFamilyV1 family,
        CreatureMovementHabitatV1 habitat,
        IReadOnlyList<CreatureMovementRouteSampleV1> route,
        double maximumTravel,
        double maximumGrade,
        double maxWadeDepth,
        double minSwimDepth,
        double spacing)
        => new(family, habitat, route, maximumTravel, maximumGrade,
            maxWadeDepth, minSwimDepth, spacing);
}
