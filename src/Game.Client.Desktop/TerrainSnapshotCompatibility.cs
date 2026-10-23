using Game.Contracts;
using Game.Modules.Geosphere;

namespace Game.Client.Desktop;

public static class TerrainSnapshotCompatibility
{
    public static void Validate(WorldSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var expected = TerrainModel.GenerateElevationField(snapshot.Seed);
        if (snapshot.Cells.Count != expected.Length)
            throw new InvalidDataException("The saved terrain grid does not match the current terrain generator.");

        for (var index = 0; index < expected.Length; index++)
        {
            if (Math.Abs(snapshot.Cells[index].ElevationM - expected[index]) > 1e-8)
                throw new InvalidDataException(
                    $"The saved terrain at cell {index} does not match the current terrain generator. "
                    + "Open this save with the version that created it or migrate it before visual evaluation.");
        }
    }
}
