using Game.Contracts;

namespace Game.Modules.Biosphere;

/// <summary>
/// Plans deterministic grid detours from caller-sampled terrain and water fields. It owns no
/// environmental state and does not replace the movement solver's final route validation.
/// </summary>
public static class CreatureTerrainRoutePlannerV1
{
    private enum RouteMode : byte { Terrestrial, Freshwater }

    private const double Epsilon = 1e-9;
    private static readonly (int X, int Z)[] NeighborOffsets =
    [
        (0, -1), (-1, 0), (1, 0), (0, 1),
        (-1, -1), (1, -1), (-1, 1), (1, 1)
    ];
    [ThreadStatic] private static RoutePlanningWorkspace? _workspace;

    private sealed class RoutePlanningWorkspace
    {
        public readonly double[] Costs = new double[SimulationState.Width * SimulationState.Height];
        public readonly int[] Previous = new int[SimulationState.Width * SimulationState.Height];
        public readonly PriorityQueue<int, (double Score, double Cost, int Index)> Frontier = new();
    }

    public static IReadOnlyList<CreatureMovementRouteSampleV1>? PlanTerrestrialRoute(
        IReadOnlyList<CreatureMovementRouteSampleV1> environmentalGrid,
        int columns,
        int rows,
        double cellSpacingM,
        CreatureMovementRouteSampleV1 start,
        CreatureMovementRouteSampleV1 goal,
        double maximumSlopeGrade,
        double maximumWadeDepthM,
        double maximumSampleSpacingM)
        => PlanGridRoute(environmentalGrid, columns, rows, cellSpacingM, start, goal,
            RouteMode.Terrestrial, maximumSlopeGrade, maximumWadeDepthM, maximumSampleSpacingM, true);

    // Zoology constructs this complete grid from its owner-controlled cache immediately before
    // a detour request. Rechecking every cell for each animal would add no validation coverage.
    internal static IReadOnlyList<CreatureMovementRouteSampleV1>? PlanTerrestrialRouteFromValidatedGrid(
        IReadOnlyList<CreatureMovementRouteSampleV1> environmentalGrid,
        int columns,
        int rows,
        double cellSpacingM,
        CreatureMovementRouteSampleV1 start,
        CreatureMovementRouteSampleV1 goal,
        double maximumSlopeGrade,
        double maximumWadeDepthM,
        double maximumSampleSpacingM)
        => PlanGridRoute(environmentalGrid, columns, rows, cellSpacingM, start, goal,
            RouteMode.Terrestrial, maximumSlopeGrade, maximumWadeDepthM, maximumSampleSpacingM, false);

    /// <summary>
    /// Plans a freshwater route through cells with enough caller-sampled water depth.
    /// Bottom slope does not block a fish route; solid-boundary/current fields are not yet published.
    /// </summary>
    public static IReadOnlyList<CreatureMovementRouteSampleV1>? PlanFreshwaterRoute(
        IReadOnlyList<CreatureMovementRouteSampleV1> environmentalGrid,
        int columns,
        int rows,
        double cellSpacingM,
        CreatureMovementRouteSampleV1 start,
        CreatureMovementRouteSampleV1 goal,
        double minimumSwimDepthM,
        double maximumSampleSpacingM)
        => PlanGridRoute(environmentalGrid, columns, rows, cellSpacingM, start, goal,
            RouteMode.Freshwater, minimumSwimDepthM, 0, maximumSampleSpacingM, true);

    private static IReadOnlyList<CreatureMovementRouteSampleV1>? PlanGridRoute(
        IReadOnlyList<CreatureMovementRouteSampleV1> environmentalGrid,
        int columns,
        int rows,
        double cellSpacingM,
        CreatureMovementRouteSampleV1 start,
        CreatureMovementRouteSampleV1 goal,
        RouteMode mode,
        double routeLimit,
        double secondaryLimit,
        double maximumSampleSpacingM,
        bool validateGrid)
    {
        ArgumentNullException.ThrowIfNull(environmentalGrid);
        if (validateGrid) ValidateGrid(environmentalGrid, columns, rows, cellSpacingM);
        ValidateSample(start, nameof(start));
        ValidateSample(goal, nameof(goal));
        if (!double.IsFinite(routeLimit) || routeLimit < 0
            || mode == RouteMode.Freshwater && routeLimit <= 0
            || mode == RouteMode.Terrestrial && (!double.IsFinite(secondaryLimit) || secondaryLimit < 0)
            || !double.IsFinite(maximumSampleSpacingM) || maximumSampleSpacingM <= 0)
            throw new ArgumentOutOfRangeException(nameof(routeLimit),
                "Movement limits must be finite and non-negative; freshwater depth and sample spacing must be positive.");

        var originX = environmentalGrid[0].XPositionM;
        var originZ = environmentalGrid[0].ZPositionM;
        var startIndex = FindNearestCell(start, originX, originZ, columns, rows, cellSpacingM);
        var goalIndex = FindNearestCell(goal, originX, originZ, columns, rows, cellSpacingM);
        if (startIndex < 0 || goalIndex < 0
            || !IsCellPassable(start, mode, routeLimit, secondaryLimit)
            || !IsCellPassable(goal, mode, routeLimit, secondaryLimit))
            return null;

        if (HorizontalDistance(start, goal) <= Epsilon)
            return Math.Abs(start.GroundElevationM - goal.GroundElevationM) <= Epsilon
                ? Array.AsReadOnly([start])
                : null;

        var startCell = environmentalGrid[startIndex];
        var goalCell = environmentalGrid[goalIndex];
        if (!CanTraverse(start, startCell, mode, routeLimit, secondaryLimit)
            || !CanTraverse(goalCell, goal, mode, routeLimit, secondaryLimit))
            return null;

        var cellPath = FindCellPath(environmentalGrid, columns, rows, cellSpacingM,
            startIndex, goalIndex, mode, routeLimit, secondaryLimit);
        if (cellPath is null) return null;

        var waypoints = new List<CreatureMovementRouteSampleV1>(cellPath.Count + 2) { start };
        foreach (var cellIndex in cellPath) AddWaypoint(waypoints, environmentalGrid[cellIndex]);
        AddWaypoint(waypoints, goal);
        return Densify(waypoints, maximumSampleSpacingM);
    }

