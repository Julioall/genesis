using System.Text;
using System.Text.Json;
using Game.Contracts;
using Game.Procedural;

namespace Game.Modules.Biosphere;

/// <summary>Versioned, renderer-neutral species and development prototypes for Zoology Z1.</summary>
public static class CreatureSpeciesCatalogV1
{
    private static readonly CreatureGeneBoundsV1 QuadrupedBounds = new(
        new(0.85, 1.15), new(0.80, 1.20), new(0.85, 1.15), new(0.10, 0.90),
        new(15, 55), new(0.75, 3.50), new(0.002, 0.005));

    private static readonly CreatureGeneBoundsV1 FishBounds = new(
        new(0.85, 1.15), new(0.75, 1.25), new(0.85, 1.15), new(0, 0),
        new(170, 230), new(0.50, 8.00), new(0.004, 0.015));

    private static readonly CreatureSpeciesDefinitionV1[] Definitions =
    [
        new("prototype.quadruped.generalist.v1", "Quadruped prototype", CreatureFamilyV1.Quadruped,
            "vertebrate.quadruped.v1", 1, "creature.genome.direct.v1", 1,
            CreatureHabitatV1.Terrestrial, "prototype.quadruped.generalist.v1",
            1.25, 15.0, 365, QuadrupedBounds),
        new("prototype.freshwater.fish.v1", "Freshwater fish prototype", CreatureFamilyV1.Fish,
            "vertebrate.fish.v1", 1, "creature.genome.direct.v1", 1,
            CreatureHabitatV1.Freshwater, "prototype.freshwater.fish.v1",
            0.45, 0.60, 200, FishBounds)
    ];

    private static readonly IReadOnlyList<CreatureSpeciesDefinitionV1> ReadOnlyDefinitions
        = Array.AsReadOnly(Definitions);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static IReadOnlyList<CreatureSpeciesDefinitionV1> All => ReadOnlyDefinitions;
    public static CreatureSpeciesDefinitionV1 Quadruped => Definitions[0];
    public static CreatureSpeciesDefinitionV1 FreshwaterFish => Definitions[1];

    public static CreatureSpeciesDefinitionV1 Resolve(string speciesId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(speciesId);
        return Definitions.FirstOrDefault(definition =>
                   string.Equals(definition.SpeciesId, speciesId, StringComparison.Ordinal))
               ?? throw new KeyNotFoundException($"Unknown built-in creature species '{speciesId}'.");
    }

    public static string Export(CreatureSpeciesDefinitionV1 species)
    {
        CreatureDevelopmentModelV1.ValidateSpecies(species);
        return JsonSerializer.Serialize(species, JsonOptions);
    }

    public static CreatureSpeciesDefinitionV1 Restore(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var species = JsonSerializer.Deserialize<CreatureSpeciesDefinitionV1>(json, JsonOptions)
            ?? throw new JsonException("Species document was empty.");
        CreatureDevelopmentModelV1.ValidateSpecies(species);
        return species;
    }
}

/// <summary>
/// Pure genetics and morphology generation. Prototype ranges are software envelopes, not
/// calibrated measurements for real species; this service never receives or mutates world state.
/// </summary>
public sealed class CreatureDevelopmentModelV1 : ICreatureDevelopmentV1
{
    public const int GeneratorVersion = 1;
    public const int VisualRecipeVersion = 1;
    public const double MaximumMutationFractionOfRange = 0.05;

    private const ulong AnatomyGeneTag = 0x5A31414E41544F4DUL;
    private const ulong PigmentGeneTag = 0x5A315049474D454EUL;
    private const ulong MutationTag = 0x5A314D5554415445UL;
    private const ulong MutationAnatomySeedTag = 0x5A314D5554414E41UL;
    private const ulong MutationPigmentSeedTag = 0x5A314D5554504947UL;
    private const ulong PatternSeedTag = 0x5A31504154544552UL;
    private const ulong StructuralSignatureTag = 0x5A31535452554354UL;
    private const ulong GenomeIdTag = 0x5A3147454E4F4D45UL;
    private const ulong WorldAnatomyStreamTag = 0x5A31574F52414E41UL;
    private const ulong WorldPigmentStreamTag = 0x5A31574F52504947UL;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public CreatureGenomeV1 CreateGenome(
        CreatureSpeciesDefinitionV1 species,
        ulong anatomySeed,
        ulong pigmentSeed)
    {
        ValidateSpecies(species);
        var speciesHash = StableHash(species.SpeciesId);
        var bounds = species.GeneBounds;
        var genes = new CreatureGeneValuesV1(
            Sample(bounds.BodyScale, anatomySeed, AnatomyGeneTag, speciesHash, 0),
            Sample(bounds.AppendageRelativeLength, anatomySeed, AnatomyGeneTag, speciesHash, 1),
            Sample(bounds.TrunkRelativeLength, anatomySeed, AnatomyGeneTag, speciesHash, 2),
            Sample(bounds.ThermalInsulationIndex, anatomySeed, AnatomyGeneTag, speciesHash, 3),
            Sample(bounds.PigmentHueDegrees, pigmentSeed, PigmentGeneTag, speciesHash, 0),
            Sample(bounds.PatternFrequencyPerM, pigmentSeed, PigmentGeneTag, speciesHash, 1),
            Sample(bounds.GrowthRatePerDay, anatomySeed, AnatomyGeneTag, speciesHash, 4));
        var genome = new CreatureGenomeV1(species.GeneticsSchemaVersion, GeneratorVersion,
            species.SpeciesId, ComputeGenomeId(species, anatomySeed, pigmentSeed, genes, null, null),
            anatomySeed, pigmentSeed, genes, null, null);
        ValidateGenome(species, genome);
        return genome;
    }

