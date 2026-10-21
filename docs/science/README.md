# Fundamentação científica — documentação, não runtime

**Natureza deste diretório:** repositório de conhecimento, hipóteses, decisões científicas e critérios de validação. **Não** é um projeto C#, DLL, assembly ou namespace obrigatório. Não criar `src/Game.ScientificFoundation`, `Game.ScientificFoundation.csproj`, nem inserir `Game.ScientificFoundation` na `Game.sln`.

A fundamentação orienta os modelos dos cinco domínios (`Game.Modules.Cosmos`, `.Atmosphere`, `.Geosphere`, `.Hydrosphere`, `.Biosphere`). As quatro disciplinas abaixo organizam os fundamentos usados pelos sistemas do mundo:

```text
docs/
└── science/
    ├── README.md
    ├── Mathematics.md
    ├── Physics.md
    ├── Chemistry.md
    └── Biology.md
```

- [Mathematics](Mathematics.md): métodos numéricos, escalas, unidades, incerteza e determinismo.
- [Physics](Physics.md): fluxos de massa e energia, radiação, calor, mecânica e água.
- [Chemistry](Chemistry.md): nutrientes, matéria orgânica, transformações e conservação de espécies químicas modeladas.
- [Biology](Biology.md): fisiologia, metabolismo, crescimento, reprodução, competição, mortalidade e ecossistemas.

## Documentar a teoria não significa criar um módulo de simulação

1. **Documentação:** equações reconhecidas, hipóteses, simplificações, limites de validade, fonte, unidades e evidência.
2. **Contratos executáveis:** interfaces C# pequenas em `Game.Contracts`, exclusivamente quando necessárias à integração entre módulos.
3. **Implementação:** código responsável por calcular o processo dentro de seu domínio científico (`Game.Modules.*`).
4. **Biblioteca compartilhada opcional:** apenas matemática, numerics, PRNG ou algoritmos realmente transversais e demonstradamente reutilizados em `Game.Procedural` ou biblioteca técnica `Game.Common.*` criada mediante ADR. Não criar projetos `Game.Physics`, `Game.Biology` ou semelhantes automaticamente pela taxonomia da documentação.
5. **Estado:** pertence ao módulo de domínio proprietário; nenhuma documentação ou função compartilhada é um proprietário mutável.

Exemplo: o fundamento de geometria solar é listado em `docs/science/Physics.md`; sua equação deverá ser registrada na ficha do processo antes da implementação, mas sua implementação concreta, se requerida, pode ser interna a `Game.Modules.Cosmos/SolarRadiation`, ou reutilizar uma função matemática pura em `Game.Procedural` se fizer sentido. A resposta térmica do crescimento vegetal é um requisito de `docs/science/Biology.md`; a formulação permanece a definir na ficha, e sua implementação pertence a `Game.Modules.Biosphere/Botany`.

## Níveis de fidelidade

- **L0 — simplificado e testável:** relações causais e parâmetros de jogo declarados, com fluxos auditáveis.
- **L1 — baseado em processo:** lei/fórmula científica identificada, domínio de aplicação conhecido e resultados comparados com referência adequada.
- **L2 — calibrado/avaliado:** dados ou benchmarks, incerteza/erro e limites de aplicabilidade documentados.

Não qualificar um modelo como L2 sem evidência. Cada processo científico implementado deve ter ficha (não cada getter ou utilitário trivial) com ID, proprietário, equação, entradas, saídas, unidades, passo temporal, escala espacial, limites, fontes, hipóteses e testes. Consulte [Modelos e padrões de ficha](../17_TEMPLATES.md) e [Ownership](../04_OWNERSHIP.md).
