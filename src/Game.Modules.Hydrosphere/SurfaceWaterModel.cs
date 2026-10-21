using Game.Contracts;

namespace Game.Modules.Hydrosphere;

public readonly record struct SurfaceFlowResult(WorldCell[] Cells, double OutflowLiters);
public readonly record struct SurfaceDrainageField(int[] Receiver, double[] Accumulation);

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

        // A few large closed catchments become physical lake storage, selected by drainage area.
        var lakeSinks = Enumerable.Range(0, terrain.Length)
            .Where(index => receiver[index] == -1 && accumulation[index] >= 40)
            .OrderByDescending(index => accumulation[index])
            .ToArray();
        var acceptedLakeCenters = new List<int>();
        foreach (var sink in lakeSinks)
        {
            var center = terrain[sink];
            if (acceptedLakeCenters.Count >= 3) break;
            if (acceptedLakeCenters.Any(other =>
                Math.Abs(terrain[other].X - center.X) < 9 && Math.Abs(terrain[other].Y - center.Y) < 9)) continue;

            var waterlineM = center.ElevationM + Math.Clamp(0.6 + Math.Log2(accumulation[sink]) * 0.22, 0.8, 3.2);
            var queue = new Queue<int>();
            var visited = new HashSet<int> { sink };
            queue.Enqueue(sink);
            var lakeCells = new List<int>();
            while (queue.TryDequeue(out var current))
            {
                lakeCells.Add(current);
                var cell = terrain[current];
                foreach (var (dx, dy) in Neighbors)
                {
                    var x = cell.X + dx;
                    var y = cell.Y + dy;
                    if (x < 0 || x >= SimulationState.Width || y < 0 || y >= SimulationState.Height) continue;
                    var target = y * SimulationState.Width + x;
                    if (visited.Contains(target) || terrain[target].ElevationM < center.ElevationM
                        || terrain[target].ElevationM >= waterlineM) continue;
                    visited.Add(target);
                    queue.Enqueue(target);
                }
            }

            if (lakeCells.Count < 3) continue;
            acceptedLakeCenters.Add(sink);
            foreach (var index in lakeCells)
            {
                var depth = (waterlineM - terrain[index].ElevationM) * 1000;
                if (depth <= 0) continue;
                result[index] = result[index] with { SurfaceWaterMm = Math.Max(result[index].SurfaceWaterMm, depth) };
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
