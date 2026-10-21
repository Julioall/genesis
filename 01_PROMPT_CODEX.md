# PROMPT MESTRE PARA CODEX — fundar o mundo natural em `Julioall/genesis`

> **Etapa atual: validação documental.** Este pacote não autoriza implementação. O prompt mestre só poderá ser executado mediante solicitação explícita posterior; nesta etapa, não criar solution, projetos, runtime, commits de implementação ou PR. Para limites revisados, consultar `docs/18_DOCUMENT_REVIEW.md`, `docs/19_CODING_RULES.md`, `docs/22_VISUAL_DIRECTION_AND_TECHNOLOGY.md` e ADR-0004.

Você é o responsável pela arquitetura, implementação incremental, revisão científica e validação do novo repositório **https://github.com/Julioall/genesis.git**. O projeto poderá mudar de nome. Portanto, **jamais use o nome do jogo, `Genesis` ou `Asterra` em nomes de projetos C#, assemblies, namespaces, pastas `src/`, classes de infraestrutura ou identificadores de domínio**. Use apenas prefixos estáveis `Game.*`, como `Game.Core`, `Game.Contracts`, `Game.Modules.Atmosphere` etc. Os títulos de documentos podem descrever a visão "ASTERRA — Systema Naturae".

## Missão inegociável

Criar uma base técnica limpa para um mundo procedural, dinâmico e cientificamente estruturado, em que **variáveis ambientais desencadeiam efeitos em outras variáveis**, formando redes de causa e efeito auditáveis. Não é objetivo criar uma simulação científica completa da Terra; é construir **uma ecologia jogável coerente**, cuja profundidade possa crescer sem quebrar a arquitetura.

**Primeira entrega externa:** um **mundo natural funcional, simulando vida**. Não aceite como entrega final apenas solution vazia, diagrama, motor de terreno ou plantas estáticas. Aplique as fases internas do roadmap até uma integração demonstrável entre ambiente, vegetação, herbívoros, predadores e decomposição, ainda que tudo comece em baixo grau de fidelidade.

## Repositórios

- **Novo, única fonte de verdade:** `Julioall/genesis` (verificar branch, arquivos locais e estado remoto antes de começar).
- **Anterior, referência exclusivamente:** `Julioall/Spectators-of-Life` (TypeScript/Phaser, cenários naturais, modelo climático/hídrico, testes determinísticos, arte, documentação e algoritmos).
- Não copiar histórico, não migrar a estrutura web, não adicionar links de código ativo para classes Phaser/TypeScript.
- Pode examinar conceitos e fixtures antigas, documentando origem, limitações e licenças. Não transpor fórmulas sem entender unidades, massa/energia, escala temporal e comportamento.

## Git e controle de mudanças

1. Verifique estado e branch `main` do novo repositório.
2. Crie **`codex/natural-world-foundation`** a partir da `main`, salvo se já existir; não force push nem sobrescreva trabalho.
3. Faça commits coesos ao completar cada marco e mantenha o README atualizado.
4. Crie PR **draft** para `main` ao final, sem merge automático.
5. Se encontrar erros de permissão, descreva-os e mantenha resultados verificáveis localmente; jamais alegue push concluído sem confirmação.

## Ordem obrigatória de leitura

`00_COMECE_AQUI.md`, `docs/README.md`, `docs/GDD.md`, `docs/decisions/ADR-0001-Science-Is-Documentation.md`, `docs/decisions/ADR-0004-visual-direction-and-renderer-evaluation.md`, `docs/science/Mathematics.md`, `docs/science/Physics.md`, `docs/science/Chemistry.md`, `docs/science/Biology.md`, `docs/00_SCOPE.md`, `docs/01_ARCHITECTURE.md`, `docs/science/README.md`, `docs/03_MODULES.md`, `docs/04_OWNERSHIP.md`, `docs/05_CONTRACTS.md`, `docs/06_FLUX_LEDGER.md`, `docs/07_SCHEDULER.md`, `docs/08_PHYSICAL_MVP.md`, `docs/09_BIOLOGICAL_MVP.md`, `docs/10_PROCEDURAL.md`, `docs/11_DETERMINISM_AND_SAVES.md`, `docs/12_ROADMAP.md`, `docs/13_TESTS.md`, `docs/14_WEB_GATE.md`, `docs/15_RESEARCH.md`, `docs/16_LEGACY.md`, `docs/17_TEMPLATES.md`, `docs/18_DOCUMENT_REVIEW.md`, `docs/19_CODING_RULES.md`, `docs/20_L0_MODEL_AND_BENCHMARKS.md`, `docs/22_VISUAL_DIRECTION_AND_TECHNOLOGY.md`.

