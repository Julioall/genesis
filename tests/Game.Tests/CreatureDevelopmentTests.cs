using Game.Contracts;
using Game.Modules.Biosphere;
using Game.Procedural;

namespace Game.Tests;

public sealed class CreatureDevelopmentTests
{
    private readonly CreatureDevelopmentModelV1 _model = new();

    [Fact]
    public void One_thousand_seeded_genomes_per_family_develop_into_valid_immutable_bodies()
    {
        foreach (var species in CreatureSpeciesCatalogV1.All)
        {
            var structuralSignatures = new HashSet<ulong>();
        for (ulong seed = 1; seed <= 1_000; seed++)
        {
            var pigmentSeed = CounterRandom.Bits(seed, 0x5A315049474D454EUL,
                (ulong)species.Family, 0, 1, 0);
            var genome = _model.CreateGenome(species, seed, pigmentSeed);
            var result = _model.Develop(species, genome,
                new(species.MaturityDays * 2, 1, 1));
            var repeated = _model.Develop(species, genome,
                new(species.MaturityDays * 2, 1, 1));

            Assert.Empty(CreatureDevelopmentModelV1.ValidateDevelopedBody(species, genome, result.Body));
            structuralSignatures.Add(result.Body.StructuralSignature);
            Assert.Equal(genome, _model.CreateGenome(species, seed, pigmentSeed));
            Assert.Equal(result.Body.StructuralSignature, repeated.Body.StructuralSignature);
            Assert.Equal(result.VisualRecipe, repeated.VisualRecipe);
            Assert.InRange(result.Body.DevelopmentProgress01, 0.999999, 1.0);
            Assert.Equal(species.Family, result.Body.Family);
            Assert.Equal(species.BodyPlanId, result.Body.BodyPlanId);
            Assert.Equal(result.Body.ReferenceStructuralMassKg,
                result.Body.Parts.Sum(part => part.StructuralMassKg), 10);

            if (species.Family == CreatureFamilyV1.Quadruped)
            {
                Assert.Equal(4, result.Body.Parts.Count(part => part.Kind == AnatomicalPartKindV1.LimbProximal));
                Assert.Equal(4, result.Body.Parts.Count(part => part.Kind == AnatomicalPartKindV1.LimbDistal));
                Assert.Equal(0, result.Body.Morphology.PropulsionSurfaceAreaProxyM2);
                Assert.True(result.Body.Morphology.NominalStrideLengthM > 0);
            }
            else
            {
                Assert.True(result.Body.Parts.Count(part => part.Kind == AnatomicalPartKindV1.Fin) >= 5);
                Assert.Equal(0, result.Body.Morphology.NominalStrideLengthM);
                Assert.True(result.Body.Morphology.PropulsionSurfaceAreaProxyM2 > 0);
            }
        }
            Assert.True(structuralSignatures.Count >= 995,
                $"Species '{species.SpeciesId}' generated only {structuralSignatures.Count} distinct structures from 1,000 seeds.");
        }
    }

    [Fact]
    public void Pigment_seed_changes_visual_recipe_without_changing_anatomy_or_morphology()
    {
        foreach (var species in CreatureSpeciesCatalogV1.All)
        {
            const ulong anatomySeed = 0xA11CE;
            var firstGenome = _model.CreateGenome(species, anatomySeed, 0x1111);
            var secondGenome = _model.CreateGenome(species, anatomySeed, 0x2222);
            var context = new CreatureDevelopmentContextV1(species.MaturityDays, 0.8, 0.9);

            var first = _model.Develop(species, firstGenome, context);
            var second = _model.Develop(species, secondGenome, context);

            Assert.Equal(first.Body.StructuralSignature, second.Body.StructuralSignature);
            Assert.Equal(first.Body.Morphology, second.Body.Morphology);
            Assert.Equal(first.Body.Parts, second.Body.Parts);
            Assert.Equal(first.Body.Joints, second.Body.Joints);
            Assert.NotEqual(first.VisualRecipe, second.VisualRecipe);
            Assert.Equal(first.Body.StructuralSignature, first.VisualRecipe.StructuralSignature);
            Assert.Equal(second.Body.StructuralSignature, second.VisualRecipe.StructuralSignature);
        }
    }

