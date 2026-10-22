# Auditoria modular do runtime — 22/10/2026

## Pergunta e método

Revisei Cosmos, Atmosphere, Geosphere, Hydrosphere e Biosphere, seus contratos, dependências `.csproj`, composição dos executáveis, estado persistido e gates do roadmap. O objetivo foi separar integração causal — necessária para executar o mundo — de implementação simultânea dos domínios.

Não encontrei decisão registrada para começar os cinco módulos em paralelo. A hipótese mais plausível é que se buscou cedo uma fatia vertical executável: o ADR-0002 cobria N0–N2 e o cenário L0 conecta clima, terreno, água e vegetação. Isso explica a presença de todos os domínios no runtime, mas não justifica abrir todas as frentes de implementação de uma vez. O roadmap já organiza o trabalho em N1 (Cosmos–Hydrosphere), N2 (Botany/Ecology), N3 (fauna) e N4 (apresentação). O ADR-0005 registra que a integração pode continuar enquanto a implementação avança pelos gates, um incremento por vez.

## Estado dos módulos

| Módulo | Responsabilidade e contratos atuais | Limite explícito |
|---|---|---|
| Cosmos | `ISolarForcingV1` publica amostras solares; `CosmosModuleV1` delega à geometria solar. | Tick/calendário continua coordenado no Core; Lua e ciclos astronômicos estão fora do L0. |
| Atmosphere | `IClimateForcingV1` fornece previsão horária e `IAtmosphericTemperatureV1` fornece temperatura sazonal/horária. | Circulação e pressão avançadas não fazem parte do L0. |
| Geosphere | `IGeosphereTerrainV1`, `ISoilWaterV1` e `ISoilBiogeochemistryV1` governam relevo, água retida no solo, ET confirmada, N mineral e matéria orgânica edáfica. | Aquífero/camadas de solo são simplificados no L0. |
| Hydrosphere | `ISurfaceWaterV1` recebe células físicas compactas, processa chuva, roteia água e devolve apenas estoques/fluxos hídricos. | Reservatório subterrâneo é simples; neve e gelo estão adiados. |
| Biosphere | `IBotanyV1`, `IEcologyV1` e `IZoologyV1` usam contratos de domínio tipados. Botany resolve herbivoria; Ecology propõe estoques edáficos; Zoology coordena agentes e transferências confirmadas por Botany, Hydrosphere, Geosphere e Ecology. | N3 passou os casos definidos, três seeds anuais e throughput. Predadores extinguem-se depois da cadeia ter sido observada. Valores biológicos L0 são heurísticas de jogo, não calibração ecológica. |

O cruzamento dos documentos encontrou uma divergência: a matriz de ownership agrupava tempo/dia com Cosmos, enquanto Core possui `TickIndex`, agenda e deriva `DayOfYear`/`Season`. A fronteira agora está explícita em [04_OWNERSHIP](../04_OWNERSHIP.md), [07_SCHEDULER](../07_SCHEDULER.md) e ADR-0005: Core controla o relógio L0; Cosmos fornece geometria solar para o instante solicitado. O teste de calendário cobre a virada no 144º tick.

`WorldCell` ainda é o formato agregado persistido em `SimulationState`, mas não atravessa as APIs públicas de Geosphere, Hydrosphere ou Biosphere. Core traduz entre o checkpoint agregado e DTOs de domínio; mudanças de água, N e matéria orgânica passam pelo owner correspondente. Portanto, a fronteira de contratos melhorou sem introduzir já uma migração de formato/save para armazenamento particionado.

## Dependências e composição

- `Game.Core` referencia apenas `Game.Contracts`.
- Assemblies de domínio não referenciam outros `Game.Modules.*`; utilitários procedurais compartilhados são permitidos.
- `Game.App` e `Game.Client.Desktop` fazem composição explícita. Testes substituem capabilities para confirmar a orquestração.
- Os onze manifests são estáticos e testados contra capabilities existentes. Eles não são carregados pelo runtime nem criam framework de plugins.
- A separação de implementações e dependências está registrada em [ADR-0005](../decisions/ADR-0005-module-composition-roots.md), que substitui a composição em Core do ADR-0002.

## Incrementos e evidência

