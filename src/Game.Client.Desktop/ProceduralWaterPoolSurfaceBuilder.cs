using Game.Contracts;
using Game.Core;
using Game.Modules.Hydrosphere;

namespace Game.Client.Desktop;

internal sealed class ProceduralWaterPoolSurface
{
    private readonly double[] _coverageByCell;
    private readonly double[] _poolStageMByCell;

    internal ProceduralWaterPoolSurface(
        double[] surfaceStageMByCell,
        double[] coverageByCell,
        double[] poolStageMByCell,
        int poolCount,
        int poolBasinCount)
    {
        SurfaceStageMByCell = surfaceStageMByCell;
        _coverageByCell = coverageByCell;
        _poolStageMByCell = poolStageMByCell;
        PoolCount = poolCount;
        PoolBasinCount = poolBasinCount;
    }

    public double[] SurfaceStageMByCell { get; }
    public double[] CoverageByCell => _coverageByCell;
    public double[] PoolStageMByCell => _poolStageMByCell;
    public int PoolCount { get; }
    public int PoolBasinCount { get; }

    public bool TryGetNearestPoolStage(double gridX, double gridY, out double stageM)
    {
        var centerX = (int)Math.Floor(gridX - 0.5);
        var centerY = (int)Math.Floor(gridY - 0.5);
        var nearestDistanceSquared = double.PositiveInfinity;
        var nearestStage = double.NaN;
        for (var y = centerY - 1; y <= centerY + 2; y++)
        for (var x = centerX - 1; x <= centerX + 2; x++)
        {
            if (x < 0 || x >= SimulationState.Width || y < 0 || y >= SimulationState.Height) continue;
            var index = y * SimulationState.Width + x;
            if (_coverageByCell[index] <= 0.05 || !double.IsFinite(_poolStageMByCell[index])) continue;

            var dx = gridX - (x + 0.5);
            var dy = gridY - (y + 0.5);
            var distanceSquared = dx * dx + dy * dy;
            if (distanceSquared >= nearestDistanceSquared) continue;
            nearestDistanceSquared = distanceSquared;
            nearestStage = _poolStageMByCell[index];
        }

        stageM = nearestStage;
        return double.IsFinite(nearestStage);
    }
}

/// <summary>Builds render-only level surfaces for connected snapshot water in topographic basins.</summary>
internal static class ProceduralWaterPoolSurfaceBuilder
{
    private const double MinimumPoolCellWaterMm = 96;
    private const double MinimumLevelizedCellFraction = 0.75;
    private const double MinimumVisiblePoolDepthM = 0.02;
    private static readonly (int X, int Y)[] Neighbors =
    [
        (-1,-1),(0,-1),(1,-1),(-1,0),(1,0),(-1,1),(0,1),(1,1)
    ];