    [Fact]
    public void Moderate_mutation_is_deterministic_bounded_and_keeps_the_species_family()
    {
        foreach (var species in CreatureSpeciesCatalogV1.All)
        {
            var parent = _model.CreateGenome(species, 917, 1291);
            var child = _model.MutateGenome(species, parent, 44_221);
            var repeated = _model.MutateGenome(species, parent, 44_221);

            Assert.Equal(child, repeated);
            Assert.NotEqual(parent.GenomeId, child.GenomeId);
            Assert.Equal(parent.GenomeId, child.ParentAId);
            Assert.Null(child.ParentBId);
            Assert.Equal(species.SpeciesId, child.SpeciesId);
            Assert.Equal(species.GeneticsSchemaVersion, child.SchemaVersion);
            Assert.Equal(CreatureDevelopmentModelV1.GeneratorVersion, child.GeneratorVersion);
            AssertMutationWithin(species.GeneBounds.BodyScale, parent.Genes.BodyScale, child.Genes.BodyScale);
            AssertMutationWithin(species.GeneBounds.AppendageRelativeLength,
                parent.Genes.AppendageRelativeLength, child.Genes.AppendageRelativeLength);
            AssertMutationWithin(species.GeneBounds.TrunkRelativeLength,
                parent.Genes.TrunkRelativeLength, child.Genes.TrunkRelativeLength);
            AssertMutationWithin(species.GeneBounds.ThermalInsulationIndex,
                parent.Genes.ThermalInsulationIndex, child.Genes.ThermalInsulationIndex);
            AssertMutationWithin(species.GeneBounds.PigmentHueDegrees,
                parent.Genes.PigmentHueDegrees, child.Genes.PigmentHueDegrees);
            AssertMutationWithin(species.GeneBounds.PatternFrequencyPerM,
                parent.Genes.PatternFrequencyPerM, child.Genes.PatternFrequencyPerM);
            AssertMutationWithin(species.GeneBounds.GrowthRatePerDay,
                parent.Genes.GrowthRatePerDay, child.Genes.GrowthRatePerDay);
            Assert.NotEqual(parent.Genes, child.Genes);
            var parentBody = _model.Develop(species, parent, new(species.MaturityDays, 1, 1)).Body;
            var childBody = _model.Develop(species, child, new(species.MaturityDays, 1, 1)).Body;
            Assert.NotEqual(parentBody.StructuralSignature, childBody.StructuralSignature);
            Assert.Empty(CreatureDevelopmentModelV1.ValidateDevelopedBody(species, child, childBody));
            Assert.Equal(parent, _model.CreateGenome(species, parent.AnatomySeed, parent.PigmentSeed));
        }
    }

    [Fact]
    public void Mutation_operator_has_bounded_steps_and_a_centered_seed_distribution()
    {
        const int SampleCount = 1024;
        foreach (var species in CreatureSpeciesCatalogV1.All)
        {
            var parent = Enumerable.Range(1, 512)
                .Select(seed => _model.CreateGenome(species, (ulong)seed, (ulong)(seed * 17)))
                .First(genome => HasMutationMargin(species.GeneBounds.BodyScale, genome.Genes.BodyScale)
                    && HasMutationMargin(species.GeneBounds.AppendageRelativeLength, genome.Genes.AppendageRelativeLength)
                    && HasMutationMargin(species.GeneBounds.TrunkRelativeLength, genome.Genes.TrunkRelativeLength)
                    && HasMutationMargin(species.GeneBounds.ThermalInsulationIndex, genome.Genes.ThermalInsulationIndex)
                    && HasMutationMargin(species.GeneBounds.PigmentHueDegrees, genome.Genes.PigmentHueDegrees)
                    && HasMutationMargin(species.GeneBounds.PatternFrequencyPerM, genome.Genes.PatternFrequencyPerM)
                    && HasMutationMargin(species.GeneBounds.GrowthRatePerDay, genome.Genes.GrowthRatePerDay));

            (GeneRangeV1 Range, Func<CreatureGeneValuesV1, double> Read)[] traits =
            [
                (species.GeneBounds.BodyScale, genes => genes.BodyScale),
                (species.GeneBounds.AppendageRelativeLength, genes => genes.AppendageRelativeLength),
                (species.GeneBounds.TrunkRelativeLength, genes => genes.TrunkRelativeLength),
                (species.GeneBounds.ThermalInsulationIndex, genes => genes.ThermalInsulationIndex),
                (species.GeneBounds.PigmentHueDegrees, genes => genes.PigmentHueDegrees),
                (species.GeneBounds.PatternFrequencyPerM, genes => genes.PatternFrequencyPerM),
                (species.GeneBounds.GrowthRatePerDay, genes => genes.GrowthRatePerDay)
            ];
            var deltas = traits.Select(_ => new List<double>(SampleCount)).ToArray();

            for (var sample = 0; sample < SampleCount; sample++)
            {
                var child = _model.MutateGenome(species, parent, (ulong)(sample + 1));
                for (var traitIndex = 0; traitIndex < traits.Length; traitIndex++)
                {
                    var trait = traits[traitIndex];
                    var before = trait.Read(parent.Genes);
                    var after = trait.Read(child.Genes);
                    AssertMutationWithin(trait.Range, before, after);
                    deltas[traitIndex].Add(after - before);
                }
            }

            for (var traitIndex = 0; traitIndex < traits.Length; traitIndex++)
            {
                var range = traits[traitIndex].Range;
                var rangeWidth = range.Maximum - range.Minimum;
                var meanDelta = deltas[traitIndex].Average();
                Assert.InRange(meanDelta, -rangeWidth * 0.005, rangeWidth * 0.005);
                if (rangeWidth > 0)
                {
                    Assert.Contains(deltas[traitIndex], delta => delta < 0);
                    Assert.Contains(deltas[traitIndex], delta => delta > 0);
                }
            }
        }
    }

