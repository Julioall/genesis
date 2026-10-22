# Terreno geométrico e protótipo visual atual

> Os registros de relevo v16, hidrologia v17 e flora 3D descrevem protótipos do diretório de trabalho local em 21/10/2026. O código dessas alterações não integra esta publicação documental; os resultados históricos não foram reexecutados nesta revisão.

**Status:** histórico técnico do terreno e do cliente existente. MonoGame permanece a stack obrigatória; as especificações de câmera, sprites e projeção abaixo descrevem protótipos anteriores, não requisitos para a apresentação final. A nova direção e a avaliação de técnicas dentro do MonoGame estão em [22_VISUAL_DIRECTION_AND_TECHNOLOGY.md](22_VISUAL_DIRECTION_AND_TECHNOLOGY.md) e [ADR-0004](decisions/ADR-0004-visual-direction-and-renderer-evaluation.md).

## Direção

Na implementação registrada nesta revisão, a câmera é ortográfica e o cliente MonoGame amostra a elevação física da Geosfera em uma malha. Esse estado é evidência do protótipo atual, não obrigação de câmera ou técnica. O requisito permanente é a apresentação consumir o relevo publicado sem substituí-lo por textura e sem decidir a geografia.

As referências de Mapgen4 e do artigo Polygonal Map Generation orientam a separação entre feições geográficas, drenagem e biomas. Neste marco usamos curvas e campos de distância diretamente no sampler determinístico; não copiamos código, modelos ou imagens, nem incorporamos bibliotecas externas. Um grafo Voronoi/Delaunay pode ser avaliado quando o mapa deixar de ser uma grade pequena de simulação.

## Implementação acumulada v10–v17

- A Geosfera conserva o vale sinuoso e o planalto de topo plano, e acrescenta duas cordilheiras sinuosas em lados opostos do vale. Cada cadeia usa um eixo deslocado e curvo, largura variável, cristas estreitas sobre ombros largos, modulação de maciços e passagens mais baixas. A mesma seed fixa os eixos, fases e máscaras; o campo físico continua contínuo fora do paredão do planalto.
- A superfície de terreno possui altura `Y` real. A malha visual é subdividida oito vezes por célula em chunks de 16 × 16 células; a borda amostrada por chunks vizinhos tem a mesma posição, normal e UV. O afloramento menor mantém uma malha superior e paredes rochosas explícitas ligadas ao terreno-base. Árvores, gramíneas e rochas usam a altitude física da célula como âncora. A textura expõe mais rocha em encostas íngremes.
- O gradiente usado pela iluminação solar agora é expresso em metros de elevação por metro horizontal, usando os 10 m por célula derivados da área de 100 m². A implantação inicial da flora combina altitude, inclinação e água superficial: árvores não começam em células alagadas e ficam raras na faixa alpina; gramíneas toleram água rasa. A cobertura visual só desenha tufos em parte das células vegetadas, com densidade espacialmente agrupada. Árvores ripárias favorecem salgueiros e as de altitude favorecem coníferas. Os limites são parâmetros heurísticos do cenário, não tolerâncias biológicas calibradas.
- A apresentação visual refina as copas com massas foliares mais cheias e reduz a repetição de galhos; tufos aparecem mais densamente em áreas com biomassa. As paredes do afloramento usam variação cromática contínua. A água tem oito subdivisões por célula, margens com ruído determinístico em três escalas e largura irregular nas fitas dos rios; essas mudanças são apenas de renderização e não modificam o campo hidrológico.
- O autotile de grama usa uma máscara 4 × 4 subcélulas por célula ecológica. Dois campos determinísticos de ruído controlam os vazios; vizinhos diagonais arredondam os contornos. A máscara só é gerada onde existe biomassa viva e não altera cobertura, biomassa ou sucessão na simulação.
- A revisão visual v13 diminui os raios do afloramento e acrescenta irregularidade coerente de pequena escala ao perímetro, mantendo o topo plano e a parede lateral física. O cenário determinístico passa de v10 para v11 porque a altitude inicial e os estados derivados podem mudar com o contorno.
- A revisão v14 acrescenta dois afloramentos menores às encostas das cadeias: cada um tem coroa, perímetro com ruído coerente e face vertical gerada pela Geosfera. Os três topos e paredões são enviados em buffers agregados. Como essas feições alteram elevações, drenagem e estado inicial, o cenário passa a v12.
- A revisão visual v15 representa cada patch lenhoso como um pequeno grupo de 2–5 árvores. O porte e a variação do grupo são derivados da biomassa e da seed do patch; a mudança só afeta a apresentação, sem criar estado ou organismos novos na simulação.
- Antes do ajuste v13, a ocupação lenhosa inicial usava 27%/40%/12% por faixa e uma máscara de agrupamento em escalas mais amplas para formar bosques maiores. Flores silvestres pontuais são desenhadas sobre células com gramínea; são detalhe visual derivado da seed, sem estoque ou efeito ecológico.
- O cenário ecológico v13 usa frações lenhosas de 30%/62%/40% por altitude e permite inclinações até 1.0 m/m, mantendo a exclusão de células alagadas. A seleção continua agrupada por ruído de larga escala; a revisão mira a distribuição de bosque observada na referência e mantém faces íngremes expostas. São parâmetros heurísticos de colocação inicial, não frequências calibradas de ecossistema real.
- A Hydrosphere continua calculando receptores D8 a partir das elevações físicas. Cada receptor interno é mais baixo que sua origem; acumulação, canais iniciais, depressões alagadas e cachoeiras visuais derivam dessa rede. O renderizador não recalcula nem altera o fluxo.
- Texturas de terreno, água, grama, árvores e rochas continuam geradas por código em tempo de execução. Não há dependência de pacotes externos de arte.