    public CreatureGenomeV1 CreateGenomeFromWorldSeed(
        CreatureSpeciesDefinitionV1 species,
        ulong worldSeed,
        ulong individualId)
    {
        ValidateSpecies(species);
        var speciesHash = StableHash(species.SpeciesId);
        var anatomySeed = CounterRandom.Bits(worldSeed, WorldAnatomyStreamTag,
            speciesHash, individualId, GeneratorVersion, 0);
        var pigmentSeed = CounterRandom.Bits(worldSeed, WorldPigmentStreamTag,
            speciesHash, individualId, VisualRecipeVersion, 0);
        return CreateGenome(species, anatomySeed, pigmentSeed);
    }

    public CreatureGenomeV1 MutateGenome(
        CreatureSpeciesDefinitionV1 species,
        CreatureGenomeV1 parent,
        ulong mutationSeed)
    {
        ValidateSpecies(species);
        ValidateGenome(species, parent);
        var speciesHash = StableHash(species.SpeciesId);
        var bounds = species.GeneBounds;
        var genes = parent.Genes with
        {
            BodyScale = Mutate(parent.Genes.BodyScale, bounds.BodyScale, parent, speciesHash, mutationSeed, 0),
            AppendageRelativeLength = Mutate(parent.Genes.AppendageRelativeLength,
                bounds.AppendageRelativeLength, parent, speciesHash, mutationSeed, 1),
            TrunkRelativeLength = Mutate(parent.Genes.TrunkRelativeLength,
                bounds.TrunkRelativeLength, parent, speciesHash, mutationSeed, 2),
            ThermalInsulationIndex = Mutate(parent.Genes.ThermalInsulationIndex,
                bounds.ThermalInsulationIndex, parent, speciesHash, mutationSeed, 3),
            PigmentHueDegrees = Mutate(parent.Genes.PigmentHueDegrees,
                bounds.PigmentHueDegrees, parent, speciesHash, mutationSeed, 4),
            PatternFrequencyPerM = Mutate(parent.Genes.PatternFrequencyPerM,
                bounds.PatternFrequencyPerM, parent, speciesHash, mutationSeed, 5),
            GrowthRatePerDay = Mutate(parent.Genes.GrowthRatePerDay,
                bounds.GrowthRatePerDay, parent, speciesHash, mutationSeed, 6)
        };
        var anatomySeed = CounterRandom.Bits(parent.AnatomySeed, MutationAnatomySeedTag,
            parent.GenomeId, mutationSeed, GeneratorVersion, 0);
        var pigmentSeed = CounterRandom.Bits(parent.PigmentSeed, MutationPigmentSeedTag,
            parent.GenomeId, mutationSeed, GeneratorVersion, 0);
        var child = new CreatureGenomeV1(species.GeneticsSchemaVersion, GeneratorVersion,
            species.SpeciesId, ComputeGenomeId(species, anatomySeed, pigmentSeed, genes,
                parent.GenomeId, null), anatomySeed, pigmentSeed, genes, parent.GenomeId, null);
        ValidateGenome(species, child);
        return child;
    }

