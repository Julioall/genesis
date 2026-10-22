using Game.Client.Desktop;
using Game.Contracts;
using Microsoft.Xna.Framework;

namespace Game.Tests;

public sealed class ProceduralTreeMeshTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public void Branch_mesh_with_junctions_is_deterministic_and_finite(int profileRow)
    {
        var tree = new ProceduralTreeInstance(
            new Vector3(3f, 0f, -2f), 9f, profileRow, 1f, 0xB12A4C4FUL, Season.Summer);

        var first = ProceduralFloraMeshBuilder.Build(
            [tree], 44f, perspectiveProjection: false, new Vector3(10f, 20f, 15f), branchesOnly: true);
        var second = ProceduralFloraMeshBuilder.Build(
            [tree], 44f, perspectiveProjection: false, new Vector3(10f, 20f, 15f), branchesOnly: true);

        Assert.NotEmpty(first);
        Assert.Equal(first.Length, second.Length);
        for (var index = 0; index < first.Length; index++)
        {
            Assert.Equal(first[index].Position, second[index].Position);
            Assert.Equal(first[index].Color, second[index].Color);
            Assert.Equal(first[index].TextureCoordinate, second[index].TextureCoordinate);
            Assert.True(float.IsFinite(first[index].Position.X));
            Assert.True(float.IsFinite(first[index].Position.Y));
            Assert.True(float.IsFinite(first[index].Position.Z));
        }
    }
}