## Validação

- O teste do campo físico verifica cristas acima de 145 m e sua presença ao longo de pelo menos 20 linhas da seed canônica.
- O teste da malha confirma variação vertical geométrica, triângulos válidos e correspondência exata nas bordas de chunks vizinhos.
- Os testes hidrológicos confirmam que os receptores D8 descem o relevo e que há ao menos uma quebra de altitude com água de canal.
- O teste de flora verifica frações por faixa de altitude, limite de inclinação e agrupamento espacial.
- A validação v10 avançou três seeds até o tick 144 e manteve resíduos dentro das tolerâncias; hashes, populações, elevação e benchmark desktop constam em [20_L0_MODEL_AND_BENCHMARKS](20_L0_MODEL_AND_BENCHMARKS.md). A suíte de Release passou (17/17).
- A captura `genesis-flora-v10.png` está no diretório de visualizações do Codex, fora do repositório. Ela mostra bosques mais conectados e flores de campo pontuais. A referência final ainda tem mais variação de copa, rocha e micro-relevo que esta versão.
- A captura `genesis-flora-v11-final.png` registra o refinamento de margens e fitas fluviais. A janela continua em 64 × 64 células, então os bancos ainda precisam de mais microdetalhe e integração de vegetação para se aproximar da referência.
- A captura `genesis-flora-v12-grass-autotile.png` registra o autotile de grama em subcélulas. Um teste confirma determinismo, conexões válidas da máscara e ausência de grama visual em células sem biomassa.
- A captura `genesis-flora-v13-weathered-outcrop.png` mostra o afloramento menor e irregular. A suíte Release passou com 18 testes, incluindo limite de raio, variação de contorno, determinismo e faces geométricas.
- A suíte Release passou com 19 testes; o teste novo confirma três feições distintas, determinísticas e com desnível físico mínimo de 18 m. A captura `genesis-flora-v14-multiple-outcrops.png` mostra a distribuição dos afloramentos na cena.
- A captura `genesis-flora-v15-canopy-groves.png` confirma a leitura de bosques mais cheios. A medição Release de 60 s permanece dentro da meta N4; os valores completos constam em [20_L0_MODEL_AND_BENCHMARKS](20_L0_MODEL_AND_BENCHMARKS.md).
- A captura `genesis-flora-v17-highland-forest.png` registra o cenário ecológico v13 com cobertura lenhosa aumentada e limite de inclinação de 1.0 m/m. Ela mostra bosques maiores sobre as encostas, mas ainda tem áreas altas expostas e rios largos em comparação com a referência; hashes e benchmark desta revisão estão em [20_L0_MODEL_AND_BENCHMARKS](20_L0_MODEL_AND_BENCHMARKS.md).

## Revisão visual v23 — ancoragem e profundidade da flora

