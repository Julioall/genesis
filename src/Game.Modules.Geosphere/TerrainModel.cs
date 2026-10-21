using Game.Contracts;
using Game.Procedural;

namespace Game.Modules.Geosphere;

public static class TerrainModel
{
    public static double Elevation(int x, int y, ulong seed)
    {
        var center = 31.5;
        var cell = (ulong)(y * SimulationState.Width + x);
        var perturbation = 4 * CounterRandom.Unit(seed, CounterRandom.TerrainTag, cell, 0, 0, 0) - 2;
        return 25 + 0.06 * (Math.Pow(x - center, 2) + Math.Pow(y - center, 2)) + perturbation;
    }

    public static (double East, double North) Gradient(WorldCell[] cells, int x, int y)
    {
        double At(int px, int py) => cells[py * SimulationState.Width + px].ElevationM;
        var east = x == 0 ? At(1, y) - At(0, y)
            : x == SimulationState.Width - 1 ? At(x, y) - At(x - 1, y)
            : (At(x + 1, y) - At(x - 1, y)) / 2;
        var north = y == 0 ? At(x, 1) - At(x, 0)
            : y == SimulationState.Height - 1 ? At(x, y) - At(x, y - 1)
            : (At(x, y + 1) - At(x, y - 1)) / 2;
        return (east, north);
    }

    public static double TerrainSolarFactor(WorldCell[] cells, WorldCell cell, double sunEast, double sunNorth, double sunUp)
    {
        var gradient = Gradient(cells, cell.X, cell.Y);
        var length = Math.Sqrt(gradient.East * gradient.East + gradient.North * gradient.North + 1);
        var normalEast = -gradient.East / length;
        var normalNorth = -gradient.North / length;
        var normalUp = 1 / length;
        if (sunUp <= 0) return 0;
        return Math.Clamp(Math.Max(0, normalEast * sunEast + normalNorth * sunNorth + normalUp * sunUp)
                          / Math.Max(sunUp, 0.05), 0, 1.2);
    }
}
