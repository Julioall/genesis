# Natural World L0

Implementação incremental em C#/.NET com núcleo headless e um cliente desktop MonoGame. Este repositório implementa a base determinística (N0), o cenário físico L0 (N1) e patches de gramíneas/lenhosas com sementes e decomposição agregada (N2), além de uma janela mínima para inspecionar o mundo.

**Esta etapa não implementa fauna e não declara o MVP natural completo.** Herbívoros e predadores continuam descritos como escopo futuro nos documentos do projeto; não há entidades, classes ou processos animais no runtime desta entrega.

## Executar

Requer .NET SDK 10.0. O núcleo e o runner não dependem de MonoGame.

```powershell
dotnet restore Game.sln
dotnet build Game.sln -c Release
dotnet test Game.sln -c Release
dotnet run --project src/Game.App/Game.App.csproj -c Release -- --seed 1 --ticks 1440 --save world.json
dotnet run --project src/Game.App/Game.App.csproj -c Release -- --load world.json --ticks 144
dotnet run --project src/Game.Client.Desktop/Game.Client.Desktop.csproj -c Release
```

O runner usa `temperate-valley-l0-v15`, seed 1 por padrão e ticks de 10 minutos. A janela começa a 6 ticks simulados por segundo. Teclas `1/2/3` alternam o fundo entre relevo, água do solo e biomassa; `Space` pausa; `+/-` muda a velocidade; `F5/F9` salva/carrega em `%LOCALAPPDATA%/Game/natural-world-save.json`. Passe o mouse sobre uma célula para inspecionar seus campos.

## O que está implementado

- Geração determinística 64×64 por seed, calendário, geometria solar, temperatura, chuva por zona, relevo, perfis de solo, infiltração, água superficial, evaporação, escoamento de borda e respectivos fluxos de água.
- Gramíneas e plantas lenhosas por patch/célula, crescimento horário condicionado a PAR, água e N, banco/dispersão/germinação de sementes, senescência, detritos e mineralização/decomposição diária.
- Livro agregado para água, matéria seca equivalente e N; snapshots de leitura para a apresentação; save/load JSON com validação básica e continuação determinística.
- Malha 2.5D com altitude geométrica, chunks contínuos e afloramentos com paredes laterais; a cobertura vegetal e os rios continuam ligados ao snapshot físico.
- Imagens procedurais em memória para terreno, pedras, gramíneas, troncos e folhas sazonais. A aparência usa uma seed visual separada e não altera a simulação. Nenhum pack de imagens é obrigatório.

Os números e as simplificações são os parâmetros heurísticos L0 documentados; não representam calibração de campo. Consulte [equações, limites e metas](docs/20_L0_MODEL_AND_BENCHMARKS.md), [pesquisa e decisões visuais](docs/15_RESEARCH.md) e [roadmap](docs/12_ROADMAP.md).

## Estrutura e documentação

- [Índice dos documentos](docs/README.md)
- [Terreno geométrico 2.5D](docs/21_GEOMETRIC_TERRAIN.md)
- [Arquitetura e marcos](docs/01_ARCHITECTURE.md) · [ADR-0001](docs/decisions/ADR-0001-Science-Is-Documentation.md) · [ADR-0002](docs/decisions/ADR-0002-L0-runtime-and-presentation.md)
- [Síntese visual do Consensus](docs/research/consensus-procedural-flora.md)
- [Plano de testes](docs/13_TESTS.md)

O código não depende de `../Spectators-of-Life`. O checkout legado foi tratado somente como referência de leitura; os algoritmos e assets daquela árvore não foram copiados.
