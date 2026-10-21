# Terreno geométrico 2.5D

## Direção

A câmera permanece ortográfica, fixa e inclinada o bastante para revelar faces e encostas. A altitude é dado físico da Geosfera; o cliente MonoGame amostra esse campo e o grava na coordenada vertical da malha. Textura e iluminação reforçam o relevo, mas não o substituem.

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

## Limites desta etapa

O mundo de simulação ainda mede 64 × 64 células e usa quatro chunks fixos por lado; não há LOD, streaming ou reconstrução seletiva. As cordilheiras têm encostas contínuas; três afloramentos possuem faces verticais explícitas. A drenagem usa o receptor D8 de maior declive e depressões locais, sem erosão que atualize o relevo, transporte de sedimentos ou chuva orográfica. O nível visual da água ainda aproxima a profundidade em vez de resolver uma superfície livre hidrostática. A flora usa faixas de altitude, inclinação e água determinísticas, não associações calibradas por espécie ou dados de campo.