    [Fact]
    public void Multi_generation_mutation_preserves_a_deterministic_single_parent_pedigree()
    {
        var species = CreatureSpeciesCatalogV1.Quadruped;
        var founder = _model.CreateGenome(species, 7001, 8001);

        CreatureGenomeV1[] BuildLineage()
        {
            var lineage = new CreatureGenomeV1[32];
            var parent = founder;
            for (var generation = 0; generation < lineage.Length; generation++)
            {
                var child = _model.MutateGenome(species, parent, (ulong)(90_000 + generation));
                Assert.Equal(parent.GenomeId, child.ParentAId);
                Assert.Null(child.ParentBId);
                Assert.NotEqual(parent.GenomeId, child.GenomeId);
                Assert.Equal(child, CreatureDevelopmentModelV1.RestoreGenome(
                    CreatureDevelopmentModelV1.ExportGenome(species, child), species));
                lineage[generation] = child;
                parent = child;
            }
            return lineage;
        }

        Assert.Equal(BuildLineage(), BuildLineage());
    }

    [Fact]
    public void Species_and_genomes_export_and_restore_without_external_assets()
    {
        foreach (var species in CreatureSpeciesCatalogV1.All)
        {
            var restoredSpecies = CreatureSpeciesCatalogV1.Restore(CreatureSpeciesCatalogV1.Export(species));
            var genome = _model.CreateGenome(species, 123, 456);
            var restoredGenome = CreatureDevelopmentModelV1.RestoreGenome(
                CreatureDevelopmentModelV1.ExportGenome(species, genome), restoredSpecies);

            Assert.Equal(species, restoredSpecies);
            Assert.Equal(genome, restoredGenome);
            Assert.Equal(species.BodyPlanId,
                _model.Develop(restoredSpecies, restoredGenome,
                    new(species.MaturityDays, 1, 1)).Body.BodyPlanId);
        }
    }

    [Fact]
    public void World_seed_derives_stable_independent_streams_per_individual()
    {
        foreach (var species in CreatureSpeciesCatalogV1.All)
        {
            var first = _model.CreateGenomeFromWorldSeed(species, 9001, 17);
            var repeated = _model.CreateGenomeFromWorldSeed(species, 9001, 17);
            var anotherIndividual = _model.CreateGenomeFromWorldSeed(species, 9001, 18);

            Assert.Equal(first, repeated);
            Assert.NotEqual(first.GenomeId, anotherIndividual.GenomeId);
            Assert.NotEqual(first.AnatomySeed, first.PigmentSeed);
            Assert.NotEqual(first.AnatomySeed, anotherIndividual.AnatomySeed);
            Assert.NotEqual(first.PigmentSeed, anotherIndividual.PigmentSeed);
        }
    }

