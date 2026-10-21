# Direção visual e técnicas de apresentação no MonoGame

**Status:** stack MonoGame confirmada; requisitos visuais aceitos; técnica de apresentação em avaliação. **Revisão:** 21/10/2026. Este documento orienta a validação visual dentro do MonoGame; a implementação completa do cliente continua condicionada ao gate visual.

## Referências visuais

As imagens abaixo foram fornecidas pelo usuário para orientar a identidade e a qualidade desejadas. São referências documentais; não são assets do runtime nem metas de reprodução literal.

![Referência de direção visual 1](references/visual-direction/reference-01.png)

![Referência de direção visual 2](references/visual-direction/reference-02.png)

Elas mostram uma paisagem natural estilizada em câmera elevada, com leitura ampla e sensação de volume: relevo de encostas, vales e paredões; água que acompanha a topografia e forma quedas; rochas volumétricas; árvores de tipos e alturas variados; vegetação em manchas orgânicas; luz direcional e sombras. A imagem orienta composição, materiais, densidade e leitura dos elementos, sem exigir fotorrealismo, reprodução de cores exata ou uma câmera específica.

## Diretriz visual aprovada

O objetivo é um mundo natural procedural, reconhecível, coerente e visualmente rico. A estilização pode simplificar superfícies, materiais e silhuetas, mas deve preservar a leitura de profundidade, volume, escala relativa, relevo e relações entre terreno, água, rocha e vegetação. Aparência low-poly, sprites, tiles, câmera top-down e projeção ortográfica são opções de técnica, não requisitos.

O resultado deve cumprir estas necessidades:

- Representar elevações, declives, encostas, vales, platôs e paredes rochosas com geometria ou uma técnica que demonstre resultado equivalente.
- Integrar rios, lagos e cachoeiras à topografia e ao estado hidrológico exposto pelos snapshots.
- Dar diversidade morfológica legível a árvores, arbustos e gramíneas e distribuir elementos por receitas determinísticas, usando observações ambientais disponíveis.
- Variar forma e composição de rochas procedurais e integrá-las à superfície.
- Produzir luz, sombras e oclusão coerentes com a geometria e com a direção de iluminação escolhida.
- Evitar juntas visíveis de chunks, padrões repetidos de tiles e manchas artificiais causadas pela representação.
- Compor uma região extensa que ainda permita reconhecer água, relevo e vegetação em resolução de jogo.

Conteúdo natural visível deve ser criado por algoritmos, parâmetros e seeds controlados pelo projeto. Bibliotecas de geometria, renderizadores, shaders, ruídos e ferramentas técnicas são permitidos após análise de licença e integração. Pacotes externos de modelos artísticos prontos não devem ser necessários para construir o mundo. As imagens de referência não são carregadas pelo runtime.

## Simulação e apresentação

A arquitetura modular e os contratos científicos existentes permanecem válidos. Cosmos, Atmosphere, Geosphere, Hydrosphere e Biosphere mantêm ownership, unidades, cadências, determinismo, conservação e limites já documentados. A camada visual lê snapshots e resultados publicados; não escolhe parâmetros físicos nem escreve nos estados dos módulos.

A dimensionalidade da simulação é uma decisão de cada processo. Grades de elevação, drenagem, clima, solo e ecologia podem continuar sendo campos 2D ou usar resoluções próprias, enquanto a apresentação constrói terreno e objetos em 3D. Representar uma cena em 3D não exige física volumétrica para todos os processos. Câmera, frame rate, nível de detalhe, cache e carregamento visual não podem alterar a evolução do mundo.

## Abordagens de representação

| Abordagem | Vantagens para este projeto | Custos e riscos | Tratamento nesta revisão |
|---|---|---|---|
| 2D por sprites/camadas | Baixo custo inicial; encaixa bem com campos em grade, inspeção e desenhos determinísticos | Profundidade, paredes, água em desníveis e espécies volumétricas dependem de composição, ordem e arte específica; maior adaptação para técnicas geométricas | Permanece elegível se provar a qualidade e a variedade exigidas |
| Híbrida/2.5D | Pode combinar malha de elevação com elementos 2D, projeções ou geometria limitada; transição incremental a partir do protótipo atual | Ordenação, oclusão, âncoras, sombras e mudanças de câmera podem exigir exceções por tipo de objeto; qualidade depende da composição | Candidata legítima para comparação, sem compromisso com as decisões atuais |
| 3D estilizado | Geometria, câmeras, luz, sombras, instanciamento e níveis de detalhe podem atender diretamente a vários elementos da referência; bibliotecas 3D procedurais podem ser usadas sem converter o core | Requer geração e gestão de malhas, materiais, água, distribuição e custo de GPU; 3D por si só não garante boa direção artística ou desempenho | Primeira hipótese a validar, não decisão definitiva |

