# SPEC-ZOO-110 — Anatomia, cinemática e locomoção

**Status:** pose/IK, rota cinemática, desvio terrestre em grade, planejamento de rota de água doce por profundidade e integração terrestre básica com posição N3 persistente estão implementados. `ICreatureMovementSolverV1` continua puro e renderer-neutral. Zoology recebe do Core cópias somente de leitura da elevação Geosphere e da água superficial Hydrosphere; tenta primeiro a rota terrestre direta e, quando ela para por declive ou vau, planeja um desvio determinístico antes de aplicar ao agente apenas a posição aceita pelo solver. O planner de água doce usa a mesma grade ambiental, sem inventar correnteza ou profundidade.

## Implementação inicial disponível

- `SolveQuadrupedLimb` calcula joelho e pé usando os comprimentos dos segmentos do corpo Z1 e um alvo de contato no espaço local. Preserva os dois comprimentos e sinaliza quando o alvo solicitado precisou ser limitado ao alcance.
- `BuildFishSwimmingPose` gera ângulos para juntas flexíveis do eixo corporal/nadadeira caudal dentro dos limites anatômicos e a partir de fase/amplitude normalizadas.
- `ResolveMovement` percorre uma rota ambiental amostrada e limitada por distância, declive, profundidade de vau ou profundidade mínima de natação. O N3 fornece amostras a cada 5 m usando interpolação bilinear de campos recebidos do Core.
- `CreatureTerrainRoutePlannerV1` usa A* em uma grade 8-vizinhos para contornar células cuja transição excede o limite de declive ou vau já recebido. A rota alternativa é densificada dentro do espaçamento contratado e validada novamente por `ResolveMovement`; se não houver caminho, permanece a posição alcançada pela rota direta.
- `PlanFreshwaterRoute` usa A* determinístico para contornar células que não atingem a profundidade mínima fornecida pelo chamador. Passagens diagonais não cortam cantos rasos; amostras intermediárias respeitam o espaçamento máximo e podem ser validadas pelo mesmo solver. Limites de profundidade continuam sendo parâmetros explícitos por perfil, não valores universais.
- No solver, o declive do leito restringe apenas deslocamento terrestre. Para peixe, cada amostra precisa conter água e atingir a profundidade mínima; a posição vertical permanece no meio da coluna disponível. Correnteza e colisão com leito/obstáculos ainda não existem nos campos públicos.
- O movimento terrestre N3 usa os valores provisórios `grade máxima = 0,75` e `vau máximo = 0,25 m`; são aproximações de engenharia explícitas, não parâmetros biológicos calibrados. A distância efetivamente aceita alimenta o custo locomotor existente e a posição persistida.
- O espaço de pose usa X lateral, Y vertical e +Z à frente. Ambos os resultados são imutáveis e renderer-neutral.
- Nem o solver de movimento nem o planejador possuem ou alteram terreno/água. O planejador contorna apenas limites amostrados de declive e vau; não calcula colisão volumétrica, força ou correnteza. O N3 aplica a saída aceita ao agente persistente. Os limites de declive, vau e coluna d'água continuam sem calibração biológica.

## Princípios

Separar três níveis:

1. **Capacidade física/biológica**: corpo e fisiologia determinam potência, velocidade, fadiga e habitat.
2. **Movimento real do organismo**: integrador espacial/colisões confirma posição e gasto.
3. **Pose visual**: animação e IK apresentam o movimento sem alterar estado fisiológico.

Core fornece os campos pertencentes a Geosphere/Hydrosphere sem permitir que Zoology os altere; Zoology interpola a rota, busca desvio de grade diante de declive/vau bloqueante e persiste somente a posição horizontal confirmada. A rota ainda pode parar se não houver caminho válido. A posição vertical aquática é um ponto médio geométrico da coluna disponível, não um comportamento de natação nem uma preferência de espécie.

