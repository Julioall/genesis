# ADR-0004 — direção visual natural e apresentação no MonoGame

- **Status:** direção visual aceita; stack MonoGame confirmada; técnica de apresentação em avaliação
- **Data:** 21/10/2026

## Contexto

Documentos anteriores tornaram obrigatórios um cliente 2D top-down, pixel art procedural e câmera fixa. A referência fornecida agora descreve outra prioridade: paisagem natural estilizada com profundidade, relevo, água integrada à topografia, rochas volumétricas, vegetação variada, iluminação e uma vista ampla. Ela não exige fotorrealismo nem determina que a implementação seja 3D. O usuário confirmou que o projeto deve permanecer na stack MonoGame para que o desenvolvimento e os recursos possam ser produzidos por IA dentro da base C#/.NET existente.

A separação entre simulação e renderização já é princípio arquitetural. Os domínios científicos, os contratos quantitativos L0, determinismo, conservação e escopo funcional não dependem da projeção visual.

## Decisão

1. Aceitar como requisitos de produto a direção visual descrita em [22_VISUAL_DIRECTION_AND_TECHNOLOGY.md](../22_VISUAL_DIRECTION_AND_TECHNOLOGY.md) e as duas imagens fornecidas e arquivadas em `docs/references/visual-direction/`.
2. Fixar MonoGame como stack obrigatória do cliente. Não avaliar nem recomendar migração para outro motor. Remover apenas as restrições anteriores de 2D, top-down, pixel art, sprites, tiles e câmera/projeção fixa; essas técnicas permanecem opções de apresentação dentro do MonoGame.
3. Manter os cinco domínios, ownership, contratos, seed, determinismo, conservações, testes sem janela e a independência entre estado simulado e estado renderizável. Cada processo usa a dimensionalidade e resolução necessárias à sua ciência.
4. Manter geração algorítmica de todo o conteúdo natural necessário. Bibliotecas técnicas são permitidas sob análise; pacotes externos de modelos artísticos prontos não são requisito do mundo.
5. Executar no MonoGame um protótipo comparativo de representação — 3D estilizado, composição híbrida e, se necessário, 2D em camadas — com a mesma cena e os mesmos dados antes de fechar o cliente de N4.
6. Não iniciar implementação gráfica completa nesta revisão. O cliente permanece MonoGame enquanto a câmera, a geometria, os materiais e a técnica visual são validados.

## Alternativas consideradas

- **Manter 2D/pixel art/top-down como contrato:** rejeitada porque transforma uma decisão antiga em limite do resultado visual antes de comparar técnicas.
- **Trocar MonoGame por outro motor:** fora do escopo e recusada segundo a restrição de stack definida pelo usuário.
- **Escolher imediatamente uma técnica visual de produção:** adiada até a cena demonstrativa verificar legibilidade, qualidade, custo e desempenho dentro do MonoGame.
- **Validar 3D estilizado e composição híbrida no MonoGame:** aceita como próximo experimento; permite medir volume e custo sem alterar a stack.

## Consequências

- O roadmap N0–N3 e os modelos quantitativos permanecem inalterados. N4 passa a depender de uma decisão fundamentada e da validação descrita no documento visual.
- O benchmark atual continua válido como evidência do cliente MonoGame nesta máquina, mas não comprova a direção visual final nem o desempenho das técnicas ainda não implementadas.
- As descrições de câmera e sprites em ADR-0002 são históricas; sua decisão de manter MonoGame permanece vigente e é reafirmada aqui. Modularidade, composição headless, snapshots e geração procedural continuam como princípios arquiteturais.
- Projeção, nível de geometria, formatos de dados de apresentação, instanciamento, chunks e LOD permanecem em aberto para validação dentro do MonoGame. Alvos de plataforma precisam de builds comprovados.

## Critério de revisão/encerramento

Encerrar a avaliação quando a cena demonstrativa atingir os critérios técnicos e visuais registrados e apresentar resultados repetíveis para as técnicas no MonoGame. Se nenhuma técnica passar, ajustar a abordagem visual com justificativa e registrar nova decisão dentro da stack confirmada; não alterar contratos científicos por causa do renderer.
