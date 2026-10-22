# Plano modular de Zoology

**Status:** plano de evolução após o baseline Zoology/N3; Z1 técnico foi implementado e validado em 2026-10-22. As specs `ZOO-*` distinguem capacidades validadas de propostas e pendências; elas não alteram os gates N0–N4.

## Objetivo e limites

Evoluir Zoology em fatias pequenas e verificáveis. `Game.Modules.Biosphere` continua dono de espécie, fisiologia, anatomia funcional, comportamento, reprodução e estado animal. `Game.Procedural` pode produzir geometria e padrões visuais a partir de dados derivados; um adaptador em `Game.Rendering` ou no cliente converte esses dados para o backend gráfico. A API gráfica nunca é dependência do núcleo Zoology.

Os números, taxas e tolerâncias de processos L0 continuam definidos exclusivamente em [20_L0_MODEL_AND_BENCHMARKS.md](../20_L0_MODEL_AND_BENCHMARKS.md). Este plano não transforma os requisitos de pesquisa em parâmetros calibrados, nem exige uma assembly por pasta. MonoGame permanece a stack do cliente conforme [ADR-0004](../decisions/ADR-0004-visual-direction-and-renderer-evaluation.md); a agnosticidade visual aqui significa que os contratos e a simulação não dependem de MonoGame.

## Estado de partida auditado

| Capacidade | Estado no repositório |
|---|---|
| Agentes e ciclo ecológico L0 | Implementados para os perfis funcionais herbívoro e predador; consumo, predação, metabolismo simplificado, água, reprodução, mortalidade, detritos e save/load constam do checkpoint [Zoology/N3](../reports/ZOOLOGY_N3_2026-10-22.md). |
| Identidade/contrato de agente | `AnimalAgentV1` persiste ID, perfil funcional, posição, idade, massas, hidratação, déficits, cooldown e genoma/linhagem prototípicos Z5. |
| Espécies, genomas e linhagens versionados | Z1 fornece dois protótipos técnicos, schemas/envelopes versionados, streams separados e mutação limitada. Z5 persiste genomas de fundador e herança uniparental no N3 atual. Parâmetros não são calibrados para espécies reais; fisiologia/ecologia N3 ainda não consomem os alelos. |
| Plano corporal e anatomia gerados | Z1 produz corpos headless imutáveis com grafo de partes, juntas e sensores. Z2 fornece IK, pose axial e integração de rota terrestre para a posição persistida do N3; os perfis funcionais N3 ainda não consomem os corpos/genomas Z1. |
| Peixe e locomoção aquática | Não implementados. A fauna N3 é terrestre. |
| Visual procedural derivado de anatomia | Z1 produz receita visual; Z4 v8 preserva superfícies-fonte por parte e extrai uma malha unificada com união suave dos campos anatômicos e conectores pai-filho. A versão v8 corrige winding local orientando faces pelo gradiente interpolado em cada tetraedro. A regressão passou 11/11 e a suíte Release completa 151/151. A matriz de malha cobre 12 combinações: dois planos corporais, três pares de genoma/pigmento e duas tesselações; todas passaram conectividade, incidência/orientação, links cíclicos de vértice e degenerações. A busca de interseção omite pares que compartilham vértices e ainda não cobre amostra ampla de seeds/resoluções. O modo MonoGame desenvolve/cacheia geometria por genoma/estágio, aplica postura estática via IK Z2 e acompanha posições N3; a captura confirma 36/36 instâncias. Revisão multiângulo, detecção mais ampla de auto-interseções, animação, invariância a câmera/FPS/LOD e custo permanecem abertos. Caracteres radiculares também são fenótipo por perfil: carvalho, bordo, salgueiro, pinheiro e bétula não compartilham uma regra universal de exposição. O experimento atual de sprites L0 não é fonte de verdade biológica. |
| Herança genética e especiação | Z1 oferece mutação determinística e IDs parentais no protótipo. Reprodução, seleção e especiação não estão ligadas à reprodução L0; continuam em Z5. |

O selo `validated` do manifesto `zoology.n3.json` vale somente para o escopo L0 de herbívoro/predador. Ele não aprova os gates das specs `ZOO-*`.

## Sequência de módulos e gates

Cada etapa deve produzir uma alteração de escopo estreito: ficha científica quando houver processo novo, contrato apenas se a integração/save precisar dele, código dentro do owner, testes reproduzíveis, atualização do manifesto e um relatório curto. Só se inicia a próxima etapa depois de revisar a evidência da etapa atual. Etapas que dependam de decisão visual podem avançar headless sem fixar o formato de saída.