**Limites ainda abertos:** não há colisão do volume corporal, reação à correnteza, integração de peixe no estado populacional nem calibração por espécie. Os planners recebem uma grade regular de elevação e profundidade e não enxergam rochas, vegetação sólida ou fluxo vetorial. Os animais persistentes atuais são perfis funcionais genéricos do L0 e ainda não consomem os genomas/anatomias Z1. Uma busca no SciSpace encontrou limites e custos dependentes de espécie, tamanho, população e contexto experimental; portanto, não foi possível substituir os parâmetros provisórios sem primeiro declarar populações de referência. Ver [auditoria de dados de locomoção Z2](../research/zoology/Z2_LOCOMOTION_DATA_AUDIT_2026-10-22.md).

## Planos corporais de pesquisa

| Família | Estrutura | Locomoção inicial | Evolução futura |
|---|---|---|---|
| Quadrúpede | Tronco, cabeça, pelve, quatro membros, cauda opcional | caminhada/trote parametrizados, IK 2 segmentos | galope, corrida, salto, equilíbrio com forças |
| Peixe | Cabeça/tronco, coluna flexível, nadadeiras | onda sinusoidal corporal e avanço em água | hidrodinâmica de nadadeiras e manobras avançadas |
| Ave | Tronco, dois membros posteriores, asas, cauda | caminhada e pose aérea visual para testes | voo com sustentação, gasto e aerodinâmica |
| Artrópode | Exoesqueleto segmentado, múltiplos membros | marcha tripod ou alternada | restrições articulares e tipos especializados |
| Serpente | Cadeia axial e cabeça | curvatura, ondulação e contato com terreno | locomoção dependente de atrito e superfície |

Não implementar todas simultaneamente.

## Métodos

- Quadrúpedes: fases cíclicas dos membros, planejamento de contatos, controle simples e FABRIK ou IK de dois segmentos para alinhar patas ao solo.
- Peixes: oscilação progressiva da cadeia axial, controlada por frequência/amplitude e posição, com deslocamento efetivo governado pelo módulo de física/habitat aquático.
- Aves: distinguir animação de asas de simulação de voo; a presença de animação não garante sustentação ou custo energético adequado.
- Vegetação/obstáculos: o corpo funcional define colisores; apenas o visual pode atravessar elementos decorativos quando não interferirem no mundo físico.
- Derivar velocidade funcional e custo locomotor de massa/biomecânica aproximada, não diretamente do tamanho do sprite.

## Interfaces propostas

```csharp
public interface ILocomotionPlannerV1
{
    LocomotionIntent Plan(DevelopedBody anatomy, MovementContext context);
}

public interface IPoseSolverV1
{
    Pose Solve(DevelopedBody anatomy, LocomotionIntent intent,
               GroundContacts contacts, Pose previousPose);
}
```

Tipos auxiliares são propostos; sua definição concreta e as unidades são tarefa de implementação. `Solve` modifica pose, não reservas corporais nem água.

## Citações

- Aristidou & Lasenby (2011), FABRIK, https://doi.org/10.1016/j.gmod.2011.05.003
- Coros et al. (2011), quadrupeds, https://doi.org/10.1145/1964921.1964954
- Tu & Terzopoulos (1994), fish, https://doi.org/10.1145/192161.192170
- Wampler et al. (2014), transferring locomotion style, https://doi.org/10.1145/2601097.2601192

## Aceite

- Comprimento de segmento preservado por IK dentro de tolerâncias.
- Patas apoiadas acompanham altura local e não deslizam excessivamente em velocidade estável, dentro de tolerância medida.
- Movimento lógico igual sob taxas de FPS distintas.
- Um peixe não atravessa terreno sólido e um quadrúpede não nada sem capacidade definida.
- Mesmo corpo sob níveis de fome/fadiga distintos pode produzir comportamentos/velocidades diferentes sem alterar sua forma permanentemente.