- O autotile de grama usa até 8 × 8 subcélulas por célula ecológica, combinando ruído espacial em escalas próximas a 6, 2 e 1 células. Um pigmento suave de solo e umidade mistura materiais nas bordas sem criar indivíduos nem alterar biomassa. A textura continua determinística e gerada em memória.
- Pedras, tufos, flores e árvores recebem coordenadas locais estáveis dentro de cada célula; cada âncora consulta a mesma altitude física usada pelo terreno. Todos os sprites são ordenados de trás para frente pela profundidade projetada pela câmera. As posições projetadas ficam em cache até mudar snapshot, resolução ou estação visual.
- Esta é uma alteração exclusiva de apresentação. O cenário e a geografia física continuam em `temperate-valley-l0-v14`; o campo de elevação e a malha geométrica não foram alterados.
- A captura `genesis-geometric-v23-depth-sort.png` está fora do repositório no diretório de visualizações do Codex. A malha contínua, os paredões e a ancoragem física estão visíveis, mas a imagem ainda fica distante da referência: o vale e os canais são largos, os bosques têm bordas em manchas e a geografia permanece concentrada em duas cordilheiras e três afloramentos.
- `dotnet test Game.sln -c Release --nologo`: 21/21 aprovados. Benchmark Release de 60 s, seed 1, 1280 × 720 e 6 ticks/s: média 58.63 FPS, P95 21.74 ms, P99 50.39 ms, CPU 53.5% de um núcleo e working set 171.4 MiB. O P99 excedeu a meta de 50 ms por 0.39 ms nesta amostra.

## Limites desta etapa

O mundo de simulação ainda mede 64 × 64 células e usa quatro chunks fixos por lado; não há LOD, streaming ou reconstrução seletiva. A v16 eleva as cristas e distribui sete afloramentos com faces verticais sobre as cordilheiras e o vale. A drenagem usa o receptor D8 de maior declive e depressões locais, sem erosão que atualize o relevo, transporte de sedimentos ou chuva orográfica. O nível visual da água ainda aproxima a profundidade em vez de resolver uma superfície livre hidrostática. A flora usa faixas de altitude, inclinação e água determinísticas, não associações calibradas por espécie ou dados de campo. A malha já tem altitude geométrica, mas ainda faltam regiões geográficas mais diversas, erosão e uma rede de água contínua com quedas ligada aos níveis físicos.

## Revisão geométrica v18 — relevo multiescala

- O campo físico contínuo agora soma ruído determinístico de 20 m (até ±4,8 m) e 10 m (até ±2,6 m), moderado pela proximidade do vale e por uma máscara de altitude. Assim, as encostas ganham ondulação geométrica coerente sem transformar a planície inteira em terreno acidentado. O mesmo sampler continua alimentando células ecológicas, rede de drenagem e malha; os afloramentos mantêm suas coroas planas e paredes laterais explícitas.
- Como essa alteração modifica elevação, drenagem e estado inicial, o identificador atual passa para `temperate-valley-l0-v14`. Os valores de amplitude são parâmetros artísticos/engenharia, não estimativas geológicas. A apresentação usa olho a 680 m na horizontal e 1500 m acima do centro; a projeção continua ortográfica.
- A captura `genesis-flora-v18-mesorelief.png` está no diretório de visualizações do Codex, fora do repositório. A revisão mantém a silhueta de vale e cordilheiras, com ondulações adicionais visíveis nas encostas. Ainda faltam copas mais contínuas, bancos mais orgânicos e uma água menos larga para se aproximar da referência.
- Em Release, seeds 1–3 foram avançadas até tick 144; os hashes, populações, intervalos de elevação, resíduos de balanço e benchmark atualizados estão em [20_L0_MODEL_AND_BENCHMARKS](20_L0_MODEL_AND_BENCHMARKS.md). A suíte completa passou com 19 testes.

## Geografia do cenário v15 — cristas independentes do rio

- A captura v23 revelou que as cordilheiras ainda repetiam a curva do meandro. O cenário v15 dá eixos independentes às cristas principais e soma seis cristas menores ramificadas dos seus ombros em direção às margens do vale.
- A Geosfera mantém o sampler contínuo e determinístico. Os novos campos alteram a altura física antes da geração do snapshot; D8, água inicial, gradiente solar e flora leem esse mesmo relevo. O renderizador continua somente reconstruindo a malha e ancorando objetos.
- A decisão segue a composição de soerguimento e drenagem da literatura consultada em [15_RESEARCH](15_RESEARCH.md). Termos de forma e amplitudes secundárias são heurísticas; não simulam tectônica ou erosão e não afirmam validar geomorfologia.
- A captura v24 `genesis-geometric-v24-ridge-network.png` e a captura final sem HUD v26 `genesis-geometric-v26-camera-depth.png` estão fora do repositório no diretório de visualizações do Codex. A inclinação maior deixa paredes e cristas mais legíveis; ainda não alcança a densidade e a delicadeza de vegetação da referência, e alguns canais seguem largos.
- `dotnet test Game.sln -c Release --nologo`: 22/22 aprovados. Seeds 1–3 avançaram até tick 144 com balanços conservativos dentro das tolerâncias. Benchmark de 60 s: média 58.62 FPS, P95 19.90 ms, P99 48.43 ms, working set 201.5 MiB; atende ao gate N4 do documento [20_L0_MODEL_AND_BENCHMARKS](20_L0_MODEL_AND_BENCHMARKS.md).

