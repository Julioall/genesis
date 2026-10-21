# Contratos científicos, capacidades e evolução sem quebra

## Semântica antes de assinatura

Todo contrato deve descrever `id`, `major/minor`, owner, grandeza física, unidade, concentração/estoque/taxa, resolução espacial, coordenadas, horizonte/janela `(t0,t1]`, instante de validade, representação de ausência, precisão e interpretação. Uma `precipitationMm` de chuva **acumulada durante uma hora** difere de `precipitationMmPerDay` (taxa), mesmo quando ambas usam `double`.

**Canais:** (a) `snapshots` de campos imutáveis, (b) pedidos/comandos por lotes ao owner, (c) confirmações/recibos com ID, (d) eventos discretos de fatos confirmados. Não usar um EventBus para transmitir cada célula de temperatura.

## Capacidades pequenas

- `IClimateBasicV1` publica campos de temperatura, umidade do ar, precipitação e radiação efetiva.
- `IWindFieldV1` ou `ICloudCoverV1` são capacidades adicionáveis.
- `ISoilWaterV1` publica água retida/capacidade e aceita pedidos de retirada.
- `ISoilNutrientsV1` publica nitrogênio disponível e recebe pedidos de retirada/deposição com recibo.
- `ISoilOrganicMatterV1` publica matéria orgânica/transformações de estoque.
- `IVegetationCoverageV1`, `IBiomassResourceV1`, `ISurfaceWaterV1` etc. são capacidades que podem existir independentemente.

Não criar interface única `IEnvironment` com 100 campos, nem um dicionário `Dictionary<string,object>` como API pública padrão. Manter matrizes numéricas compactas por chunk. O manifesto informa o que cada módulo **provê**, o que **requer** e o que apenas **usa se disponível**.

## Compatibilidade e escala semântica

- Acrescentar contrato separado preserva consumidores existentes. Campo opcional só é compatível se o formato e os consumidores aceitarem ausência/campos desconhecidos; assinaturas C# precisam de verificação própria.
- Acrescentar campo obrigatório à mesma interface pública C#: potencialmente quebrador; usar outra interface ou versão.
- Mudar mm acumulado para mm/h, coeficiente por dia para coeficiente por tick, ou redefinir umidade de gravimétrica para volumétrica: **incompatível**; criar V2/adaptador/migração.
- Dado opcional ausente: `unavailable` ou `NotSupported` com política explícita; não substituir por zero salvo quando fisicamente equivalente e documentado.
- `minor` não altera o significado de uma capacidade existente; `major` troca semântica; save schema e generatorVersion têm versionamento independente de API.

## Exemplo C# ilustrativo — não é código já implementado

```csharp
// Tipos comuns simplificados no mesmo namespace apenas para este exemplo.
namespace Game.Contracts;

public readonly record struct SimulationWindow(long StartTick, long EndTick);
public readonly record struct RegionId(int X, int Y);

public readonly record struct ClimateBasicSampleV1(
    double AirTemperatureC,
    double RelativeHumidityFraction,
    double PrecipitationAccumulatedMm,
    double SurfaceRadiationAccumulatedMJPerM2,
    SimulationWindow Window);

public interface IClimateBasicV1
{
    ClimateBasicSampleV1 Read(RegionId region, SimulationWindow window);
}

public interface IWindFieldV1
{
    // A unidade da velocidade é m/s. Direção em coordenadas cartesianas definidas.
    (double EastMS, double NorthMS) ReadWind(RegionId region, SimulationWindow window);
}

public interface ISoilWaterV1
{
    SoilWaterSampleV1 Read(CellId cell, long tick);
    void SubmitBatch(ReadOnlySpan<WaterDemandV1> demands);
}

public readonly record struct CellId(int X, int Y);
public readonly record struct SoilWaterSampleV1(
    double StoredWaterMm, double CapacityMm, long Tick);
public readonly record struct WaterDemandV1(
    ulong RequestId, ulong ConsumerId, CellId Cell,
    double RequestedMm, long Tick);

// Reserva provisória da fase 5; consumo final é confirmado na fase 6.
public readonly record struct ReservedWaterV1(
    ulong RequestId, double ReservedMm, long Tick);
```

**Nota:** `ReadOnlySpan<T>` como fronteira pública precisa de contrato de lifetime, plataforma e padrão de uso síncrono; para um transporte versionado entre processos, preferir DTOs/arrays adequados. O exemplo comunica fronteiras, não a implementação ideal de runtime.

## Testes obrigatórios

1. Manter consumidor de `IClimateBasicV1` inalterado ao adicionar uma capacidade em fixture de teste; validar mesma semântica v1. Não criar 10 ou 20 variáveis fictícias nem carregador de plugins para esse teste.
2. Acrescentar `ISoilOrganicMatterV1` sem alterar o código dos consumidores de `ISoilWaterV1`.
3. Recusar requisição de unidade/tempo incompatível com erro descritivo antes do tick.
4. Desativar capacidade opcional e confirmar que o consumidor usa aproximação declarada ou não inicia aquela mecânica.
5. Substituir implementação do submódulo Soil mantendo os contratos consumidos.
6. Executar contract tests para versão dos schemas, IDs e zero event duplication.

## Limite do exemplo e concessões

O trecho C# acima é ilustrativo de fronteiras, sem resolver composição, roteamento de recibos ou validação. Não é biblioteca pronta nem autorização para criar código nesta revisão. Em implementação futura, organizar os tipos espaciais/temporais comuns em contratos compartilhados; domínios não devem depender semanticamente de Climate para identificar célula.

`SubmitBatch` copia/consome o span sincronamente; não armazena referência para uso posterior. Recibos são roteados ao consumidor por chave de tick/fase/processo/pedido e incluem concessão, uso confirmado, rejeição ou devolução. Consultas de janela indisponível falham explicitamente. Contratos mínimos de clima/solo/biomassa são obrigatórios para a ecologia L0; opcionais cobrem extensões, sem tornar N ou alimento dispensáveis silenciosamente. Compatibilidade compilada vale para capacidades efetivamente publicadas; o MVP não exige distribuição binária ou plugins externos.