Antes de implementar cada processo do primeiro cenário, use `docs/20_L0_MODEL_AND_BENCHMARKS.md` como fonte única das equações, parâmetros, tolerâncias e metas mensuráveis. Se a ficha do processo estiver incompleta para o marco, completar apenas essa ficha antes de codificar; não adicionar processos adiados. Use os documentos já presentes em `docs/`, preservando os caminhos relativos e as revisões locais. Não substituir por cópias antigas do pacote. Mantenha `docs/GDD.md`, `docs/science/`, `docs/specs/` e `docs/decisions/` separados do código-fonte. O prompt e o manifesto são materiais de operação, não parte do runtime. Mantenha ADRs.

## Separação obrigatória entre documentação e código

O novo repositório deve ter `docs/GDD.md`, `docs/01_ARCHITECTURE.md`, `docs/specs/`, `docs/decisions/` e `docs/science/{Mathematics,Physics,Chemistry,Biology}.md`. A pasta `docs/science` não é um módulo de runtime. Não criar `src/Game.ScientificFoundation`, DLL, assembly, namespace ou `.csproj` para essa taxonomia. Cada processo científico documentado é implementado dentro de seu domínio `Game.Modules.*`, podendo usar utilitários puros quando o reuso for real. Leia a ADR-0001.

## Pilha e composição

- **C# / .NET** é a stack do núcleo headless; **MonoGame** é obrigatório no cliente gráfico e não é dependência dos módulos científicos. O projeto permanece nessa stack para que o desenvolvimento seja produzido por IA dentro de uma base única. A técnica visual, câmera e composição permanecem abertas para validação no MonoGame, conforme `docs/22_VISUAL_DIRECTION_AND_TECHNOLOGY.md`.
- Projetos previstos, criados incrementalmente quando exercitados: `Game.Contracts`, `Game.Core`, `Game.World`, `Game.Procedural`, `Game.Persistence`, `Game.Modules.Cosmos`, `Game.Modules.Atmosphere`, `Game.Modules.Geosphere`, `Game.Modules.Hydrosphere`, `Game.Modules.Biosphere`, `Game.Rendering`, `Game.Client.Desktop`, testes e ferramentas.
- Use `Game.Services.*` só quando serviço tiver função específica; evite um `Game.Services` central que concentre responsabilidades.
- Em cada módulo principal, **submódulos internos especializados**; não criar assemblies de cada variável meteorológica/espécie.
- Projetos de domínio referenciam Contracts e, quando justificado, utilitários técnicos compartilhados; jamais outras implementações de domínio. Não criar `Game.ScientificFoundation` como projeto.
- `docs/science/{Mathematics,Physics,Chemistry,Biology}.md` descreve fundamentos, hipóteses, equações, unidades e limites dos modelos; **é documentação, não código**. Implementar as regras físicas e biológicas dentro de `Game.Modules.*` conforme a responsabilidade definida.
- A composição explícita fica nos executáveis; o Kernel recebe contratos, registra, valida, agenda, resolve capacidades/solicitações e publica snapshots; **não calcula fenômenos científicos**.

## Organização dos cinco domínios (MVP vs futuro)

1. **Cosmos:** `OrbitalMechanics`, `SolarRadiation`, `LunarDynamics`, `AstronomicalCycles`. Implementar agora relógio, dia/noite, estações astronômicas e geometria solar simplificada; órbitas complexas e marés ficam para depois. A geometria solar acima da atmosfera é Cosmos; atenuação por nuvens é Atmosphere.
2. **Atmosphere:** `Thermodynamics`, `AtmosphericCirculation`, `HumidityAndClouds`, `Precipitation`. Implementar agora temperatura por altitude/estação, precipitação e umidade como proxy; vento e advecção ficam fora do cenário L0, sem afirmar simular meteorologia 3D.
3. **Geosphere:** `Terrain`, `Geology`, `Soil`, `Geomorphology`. Implementar agora relevo, substratos simplificados, retenção de água no solo, matéria orgânica e nitrogênio agregado; erosão avançada/tectônica ficam para depois.
4. **Hydrosphere:** `SurfaceWater`, `Groundwater`, `SnowAndIce`, `WaterQuality`. Implementar agora lâmina superficial, infiltração, escoamento local, depressões e saída de borda; canais/rios persistentes, água subterrânea e qualidade detalhada ficam para evolução posterior. **Neve/gelo acumulados ficam aqui**, precipitação de neve é Atmosphere, congelamento físico do solo fica em Geosphere.
5. **Biosphere:** `Botany`, `Zoology`, `Ecology`. Implementar agora gramíneas e árvores (ou arbustos) por grupos funcionais, fotossíntese/crescimento simplificados, sementes/reprodução, herbivoria, predadores, decomposição agregada, retorno de nutrientes e competição. Sem doença complexa, aprendizado neural ou genética evolutiva completa.