    public CreatureDevelopmentResultV1 Develop(
        CreatureSpeciesDefinitionV1 species,
        CreatureGenomeV1 genome,
        CreatureDevelopmentContextV1 context)
    {
        ValidateSpecies(species);
        ValidateGenome(species, genome);
        if (!double.IsFinite(context.AgeDays) || context.AgeDays < 0
            || !InUnitInterval(context.ThermalDevelopmentFactor01)
            || !InUnitInterval(context.NutritionalCondition01))
            throw new ArgumentOutOfRangeException(nameof(context),
                "Age must be non-negative and development factors must be finite values in [0, 1].");

        var plan = CreatureBodyPlanCatalogV1.Resolve(species.BodyPlanId, species.BodyPlanVersion);
        if (plan.Family != species.Family)
            throw new InvalidOperationException("Species family does not match its body plan.");

        var developmentInput = Math.Clamp(context.AgeDays * genome.Genes.GrowthRatePerDay
            * context.ThermalDevelopmentFactor01, 0, 1);
        var developmentProgress = SmoothStep(developmentInput);
        var growthScale = 0.12 + 0.88 * developmentProgress;
        var nutritionScale = 0.85 + 0.15 * context.NutritionalCondition01;
        var bodyLengthM = species.ReferenceAdultBodyLengthM * genome.Genes.BodyScale
            * growthScale * nutritionScale;
        var structuralMassKg = species.ReferenceAdultStructuralMassKg
            * Math.Pow(genome.Genes.BodyScale * growthScale * nutritionScale, 3);

        var parts = new AnatomicalPartV1[plan.Parts.Count];
        for (var index = 0; index < plan.Parts.Count; index++)
        {
            var template = plan.Parts[index];
            var geneScale = template.LengthInfluence switch
            {
                GeneInfluenceV1.Appendage => genome.Genes.AppendageRelativeLength,
                GeneInfluenceV1.Trunk => genome.Genes.TrunkRelativeLength,
                _ => 1.0
            };
            var lengthM = bodyLengthM * template.LengthFraction * geneScale;
            parts[index] = new(template.PartId, template.ParentPartIndex, template.Kind,
                template.Side, lengthM, Math.Max(lengthM * template.RadiusToLengthRatio, bodyLengthM * 0.004),
                structuralMassKg * template.MassFraction,
                bodyLengthM * template.OffsetXBodyLengths,
                bodyLengthM * template.OffsetYBodyLengths,
                bodyLengthM * template.OffsetZBodyLengths);
        }

        var joints = plan.Parts.Select((template, childIndex) => (template, childIndex))
            .Where(item => item.template.ParentPartIndex >= 0)
            .Select(item => new AnatomicalJointV1(item.template.ParentPartIndex, item.childIndex,
                item.template.JointKind, item.template.MinimumAngleDegrees, item.template.MaximumAngleDegrees))
            .ToArray();
        var sensors = plan.Sensors.Select(sensor => new SensoryAnchorV1(sensor.SensorId,
            sensor.PartIndex, sensor.Kind,
            bodyLengthM * sensor.OffsetXBodyLengths,
            bodyLengthM * sensor.OffsetYBodyLengths,
            bodyLengthM * sensor.OffsetZBodyLengths)).ToArray();
        var morphology = BuildMorphology(species.Family, genome.Genes.AppendageRelativeLength, bodyLengthM, parts);
        var signature = ComputeStructuralSignature(species, genome, context, developmentProgress,
            bodyLengthM, structuralMassKg, morphology, parts, joints, sensors);
        var body = new DevelopedBodyV1(species.SpeciesId, species.BodyPlanId, species.BodyPlanVersion,
            species.Family, developmentProgress, bodyLengthM, structuralMassKg,
            genome.Genes.ThermalInsulationIndex, morphology, signature, parts, joints, sensors);
        var visualRecipe = BuildVisualRecipe(species, genome, signature);

        var errors = ValidateDevelopedBody(species, genome, body);
        if (errors.Count > 0)
            throw new InvalidOperationException("Generated body failed validation: " + string.Join("; ", errors));
        return new(body, visualRecipe);
    }

