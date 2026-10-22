using System.Xml.Linq;
using System.Text.Json;
using Game.Contracts;
using Game.Modules.Biosphere;
using Game.Modules.Geosphere;
using Game.Modules.Hydrosphere;

namespace Game.Tests;

public sealed class ModuleArchitectureTests
{
    [Fact]
    public void Core_references_only_contracts_and_domain_modules_do_not_reference_each_other()
    {
        var root = FindRepositoryRoot();
        var coreReferences = ProjectReferences(Path.Combine(root, "src", "Game.Core", "Game.Core.csproj"));
        Assert.Equal(["Game.Contracts"], coreReferences);

        var moduleProjects = Directory.GetFiles(Path.Combine(root, "src"), "Game.Modules.*.csproj",
            SearchOption.AllDirectories);
        Assert.NotEmpty(moduleProjects);
        foreach (var project in moduleProjects)
        {
            var references = ProjectReferences(project);
            Assert.All(references, reference => Assert.Contains(reference,
                new[] { "Game.Contracts", "Game.Procedural" }));
        }
    }

    [Fact]
    public void Geosphere_and_hydrosphere_public_boundaries_do_not_expose_the_aggregate_world_cell()
    {
        var physicalCapabilities = new[]
        {
            typeof(IGeosphereTerrainV1),
            typeof(ISoilWaterV1),
            typeof(ISoilBiogeochemistryV1),
            typeof(ISurfaceWaterV1)
        };

        foreach (var capability in physicalCapabilities)
        foreach (var method in capability.GetMethods())
        {
            Assert.DoesNotContain(typeof(WorldCell), ExposedTypes(method.ReturnType));
            Assert.All(method.GetParameters(), parameter =>
                Assert.DoesNotContain(typeof(WorldCell), ExposedTypes(parameter.ParameterType)));
        }

        var physicalAssemblies = new[] { typeof(GeosphereModuleV1).Assembly, typeof(HydrosphereModuleV1).Assembly };
        foreach (var assembly in physicalAssemblies)
        foreach (var type in assembly.GetExportedTypes())
        foreach (var method in type.GetMethods(System.Reflection.BindingFlags.Public
                                               | System.Reflection.BindingFlags.Instance
                                               | System.Reflection.BindingFlags.Static
                                               | System.Reflection.BindingFlags.DeclaredOnly))
        {
            Assert.DoesNotContain(typeof(WorldCell), ExposedTypes(method.ReturnType));
            Assert.All(method.GetParameters(), parameter =>
                Assert.DoesNotContain(typeof(WorldCell), ExposedTypes(parameter.ParameterType)));
        }
    }

    [Fact]
    public void Roadmap_module_manifests_declare_existing_capabilities()
    {
        var root = FindRepositoryRoot();
        var directory = Path.Combine(root, "docs", "module-manifests");
        var manifests = Directory.GetFiles(directory, "*.json").Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(11, manifests.Length);

        var contractTypes = typeof(ISolarForcingV1).Assembly.GetTypes().Select(type => type.Name).ToHashSet();
        foreach (var path in manifests)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var manifest = document.RootElement;
            Assert.Equal(1, manifest.GetProperty("manifestVersion").GetInt32());
            var expectedPhase = Path.GetFileName(path).Split('.')[1].ToUpperInvariant();
            Assert.Equal(expectedPhase, manifest.GetProperty("roadmapPhase").GetString());
            var moduleId = manifest.GetProperty("moduleId").GetString();
            Assert.True(moduleId?.StartsWith("Game.Modules.", StringComparison.Ordinal) == true
                        || moduleId?.StartsWith("Game.Procedural.", StringComparison.Ordinal) == true,
                $"Manifest {path} must identify an implemented module owner.");
            Assert.NotEmpty(manifest.GetProperty("ownedProcesses").EnumerateArray());
            foreach (var capability in manifest.GetProperty("providesCapabilities").EnumerateArray())
            {
                var capabilityName = capability.GetString()
                    ?? throw new InvalidDataException($"Manifest {path} contains an empty capability name.");
                Assert.Contains(capabilityName, contractTypes);
            }
        }
    }

    [Fact]
    public void Biosphere_public_boundaries_do_not_expose_the_aggregate_world_cell()
    {
        foreach (var capability in new[] { typeof(IBotanyV1), typeof(IEcologyV1) })
        foreach (var method in capability.GetMethods())
        {
            Assert.DoesNotContain(typeof(WorldCell), ExposedTypes(method.ReturnType));
            Assert.All(method.GetParameters(), parameter =>
                Assert.DoesNotContain(typeof(WorldCell), ExposedTypes(parameter.ParameterType)));
        }

        var assembly = typeof(BiosphereModuleV1).Assembly;
        foreach (var type in assembly.GetExportedTypes())
        foreach (var method in type.GetMethods(System.Reflection.BindingFlags.Public
                                               | System.Reflection.BindingFlags.Instance
                                               | System.Reflection.BindingFlags.Static
                                               | System.Reflection.BindingFlags.DeclaredOnly))
        {
            Assert.DoesNotContain(typeof(WorldCell), ExposedTypes(method.ReturnType));
            Assert.All(method.GetParameters(), parameter =>
                Assert.DoesNotContain(typeof(WorldCell), ExposedTypes(parameter.ParameterType)));
        }
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Game.sln"))) return directory.FullName;

        throw new DirectoryNotFoundException("Could not locate Game.sln from the test output directory.");
    }

    private static string[] ProjectReferences(string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        return XDocument.Load(projectPath)
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value
                ?? throw new InvalidDataException($"Project reference in {projectPath} has no Include attribute."))
            .Select(include => include.Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar))
            .Select(include => Path.GetFileNameWithoutExtension(Path.GetFullPath(Path.Combine(projectDirectory, include))))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<Type> ExposedTypes(Type type)
    {
        yield return type;
        if (type.HasElementType && type.GetElementType() is { } elementType)
        {
            foreach (var nestedType in ExposedTypes(elementType)) yield return nestedType;
        }

        if (type.IsGenericType)
        foreach (var argument in type.GetGenericArguments())
        foreach (var nestedType in ExposedTypes(argument))
            yield return nestedType;
    }
}
