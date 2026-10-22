# Arquitetura de criaturas — domínio zoológico independente do renderizador

## Pastas propostas (não obrigar 1 assembly para cada pasta)

```text
src/
├── Game.Contracts/
│   ├── Biology/
│   ├── Zoology/
│   └── Ecology/
├── Game.Core/
├── Game.World/
├── Game.Modules.Biosphere/
│   ├── Zoology/
│   │   ├── Species/
│   │   ├── Genomes/
│   │   ├── Development/
│   │   ├── Anatomy/
│   │   ├── Physiology/
│   │   ├── LifeCycle/
│   │   ├── Reproduction/
│   │   └── Behavior/
│   └── Ecology/
├── Game.Procedural/
│   └── Creatures/
│       ├── Geometry/
│       ├── SurfacePatterns/
│       └── VisualRecipes/
├── Game.Rendering/
│   └── Creatures/
└── Game.Client.Desktop/

docs/
├── science/Biology/
├── specs/
├── architecture/
└── decisions/
```

`Game.Procedural` não interpreta metabolismo ou genes biológicos; gera materiais e representação visual a partir do fenótipo e do conjunto de características visuais. Se a morfogênese afetar capacidade funcional (ex.: número de patas), sua regra tem proprietário em `Zoology`, ainda que reutilize matemática genérica.

## Estados e proprietários

| Estado | Proprietário | Consumidores |
|---|---|---|
| Definição de linhagem, parâmetros hereditários e compatibilidade reprodutiva | Zoology/Species | Reproduction, Development, UI |
| Genoma, linhagem, parentesco | Zoology/Genomes | Development, Reproduction |
| Morfologia desenvolvida (comprimentos e topologia funcionais) | Zoology/Anatomy | Locomotion, Physiology, rendering adapter |
| Fase de vida, idade, massa, reservas, hidratação, saúde | Zoology/Physiology & LifeCycle | Ecology, Behavior, UI |
| Posição, habitat atual, interações espaciais | World/Spatial state | Zoology, Geosphere, Hydrosphere, UI |
| Água disponível, plantas e biomassa removível | Hidrosphere / Geosphere / Botany conforme tipo de reservatório | Zoology via contratos; transferência autorizada pelo proprietário |
| Intenção de movimento, alimentação ou reprodução | Zoology/Behavior ou PlayerCommands | Kernel resolver |
| Geometria e textura visual, cache, sombras e animação gráfica | Game.Procedural / Rendering | Cliente |

**Atenção:** um organismo controlado pelo jogador e outro por IA usam as mesmas regras metabólicas, de reprodução e de conservação de matéria. Só o produtor de intenções muda. Regras de controle humano/social permanecem fora do escopo natural.

## Contratos mínimos

Os contratos de espécie, genoma e corpo estão implementados em `Game.Contracts/CreatureGeneticsContracts.cs`; `ICreatureDevelopmentV1` produz os corpos headless dos protótipos Z1. `ICreaturePoseSolverV1` recebe `DevelopedBodyV1` e calcula pose renderer-neutral. `ICreatureGeometryBuilderV1` produz superfícies-fonte por parte e, após a pose, uma superfície conectada de renderização por união implícita dos volumes anatômicos e conectores pai-filho em `Game.Procedural`, sem dependência do renderer. A primeira pose cobre IK de dois segmentos para quadrúpedes e ângulos axiais limitados para peixe. Nenhuma dessas interfaces recebe estado de mundo ou agente.

O fragmento abaixo permanece pseudocódigo de evolução, não é a assinatura atual dos tipos versionados.

```csharp
public readonly record struct SpeciesId(string Value);
public readonly record struct CreatureId(ulong Value);
public readonly record struct GenomeVersion(int Major, int Minor);

public sealed record SpeciesDefinition(
    SpeciesId Id,
    string BodyPlanId,
    string GeneticsSchemaId,
    string EcologicalProfileId);

public sealed record GenomeDocument(
    GenomeVersion Version,
    SpeciesId Species,
    ulong AnatomySeed,
    ulong PigmentSeed,
    IReadOnlyDictionary<string, double> Genes);

public interface ICreatureDevelopmentV1
{
    DevelopedBody Develop(SpeciesDefinition species, GenomeDocument genome,
                          DevelopmentContext environment);
}

public interface ICreatureVisualRecipeV1
{
    VisualRecipe Build(DevelopedBody body, PigmentProfile pigment,
                       ulong visualSeed);
}
```

Tipos auxiliares acima são placeholders de contrato a definir, não código que compila isoladamente. Units devem ser tipos de valor ou anotação formal: `LengthM`, `MassKg`, `EnergyJ`, `TimeS` etc. Não serializar interface, GPU buffer ou sprite no save.

## Processo causal por tick (proposta, a testar)

1. Ler snapshot físico/climático do intervalo anterior.
2. Atualizar necessidade metabólica/estado fisiológico do organismo (reservas, perda de água, temperatura aproximada).
3. Processar percepção e formular intenções (não transferir recursos ainda).
4. Resolver deslocamento, interação e acesso espacial ao recurso.
5. Solicitar consumo/transferência ao proprietário de água ou biomassa; consolidar sem gasto duplo.
6. Atualizar assimilação, crescimento, doença, reprodução e mortalidade após as transferências confirmadas.
7. Registrar mortes/detritos, liberar recursos para Ecology e preparar snapshot final.

Loops ecológicos (animal come planta; planta cresce com nutrientes produzidos por decomposição) são **causalmente cíclicos em tempo**, mas não envolvem importações circulares de assemblies.

## Persistência e determinismo

Salvar schema e versão de espécie, genoma, estado anatômico quando necessário, fisiologia dinâmica, estado do controlador, estado do RNG, IDs e genealogia. Na migração, não inferir que o mesmo genoma produzirá geometria idêntica após alterar o gerador: pin de versão e migração explícita. Câmera, FPS e distância de render não alteram a vida do animal.