Para a validação, comparar câmera ortográfica e perspectiva elevada. A escolha de projeção e ângulo deve vir da leitura do mundo e dos resultados observados.

## Stack confirmada: MonoGame

MonoGame é a stack obrigatória do cliente. O projeto permanecerá em C#/.NET e MonoGame para que o desenvolvimento e os recursos do jogo possam ser produzidos por IA dentro de uma base de código única, conforme a restrição definida pelo usuário. Não há avaliação, recomendação ou gate para trocar de motor.

MonoGame é um framework C#/.NET com APIs para gráficos 2D e 3D, efeitos/shaders e modelos. Como não inclui editor de cenas ou ferramenta de autoria pronta, o terreno, materiais, sombras, câmera, instanciamento, LOD e utilitários específicos serão implementados em código no próprio projeto. Esse fluxo code-first se alinha à produção assistida por IA pretendida; os custos serão controlados por protótipos pequenos, componentes reutilizáveis e ferramentas internas simples. Fontes oficiais: [visão geral e modelo de framework](https://docs.monogame.net/articles/tutorials/building_2d_games/01_what_is_monogame/index.html), [renderização 3D](https://docs.monogame.net/articles/getting_to_know/whatis/graphics/WhatIs_3DRendering.html), [plataformas](https://docs.monogame.net/articles/getting_started/platforms.html), [licença](https://github.com/MonoGame/MonoGame/blob/develop/LICENSE.txt).

### Técnicas a validar dentro do MonoGame

| Técnica | Aplicação no MonoGame | Pontos a medir |
|---|---|---|
| **3D estilizado por malhas** | Terreno elevado, rochas, água, árvores e vegetação como geometria procedural; câmera, luz, materiais e shaders no cliente MonoGame. | Qualidade de volume e silhueta, custo de geração de malha, draw calls, sombras, memória e desempenho. |
| **Híbrida: terreno 3D e elementos 2D** | Malha para relevo e água com billboards/sprites ou elementos planos para folhagem, detalhes e marcadores. | Legibilidade em diferentes ângulos, ordenação, oclusão, escala, custo de conteúdo e coerência das sombras. |
| **2D em camadas** | Sprites, polígonos e composição em camadas projetadas, mantendo a cena e as ferramentas no MonoGame. | Se profundidade, paredões, água em desníveis e diversidade de espécies atendem aos critérios visuais e permitem inspeção clara. |

Começar pela prova de 3D estilizado em MonoGame e avaliar a composição híbrida como alternativa de custo. A direção artística pode combinar malhas, shaders, primitivas, texturas e sprites gerados proceduralmente por código desenvolvido com IA, desde que os recursos sejam rastreáveis, reproduzíveis e utilizáveis sem depender de pacotes artísticos prontos. A escolha entre técnicas depende da cena demonstrativa, do custo de implementação, do desempenho e dos alvos de distribuição aprovados; a stack MonoGame permanece fixa.

## Validação visual comparativa

Antes de consolidar o cliente de N4, produzir uma cena demonstrativa procedural no MonoGame e comparar nela as técnicas de apresentação. Priorizar o cenário canônico L0 seed 1 e repetir as verificações de determinismo com seeds 2 e 3. Reutilizar snapshots existentes; se algum elemento de apresentação — por exemplo, uma cachoeira — não existir nos dados atuais, usar uma fixture de renderização identificada como tal, sem alegar que ela foi simulada nem alterar parâmetros físicos ou biológicos.

### Conteúdo da cena

