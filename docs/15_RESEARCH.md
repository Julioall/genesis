# Fontes científicas e de engenharia para a arquitetura (não equivalem a validação do jogo)

## Acoplamento e arquitetura

1. Hutton, E. W. H., Piper, M., Tucker, G. E. (2020). **The Basic Model Interface 2.0: A standard interface for coupling numerical models in the geosciences.** JOSS. https://doi.org/10.21105/joss.02317 — Interfaces entre modelos; não é benchmark de C# ou MonoGame.
2. Müller, S. et al. (2025). **FINAM is not a model (v1.0): a new Python-based model coupling framework.** Geoscientific Model Development. https://doi.org/10.5194/gmd-18-4483-2025 — Acoplamento multiescala, adaptação temporal, interfaces e remapeamento; a implementação é Python, não prova direta de nosso runtime.
3. Tan, W. J. et al. (2021). **Causality and Consistency of State Update Schemes in Synchronous Agent-based Simulations.** PADS. https://doi.org/10.1145/3437959.3459262 — causalidade/conflitos na atualização de agentes.

## Natureza, ecologia e modelagem de organismos

4. USGS Water Science School. **Water Cycle** (conteúdo educativo e panorama dos reservatórios). https://www.usgs.gov/special-topics/water-science-school/water-cycle — distinguir estoques/fluxos de água e estados físicos.
5. Anderson, E. et al. (2024). **Water cycle processes**. USGS General Information Product 235. https://doi.org/10.3133/gip235 — processos evaporação, transpiração, condensação, precipitação e infiltração.
6. USGS (2026). **Infiltration and the Water Cycle.** https://www.usgs.gov/water-science-school/science/infiltration-and-water-cycle — umidade, solo, cobertura e escoamento.
7. Grimm, V. et al. (2006). **A standard protocol for describing individual-based and agent-based models.** Ecological Modelling. https://doi.org/10.1016/j.ecolmodel.2006.04.023 — protocolo ODD, descrição de agentes, regras/escala/agenda/experimentos.
8. Prusinkiewicz, P. & Lindenmayer, A. (1990). **The Algorithmic Beauty of Plants.** Livro de referência de modelos de ramificação procedurais, não validação fisiológica automática de qualquer planta de jogo. https://algorithmicbotany.org/papers/#abop
9. FAO-56, Allen et al. (1998). **Crop Evapotranspiration — Guidelines for Computing Crop Water Requirements.** https://www.fao.org/4/x0490e/x0490e00.htm — referência para possível fase L1 de evapotranspiração; não obrigatória no modelo L0.

## Stack MonoGame e plataforma

MonoGame é a stack obrigatória do cliente. Estas fontes registram capacidades e limites relevantes para implementação e distribuição; a avaliação visual compara técnicas dentro do MonoGame, conforme [22_VISUAL_DIRECTION_AND_TECHNOLOGY.md](22_VISUAL_DIRECTION_AND_TECHNOLOGY.md).