    public static ProceduralWaterPoolSurface Build(
        IReadOnlyList<SurfaceWaterCellV1> terrain,
        IReadOnlyList<DepressionBasin> basins,
        int[] basinIdByCell,
        double cellAreaM2 = SimulationState.CellAreaM2,
        double[]? displayBedMByCell = null)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentNullException.ThrowIfNull(basins);
        ArgumentNullException.ThrowIfNull(basinIdByCell);
        if (terrain.Count != SimulationState.Width * SimulationState.Height
            || basinIdByCell.Length != terrain.Count)
            throw new ArgumentException("Water surface inputs must match the simulation grid dimensions.");
        if (displayBedMByCell is not null && displayBedMByCell.Length != terrain.Count)
            throw new ArgumentException("Display elevations must match the simulation grid dimensions.", nameof(displayBedMByCell));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellAreaM2);
        if (!double.IsFinite(cellAreaM2))
            throw new ArgumentOutOfRangeException(nameof(cellAreaM2));

        var surfaceStageMByCell = new double[terrain.Count];
        var bedMByCell = displayBedMByCell ?? new double[terrain.Count];
        for (var index = 0; index < terrain.Count; index++)
        {
            var cell = terrain[index];
            if (displayBedMByCell is null) bedMByCell[index] = cell.ElevationM;
            surfaceStageMByCell[index] = bedMByCell[index] + Math.Max(0, cell.SurfaceWaterMm) / 1000;
        }

        var coverageByCell = new double[terrain.Count];
        var poolStageMByCell = Enumerable.Repeat(double.NaN, terrain.Count).ToArray();
        var visited = new bool[terrain.Count];
        var poolCount = 0;
        var poolBasinIds = new HashSet<int>();
        foreach (var basin in basins)
        {
            if (basin.Id < 0 || basin.Id >= basins.Count || basin.Cells.Length == 0) continue;
            var spillStageM = FindDisplayedSpillStage(basin, terrain, basinIdByCell, bedMByCell)
                ?? basin.SpillElevationM;
            foreach (var start in basin.Cells)
            {
                if (start < 0 || start >= terrain.Count || visited[start]
                    || basinIdByCell[start] != basin.Id
                    || terrain[start].SurfaceWaterMm < MinimumPoolCellWaterMm) continue;

                var component = CollectWaterComponent(start, basin.Id, terrain, basinIdByCell, visited);
                if (component.Count == 0) continue;
                var storedVolumeM3 = component.Sum(index =>
                    Math.Max(0, terrain[index].SurfaceWaterMm) / 1000 * cellAreaM2);
                var lowestBedM = component.Min(index => bedMByCell[index]);
                if (!double.IsFinite(storedVolumeM3) || storedVolumeM3 <= 0
                    || !double.IsFinite(lowestBedM) || spillStageM <= lowestBedM)
                {
                    PreserveSnapshotSurface(component, terrain, bedMByCell,
                        surfaceStageMByCell, coverageByCell);
                    continue;
                }

                // Keep separate wet patches separate: this snapshot does not exchange water
                // between every cell of a topographic basin during a render update.
                var displayCapacityM3 = VolumeAtStageM3(component, bedMByCell, spillStageM, cellAreaM2);
                var targetVolumeM3 = Math.Min(storedVolumeM3, displayCapacityM3);
                var lowStageM = lowestBedM;
                var highStageM = spillStageM;
                for (var iteration = 0; iteration < 48; iteration++)
                {
                    var candidateStageM = (lowStageM + highStageM) * 0.5;
                    var candidateVolumeM3 = VolumeAtStageM3(component, bedMByCell,
                        candidateStageM, cellAreaM2);
                    if (candidateVolumeM3 < targetVolumeM3) lowStageM = candidateStageM;
                    else highStageM = candidateStageM;
                }

                var stageM = (lowStageM + highStageM) * 0.5;
                var visibleCells = component.Count(index =>
                    stageM - bedMByCell[index] >= MinimumVisiblePoolDepthM);
                if (visibleCells / (double)component.Count < MinimumLevelizedCellFraction)
                {
                    // Do not drain a visibly wet patch to make a basin-wide level surface.
                    // This model snapshot can hold disconnected local stores above uneven beds.
                    PreserveSnapshotSurface(component, terrain, bedMByCell,
                        surfaceStageMByCell, coverageByCell);
                    continue;
                }

                var renderedAny = false;
                foreach (var index in component)
                {
                    var depthM = stageM - bedMByCell[index];
                    if (depthM <= 0) continue;
                    poolStageMByCell[index] = stageM;
                    surfaceStageMByCell[index] = stageM;
                    coverageByCell[index] = SmoothStep(0.02, 0.16, depthM);
                    renderedAny |= coverageByCell[index] > 0;
                }

                if (!renderedAny) continue;
                poolCount++;
                poolBasinIds.Add(basin.Id);
            }
        }

        return new ProceduralWaterPoolSurface(surfaceStageMByCell, coverageByCell,
            poolStageMByCell, poolCount, poolBasinIds.Count);
    }

    private static List<int> CollectWaterComponent(
        int start,
        int basinId,
        IReadOnlyList<SurfaceWaterCellV1> terrain,
        int[] basinIdByCell,
        bool[] visited)
    {
        var result = new List<int>();
        var queue = new Queue<int>();
        queue.Enqueue(start);
        visited[start] = true;
        while (queue.TryDequeue(out var current))
        {
            result.Add(current);
            var cell = terrain[current];
            foreach (var (offsetX, offsetY) in Neighbors)
            {
                var x = cell.X + offsetX;
                var y = cell.Y + offsetY;
                if (x < 0 || x >= SimulationState.Width || y < 0 || y >= SimulationState.Height) continue;
                var next = y * SimulationState.Width + x;
                if (visited[next] || basinIdByCell[next] != basinId
                    || terrain[next].SurfaceWaterMm < MinimumPoolCellWaterMm) continue;
                visited[next] = true;
                queue.Enqueue(next);
            }
        }

        return result;
    }

    private static double? FindDisplayedSpillStage(
        DepressionBasin basin,
        IReadOnlyList<SurfaceWaterCellV1> terrain,
        int[] basinIdByCell,
        double[] bedMByCell)
    {
        var minimumSpillM = double.PositiveInfinity;
        foreach (var index in basin.Cells)
        {
            var x = index % SimulationState.Width;
            var y = index / SimulationState.Width;
            for (var offsetY = -1; offsetY <= 1; offsetY++)
            for (var offsetX = -1; offsetX <= 1; offsetX++)
            {
                if (offsetX == 0 && offsetY == 0) continue;
                var neighborX = x + offsetX;
                var neighborY = y + offsetY;
                if (neighborX < 0 || neighborX >= SimulationState.Width
                    || neighborY < 0 || neighborY >= SimulationState.Height) continue;
                var neighbor = neighborY * SimulationState.Width + neighborX;
                if (basinIdByCell[neighbor] == basin.Id) continue;
                if (terrain[neighbor].ElevationM < basin.SpillElevationM - 1e-9) continue;
                minimumSpillM = Math.Min(minimumSpillM, bedMByCell[neighbor]);
            }
        }

        return double.IsFinite(minimumSpillM) ? minimumSpillM : null;
    }

    private static double VolumeAtStageM3(
        IReadOnlyList<int> cells,
        double[] bedMByCell,
        double stageM,
        double cellAreaM2)
    {
        var volumeM3 = 0.0;
        foreach (var index in cells)
            volumeM3 += Math.Max(0, stageM - bedMByCell[index]) * cellAreaM2;
        return volumeM3;
    }

    private static void PreserveSnapshotSurface(
        IReadOnlyList<int> cells,
        IReadOnlyList<SurfaceWaterCellV1> terrain,
        double[] bedMByCell,
        double[] surfaceStageMByCell,
        double[] coverageByCell)
    {
        foreach (var index in cells)
        {
            surfaceStageMByCell[index] = bedMByCell[index]
                + Math.Max(0, terrain[index].SurfaceWaterMm) / 1000;
            coverageByCell[index] = SmoothStep(96, 420, terrain[index].SurfaceWaterMm);
        }
    }

    private static double SmoothStep(double edge0, double edge1, double value)
    {
        var t = Math.Clamp((value - edge0) / (edge1 - edge0), 0, 1);
        return t * t * (3 - 2 * t);
    }
}