- Uma área de vale com encostas, cristas, platô, parede rochosa e variação de relevo em escalas diferentes.
- Uma rede de água ligada à elevação, um lago nivelado em depressão e uma queda d’água demonstrativa amarrada a um desnível controlado.
- Rochas procedurais com volumes, contornos e materiais diferentes.
- Gramíneas, arbustos, árvores de copa ampla e coníferas geradas a partir de receitas; a distribuição usa campos ambientais existentes e permanece visual, sem criar organismos simulados.
- Luz direcional, sombras, oclusão e variação sazonal de aparência separadas do estado científico.
- Câmeras ortográfica e perspectiva elevadas, cada uma enquadrando a mesma área e permitindo inspeção e navegação simples.

### Critérios de aprovação

| Dimensão | Evidência e critério |
|---|---|
| Terreno | A malha acompanha a elevação recebida, exibe encostas e faces em ângulos de referência e não apresenta fendas nas bordas de chunks. Verificar geometrias com teste automatizado e captura da cena. |
| Água | A superfície de lago é nivelada por estágio; rios, margens e queda respeitam a geometria da fixture; não há água suspensa ou enterrada por erro de renderização. O teste de integração confere níveis e alturas usados na malha. |
| Vegetação | Pelo menos quatro silhuetas distinguíveis; escala e agrupamento são determinísticos; áreas de água profunda, inclinação e habitat seguem as regras fornecidas; nenhuma projeção substitui estado ausente. |
| Rochas | Geometria irregular volumétrica, sem aparência de sprite plano em ângulos validados; a rocha acompanha a superfície e suas faces se leem sob a luz. |
| Luz e profundidade | A direção das sombras coincide com a fonte de luz; superfícies e objetos mantêm ordem de oclusão; encostas, vales e copas continuam reconhecíveis na vista ampla. |
| Composição | A grade L0 inteira é visível a 1280 × 720 e os elementos principais continuam legíveis; sem padrão de tile ou borda de chunk dominante. Avaliar as duas projeções por rubrica visual 0–4 em relevo, água, vegetação, rocha, iluminação e composição; cada categoria precisa obter pelo menos 3. |
| Determinismo | Mesma seed, versão de receita e snapshot produzem a mesma distribuição, transformações e malhas de entrada em execução repetida. Capturas de pixels podem variar entre drivers e não são o único teste. |
| Desempenho | Medir a cena igual em Release, 1280 × 720 e máquina de referência de N4: média ≥30 FPS, P95 ≤33,3 ms, P99 ≤50 ms e bytes privados desktop estabilizados ≤1 GiB. Registrar aquecimento, driver, versão, seed, CPU/GPU, memória, custo de geração/reconstrução e tempos de quadro. |
| Escala | Repetir com a população visual do cenário e com 10× de instâncias decorativas procedurais; contabilizar draw calls, instâncias, triângulos, memória e tempo de geração. Cache, instanciamento, chunks e LOD só entram se os perfis mostrarem necessidade e sem alterar o snapshot. |
| Integração e plataforma | Core, testes científicos e runner headless continuam independentes do cliente gráfico; medir esforço de integração e registrar build/exportação MonoGame comprovada para cada alvo aprovado. Web é um gate separado, não inferido do suporte desktop. |
| Licença | Inventário das bibliotecas, ferramentas e recursos gerados com licença/SPDX, avisos de redistribuição e limitações por plataforma. |

Anexar capturas em enquadramento comum, configuração completa, resultados brutos, inventário de componentes e esforço de implementação. Se nenhuma técnica cumprir os gates, registrar o gargalo e ajustar a técnica ou o escopo visual dentro do MonoGame, com justificativa. Não reduzir o modelo científico para favorecer uma abordagem de renderização.

## Impacto no roadmap

N0–N3, seus contratos científicos, cenários, conservação, cadências e testes headless continuam iguais. A seleção visual não bloqueia a evolução de clima, hidrologia, botânica ou fauna. O gate de validação visual antecede o fechamento de N4 e é responsável por fechar câmera, representação e técnica dentro do MonoGame usando evidência. Benchmarks anteriores continuam úteis como linha de base do cliente atual, mas não comparam técnicas nem comprovam a direção visual final.

## Fontes e data de consulta

Fontes técnicas primárias do MonoGame foram consultadas em 21/10/2026. Recursos e termos podem mudar; confirmar versões, licenças e plataformas na execução do protótipo. Esta análise é qualitativa, não benchmark independente nem parecer jurídico.
