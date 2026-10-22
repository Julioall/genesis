using Game.Contracts;

namespace Game.Modules.Hydrosphere;

public readonly record struct SurfaceFlowResult(WorldCell[] Cells, double OutflowLiters);
public readonly record struct SurfaceDrainageField(int[] Receiver, double[] Accumulation);
public readonly record struct DepressionBasin(int Id, double SpillElevationM, int[] Cells, double CapacityM3);
public readonly record struct HydrologyTopology(
    double[] FilledElevationM,
    int[] Receiver,
    double[] Accumulation,
    int[] DepressionId,
    DepressionBasin[] Basins);

public static class SurfaceWaterModel
{
    private const double Diffusion = 0.02;
    public const int ChannelThresholdCells = 12;
    private const double InitialAquiferStorageMm = 2600;
    private const double BaseflowMinimumMmPerHour = 0.30;
    private const double BaseflowPerLog2AreaMmPerHour = 0.08;
    private static readonly (int X, int Y)[] Neighbors =
    [
        (-1,-1),(0,-1),(1,-1),(-1,0),(1,0),(-1,1),(0,1),(1,1)
    ];

    public static SurfaceDrainageField AnalyzeDrainage(WorldCell[] terrain)
    {
        var drainage = BuildDrainage(terrain);
        return new(drainage.Receiver, drainage.Accumulation);
    }

    /// <summary>
    /// Conditions depressions for drainage analysis while preserving the input bed elevations.
    /// Filled elevations are a separate topological field; they are not written back to the world.
    /// </summary>
    public static HydrologyTopology AnalyzeHydrology(WorldCell[] terrain)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        var width = SimulationState.Width;
        var height = SimulationState.Height;
        if (terrain.Length != width * height)
            throw new ArgumentException("Terrain must match the simulation grid dimensions.", nameof(terrain));

        var filled = Enumerable.Repeat(double.PositiveInfinity, terrain.Length).ToArray();
        var receiver = Enumerable.Repeat(-1, terrain.Length).ToArray();
        var visited = new bool[terrain.Length];
        var floodOrder = new List<int>(terrain.Length);
        var frontier = new PriorityQueue<int, (double ElevationM, int Index)>();

        for (var index = 0; index < terrain.Length; index++)
        {
            var cell = terrain[index];
            if (cell.X != index % width || cell.Y != index / width)
                throw new ArgumentException("Terrain cells must be stored in row-major grid order.", nameof(terrain));
            if (!double.IsFinite(cell.ElevationM))
                throw new ArgumentException("Terrain elevations must be finite.", nameof(terrain));
            if (cell.X != 0 && cell.X != width - 1 && cell.Y != 0 && cell.Y != height - 1) continue;

            visited[index] = true;
            filled[index] = cell.ElevationM;
            frontier.Enqueue(index, (filled[index], index));
        }

        while (frontier.TryDequeue(out var current, out _))
        {
            floodOrder.Add(current);
            var cell = terrain[current];
            foreach (var (dx, dy) in Neighbors)
            {
                var x = cell.X + dx;
                var y = cell.Y + dy;
                if (x < 0 || x >= width || y < 0 || y >= height) continue;
                var next = y * width + x;
                if (visited[next]) continue;

                visited[next] = true;
                filled[next] = Math.Max(terrain[next].ElevationM, filled[current]);
                receiver[next] = current;
                frontier.Enqueue(next, (filled[next], next));
            }
        }

        var accumulation = Enumerable.Repeat(1.0, terrain.Length).ToArray();
        for (var order = floodOrder.Count - 1; order >= 0; order--)
        {
            var index = floodOrder[order];
            if (receiver[index] >= 0) accumulation[receiver[index]] += accumulation[index];
        }

        var depressionIds = Enumerable.Repeat(-1, terrain.Length).ToArray();
        var basins = new List<DepressionBasin>();
        var basinQueue = new Queue<int>();
        for (var seed = 0; seed < terrain.Length; seed++)
        {
            if (depressionIds[seed] >= 0 || filled[seed] <= terrain[seed].ElevationM + 1e-9) continue;
            var spillElevation = filled[seed];
            var basinId = basins.Count;
            var basinCells = new List<int>();
            var capacityM3 = 0.0;
            depressionIds[seed] = basinId;
            basinQueue.Enqueue(seed);
            while (basinQueue.TryDequeue(out var current))
            {
                basinCells.Add(current);
                capacityM3 += (spillElevation - terrain[current].ElevationM) * SimulationState.CellAreaM2;
                var cell = terrain[current];
                foreach (var (dx, dy) in Neighbors)
                {
                    var x = cell.X + dx;
                    var y = cell.Y + dy;
                    if (x < 0 || x >= width || y < 0 || y >= height) continue;
                    var next = y * width + x;
                    if (depressionIds[next] >= 0
                        || filled[next] <= terrain[next].ElevationM + 1e-9
                        || Math.Abs(filled[next] - spillElevation) > 1e-9) continue;
                    depressionIds[next] = basinId;
                    basinQueue.Enqueue(next);
                }
            }

            basins.Add(new DepressionBasin(basinId, spillElevation, basinCells.ToArray(), capacityM3));
        }