## Apresentação v26 — leitura do relevo e cursos curvos

- A câmera ortográfica passou de 24° para aproximadamente 30° da vertical (880 m horizontal, 1500 m de elevação), ampliando a leitura das faces físicas. O ribbon do rio interpola os receptores D8 por curvas Catmull–Rom, e cada vértice continua consultando a elevação do terreno; o campo hidrológico não muda.
- A captura de revisão omite o HUD apenas no modo `--capture`; o jogo interativo continua com a interface normal. Veja `genesis-geometric-v26-camera-depth.png` no diretório de visualizações do Codex.
- A cena destaca melhor o paredão e as encostas, mas ainda deixa canais largos e bosques em blocos de densidade visíveis. Em Release, 60 s: média 57.67 FPS, P95 30.52 ms, P99 49.53 ms, working set 198.2 MiB; cumpre as metas N4.

## Geografia v16 — cristas mais altas e platôs nas encostas

- As amplitudes das cristas principais sobem para 170–190 m a oeste e 132–150 m a leste; as seis cristas tributárias passam a 24–36 m. A Geosfera acrescenta quatro coroas de topo plano sobre os dois eixos montanhosos, com posições, perímetros, raios e alturas derivados da seed. Com as três feições existentes, cada seed gera sete faces rochosas ligadas ao mesmo campo de elevação que alimenta D8, água inicial, iluminação solar e flora.
- O limite superior do campo passou a 340 m para comportar as coroas sem achatá-las. O terreno continua como malha triangulada de altura real; os chunks mantêm vértices, normais e UV coincidentes nas bordas. A câmera segue ortográfica e passa para aproximadamente 38° da vertical (1050 m horizontal, 1350 m de elevação).
- A captura `genesis-geometric-v27-ridge-tablelands.png` registra a seed 1 sem HUD no diretório de visualizações do Codex. As montanhas e as faces laterais aparecem com mais clareza; a composição ainda tem canais largos e manchas de árvores perceptíveis em relação à referência.
- O cliente reutiliza a composição projetada de sprites nos seis ticks de 10 minutos entre as atualizações ambientais horárias; seed, revisão horária, resolução e estação formam a chave do cache.
- A validação Release passou com 22 testes. Seeds 1–3 avançaram até o tick 144 com altitude máxima entre 231,8 e 246,5 m e resíduos conservativos abaixo de `5.6×10⁻⁶` nas unidades reportadas pelo runner. O benchmark de 60 s será registrado em [20_L0_MODEL_AND_BENCHMARKS](20_L0_MODEL_AND_BENCHMARKS.md).

## Hidrologia v17 — soleiras preservando o leito

- O Priority-Flood condiciona depressões em um campo separado, sem escrever no relevo da Geosfera. A Hydrosphere agora mede soleira, capacidade e área submersa, e a água inicial nas bacias forma uma cota de lago nivelada abaixo do extravasamento.
- Isso acrescenta bacias de armazenamento ao vale, mas ainda não faz o lago subir com chuva nem extravasar dinamicamente. Canais seguem D8 e o fluxo atual; a rede potencial ainda não usa vazão hidráulica para determinar largura visual.
- A captura `genesis-hydrology-v17-level-pools.png` mostra os pools nivelados no relevo v16. A cena continua top-down ortográfica; o rio central ainda está largo/cruzado e as manchas de bosque ainda não têm a continuidade da referência.
- Os 24 testes Release passaram; o benchmark de 60 s atende P95/P99 por margem estreita. Os máximos de reconstrução de geometria seguem altos e estão discriminados em [20_L0_MODEL_AND_BENCHMARKS](20_L0_MODEL_AND_BENCHMARKS.md).