10. MonoGame, [visão geral e modelo de framework](https://docs.monogame.net/articles/tutorials/building_2d_games/01_what_is_monogame/index.html): framework C#/.NET com ferramentas de apresentação construídas pelo projeto.
11. MonoGame, [Supported Platforms](https://docs.monogame.net/articles/getting_started/platforms.html), [renderização 3D](https://docs.monogame.net/articles/getting_to_know/whatis/graphics/WhatIs_3DRendering.html), [licença](https://github.com/MonoGame/MonoGame/blob/develop/LICENSE.txt) e [proposta de plataforma web #9515](https://github.com/MonoGame/MonoGame/issues/9515): issue/roadmap não demonstram compatibilidade do produto.

## Instruções ao Codex sobre referências

- Conferir fonte e aplicabilidade antes de promover fórmula para produção.
- Anotar se parâmetro provém de literatura, estimativa heurística ou calibração do jogo.
- Registrar intervalo, unidade, versão, propósito e limites; não extrapolar números sem justificativa.
- Citações apoiam o princípio ou fenômeno descrito, **não certificam** que a implementação própria é correta, estável ou rápida.

## Complemento consultado na revisão documental de 20/10/2026

- FAO, [Optimizing Nitrogen use on the farm](https://www.fao.org/4/y5146e/y5146e09.htm): apoia a interação entre disponibilidade hídrica, N e crescimento e a mineralização do N orgânico. Trata de culturas agrícolas; não fornece coeficientes calibrados para os perfis ecológicos fictícios deste MVP.
- Na revisão documental de 20/10/2026, as fontes MonoGame acima foram consultadas para separar plataforma oficialmente listada de trabalho em andamento. Uma issue aberta ou roadmap não demonstra compatibilidade do jogo; cada alvo precisa de validação própria.

As correções de reprodução, composição de N e transações são requisitos de consistência dos estoques escolhidos, não reprodução integral da fisiologia real nem validação experimental.

## Geografia procedural e apresentação do protótipo atual (consulta de 21/10/2026)

As observações abaixo registram pesquisa e decisões usadas no protótipo geométrico então existente. Elas não tornam 2.5D, top-down ou projeção ortográfica requisitos vigentes. MonoGame permanece a stack; a referência visual e a comparação de técnicas dentro dela estão no documento 22.

- Amit Patel, [Polygonal Map Generation for Games](https://theory.stanford.edu/~amitp/game-programming/polygon-map-generation/), atualizado em 2025: separa restrições de jogo/geografia (por exemplo, elevação e fluxo) de variação menos restrita por ruído. O próprio exemplo prioriza ilhas e montanhas afastadas da costa, uma premissa que não adotamos; aqui as cordilheiras são interiores e enquadram um vale.
- Red Blob Games, [Mapgen4](https://www.redblobgames.com/maps/mapgen4/) e [código-fonte](https://github.com/redblobgames/mapgen4): explora a edição de montanhas, vales e oceanos antes de derivar chuva, rios e biomas, além de uma projeção 3D estilizada. Isso é referência de fluxo e apresentação, não código, arte incorporada ou validação física de nosso gerador.
- A revisão v14 aplica variação coerente em escalas de 20 m e 10 m ao mesmo campo de altitude usado pela Geosfera, drenagem e colocação inicial de flora. Amplitudes de engenharia são atenuadas perto do vale e em cotas baixas; não foram calibradas com dados geomorfológicos. A malha continua sendo uma representação visual mais densa do campo físico, não uma simulação de erosão.

### Relevo organizado, soerguimento e drenagem (consulta de 21/10/2026)

- Cordonnier et al. (2016), [Large Scale Terrain Generation from Tectonic Uplift and Fluvial Erosion](https://doi.org/10.1111/CGF.12820), *Computer Graphics Forum* 35. Combina soerguimento prescrito, grafo hidrográfico e erosão por potência do escoamento para obter cristas e bacias ramificadas. Orienta separar o campo de soerguimento da drenagem; esta revisão não implementa a erosão iterativa do artigo.
- Tzathas et al. (2024), [Physically-based analytical erosion for fast terrain generation](https://doi.org/10.1111/cgf.15033), *Computer Graphics Forum* 43. Propõe solução analítica acelerada para a lei de potência do escoamento e inclui processos de encosta. Apoia distinguir ruído visual de evolução geomorfológica, mas não fornece parâmetros calibrados para esta grade.
- Schott et al. (2023), [Large-scale Terrain Authoring through Interactive Erosion Simulation](https://doi.org/10.1145/3592787), *ACM Transactions on Graphics* 42. Usa domínio de soerguimento e roteamento de fluxo para permitir cristas e vales controláveis com consistência hidrológica. É referência para acoplar feições estruturais ao relevo antes de extrair a rede D8.
- Teoh (2009), [RiverLand: An Efficient Procedural Modeling System for Creating Realistic-Looking Terrains](https://doi.org/10.1007/978-3-642-10331-5_44), *International Symposium on Visual Computing*. Constrói o terreno a partir de redes hidrográficas e conecta bacias, montanhas e falésias sem simulação fluida complexa.
- SciSpace localizou essas linhas de trabalho; uma busca pontual no Consensus recuperou os três artigos sobre soerguimento e erosão acima. A inferência de implementação para este mundo de 64 × 64 células é gerar eixos de crista determinísticos independentes do meandro, ramificá-los em cristas secundárias mais baixas e continuar calculando a drenagem a partir da altitude resultante. Amplitudes e larguras serão parâmetros de engenharia, não valores geológicos observados. Não será alegada erosão, calibração ou validação geomorfológica.
- A anotação anterior sobre roteamento de Game Studio refletia um estudo técnico histórico; não altera a stack MonoGame fixada para o projeto.

## Conferência pontual da segunda leitura

Em 20/10/2026 foram confirmados título/autoria/ano e pertinência geral das seguintes referências em fontes primárias:

- [BMI 2.0 — JOSS](https://joss.theoj.org/papers/10.21105/joss.02317): interfaces de acoplamento; não impõe framework ao MVP.
- [FINAM — GMD](https://gmd.copernicus.org/articles/18/4483/2025/): framework Python e acoplamento de modelos; referência conceitual, sem recomendar adoção no runtime C#.
- [Tan et al. — versão disponibilizada por coautor](https://philipp-andelfinger.net/pdfs/tan2021causality.pdf): causalidade e consistência de atualizações; a ordem concreta das fases do jogo é decisão própria a testar.
- [USGS — Water cycle processes](https://www.usgs.gov/publications/water-cycle-processes-poster): registro do pôster de 2024 e DOI GIP 235.
- [USGS — Infiltration and the Water Cycle](https://www.usgs.gov/water-science-school/science/infiltration-and-water-cycle): fatores qualitativos que influenciam infiltração; não valida o algoritmo L0.

A resolução direta de alguns DOIs falhou na ferramenta; as páginas das editoras/instituição e a versão do coautor permitiram a conferência acima. Não foi realizada revisão integral de todos os artigos nem calibração de modelos.

## Flora procedural e composição visual (consulta de 21/10/2026)

Consultas feitas no SciSpace e uma síntese de literatura no Consensus foram usadas para separar morfologia vegetal observável de técnicas de desenho. A resposta integral do Consensus, com marcadores e links de citação preservados, está em [síntese procedural de flora e paisagem](research/consensus-procedural-flora.md). Ela também reúne heurísticas gráficas; essas heurísticas não são evidência biológica.

- Nauber, Hodač, Wäldchen & Mäder (2024), [Parametrization of biological assumptions to simulate growth of tree branching architectures](https://doi.org/10.1093/treephys/tpae045), *Tree Physiology* 44. O estudo varia parâmetros funcionais (incluindo resposta à gravidade/luz e shedding) e compara contornos por morfometria geométrica em quatro formas arbóreas. Apoia separar receita de forma e fatores ambientais; a amostra/modelo não calibra nossos patches nem as silhuetas pixel art.
- Palubicki et al. (2022), [Ecoclimates: climate-response modeling of vegetation](https://doi.org/10.1145/3528223.3530146), *ACM Transactions on Graphics*. O trabalho acopla árvore, solo e gradientes locais de água, temperatura e luz e compara plausibilidade a estudos ambientais. É uma demonstração de simulação/visualização, não valida os coeficientes L0 deste projeto.
- Stützel & Kahlen (2016), [Virtual Plants: Modeling Plant Architecture in Changing Environments](https://doi.org/10.3389/fpls.2016.01734), *Frontiers in Plant Science*. Contextualiza modelos funcionais-estruturais que distinguem forma, função e ambiente; o texto é editorial e não fornece parâmetros para os nossos grupos funcionais.
- Prusinkiewicz (1999), [Modeling Plants and Plant Ecosystems: Recent Results and Current Open Problems](https://doi.org/10.1109/CGI.1999.10000). Discute L-systems e interação espacial, registrando problemas em árvores antigas, controle artístico e ecossistemas complexos. A gramática de desenho é uma opção computacional, não uma lei de crescimento.
- Li et al. (2021), [Learning to reconstruct botanical trees from single images](https://doi.org/10.1145/3478513.3480525), *ACM Transactions on Graphics*. A reconstrução usa volumes radiais de copa inferidos de imagens; depende de dados/imagens e não é requisito para geração por regras do L0.
- Blaise, Barczi & Jaeger (1998), [Simulation of the growth of plants: modeling of metamorphosis and spatial interactions in the architecture and development of plants](https://doi.org/10.1007/978-4-431-67941-7_6). Descreve eixos de referência, eventos discretos e interações espaciais/ambientais; é uma alternativa de maior custo que não adotamos para o patch agregado.

Como referências de implementação, foram estudados [EZ-Tree](https://github.com/dgreenheck/ez-tree), [MTree/GoodPie](https://github.com/GoodPie/modular_tree), [Space Colonization](https://github.com/jakobrichert/space-colonization), [SeedThree](https://github.com/SkyeShark/SeedThree) e [Unity Space Colonization](https://github.com/jasonwebb/unity-space-colonization). Eles demonstram técnicas de geração, não validam a biologia nem os coeficientes do Gênesis. A comparação inicial de algoritmos está em [iteração 43](reports/PROCEDURAL_TREE_REPOSITORY_RESEARCH_ITERATION_43_2026-10-22.md), a aplicação de regras de galho em [iteração 47](reports/PROCEDURAL_TREE_BRANCH_SHAPE_ITERATION_47_2026-10-22.md) e o estudo da estabilidade entre LODs em [iteração 48](reports/PROCEDURAL_TREE_LOD_SCAFFOLD_ITERATION_48_2026-10-22.md). Nenhum código, asset ou dependência desses projetos foi incorporado.

### Histórico de decisões para o cliente pixel-art anterior

As notas a seguir descrevem o cliente mínimo e a referência visual usados antes da revisão 2.7. A geração procedural por seed e a separação de arte e simulação continuam válidas; quantização de pixels e cache de texturas são escolhas históricas, não requisitos. A mudança v13 de densidade de cobertura também é um registro histórico do cenário; nenhum parâmetro foi alterado nesta revisão visual.

**Decisão artística histórica:** gerar terreno, rochas, gramíneas, estrutura lenhosa e pequenas massas de folhas por código; quantizar pixels e cachear texturas por receita/variante visual/estação. A seed visual é derivada separadamente e nunca entra em fluxos ecológicos. A camada de folhas pode mudar aparência sazonal a partir do calendário, mas não cria folhas como estoque, nem muda biomassa, cobertura, água ou N. Silhuetas, cores de estação, agrupamento de rochas e densidade de pixels eram escolhas de legibilidade, não inferências botânicas ou geológicas.

**Ajuste do cenário de cobertura v13:** para aproximar os bosques visíveis das encostas da referência, a colocação lenhosa inicial passou de 27%/40%/12% para 30%/62%/40% nas faixas de altitude L0 e o limite heurístico de inclinação passou de 0.70 para 1.0 m/m; a exclusão de células alagadas foi mantida. Essas frações alteram o estado inicial e, por isso, a versão do cenário foi incrementada. São uma escolha de densidade para o jogo, não estimativa ecológica, calibração de campo ou resultado dos estudos abaixo. Biomassa, crescimento, senescência e decomposição continuam conforme a ficha L0.

**Registro de implementação anterior:** o Game Studio skill foi consultado como guia de organização de loop, superfícies de apresentação, pipeline visual e validação. As notas abaixo registram a composição MonoGame do protótipo existente. A revisão atual fixa MonoGame e mantém em avaliação apenas a técnica de apresentação; a simulação continua headless e independente do cliente gráfico.

- MonoGame, [Supported Platforms](https://docs.monogame.net/articles/getting_started/platforms.html): a documentação lista DesktopGL para Windows/macOS/Linux. A escolha de `MonoGame.Framework.DesktopGL` é de compatibilidade do cliente, não uma promessa de navegador.
- MonoGame, [Texture Sampling and Tiling Backgrounds](https://docs.monogame.net/articles/tutorials/building_2d_games/18_texture_sampling/index.html): `PointClamp` preserva pixels nítidos em texturas ampliadas; é uma decisão de renderização, sem consequência para o estado do mundo.


## Referências do contrato L0 (consulta de 21/10/2026)

SciSpace foi usado para localizar trabalhos sobre balanço diário de água/radiação, eficiência de uso de luz e decomposição; os valores de jogo não foram extraídos como calibração desses estudos.

- Davis et al. (2017), [SPLASH v1.0 — GMD](https://gmd.copernicus.org/articles/10/689/2017/), referência para formas compactas de radiação, evapotranspiração, solo e escoamento.
- Allen et al. (1998), [FAO-56](https://www.fao.org/4/X0490E/X0490E00.htm), padrão de referência para evapotranspiração; o L0 usa deliberadamente um proxy sem vento/vapor medidos.
- Kiniry et al. (1989), [radiation-use efficiency](https://doi.org/10.1016/0378-4290(89)90023-3), apoio à forma proporcional entre PAR interceptada e produção vegetal em culturas específicas.
- Bolker, Pacala & Parton (1998), [simplifying the Century decomposition model](https://doi.org/10.1890/1051-0761(1998)008%5B0425%3ALAOSDI%5D2.0.CO%3B2); Lutz et al. (2019), [LPJmL](https://gmd.copernicus.org/articles/12/2419/2019/), referências para cinética de primeira ordem modulada por condições ambientais.

Essas fontes apoiam escolhas de forma e não testam coeficientes, perfis animais, chuva sintética ou tolerâncias do cenário `temperate-valley-l0-v13`.

## Revisão visual da referência anterior de paisagem (21/10/2026)

Esta seção documenta uma imagem analisada antes da revisão 2.7 e não corresponde às duas novas imagens de referência, arquivadas em `docs/references/visual-direction/`. A descrição de pixel art abaixo é histórica; a direção vigente e os critérios de validação estão em `docs/22_VISUAL_DIRECTION_AND_TECHNOLOGY.md`. Nenhuma imagem é dado científico ou asset do runtime.

A imagem anterior mostrava um recorte panorâmico de vale com terreno contínuo sem contornos de célula, grupos irregulares de coníferas e árvores de folhas largas, afloramentos, cobertura baixa e água com margens sinuosas. Essa composição era uma referência artística, não evidência sobre o relevo ou a flora do cenário L0.

- A apresentação MonoGame usa uma transformação oblíqua suave da superfície e mantém os sprites de plantas na tela, com ordenação pelo eixo de profundidade projetado. O relevo físico continua vindo de `TerrainModel`; a transformação não cria degraus, voxels ou elevação nova.
- A projeção agora centraliza as 64 × 64 células na janela de 1600 × 900. O painel lateral virou um HUD curto no canto, com inspeção perto do cursor; `H` oculta a interface para comparar a paisagem diretamente com a referência.
- A paleta do solo usa interpolação bilinear, ruído visual determinístico em mais de uma escala e sombreamento derivado da elevação. A inspiração para a suavização regional 5×5 e o hillshade veio da leitura do renderer em `Spectators-of-Life`; esse checkout foi consultado somente para leitura. Os coeficientes desta implementação são artísticos e não substituem a topografia L0.
- As copas eram texturas C# de 64 × 64 pixels: coníferas tinham camadas de ramos em 12 níveis; caducifólias usavam massas de folhas sobrepostas, com tronco curvo, lacunas e uma paleta sazonal. A estrutura visual foi orientada por bibliotecas de referência para árvores e folhas, sem incluir imagens ou dependência JavaScript no runtime MonoGame. **A paleta sazonal foi removida na revisão de 21/10/2026**; a política vigente exige traço de espécie e estado causal antes de exibir pigmento sazonal.
- A cobertura de grama é suavizada para formar manchas e limitada às células que têm biomassa de grama no snapshot. O autotile agora usa uma grade 4 × 4 de subcélulas, ruído em duas escalas e máscaras de oito vizinhos para recortar cantos; tufos e flores continuam sendo detalhes de desenho.
- As rochas recebem silhuetas facetadas e detritos por uma regra de colocação visual baseada na elevação. As probabilidades e paletas são heurísticas de apresentação; não representam litologia ou estoque geológico simulado.
- O controle `V` e o HUD `ART/SIM` existiam na implementação histórica para percorrer quatro paletas de apresentação. Foram removidos na revisão de 21/10/2026: estação exibida agora vem do snapshot, sem prévia de cores que não estejam apoiadas em traços e estados biológicos.

Esta passada ainda não reproduz o alvo completo: o cenário inicial pode ter pouca água superficial, a grade L0 tem resolução limitada e a imagem gerada ainda depende da distribuição de plantas existente no snapshot. Não se desenha um rio sem estado de água que o sustente, nem se aumenta a população para preencher a cena. Esses limites precisam ser considerados em qualquer nova evolução visual.

## Referências de composição e terreno (consulta de 21/10/2026)

- [DwarfCorp](https://github.com/Blecki/dwarfcorp) serve como referência de mundo 3D e renderização em chunks. Seu repositório declara código sob licença modificada MIT, mas mantém imagens, texturas, modelos, efeitos sonoros e música como conteúdo proprietário; usamos princípios de organização, sem copiar esses assets.
- [Canvas of Kings](https://store.steampowered.com/app/2498570/Canvas_of_Kings/) é um editor de mapas 2D que combina assets desenhados à mão em caminhos e áreas editáveis; a página oficial diz que ele ainda não gera mapas totalmente sozinho. É útil para estudar composição e variação de objetos, mas não como gerador de relevo 3D. O ZIP local identificado como SteamUnlocked não foi executado; a página oficial oferece uma demo.

### Algoritmos de geografia procedural

- Amit Patel, [Polygonal Map Generation for Games](https://theory.stanford.edu/~amitp/game-programming/polygon-map-generation/): a ideia útil é separar propriedades impostas por regras do mapa, como elevação e fluxo, da variação estética não restringida; relações de vizinhança em um grafo ajudam a organizar drenagem e regiões. A página permite aplicar as técnicas separadamente; não copiamos seu código nem adotamos a hipótese de ilhas com montanhas no centro.
- Red Blob Games, [Mapgen2](https://github.com/redblobgames/mapgen2) e [Mapgen4](https://www.redblobgames.com/maps/mapgen4/): Mapgen4 calcula distâncias no grafo a partir dos picos e mistura um campo de montanhas com colinas de baixa amplitude. A mesma malha suporta drenagem, ordem de fluxo e um modelo de vento, umidade e chuva orográfica. O repositório declara Apache 2.0; este projeto usa os conceitos descritos e não incorpora código ou bibliotecas desses repositórios.
- A discussão comunitária [sobre mapas estilizados com planícies, patamares, rampas e montanhas](https://www.reddit.com/r/proceduralgeneration/comments/1pbpmch/how_to_approach_dynamically_generating_stylized/) é uma fonte de hipóteses, não uma especificação validada. Ela reforça que patamares e transições precisam ser modelados como feições, em vez de esperar que ruído isotrópico gere todos os acidentes geográficos.

No cenário atual, os dados físicos seguem em uma grade de 64 × 64 células: campos determinísticos descrevem vale, cadeias, depressões e afloramento; a malha gráfica subdivide o snapshot sem mudar a resolução da simulação. Um grafo geográfico de menor resolução continua como opção para controle por regiões e redes de drenagem mais ricas quando a escala exigir; não foi adicionado como segunda representação concorrente neste marco.

## Vegetação em gradientes topográficos e estrutura de copa (SciSpace, 21/10/2026)

Uma busca semântica no SciSpace localizou os estudos abaixo. Eles ajudam a justificar que altitude, aspecto, declive, clima e estrutura de copa produzam variação espacial; não fornecem parâmetros para o cenário fictício do Genesis. Os resumos disponíveis não bastam para uma revisão sistemática, e os resultados remotos/regionais não calibram cobertura ou morfologia de sprites.

- Rich & Fu (2004), [Topoclimatic Habitat Models](https://professorpaul.com/publications/rich_fu_2000_gisem4.pdf): combina lapse rate modificado e modelo geométrico de insolação para estimar superfícies microclimáticas; o texto relata previsão de padrões de vegetação melhor que usar apenas elevação, aspecto e declive. Limites citados incluem dependência de dados locais, validação difícil e necessidade de conhecimento biológico para transferir adequação de habitat.
- Tian et al. (2024), [Global assessment of vegetation patterns along topographic gradients](https://doi.org/10.6084/m9.figshare.27073115.v1): usa máximos e amplitude sazonal de verde derivados de Sentinel-2, comparados com aspecto, declive e altitude em células montanhosas de 0.15°. Encontra diferenças entre regiões e efeitos relevantes de cada fator. O material localizado é um registro de repositório/resumo; a resolução espacial e as métricas de verde não identificam espécies ou justificam taxas locais de ocupação.
- Zhang et al. (2025), [Topographic effects on vegetation phenology in response to climate change on the southern slope of Qilian Mountains](https://doi.org/10.13287/j.1001-9332.202507.022): analisa série MODIS-NDVI de 2002–2020 com topografia e meteorologia, correlação e modelagem de equações estruturais; aponta mediação por fatores térmicos/hídricos e importância de altitude e aspecto. A evidência é regional e de fenologia por proxy, não de uma regra universal de densidade lenhosa.
- Rawal et al. (2025), [Pattern of forest structure and species regeneration along elevation gradients and aspects](https://doi.org/10.1007/s44372-025-00381-3): amostra 105 parcelas entre 2100–3300 m e usa PCA/agrupamento para comparar composição, densidade e regeneração em três aspectos; a densidade arbórea e a regeneração variam com altitude e encosta, com gargalos em áreas altas. As espécies e as pressões locais do Himalaia não são transferíveis ao vale temperado do jogo.
- Xiang et al. (2025), [Global 500m Resolution Tree Crown Structure Dataset](https://doi.org/10.5194/egusphere-egu25-16271): resumo de conferência descreve Random Forest para relações entre altura, largura e profundidade de copa, usando mais de 340 mil observações e oito tipos funcionais; reporta R² > 0.5 e RMSE < 3 m. A escala de 500 m, o formato de resumo e o erro reportado tornam o trabalho orientação para variação alométrica, não base para sprites ou medidas do cenário.
- Mu et al. (2026), [Spatial Diagnosis of Climatic and Landscape Controls on Forest Leaf Area Index Across China](https://doi.org/10.3390/f17020203): integra LAI, clima, topografia e estrutura de paisagem com SHAP, GeoDetector e diagnósticos não lineares; o resumo destaca regimes regionais distintos e saturação de precipitação. O resumo consultado não explicita incertezas/limitações do método e a geografia observada é China continental; não permite inferir causalidade ou coeficientes para o jogo.

Esses resultados apoiam usar campos ambientais e mais de uma escala espacial para variar colocação e forma. A próxima revisão de flora pode expor aspecto/insolação, umidade do solo e substrato como fatores visuais explicitamente derivados do snapshot; até isso existir e ser testado, a distribuição atual depende somente dos fatores documentados no código. A síntese anterior do Consensus, com métodos de geração de árvores e limites de L-systems/space-colonization, permanece em [consensus-procedural-flora.md](research/consensus-procedural-flora.md). Uma tentativa de nova consulta Consensus nesta rodada não foi aceita porque a conta informou apenas um crédito restante e exigiu plano Pro; nenhum resultado novo do Consensus foi usado.

## Alometria de altura e copa em povoamentos mistos (SciSpace, 21/10/2026)

Uma busca no SciSpace sobre altura, competição e dimensão de copa encontrou estudos com árvores medidas individualmente. Eles apoiam variar arquiteturas por espécie e considerar competição e sítio, mas não fornecem coeficientes aplicáveis diretamente ao Genesis:

- Savine, Cordonnier e Ligot (2025), [Robust height-diameter allometries for 42 European tree species: stand characteristics and structure matter](https://doi.org/10.1101/2025.09.29.679156), usa 269.976 observações de altura em 49.234 parcelas para 42 espécies. Área basal, diâmetro quadrático médio e estrutura do povoamento afetam as relações altura-diâmetro; para várias espécies, competição maior corresponde a árvores mais altas para um mesmo diâmetro. É um preprint.
- Esfahani, Meng e Wong (2025), [Species-specific modeling of tree diameter at breast height using tree height and relative density](https://doi.org/10.1016/j.foreco.2025.123372), usa 1.807 árvores em 653 parcelas de floresta temperada mista. Modelos específicos por espécie melhoram ao incluir densidade relativa como índice de competição; a qualidade varia entre grupos de espécies.
- Buchacher e Ledermann (2020), [Interregional Crown Width Models for Individual Trees Growing in Pure and Mixed Stands in Austria](https://doi.org/10.3390/f11010114), modela larguras de copa de cerca de 8.900 árvores em 1.508 parcelas. As relações consideram dimensão da árvore, competição, mistura de espécies e características do sítio; respostas de copa diferem entre grupos.
- Liu et al. (2023), [Disentangling interspecific and intraspecific variability in height-diameter allometry](https://doi.org/10.1093/forestry/cpad048), analisa 569.502 pares altura-diâmetro de 20 espécies em 53.808 parcelas e encontra variação entre e dentro de espécies associada a sítio, clima e competição.

Os dados do Genesis são biomassa lenhosa por área de célula, não diâmetro, altura ou biomassa de cada árvore. Portanto, não é possível estimar uma alometria ou idade individual com essas observações. A iteração visual 142 usa alturas de exibição por perfil, variação individual derivada da seed e cobertura da copa derivada da função já existente `BotanyModel.Cover`; competição de vizinhança altera o porte apenas modestamente. São heurísticas de apresentação, sem novos parâmetros científicos ou calibração empírica.

## Arquitetura procedural, crescimento e variação hereditária (SciSpace e fontes primárias, 21/10/2026)

Uma nova consulta ao SciSpace encontrou linhas de pesquisa úteis para separar arquitetura da árvore, desenvolvimento e renderização. A tentativa de consulta ao Consensus nesta rodada retornou a mensagem de cota mensal esgotada; não foi usada como evidência nova.

- Pałubicki et al. (2009), [Self-organizing tree models for image synthesis](https://www.algorithmicbotany.org/papers/selforg.sig2009.html): estrutura ramos a partir de gemas que competem por luz ou espaço, com regulação interna. É uma base melhor para desenvolvimento estrutural que ramificação fractal indiferenciada; o próprio artigo é um modelo de síntese de formas, não uma simulação completa de carbono, hidráulica e sobrevivência.
- Prusinkiewicz et al. (2018), [Modeling plant development with L-systems](https://www.algorithmicbotany.org/papers/modeling-plant-development-with-l-systems.html): descreve sistemas de desenvolvimento em várias escalas e distingue modelos descritivos de mecanísticos. Regras de reescrita podem codificar ontogenia e sinais ambientais, mas a escolha do formalismo não valida os parâmetros biológicos.
- Jucker et al. (2025), [The global spectrum of tree crown architecture](https://www.nature.com/articles/s41467-025-60262-x): mede relações de altura, diâmetro do tronco e copa em 374.888 árvores de 1.914 espécies. O estudo relaciona altura por diâmetro principalmente à água disponível e competição por luz; largura da copa associa-se sobretudo à exposição a vento e fogo. Isso apoia combinar arquitetura herdável com plasticidade do ambiente, sem transferir coeficientes globais diretamente ao cenário fictício.

O HTML de referência fornecido foi inspecionado como material técnico local. Seu G7 define loci paramétricos para dimensões, lobos, nervuras, pigmento, rigidez e longevidade, com expressão fenotípica determinística e limites por espécie; o próprio código identifica esse mecanismo como modelo estilizado, não calibrado como simulador genético empírico. Os pontos de inserção persistentes e o cache/LOD do G8 também são ideias úteis. Foram reaproveitados os conceitos gerais; o renderer MonoGame gera seus materiais em C# e não depende do runtime JavaScript do HTML.

Para o Genesis, `genótipo` deve significar inicialmente um vetor de características quantitativas herdáveis e delimitadas por espécie, não DNA nem um mapa molecular. Fenótipo precisa combinar esse vetor com idade/estágio, estado fisiológico e ambiente. Recombinação, mutação e seleção só passam a ser parte da ecologia quando houver identidade persistida por organismo e esses estados fizerem parte dos snapshots. A seed visual atual garante repetibilidade de apresentação, mas não cria herança biológica.

O renderer continua sendo uma leitura: câmera, LOD, malha e atlas gerado não avançam idade, não alteram genótipo e não consomem RNG ecológico. O snapshot atual ainda contém biomassa lenhosa agregada por célula, sem árvores individuais persistidas; por isso, nascimento, crescimento, reprodução e morte por indivíduo permanecem trabalho futuro da Biosfera, não recursos já simulados.

### Gradiente vertical do tamanho foliar

Hagemeier e Leuschner (2019), [Functional Crown Architecture of Five Temperate Broadleaf Tree Species](https://www.mdpi.com/1999-4907/10/3/265), mediram 29 árvores adultas de cinco espécies temperadas de folhas largas, incluindo bétula-prateada (*Betula pendula*) e carvalho-séssil (*Quercus petraea*). O tamanho das folhas aumentou continuamente com a profundidade da copa em *B. pendula*; em *Q. petraea*, atingiu o máximo no meio da copa e diminuiu em direção à parte sombreada inferior. Área foliar específica e ângulo também variaram verticalmente nas espécies estudadas, mas não são representados nesta alteração.

Como ensaio geométrico, o renderer aplica apenas a direção observada de variação do tamanho foliar aos arquétipos visuais de bétula e carvalho. Uma curva ilustrativa limitada a 0,90–1,10 escala comprimento e largura das lâminas conforme a altura relativa dentro do envelope da copa; os limites são escolhas gráficas, não coeficientes do artigo. Os demais arquétipos mantêm folhas de tamanho uniforme. A amostra e os resultados não calibram essa curva para o ecossistema fictício, nem justificam mudanças de cor, densidade, crescimento, sobrevivência ou distribuição por habitat.

## Fenologia e pigmento foliar: limite da representação atual

As cores de outono não são uma resposta visual universal ao calendário. Renner e Zohner (2019) sintetizam diferenças filogenéticas e regionais na ocorrência de folhas vermelhas/amarelas e discutem interações de pigmentos, irradiância e temperatura durante a senescência. Xie et al. (2022) observaram variação de cor e fenologia entre 46 espécies lenhosas decíduas temperadas; a coloração variou entre vermelho, amarelo, verde e estados intermediários, com diferenças entre espécies e anos. A meta-análise de Liu et al. (2015) também mostra que latitude altera o peso relativo de fotoperíodo e temperatura na data de senescência. Nenhuma dessas fontes fornece uma paleta ou coeficiente transferível diretamente para o cenário fictício do Genesis.

- Renner & Zohner (2019), [The occurrence of red and yellow autumn leaves explained by regional differences in insolation and temperature](https://doi.org/10.1111/nph.15900), *New Phytologist* 224(2):432–439.
- Xie et al. (2022), [Coloration and phenology manifest nutrient variability in senesced leaves of 46 temperate deciduous woody species](https://doi.org/10.1093/jpe/rtab105), *Journal of Plant Ecology* 15(4):700–711.
- Liu et al. (2015), [Changes in autumn senescence in northern hemisphere deciduous trees: a meta-analysis of autumn phenology studies](https://doi.org/10.1093/aob/mcv055), *Annals of Botany* 116(6):875–888.

Consequências no renderer deste protótipo:

- O calendário visual segue a estação publicada no snapshot; não existe mais prévia independente que force cores de uma estação.
- Folhagem sadia usa swatches qualitativos fixos por arquétipo, apoiados em descritores botânicos de espécies representativas na seção abaixo. Como a coluna de espécie ainda não existe no snapshot, eles não são uma atribuição taxonômica nem valores RGB medidos; a seed nunca sorteia pigmento, e não há amarelo/laranja sazonal ou por indivíduo.
- A perda de folhas no inverno usa o traço explícito `LeafHabit` dos perfis de demonstração. Os cinco perfis continuam sendo arquétipos temperados estilizados, não táxons ou parâmetros fenológicos validados. A regra grosseira deve ser substituída por fenologia por espécie/local quando esses dados existirem.
- A distribuição de árvores e os arbustos de sub-bosque continuam receitas visuais sobre biomassa/ambiente agregado, não populações de espécies simuladas. Flores foram retiradas da cena porque o snapshot não expõe espécie, florescimento ou estado reprodutivo. Amarelecimento por seca/doença, pigmento de outono e reprodução visível aguardam estados e causas correspondentes na Biosfera.

Essa simplificação é intencional: a referência visual não justifica pigmentos nem comportamentos. A próxima representação deve conservar agrupamento, volume e leitura da paisagem por geometria, silhueta, densidade, luz e sombra; detalhes que custem GPU/CPU devem ser aceitos pelo ganho visual medido, não por quantidade de polígonos.

## Casca: diferenças de espécie e limite da representação

Guias de identificação descrevem diferenças estáveis de casca: o carvalho-inglês é cinza-acastanhado e torna-se rugoso/fissurado com a maturidade; o pinheiro-silvestre tem casca escamosa castanho-alaranjada, mais avermelhada na copa e castanha na base; a bétula-prateada tem casca branca, com base escura e fissuras que se acentuam com a idade; o bordo-campestre é castanho-claro e descamativo; o salgueiro-cinzento é cinza-acastanhado e fissurado com a idade.

- [Woodland Trust — oak bark](https://www.woodlandtrust.org.uk/trees-woods-and-wildlife/british-trees/oak-tree-wildlife/).
- [Woodland Trust — Scots pine](https://www.woodlandtrust.org.uk/trees-woods-and-wildlife/british-trees/a-z-of-british-trees/scots-pine/).
- [Woodland Trust — silver birch](https://www.woodlandtrust.org.uk/trees-woods-and-wildlife/british-trees/a-z-of-british-trees/silver-birch/).
- [Woodland Trust — field maple](https://www.woodlandtrust.org.uk/trees-woods-and-wildlife/british-trees/a-z-of-british-trees/field-maple/).
- [Woodland Trust — grey willow](https://www.woodlandtrust.org.uk/trees-woods-and-wildlife/british-trees/a-z-of-british-trees/grey-willow/).

Na iteração 18, a casca deixou de usar creme em quase todas as árvores: carvalho e salgueiro usam tons cinza-acastanhados, pinheiro usa castanho mais quente, bordo castanho-claro e bétula permanece clara. São swatches qualitativos de apresentação baseados nesses descritores, não medidas espectrais nem perfis taxonômicos validados. Não adicionamos o escurecimento basal/gradiente de idade: `TreeVisual.Maturity` deriva de cobertura lenhosa agregada para desenvolver a copa e não é idade individual. Esse efeito precisa aguardar idade/estágio real por árvore no snapshot.

## Raízes superficiais e exposição

O carvalho tem a maioria das raízes nos 30 cm superiores do solo, estendendo-se além da copa; a bétula-prateada é descrita com raízes compactas e rasas em espaços menores, além de raízes amplamente distribuídas; no salgueiro-cinzento, ramos que tocam o solo podem formar raízes. Esses traços descrevem distribuição ou propagação radicular — não significam que raízes devam aparecer acima do solo em todo exemplar.

- [Woodland Trust — oak roots](https://www.woodlandtrust.org.uk/trees-woods-and-wildlife/british-trees/oak-tree-wildlife/).
- [Woodland Trust — silver birch roots](https://www.woodlandtrust.org.uk/blog/6-uk-trees-to-plant-in-small-gardens-for-climate-change/).
- [Woodland Trust — grey willow propagation](https://www.woodlandtrust.org.uk/trees-woods-and-wildlife/british-trees/a-z-of-british-trees/grey-willow/).

Arquitetura radicular e exposição acima do solo são relacionadas, mas não idênticas. A biologia da espécie define o fenótipo radicular — distribuição, alcance e potencial de expressão superficial — enquanto a exposição efetivamente observável também depende de idade, solo descoberto e erosão. Essas causas locais ainda não fazem parte do snapshot por organismo.

Estudos primários reforçam esse limite: em pinheiros-silvestres (*Pinus sylvestris*) expostos por erosão laminar, foram observadas respostas anatômicas que permitem identificar quando ocorreu a exposição; isso não torna a exposição uma propriedade visual constante da espécie ([Rubiales et al., 2008](https://doi.org/10.5194/nhess-8-223-2008)). Em carvalhos-pedunculados (*Quercus robur*) de 150 anos, medidos em solo profundo e sem restrição edáfica, foram descritos componentes radiculares superficiais e profundos — arquitetura subterrânea, não prova de raízes visíveis acima do terreno ([Lucot & Bruckert, 1992](https://doi.org/10.1051/forest:19920503)). Uma comparação por tomografia de quatro árvores ornamentais em recipientes também encontrou padrões espaciais diferentes entre táxons, mas seu pequeno ensaio de viveiro não calibra árvores adultas em solo natural ([Bauerle & Centinari, 2014](https://doi.org/10.21273/HORTSCI.49.1.44)).

O renderer foi corrigido para não decidir a presença de raízes por uma rolagem visual comum. A Biosfera agora fornece `TreeRootTraits` distintos por perfil, incluindo `SurfaceRootExpressionPotential`: o perfil de carvalho tem maior potencial de expressão superficial, bordo intermediário, salgueiro discreto, e os perfis de pinheiro e bétula não desenham raízes aparentes na condição de referência. Variações determinísticas mudam a forma individual, mas não criam raízes quando o perfil não expressa esse potencial. Esses coeficientes continuam sendo arquétipos qualitativos não calibrados; potencial biológico não torna a exposição inevitável, e referências de raízes rasas não provam, por si só, exposição acima do solo. A próxima modelagem deverá combinar o fenótipo com condição de cobertura/erosão do local, em vez de tratar a exposição como acessório universal.

## Tons de folhagem sadia no verão

Descritores de espécies também sustentam diferenças limitadas dentro dos verdes: agulhas de pinheiro-silvestre são azul-esverdeadas, folhas de bétula-prateada são verde-claras e folhas de bordo-campestre são verde-escuras e brilhantes. Folhas de salgueiro-osier são verde-escuras com pelos prateados na face inferior. As referências registram ainda mudanças sazonais para amarelos/dourados, mas essa mudança requer estação/estado fisiológico compatível e não é aplicada à captura de verão.

- [Woodland Trust — Scots pine](https://www.woodlandtrust.org.uk/trees-woods-and-wildlife/british-trees/a-z-of-british-trees/scots-pine/).
- [Woodland Trust — silver birch](https://www.woodlandtrust.org.uk/trees-woods-and-wildlife/british-trees/a-z-of-british-trees/silver-birch/).
- [Woodland Trust — field maple](https://www.woodlandtrust.org.uk/trees-woods-and-wildlife/british-trees/a-z-of-british-trees/field-maple/).
- [Woodland Trust — osier willow](https://www.woodlandtrust.org.uk/trees-woods-and-wildlife/british-trees/a-z-of-british-trees/osier-willow/).

O renderer usa swatches RGB qualitativos e fixos por arquétipo no snapshot de verão: azul-esverdeado para conífera, verde mais claro para bétula e verde mais escuro para bordo; carvalho e salgueiro permanecem em verdes médios. Não são valores de clorofila/refletância medidos, não mudam por seed nem por câmera e não inferem seca/doença. Como a coluna taxonômica ainda não existe no snapshot, essas cores são apenas hipótese de apresentação inspirada em descritores, não uma previsão de espécies nesta ilha. Quando espécies observadas ou populacionais forem publicadas, o vínculo deverá migrar para os estados persistidos. A antiga região amarelada de deserto era posicionada pela seed e foi removida; a indicação atual de superfície seca deriva da saturação do solo e da água superficial do snapshot, sem recolorir folhas ou ocultar plantas.