        return new(filled, receiver, accumulation, depressionIds, basins.ToArray());
    }

    /// <summary>Returns the stored volume under a level pool, capped at the basin spill elevation.</summary>
    public static double DepressionVolumeAtStageM3(
        WorldCell[] terrain, DepressionBasin basin, double stageM, double cellAreaM2 = SimulationState.CellAreaM2)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellAreaM2);
        var stage = Math.Min(stageM, basin.SpillElevationM);
        var volume = 0.0;
        foreach (var index in basin.Cells)
            volume += Math.Max(0, stage - terrain[index].ElevationM) * cellAreaM2;
        return volume;
    }

    /// <summary>Returns the basin area submerged by a level pool, capped at the spill elevation.</summary>
    public static double DepressionAreaAtStageM2(
        WorldCell[] terrain, DepressionBasin basin, double stageM, double cellAreaM2 = SimulationState.CellAreaM2)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellAreaM2);
        var stage = Math.Min(stageM, basin.SpillElevationM);
        return basin.Cells.Count(index => terrain[index].ElevationM < stage) * cellAreaM2;
    }

    /// <summary>Creates the initial surface-water stores from the terrain's D8 drainage network.</summary>
    public static WorldCell[] SeedInitialWater(WorldCell[] terrain)
    {
        var drainage = BuildDrainage(terrain);
        var receiver = drainage.Receiver;
        var accumulation = drainage.Accumulation;

        var result = (WorldCell[])terrain.Clone();
        for (var i = 0; i < result.Length; i++)
        {
            var cellsOfFlow = accumulation[i];
            if (cellsOfFlow < ChannelThresholdCells) continue;
            var depth = Math.Clamp(72 + 24 * Math.Log2(cellsOfFlow / 12), 72, 220);
            result[i] = result[i] with
            {
                SurfaceWaterMm = depth,
                GroundwaterMm = InitialAquiferStorageMm
            };
        }

        // Seed a few level pools from measured depression stage curves. The original bed remains
        // untouched, and every initial pool stays below its topographic spill elevation.
        var topology = AnalyzeHydrology(terrain);
        var lakeBasins = topology.Basins
            .Where(basin => basin.Cells.Length >= 3 && basin.CapacityM3 >= 100)
            .OrderByDescending(basin => basin.CapacityM3)
            .ThenBy(basin => basin.Cells[0])
            .ToArray();
        var acceptedLakeCenters = new List<int>();
        foreach (var basin in lakeBasins)
        {
            var centerIndex = basin.Cells.OrderBy(index => terrain[index].ElevationM).ThenBy(index => index).First();
            var center = terrain[centerIndex];
            if (acceptedLakeCenters.Count >= 3) break;
            if (acceptedLakeCenters.Any(other =>
                Math.Abs(terrain[other].X - center.X) < 9 && Math.Abs(terrain[other].Y - center.Y) < 9)) continue;

            var targetVolumeM3 = Math.Min(basin.CapacityM3 * 0.45, basin.Cells.Length * SimulationState.CellAreaM2 * 0.9);
            var low = center.ElevationM;
            var high = basin.SpillElevationM;
            for (var iteration = 0; iteration < 32; iteration++)
            {
                var stage = (low + high) * 0.5;
                if (DepressionVolumeAtStageM3(terrain, basin, stage) < targetVolumeM3) low = stage;
                else high = stage;
            }
            var waterlineM = (low + high) * 0.5;
            if (waterlineM <= center.ElevationM + 0.05) continue;

            acceptedLakeCenters.Add(centerIndex);
            foreach (var index in basin.Cells)
            {
                var depth = (waterlineM - terrain[index].ElevationM) * 1000;
                if (depth > 0) result[index] = result[index] with { SurfaceWaterMm = Math.Max(result[index].SurfaceWaterMm, depth) };
            }
        }

        return result;
    }

    public static SurfaceFlowResult Route(WorldCell[] source)
    {
        var drainage = BuildDrainage(source);
        var working = (WorldCell[])source.Clone();
        for (var i = 0; i < working.Length; i++)
        {
            var cellsOfFlow = drainage.Accumulation[i];
            if (cellsOfFlow < ChannelThresholdCells || working[i].GroundwaterMm <= 0) continue;
            var baseflow = BaseflowMinimumMmPerHour
                + BaseflowPerLog2AreaMmPerHour * Math.Log2(cellsOfFlow / ChannelThresholdCells);
            baseflow = Math.Min(working[i].GroundwaterMm, baseflow);
            working[i] = working[i] with
            {
                GroundwaterMm = working[i].GroundwaterMm - baseflow,
                SurfaceWaterMm = working[i].SurfaceWaterMm + baseflow
            };
        }

        var proposed = new List<(int From, int To, double Mm)>();
        var outgoing = new double[working.Length];
        for (var i = 0; i < working.Length; i++)
        {
            var cell = working[i];
            var head = cell.ElevationM + cell.SurfaceWaterMm / 1000;
            foreach (var (dx, dy) in Neighbors)
            {
                var x = cell.X + dx;
                var y = cell.Y + dy;
                if (x < 0 || x >= SimulationState.Width || y < 0 || y >= SimulationState.Height)
                {
                    var boundary = Diffusion * cell.SurfaceWaterMm;
                    if (boundary > 0)
                    {
                        proposed.Add((i, -1, boundary));
                        outgoing[i] += boundary;
                    }
                    continue;
                }

                var target = y * SimulationState.Width + x;
                if (target <= i) continue;
                var other = working[target];
                var otherHead = other.ElevationM + other.SurfaceWaterMm / 1000;
                if (head == otherHead) continue;
                var from = head > otherHead ? i : target;
                var to = head > otherHead ? target : i;
                var flow = Diffusion * 1000 * Math.Abs(head - otherHead);
                if (flow <= 0) continue;
                proposed.Add((from, to, flow));
                outgoing[from] += flow;
            }
        }

        var delta = new double[working.Length];
        var outflowLiters = 0.0;
        foreach (var proposal in proposed)
        {
            var available = working[proposal.From].SurfaceWaterMm;
            var scale = outgoing[proposal.From] > available && outgoing[proposal.From] > 0
                ? available / outgoing[proposal.From]
                : 1;
            var flow = proposal.Mm * scale;
            delta[proposal.From] -= flow;
            if (proposal.To < 0) outflowLiters += flow * SimulationState.CellAreaM2;
            else delta[proposal.To] += flow;
        }

        var result = working;
        for (var i = 0; i < result.Length; i++)
            result[i] = result[i] with { SurfaceWaterMm = Math.Max(0, result[i].SurfaceWaterMm + delta[i]) };
        return new(result, outflowLiters);
    }

    private static DrainageNetwork BuildDrainage(WorldCell[] terrain)
    {
        var receiver = Enumerable.Repeat(-1, terrain.Length).ToArray();
        var accumulation = Enumerable.Repeat(1.0, terrain.Length).ToArray();
        var downhillOrder = Enumerable.Range(0, terrain.Length)
            .OrderByDescending(index => terrain[index].ElevationM)
            .ThenBy(index => index)
            .ToArray();

        for (var i = 0; i < terrain.Length; i++)
        {
            var cell = terrain[i];
            var steepestSlope = 0.0;
            foreach (var (dx, dy) in Neighbors)
            {
                var x = cell.X + dx;
                var y = cell.Y + dy;
                if (x < 0 || x >= SimulationState.Width || y < 0 || y >= SimulationState.Height) continue;
                var target = y * SimulationState.Width + x;
                var drop = cell.ElevationM - terrain[target].ElevationM;
                if (drop <= 0) continue;
                var slope = drop / Math.Sqrt(dx * dx + dy * dy);
                if (slope <= steepestSlope) continue;
                steepestSlope = slope;
                receiver[i] = target;
            }
            if (receiver[i] < 0 && (cell.X == 0 || cell.X == SimulationState.Width - 1
                                     || cell.Y == 0 || cell.Y == SimulationState.Height - 1))
                receiver[i] = -2; // open map-edge outlet, not an enclosed lake
        }

        foreach (var index in downhillOrder)
            if (receiver[index] >= 0) accumulation[receiver[index]] += accumulation[index];
        return new DrainageNetwork(receiver, accumulation);
    }

    private readonly record struct DrainageNetwork(int[] Receiver, double[] Accumulation);
}
