using System.Buffers;
using Game.Contracts;

namespace Game.Modules.Hydrosphere;

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
    private sealed record FlowNeighborLayout(int[] Offsets, int[] Targets);

    private const double Diffusion = 0.02;
    public const int ChannelThresholdCells = 12;
    private const double InitialAquiferStorageMm = 2600;
    private const double BaseflowMinimumMmPerHour = 0.30;
    private const double BaseflowPerLog2AreaMmPerHour = 0.08;
    private static readonly (int X, int Y)[] Neighbors =
    [
        (-1,-1),(0,-1),(1,-1),(-1,0),(1,0),(-1,1),(0,1),(1,1)
    ];
    private static readonly FlowNeighborLayout FlowNeighbors = CreateFlowNeighborLayout();

    public static SurfaceDrainageField AnalyzeDrainage(SurfaceWaterCellV1[] terrain)
    {
        var drainage = BuildDrainage(terrain);
        return new(drainage.Receiver, drainage.Accumulation);
    }

    /// <summary>
    /// Conditions depressions for drainage analysis while preserving the input bed elevations.
    /// Filled elevations are a separate topological field; they are not written back to the world.
    /// </summary>
    public static HydrologyTopology AnalyzeHydrology(SurfaceWaterCellV1[] terrain)
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
        SurfaceWaterCellV1[] terrain, DepressionBasin basin, double stageM, double cellAreaM2 = SimulationState.CellAreaM2)
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
        SurfaceWaterCellV1[] terrain, DepressionBasin basin, double stageM, double cellAreaM2 = SimulationState.CellAreaM2)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellAreaM2);
        var stage = Math.Min(stageM, basin.SpillElevationM);
        return basin.Cells.Count(index => terrain[index].ElevationM < stage) * cellAreaM2;
    }

    /// <summary>Creates the initial surface-water stores from the terrain's D8 drainage network.</summary>
    public static SurfaceWaterCellV1[] SeedInitialWater(SurfaceWaterCellV1[] terrain)
    {
        var drainage = BuildDrainage(terrain);
        var receiver = drainage.Receiver;
        var accumulation = drainage.Accumulation;

        var result = (SurfaceWaterCellV1[])terrain.Clone();
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

    public static SurfaceWaterExchangeResultV1 ReceivePrecipitation(
        SurfaceWaterStateV1 water,
        double precipitationMm,
        double plantCover,
        double soilInfiltrationCapacityMm,
        double potentialEtMm)
    {
        if (!double.IsFinite(water.SurfaceWaterMm) || water.SurfaceWaterMm < 0
            || !double.IsFinite(precipitationMm) || precipitationMm < 0
            || !double.IsFinite(plantCover) || plantCover is < 0 or > 1
            || !double.IsFinite(soilInfiltrationCapacityMm) || soilInfiltrationCapacityMm < 0
            || !double.IsFinite(potentialEtMm) || potentialEtMm < 0)
            throw new ArgumentOutOfRangeException(nameof(precipitationMm), "Surface-water exchange inputs must be finite and within their physical ranges.");

        var intercepted = 0.15 * plantCover * precipitationMm;
        var availableSurface = water.SurfaceWaterMm + precipitationMm - intercepted;
        var infiltration = Math.Min(availableSurface, soilInfiltrationCapacityMm);
        var surfaceBeforeEvaporation = availableSurface - infiltration;
        var waterCover = Math.Clamp(surfaceBeforeEvaporation / 10, 0, 1);
        var waterEvaporation = Math.Min(surfaceBeforeEvaporation, potentialEtMm * waterCover);

        return new(
            surfaceBeforeEvaporation - waterEvaporation,
            infiltration,
            waterEvaporation,
            intercepted,
            1 - waterCover);
    }

    public static SurfaceFlowResultV1 Route(SurfaceWaterCellV1[] source)
    {
        var routingPlan = CreateRoutingPlan(source);
        return Route(source, routingPlan);
    }

    public static SurfaceWaterRoutingPlanV1 CreateRoutingPlan(SurfaceWaterCellV1[] terrain)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        if (terrain.Length != SimulationState.Width * SimulationState.Height)
            throw new ArgumentException("A routing plan requires a complete surface-water terrain field.", nameof(terrain));
        for (var index = 0; index < terrain.Length; index++)
        {
            var cell = terrain[index];
            if (cell.Index != index || cell.X != index % SimulationState.Width
                || cell.Y != index / SimulationState.Width || !double.IsFinite(cell.ElevationM))
                throw new ArgumentException("Routing terrain must use finite elevations and canonical row-major cell identities.", nameof(terrain));
        }
        var drainage = BuildDrainage(terrain);
        return new(terrain.Select(cell => cell.ElevationM).ToArray(), drainage.Accumulation);
    }

    public static SurfaceFlowResultV1 Route(SurfaceWaterCellV1[] source, SurfaceWaterRoutingPlanV1 routingPlan)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Length != SimulationState.Width * SimulationState.Height
            || routingPlan.ElevationM is null || routingPlan.FlowAccumulation is null
            || routingPlan.ElevationM.Length != source.Length || routingPlan.FlowAccumulation.Length != source.Length)
            throw new ArgumentException("Surface-water source and routing plan must cover the complete world.", nameof(routingPlan));
        for (var index = 0; index < source.Length; index++)
        {
            var cell = source[index];
            if (cell.Index != index || cell.X != index % SimulationState.Width
                || cell.Y != index / SimulationState.Width || cell.ElevationM != routingPlan.ElevationM[index]
                || !double.IsFinite(cell.SurfaceWaterMm) || cell.SurfaceWaterMm < 0
                || !double.IsFinite(cell.GroundwaterMm) || cell.GroundwaterMm < 0
                || !double.IsFinite(routingPlan.FlowAccumulation[index]) || routingPlan.FlowAccumulation[index] < 1)
                throw new ArgumentException("Surface-water source does not match its immutable routing plan or contains invalid stocks.", nameof(source));
        }
        var accumulation = routingPlan.FlowAccumulation;
        var working = ArrayPool<SurfaceWaterCellV1>.Shared.Rent(source.Length);
        var outgoing = ArrayPool<double>.Shared.Rent(source.Length);
        var delta = ArrayPool<double>.Shared.Rent(source.Length);
        try
        {
        Array.Copy(source, working, source.Length);
        Array.Clear(outgoing, 0, source.Length);
        for (var i = 0; i < source.Length; i++)
        {
            var cellsOfFlow = accumulation[i];
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

        var proposed = new List<(int From, int To, double Mm)>(source.Length * 2);
        for (var i = 0; i < source.Length; i++)
        {
            var cell = working[i];
            var hasSurfaceWater = cell.SurfaceWaterMm > 0;
            var head = cell.ElevationM + cell.SurfaceWaterMm / 1000;
            for (var neighbor = FlowNeighbors.Offsets[i]; neighbor < FlowNeighbors.Offsets[i + 1]; neighbor++)
            {
                var target = FlowNeighbors.Targets[neighbor];
                if (target < 0)
                {
                    if (!hasSurfaceWater) continue;
                    var boundary = Diffusion * cell.SurfaceWaterMm;
                    if (boundary > 0)
                    {
                        proposed.Add((i, -1, boundary));
                        outgoing[i] += boundary;
                    }
                    continue;
                }
                var other = working[target];
                if (!hasSurfaceWater && other.SurfaceWaterMm <= 0) continue;
                var otherHead = other.ElevationM + other.SurfaceWaterMm / 1000;
                if (head == otherHead) continue;
                var from = head > otherHead ? i : target;
                var to = head > otherHead ? target : i;
                if (working[from].SurfaceWaterMm <= 0) continue;
                var flow = Diffusion * 1000 * Math.Abs(head - otherHead);
                if (flow <= 0) continue;
                proposed.Add((from, to, flow));
                outgoing[from] += flow;
            }
        }

        Array.Clear(delta, 0, source.Length);
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

        var result = new SurfaceWaterStateV1[source.Length];
        for (var i = 0; i < result.Length; i++)
            result[i] = new(working[i].Index,
                Math.Max(0, working[i].SurfaceWaterMm + delta[i]), working[i].GroundwaterMm);
        return new(result, outflowLiters);
        }
        finally
        {
            ArrayPool<SurfaceWaterCellV1>.Shared.Return(working);
            ArrayPool<double>.Shared.Return(outgoing);
            ArrayPool<double>.Shared.Return(delta);
        }
    }

    private static FlowNeighborLayout CreateFlowNeighborLayout()
    {
        var width = SimulationState.Width;
        var height = SimulationState.Height;
        var offsets = new int[width * height + 1];
        var targets = new List<int>(width * height * 5);
        for (var index = 0; index < width * height; index++)
        {
            var x = index % width;
            var y = index / width;
            offsets[index] = targets.Count;
            for (var neighbor = 0; neighbor < Neighbors.Length; neighbor++)
            {
                var nx = x + Neighbors[neighbor].X;
                var ny = y + Neighbors[neighbor].Y;
                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                {
                    targets.Add(-1);
                    continue;
                }
                var target = ny * width + nx;
                if (target > index) targets.Add(target);
            }
        }
        offsets[^1] = targets.Count;
        return new(offsets, targets.ToArray());
    }

    public static SurfaceWaterWithdrawalResultV1 AllocateAnimalWater(
        SurfaceWaterCellV1[] source,
        SurfaceWaterDemandV1[] demands)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(demands);
        if (source.Length != SimulationState.Width * SimulationState.Height)
            throw new ArgumentException("Surface-water withdrawals require a complete world field.", nameof(source));
        for (var index = 0; index < source.Length; index++)
        {
            if (source[index].Index != index || source[index].X != index % SimulationState.Width
                || source[index].Y != index / SimulationState.Width
                || !double.IsFinite(source[index].SurfaceWaterMm) || source[index].SurfaceWaterMm < 0
                || !double.IsFinite(source[index].GroundwaterMm) || source[index].GroundwaterMm < 0)
                throw new ArgumentException("Surface-water source stocks or cell identities are invalid.", nameof(source));
        }
        var grants = new List<SurfaceWaterGrantV1>(demands.Length);
        var updated = source.Select(cell => new SurfaceWaterStateV1(
            cell.Index, cell.SurfaceWaterMm, cell.GroundwaterMm)).ToArray();
        var consumerIds = new HashSet<ulong>();
        var ordered = demands.OrderBy(demand => demand.CellIndex).ThenBy(demand => demand.ConsumerId).ToArray();
        foreach (var demand in ordered)
        {
            if (demand.CellIndex < 0 || demand.CellIndex >= source.Length || demand.ConsumerId == 0
                || !double.IsFinite(demand.RequestedLiters) || demand.RequestedLiters < 0
                || !consumerIds.Add(demand.ConsumerId))
                throw new ArgumentException("Surface-water demands must have unique consumers and finite non-negative amounts.", nameof(demands));
        }

        foreach (var cellGroup in ordered.GroupBy(demand => demand.CellIndex))
        {
            var cellIndex = cellGroup.Key;
            if (source[cellIndex].Index != cellIndex)
                throw new ArgumentException("Surface-water source cells must use row-major index order.", nameof(source));
            var requestedL = cellGroup.Sum(demand => demand.RequestedLiters);
            if (!double.IsFinite(requestedL))
                throw new ArgumentException("Combined surface-water demand is not finite.", nameof(demands));
            var availableL = Math.Max(0, source[cellIndex].SurfaceWaterMm) * SimulationState.CellAreaM2;
            var scale = requestedL <= 0 ? 0 : Math.Min(1, availableL / requestedL);
            var grantedL = 0.0;
            foreach (var demand in cellGroup)
            {
                var amount = demand.RequestedLiters * scale;
                grantedL += amount;
                grants.Add(new(demand.ConsumerId, amount));
            }
            updated[cellIndex] = updated[cellIndex] with
            {
                SurfaceWaterMm = Math.Max(0, updated[cellIndex].SurfaceWaterMm - grantedL / SimulationState.CellAreaM2)
            };
        }
        return new(updated, grants.ToArray());
    }

    public static SurfaceWaterStateV1[] DepositAnimalWater(
        SurfaceWaterCellV1[] source,
        SurfaceWaterDepositV1[] deposits)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(deposits);
        if (source.Length != SimulationState.Width * SimulationState.Height)
            throw new ArgumentException("Surface-water deposits require a complete world field.", nameof(source));
        for (var index = 0; index < source.Length; index++)
        {
            if (source[index].Index != index || source[index].X != index % SimulationState.Width
                || source[index].Y != index / SimulationState.Width
                || !double.IsFinite(source[index].SurfaceWaterMm) || source[index].SurfaceWaterMm < 0
                || !double.IsFinite(source[index].GroundwaterMm) || source[index].GroundwaterMm < 0)
                throw new ArgumentException("Surface-water source stocks or cell identities are invalid.", nameof(source));
        }
        var updated = source.Select(cell => new SurfaceWaterStateV1(
            cell.Index, cell.SurfaceWaterMm, cell.GroundwaterMm)).ToArray();
        foreach (var group in deposits.OrderBy(deposit => deposit.CellIndex).GroupBy(deposit => deposit.CellIndex))
        {
            var cellIndex = group.Key;
            if (cellIndex < 0 || cellIndex >= source.Length || source[cellIndex].Index != cellIndex)
                throw new ArgumentException("Surface-water deposits refer to an invalid cell.", nameof(deposits));
            var totalLiters = group.Sum(deposit => deposit.Liters);
            if (!double.IsFinite(totalLiters) || totalLiters < 0)
                throw new ArgumentException("Surface-water deposits must be finite and non-negative.", nameof(deposits));
            var updatedSurfaceWaterMm = updated[cellIndex].SurfaceWaterMm + totalLiters / SimulationState.CellAreaM2;
            if (!double.IsFinite(updatedSurfaceWaterMm))
                throw new ArgumentException("Surface-water deposits exceed the supported numeric range.", nameof(deposits));
            updated[cellIndex] = updated[cellIndex] with
            {
                SurfaceWaterMm = updatedSurfaceWaterMm
            };
        }
        return updated;
    }

    private static DrainageNetwork BuildDrainage(SurfaceWaterCellV1[] terrain)
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
