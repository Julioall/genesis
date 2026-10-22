namespace Game.Contracts;

public enum CreatureFamilyV1 : byte
{
    Quadruped = 1,
    Fish = 2
}

public enum CreatureHabitatV1 : byte
{
    Terrestrial = 1,
    Freshwater = 2,
    Marine = 3
}

public enum AnatomicalSideV1 : sbyte
{
    Center = 0,
    Left = -1,
    Right = 1
}

public enum AnatomicalPartKindV1 : byte
{
    Torso = 1,
    Neck = 2,
    Head = 3,
    Tail = 4,
    LimbProximal = 5,
    LimbDistal = 6,
    AxialSegment = 7,
    Fin = 8
}

public enum AnatomicalJointKindV1 : byte
{
    Ball = 1,
    Hinge = 2,
    FlexibleAxial = 3
}

public enum SensoryKindV1 : byte
{
    Vision = 1,
    Olfaction = 2,
    LateralLine = 3,
    Touch = 4
}

/// <summary>Closed numeric interval for a species prototype's versioned gene schema.</summary>
public readonly record struct GeneRangeV1(double Minimum, double Maximum);

/// <summary>Prototype gene envelope; units are named in each field and in SPEC-ZOO-100.</summary>
public readonly record struct CreatureGeneBoundsV1(
    GeneRangeV1 BodyScale,
    GeneRangeV1 AppendageRelativeLength,
    GeneRangeV1 TrunkRelativeLength,
    GeneRangeV1 ThermalInsulationIndex,
    GeneRangeV1 PigmentHueDegrees,
    GeneRangeV1 PatternFrequencyPerM,
    GeneRangeV1 GrowthRatePerDay);

/// <summary>Fixed, typed allele schema. Dynamic age, reserves, health and hydration are not genes.</summary>
public readonly record struct CreatureGeneValuesV1(
    double BodyScale,
    double AppendageRelativeLength,
    double TrunkRelativeLength,
    double ThermalInsulationIndex,
    double PigmentHueDegrees,
    double PatternFrequencyPerM,
    double GrowthRatePerDay);

/// <summary>Versioned lineage definition. Reference size and mass are prototype scales, not calibrated data.</summary>
public sealed record CreatureSpeciesDefinitionV1(
    string SpeciesId,
    string DisplayName,
    CreatureFamilyV1 Family,
    string BodyPlanId,
    int BodyPlanVersion,
    string GeneticsSchemaId,
    int GeneticsSchemaVersion,
    CreatureHabitatV1 Habitat,
    string ReproductiveCompatibilityGroupId,
    double ReferenceAdultBodyLengthM,
    double ReferenceAdultStructuralMassKg,
    double MaturityDays,
    CreatureGeneBoundsV1 GeneBounds);

/// <summary>Portable genome document. Only gene data, seeds, versions and pedigree IDs are persisted.</summary>
public readonly record struct CreatureGenomeV1(
    int SchemaVersion,
    int GeneratorVersion,
    string SpeciesId,
    ulong GenomeId,
    ulong AnatomySeed,
    ulong PigmentSeed,
    CreatureGeneValuesV1 Genes,
    ulong? ParentAId,
    ulong? ParentBId);

/// <summary>Normalized cumulative environmental factors supplied by later physiology/ecology integration.</summary>
public readonly record struct CreatureDevelopmentContextV1(
    double AgeDays,
    double ThermalDevelopmentFactor01,
    double NutritionalCondition01);

/// <summary>LocalOffset is relative to the parent part; root part offsets are body-local.</summary>
public readonly record struct AnatomicalPartV1(
    string PartId,
    int ParentPartIndex,
    AnatomicalPartKindV1 Kind,
    AnatomicalSideV1 Side,
    double LengthM,
    double RadiusM,
    double StructuralMassKg,
    double LocalOffsetXM,
    double LocalOffsetYM,
    double LocalOffsetZM);

public readonly record struct AnatomicalJointV1(
    int ParentPartIndex,
    int ChildPartIndex,
    AnatomicalJointKindV1 Kind,
    double MinimumAngleDegrees,
    double MaximumAngleDegrees);