    public static CreatureGenomeV1 RestoreGenome(string json, CreatureSpeciesDefinitionV1 species)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var genome = JsonSerializer.Deserialize<CreatureGenomeV1>(json, JsonOptions);
        ValidateGenome(species, genome);
        return genome;
    }

    public static string ExportGenome(CreatureSpeciesDefinitionV1 species, CreatureGenomeV1 genome)
    {
        ValidateGenome(species, genome);
        return JsonSerializer.Serialize(genome, JsonOptions);
    }

    public static bool CanReproduceWith(CreatureSpeciesDefinitionV1 first, CreatureSpeciesDefinitionV1 second)
    {
        ValidateSpecies(first);
        ValidateSpecies(second);
        return first.Family == second.Family
            && first.Habitat == second.Habitat
            && first.BodyPlanId == second.BodyPlanId
            && first.BodyPlanVersion == second.BodyPlanVersion
            && first.GeneticsSchemaId == second.GeneticsSchemaId
            && first.GeneticsSchemaVersion == second.GeneticsSchemaVersion
            && first.ReproductiveCompatibilityGroupId == second.ReproductiveCompatibilityGroupId;
    }

    public static IReadOnlyList<string> ValidateDevelopedBody(
        CreatureSpeciesDefinitionV1 species,
        CreatureGenomeV1 genome,
        DevelopedBodyV1 body)
    {
        ArgumentNullException.ThrowIfNull(body);
        var errors = new List<string>();
        BodyPlanDefinitionV1 plan;
        try
        {
            ValidateSpecies(species);
            ValidateGenome(species, genome);
            plan = CreatureBodyPlanCatalogV1.Resolve(species.BodyPlanId, species.BodyPlanVersion);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
                                          or KeyNotFoundException or NotSupportedException)
        {
            return [exception.Message];
        }

        if (body.SpeciesId != species.SpeciesId || body.BodyPlanId != species.BodyPlanId
            || body.BodyPlanVersion != species.BodyPlanVersion || body.Family != species.Family)
            errors.Add("Developed body identity does not match the species definition.");
        if (!FinitePositive(body.BodyLengthM) || !FinitePositive(body.ReferenceStructuralMassKg)
            || !InUnitInterval(body.DevelopmentProgress01)
            || !FiniteNonNegative(body.ThermalInsulationIndex))
            errors.Add("Body-wide dimensions, mass, development and insulation must be finite and valid.");
        if (body.Parts.Count != plan.Parts.Count)
            errors.Add("The body does not contain the body plan's complete part set.");
        if (body.Parts.Count > 0 && body.Parts.Count == plan.Parts.Count)
        {
            var roots = 0;
            var massSum = 0.0;
            for (var index = 0; index < body.Parts.Count; index++)
            {
                var part = body.Parts[index];
                if (part.ParentPartIndex == -1) roots++;
                if (part.ParentPartIndex >= index || part.ParentPartIndex < -1)
                    errors.Add($"Part '{part.PartId}' has an invalid or cyclic parent index.");
                if (part.PartId != plan.Parts[index].PartId || part.Kind != plan.Parts[index].Kind
                    || part.Side != plan.Parts[index].Side)
                    errors.Add($"Part at index {index} does not match the versioned body plan.");
                if (!FinitePositive(part.LengthM) || !FinitePositive(part.RadiusM)
                    || !FinitePositive(part.StructuralMassKg)
                    || !double.IsFinite(part.LocalOffsetXM) || !double.IsFinite(part.LocalOffsetYM)
                    || !double.IsFinite(part.LocalOffsetZM))
                    errors.Add($"Part '{part.PartId}' has invalid dimensions, mass or offsets.");
                massSum += part.StructuralMassKg;
            }
            if (roots != 1) errors.Add("The body plan must have exactly one root part.");
            if (Math.Abs(massSum - body.ReferenceStructuralMassKg)
                > Math.Max(1e-9, body.ReferenceStructuralMassKg * 1e-10))
                errors.Add("Part masses do not sum to the body's reference structural mass.");

            foreach (var (leftIndex, rightIndex) in plan.SymmetryPairs)
            {
                if (leftIndex >= body.Parts.Count || rightIndex >= body.Parts.Count) continue;
                var left = body.Parts[leftIndex];
                var right = body.Parts[rightIndex];
                if (left.Side != AnatomicalSideV1.Left || right.Side != AnatomicalSideV1.Right
                    || left.Kind != right.Kind || !NearlyEqual(left.LengthM, right.LengthM)
                    || !NearlyEqual(left.RadiusM, right.RadiusM)
                    || !NearlyEqual(left.StructuralMassKg, right.StructuralMassKg)
                    || !NearlyEqual(left.LocalOffsetXM, -right.LocalOffsetXM)
                    || !NearlyEqual(left.LocalOffsetYM, right.LocalOffsetYM)
                    || !NearlyEqual(left.LocalOffsetZM, right.LocalOffsetZM))
                    errors.Add($"Symmetry pair {leftIndex}/{rightIndex} is not mirrored.");
            }
        }

        if (body.Joints.Count != Math.Max(0, plan.Parts.Count - 1))
            errors.Add("Every non-root body part must have exactly one joint.");
        foreach (var joint in body.Joints)
            if (joint.ParentPartIndex < 0 || joint.ParentPartIndex >= body.Parts.Count
                || joint.ChildPartIndex <= joint.ParentPartIndex || joint.ChildPartIndex >= body.Parts.Count
                || !double.IsFinite(joint.MinimumAngleDegrees) || !double.IsFinite(joint.MaximumAngleDegrees)
                || joint.MinimumAngleDegrees >= joint.MaximumAngleDegrees)
                errors.Add("Anatomical joints must connect ordered parts and have finite non-empty limits.");
        foreach (var sensor in body.Sensors)
            if (string.IsNullOrWhiteSpace(sensor.SensorId) || sensor.PartIndex < 0
                || sensor.PartIndex >= body.Parts.Count || !double.IsFinite(sensor.LocalOffsetXM)
                || !double.IsFinite(sensor.LocalOffsetYM) || !double.IsFinite(sensor.LocalOffsetZM))
                errors.Add("Sensory anchors must reference a body part and finite local coordinates.");
        if (!FiniteNonNegative(body.Morphology.NominalStrideLengthM)
            || !FiniteNonNegative(body.Morphology.PropulsionSurfaceAreaProxyM2))
            errors.Add("Morphology capacity proxies must be finite and non-negative.");

        return errors.AsReadOnly();
    }

    internal static void ValidateSpecies(CreatureSpeciesDefinitionV1 species)
    {
        ArgumentNullException.ThrowIfNull(species);
        if (string.IsNullOrWhiteSpace(species.SpeciesId) || string.IsNullOrWhiteSpace(species.DisplayName)
            || string.IsNullOrWhiteSpace(species.BodyPlanId) || string.IsNullOrWhiteSpace(species.GeneticsSchemaId)
            || string.IsNullOrWhiteSpace(species.ReproductiveCompatibilityGroupId))
            throw new ArgumentException("Species identifiers and display name cannot be empty.", nameof(species));
        if (species.BodyPlanVersion <= 0 || species.GeneticsSchemaVersion <= 0
            || !FinitePositive(species.ReferenceAdultBodyLengthM)
            || !FinitePositive(species.ReferenceAdultStructuralMassKg)
            || !FinitePositive(species.MaturityDays))
            throw new ArgumentException("Species versions and reference dimensions must be positive.", nameof(species));
        if (species.GeneticsSchemaId != "creature.genome.direct.v1" || species.GeneticsSchemaVersion != 1)
            throw new NotSupportedException($"Genetics schema '{species.GeneticsSchemaId}' version {species.GeneticsSchemaVersion} is not supported.");
        ValidateBounds(species.GeneBounds);
        var plan = CreatureBodyPlanCatalogV1.Resolve(species.BodyPlanId, species.BodyPlanVersion);
        if (plan.Family != species.Family)
            throw new ArgumentException("Species family and body plan family do not match.", nameof(species));
    }

    private static void ValidateGenome(CreatureSpeciesDefinitionV1 species, CreatureGenomeV1 genome)
    {
        ValidateSpecies(species);
        if (genome.SchemaVersion != species.GeneticsSchemaVersion || genome.GeneratorVersion != GeneratorVersion
            || genome.SpeciesId != species.SpeciesId || genome.GenomeId == 0)
            throw new ArgumentException("Genome identity or schema versions do not match the species.", nameof(genome));
        var genes = genome.Genes;
        var bounds = species.GeneBounds;
        if (!Contains(bounds.BodyScale, genes.BodyScale)
            || !Contains(bounds.AppendageRelativeLength, genes.AppendageRelativeLength)
            || !Contains(bounds.TrunkRelativeLength, genes.TrunkRelativeLength)
            || !Contains(bounds.ThermalInsulationIndex, genes.ThermalInsulationIndex)
            || !Contains(bounds.PigmentHueDegrees, genes.PigmentHueDegrees)
            || !Contains(bounds.PatternFrequencyPerM, genes.PatternFrequencyPerM)
            || !Contains(bounds.GrowthRatePerDay, genes.GrowthRatePerDay))
            throw new ArgumentException("Genome contains a non-finite allele or an allele outside the family envelope.", nameof(genome));
        if (genome.ParentAId == genome.GenomeId || genome.ParentBId == genome.GenomeId
            || genome.ParentAId is not null && genome.ParentAId == genome.ParentBId)
            throw new ArgumentException("Genome parent IDs must be distinct from the child and each other.", nameof(genome));
        if (ComputeGenomeId(species, genome.AnatomySeed, genome.PigmentSeed, genome.Genes,
                genome.ParentAId, genome.ParentBId) != genome.GenomeId)
            throw new ArgumentException("Genome ID does not match its versioned seeds, alleles and parentage.", nameof(genome));
    }

    private static void ValidateBounds(CreatureGeneBoundsV1 bounds)
    {
        ValidateRange(bounds.BodyScale, "body.scale", requirePositive: true);
        ValidateRange(bounds.AppendageRelativeLength, "appendage.relativeLength", requirePositive: true);
        ValidateRange(bounds.TrunkRelativeLength, "trunk.relativeLength", requirePositive: true);
        ValidateRange(bounds.ThermalInsulationIndex, "coat.insulationIndex", minimum: 0, maximum: 1);
        ValidateRange(bounds.PigmentHueDegrees, "coat.pigmentHueDegrees", minimum: 0, maximum: 360);
        ValidateRange(bounds.PatternFrequencyPerM, "coat.patternFrequencyPerM", minimum: 0);
        ValidateRange(bounds.GrowthRatePerDay, "life.growthRatePerDay", requirePositive: true);
    }

    private static void ValidateRange(
        GeneRangeV1 range,
        string gene,
        double minimum = double.NegativeInfinity,
        double maximum = double.PositiveInfinity,
        bool requirePositive = false)
    {
        if (!double.IsFinite(range.Minimum) || !double.IsFinite(range.Maximum)
            || range.Minimum > range.Maximum || range.Minimum < minimum || range.Maximum > maximum
            || requirePositive && range.Minimum <= 0)
            throw new ArgumentException($"Gene range '{gene}' is invalid.");
    }

    private static CreatureVisualRecipeV1 BuildVisualRecipe(
        CreatureSpeciesDefinitionV1 species,
        CreatureGenomeV1 genome,
        ulong structuralSignature)
    {
        var speciesHash = StableHash(species.SpeciesId);
        var patternSeed = CounterRandom.Bits(genome.PigmentSeed, PatternSeedTag,
            speciesHash, genome.GenomeId, VisualRecipeVersion, 0);
        var contrast = 0.2 + 0.65 * CounterRandom.Unit(genome.PigmentSeed, PatternSeedTag,
            speciesHash, genome.GenomeId, VisualRecipeVersion, 1);
        return new(VisualRecipeVersion, species.SpeciesId, structuralSignature,
            genome.Genes.PigmentHueDegrees, genome.Genes.PatternFrequencyPerM,
            contrast, patternSeed);
    }

    private static MorphologicalCapacityV1 BuildMorphology(
        CreatureFamilyV1 family,
        double appendageRelativeLength,
        double bodyLengthM,
        IReadOnlyList<AnatomicalPartV1> parts)
    {
        if (family == CreatureFamilyV1.Quadruped)
        {
            var upper = parts.First(part => part.Kind == AnatomicalPartKindV1.LimbProximal
                                            && part.Side == AnatomicalSideV1.Left);
            var lower = parts.First(part => part.Kind == AnatomicalPartKindV1.LimbDistal
                                            && part.Side == AnatomicalSideV1.Left);
            return new(1.2 * (upper.LengthM + lower.LengthM), 0);
        }

        var finAreaProxy = 0.06 * bodyLengthM * bodyLengthM
            * appendageRelativeLength * appendageRelativeLength;
        return new(0, finAreaProxy);
    }

    private static ulong ComputeStructuralSignature(
        CreatureSpeciesDefinitionV1 species,
        CreatureGenomeV1 genome,
        CreatureDevelopmentContextV1 context,
        double developmentProgress,
        double bodyLengthM,
        double structuralMassKg,
        MorphologicalCapacityV1 morphology,
        IReadOnlyList<AnatomicalPartV1> parts,
        IReadOnlyList<AnatomicalJointV1> joints,
        IReadOnlyList<SensoryAnchorV1> sensors)
    {
        var hash = CounterRandom.Bits(genome.AnatomySeed, StructuralSignatureTag,
            StableHash(species.SpeciesId), StableHash(species.BodyPlanId),
            (ulong)species.BodyPlanVersion, (ulong)species.GeneticsSchemaVersion);
        foreach (var value in new[]
                 {
                     genome.Genes.BodyScale, genome.Genes.AppendageRelativeLength,
                     genome.Genes.TrunkRelativeLength, genome.Genes.ThermalInsulationIndex,
                     genome.Genes.GrowthRatePerDay, context.AgeDays,
                     context.ThermalDevelopmentFactor01, context.NutritionalCondition01,
                     developmentProgress, bodyLengthM, structuralMassKg,
                     morphology.NominalStrideLengthM, morphology.PropulsionSurfaceAreaProxyM2
                 })
            hash = HashDouble(hash, value);
        foreach (var part in parts)
        {
            hash = CounterRandom.Mix64(hash ^ StableHash(part.PartId));
            hash = CounterRandom.Mix64(hash ^ unchecked((ulong)part.ParentPartIndex));
            hash = CounterRandom.Mix64(hash ^ (ulong)part.Kind ^ ((ulong)(byte)part.Side << 8));
            hash = HashDouble(hash, part.LengthM);
            hash = HashDouble(hash, part.RadiusM);
            hash = HashDouble(hash, part.StructuralMassKg);
            hash = HashDouble(hash, part.LocalOffsetXM);
            hash = HashDouble(hash, part.LocalOffsetYM);
            hash = HashDouble(hash, part.LocalOffsetZM);
        }
        foreach (var joint in joints)
        {
            hash = CounterRandom.Mix64(hash ^ (ulong)joint.ParentPartIndex ^ ((ulong)joint.ChildPartIndex << 16));
            hash = CounterRandom.Mix64(hash ^ (ulong)joint.Kind);
            hash = HashDouble(hash, joint.MinimumAngleDegrees);
            hash = HashDouble(hash, joint.MaximumAngleDegrees);
        }
        foreach (var sensor in sensors)
        {
            hash = CounterRandom.Mix64(hash ^ StableHash(sensor.SensorId));
            hash = CounterRandom.Mix64(hash ^ (ulong)sensor.PartIndex ^ ((ulong)sensor.Kind << 32));
            hash = HashDouble(hash, sensor.LocalOffsetXM);
            hash = HashDouble(hash, sensor.LocalOffsetYM);
            hash = HashDouble(hash, sensor.LocalOffsetZM);
        }
        return hash == 0 ? 1 : hash;
    }

    private static ulong ComputeGenomeId(
        CreatureSpeciesDefinitionV1 species,
        ulong anatomySeed,
        ulong pigmentSeed,
        CreatureGeneValuesV1 genes,
        ulong? parentA,
        ulong? parentB)
    {
        var hash = CounterRandom.Bits(1, GenomeIdTag, StableHash(species.SpeciesId),
            anatomySeed, (ulong)species.GeneticsSchemaVersion, pigmentSeed);
        foreach (var value in new[]
                 {
                     genes.BodyScale, genes.AppendageRelativeLength, genes.TrunkRelativeLength,
                     genes.ThermalInsulationIndex, genes.PigmentHueDegrees,
                     genes.PatternFrequencyPerM, genes.GrowthRatePerDay
                 })
            hash = HashDouble(hash, value);
        hash = CounterRandom.Mix64(hash ^ parentA.GetValueOrDefault());
        hash = CounterRandom.Mix64(hash ^ parentB.GetValueOrDefault());
        return hash == 0 ? 1 : hash;
    }

    private static double Mutate(
        double value,
        GeneRangeV1 bounds,
        CreatureGenomeV1 parent,
        ulong speciesHash,
        ulong mutationSeed,
        ulong drawIndex)
    {
        var unit = CounterRandom.Unit(mutationSeed, MutationTag,
            parent.GenomeId, speciesHash, GeneratorVersion, drawIndex);
        var delta = (2 * unit - 1) * (bounds.Maximum - bounds.Minimum) * MaximumMutationFractionOfRange;
        return Math.Clamp(value + delta, bounds.Minimum, bounds.Maximum);
    }

    private static double Sample(GeneRangeV1 range, ulong seed, ulong tag, ulong speciesHash, ulong drawIndex)
    {
        if (range.Minimum == range.Maximum) return range.Minimum;
        var unit = CounterRandom.Unit(seed, tag, speciesHash, GeneratorVersion, 0, drawIndex);
        return range.Minimum + (range.Maximum - range.Minimum) * unit;
    }

    private static bool Contains(GeneRangeV1 range, double value)
        => double.IsFinite(value) && value >= range.Minimum && value <= range.Maximum;

    private static bool InUnitInterval(double value)
        => double.IsFinite(value) && value is >= 0 and <= 1;

    private static bool FinitePositive(double value) => double.IsFinite(value) && value > 0;
    private static bool FiniteNonNegative(double value) => double.IsFinite(value) && value >= 0;
    private static bool NearlyEqual(double first, double second)
        => Math.Abs(first - second) <= 1e-10 * Math.Max(1, Math.Max(Math.Abs(first), Math.Abs(second)));
    private static double SmoothStep(double value) => value * value * (3 - 2 * value);

    private static ulong HashDouble(ulong hash, double value)
        => CounterRandom.Mix64(hash ^ unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));

    private static ulong StableHash(string value)
    {
        var hash = 14695981039346656037UL;
        foreach (var character in Encoding.UTF8.GetBytes(value))
            hash = (hash ^ character) * 1099511628211UL;
        return CounterRandom.Mix64(hash);
    }
}

