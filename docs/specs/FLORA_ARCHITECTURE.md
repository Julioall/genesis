# Arquitetura procedural da flora 3D — protótipo atual

> Os registros de relevo v16, hidrologia v17 e flora 3D descrevem protótipos do diretório de trabalho local em 21/10/2026. O código dessas alterações não integra esta publicação documental; os resultados históricos não foram reexecutados nesta revisão.

**Status:** primeira etapa estrutural implementada no cliente desktop MonoGame. Este documento descreve o protótipo de árvores tridimensionais existente; a técnica visual final continua em validação dentro da stack confirmada. A decisão e os critérios estão em [ADR-0004](../decisions/ADR-0004-visual-direction-and-renderer-evaluation.md). As árvores têm um grafo botânico determinístico separado da malha MonoGame. Os perfis atuais representam carvalho, pinheiro, bétula, bordo e salgueiro em um vale temperado.

## Divisão de responsabilidades

- `Game.Modules.Biosphere.ProceduralTreeGenerator` recebe perfil, altura, maturidade visual, detalhe e seed; devolve segmentos conectados de tronco/galhos e locais de inserção foliar. Perfis de folhas largas recebem brotos adicionais por uma passagem limitada de Space Colonization dentro do envelope da copa.
- `TreeSpeciesProfile` guarda os parâmetros arquitetônicos e foliares. A geração não consulta câmera, GPU, relevo gráfico ou `MonoGame`.
- `ProceduralFloraMeshBuilder` converte o grafo para tubos afunilados e folhas triangulares no cliente. Estação, paleta, iluminação aproximada e formato de malha pertencem à apresentação.
- A colocação continua ancorada no sampler contínuo de terreno; uma esfera de limite remove árvores fora do frustum antes de gerar sua malha.
- A malha usa três níveis de ramificação conforme a altura projetada em pixels. A hierarquia permanece no modelo, enquanto árvores pequenas recebem menos ordens e folhagem simplificada.

As seeds visuais derivam da seed do mundo e da célula, sem consumir o fluxo aleatório da ecologia. A geometria é reconstruída quando o cache horário, a estação visual ou a resolução mudam.

## Arquiteturas presentes

| Perfil | Organização atual |
|---|---|
| Carvalho | Eixo principal, galhos largos e copa espalhada |
| Pinheiro | Eixo dominante, ramos em espiral e forma cônica |
| Bétula | Tronco fino, copa vertical e ramos ascendentes |
| Bordo | Galhos distribuídos por níveis e copa arredondada |
| Salgueiro | Copa ampla com inclinação pendente nos ramos distais |

As ramificações usam níveis de ordem crescente, afunilamento e pequenas curvaturas determinísticas. Os pontos foliares ficam nos ramos secundários e terminais. A estrutura é contínua e tridimensional, mas a junção entre tubos ainda pode ser melhorada.

## Limites declarados

Esta etapa implementa perfis paramétricos, um grafo hierárquico e uma passagem de Space Colonization com orçamento fixo de brotos por indivíduo. Os atratores definem um envelope geométrico da copa; ainda não representam competição observada por luz, nem crescimento temporal. Ainda não implementa L-Systems, plasticidade do crescimento, idade persistida no estado ecológico, raízes simuladas, vento, sombras projetadas de copa, cache de módulos ou impostores distantes. O LOD atual só reduz ramos e folhagem em objetos menores na tela. A malha de folhas é uma aproximação estilizada; ela não equivale à fisiologia ou anatomia validada de espécies reais.

A captura v26 está fora do repositório no diretório de visualizações da tarefa. A distribuição de folhagem está mais preenchida em torno dos ramos e as copas se afastam de volumes esféricos. A composição do vale, a rede de água, as sombras e a densidade geral da floresta ainda estão distantes da referência enviada. A cena não deve ser considerada como objetivo visual atingido.

Na medição Release de 30 s (seed 1, 1280 × 720, 6 ticks/s), o cliente registrou média de 55,77 FPS, P95 de 17,51 ms, P99 de 100,23 ms e working set de 368 MiB, com 1.710 árvores visíveis e 1.712.208 vértices de flora. A média voltou a ficar próxima de 56 FPS após reduzir a expansão foliar; o P99 segue mostrando picos que precisam de investigação.

## Próximos gates

1. Validar os perfis de copa com árvores isoladas e grupos, ajustando os envelopes e ângulos dos atratores por arquitetura.
2. Ligar maturidade e crescimento gradual aos estados biológicos, em vez de inferir a idade apenas do bucket de biomassa.
3. Suavizar bifurcações e variar a seção dos ramos; validar silhuetas isoladas por espécie e grupos no terreno inclinado.
4. Medir o LOD por resolução e distância, adicionar cache e geometria compartilhada e manter o custo estável ao aumentar a densidade.
5. Avaliar iluminação, auto-sombreamento e vento após a estrutura/LOD estarem estáveis.

Os gates seguem a ordem e os critérios da pesquisa de arquitetura botânica enviada pelo usuário. Os modelos paramétricos e o grafo não são tratados como substitutos de dados ou validação botânica.