## Primeiro mundo: comportamento mínimo observável

- Mundo procedural por seed com 2+ habitats contrastantes; sua representação visual deve comunicar relevo, água, volume, vegetação e profundidade. 2D, híbrido e 3D estilizado são opções dentro do MonoGame até o gate comparativo.
- Dia/noite, calendário/estação, energia solar e precipitação espacial; chuva escoa e infiltra; solo retém água.
- Plantas dependem de água, luz, temperatura e nutrientes; acumulam biomassa, dispersam sementes, reproduzem, envelhecem e morrem.
- Herbívoros se deslocam e se alimentam da biomassa disponível; consumo reduz alimento e recursos são finitos.
- Predadores encontram herbívoros, consomem energia, podem se reproduzir e morrer; decisões iniciais são regras simples, não redes neurais.
- Serapilheira/fezes/cadáveres entram em detritos; decomposição devolve uma fração de nutrientes ao solo, registrando perdas materiais e energéticas de fronteira.
- Seca, chuva intensa, sombreamento e escassez geram consequências ao longo do tempo; não introduzir scripts que forcem uma população constante.
- Interface de inspeção: alternar visualizações de altitude, precipitação, temperatura, água superficial, água no solo, nutrientes, biomassa e seres vivos; observar causas e fluxos; controlar velocidade, pausar e salvar/carregar. A câmera e a projeção devem ser escolhidas pela validação visual, mantendo a leitura de uma região extensa.
- Gráficos e áudio, se presentes, são procedurais, derivados de código/receitas e armazenáveis em cache. Nenhum pack PNG externo deve ser obrigatório.

## Contratos e causalidade

- Interações: leitura imutável de campos/snapshots; comandos ou solicitações de recursos; eventos apenas para fatos confirmados. Recursos contínuos circulam em **lotes** por chunk, não um evento por gota/célula/folha.
- **Um proprietário por estado mutável.** Atmosphere controla estado atmosférico; Geosphere.Soil controla água/nutrientes do solo; Hydrosphere controla água superficial/neve; Biosphere controla organismos/detritos biológicos. Interceptação e evapotranspiração são fluxos negociados, não alterações diretas.
- Agendador orientado a fases lê estados da fase anterior, calcula propostas, resolve pedidos/limites e aplica saldos, depois publica estado consistente. Retroalimentação causal entre ticks é permitida; referências circulares de assemblies, proibidas.
- Cada grandeza especifica dimensão física, unidade, escala espacial, janela temporal, owner, contrato versão, limites e como interpretar ausências.
- Teste de **evolução de contrato**: ClimateBasic v1 permanece consumível quando o clima adiciona outra capacidade em fixture de teste; matéria orgânica passa a ser capacidade do solo sem modificar quem só lê água. Mudanças semânticas ou de unidades precisam de nova versão/adaptador.
- Não tratar evento `RainOccurred` como substituto de campo de chuva. Não tratar dado ausente como zero por conveniência.

## Conservação, precisão e dependências científicas

- Auditoria por reservatório: estoque final = inicial + entradas - saídas + fontes externas - sumidouros externos.
- Separar água, matéria seca/carbono, nitrogênio e energia. **Não afirmar conservação global de água se a atmosfera for somente uma forçante climática:** chuva é entrada externa explicitada; evaporação é saída externa até vapor atmosférico ser modelado.
- Quando fotossíntese cria matéria seca em modelo simplificado, contabilizar entrada externa de carbono/matéria equivalente; animais não crescem sem alimentação assimilada.
- Evitar dupla contagem de precipitação, infiltração, escoamento, transpiração e decomposição. Resolver demandas conflitantes em conjunto, com IDs e desempates estáveis.
- Documente Mathematics, Physics, Chemistry e Biology em `docs/science/`. Implemente funções testáveis (interpolação, ruído, vetores/unidades, balanços, curvas-resposta e taxas) **no módulo responsável**; utilitários amplamente reutilizados podem ficar em `Game.Procedural` ou biblioteca técnica pequena aprovada por ADR. Não criar biblioteca científica universal.
- Ao alegar fundamento científico, anexar fontes, hipóteses, limites de validade e escala. Não inventar coeficientes calibrados ou supor que um modelo meteorológico completo exista.

