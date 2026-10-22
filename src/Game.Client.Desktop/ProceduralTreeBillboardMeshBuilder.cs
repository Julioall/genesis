using Game.Procedural;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game.Client.Desktop;

internal readonly record struct TreeBillboardMeshData(int RenderProfileRow, VertexPositionTexture[] Vertices);

/// <summary>Builds camera-facing tree impostors for the optional hybrid renderer comparison.</summary>
internal static class ProceduralTreeBillboardMeshBuilder
{
    private static readonly float[] WidthByProfile = [1.02f, 0.82f, 0.90f, 1.05f, 1.18f];

    public static TreeBillboardMeshData[] Build(
        IReadOnlyList<ProceduralTreeInstance> trees,
        Vector3 cameraDirection,
        float pixelsPerWorldUnit,
        bool crossed = false)
    {
        ArgumentNullException.ThrowIfNull(trees);
        cameraDirection.Y = 0;
        if (cameraDirection.LengthSquared() < 0.0001f) cameraDirection = Vector3.UnitZ;
        cameraDirection.Normalize();
        var right = Vector3.Normalize(new Vector3(cameraDirection.Z, 0, -cameraDirection.X));
        var verticesByProfile = Enumerable.Range(0, WidthByProfile.Length)
            .Select(_ => new List<VertexPositionTexture>())
            .ToArray();

        foreach (var tree in trees)
        {
            var profileRow = Math.Clamp(tree.RenderProfileRow, 0, WidthByProfile.Length - 1);
            var detailScale = pixelsPerWorldUnit < 0.75f ? 0.92f : 1f;
            var variation = 0.91f + Unit(tree.Seed, 0x42494C4C5343414CUL, 0) * 0.18f;
            var cardHeight = tree.Height * 1.16f * variation * detailScale;
            var cardWidth = tree.Height * WidthByProfile[profileRow] * variation * detailScale;
            var bottom = tree.Ground - Vector3.Up * cardHeight * 0.04f;
            var top = bottom + Vector3.Up * cardHeight;
            var mirror = (Unit(tree.Seed, 0x42494C4C4D495252UL, 0) >= 0.5f);
            var leftU = mirror ? 1f : 0f;
            var rightU = mirror ? 0f : 1f;
            var vertices = verticesByProfile[profileRow];
            var planeCount = crossed ? 3 : 1;
            for (var plane = 0; plane < planeCount; plane++)
            {
                // The first plane faces the camera; two more at 60-degree intervals keep a
                // broad crown silhouette as the camera looks across the canopy from above.
                var planeRight = crossed
                    ? Vector3.Transform(right, Matrix.CreateRotationY(MathHelper.TwoPi * plane / 3))
                    : right;
                var halfWidth = planeRight * (cardWidth * 0.5f);
                var bottomLeft = bottom - halfWidth;
                var bottomRight = bottom + halfWidth;
                var topLeft = top - halfWidth;
                var topRight = top + halfWidth;
                var planeLeftU = plane > 0 && (plane & 1) == 1 ? rightU : leftU;
                var planeRightU = plane > 0 && (plane & 1) == 1 ? leftU : rightU;
                vertices.Add(new VertexPositionTexture(bottomLeft, new Vector2(planeLeftU, 1f)));
                vertices.Add(new VertexPositionTexture(topLeft, new Vector2(planeLeftU, 0f)));
                vertices.Add(new VertexPositionTexture(bottomRight, new Vector2(planeRightU, 1f)));
                vertices.Add(new VertexPositionTexture(bottomRight, new Vector2(planeRightU, 1f)));
                vertices.Add(new VertexPositionTexture(topLeft, new Vector2(planeLeftU, 0f)));
                vertices.Add(new VertexPositionTexture(topRight, new Vector2(planeRightU, 0f)));
            }
        }

        return verticesByProfile
            .Select((vertices, profileRow) => new TreeBillboardMeshData(profileRow, vertices.ToArray()))
            .Where(batch => batch.Vertices.Length > 0)
            .ToArray();
    }

    private static float Unit(ulong seed, ulong salt, ulong index)
        => (float)CounterRandom.Unit(seed, salt, index, 0, 0, 0);
}
