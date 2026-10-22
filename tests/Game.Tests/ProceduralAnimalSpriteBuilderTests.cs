using Game.Client.Desktop;
using Game.Contracts;

namespace Game.Tests;

public sealed class ProceduralAnimalSpriteBuilderTests
{
    [Theory]
    [InlineData(AnimalFunctionalProfileV1.Herbivore, 0)]
    [InlineData(AnimalFunctionalProfileV1.Herbivore, 3)]
    [InlineData(AnimalFunctionalProfileV1.Predator, 1)]
    [InlineData(AnimalFunctionalProfileV1.Predator, 2)]
    public void Pixel_silhouettes_are_seed_variant_stable_and_transparent_around_the_creature(
        AnimalFunctionalProfileV1 profile, int variation)
    {
        var first = ProceduralAnimalSpriteBuilder.Build(profile, variation);
        var repeated = ProceduralAnimalSpriteBuilder.Build(profile, variation);

        Assert.Equal(first, repeated);
        Assert.Equal(ProceduralAnimalSpriteBuilder.Width * ProceduralAnimalSpriteBuilder.Height, first.Length);
        Assert.Contains(first, color => color.A == 0);
        Assert.Contains(first, color => color.A == 255);
    }

    [Fact]
    public void Functional_profiles_have_distinguishable_render_only_shapes()
    {
        var herbivore = ProceduralAnimalSpriteBuilder.Build(AnimalFunctionalProfileV1.Herbivore, 0);
        var predator = ProceduralAnimalSpriteBuilder.Build(AnimalFunctionalProfileV1.Predator, 0);

        Assert.NotEqual(herbivore, predator);
    }

    [Fact]
    public void Layout_hash_is_order_independent_and_changes_with_seed_or_position()
    {
        var herbivore = new AnimalAgentV1(1, AnimalFunctionalProfileV1.Herbivore, 120, 240,
            3, 5000, 1000, 1, 0, 0, 0, -1);
        var predator = new AnimalAgentV1(2, AnimalFunctionalProfileV1.Predator, 130, 250,
            3, 12000, 2000, 2, 0, 0, 0, -1);

        var forward = ProceduralAnimalSpriteBuilder.LayoutHash(7, [herbivore, predator]);
        var reversed = ProceduralAnimalSpriteBuilder.LayoutHash(7, [predator, herbivore]);
        var otherSeed = ProceduralAnimalSpriteBuilder.LayoutHash(8, [herbivore, predator]);
        var moved = ProceduralAnimalSpriteBuilder.LayoutHash(7, [herbivore, predator with { PositionXM = 131 }]);

        Assert.Equal(forward, reversed);
        Assert.NotEqual(forward, otherSeed);
        Assert.NotEqual(forward, moved);
    }
}
