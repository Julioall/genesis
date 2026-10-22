# Genesis — simulador observacional de um mundo natural

Genesis é um simulador determinístico em C#/.NET 10, com núcleo headless e cliente desktop MonoGame. A proposta é que o jogador crie um mundo por seed, acompanhe água, vegetação e fauna, controle o tempo e entenda as causas dos acontecimentos. O cliente atual oferece pausa, aceleração, navegação e inspeção de snapshots; histórico explicativo de eventos ainda é uma meta. A simulação é proprietária do estado; a apresentação lê snapshots.

## Estado deste checkout

Este checkout inclui código e documentação. Nos registros de 22/10/2026, N0–N3 estão implementados e seus gates L0 foram validados. O cliente e a integração N4 existem, mas a avaliação visual continua aberta: desempenho foi medido nesta máquina, enquanto a rubrica visual ainda não atingiu o aceite. Portanto, o MVP Natural completo ainda não está aprovado. Consulte o [índice de baselines e evidências](docs/reports/README.md) e o [roadmap](docs/12_ROADMAP.md).

O cenário canônico usa **64 × 64 células**, cada uma com **100 m²** (células de 10 m por lado), e ticks de 10 minutos. O commit auditado `c6aecbc` usou uma grade experimental de 256 × 256; o checkout atual `dd2cf47` voltou à grade canônica de 64 × 64. Resultados desses dois tamanhos não são comparáveis sem identificar cenário e commit.

## O que está implementado

- Mundo ambiental determinístico com calendário, geometria solar, clima prescrito, relevo, solos, água superficial e subterrânea, infiltração, evaporação e roteamento conservativo.
- Gramíneas e plantas lenhosas em patches, crescimento condicionado por luz, água e nitrogênio, sementes, senescência e reciclagem agregada.
- Zoology L0 com perfis funcionais de herbívoro e predador, IDs persistentes, busca local, movimento, consumo finito, metabolismo, hidratação, predação, reprodução, mortalidade e transferências de matéria, nitrogênio e água.
- Cenário headless `--n3-scenario` com 40 herbívoros e 4 predadores. A evidência registrada cobre três seeds por 365 dias, conservação e continuação por save/load.
- Saves JSON versionados, snapshots de leitura e cliente MonoGame com relevo e flora procedurais, câmera, inspeção, pausa, aceleração e ferramentas de avaliação.
- Protótipos zoológicos Z1–Z5 de genoma, desenvolvimento, locomoção, energia e geometria visual. Eles não equivalem a espécies calibradas nem fazem anatomia/genética controlar a ecologia N3; veja os relatórios Z em `docs/reports/`.

## Limites importantes

- A Hydrosphere calcula topologia de bacias e roteia água superficial, mas ainda não simula lagos como reservatórios que sobem até a soleira e extravasam. Uma superfície ou cachoeira de apresentação não comprova fluxo hidrológico simulado.
- As plantas continuam agregadas por patches. As formas arbóreas do renderer são arquétipos visuais; não representam cinco espécies ou populações ecológicas distintas.
- Mortes animais transferem matéria para detritos agregados. Ainda não há entidade persistente de carcaça, consumo posterior por carniceiros ou ossada visível.
- O domínio não modela um oceano. Qualquer costa ou mar desenhado pertence à apresentação e não representa um ciclo hídrico oceânico.
- Parâmetros L0 são heurísticos, não calibração de campo. Não há política neural obrigatória nem treinamento on-line de animais.

## Executar

Requer o .NET SDK 10.0.

```powershell
dotnet restore Game.sln
dotnet build Game.sln -c Release
dotnet test Game.sln -c Release
dotnet run --project src/Game.App/Game.App.csproj -c Release -- --seed 1 --ticks 144 --save world.json
dotnet run --project src/Game.App/Game.App.csproj -c Release -- --load world.json --ticks 144
dotnet run --project src/Game.App/Game.App.csproj -c Release -- --n3-scenario --seed 1 --ticks 52560 --save n3-seed-1.json
dotnet run --project src/Game.Client.Desktop/Game.Client.Desktop.csproj -c Release -- --seed 1
```

O cliente aceita `--visual-evaluation` para inspeção e captura. Os parâmetros de câmera, snapshots, benchmark e as evidências reproduzíveis estão descritos no [relatório de integração N4](docs/reports/N4_VISUAL_INTEGRATION_2026-10-22.md). O runner usa `temperate-valley-l0-v17` por padrão; o cenário N3 é opt-in.

## Documentação principal

- [Índice da documentação](docs/README.md) · [visão de produto](docs/GDD.md) · [escopo](docs/00_SCOPE.md)
- [Modelo biológico e estado de implementação](docs/09_BIOLOGICAL_MVP.md) · [roadmap](docs/12_ROADMAP.md)
- [Modelo L0 e benchmarks](docs/20_L0_MODEL_AND_BENCHMARKS.md) · [direção visual MonoGame](docs/22_VISUAL_DIRECTION_AND_TECHNOLOGY.md)
- [Índice de baselines e relatórios](docs/reports/README.md) · [decisões arquiteturais](docs/decisions/README.md)
