# SPEC-ZOO-130 — Geometria, padrões e renderização procedural

**Status:** Z4 em andamento. `ICreatureGeometryBuilderV1` v8 conserva superfícies por parte como fonte da pose e dos marcadores, e `BuildUnifiedRenderGeometry` extrai a união suave dos elipsoides e conectores implícitos alinhados a cada elo pai-filho. A v8 orienta faces pelo gradiente do campo interpolado em cada tetraedro, corrigindo uma inconsistência local de winding que a média das normais dos vértices não detectava. Para os dois planos corporais, três pares de genoma/pigmento e tesselações `6×8` e `8×12`, os testes validam componente conectado, duas faces com winding oposto por aresta, link de vértice como ciclo único e rejeição de faces degeneradas (12 combinações). A busca espacial não encontrou interseções entre triângulos sem vértices compartilhados; ainda não cobre pares adjacentes nem uma amostragem ampla de seeds/resoluções. Portanto, ela fornece evidência local, mas não certifica ausência geral de auto-interseções. A suíte focada passou 11/11 e a suíte completa Release 151/151. O adaptador MonoGame usa a malha unificada na galeria e em `--creature-geometry-view`, que desenvolve anatomia/pigmento do genoma persistido de cada agente N3, aplica uma postura estática às quatro patas por meio do solver Z2, cacheia a malha por genoma/estágio e a posiciona nas coordenadas atuais. A postura é visual e não grava no estado. A câmera integrada permite foco, zoom, azimute e elevação reproduzíveis; o quadrúpede genérico foi revisado em vistas frontal, lateral e de três quartos. Os marcadores de olhos e focinho cruzam a superfície da cabeça. `CreatureGeometryBuilderTests` também cobre determinismo, componentes conectados, incidência e orientação de arestas, ciclos de link dos vértices, degenerações, busca espacial de interseções e normais/índices válidos. O filtro de árvores verifica que a expressão de raízes varia por perfil biológico. `--zoology-gallery-fish` isola o protótipo de peixe e permite revisão orbital lateral, frontal e oblíqua. As correções visuais v7 afinam a nadadeira caudal no eixo longitudinal e tornam os marcadores de visão legíveis nesses ângulos; não alegam calibração por espécie. O peixe continua fora da população persistente N3. Faltam anatomias específicas de espécies/populações, animação orientada por movimento, comparação de invariância com câmera/FPS/culling/LOD e custo por nível de detalhe.

## Decisão

O Gênesis não ficará preso a 2D, 2.5D, pixel art nem 3D. O renderer é substituível. A topologia anatômica funcional, materiais e receitas visuais são dados neutros à API gráfica. A decisão de aparência é independente da física e da fisiologia.

## Pipeline gráfico

`DevelopedBody + PigmentProfile + VisualSeed -> VisualRecipe -> Geometry/Surface -> AnimationPose -> RendererAdapter`.

- Geometria parametrizada: segmentos, cápsulas, elipsoides, volumes implícitos/SDF e superfícies conectadas a um rig sem topologia inválida.
- Textura procedural: cor base, gradientes, ruído orientado, manchas e listras por reação-difusão, padrões e características de material (pelos, escamas, penas em níveis de detalhe).
- Rig: juntas semânticas conectadas à anatomia; âncoras de pé, olho, asa, cauda etc.
- Projeção: o mesmo modelo anatômico pode ser rasterizado em pixels ou convertido em malha 3D, sujeito às limitações de cada backend.
- Cache: genoma/versionamento/renderizador/LOD nas chaves; se o genoma muda só em pigmento, não recomputar o esqueleto.

## Integração de diagnóstico atual

- `--creature-geometry-view` desenha a posição atual dos agentes N3 sobre terreno e água usando buffers cacheados por genoma/estágio. Quando nenhum snapshot é fornecido, cria uma população de demonstração; ao carregar snapshot, usa os agentes existentes.
- O modo é somente de apresentação: não altera posição, metabolismo, população nem snapshot. Ele oculta os sprites de fauna e a flora para tornar a malha legível.
- Sem foco informado, a câmera seleciona um agente terrestre e começa em zoom próximo; foco, zoom, azimute e elevação podem ser definidos por linha de comando para repetir vistas. O campo ortográfico-base do modo de diagnóstico é 80 m. O alvo vertical acompanha a elevação local para evitar enquadrar o animal abaixo da câmera ao aproximar.
- As formas quadrúpedes variam por genoma individual e por um de 12 estágios visuais de desenvolvimento, com receitas Z1 ainda não calibradas. Herbívoros e predadores continuam usando o mesmo plano de espécie quadrúpede; sua escala adulta de referência é um proxy de apresentação. O solver Z2 reposiciona as superfícies dos segmentos das patas em uma postura estática de apoio. Os contatos são alvos visuais derivados da altura mínima da malha; não vêm de velocidade, passo, rota, colisão ou contato persistido. O corpo não anima nem representa locomoção.

## Referência concreta de engenharia

Revisão do snapshot `main` `a2207c4bd6f1be51d426684cf58f11668def7579` confirmou um framework funcional com quatro streams de seed, geração/cache por estágio, anatomias extensíveis e animação por rig/motor. O Gênesis já aplica os limites renderer-neutral e a geração determinística; o maior delta é a pose contínua integrada à geometria 3D. O rasterizador de pixels e o adaptador Godot não atendem à apresentação 3D adotada. Plantfolk é uma família de criaturas vegetais com raízes como membros locomotores, não um gerador biológico de árvores. A matriz detalhada e as decisões de adaptação estão na [auditoria comparativa](../research/zoology/PROCEDURAL_PIXEL_CREATURES_AUDIT_2026-10-22.md).

## Reaction-Diffusion — uso recomendado

Aplicar campos abstratos de pigmentos sobre a superfície UV ou sobre domínio espacial parametrizado. A espécie define intervalos e máscaras; a seed produz variações. Conservar padrões aderidos ao corpo durante animação e crescimento. Evitar simplesmente amostrar ruído no espaço de câmera, pois as manchas devem acompanhar o animal.

Referências: Turing (1952) https://doi.org/10.1007/BF02459572 ; Murray (1981) https://doi.org/10.1016/0022-5193(81)90109-0 .

## LOD e execução

- `SimLOD`: nível de detalhe comportamental/ecológico (sempre conservar estoques relevantes).
- `RenderLOD`: frequência/quantidade de detalhes visuais, sem alterar resultados ecológicos.
- Render em lote, cache de materiais e texturas, geração assíncrona, budget e cancelamento, sem acessar API de GPU fora do thread correto.
- Não recarregar modelos a cada frame. Gerar no spawn, alteração de estágio perceptível ou mudança permanente de fenótipo.
- O modo de integração Z4 é evidência de posicionamento na população e de aplicação visual de uma postura estática. O teste de igualdade do save cobre somente a geração de geometria/postura; não prova invariância a câmera, FPS, culling ou LOD no cliente, nem mede performance.

## Testes

- Alterar FPS, câmera, LOD visual ou ocultar criatura não altera estado científico.
- Mesmo genoma + versão + seed + renderizador produz saída equivalente nos ambientes testados.
- Não usar sprites artesanais obrigatórios e não carregar dependências remotas no runtime.
- Medir custo por criatura visível, memória do cache, churn de alocação, render de 10/100/1000 animais; **metas a estabelecer após baseline**, não resultados prévios.