internal enum GeneInfluenceV1 : byte
{
    None,
    Trunk,
    Appendage
}

internal sealed record BodyPartTemplateV1(
    string PartId,
    int ParentPartIndex,
    AnatomicalPartKindV1 Kind,
    AnatomicalSideV1 Side,
    double LengthFraction,
    double RadiusToLengthRatio,
    double MassFraction,
    double OffsetXBodyLengths,
    double OffsetYBodyLengths,
    double OffsetZBodyLengths,
    GeneInfluenceV1 LengthInfluence,
    AnatomicalJointKindV1 JointKind,
    double MinimumAngleDegrees,
    double MaximumAngleDegrees);

internal sealed record SensorTemplateV1(
    string SensorId,
    int PartIndex,
    SensoryKindV1 Kind,
    double OffsetXBodyLengths,
    double OffsetYBodyLengths,
    double OffsetZBodyLengths);

internal sealed record BodyPlanDefinitionV1(
    string Id,
    int Version,
    CreatureFamilyV1 Family,
    IReadOnlyList<BodyPartTemplateV1> Parts,
    IReadOnlyList<(int Left, int Right)> SymmetryPairs,
    IReadOnlyList<SensorTemplateV1> Sensors);

internal static class CreatureBodyPlanCatalogV1
{
    private static readonly BodyPlanDefinitionV1 Quadruped = CreateQuadruped();
    private static readonly BodyPlanDefinitionV1 Fish = CreateFish();

