# Arquitetura procedural da flora 3D — protótipo atual

> Os registros de relevo v16, hidrologia v17 e flora 3D descrevem protótipos do diretório de trabalho local em 21/10/2026. O código dessas alterações não integra esta publicação documental; os resultados históricos não foram reexecutados nesta revisão.

Pesquisa comparativa e mudanças da iteração 43: [referências de geração procedural de árvores](../reports/PROCEDURAL_TREE_REPOSITORY_RESEARCH_ITERATION_43_2026-10-22.md).

**Status:** primeira etapa estrutural implementada no cliente desktop MonoGame. Este documento descreve o protótipo de árvores tridimensionais existente; a técnica visual final continua em validação dentro da stack confirmada. A decisão e os critérios estão em [ADR-0004](../decisions/ADR-0004-visual-direction-and-renderer-evaluation.md). As árvores têm um grafo estrutural determinístico separado da malha MonoGame. Os cinco perfis de carvalho, pinheiro, bétula, bordo e salgueiro são arquétipos de forma temperada para o protótipo, não espécies calibradas nem dados botânicos validados.

## Divisão de responsabilidades

- `Game.Modules.Biosphere.ProceduralTreeGenerator` recebe perfil, altura, maturidade visual, detalhe e seed; devolve segmentos conectados de tronco/galhos e locais de inserção foliar. Perfis de folhas largas recebem brotos adicionais por uma passagem limitada de Space Colonization dentro do envelope da copa.
- `TreeRenderProfile` guarda parâmetros arquitetônicos, morfologia foliar e um hábito foliar coarse (`decídua`/`perene`) para as receitas visuais atuais. A geração não consulta câmera, GPU, relevo gráfico ou `MonoGame`.
- `TreeRenderProfile.CrownShape` determina um envelope qualitativo para o comprimento dos galhos primários (famílias cônica, esférica, hemisférica e cilíndrica); os perfis são arquétipos visuais e os fatores ainda não foram ajustados a medições de táxons.
- O preenchimento por Space Colonization começa na ponta terminal real dos galhos primários. Os pontos de atração continuam limitados a um envelope geométrico; não simulam busca por luz, competição ou crescimento temporal.
- Os perfis incluem um padrão qualitativo opcional de tamanho de folha ao longo da copa. O renderer o reduz a uma variação pequena e limitada no tamanho das lâminas de bétula e carvalho; é um proxy visual direcional baseado em um estudo de 29 árvores, não um coeficiente calibrado nem uma resposta à estação/ambiente. Ver [pesquisa de arquitetura de copa](../15_RESEARCH.md#gradiente-vertical-do-tamanho-foliar).
- `ProceduralFloraMeshBuilder` converte o grafo para tubos afunilados e folhas triangulares no cliente. A estação vem do snapshot; a apresentação não inventa pigmentos ou respostas a clima/doença que não existam no estado publicado.
- A colocação continua ancorada no sampler contínuo de terreno; uma esfera de limite remove árvores fora do frustum antes de gerar sua malha.
- A malha usa três níveis de ramificação conforme a altura projetada em pixels. As inserções primárias agora permanecem estáveis entre níveis; árvores menores ainda recebem menos ramificações finas e folhagem simplificada. No LOD mais distante, copas largas usam massas volumétricas facetadas e coníferas usam superfícies cônicas em anéis. A silhueta ainda pode variar parcialmente porque os níveis reduzem ordens secundárias.

As seeds visuais derivam da seed do mundo e da célula, sem consumir o fluxo aleatório da ecologia. A geometria é reconstruída quando o cache horário, a estação publicada ou a resolução mudam.

## Arquiteturas presentes

| Perfil | Organização atual |
|---|---|
| Carvalho | Eixo principal, galhos largos e copa espalhada |
| Pinheiro | Eixo dominante, ramos em espiral e forma cônica |
| Bétula | Tronco fino, copa vertical e ramos ascendentes |
| Bordo | Galhos distribuídos por níveis e copa arredondada |
| Salgueiro | Copa ampla com inclinação pendente nos ramos distais |

As ramificações usam níveis de ordem crescente, afunilamento e curvaturas determinísticas. Segmentos contíguos da mesma ordem agora compartilham uma malha varrida com seção transversal transportada ao longo da curva. A relação entre raio do tronco e raio dos galhos primários é configurável por perfil; o renderer alarga a primeira seção do galho, interpola uma expansão curta no tronco em cada inserção primária e afina as pontas. O custo medido aumentou 2,6% na galeria isolada de cinco árvores. Os pontos foliares ficam nos ramos secundários e terminais. As bifurcações ainda combinam tubos separados; a malha não funde os ramos nos nós e as interseções continuam visíveis de perto.

## Limites declarados

Esta etapa implementa perfis paramétricos, um grafo hierárquico e uma passagem de Space Colonization com orçamento fixo de brotos por indivíduo. Os atratores definem um envelope geométrico da copa; ainda não representam competição observada por luz, nem crescimento temporal. Ainda não implementa L-Systems, plasticidade do crescimento, idade persistida no estado ecológico, raízes simuladas, vento, sombras projetadas de copa, cache de módulos ou impostores distantes. O LOD atual só reduz ramos e folhagem em objetos menores na tela. A malha de folhas é uma aproximação estilizada; ela não equivale à fisiologia ou anatomia validada de espécies reais.

O snapshot contém biomassa lenhosa agregada por célula de 100 m², sem altura, diâmetro ou idade de cada árvore. A apresentação usa essa biomassa como proxy de cobertura da copa e aplica alturas de exibição por perfil de espécie, competição local e variação determinística por indivíduo visual. Esses valores são heurísticas de renderer, não alometria validada nem estágios etários simulados. Uma representação real de idades e crescimento individual depende de dados por organismo no contrato da Biosfera.

## Crescimento individual e características hereditárias

A geração procedural permite controlar estrutura e aparência ao longo do desenvolvimento, mas não constitui, sozinha, uma simulação de vida. A rota futura separa quatro responsabilidades:

1. **Estado individual da Biosfera:** identidade persistente, espécie, idade ou idade térmica, estágio de vida, porte, reservas/saúde, estado reprodutivo e dano. O snapshot deve expor apenas os atributos necessários à apresentação e à interação ecológica.
2. **Características herdáveis:** vetor de parâmetros quantitativos delimitado pela espécie e associado ao indivíduo/linhagem. Esse vetor pode controlar alometria, arquitetura, filotaxia e traços foliares. Chamá-lo de genótipo é uma abstração de jogo; não representa genes ou alelos empíricos sem marcadores e relações genótipo-fenótipo documentados para a espécie.
3. **Desenvolvimento e ambiente:** regras da Biosfera transformam idade, fase, recursos e campos ambientais em mudanças estruturais/reprodutivas. Luz, água do solo, nutrientes, temperatura, competição e distúrbios podem alterar a expressão fenotípica; esses efeitos precisam ser definidos no modelo ecológico e persistidos pelo snapshot, não inferidos da câmera.
4. **Apresentação:** o renderer constrói malhas, materiais, sombras e LOD a partir do snapshot e de seeds derivadas de identidade estável. Mudar o nível de detalhe ou a câmera nunca avança o organismo, altera sua herança nem consome aleatoriedade ecológica.

Uma futura árvore pode passar por germinação, juvenil, adulta reprodutiva, senescência e morte, mantendo gemas e pontos de inserção estáveis para que ramos, folhas e órgãos reprodutivos surjam ou desapareçam sem reconstruir uma espécie diferente a cada quadro. Recombinação/mutação e herança exigem identidade e dados por organismo; ainda não podem ser inferidas da biomassa agregada atual. A pesquisa e a limitação dos dados estão registradas em [15_RESEARCH.md](../15_RESEARCH.md).

## Materiais arbóreos procedurais no renderer

O cliente MonoGame agora gera em C# uma atlas em memória com texturas de casca, massa foliar e folhas para os cinco perfis existentes. As máscaras e nervuras diferenciam folha lobada (carvalho), acicular (pinheiro), oval serrilhada (bétula), palmada (bordo) e lanceolada (salgueiro). A malha detalhada usa essas texturas com alpha test; árvores pequenas usam cartões cruzados com envelope de copa por arquitetura. A atlas é material de apresentação, não dado biológico, e ainda não é calibrada com anatomia medida.

**Política de aparência:** a imagem de referência orienta composição, volume e densidade, não pigmentos de espécie. Os verdes e as cascas atuais são swatches qualitativos fixos por arquétipo, limitados a descritores de táxons representativos documentados em [15_RESEARCH.md](../15_RESEARCH.md); não são RGB medidos e não identificam árvores da simulação. O perfil visual ainda é escolhido por uma receita de renderer, pois o snapshot só contém biomassa agregada e não publica espécie por organismo. A seed varia a forma, nunca o pigmento. Mudanças cromáticas por estação, seca ou doença exigem identidade/traço de espécie e estado causal no snapshot. A queda foliar no inverno permanece uma demonstração do hábito declarado no perfil, não uma fenologia local validada. Flores não aparecem até que espécie e estágio reprodutivo façam parte do estado publicado.

A composição espacial dos cinco perfis também é estritamente visual: campos determinísticos locais criam agrupamentos de arquitetura, sem atribuir salgueiros à umidade ou coníferas à altitude. Essas relações ecológicas não foram calibradas para este mundo e foram removidas. Água e elevação publicadas continuam controlando o terreno e os limites de colocação já existentes; não escolhem identidade vegetal.

O solo não recebe mais uma mancha de deserto posicionada pela seed. O tom de superfície seca deriva da saturação e da água superficial atuais; isso descreve a condição momentânea do substrato e não classifica um bioma climático nem muda a presença das plantas. A composição de desertos só deve voltar quando houver um indicador ambiental adequado, e qualquer resposta de vegetação depende dos estados ecológicos correspondentes.

**Compatibilidade com fauna:** [Procedural Pixel Creatures, idlerunner00](https://github.com/idlerunner00/procedural-pixel-creatures) é uma referência de algoritmo para anatomia, genética procedural, rigs e animação da futura fauna, não uma licença para introduzir estilos incompatíveis. A cena 3D permanece estilizada e com detalhe controlado. Antes de fixar a linguagem final, comparar uma criatura exportada/portada com a ilha na mesma câmera, usando escala de pixel/projeção, legibilidade de silhueta, contorno, contraste de fundo, iluminação e sombra de contato como critérios comuns. Se a combinação ainda parecer colada, avaliar um tratamento compartilhado de renderização para cena e fauna; não decidir isso só pelo nome “pixel” do gerador.

O LOD de copa volumétrica da iteração 18 reduz textura foliar não resolvida à distância sem alterar pigmentos. Na iteração 20, esse LOD também reaproveita a máscara de copa procedural de cada perfil e interpola iluminação por vértice; a quantidade de triângulos permaneceu igual na captura da ilha inteira. Isso acrescenta leitura de superfície sem elevar o custo geométrico, mas a silhueta ainda é simplificada e não representa folhas individuais calibradas. A avaliação da futura fauna ainda precisa confirmar como o material 3D e o desenho derivado do algoritmo pixel-art coexistem na mesma câmera.

No passe de 21/10/2026, corrigimos a separação de buffers entre malha texturizada de árvore e cobertura de solo e reduzimos o tamanho de folhas/cachos distantes. A captura de ilha inteira caiu de 11,91 milhões para 6,86 milhões de vértices combinados de flora; o teste em 1280 × 720 registrou 60,07 FPS médios e 815 MiB de working set. A visualização aproximada permaneceu em 59,98 FPS e 948 MiB, com P95 de 27 ms. Esses números são do build Debug e da máquina desta tarefa, com outro cliente Release já aberto; não são uma garantia de desempenho isolado ou de hardware mínimo. As copas ganharam massa e folhas específicas, mas a vista de ilha ainda reduz espécies a manchas e o LOD próximo precisa de novos cortes antes de escalar a densidade.

Na iteração 17, um snapshot simulado até o verão (seed 2, dia 172) gerou 10.245.357 vértices de flora; o benchmark Debug pausado registrou 59,97 FPS médios, P95 de 26,36 ms e P99 de 27,72 ms. Working set (1.611 MiB) e memória privada (2.225,5 MiB) não são medições isoladas, e VRAM não foi medida. A captura e o restante da avaliação estão em [VISUAL_RENDERER_EVALUATION_ITERATION_17](../reports/VISUAL_RENDERER_EVALUATION_ITERATION_17_2026-10-21.md). A cena continua reprovada no gate visual.

A captura v26 está fora do repositório no diretório de visualizações da tarefa. A distribuição de folhagem está mais preenchida em torno dos ramos e as copas se afastam de volumes esféricos. A composição do vale, a rede de água, as sombras e a densidade geral da floresta ainda estão distantes da referência enviada. A cena não deve ser considerada como objetivo visual atingido.

Na medição Release de 30 s (seed 1, 1280 × 720, 6 ticks/s), o cliente registrou média de 55,77 FPS, P95 de 17,51 ms, P99 de 100,23 ms e working set de 368 MiB, com 1.710 árvores visíveis e 1.712.208 vértices de flora. A média voltou a ficar próxima de 56 FPS após reduzir a expansão foliar; o P99 segue mostrando picos que precisam de investigação.

## Próximos gates

1. Validar os perfis de copa com árvores isoladas e grupos, ajustando os envelopes e ângulos dos atratores por arquitetura.
2. Quando existirem atributos individuais no snapshot, ligar maturidade e crescimento a esses estados; até lá, manter explícita a separação entre heurística visual de cobertura/porte e idade biológica.
3. Suavizar bifurcações e variar a seção dos ramos; validar silhuetas isoladas por espécie e grupos no terreno inclinado.
4. Medir o LOD por resolução e distância, adicionar cache e geometria compartilhada e manter o custo estável ao aumentar a densidade.
5. Avaliar iluminação, auto-sombreamento e vento após a estrutura/LOD estarem estáveis.

Os gates seguem a ordem e os critérios da pesquisa de arquitetura botânica enviada pelo usuário. Os modelos paramétricos e o grafo não são tratados como substitutos de dados ou validação botânica.
