using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game.Client.Desktop;

/// <summary>Builds soft, terrain-conforming render shadows for the visual evaluation scene.</summary>
internal static class ProceduralTreeShadowMeshBuilder
{
    public static VertexPositionColor[] Build(
        IReadOnlyList<ProceduralTreeInstance> trees,
        IReadOnlyList<ProceduralRockInstance> rocks,
        Func<double, double, double> elevationSampler,
        float cellSizeM)
    {
        var vertices = new List<VertexPositionColor>(trees.Count * 72 + rocks.Count * 42);
        var awayFromLight = Vector2.Normalize(new Vector2(0.38f, -0.43f));
        var across = new Vector2(-awayFromLight.Y, awayFromLight.X);
        const int segments = 12;

        foreach (var tree in trees)
        {
            var length = Math.Max(2.0f, tree.Height * 0.40f);
            var width = Math.Max(1.1f, tree.Height * 0.18f);
            var displaced = new Vector2(tree.Ground.X, tree.Ground.Z)
                + awayFromLight * (tree.Height * 0.32f);
            var center = new Vector3(displaced.X,
                (float)elevationSampler(displaced.X / cellSizeM, displaced.Y / cellSizeM) + 0.055f,
                displaced.Y);
            var inner = new Color(19, 29, 25, 56);
            var outer = new Color(19, 29, 25, 0);

            for (var segment = 0; segment < segments; segment++)
            {
                var firstAngle = MathF.Tau * segment / segments;
                var secondAngle = MathF.Tau * (segment + 1) / segments;
                var first = ShadowPoint(firstAngle);
                var second = ShadowPoint(secondAngle);
                vertices.Add(new VertexPositionColor(center, inner));
                vertices.Add(new VertexPositionColor(first, outer));
                vertices.Add(new VertexPositionColor(second, outer));

                Vector3 ShadowPoint(float angle)
                {
                    var offset = awayFromLight * (MathF.Cos(angle) * length)
                        + across * (MathF.Sin(angle) * width);
                    var position = displaced + offset;
                    var height = elevationSampler(position.X / cellSizeM, position.Y / cellSizeM) + 0.055f;
                    return new Vector3(position.X, (float)height, position.Y);
                }
            }
        }

        foreach (var rock in rocks)
        {
            var displaced = new Vector2(rock.Ground.X, rock.Ground.Z)
                + awayFromLight * (rock.HeightM * 0.22f);
            var length = rock.WidthM * 0.54f + rock.HeightM * 0.28f;
            var width = rock.WidthM * 0.34f;
            var center = GroundPoint(displaced);
            var inner = new Color(22, 27, 23, 72);
            var outer = new Color(22, 27, 23, 0);

            for (var segment = 0; segment < 14; segment++)
            {
                var firstAngle = MathF.Tau * segment / 14;
                var secondAngle = MathF.Tau * (segment + 1) / 14;
                var first = RockShadowPoint(firstAngle);
                var second = RockShadowPoint(secondAngle);
                vertices.Add(new VertexPositionColor(center, inner));
                vertices.Add(new VertexPositionColor(first, outer));
                vertices.Add(new VertexPositionColor(second, outer));

                Vector3 RockShadowPoint(float angle)
                {
                    var offset = awayFromLight * (MathF.Cos(angle) * length)
                        + across * (MathF.Sin(angle) * width);
                    return GroundPoint(displaced + offset);
                }
            }
        }

        return vertices.ToArray();

        Vector3 GroundPoint(Vector2 point)
            => new(point.X, (float)elevationSampler(point.X / cellSizeM, point.Y / cellSizeM) + 0.07f, point.Y);
    }
}
