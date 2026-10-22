# Avaliação de plataforma e gate web no MonoGame

**Status:** MonoGame é a stack obrigatória; desktop é o primeiro alvo de referência e suporte a navegador continua em avaliação.

## Princípios

- O core C# e os módulos científicos continuam headless e independentes de MonoGame, câmera, GPU e UI.
- O cliente usa MonoGame. As opções de apresentação e os critérios técnicos estão em [direção visual e técnicas de apresentação](22_VISUAL_DIRECTION_AND_TECHNOLOGY.md).
- A lista de plataformas documentadas não substitui evidência: é necessário compilar, executar, medir e registrar cada alvo que o projeto pretende prometer.
- Suporte web é um gate separado da escolha visual. A existência de uma rota de plataforma não implica que o core compartilhado, threading, memória, shaders ou desempenho sejam adequados no browser.

## Avaliação de plataforma

1. Usar o mesmo snapshot, conteúdo procedural, enquadramento e carga da cena demonstrativa ao comparar técnicas de apresentação no MonoGame; manter a suite científica e o runner headless independentes.
2. Verificar build de desenvolvimento e distribuição, input, persistência, janelas/telas, logs, profiling e operação em Release nos sistemas pretendidos.
3. Para web, provar compilação do core, ciclo de ticks, representação mínima, input, save/load, memória, threading/WASM, shaders, tamanho do bundle e profiling em navegadores reais. Registrar browser, versão, sistema e hardware.
4. Medir diferenças de plataforma sem mudar equações ou regras para contornar limitações de MonoGame. A apresentação pode variar se mantiver os mesmos dados e comportamento simulado.
5. Revalidar documentação, licenças e dependências no momento de implementar o spike. Versões e suporte de plataformas mudam.

## Condições conhecidas para o próximo spike

- A plataforma web do MonoGame exige verificação própria; a existência de APIs gráficas 2D/3D não comprova exportação funcional do jogo para browser.
- O spike deve validar o ciclo headless, apresentação MonoGame, input, save/load, memória, threading/WASM, shaders, tamanho do bundle e profiling em navegadores reais. Registrar navegador, versão, sistema e hardware.

Fontes primárias consultadas em 21/10/2026: [plataformas MonoGame](https://docs.monogame.net/articles/getting_started/platforms.html) e [proposta da plataforma web MonoGame #9515](https://github.com/MonoGame/MonoGame/issues/9515). Estes registros descrevem limites documentados, não a compatibilidade do produto.

## Decisão de plataforma

Manter desktop como primeiro alvo de referência e testar a web cedo para evitar dependências irreversíveis. Até que o spike produza evidência, classificar suporte web como inconclusivo e não anunciar portabilidade. Ausência de suporte web não invalida os testes headless ou a evolução desktop em MonoGame; a prioridade de plataformas é independente da escolha de stack.
