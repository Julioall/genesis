# Physics — princípios físicos que governam o mundo

**Natureza:** documentação; as implementações pertencem a Cosmos, Atmosphere, Geosphere e Hydrosphere conforme a propriedade do estado.

## Fenômenos do primeiro recorte

- Geometria solar, duração do dia e distribuição aproximada de energia incidente.
- Temperatura e balanços energéticos simplificados, umidade atmosférica e vento parametrizado.
- Conservação de água entre precipitação, interceptação, infiltração, reservatórios, evaporação, transpiração e escoamento.
- Movimento de água dependente de relevo, gradientes e condições de borda, sem prometer hidrodinâmica completa.
- Diferença entre estoque (por exemplo mm de água armazenada), taxa (mm/dia) e fluxo do intervalo (mm por passo).

## Fronteiras de responsabilidade

| Fenômeno | Quem executa e mantém o estado |
|---|---|
| Dia, ano, posição solar | `Game.Modules.Cosmos` |
| Temperatura, vento, precipitação | `Game.Modules.Atmosphere` |
| Solo, infiltração, água no solo | `Game.Modules.Geosphere/Soil` |
| Água superficial, rios e escoamento | `Game.Modules.Hydrosphere` |
| Demanda de transpiração e interceptação biológica | `Game.Modules.Biosphere` em cooperação por contrato |

Se a atmosfera não tem reservatório de vapor explícito, chuva é entrada de fronteira e evaporação saída de fronteira; ambas precisam aparecer na contabilidade. Consulte [Flux ledger](../06_FLUX_LEDGER.md).

## Expansões futuras

Radiação avançada, dinâmica de massas de ar, hidrogeologia completa, glaciares, marés e fluxos térmicos refinados não são exigidos no MVP.
