# Diagnóstico do relevo físico — N4 / Geosphere

**Data:** 22/10/2026\
**Estado:** causa identificada; correção física ainda não aprovada. A Geosphere segue validada para N1, mas a apresentação do relevo continua abaixo do gate visual N4.

## Método e evidência

Compilei a Geosphere Release a partir do código-fonte atual e medi as elevações em uma grade de 64 × 64 células de 10 m para as seeds 1–3. As diferenças comuns excluem células cuja elevação diverge mais de 2 m do campo-base, isolando as bordas dos afloramentos. O declive foi calculado no campo físico publicado.

| Seed | Elevação (m) | Diferença comum P95 / máx. em 10 m | Borda de afloramento P95 / máx. | Declive P95 / máx. |
|---:|---:|---:|---:|---:|
| 1 | 18,2–231,8 | 16,4 / 40,0 m | 83,3 / 84,6 m | 2,14 / 5,97 m/m |
| 2 | 20,4–246,5 | 18,4 / 41,2 m | 87,5 / 111,5 m | 2,51 / 7,76 m/m |
| 3 | 17,7–235,6 | 16,8 / 38,8 m | 87,1 / 109,5 m | 2,31 / 7,82 m/m |

São diferenças entre centros de células vizinhas, não declives universais de aceitação. O código cobre as cordilheiras com amplitudes de 170–190 m e 132–150 m e larguras gaussianas de aproximadamente 40–56 m; a crista central recebe 24% da amplitude total numa largura de 24% do ombro. Essa combinação concentra grande mudança vertical numa distância curta. A grade de simulação permanece em 10 m, ainda que o renderer subdivida a malha visual.

Os sete afloramentos adicionam uma segunda causa: dentro do contorno, `Elevation` substitui a superfície por uma cota horizontal; fora dele, volta imediatamente ao campo-base. O topo é elevado para ficar pelo menos 18 m acima do ponto mais alto do contorno, mas não há limite relativo ao ponto mais baixo. Por exemplo, no afloramento principal da seed 1, o terreno-base da borda varia de 38,7 a 104,8 m enquanto o topo fica em 122,8 m: a parede varia de 18,0 a 84,1 m ao redor do mesmo platô. A altura não é ruído da seed; decorre do posicionamento do afloramento sobre uma encosta inclinada.

Os testes existentes provam determinismo e um desnível mínimo; não limitam o declive comum nem a variação da parede. Por isso, 150/150 testes passam mesmo com as diferenças medidas. A avaliação N4 também já marcou o terreno como **2/4**, observando superfície blocada e paredes dominantes.

## Correções tentadas e decisão

| Experimento | Efeito medido | Decisão |
|---|---|---|
| Aumentar moderadamente apenas as larguras das cordilheiras | P95 comum caiu para 14,7–16,1 m; o máximo ficou entre 32,8 e 38,7 m. N3 anual da seed 1: 20,454 s, acima do limite de 18,25 s. | Revertido. |
| Redistribuir amplitude da crista para o ombro, mantendo a largura | P95 comum ficou em 16,2–18,3 m e o máximo em 37,8–38,5 m. N3 anual da seed 1: 24,891 s. | Revertido. |
| Fixar um máximo de 32 m acima do ponto mais alto da borda | Falhou no teste: o lado baixo ainda ficava 84,1 m abaixo do topo, porque a própria borda varia dezenas de metros. | Revertido; um limite de cota sem tratar a encosta não limita a parede completa. |

Uma tentativa anterior que alterava largura de cristas, espurões, eixo e taper em conjunto reduziu mais os saltos, mas elevou o benchmark anual a 22,8–23,5 s. Como as causas não foram isoladas e o gate de N3 falhou, essa variante também foi revertida. O código atual mantém a configuração de relevo anterior a esses experimentos; a suavização visual da transição costeira não altera o campo físico.

## Pesquisa e limite do que ela resolve

Uma busca SciSpace encontrou revisões sobre dependência de declive em relação à resolução do DEM, métodos de simplificação que tentam preservar estrutura e morfologia suave, modelagem que preserva explicitamente linhas de descontinuidade e um método de erosão procedural com mapa local de restrições de altura. Esses trabalhos apoiam tratar a escala e as feições de parede separadamente; nenhum dos resumos consultados fornece um limite universal de declive para este mundo. Referências: [Yu, Zhang e Ai (2021)](https://doi.org/10.1080/13658816.2020.1772479), [Lim, Wei e Bhojan (2022)](https://doi.org/10.5220/0010799700003124), [Chen, Wang e Hao (2025)](https://doi.org/10.1109/tgrs.2025.3604736).

## Bloqueio e próximo passo

Não há evidência suficiente para escolher um teto de declive sem uma meta de relevo calibrada para a escala de 10 m. A tentativa de ampliar as cordilheiras não preservou o throughput anual; limitar apenas os platôs não corrige a variação da encosta sob eles. O próximo ensaio precisa separar as superfícies: avaliar largura/altura das cordilheiras contra o benchmark N3 e selecionar ou reposicionar afloramentos somente onde a borda tenha variação compatível com uma parede visualmente legível. Depois, deve verificar fotografia ortográfica/perspectiva e repetir as três seeds anuais.

O artefato continua sendo um bloqueio **visual de N4**, não uma falha dos balanços N1. Ele fica registrado para retorno após o próximo gate independente.