| Ordem | Módulo de trabalho | Entrega e gate de saída |
|---|---|---|
| Z0 | Baseline L0 existente | Preservar o comportamento e os balanços N3 já validados. Reexecutar a regressão direcionada quando uma etapa posterior tocar o estado ou as transferências L0. |
| Z1 | Espécie, plano corporal, genoma e desenvolvimento ([SPEC-ZOO-100](ZOO-100-BODYPLANS-GENOMES.md)) | **Gate técnico validado:** schema/versão, unidades, envelopes declarados, streams determinísticos, corpos imutáveis para dois protótipos, pigmento isolado e 1.000 seeds por protótipo. Calibração por espécie/população é pendência explícita e não autoriza tratar valores atuais como dados naturais. |
| Z2 | Anatomia, capacidade e locomoção ([SPEC-ZOO-110](ZOO-110-ANATOMY-LOCOMOTION.md)) | **Pose/IK, desvio terrestre e planejamento aquático por profundidade agora cobertos por casos técnicos;** Core fornece observações de elevação/água e Zoology persiste apenas a posição aceita. A rota aquática contorna células rasas e recusa cortes diagonais por cantos secos; o solver não aplica limite de declive de terra ao peixe. A validação dirigida desta revisão passou 24/24 casos de pose, solver, planner e integração N3. O gate integrado continua em andamento: limiares não calibrados, colisão volumétrica, correnteza e integração de peixe à população persistente permanecem abertos. Dados de locomoção são específicos de espécie/população, como documenta a [auditoria Z2](../research/zoology/Z2_LOCOMOTION_DATA_AUDIT_2026-10-22.md). |
| Z3 | Fisiologia e ecologia por perfil ([SPEC-ZOO-120](ZOO-120-PHYSIOLOGY-ECOLOGY.md)) | **Balanço em joules integrado ao metabolismo N3; política L0 prioriza manutenção basal antes de locomoção e limita distância pelo saldo.** O filtro dirigido de energia passou 10/10 (5 unitários e 5 de integração/contrato). A suíte completa foi reexecutada após as inclusões do Z2 e passou 150/150. O solver fecha a identidade energética e alimenta o fluxo L0 de massa sem misturar unidades. Custos e conversão atuais continuam proxies; políticas por perfil, crescimento/reprodução energéticos, perfis calibrados e cenário aquático seguem abertos. Fontes candidatas e limites estão em [auditoria Z3](../research/zoology/Z3_PHYSIOLOGY_DATA_AUDIT_2026-10-22.md); não é DEB completo. |
| Z4 | Geometria, padrões e adaptador de render ([SPEC-ZOO-130](ZOO-130-RENDERING.md)) | **Em andamento:** `CreatureGeometryBuilderV1` v8 extrai uma superfície conectada por plano corporal usando união suave e conectores nos elos pai-filho; a orientação de faces agora usa o gradiente interpolado em cada tetraedro, após uma regressão revelar winding local inconsistente. A matriz cobre dois planos corporais, três pares de genoma/pigmento e tesselações `6×8`/`8×12` (12 combinações); o filtro passou 11/11 e a suíte completa 151/151. Testes verificam incidência e winding por aresta, link cíclico por vértice e ausência de degenerações. A busca espacial omite pares com vértices compartilhados e ainda não cobre amostra ampla de seeds/resoluções. Galeria v7 e capturas integradas v16/v19/v22 documentam inspeção multiângulo; o modo registra 36/36 instâncias. A anatomia continua genérica; as capturas não validam espécie real. Animação, custo e invariância com câmera/FPS/LOD permanecem abertos. A seleção final da técnica segue o gate N4 e ADR-0004. |
| Z5 | Herança e adaptação ([SPEC-ZOO-140](ZOO-140-EVOLUTION.md)) | **Subgate técnico em andamento:** genomas determinísticos persistem por agente; descendentes do processo uniparental atual registram o genoma parental e mutação limitada; saves antigos migram para schema 3. Testes cobrem linhagem determinística de 32 gerações e operador em 1.024 seeds por protótipo, mas não modelam reprodução sexual nem seleção sobre genes. Soay de St Kilda continua candidato para calibração isolada de herbívoro: o inventário Dryad está confirmado, mas os downloads dos READMEs e tabelas exigem token bearer e retornaram 401; os links de fluxo retornaram 403. As chaves de ID não foram verificadas. Chinook permanece candidato anádromo distinto. Predador de referência, recombinação/compatibilidade, ligação genótipo-fenótipo, deriva/seleção populacional e especiação continuam abertos; ver [relatório Z5](../reports/ZOOLOGY_Z5_2026-10-22.md) e [auditoria de dados](../research/zoology/Z5_INHERITANCE_DATA_AUDIT_2026-10-22.md). |

A ordem acima é executada um módulo de cada vez. Z2 pode consumir a topologia prototípica de Z1; para contato físico, aguardar um contrato confirmado de movimento/habitat. Z4 tem uma implementação renderer-neutral possível, mas só deve entrar em trabalho depois de revisar o gate Z3. Se um comportamento exigir medidas naturais que não possam ser inferidas do protótipo, registrar a dependência como bloqueio de calibração e avançar para o módulo seguinte da sequência, voltando à integração quando houver dados/contratos. Recursos de pesquisa de coevolução forma/controle ficam fora dos gates do primeiro ecossistema.

## Critérios comuns de conclusão

- Versão, seed, parâmetros e condições externas são suficientes para reproduzir a saída lógica.
- Mudança de câmera, FPS, renderização ou culling não altera simulação, metabolismo ou fluxos.
- Unidades e donos de estoques são explícitos; transferências continuam mediadas pelos owners.
- Novos campos persistidos têm migração definida; não serializar interfaces, rigs de GPU ou sprites.
- Testes declarados no plano [ZOO-ACCEPTANCE](ZOO-ACCEPTANCE.md) distinguem propostas futuras de evidência já executada.
- Relatórios descrevem hardware, build, seed, ambiente, limites e resultados; referências de terceiros não são resultados do projeto.

## Referências e rastreabilidade

- [Arquitetura de criaturas](../architecture/CREATURE_ARCHITECTURE.md)
- [Fundamentação de morfogênese e vida artificial](../science/Biology/Morphogenesis-and-Artificial-Life.md)
- [Pacote de pesquisa original](../research/zoology/README.md)
- [Auditoria da referência Procedural Pixel Creatures](../research/zoology/PROCEDURAL_PIXEL_CREATURES_AUDIT_2026-10-22.md)
- [Modelo biológico MVP atual](../09_BIOLOGICAL_MVP.md)
- [Roadmap de integração](../12_ROADMAP.md)
