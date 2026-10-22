# Copas 3D mais contínuas no LOD distante — iteração 55

## Objetivo

Reduzir os vazios entre brotos terminais na vista distante da ilha. A direção continua sendo geometria 3D procedural: nenhum sprite ou billboard de árvore foi introduzido.

## Alteração

Em `ProceduralFloraMeshBuilder.AddDistantBroadleafMasses`, os lobos de copa se sobrepõem mais, com escalas horizontais e verticais ajustadas segundo a arquitetura hemisférica, esférica, cilíndrica ou pendente. A topologia de cada lobo e a quantidade simulada de folhagem não mudaram nesta iteração. Os LODs próximos continuam usando ramos e folhas procedurais.

## Evidência visual

Build Release antes das capturas: `dotnet build src/Game.Client.Desktop/Game.Client.Desktop.csproj -c Release --nologo` — 0 avisos e 0 erros.

| Captura | Cenário e observações |
|---|---|
| [Perspectiva](evidence/n4-seed1-day172-canopy-overlap-iteration55-perspective.png) | Seed 1, dia 172, zoom 3,4; 621 árvores, 0 billboards, 480.210 vértices de árvores e 656.127 vértices totais de flora. Build visual: 249,89 ms. As massas cobrem melhor os espaços, mas ainda formam grupos facetados. |
| [Ortográfica](evidence/n4-seed1-day172-canopy-overlap-iteration55-orthographic.png) | Mesma seed e dia, zoom 2,8; 621 árvores, 0 billboards, 539.796 vértices de árvores e 715.713 vértices totais de flora. Build visual: 246,40 ms. |
| [Galeria aproximada](evidence/n4-tree-canopy-overlap-iteration55-gallery-zoom38.png) | Cinco perfis procedurais, zoom 38; 0 billboards. As arquiteturas continuam distinguíveis, embora a folhagem próxima ainda pareça aglomerada. |

Os vértices variam entre projeções por diferenças de LOD e visibilidade. Tempos de build visual não são FPS. Não houve benchmark, avaliação completa da rubrica ou novo teste automatizado nesta iteração.

## Avaliação e próximos limites

A continuidade das copas melhorou em relação à captura anterior, mas o resultado ainda tem aspecto de aglomerados facetados. **A nota visual não foi elevada para 3/4 e N4 continua aberto.** O próximo passe deve inspecionar árvores isoladas e grupos em ângulos laterais/oblíquos e suavizar as transições entre copa, ramos e LOD antes de nova decisão da rubrica.

Esta alteração não muda os traços radiculares da iteração 53. Raízes aparentes continuam associadas a potenciais qualitativos distintos por perfil: carvalho alto, bordo intermediário, salgueiro discreto e pinheiro/bétula sem expressão aparente na referência. Esses arquétipos não substituem identidade de espécie individual ou condições locais de exposição, como cobertura do solo e erosão; tais variáveis não estão no snapshot.
