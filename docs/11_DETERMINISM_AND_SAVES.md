# Seed, determinismo, estados espaciais e persistência

## Identidade e RNG

- `WorldSeed` normalizada e serializável, versões independentes de geradores/algoritmos.
- Derivar streams de RNG por `moduleId + processId + chunkId + entityId + operationId + generatorVersion` com hashing documentado, estável.
- Não usar RNG global consumido pela renderização; nenhuma escolha biológica pode depender de seed visual.
- Definir ordenação de entidades e desempates de transações; controlar paralelismo e operações com ponto flutuante.
- Determinismo declarado **dentro do ambiente testado**; bit-exato multiplataforma não é automático. Fixed-point pode ser usado em contas críticas depois de comprovar necessidade.

## World State

Estado composto por células/chunks/grades e registros individuais ou agregados. `CellId` identifica célula física, nunca pixel visual; `ChunkId` guarda partição; `EntityId` identifica organismo/objeto persistente. Matrizes físicas usam unidades e resoluções próprias. Leitores recebem cópias/snapshots imutáveis ou spans controlados por fase; owners aplicam escrita exclusiva em seus campos.

## Estrutura lógica de save

```text
SaveHeader
  schemaVersion
  worldSeed, worldId, logicalTick, calendar, scenarioId
  buildVersion, moduleVersions, contractVersions, generatorVersions
WorldGeneration
  recipeIds, generatorSettings, chunkSeeds
WorldState
  terrainOrReconstructionRecipe, soilWater, surfaceWater, nutrients
  plantPatches, animals, detritus, biological state
SchedulerState
  nextDueTicks, pendingScheduledEvents, rngStreams, nextEntityIds
Integrity
  hashes, reference validation, migrationHistory
```

Uma seed sozinha não reproduz crescimento, consumo ou mortalidade de um mundo vivo; armazenar alterações/estados necessários. Não salvar GPU/DOM. Migração cria novo documento e preserva original até validação. Não permitir que um upgrade aceite estado incompatível silenciosamente.

## Testes fundamentais

- Mesmo seed + versão + comandos + ticks → mesmo estado canônico no mesmo ambiente.
- `STEP(60)` equivale a `STEP(20)` + `STEP(40)`.
- Simulação headless equivale à simulação com render/câmera alterada.
- Save em `t`; continuar até `t+n` versus carregar e continuar → mesmo estado, incluindo RNG e agenda.
- Mudança de conteúdo visual não muda estado vivo.
- Save de schema antigo suportado por migração explícita ou rejeição descritiva, jamais mutação silenciosa.
- Carregar/descarregar recursos visuais de chunks não recria organismos mortos; o estado do mapa finito continua ativo e não depende da câmera.

## Checkpoint e comandos L0

Salvar apenas ao final do tick consolidado (fase 8); pedido durante atualização aguarda esse ponto. Armazenar próxima execução de cada cadência, contadores de IDs, parâmetros/perfis versionados, comandos futuros já aceitos e PRNG quando houver estado. Nenhuma proposta em voo precisa ser persistida nesse recorte. Pausa altera só avanço; comandos que mudam o mundo têm tick alvo e ordem estável. Testar equivalência de partições significa executar os mesmos ticks fixos, nunca mudar o passo numérico ao agrupar `STEP(n)`.

IDs/hashes não usam `string.GetHashCode()` ou ordem de coleções como identidade persistente. Carregamento valida finitude, unidades, referências, inventários e versões antes de substituir o mundo ativo. Salvar por arquivo temporário e substituir após validação para evitar destruir o último save válido.
