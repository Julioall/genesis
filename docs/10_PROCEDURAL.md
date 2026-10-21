# Mundo procedural, apresentação visual e desempenho

As metas numéricas de desempenho e o cenário de medição L0 constam em [20_L0_MODEL_AND_BENCHMARKS.md](20_L0_MODEL_AND_BENCHMARKS.md); este documento não cria limites alternativos.

## Regra de produto

O mundo físico, a simulação e os recursos visuais de runtime devem ser reproduzíveis por seed. Árvores, rochas, gramíneas, superfícies e detalhes são desenhados por receitas em código e cacheados em memória. O cliente não depende de pacotes externos de imagens, modelos ou texturas artísticas.

## Etapas independentes

1. **Genesis procedural do mundo físico** por seed: elevação → drenagem → substrato → parâmetros de solo → habitats iniciais e reservatórios.
2. **Estado vivo** por tick: água, temperatura, biomassa, detritos, população, mortalidade; nunca depender do desenho.
3. **Receitas de aparência**: espécies funcionais → geração procedural de tronco/copa/folhas, grama, solo, pedras, silhuetas animais e animações.
4. **Render L0**: gerar e cachear receitas, compor cena por chunks visuais, animações simples e UI/inspeção; LOD é evolução posterior.

O termo 'genesis' da etapa 1 descreve o processo de origem do mundo e **não autoriza** uso de `Genesis` como nome de projeto, pasta ou namespace.

## Princípios visuais

- A câmera, projeção e dimensionalidade da representação serão escolhidas após a validação visual comparativa. A direção deve comunicar profundidade, volume, relevo, água, rocha e vegetação; requisitos completos estão em [22_VISUAL_DIRECTION_AND_TECHNOLOGY.md](22_VISUAL_DIRECTION_AND_TECHNOLOGY.md).
- O cliente ancora todo objeto às coordenadas e à elevação física publicadas. Malhas, sprites, billboards ou técnicas híbridas são escolhas do renderer e não decidem geografia nem estado biológico.
- Não há obrigação de pixel art, anti-aliasing específico, ortografia, perspectiva ou câmera fixa. A qualidade será avaliada por leitura e integração natural, sem limitar a técnica de antemão.
- Mundo finito gerado antes do avanço; chunks visuais podem gerar recursos sob demanda para mostrar uma área maior que a tela. `seed` global estável, seeds locais derivadas por chunk, entidade, receita e versão.
- Arbustos/árvores: parâmetros botânicos afetam morfologia procedural mas não implicam uma folha como entidade persistente.
- Fora da tela, render desligado e simulação ativa. LOD visual detalhado é evolução posterior; não há LOD de simulação no MVP.
- Não regenerar geometria completa de todas as árvores por quadro de vento. Cache inicial por receita, versão, grupo e estágio; adicionar estação/LOD e animação hierárquica apenas quando usados e medidos.
- Recursos e APIs do renderer, shaders e eventos de input não entram em `Game.Modules.*`; sua tecnologia fica isolada no cliente/adaptador.

## Procedural e biologia não devem divergir

- O estado `alive/dead`, idade, biomassa, copa, estação visual e reserva é comunicado por snapshot; render não decide essas propriedades.
- Sombra visual não deve criar efeito científico sem cálculo/contrato correspondente.
- Animais só têm corpo visível após spawn biológico confirmado; desaparecimentos visuais não significam mortes.
- Câmera ou resolução nunca afetam RNG ecológico, chuva, morte ou reprodução.

## Performance

Instrumentar CPU por módulo/fase, allocations por tick, cache hit rate, VRAM/RAM, tempo de geração de chunk, P50/P95 FPS e custo de persistência. Os alvos são metas a medir, não desempenho observado. Manter uma implementação de referência simples e só investir em ECS/instancing completo após profiling.

## Tipografia/áudio

Se for necessário usar uma fonte incorporada, registrar uma exceção explícita à política procedural-only antes de decidir; idealmente, criar glifos simples por código para inspeção inicial. Áudio pode ficar ausente no MVP; não bloquear a prova ecológica por síntese musical.

## Corte visual inicial

Usar silhuetas procedurais cacheadas por grupo/estágio, malha de terreno em buffers persistentes e UI de inspeção simples. A malha de água segue as faixas D8 e as áreas alagadas do snapshot. Chunks e LOD são otimizações posteriores. A câmera recorta renderização; todo o mapa finito continua simulado. Não exigir geração infinita nem suspensão biológica fora da tela.
