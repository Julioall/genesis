# Systema Naturae — Pacote de fundação do mundo natural

> **Estado atual: N0–N2 executáveis nesta branch.** O runtime termina na flora, exclui fauna e não declara o MVP Natural completo. Consulte o [README](README.md) para comandos, cobertura validada e limites. Esta implementação parte da especificação e preserva os valores L0; os marcos posteriores continuam no roadmap.

- **Repositório-alvo:** https://github.com/Julioall/genesis.git
- **Repositório de referência (somente leitura):** https://github.com/Julioall/Spectators-of-Life
- **Data da revisão:** 21/10/2026
- **Revisão de organização:** 2.1 — ScientificFoundation passou a `docs/science/` sem projeto C#.
- **Estado:** especificação e critérios de validação para um runtime N0–N2 implementado nesta branch; os documentos de roadmap continuam descrevendo requisitos futuros.
- **Designação interna:** Systema Naturae / APMA-Natural 2.0; o nome comercial do jogo não deve aparecer em namespaces ou pastas de código.

## Missão

Construir do zero uma base C# modular, procedural e reprodutível para simular um **mundo natural vivo**, com causalidade observável entre astronomia, atmosfera, geosfera, hidrosfera e biosfera. A primeira entrega precisa ser **executável e funcional**, não apenas um conjunto de interfaces vazias ou um mapa bonito.

### Regras invioláveis

1. Projetos, assemblies, namespaces e pastas de projeto usam prefixo `Game.*`; subpastas internas podem usar nomes de domínio como `Soil` ou `Botany`; jamais `Genesis.*`, `Asterra.*` ou o título do jogo. O slug do repositório `genesis` é apenas um endereço Git; os títulos nos documentos podem citar ASTERRA — Systema Naturae.
2. O repositório novo é a fonte de verdade. O legado é referência; não efetuar merge de histórico nem copiar código web sem análise.
3. Nada de humanidade, economia, cidades, política, militar ou redes neurais no primeiro marco.
4. A fundamentação `Mathematics/Physics/Chemistry/Biology` fica em `docs/science/` e **não** é projeto C#, namespace ou assembly. Implementar funções científicas nos módulos proprietários e utilitários puros apenas quando houver reuso demonstrado.
5. Os cinco domínios são os únicos módulos principais de mundo; especialidades são submódulos internos até justificativa técnica para separação.
6. Cada estoque mutável tem um proprietário; módulos nunca escrevem diretamente em estados alheios. Compartilhar observações, propostas de fluxo, comandos e fatos, sempre com unidades, timestamp, identidade e versão.
7. Ciclos ecológicos são permitidos; ciclos de dependência de código, proibidos. Orquestração é explícita, por fases e atualizações de estados estáveis.
8. Mundos reproduzíveis por seed e versões; rendering não influencia simulação; visual procedural por código com cache.
9. Desktop C# é o primeiro alvo de referência, mas o núcleo permanece independente; teste do navegador ocorre cedo, antes de fazer promessas multiplataforma.
10. Testes científicos e de integração são requisito da entrega. Não declarar realismo ou calibração sem dados.

## Organização documental

As decisões e pesquisas ficam sob `docs/` (GDD, arquitetura, specs, ADRs e `docs/science/`). O runtime C# fica sob `src/Game.*` e não contém uma biblioteca `Game.ScientificFoundation`. Veja [`docs/README.md`](docs/README.md).

## Ponto de partida e navegação

1. Leia [`docs/00_SCOPE.md`](docs/00_SCOPE.md), [`docs/01_ARCHITECTURE.md`](docs/01_ARCHITECTURE.md), [`docs/science/README.md`](docs/science/README.md), [`docs/03_MODULES.md`](docs/03_MODULES.md) e [`docs/GDD.md`](docs/GDD.md) antes de escrever código.
2. Para cada sistema, consulte [`docs/04_OWNERSHIP.md`](docs/04_OWNERSHIP.md), [`docs/05_CONTRACTS.md`](docs/05_CONTRACTS.md), [`docs/06_FLUX_LEDGER.md`](docs/06_FLUX_LEDGER.md) e [`docs/07_SCHEDULER.md`](docs/07_SCHEDULER.md).
3. Use [`docs/20_L0_MODEL_AND_BENCHMARKS.md`](docs/20_L0_MODEL_AND_BENCHMARKS.md) como fonte única das definições numéricas; siga [`docs/12_ROADMAP.md`](docs/12_ROADMAP.md) e os critérios de [`docs/13_TESTS.md`](docs/13_TESTS.md).
4. Leia [`docs/14_WEB_GATE.md`](docs/14_WEB_GATE.md) antes de consolidar infraestrutura dependente de renderizador.
5. Siga [`docs/19_CODING_RULES.md`](docs/19_CODING_RULES.md) e registre decisões arquiteturais duráveis em [`docs/decisions/`](docs/decisions/README.md).

## Entrega final desejada (visão futura)

Uma aplicação desktop top-down com geração procedural de terreno, sol e estações, eventos meteorológicos, água superficial/solo, ao menos dois tipos funcionais de vegetação, dois perfis animais simples (herbívoro e predador), detritos/decompositores agregados, balanços auditáveis, interface de inspeção por célula/organismo, pausa/velocidade, seed, save/load e teste headless. Não forçar equilíbrio: extinção local pode ocorrer se recursos acabarem.

**Nota de escopo:** a visão acima também inclui fauna e interface completa, que não fazem parte do runtime N0–N2 atual. O estado desta branch e as limitações estão documentados no [README](README.md).

## Leitura da revisão atual

A revisão documental 2.6 alinha o roadmap ao cenário L0 v2 sem alterar seu contrato quantitativo. As equações, parâmetros, tolerâncias e metas do primeiro cenário permanecem centralizados em [modelos e metas](docs/20_L0_MODEL_AND_BENCHMARKS.md); o [parecer](docs/18_DOCUMENT_REVIEW.md) registra as revisões e as [regras de codificação](docs/19_CODING_RULES.md) orientam evoluções futuras. O runtime N0–N2 e a cobertura executada estão resumidos no [README](README.md).
