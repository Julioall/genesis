# Contato de rochas com o relevo — iteração 57

## Objetivo

Corrigir aberturas visíveis e rochas tombadas em encostas na malha 3D de avaliação, sem alterar colocação, seed, elevação física ou estado geológico.

## Alteração

`ProceduralRockMeshBuilder` agora calcula a normal local por diferenças centrais do sampler de terreno; a malha acompanha essa normal até uma inclinação máxima de 25°. O primeiro anel começa 7% da altura abaixo da ancoragem, a base ficou mais larga por perfil e uma tampa fecha a parte inferior. O topo plano virou uma crista baixa deslocada por seed, com iluminação facetada na mesma direção dos lados. O relevo e os dados de rocha não foram modificados.

## Evidência

Build Release: `dotnet build src/Game.Client.Desktop/Game.Client.Desktop.csproj -c Release --nologo` — 0 avisos e 0 erros. A captura ampla tem 621 árvores, 277 rochas e 0 billboards; o build visual foi 262,66 ms. O detalhe em zoom 8 tem 169 rochas visíveis e build de 546,97 ms.

- [Perspectiva ampla, zoom 3,4](evidence/n4-seed1-day172-slope-seated-rocks-iteration57.png)
- [Detalhe em encosta, zoom 8](evidence/n4-seed1-day172-slope-seated-rocks-detail-iteration57.png)

Benchmark Release de 60 s, perspectiva, 1280 × 720, câmera fixa e simulação pausada: **59,98 FPS**, P95 **17,21 ms**, P99 **17,59 ms**, máximo **47,80 ms**, 286,5 MiB de working set, 328,9 MiB privados, 29 draw calls e máximo de 696.787 primitivas por quadro. A geração estática da cena marcou 290,91 ms. O assembly tem SHA-256 `9DB0F568E248BE7B4336CCB3701FE2AC08589A302CDF4239A5A8F7EAAC408F2D`.

## Avaliação e limites

A inspeção aproximada não mostra mais terreno atravessando a base; em encostas suaves as pedras assentam melhor. No conjunto, porém, muitos volumes ainda são pequenos e arredondados/facetados e a rubrica de rochas permanece abaixo de 3/4. A inclinação é apenas um alinhamento gráfico limitado; não é simulação de estabilidade ou fraturamento geológico. O próximo passe deve melhorar variedade de tamanho, composição dos agrupamentos e integração visual das formações sem inventar propriedades ausentes do estado geológico.