    private static List<int>? FindCellPath(
        IReadOnlyList<CreatureMovementRouteSampleV1> grid,
        int columns,
        int rows,
        double spacing,
        int startIndex,
        int goalIndex,
        RouteMode mode,
        double routeLimit,
        double secondaryLimit)
    {
        if (startIndex == goalIndex) return [startIndex];

        var count = checked(columns * rows);
        var workspace = _workspace ??= new RoutePlanningWorkspace();
        if (workspace.Costs.Length < count)
            throw new InvalidOperationException("Route planning workspace is smaller than the requested grid.");
        var costs = workspace.Costs;
        var previous = workspace.Previous;
        Array.Fill(costs, double.PositiveInfinity, 0, count);
        Array.Fill(previous, -1, 0, count);
        var frontier = workspace.Frontier;
        frontier.Clear();
        costs[startIndex] = 0;
        frontier.Enqueue(startIndex, (Heuristic(grid[startIndex], grid[goalIndex]), 0, startIndex));

        while (frontier.TryDequeue(out var currentIndex, out var priority))
        {
            if (priority.Cost > costs[currentIndex] + Epsilon) continue;
            if (currentIndex == goalIndex) break;

            var currentX = currentIndex % columns;
            var currentZ = currentIndex / columns;
            foreach (var (offsetX, offsetZ) in NeighborOffsets)
            {
                var nextX = currentX + offsetX;
                var nextZ = currentZ + offsetZ;
                if (nextX < 0 || nextX >= columns || nextZ < 0 || nextZ >= rows) continue;
                var nextIndex = nextZ * columns + nextX;
                if (!CanTraverse(grid[currentIndex], grid[nextIndex], mode, routeLimit, secondaryLimit))
                    continue;

                if (offsetX != 0 && offsetZ != 0)
                {
                    var horizontalIndex = currentZ * columns + nextX;
                    var verticalIndex = nextZ * columns + currentX;
                    if (!CanTraverse(grid[currentIndex], grid[horizontalIndex], mode, routeLimit, secondaryLimit)
                        || !CanTraverse(grid[horizontalIndex], grid[nextIndex], mode, routeLimit, secondaryLimit)
                        || !CanTraverse(grid[currentIndex], grid[verticalIndex], mode, routeLimit, secondaryLimit)
                        || !CanTraverse(grid[verticalIndex], grid[nextIndex], mode, routeLimit, secondaryLimit))
                        continue;
                }

                var nextCost = costs[currentIndex] + HorizontalDistance(grid[currentIndex], grid[nextIndex]);
                if (nextCost + Epsilon >= costs[nextIndex]) continue;
                costs[nextIndex] = nextCost;
                previous[nextIndex] = currentIndex;
                frontier.Enqueue(nextIndex,
                    (nextCost + Heuristic(grid[nextIndex], grid[goalIndex]), nextCost, nextIndex));
            }
        }

        if (previous[goalIndex] < 0) return null;
        var path = new List<int>();
        for (var index = goalIndex; index >= 0; index = previous[index])
        {
            path.Add(index);
            if (index == startIndex) break;
        }
        if (path[^1] != startIndex) return null;
        path.Reverse();
        return path;
    }

    private static IReadOnlyList<CreatureMovementRouteSampleV1> Densify(
        IReadOnlyList<CreatureMovementRouteSampleV1> waypoints,
        double maximumSpacing)
    {
        var samples = new List<CreatureMovementRouteSampleV1> { waypoints[0] };
        for (var index = 1; index < waypoints.Count; index++)
        {
            var first = waypoints[index - 1];
            var second = waypoints[index];
            var distance = HorizontalDistance(first, second);
            if (distance <= Epsilon)
            {
                if (Math.Abs(first.GroundElevationM - second.GroundElevationM) > Epsilon)
                    throw new InvalidOperationException("A route segment cannot change elevation without horizontal distance.");
                continue;
            }

            var steps = Math.Max(1, (int)Math.Ceiling(distance / maximumSpacing));
            for (var step = 1; step <= steps; step++)
            {
                var fraction = (double)step / steps;
                samples.Add(Interpolate(first, second, fraction));
            }
        }
        return Array.AsReadOnly(samples.ToArray());
    }