    public static BodyPlanDefinitionV1 Resolve(string id, int version)
    {
        var plan = id switch
        {
            "vertebrate.quadruped.v1" => Quadruped,
            "vertebrate.fish.v1" => Fish,
            _ => throw new KeyNotFoundException($"Unknown body plan '{id}'.")
        };
        if (plan.Version != version) throw new NotSupportedException($"Body plan '{id}' version {version} is not supported.");
        return plan;
    }

    private static BodyPlanDefinitionV1 CreateQuadruped()
    {
        BodyPartTemplateV1 Part(string id, int parent, AnatomicalPartKindV1 kind,
            AnatomicalSideV1 side, double length, double radius, double mass,
            double x, double y, double z, GeneInfluenceV1 influence,
            AnatomicalJointKindV1 joint = AnatomicalJointKindV1.Ball,
            double minimum = -35, double maximum = 35)
            => new(id, parent, kind, side, length, radius, mass, x, y, z,
                influence, joint, minimum, maximum);

        var parts = Array.AsReadOnly(new[]
        {
            Part("torso", -1, AnatomicalPartKindV1.Torso, AnatomicalSideV1.Center,
                0.44, 0.24, 0.48, 0, 0, 0, GeneInfluenceV1.Trunk),
            Part("neck", 0, AnatomicalPartKindV1.Neck, AnatomicalSideV1.Center,
                0.13, 0.34, 0.06, 0, 0.01, 0.20, GeneInfluenceV1.Trunk,
                AnatomicalJointKindV1.Ball, -35, 35),
            Part("head", 1, AnatomicalPartKindV1.Head, AnatomicalSideV1.Center,
                0.19, 0.38, 0.08, 0, 0.015, 0.07, GeneInfluenceV1.Trunk,
                AnatomicalJointKindV1.Ball, -25, 25),
            Part("tail", 0, AnatomicalPartKindV1.Tail, AnatomicalSideV1.Center,
                0.20, 0.16, 0.04, 0, -0.02, -0.23, GeneInfluenceV1.Trunk,
                AnatomicalJointKindV1.FlexibleAxial, -35, 35),
            Part("front.left.upper", 0, AnatomicalPartKindV1.LimbProximal, AnatomicalSideV1.Left,
                0.17, 0.20, 0.04, -0.13, -0.08, 0.13, GeneInfluenceV1.Appendage,
                AnatomicalJointKindV1.Hinge, -65, 65),
            Part("front.left.lower", 4, AnatomicalPartKindV1.LimbDistal, AnatomicalSideV1.Left,
                0.16, 0.17, 0.03, 0, -0.15, 0, GeneInfluenceV1.Appendage,
                AnatomicalJointKindV1.Hinge, 0, 145),
            Part("front.right.upper", 0, AnatomicalPartKindV1.LimbProximal, AnatomicalSideV1.Right,
                0.17, 0.20, 0.04, 0.13, -0.08, 0.13, GeneInfluenceV1.Appendage,
                AnatomicalJointKindV1.Hinge, -65, 65),
            Part("front.right.lower", 6, AnatomicalPartKindV1.LimbDistal, AnatomicalSideV1.Right,
                0.16, 0.17, 0.03, 0, -0.15, 0, GeneInfluenceV1.Appendage,
                AnatomicalJointKindV1.Hinge, 0, 145),
            Part("hind.left.upper", 0, AnatomicalPartKindV1.LimbProximal, AnatomicalSideV1.Left,
                0.18, 0.22, 0.06, -0.11, -0.08, -0.12, GeneInfluenceV1.Appendage,
                AnatomicalJointKindV1.Hinge, -70, 70),
            Part("hind.left.lower", 8, AnatomicalPartKindV1.LimbDistal, AnatomicalSideV1.Left,
                0.17, 0.18, 0.04, 0, -0.16, 0, GeneInfluenceV1.Appendage,
                AnatomicalJointKindV1.Hinge, 0, 150),
            Part("hind.right.upper", 0, AnatomicalPartKindV1.LimbProximal, AnatomicalSideV1.Right,
                0.18, 0.22, 0.06, 0.11, -0.08, -0.12, GeneInfluenceV1.Appendage,
                AnatomicalJointKindV1.Hinge, -70, 70),
            Part("hind.right.lower", 10, AnatomicalPartKindV1.LimbDistal, AnatomicalSideV1.Right,
                0.17, 0.18, 0.04, 0, -0.16, 0, GeneInfluenceV1.Appendage,
                AnatomicalJointKindV1.Hinge, 0, 150)
        });
        var sensors = Array.AsReadOnly(new[]
        {
            new SensorTemplateV1("eye.left", 2, SensoryKindV1.Vision, -0.075, 0.025, 0.02),
            new SensorTemplateV1("eye.right", 2, SensoryKindV1.Vision, 0.075, 0.025, 0.02),
            new SensorTemplateV1("nose", 2, SensoryKindV1.Olfaction, 0, 0, 0.10)
        });
        return new("vertebrate.quadruped.v1", 1, CreatureFamilyV1.Quadruped,
            parts, Array.AsReadOnly(new[] { (4, 6), (5, 7), (8, 10), (9, 11) }), sensors);
    }

