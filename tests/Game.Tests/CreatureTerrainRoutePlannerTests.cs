using Game.Contracts;
using Game.Modules.Biosphere;

namespace Game.Tests;

public sealed class CreatureTerrainRoutePlannerTests
{
    [Fact]
    public void Planner_finds_a_deterministic_sloped_terrain_detour_with_dense_samples()
    {
        const int Columns = 5;
        const int Rows = 5;
        const double Spacing = 10;
        var grid = CreateGrid(Columns, Rows, Spacing,
            elevation: (x, z) => x == 2 && z == 2 ? 20 : 0,
            water: (_, _) => 0);
        var start = grid[2 * Columns];
        var goal = grid[2 * Columns + Columns - 1];

        var first = CreatureTerrainRoutePlannerV1.PlanTerrestrialRoute(grid, Columns, Rows, Spacing,
            start, goal, maximumSlopeGrade: 0.75, maximumWadeDepthM: 0.25, maximumSampleSpacingM: 5);
        var repeated = CreatureTerrainRoutePlannerV1.PlanTerrestrialRoute(grid, Columns, Rows, Spacing,
            start, goal, maximumSlopeGrade: 0.75, maximumWadeDepthM: 0.25, maximumSampleSpacingM: 5);

        Assert.NotNull(first);
        Assert.Equal(first, repeated);
        Assert.Equal(start, first![0]);
        Assert.Equal(goal, first[^1]);
        Assert.DoesNotContain(first, sample => sample.XPositionM == 25 && sample.ZPositionM == 25);
        Assert.All(first, sample => Assert.InRange(sample.SurfaceWaterDepthM, 0, 0.25));
        Assert.All(first.Zip(first.Skip(1)), pair =>
        {
            var distance = HorizontalDistance(pair.First, pair.Second);
            Assert.InRange(distance, 0, 5 + 1e-9);
            Assert.InRange(Math.Abs(pair.Second.GroundElevationM - pair.First.GroundElevationM)
                / Math.Max(1e-12, distance), 0, 0.75 + 1e-9);
        });
    }

    [Fact]
    public void Planner_returns_no_route_when_a_water_barrier_spans_the_grid()
    {
        const int Columns = 5;
        const int Rows = 5;
        const double Spacing = 10;
        var grid = CreateGrid(Columns, Rows, Spacing,
            elevation: (_, _) => 0,
            water: (_, z) => z == 2 ? 1 : 0);
        var start = grid[1 * Columns + 2];
        var goal = grid[3 * Columns + 2];

        var route = CreatureTerrainRoutePlannerV1.PlanTerrestrialRoute(grid, Columns, Rows, Spacing,
            start, goal, maximumSlopeGrade: 1, maximumWadeDepthM: 0.25, maximumSampleSpacingM: 5);

        Assert.Null(route);
    }

    [Fact]
    public void Planner_finds_a_route_around_an_isolated_deep_water_cell()
    {
        const int Columns = 5;
        const int Rows = 5;
        const double Spacing = 10;
        var grid = CreateGrid(Columns, Rows, Spacing,
            elevation: (_, _) => 0,
            water: (x, z) => x == 2 && z == 2 ? 1 : 0);
        var start = grid[2 * Columns];
        var goal = grid[2 * Columns + Columns - 1];

        var route = CreatureTerrainRoutePlannerV1.PlanTerrestrialRoute(grid, Columns, Rows, Spacing,
            start, goal, maximumSlopeGrade: 0.75, maximumWadeDepthM: 0.25, maximumSampleSpacingM: 5);

        Assert.NotNull(route);
        Assert.Equal(start, route![0]);
        Assert.Equal(goal, route[^1]);
        Assert.DoesNotContain(route, sample => sample.SurfaceWaterDepthM > 0.25);
        Assert.DoesNotContain(route, sample => sample.XPositionM == 25 && sample.ZPositionM == 25);
    }

