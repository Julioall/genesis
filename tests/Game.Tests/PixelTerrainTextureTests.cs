using Game.Client.Desktop;
using Game.Contracts;
using Game.Core;

namespace Game.Tests;

public sealed class PixelTerrainTextureTests
{
    private static readonly (int X, int Y)[] Neighbors =
    [
        (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1)
    ];

    [Fact]
    public void Ground_texture_is_repeatable_and_contains_procedural_surface_variation()
    {
        var snapshot = WorldSimulation.Create(1).Snapshot();
        var first = PixelTerrainTexture.Generate(snapshot, pixelsPerCell: 4, overlayMode: 0, visualSeason: Season.Summer);
        var repeat = PixelTerrainTexture.Generate(snapshot, pixelsPerCell: 4, overlayMode: 0, visualSeason: Season.Summer);

        Assert.Equal(first, repeat);
        Assert.True(first.Distinct().Count() > 16, "The procedural terrain surface should contain multiple material and lighting colors.");
    }

    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    public void Grass_autotile_is_seeded_connected_and_stays_inside_living_cells(int detail)
    {
        var snapshot = WorldSimulation.Create(1).Snapshot();
        var field = PixelTerrainTexture.BuildGrassAutotile(snapshot, detail);
        var repeat = PixelTerrainTexture.BuildGrassAutotile(snapshot, detail);

        Assert.Equal(field.States, repeat.States);
        Assert.Equal(field.NeighborMasks, repeat.NeighborMasks);
        Assert.Contains((byte)1, field.States);

        var occupied = 0;
        for (var y = 0; y < field.Height; y++)
        for (var x = 0; x < field.Width; x++)
        {
            var index = y * field.Width + x;
            if (field.States[index] == 0)
            {
                Assert.Equal(0, field.NeighborMasks[index]);
                continue;
            }

            occupied++;
            for (var bit = 0; bit < Neighbors.Length; bit++)
            {
                if ((field.NeighborMasks[index] & (1 << bit)) == 0) continue;
                var (dx, dy) = Neighbors[bit];
                var neighborX = x + dx;
                var neighborY = y + dy;
                Assert.InRange(neighborX, 0, field.Width - 1);
                Assert.InRange(neighborY, 0, field.Height - 1);
                Assert.Equal((byte)1, field.States[neighborY * field.Width + neighborX]);
            }
        }

        for (var cellY = 0; cellY < snapshot.Height; cellY++)
        for (var cellX = 0; cellX < snapshot.Width; cellX++)
        {
            var cell = snapshot.Cells[cellY * snapshot.Width + cellX];
            if (cell.Grass.Present && cell.Grass.BiomassGPerM2 > 1) continue;
            for (var offsetY = 0; offsetY < field.Detail; offsetY++)
            for (var offsetX = 0; offsetX < field.Detail; offsetX++)
            {
                var x = cellX * field.Detail + offsetX;
                var y = cellY * field.Detail + offsetY;
                Assert.Equal(0, field.States[y * field.Width + x]);
            }
        }

        Assert.True(occupied > 0);
    }
}