public readonly record struct SensoryAnchorV1(
    string SensorId,
    int PartIndex,
    SensoryKindV1 Kind,
    double LocalOffsetXM,
    double LocalOffsetYM,
    double LocalOffsetZM);

/// <summary>Geometry-derived envelopes only; these are not calibrated speed or energy predictions.</summary>
public readonly record struct MorphologicalCapacityV1(
    double NominalStrideLengthM,
    double PropulsionSurfaceAreaProxyM2);

/// <summary>Immutable developed anatomy. Lists are defensively copied and exposed as read-only views.</summary>
public sealed class DevelopedBodyV1
{
    private readonly IReadOnlyList<AnatomicalPartV1> _parts;
    private readonly IReadOnlyList<AnatomicalJointV1> _joints;
    private readonly IReadOnlyList<SensoryAnchorV1> _sensors;

    public DevelopedBodyV1(
        string speciesId,
        string bodyPlanId,
        int bodyPlanVersion,
        CreatureFamilyV1 family,
        double developmentProgress01,
        double bodyLengthM,
        double referenceStructuralMassKg,
        double thermalInsulationIndex,
        MorphologicalCapacityV1 morphology,
        ulong structuralSignature,
        IEnumerable<AnatomicalPartV1> parts,
        IEnumerable<AnatomicalJointV1> joints,
        IEnumerable<SensoryAnchorV1> sensors)
    {
        ArgumentNullException.ThrowIfNull(parts);
        ArgumentNullException.ThrowIfNull(joints);
        ArgumentNullException.ThrowIfNull(sensors);
        SpeciesId = speciesId;
        BodyPlanId = bodyPlanId;
        BodyPlanVersion = bodyPlanVersion;
        Family = family;
        DevelopmentProgress01 = developmentProgress01;
        BodyLengthM = bodyLengthM;
        ReferenceStructuralMassKg = referenceStructuralMassKg;
        ThermalInsulationIndex = thermalInsulationIndex;
        Morphology = morphology;
        StructuralSignature = structuralSignature;
        _parts = Array.AsReadOnly(parts.ToArray());
        _joints = Array.AsReadOnly(joints.ToArray());
        _sensors = Array.AsReadOnly(sensors.ToArray());
    }

    public string SpeciesId { get; }
    public string BodyPlanId { get; }
    public int BodyPlanVersion { get; }
    public CreatureFamilyV1 Family { get; }
    public double DevelopmentProgress01 { get; }
    public double BodyLengthM { get; }
    public double ReferenceStructuralMassKg { get; }
    public double ThermalInsulationIndex { get; }
    public MorphologicalCapacityV1 Morphology { get; }
    public ulong StructuralSignature { get; }
    public IReadOnlyList<AnatomicalPartV1> Parts => _parts;
    public IReadOnlyList<AnatomicalJointV1> Joints => _joints;
    public IReadOnlyList<SensoryAnchorV1> Sensors => _sensors;
}

/// <summary>Renderer-neutral output derived only from pigment genes and the pigment random stream.</summary>
public readonly record struct CreatureVisualRecipeV1(
    int RecipeVersion,
    string SpeciesId,
    ulong StructuralSignature,
    double BaseHueDegrees,
    double PatternFrequencyPerM,
    double PatternContrast01,
    ulong PatternSeed);

public readonly record struct CreatureDevelopmentResultV1(
    DevelopedBodyV1 Body,
    CreatureVisualRecipeV1 VisualRecipe);

/// <summary>Z1 pure genetics and development boundary; no renderer or simulation state is accepted.</summary>
public interface ICreatureDevelopmentV1
{
    CreatureGenomeV1 CreateGenome(
        CreatureSpeciesDefinitionV1 species,
        ulong anatomySeed,
        ulong pigmentSeed);

    CreatureGenomeV1 CreateGenomeFromWorldSeed(
        CreatureSpeciesDefinitionV1 species,
        ulong worldSeed,
        ulong individualId);

    CreatureGenomeV1 MutateGenome(
        CreatureSpeciesDefinitionV1 species,
        CreatureGenomeV1 parent,
        ulong mutationSeed);

    CreatureDevelopmentResultV1 Develop(
        CreatureSpeciesDefinitionV1 species,
        CreatureGenomeV1 genome,
        CreatureDevelopmentContextV1 context);
}
