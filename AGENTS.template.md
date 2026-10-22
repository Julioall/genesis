# AGENTS.md — Contrato de desenvolvimento assistido por IA

O objetivo é construir um mundo natural procedural e vivo. O repositório principal é `Julioall/genesis`, mas nomes de projetos/assemblies/namespaces/pastas de código devem ser neutros, no formato **Game.***. Não incluir nomes comerciais ou títulos de jogo nas APIs.

- Ler `docs/00_SCOPE.md` e os documentos de módulo antes de alterar código.
- Fluxos naturais e ecosistemas integrados; dependências de código acíclicas.
- Um owner de estado por propriedade. Nada de acesso direto ao estado interno de outros módulos.
- Contracts: tipo, unidade, intervalo temporal, resolução, versão e condição de ausência.
- `docs/science/` contém Mathematics, Physics, Chemistry e Biology como **fundamentação documental**, não código. Proibido criar `Game.ScientificFoundation` como assembly/namespace apenas por essa taxonomia. Algoritmos executáveis pertencem ao domínio responsável; helpers reutilizados são opcionais e testados.
- Ticks e seeds determinísticos; não usar tempo de parede, camera, FPS ou recursos renderizáveis para decisões do mundo.
- Balanço explícito de água, matéria seca e nitrogênio; orçamento energético da fauna e entrada/saída de fronteira explícitos, sem exigir fechamento energético global.
- Sem sociedade, agricultura humana, construções, política, economia, IA neural ou tectônica avançada no MVP.
- Testes com fixtures fixas, cenários contrastantes e validação de save/load antes de declarar marco aprovado.
- Novo algoritmo científico: anotar origem, equação, unidades, limites, simplificações, interpretação e teste. Para o cenário L0, `docs/20_L0_MODEL_AND_BENCHMARKS.md` é a fonte única dos valores numéricos, tolerâncias e metas; atualizar esse documento antes de codificar alteração de parâmetro.
- Arte 100% procedural no runtime: sem sprites artísticos externos obrigatórios; caches internos permitidos.
- MonoGame é a stack obrigatória do cliente. A técnica de apresentação permanece em validação dentro do MonoGame, conforme `docs/22_VISUAL_DIRECTION_AND_TECHNOLOGY.md`. Isolar o cliente gráfico; núcleo, módulos e testes científicos não podem depender de MonoGame. Cada plataforma pretendida exige demonstração própria.
- Não apresentar documentação como implementação; não relatar benchmark sem execução real.
- Alterações arquiteturais importantes exigem ADR em `docs/decisions/`; manter GDD, specs, README, testes e docs consistentes.

## Modo de trabalho e padrões

- Ler o prompt mestre não é autorização para executá-lo. Durante validação documental, alterar somente documentação; não criar solution, projetos ou código.
- Aplicar [regras de codificação](docs/19_CODING_RULES.md) e limites de [escopo](docs/00_SCOPE.md).
- Frameworks/contratos de sistemas adiados não são pré-requisito de N0. Composição explícita, processamento sequencial e funções puras são o padrão inicial.
- Energia fisiológica da fauna é obrigatória; fechamento termodinâmico global é opcional. Nitrogênio acompanha animais, sementes, detritos e matéria orgânica do solo.