    private static BodyPlanDefinitionV1 CreateFish()
    {
        BodyPartTemplateV1 Part(string id, int parent, AnatomicalPartKindV1 kind,
            AnatomicalSideV1 side, double length, double radius, double mass,
            double x, double y, double z, GeneInfluenceV1 influence,
            AnatomicalJointKindV1 joint = AnatomicalJointKindV1.FlexibleAxial,
            double minimum = -30, double maximum = 30)
            => new(id, parent, kind, side, length, radius, mass, x, y, z,
                influence, joint, minimum, maximum);

        var parts = Array.AsReadOnly(new[]
        {
            Part("trunk.front", -1, AnatomicalPartKindV1.Torso, AnatomicalSideV1.Center,
                0.48, 0.31, 0.42, 0, 0, 0, GeneInfluenceV1.Trunk),
            Part("head", 0, AnatomicalPartKindV1.Head, AnatomicalSideV1.Center,
                0.23, 0.42, 0.15, 0, 0.01, 0.15, GeneInfluenceV1.Trunk,
                AnatomicalJointKindV1.FlexibleAxial, -20, 20),
            Part("trunk.rear", 0, AnatomicalPartKindV1.AxialSegment, AnatomicalSideV1.Center,
                0.38, 0.28, 0.18, 0, 0, -0.19, GeneInfluenceV1.Trunk),
            Part("caudal.peduncle", 2, AnatomicalPartKindV1.AxialSegment, AnatomicalSideV1.Center,
                0.14, 0.18, 0.08, 0, 0, -0.10, GeneInfluenceV1.Trunk),
            Part("fin.caudal", 3, AnatomicalPartKindV1.Fin, AnatomicalSideV1.Center,
                0.22, 0.50, 0.04, 0, 0, -0.06, GeneInfluenceV1.Appendage,
                AnatomicalJointKindV1.FlexibleAxial, -35, 35),
            Part("fin.dorsal", 2, AnatomicalPartKindV1.Fin, AnatomicalSideV1.Center,
                0.18, 0.40, 0.03, 0, 0.12, 0, GeneInfluenceV1.Appendage,
                AnatomicalJointKindV1.Hinge, -20, 35),
            Part("fin.pectoral.left", 1, AnatomicalPartKindV1.Fin, AnatomicalSideV1.Left,
                0.18, 0.38, 0.025, -0.10, 0, 0.05, GeneInfluenceV1.Appendage,
                AnatomicalJointKindV1.Hinge, -55, 55),
            Part("fin.pectoral.right", 1, AnatomicalPartKindV1.Fin, AnatomicalSideV1.Right,
                0.18, 0.38, 0.025, 0.10, 0, 0.05, GeneInfluenceV1.Appendage,
                AnatomicalJointKindV1.Hinge, -55, 55),
            Part("fin.pelvic.left", 2, AnatomicalPartKindV1.Fin, AnatomicalSideV1.Left,
                0.14, 0.36, 0.015, -0.08, -0.05, -0.03, GeneInfluenceV1.Appendage,
                AnatomicalJointKindV1.Hinge, -35, 35),
            Part("fin.pelvic.right", 2, AnatomicalPartKindV1.Fin, AnatomicalSideV1.Right,
                0.14, 0.36, 0.015, 0.08, -0.05, -0.03, GeneInfluenceV1.Appendage,
                AnatomicalJointKindV1.Hinge, -35, 35),
            Part("fin.anal", 2, AnatomicalPartKindV1.Fin, AnatomicalSideV1.Center,
                0.15, 0.35, 0.02, 0, -0.10, -0.05, GeneInfluenceV1.Appendage,
                AnatomicalJointKindV1.Hinge, -25, 25)
        });
        var sensors = Array.AsReadOnly(new[]
        {
            new SensorTemplateV1("eye.left", 1, SensoryKindV1.Vision, -0.09, 0.025, 0.07),
            new SensorTemplateV1("eye.right", 1, SensoryKindV1.Vision, 0.09, 0.025, 0.07),
            new SensorTemplateV1("lateral.line", 0, SensoryKindV1.LateralLine, 0, 0, 0)
        });
        return new("vertebrate.fish.v1", 1, CreatureFamilyV1.Fish,
            parts, Array.AsReadOnly(new[] { (6, 7), (8, 9) }), sensors);
    }
}
