namespace Game.Procedural;

public static class CounterRandom
{
    public const ulong TerrainTag = 1;
    public const ulong PlantPlacementTag = 2;
    public const ulong ClimateTag = 3;
    public const ulong AnimalPlacementTag = 4;
    public const ulong PredationTag = 5;
    public const ulong ReproductionTag = 6;

    public static ulong Mix64(ulong value)
    {
        unchecked
        {
            var z = value + 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    public static ulong Bits(ulong seed, ulong tag, ulong subjectId, ulong targetId, ulong tickOrHour, ulong drawIndex)
    {
        var a = Mix64(seed ^ tag);
        var b = Mix64(a ^ subjectId);
        var c = Mix64(b ^ targetId);
        var d = Mix64(c ^ tickOrHour);
        return Mix64(d ^ drawIndex);
    }

    public static double Unit(ulong seed, ulong tag, ulong subjectId, ulong targetId, ulong tickOrHour, ulong drawIndex)
        => (Bits(seed, tag, subjectId, targetId, tickOrHour, drawIndex) >> 11) * (1.0 / 9007199254740992.0);

    public static double SpatialUnit(ulong seed, ulong tag, int x, int y, int spacing, ulong profileId, ulong scaleId)
    {
        if (spacing <= 0) throw new ArgumentOutOfRangeException(nameof(spacing));
        var x0 = Math.DivRem(x, spacing, out var remainderX);
        var y0 = Math.DivRem(y, spacing, out var remainderY);
        var tx = Smooth(remainderX / (double)spacing);
        var ty = Smooth(remainderY / (double)spacing);
        var northwest = Unit(seed, tag, (ulong)x0, (ulong)y0, profileId, scaleId);
        var northeast = Unit(seed, tag, (ulong)(x0 + 1), (ulong)y0, profileId, scaleId);
        var southwest = Unit(seed, tag, (ulong)x0, (ulong)(y0 + 1), profileId, scaleId);
        var southeast = Unit(seed, tag, (ulong)(x0 + 1), (ulong)(y0 + 1), profileId, scaleId);
        var north = northwest + (northeast - northwest) * tx;
        var south = southwest + (southeast - southwest) * tx;
        return north + (south - north) * ty;
    }

    // Visual choices have their own salt and must never consume or advance an ecology stream.
    public static ulong VisualBits(ulong seed, int cellId, ulong recipeId, ulong version = 1)
        => Bits(seed ^ 0xD1B54A32D192ED03UL, recipeId, unchecked((ulong)(uint)cellId), 0, version, 0);

    private static double Smooth(double value) => value * value * (3 - 2 * value);
}
