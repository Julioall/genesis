using Game.Contracts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game.Client.Desktop;

/// <summary>Converts renderer-neutral creature triangles to the current MonoGame vertex format.</summary>
internal static class CreatureGeometryMonoGameAdapter
{
    private static readonly CreatureVector3V1 LightDirection = Normalize(new(0.35, 0.58, 0.74));

    public static VertexPositionColor[] Expand(CreatureGeometryV1 geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if (geometry.Indices.Count % 3 != 0)
            throw new ArgumentException("Creature geometry indices must form a triangle list.", nameof(geometry));

        var vertices = new VertexPositionColor[geometry.Indices.Count];
        for (var index = 0; index < geometry.Indices.Count; index++)
        {
            var vertexIndex = geometry.Indices[index];
            if (vertexIndex < 0 || vertexIndex >= geometry.Vertices.Count)
                throw new ArgumentException("Creature geometry contains an out-of-range vertex index.", nameof(geometry));
            var source = geometry.Vertices[vertexIndex];
            var normal = Normalize(source.Normal);
            var diffuse = Math.Clamp(0.52 + 0.48 * Math.Max(0, Dot(normal, LightDirection)), 0.42, 1.0);
            var packed = source.Rgba;
            var color = new Color(
                Shade((byte)(packed >> 24), diffuse),
                Shade((byte)(packed >> 16), diffuse),
                Shade((byte)(packed >> 8), diffuse),
                (byte)packed);
            vertices[index] = new(new Vector3((float)source.PositionM.X,
                (float)source.PositionM.Y, (float)source.PositionM.Z), color);
        }
        return vertices;
    }

    private static byte Shade(byte channel, double amount)
        => (byte)Math.Clamp((int)Math.Round(channel * amount), 0, 255);

    private static double Dot(CreatureVector3V1 first, CreatureVector3V1 second)
        => first.X * second.X + first.Y * second.Y + first.Z * second.Z;

    private static CreatureVector3V1 Normalize(CreatureVector3V1 value)
    {
        var length = Math.Sqrt(value.X * value.X + value.Y * value.Y + value.Z * value.Z);
        return length > 1e-12 ? new(value.X / length, value.Y / length, value.Z / length) : new(0, 1, 0);
    }
}