    private static CreatureMovementRouteSampleV1 Interpolate(
        CreatureMovementRouteSampleV1 first,
        CreatureMovementRouteSampleV1 second,
        double fraction)
        => new(Lerp(first.XPositionM, second.XPositionM, fraction),
            Lerp(first.ZPositionM, second.ZPositionM, fraction),
            Lerp(first.GroundElevationM, second.GroundElevationM, fraction),
            Lerp(first.SurfaceWaterDepthM, second.SurfaceWaterDepthM, fraction));

    private static void AddWaypoint(
        List<CreatureMovementRouteSampleV1> waypoints,
        CreatureMovementRouteSampleV1 sample)
    {
        if (HorizontalDistance(waypoints[^1], sample) > Epsilon) waypoints.Add(sample);
    }

    private static bool CanTraverse(
        CreatureMovementRouteSampleV1 first,
        CreatureMovementRouteSampleV1 second,
        RouteMode mode,
        double routeLimit,
        double secondaryLimit)
    {
        if (!IsCellPassable(first, mode, routeLimit, secondaryLimit)
            || !IsCellPassable(second, mode, routeLimit, secondaryLimit))
            return false;
        if (mode == RouteMode.Freshwater) return true;
        var distance = HorizontalDistance(first, second);
        return distance <= Epsilon
            ? Math.Abs(first.GroundElevationM - second.GroundElevationM) <= Epsilon
            : Math.Abs(second.GroundElevationM - first.GroundElevationM) / distance
              <= routeLimit + Epsilon;
    }

    private static bool IsCellPassable(
        CreatureMovementRouteSampleV1 sample,
        RouteMode mode,
        double routeLimit,
        double secondaryLimit)
        => mode switch
        {
            RouteMode.Terrestrial => sample.SurfaceWaterDepthM <= secondaryLimit,
            RouteMode.Freshwater => sample.SurfaceWaterDepthM + Epsilon >= routeLimit,
            _ => false
        };

    private static int FindNearestCell(
        CreatureMovementRouteSampleV1 point,
        double originX,
        double originZ,
        int columns,
        int rows,
        double spacing)
    {
        var x = (int)Math.Floor((point.XPositionM - originX) / spacing + 0.5);
        var z = (int)Math.Floor((point.ZPositionM - originZ) / spacing + 0.5);
        return x < 0 || x >= columns || z < 0 || z >= rows ? -1 : z * columns + x;
    }

    private static void ValidateGrid(
        IReadOnlyList<CreatureMovementRouteSampleV1> grid,
        int columns,
        int rows,
        double spacing)
    {
        if (columns <= 0 || rows <= 0 || grid.Count != checked(columns * rows)
            || !double.IsFinite(spacing) || spacing <= 0)
            throw new ArgumentException("Environmental grid dimensions and cell spacing are invalid.", nameof(grid));
        var originX = grid[0].XPositionM;
        var originZ = grid[0].ZPositionM;
        for (var index = 0; index < grid.Count; index++)
        {
            var sample = grid[index];
            ValidateSample(sample, nameof(grid));
            var column = index % columns;
            var row = index / columns;
            var expectedX = originX + column * spacing;
            var expectedZ = originZ + row * spacing;
            if (Math.Abs(sample.XPositionM - expectedX) > Math.Max(Epsilon, spacing * 1e-9)
                || Math.Abs(sample.ZPositionM - expectedZ) > Math.Max(Epsilon, spacing * 1e-9))
                throw new ArgumentException("Environmental grid samples must be row-major and evenly spaced.", nameof(grid));
        }
    }

    private static void ValidateSample(CreatureMovementRouteSampleV1 sample, string parameterName)
    {
        if (!double.IsFinite(sample.XPositionM) || !double.IsFinite(sample.ZPositionM)
            || !double.IsFinite(sample.GroundElevationM)
            || !double.IsFinite(sample.SurfaceWaterDepthM) || sample.SurfaceWaterDepthM < 0)
            throw new ArgumentOutOfRangeException(parameterName, "Route samples must contain finite coordinates and non-negative water depth.");
    }

    private static double Heuristic(CreatureMovementRouteSampleV1 first, CreatureMovementRouteSampleV1 second)
        => HorizontalDistance(first, second);

    private static double HorizontalDistance(CreatureMovementRouteSampleV1 first, CreatureMovementRouteSampleV1 second)
    {
        var x = first.XPositionM - second.XPositionM;
        var z = first.ZPositionM - second.ZPositionM;
        return Math.Sqrt(x * x + z * z);
    }

    private static double Lerp(double first, double second, double fraction)
        => first + (second - first) * fraction;
}