    [Fact]
    public void Age_thermal_history_and_nutrition_change_development_not_inherited_genes()
    {
        var species = CreatureSpeciesCatalogV1.Quadruped;
        var genome = _model.CreateGenome(species, 71, 91);
        var juvenile = _model.Develop(species, genome, new(0, 0.5, 0.2));
        var mature = _model.Develop(species, genome, new(species.MaturityDays * 2, 1, 1));

        Assert.True(mature.Body.BodyLengthM > juvenile.Body.BodyLengthM);
        Assert.True(mature.Body.ReferenceStructuralMassKg > juvenile.Body.ReferenceStructuralMassKg);
        Assert.Equal(genome, _model.CreateGenome(species, 71, 91));
        Assert.NotEqual(juvenile.Body.StructuralSignature, mature.Body.StructuralSignature);
    }

    [Fact]
    public void Reproductive_compatibility_never_crosses_family_or_body_plan()
    {
        var quadruped = CreatureSpeciesCatalogV1.Quadruped;
        var fish = CreatureSpeciesCatalogV1.FreshwaterFish;

        Assert.True(CreatureDevelopmentModelV1.CanReproduceWith(quadruped, quadruped));
        Assert.True(CreatureDevelopmentModelV1.CanReproduceWith(fish, fish));
        Assert.False(CreatureDevelopmentModelV1.CanReproduceWith(quadruped, fish));
    }

    [Fact]
    public void Invalid_genes_and_development_contexts_are_rejected()
    {
        var species = CreatureSpeciesCatalogV1.Quadruped;
        var genome = _model.CreateGenome(species, 4, 7);
        var invalidGenome = genome with
        {
            Genes = genome.Genes with { BodyScale = species.GeneBounds.BodyScale.Maximum + 1 }
        };

        Assert.Throws<ArgumentException>(() => _model.Develop(species, invalidGenome,
            new(species.MaturityDays, 1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => _model.Develop(species, genome,
            new(-1, 1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => _model.Develop(species, genome,
            new(0, double.NaN, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => _model.Develop(species, genome,
            new(0, 1, 1.01)));
    }

    [Fact]
    public void Developed_anatomy_is_immutable_and_not_a_world_biomass_transfer()
    {
        var species = CreatureSpeciesCatalogV1.Quadruped;
        var genome = _model.CreateGenome(species, 31, 41);
        var result = _model.Develop(species, genome, new(species.MaturityDays, 1, 1));
        var bodyPartCount = result.Body.Parts.Count;

        Assert.IsAssignableFrom<IReadOnlyList<AnatomicalPartV1>>(result.Body.Parts);
        Assert.Throws<NotSupportedException>(() => ((IList<AnatomicalPartV1>)result.Body.Parts)
            .Add(result.Body.Parts[0]));
        Assert.Equal(bodyPartCount, result.Body.Parts.Count);
        Assert.True(double.IsFinite(result.Body.ReferenceStructuralMassKg));
        foreach (var method in typeof(ICreatureDevelopmentV1).GetMethods())
        foreach (var parameter in method.GetParameters())
        {
            Assert.NotEqual(typeof(AnimalAgentV1), parameter.ParameterType);
            Assert.NotEqual(typeof(WorldCell), parameter.ParameterType);
            Assert.DoesNotContain("SimulationState", parameter.ParameterType.Name, StringComparison.Ordinal);
        }
    }

    private static void AssertMutationWithin(GeneRangeV1 range, double parent, double child)
    {
        var maximumDelta = (range.Maximum - range.Minimum)
            * CreatureDevelopmentModelV1.MaximumMutationFractionOfRange;
        Assert.InRange(child, range.Minimum, range.Maximum);
        Assert.True(Math.Abs(child - parent) <= maximumDelta + 1e-12,
            $"Mutation {parent:R} -> {child:R} exceeded the maximum step {maximumDelta:R}.");
    }

    private static bool HasMutationMargin(GeneRangeV1 range, double value)
    {
        if (range.Minimum == range.Maximum) return true;
        var margin = (range.Maximum - range.Minimum) * CreatureDevelopmentModelV1.MaximumMutationFractionOfRange;
        return value > range.Minimum + margin && value < range.Maximum - margin;
    }
}