1. **Cosmos + Atmosphere:** contratos solares e climáticos isolados; o filtro de capacidades e respostas ambientais passou **4/4**.
2. **Geosphere + Hydrosphere:** menor conjunto fortemente acoplado do N1, pois a infiltração exige coordenação entre os owners do solo e da água. As APIs não usam `WorldCell`; o balanço local de chuva, interceptação, infiltração e evaporação fecha. O filtro de chuva saturada, infiltração de poça sem chuva, balanço local, roteamento, perda explícita na borda, fronteira e seca controlada passou **7/7**; a suíte também cobre depressões.
3. **Biosphere N2:** Botany/Ecology deixaram de usar `WorldCell` em seus contratos; os resultados são propostas explícitas confirmadas pelo owner Geosphere. O filtro N2 passou **7/7**: capability substituível confirma transpiração, N e fechamento ecológico; testes de domínio verificam respostas a luz/água/N, competição por água em seca controlada, meia PAR, efeito do estresse na mortalidade/serapilheira, decomposição, fechamento de matéria seca/N e serialização após estoque ecológico não nulo.
4. **Zoology N3:** estado persistente, população 40/4 opt-in, comportamento local, alimentação, captura, metabolismo, água, mortalidade e reprodução foram implementados. Os testes de Zoology passaram **11/11** e o cenário de 365 dias foi rodado nas seeds 1–3. Capturas foram observadas no primeiro dia em todas as seeds; predadores extinguiram-se antes do fim do ano, sem respawn, depois da cadeia ter sido observada. Um defeito de água corporal perdida na predação foi corrigido e o resíduo hídrico global do primeiro dia ficou abaixo de `10⁻⁶ L`. O passo horário reutiliza plano de drenagem, normais do terreno, temperatura e ET; dispersão sem produção evita buffers e cópias. O hash anual permaneceu igual após as otimizações. O gate de throughput passou: medições Release de `AdvanceTicks` de **17,058 s**, **17,024 s** e **17,121 s**, mediana **17,058 s** (**21,40 dias/s**) contra limite de **18,25 s**.

A suíte Release passou com **86/86 testes**. Os filtros direcionados passaram: Cosmos/Atmosphere **4/4**, Geosphere/Hydrosphere **7/7**, Biosphere/N2 **7/7** e Zoology/N3 **11/11**. Os casos cobrem solstício no equador, sol da meia-noite e noite polar; capacidade hídrica com e sem chuva; resposta à seca até o ponto de murcha; PAR a 50%; decomposição úmida/seca e quente/fria; save/load com fauna; herbivoria e predação com transferências de água, matéria seca e N; fome com limite de três dias; cota de captura com reset diário; desidratação; idade máxima; e dispersão de sementes e fatores físicos pré-calculados. O runner do cenário `temperate-valley-l0-v17`, seed 1, 144 ticks, executou 4.096 células e reproduziu o hash canônico:

```text
state-sha256=BE5B7203E9F557C0A0585462CB9B5D87A803E9E90BD283D9E2522854F8797E49
balance-error water-L=-1,683E-007 dry-matter-g=-4,199E-006 nitrogen-g=4,657E-009
```

As seeds diagnósticas 1–3 também avançaram 144 ticks. Os três resultados produziram contagens/hash diferentes, como esperado, sem desvio de conservação acima da tolerância:

| Seed | Gramíneas | Lenhosas | Células com água | SHA-256 do estado |
|---:|---:|---:|---:|---|
| 1 | 2.220 | 1.948 | 939 | `BE5B7203E9F557C0A0585462CB9B5D87A803E9E90BD283D9E2522854F8797E49` |
| 2 | 2.267 | 1.739 | 1.006 | `F4E4ED54B19AA047D2C564C543BF7124849180D286E918A0D3699EA98A051E1A` |
| 3 | 2.426 | 1.882 | 1.070 | `71BB92133FC8F6FC1015E5B2F561684E510B892B65D60126FE1603A317C6948C` |

O runner de seed 1 e os três diagnósticos fecham apenas uma janela de 144 ticks; não são prova de robustez ecológica anual nem calibração científica. O gate web continua **inconclusivo**, conforme [14_WEB_GATE.md](../14_WEB_GATE.md), sem promessa de portabilidade.

## Próximo incremento e limites

Os critérios de gate L0 de N1 e N2 do roadmap foram verificados: N1 conserva os fluxos, responde a chuva/seca, expõe campos inspecionáveis, é determinístico, salva/retoma e mantém o spike web inconclusivo documentado; N2 tem duas guildas, respostas à luz/água/N, crescimento/mortalidade por habitat, estoques de matéria/N auditáveis e save/load depois do fechamento diário. Isso não equivale a calibração ecológica, nem valida a cadeia alimentar completa de N3.

O incremento **Zoology/N3** foi fechado após os critérios do roadmap e o gate de throughput passarem. O próximo trabalho segue para o gate N4 de apresentação, mantendo a sequência de um incremento por vez.

O cenário de referência foi alinhado de 256 × 256 para **64 × 64**, conforme `temperate-valley-l0-v17`; hashes e inventários anteriores com 256 × 256 não servem como evidência desse cenário.
