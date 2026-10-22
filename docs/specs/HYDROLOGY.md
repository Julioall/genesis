# Hidrologia do mundo L0

> Os registros de relevo v16 e hidrologia v17 descrevem experimentos avaliados em 21/10/2026. O código relacionado está incluído no checkout atual; as medições continuam vinculadas aos builds/cenas históricos e não foram reexecutadas nesta revisão. Consulte o [índice atual de evidências](../reports/README.md).

**Status:** H0 começou; Priority-Flood, caminhos potenciais e curvas de armazenamento de depressões estão implementados para a grade L0. O solver de água em movimento ainda é o modelo anterior e não deve ser descrito como validado hidrodinamicamente.

## Responsabilidades

O sistema mantém quatro etapas separadas:

1. **Topologia:** lê a cota original do leito e calcula conectividade, soleiras e áreas contribuintes.
2. **Reservatórios:** mantém água superficial, solo e armazenamento subterrâneo com owners explícitos.
3. **Movimento:** transfere volume entre células e fronteiras com balanço auditável.
4. **Apresentação:** converte snapshots físicos em superfície desenhada; nunca escreve no estado da água.

`Geosphere.Terrain` é dona da elevação do leito. `Geosphere.Soil` é dona da água nos poros. `Hydrosphere.SurfaceWater` é dona da água superficial e dos fluxos hidráulicos. Atmosphere publica precipitação e condições de evaporação; Biosphere só consome água confirmada pelo Soil. Essa divisão evita descontar duas vezes a mesma água.

## H0 — relevo hidrológico e bacias

`SurfaceWaterModel.AnalyzeHydrology` aplica Priority-Flood a partir da borda aberta da grade. O resultado separa:

- `FilledElevationM`: superfície condicionada usada para encontrar conectividade;
- `Receiver` e `Accumulation`: árvore acíclica de caminhos potenciais até a borda;
- `DepressionId` e `Basins`: componentes deprimidas agrupadas pela soleira de extravasamento;
- `CapacityM3`: volume necessário para preencher a depressão até a soleira.

O campo `WorldCell.ElevationM` nunca é alterado. O Priority-Flood é pré-processamento topológico, não aterro físico permanente. A literatura define justamente o preenchimento como um campo separado e descreve o algoritmo começando na borda com uma fila de prioridade ([Barnes, Lehman e Mulla, 2014](https://doi.org/10.1016/j.cageo.2013.04.024)).

`DepressionVolumeAtStageM3` e `DepressionAreaAtStageM2` calculam a curva nível–volume–área a partir das cotas originais das células. A água inicial de até três bacias usa uma fração limitada da capacidade e uma única cota de superfície, abaixo da soleira; assim, o fundo pode variar sem criar uma superfície de lago inclinada. Ainda não há enchimento dinâmico, recálculo de nível após chuva, nem extravasamento de lago em tempo de simulação.

O D8 de maior declive em `AnalyzeDrainage` permanece disponível como dado de saída para a rede atual de canais e a apresentação visual. Priority-Flood responde à conectividade e às depressões; o D8 atual continua escolhendo um receptor mais baixo no leito original. A rede potencial não é uma afirmação de vazão permanente nem uma dependência de motor gráfico.

## Estado atual e próximos gates

| Gate | Situação no código | Próxima validação |
|---|---|---|
| H0 — depressões e soleiras | Priority-Flood determinístico, leito preservado, árvore para borda e curva de estágio para cada depressão | bacias aninhadas, áreas planas e saídas fechadas/abertas configuráveis |
| H1 — solo e chuva | água superficial e do solo têm owners distintos; infiltração é limitada por capacidade e Ksat no Soil | reservatórios contínuos SMA, percolação explícita e demanda de ET contínua |
| H2 — fluxo superficial | `Route` gera transferências simultâneas entre vizinhos, limita a retirada pelo volume disponível e contabiliza perda na borda | virtual pipes com vazão por aresta, timestep/subpassos medidos, fronteiras explícitas e fixtures de wet/dry |
| H3 — comparação de solver | não iniciado | comparar virtual pipes e local inertial com os mesmos fixtures; só então escolher o principal |
| H4 — água visual | malha de água e ribbons D8 são construídos a partir do snapshot; rio visível ainda usa limiar de área contribuinte | bancos por profundidade, largura por vazão/seção, superfície nivelada dos lagos, fluxo e espuma no renderer |
| H5 — ecologia hídrica | plantas consultam água do solo e o ledger fecha transferências | interceptação, raízes e demanda concedida como transferências explícitas |
| H6 — sedimento | não iniciado | erosão/deposição somente após estabilidade hídrica e balanço de sólidos |

O `Route` atual é um MVP de difusão conservativa; não possui vazão persistente nas arestas, critério hidráulico de estabilidade, momentum local nem calibração. O desenho de rios ainda depende parcialmente de contribuição D8, e não só de vazão efetiva. Portanto a aparência do rio não prova a dinâmica de bacia.

## Unidades e invariantes

- Cota e profundidade: m; água superficial e água do solo nos snapshots: mm.
- Área da célula L0: 100 m²; volume de reservatório: m³; livro legado: litros.
- Nível da superfície em repouso: `stageM`; profundidade por célula: `max(0, stageM - bedElevationM)`.
- Nenhuma bacia extravasa antes de `SpillElevationM`; volume acima da capacidade deve ser uma saída contabilizada pelo próximo gate.
- Fluxos internos saem e entram no mesmo passo, em ordem determinística; saída da borda é uma perda externa auditada.
- Sem profundidade negativa, `NaN`, `Infinity`, caminhos circulares ou alteração silenciosa do leito original.
- A aceleração visual do jogo não deve multiplicar o tamanho do passo hidráulico.

## Gates de teste a completar

1. Volume e área de uma bacia em diferentes cotas, inclusive limite na soleira.
2. Chuva enche uma depressão sem perda de volume; somente o excedente à soleira sai pelo receptor correto.
3. A mesma precipitação em areia seca e solo argiloso saturado gera infiltração/escoamento diferentes e auditáveis.
4. Alternância chuva/seca permite lago e canal intermitentes sem apagar a topologia.
5. Confluência soma vazões; bifurcação divide volume; fluxo cruzado conserva movimentos opostos.
6. A água atravessa bordas de chunks sem lacuna ou duplicação.
7. Repetição por seed e save/load no meio de uma cheia dão a mesma continuação.
8. Medir tempo hidráulico por número de células úmidas e subpassos; medir separadamente reconstruções do renderer.

## Fontes de implementação e critérios

- [Priority-Flood — Barnes, Lehman e Mulla](https://doi.org/10.1016/j.cageo.2013.04.024), referência para condicionamento de depressões e rotas.
- [HEC-HMS — Soil Moisture Accounting](https://www.hec.usace.army.mil/confluence/hmsdocs/hmstrm/canopy-surface-infiltration-and-runoff-volume/infiltration/soil-moisture-accounting-loss-model), referência para armazenamento contínuo em camadas e interação com ET. Seus parâmetros requerem estimativa e calibração; não devem ser importados como constantes universais.
- [MonoGame — Custom Effects](https://docs.monogame.net/articles/getting_started/content_pipeline/custom_effects.html), referência de implementação para efeitos visuais posteriores. Shaders não serão donos de água física.
- As recomendações de Virtual Pipes, local inertial, limites wet/dry, sedimento e bibliotecas estão no estudo enviado pelo usuário; nenhuma biblioteca GPL nem código externo foi incorporado.
