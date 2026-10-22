# ADR-0005 — composição explícita dos módulos nos executáveis

- **Status:** aceito; substitui a decisão de composição do ADR-0002
- **Data:** 22/10/2026

## Contexto

O runtime precisa executar uma cadeia integrada de Cosmos, Atmosphere, Geosphere, Hydrosphere e Biosphere, mas `Game.Core` não deve conhecer as implementações concretas dos domínios. O ADR-0002 colocava a composição em Core, em conflito com `docs/01_ARCHITECTURE.md`, que atribui essa responsabilidade aos executáveis.

O roadmap também organiza o trabalho por gates: N1 integra a física de Cosmos a Hydrosphere; N2 aprofunda Botany/Ecology. Isso pede integração causal entre módulos, não implementação simultânea de todos os domínios.

## Decisão

- `Game.Core` referencia apenas `Game.Contracts` e coordena processos por capabilities explícitas e versionadas.
- Core é owner do relógio lógico, ticks, agenda e projeção do calendário L0 a partir do tick; Cosmos calcula geometria/forçamento solar para o dia e hora recebidos.
- Os executáveis (`Game.App` e `Game.Client.Desktop`) instanciam as implementações concretas e as injetam em Core.
- Assemblies de domínio não referenciam outros assemblies `Game.Modules.*`; dependências procedurais compartilhadas continuam permitidas conforme `docs/01_ARCHITECTURE.md`.
- Manter composição estática e explícita. Não introduzir carregamento dinâmico, reflexão ou framework de plugins no MVP.
- Avançar pelo roadmap: um módulo por vez, salvo o menor conjunto fortemente acoplado exigido pelo gate. Cada incremento registra evidência antes de ampliar o escopo.
- Esta decisão não declara armazenamento particionado por owner: `WorldCell` ainda agrega dados de vários domínios. As APIs públicas dos módulos não recebem esse agregado; Core converte o estado persistido em DTOs de domínio e aplica os resultados aceitos pelos owners. Uma migração do armazenamento e da persistência para estado por owner permanece fora desta decisão.

## Consequências

- Testes podem substituir providers e exercitar a orquestração de Core sem alterar implementações concretas.
- Executáveis têm referências aos módulos necessários para composição; Core não ganha dependências de domínio.
- A integração dos cinco domínios continua possível, enquanto a ordem de implementação permanece determinada pelos gates N1, N2 e seguintes.
- Contratos de capability precisam declarar versão, unidades, janela temporal e semântica dos dados trocados.

## Evidência e revisão

O grafo atual de projetos confirma que Core referencia apenas Contracts, e o teste de capacidades substituíveis verifica o consumo dos providers. Os gates quantitativos de física e de ecologia continuam independentes; esta ADR trata do limite arquitetural, não os aprova.

Revisar esta decisão se o runtime precisar de módulos carregados após o início do processo, se uma dependência entre domínios se tornar inevitável ou se o modelo de ownership deixar de caber em capabilities explícitas.