    [Fact]
    public void Freshwater_planner_finds_a_deterministic_route_around_a_shallow_channel_gap()
    {
        const int Columns = 7;
        const int Rows = 5;
        const double Spacing = 10;
        var grid = CreateGrid(Columns, Rows,
            Spacing, elevation: (_, _) => 0,
            water: (x, z) => x == 3 && z is >= 1 and <= 3 ? 0 : 1.5);
        var start = grid[2 * Columns];
        var goal = grid[2 * Columns + Columns - 1];

        var first = CreatureTerrainRoutePlannerV1.PlanFreshwaterRoute(grid, Columns, Rows, Spacing,
            start, goal, minimumSwimDepthM: 0.5, maximumSampleSpacingM: 4);
        var repeated = CreatureTerrainRoutePlannerV1.PlanFreshwaterRoute(grid, Columns, Rows, Spacing,
            start, goal, minimumSwimDepthM: 0.5, maximumSampleSpacingM: 4);

        Assert.NotNull(first);
        Assert.Equal(first, repeated);
        Assert.Equal(start, first![0]);
        Assert.Equal(goal, first[^1]);
        Assert.All(first, sample => Assert.True(sample.SurfaceWaterDepthM >= 0.5));
        Assert.All(first.Zip(first.Skip(1)), pair =>
            Assert.InRange(HorizontalDistance(pair.First, pair.Second), 0, 4 + 1e-9));
    }

    [Fact]
    public void Freshwater_planner_does_not_cut_diagonally_across_shallow_corner_cells()
    {
        var grid = CreateGrid(2, 2, 10,
            elevation: (_, _) => 0,
            water: (x, z) => x == z ? 1 : 0);

        var route = CreatureTerrainRoutePlannerV1.PlanFreshwaterRoute(grid, 2, 2, 10,
            grid[0], grid[3], minimumSwimDepthM: 0.5, maximumSampleSpacingM: 5);

        Assert.Null(route);
    }

    [Fact]
    public void Freshwater_planner_returns_no_route_across_a_full_shallow_barrier()
    {
        const int Columns = 5;
        const int Rows = 5;
        var grid = CreateGrid(Columns, Rows, 10,
            elevation: (_, _) => 0,
            water: (_, z) => z == 2 ? 0 : 1);

        var route = CreatureTerrainRoutePlannerV1.PlanFreshwaterRoute(grid, Columns, Rows, 10,
            grid[1 * Columns + 2], grid[3 * Columns + 2],
            minimumSwimDepthM: 0.5, maximumSampleSpacingM: 5);

        Assert.Null(route);
    }

    [Fact]
    public void Planner_rejects_malformed_grid_and_non_finite_limits()
    {
        var grid = CreateGrid(2, 2, 10, (_, _) => 0, (_, _) => 0);
        var start = grid[0];
        var goal = grid[^1];
        var malformedGrid = grid.ToArray();
        malformedGrid[1] = malformedGrid[1] with { XPositionM = malformedGrid[1].XPositionM + 1 };

        Assert.Throws<ArgumentException>(() => CreatureTerrainRoutePlannerV1.PlanTerrestrialRoute(
            malformedGrid, 2, 2, 10, start, goal, 1, 0.25, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatureTerrainRoutePlannerV1.PlanTerrestrialRoute(
            grid, 2, 2, 10, start, goal, double.NaN, 0.25, 5));
    }

    private static CreatureMovementRouteSampleV1[] CreateGrid(
        int columns,
        int rows,
        double spacing,
        Func<int, int, double> elevation,
        Func<int, int, double> water)
    {
        var samples = new CreatureMovementRouteSampleV1[columns * rows];
        for (var z = 0; z < rows; z++)
        for (var x = 0; x < columns; x++)
            samples[z * columns + x] = new((x + 0.5) * spacing, (z + 0.5) * spacing,
                elevation(x, z), water(x, z));
        return samples;
    }

    private static double HorizontalDistance(
        CreatureMovementRouteSampleV1 first,
        CreatureMovementRouteSampleV1 second)
    {
        var x = first.XPositionM - second.XPositionM;
        var z = first.ZPositionM - second.ZPositionM;
        return Math.Sqrt(x * x + z * z);
    }
}
