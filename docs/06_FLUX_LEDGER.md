# Conservação — inventários, fluxos e transações

## Princípio

A simulação não pode adicionar/remover recursos modelados sem contabilizar sua origem ou destino. O diagnóstico por componente verifica:

`EstoqueFinal - EstoqueInicial = EntradasInternas - SaidasInternas + EntradasDeFronteira - SaidasDeFronteira`.

Transformações são registradas como saídas de um reservatório e entradas em outro, com conversão declarada. Não existe lançamento genérico de “criação” para compensar erro de balanço.

O escopo físico é **aberto** no MVP: não existe necessariamente reservatório de água atmosférica global nem carbono CO₂ atmosférico. Isso é permitido **se** entradas/saídas de fronteira forem registradas e os balanços de cada estoque se fecharem.

## Livros independentes por substância/grandeza

| Ledger | Reservatórios no MVP | Atenção |
|---|---|---|
| Água | `surface`, `soil`, `animal`, `boundary` | mm em célula × área = volume; evaporação/transpiração saem pela fronteira se vapor não for modelado |
| Matéria seca / biomassa | `plant`, `animal`, `detritus`, `soilOrganicMatter`, `boundary` | fotossíntese recebe matéria/carbono de fronteira; respiração exporta parte; não contar energia como massa |
| Nitrogênio | `soilMineralN`, `soilOrganicN`, `plantN`, `animalN`, `detritusN`, `boundary` | produção vegetal move N do solo para a planta; decomposição devolve fração; perdas explícitas |
| Energia global (opcional L0; orçamento da fauna obrigatório) | luz incidente, assimilação, reservas, dissipação | uso fisiológico dissipa energia; não tratar biomassa em g como energia em J |

**Definição de estoque:** massa (ou volume, no caso de água) expressa de forma dimensionalmente coerente. Não somar `kg` e `mm`, nem tratar fração de saturação como quantidade de água.

## Protocolo de transação

1. Observar snapshot estável da fase anterior.
2. Preparar propostas com `RequestId`, `FromReservoir`, `ToReservoir`, `Quantity`, `Unit`, `TimeWindow` e owner.
3. Owner verifica saldo, capacidade, prioridade, tipo e condições; aloca reservas provisórias; pedidos combinados confirmam uso e liberam excedentes antes do débito.
4. Aplicar débito/crédito exatamente uma vez com idempotência por RequestId.
5. Registrar lançamento confirmado e emitir resultado/observação; evento somente se fato relevante.
6. No fim da fase/tick, testar resíduos, limites e origem dos fluxos.

## Exemplo numérico de água

Célula de `1 m²`; chuva acumulada 20 mm durante intervalo, ou 20 L. Se 3 mm forem interceptados e exportados (evaporação), 12 mm infiltrarem e 5 mm permanecerem para escoar: `20 = 3 + 12 + 5 mm`. Se o escoamento entrar em outra célula, debitar a célula doadora e creditar a recebedora; o balanço global da área só perde água quando ela cruza a fronteira. O teste é ilustrativo, não fórmula de infiltração universal.

## Exemplo biológico de matéria

Planta com 100 g de matéria seca perde 12 g por consumo confirmado. Herbívoro assimila 7 g equivalentes de matéria, produz 3 g de fezes/detritos e exporta 2 g por respiração/perdas em uma representação L0. `12=7+3+2` para o intervalo, mas **não inferir 7 g de reserva energética** sem uma conversão e orçamento físico explícitos.

## Observabilidade

Relatório de conservação por tick e por cenário: `reservoir`, `initial`, `in`, `out`, `final`, `balanceError`, `sourceEvents`, `sinkEvents`, `unit`, `assumptions`, `tolerance`. Falha relevante gera diagnóstico; não corrigir estoque negativo por clamp sem relatar.

## Critério de tolerância

Definir antes de executar testes: erro absoluto e relativo, frequência de acumulação, ponto flutuante usado, magnitude dos estoques, escala espacial. `epsilon` não pode mascarar erros de ordem física. Nunca arredondar campos persistidos a cada tick sem analisar deriva.

## Unidade canônica e atomicidade L0

`1 mm × 1 m² = 1 L`; para obter m³ dividir por 1000. Converter estoques por área em quantidades antes de transferir entre células. Nitrogênio acompanha composição e tem ledger próprio, sem somá-lo novamente à matéria seca. Transformações não autorizam criar água ou N: nelas a soma dos destinos fecha contra a retirada; fonte/sumidouro externo precisa de processo e motivo identificados.

Começar com buffers de propostas e commit sequencial por fase, sem banco transacional. Antes de mutar qualquer owner, validar todos os débitos/créditos de uma transferência, inclusive capacidade de destino, presença de entidades e duplicação. Aplicar todos ou nenhum para cada transferência validada. Rejeição esperada (saldo/capacidade insuficiente) retorna recusa ou concessão parcial antes de mutar. Exceção inesperada durante aplicação interrompe a simulação, invalida o tick em trabalho e impede publicá-lo ou salvá-lo; retomar exige restaurar o último checkpoint válido. Não prometer reversão em memória de fases já aplicadas. Commit idempotente usa chave `(tick, fase, processo, RequestId)` e só retém deduplicação pelo intervalo necessário. Checkpoint apenas após fase 8, sem transações em voo; não exigir rollback distribuído ou save no meio de fase.

Energia global é diagnóstico opcional; orçamento alimentar em J é obrigatório para fauna e precisa manter vínculo com a reserva material. Não afirmar conservação termodinâmica global com temperatura/clima prescritos.

No L0, a captação vegetal é fluxo de passagem solo → fronteira; a água corporal animal é estoque persistente. Detalhes de partição e do N orgânico estão em [MVP biológico](09_BIOLOGICAL_MVP.md).
