# ADR-0002 — composição headless L0 e apresentação MonoGame

- **Status:** princípios científicos e stack MonoGame vigentes; restrições visuais antigas atualizadas por ADR-0004
- **Data:** 21/10/2026

> Este registro preserva a decisão e o contexto da implementação inicial. ADR-0004 reafirma MonoGame como stack obrigatória e atualiza as opções de câmera e estilo; composição headless, snapshots e isolamento da simulação permanecem vigentes.

## Contexto

O contrato documental propõe uma implementação incremental em C#, com estado físico/biológico proprietário, simulação independente da câmera e renderização por snapshots. Esta solicitação autoriza N0–N2 e exige uma janela visível, mas exclui fauna. O pacote anterior é um projeto TypeScript/Phaser com mudanças locais não commitadas, e seus assets e procedência variam por módulo.

## Decisão

- Manter C#/.NET e usar `MonoGame.Framework.DesktopGL` para a janela mínima; o núcleo, runner e testes não referenciam MonoGame.
- Compor as implementações de domínio explicitamente no `Game.Core`. Os módulos compartilham DTOs em `Game.Contracts`; o formato salvo carrega um `schemaVersion`. Módulos não referenciam implementações de outros domínios. O Core agenda processos e publica snapshots; os processos científicos permanecem em Cosmos, Atmosphere, Geosphere, Hydrosphere e Biosphere.
- Persistir o estado necessário à continuação, inclusive relógio, estoque de solo/água/flora, acumuladores diários, idade de chuva por zona e previsão horária já estabelecida. Campos renderizados são cópias de leitura.
- Gerar silhuetas e pixels por código em cache da sessão. Variações visuais são derivadas por uma função separada da seed ecológica. Sazonalidade de folhas é somente aparência; não representa estoque de folhas nem altera a simulação.
- Rochas visuais são uma composição determinística baseada em elevação e variação visual; não representam estoque geológico L0, colisão nem recurso mineral.
- Não incluir fauna, nem classes de fase futura. A documentação biológica permanece como intenção futura do roadmap.
- Aplicar valores e unidades da ficha versionada `temperate-valley-l0-v17`.

## Alternativas consideradas

- Migrar para web/Phaser: descartado porque viola o stack explícito deste projeto.
- Reutilizar motores ou PNGs de `Spectators-of-Life`: descartado nesta etapa porque o cliente foi solicitado sem dependência de pacotes de arte; algoritmos de suavização visual podem ser adaptados sem introduzir o runtime do projeto de referência.
- Renderer com folhas/galhos como entidades persistentes: descartado; acrescentaria estado sem uso na simulação L0 e contrariaria a representação agregada por patch.

## Consequências e evidência

- O cliente pode ser iniciado sem carregar arquivos de arte e lê `WorldSnapshot`; cache e desenho não recebem referência ao RNG ecológico nem alteram fluxos.
- `dotnet build Game.sln` e os testes determinísticos cobrem a compilação, reprodução no mesmo runtime, fechamento dos livros implementados, save/load e respostas de Botany. A disponibilidade de browser e metas de FPS não são certificadas por esta decisão.
- O modelo permanece heurístico L0. Ele não é calibração de espécie, balanço global do carbono nem validação ecológica de campo.

## Critério de revisão

Revisar quando um gate N0–N2 não puder ser satisfeito por essa separação, quando surgir procedência/licença específica que autorize reutilização, ou quando um estudo/experimento justificadamente mudar o contrato quantitativo. A mudança de qualquer valor L0 exige primeiro atualizar `docs/20_L0_MODEL_AND_BENCHMARKS.md`.
