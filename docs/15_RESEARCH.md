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
- As copas são texturas C# de 64 × 64 pixels: coníferas têm camadas de ramos em 12 níveis; caducifólias usam 18 ramos de comprimentos variados e massas de folhas sobrepostas, com tronco curvo, lacunas e cores sazonais. A estrutura visual foi orientada pelos exemplos de `asterra-trees`/`asterra-leaves`, sem incluir imagens ou dependência JavaScript no runtime MonoGame.
- A cobertura de grama é suavizada para formar manchas e limitada às células que têm biomassa de grama no snapshot. O autotile agora usa uma grade 4 × 4 de subcélulas, ruído em duas escalas e máscaras de oito vizinhos para recortar cantos; tufos e flores continuam sendo detalhes de desenho.
- As rochas recebem silhuetas facetadas e detritos por uma regra de colocação visual baseada na elevação. As probabilidades e paletas são heurísticas de apresentação; não representam litologia ou estoque geológico simulado.
- `V` percorre quatro paletas sazonais de apresentação, começando no verão para permitir observar a copa cheia. O HUD separa `ART` de `SIM`: essa prévia muda as cores do terreno e das folhas, sem alterar calendário, biomassa, água, nitrogênio ou aleatoriedade ecológica.

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
