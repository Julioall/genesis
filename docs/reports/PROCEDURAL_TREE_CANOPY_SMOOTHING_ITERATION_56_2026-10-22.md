# Suavização das copas 3D no LOD distante — iteração 56

## Objetivo

Reduzir as grandes faces triangulares dos cachos de copa distantes sem trocar a geometria procedural 3D por sprites ou billboards.

## Alteração

O LOD distante de folhas largas agora usa a subdivisão icosférica já empregada pela geometria próxima: 80 faces por lobo em vez de 20. A irregularidade radial continua determinística e foi mantida ampla para evitar esferas perfeitas. A cor e a luz são interpoladas pelos vértices. As proporções horizontais/verticais também variam com a forma do perfil: carvalho/salgueiro mais largos, bordo mais arredondado e perfis colunares mais estreitos e altos. Biomassa, distribuição e estado da simulação não mudam.

## Evidência visual e custo

Build Release: `dotnet build src/Game.Client.Desktop/Game.Client.Desktop.csproj -c Release --nologo` — 0 avisos e 0 erros. As duas capturas mantêm 621 árvores e zero billboards:

| Projeção | Captura | Árvore / flora total | Build da cena |
|---|---|---:|---:|
| Perspectiva, zoom 3,4 | [PNG](evidence/n4-seed1-day172-smooth-canopy-iteration56-perspective.png) | 1.292.370 / 1.468.287 vértices | 260,12 ms |
| Ortográfica, zoom 2,8 | [PNG](evidence/n4-seed1-day172-smooth-canopy-iteration56-orthographic.png) | 1.351.956 / 1.527.873 vértices | 264,37 ms |

Benchmark Release de 60 s, perspectiva, 1280 × 720, câmera fixa e simulação pausada: **60,00 FPS**, P95 **17,25 ms**, P99 **17,78 ms**, máximo **42,35 ms**, 287,7 MiB de working set, 332,7 MiB privados, 29 draw calls e máximo de 693.968 primitivas por quadro. A geração estática da cena marcou 285,70 ms. Este ensaio passa os limites locais de tempo de quadro, mas não substitui hardware mínimo nem o benchmark ativo desde o dia zero.

## Avaliação e próximo passo

As faces grandes diminuíram e as silhuetas ficaram menos angulares, mas a ilha ainda lê como grupos de cachos arredondados. A inspeção não sustenta nota 3/4 para vegetação; N4 permanece aberto. A próxima melhoria deve aproximar as massas dos galhos terminais e reduzir a leitura de lobos isolados, com nova inspeção em vistas próximas e laterais.
