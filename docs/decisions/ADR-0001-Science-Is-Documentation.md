# ADR-0001 — ScientificFoundation é documentação, não assembly

- **Estado:** aceita para a especificação Systema Naturae 2.1
- **Data:** 20/10/2026

## Contexto

O desenho anterior propunha `src/Game.ScientificFoundation/Mathematics|Physics|Chemistry|Biology` como biblioteca de código. Essas disciplinas são áreas de conhecimento que atravessam os cinco domínios e, como hierarquia de projetos, podem criar abstrações artificiais, duplicação de regras ou uma dependência genérica difícil de evoluir.

## Decisão

A fundamentação passa a residir **exclusivamente em `docs/science/` como taxonomia documental**. Nenhum `Game.ScientificFoundation.csproj`, assembly ou namespace é requisito arquitetural. As teorias e equações são documentadas antes da implementação; o código científico executável pertence ao módulo dono do processo. Helpers numéricos genéricos compartilhados podem ficar em `Game.Procedural` ou em um projeto técnico pequeno criado sob demanda, mediante ADR.

## Consequências

- Mantemos separação entre documentação, contratos, código e estado.
- As quatro disciplinas continuam sendo parte da referência conceitual do projeto.
- Nenhuma classe precisa importar `Game.ScientificFoundation`.
- A modularidade decorre de **quem possui os estados e executa os processos**, não da divisão curricular das ciências.
- Os modelos científicos continuam obrigados a ter testes, fontes, unidades e precisão explícita.

## Alternativas descartadas nesta revisão

- Biblioteca científica única de código: alto risco de virar conjunto heterogêneo de fórmulas e criar acoplamento transversal.
- Um assembly por disciplina: separação artificial diante dos limites reais de propriedade do estado.
- Ausência de documentação científica: inviável para auditoria e evolução progressiva da simulação.