## Geração procedural e desempenho

- Terreno/rios/habitats e renderização de árvores/arbustos/grama/animais devem partir de receitas e seeds controladas.
- A simulação é independente de câmera e framerate; separar relógio simulado e animação.
- Começar com silhuetas e cache simples; atlas avançados, pools e LOD visual só após necessidade medida. Não regenerar árvores inteiras a cada frame de vento.
- Determinismo por seed e versão de gerador; RNG ecológico separado de RNG visual. Snapshots e persistência sem serializar a GPU.
- A renderização deve privilegiar leitura da paisagem e integração dos elementos naturais. Não escolher 2D ou 3D por inércia; preservar a independência dos modelos científicos e aplicar os critérios comparativos de direção visual dentro do MonoGame.

## Risco obrigatório da Web

Avalie os alvos pretendidos com builds reais e fontes atuais; não inferir portabilidade pela escolha do framework. Web continua sendo um gate separado e precisa de um spike curto ou blocker documentado. Considere as limitações registradas em `docs/14_WEB_GATE.md`, sem comprometer o núcleo C#. Falha web não impede testes headless e desktop, mas impede a alegação de portabilidade web verificada.

## Entrega por marcos e gates

**N0:** solution limpa, convenções Game.*, testes, README/CI, contrato de módulos e boot headless.

**N1:** Cosmos+Atmosphere+Geosphere+Hydrosphere mínimos; teste de ciclo diurno, chuva/infiltração/escoamento/balanço e primeira prova de save/load.

**N2:** Biosphere Botany e Ecology; sementes, crescimento, detritos, decomposição; árvore/gramínea afetam e respondem ao meio.

**N3:** Zoology simples; herbívoro, predador, disponibilidade real de água/alimento, reprodução/morte; ciclos alimentares e nutrientes.

**N4:** cliente gráfico, inspeção causal, geração procedural visual, persistência e experimentos longos; primeira entrega de mundo natural funcional.

Mantenha N0–N4 como **fases internas de um único objetivo**. O MVP não está completo no N0 ou N1. Respeitar critérios detalhados de `docs/12_ROADMAP.md` e `docs/13_TESTS.md`.

## Aceite final

- Mundo inicia por seed e continua de forma reproduzível no ambiente testado, inclusive após save/load.
- Ambiente e organismos apresentam interação em cadeia sob diferentes condições: seca, chuva, excesso de água, sombra, retirada de herbívoros ou predadores e decomposição.
- Um modelo alternativo de infiltração/solo com mesmo contrato substitui o existente sem editar Atmosphere/Biosphere/Hydrosphere; contratos opcionais evoluem sem romper consumidores antigos.
- Nenhuma escrita arbitrária entre módulos. Recursos não ficam negativos; contabilização de água, N, detritos e biomassa auditável.
- Headless funciona; cliente MonoGame demonstra a direção visual escolhida, mostra estado e inspeções e mede desempenho. A seleção da representação segue o gate comparativo documentado dentro do MonoGame; assets artísticos prontos não são dependência obrigatória.
- Testes e CI são executados, com relatórios honestos. Compatibilidade web somente se realmente testada.
- Criar commits e PR draft; não fazer merge na `main`.

## Relatório final do Codex

Informar link da branch e do PR, arquitetura final, arquivos e projetos, processos implementados versus planejados, resultados de testes/seed/hardware e invariantes, limites científicos, decisões sobre portabilidade, pendências e como executar o mundo vivo.

**Após autorização explícita de implementação, execute N0–N4 por entregas pequenas verificáveis. Até lá, este arquivo é apenas objeto de revisão.**

## Limites incorporados pela revisão documental

Quando a implementação for explicitamente autorizada, aplicar os limites de `docs/00_SCOPE.md`, o fechamento biológico de `docs/09_BIOLOGICAL_MVP.md` e as regras de `docs/19_CODING_RULES.md`. A árvore de projetos é alvo incremental, não scaffold obrigatório completo em N0. Não criar interfaces de neve/aquíferos antecipadamente. Primeiro mapa finito, patches vegetais, fauna local por regras, agenda fixa e commits sequenciais por fase. Save/load começa com prova pequena em N1; spike web tem orçamento limitado e não bloqueia desktop por ausência de suporte. Critérios T19–T25 integram o aceite futuro. A autorização end-to-end acima só vale após solicitação explícita de implementação.
