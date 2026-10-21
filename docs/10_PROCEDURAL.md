# Procedural-only, top-down e desempenho

As metas numéricas de desempenho e o cenário de medição L0 constam em [20_L0_MODEL_AND_BENCHMARKS.md](20_L0_MODEL_AND_BENCHMARKS.md); este documento não cria limites alternativos.

## Regra de produto

Todos os conteúdos visuais/sonoros necessários para executar o mundo serão gerados **por receitas, parâmetros e algoritmos** mantidos pelo projeto. Isso não significa proibir MonoGame/.NET, dependências técnicas ou cache de texturas geradas em RAM/disco. Não exigir packs de PNG, spritesheets, fontes desenhadas à mão ou WAV externos para iniciar o mundo.

## Etapas independentes

1. **Genesis procedural do mundo físico** por seed: elevação → drenagem → substrato → parâmetros de solo → habitats iniciais e reservatórios.
2. **Estado vivo** por tick: água, temperatura, biomassa, detritos, população, mortalidade; nunca depender do desenho.
3. **Receitas de aparência**: espécies funcionais → geração de tronco/copa/folhas, grama, solo, pedras, silhuetas animais e animações.
4. **Render L0**: gerar/carregar silhuetas em cache, compor cena por chunks visuais, animações simples e UI/inspeção; atlas e LOD são otimizações posteriores.

O termo 'genesis' da etapa 1 descreve o processo de origem do mundo e **não autoriza** uso de `Genesis` como nome de projeto, pasta ou namespace.

## Princípios visuais

- Top-down 2D, com ilusão visual 2.5D apenas nas alturas/sombras. Não inclinar recursos arbitrariamente; manter âncoras/posições coerentes com perspectiva de topo.
- Pixel art legível; anti-alias/desfoque não devem adulterar paleta e pixels lógicos.
- Mundo finito gerado antes do avanço; chunks visuais podem gerar recursos sob demanda para mostrar uma área maior que a tela. `seed` global estável, seeds locais derivadas por chunk, entidade, receita e versão.
- Arbustos/árvores: parâmetros botânicos afetam morfologia procedural mas não implicam uma folha como entidade persistente.
- Fora da tela, render desligado e simulação ativa. LOD visual detalhado é evolução posterior; não há LOD de simulação no MVP.
- Não regenerar geometria completa de todas as árvores por quadro de vento. Cache inicial por receita, versão, grupo e estágio; adicionar estação/LOD e animação hierárquica apenas quando usados e medidos.
- Recursos do renderer, `Texture2D`, `SpriteBatch`, shaders e eventos de input não entram em `Game.Modules.*`.

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

Usar silhuetas procedurais legíveis e cacheadas por grupo/estágio, terreno em chunks visuais e UI de inspeção simples. Galhos detalhados, animação hierárquica, múltiplos LODs, pools e atlas avançados só após necessidade medida. A câmera recorta renderização; todo o mapa finito continua simulado. Não exigir geração infinita nem suspensão biológica fora da tela.
