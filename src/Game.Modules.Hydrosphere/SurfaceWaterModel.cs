using Game.Contracts;

namespace Game.Modules.Hydrosphere;

public readonly record struct SurfaceFlowResult(WorldCell[] Cells, double OutflowLiters);

public static class SurfaceWaterModel
{
    private const double Diffusion = 0.02;
    private static readonly (int X, int Y)[] Neighbors =
    [
        (-1,-1),(0,-1),(1,-1),(-1,0),(1,0),(-1,1),(0,1),(1,1)
    ];

    public static SurfaceFlowResult Route(WorldCell[] source)
    {
        var proposed = new List<(int From, int To, double Mm)>();
        var outgoing = new double[source.Length];
        for (var i = 0; i < source.Length; i++)
        {
            var cell = source[i];
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
                var other = source[target];
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

        var delta = new double[source.Length];
        var outflowLiters = 0.0;
        foreach (var proposal in proposed)
        {
            var available = source[proposal.From].SurfaceWaterMm;
            var scale = outgoing[proposal.From] > available && outgoing[proposal.From] > 0
                ? available / outgoing[proposal.From]
                : 1;
            var flow = proposal.Mm * scale;
            delta[proposal.From] -= flow;
            if (proposal.To < 0) outflowLiters += flow * SimulationState.CellAreaM2;
            else delta[proposal.To] += flow;
        }

        var result = (WorldCell[])source.Clone();
        for (var i = 0; i < result.Length; i++)
            result[i] = result[i] with { SurfaceWaterMm = Math.Max(0, result[i].SurfaceWaterMm + delta[i]) };
        return new(result, outflowLiters);
    }
}
