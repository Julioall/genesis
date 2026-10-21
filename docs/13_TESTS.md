# Plano de validação e aceite científico/engenharia

Cada teste registra `build`, `worldSeed`, `scenarioVersion`, parâmetros completos, grade/escala, tempo simulado, tolerância definida antes, resultado, erro de conservação e logs. Os valores L0 de referência são centralizados em [20_L0_MODEL_AND_BENCHMARKS.md](20_L0_MODEL_AND_BENCHMARKS.md); os ensaios de 1 m² fora do cenário continuam fixtures unitárias explicitamente independentes.

## Infraestrutura

- **Arquitetura:** dependency graph sem referência entre implementações dos cinco domínios; núcleo headless.
- **Contracts:** fornecedores novos coexistem com consumidores antigos; unidades/versão inválidas falham antes da integração.
- **Determinismo:** seed e input equivalentes retornam estado equivalente; save/load; partições de ticks; câmera não altera o mundo.
- **Preservação:** sem estoque negativo, sem `NaN`, sem água/biomassa/N duplicados, idempotência e conflito resolvido.
- **Performance:** tempo/tick e P95 por módulo/cliente, memória por chunk e custo de geração/caches; metas conforme dispositivo de referência declarado.

## Cenários funcionais (mínimo)

| ID | Experimento | Evidência |
|---|---|---|
| T01 | mundo parado/sem atualização | nenhum fluxo ou organismo muda indevidamente |
| T02 | sol noite/dia e estações | calendário e energia solar mudam sem usar FPS |
| T03 | chuva de 12 mm em uma célula de 100 m² (cenário L0) | entrada 1200 L; repartição em infiltração/interceptação/superfície fecha |
| T04 | chuva e solo saturado | infiltração ≤ capacidade, excedente vai para superfície/saída |
| T05 | escoamento em relevo inclinado | massa transferida é debitada/creditada por célula; depressões podem acumular |
| T06 | seca prolongada | queda de água disponível causa crescimento lento e possivelmente mortalidade |
| T07 | planta na sombra vs pleno sol | resposta de biomassa à energia limitada e tolerância de espécie |
| T08 | duas plantas competindo por água/N | recursos totais concedidos nunca excedem saldos |
| T09 | herbívoros disputando 10 g de biomassa comestível | ≤10 g realmente retirados, sem duplicação |
| T10 | herbívoro sem alimento | reservas materiais totais caem por manutenção; transferência de tecido para reserva não cria energia; morte conforme modelo L0 |
| T11 | predador com presas vs sem presas | custos/caça afetam reservas; presa não é consumida duas vezes |
| T12 | morte, fezes, folhas e decomposição | detritos/N retornam ao solo dentro de balanço e perdas declaradas |
| T13 | nova capacidade climática | consumidor de versão básica permanece funcional |
| T14 | matéria orgânica nova em Soil | consumidor apenas hídrico segue funcionando |
| T15 | substituição de Soil por outra implementação | não exige editar Atmosphere, Biosphere, Hydrosphere |
| T16 | salvar/recarregar e seguir | mesma continuação, incluindo RNG, agendas e seres vivos |
| T17 | render ligado/desligado, zoom e movimento de câmera | estado lógico idêntico no ambiente testado |
| T18 | cenário integrado 2 habitats / 2 plantas / 2 animais | cadeia ambiental e biológica observável por múltiplos ciclos |

## Regras de aprovação

- A tolerância de conservação e valores-alvo são **escolhidos e documentados antes** dos testes; nenhum resultado ruim deve ser corrigido depois alterando apenas tolerância.
- Realismo qualitativo exige experimentos contrastantes, não um único vídeo bonito.
- Extinção não é falha automática: investigar causalidade e balanços, jamais respawn corretivo oculto.
- Pelo menos 3 seeds para smoke tests de variabilidade e 1 seed canônica de regressão. Mais réplicas quando quiser fazer afirmações sobre comportamento estatístico.
- Fail de plataforma web deve ser registrado como blocker de portabilidade, mas não invalida o núcleo headless; não dizer "web pronto".
- Concluir apenas quando `dotnet build`, `dotnet test`, runnable desktop e documentação de evidências forem reais; comandos definidos pelo Codex devem existir.

## Casos adicionais que fecham lacunas do modelo

| ID | Experimento | Evidência |
|---|---|---|
| T19 | sementes, germinação e filhote com progenitor limitado | massa/N/água/reserva saem da origem; criação rejeitada não altera estoques |
| T20 | captura concorrente, consumo parcial e morte | uma captura por presa, restos contabilizados uma vez, pedidos da vítima cancelados e reservas liberadas; morto não atua |
| T21 | alimento/metabolismo/excreção/mineralização | N atravessa animais e N orgânico edáfico; energia não duplica massa |
| T22 | destino indisponível, pedido repetido e falha inesperada durante aplicação | recusa prévia sem mutação; sem débito duplicado; falha inesperada interrompe e proíbe publicar/salvar tick incompleto |
| T23 | crescimento limitado por água mas com N reservado | N não utilizado retorna e transpiração segue regra declarada |
| T24 | cadências diferentes e chuva horária | cada janela de fluxo é consumida uma vez; save preserva próxima execução |
| T25 | escala espacial, movimento e estado inválido | mm × área converte para litros; velocidade respeita distância/tempo; NaN/Infinity rejeitados |

Esses são critérios para a implementação futura. Na revisão documental de 20/10/2026 só se verificaram coerência, referências locais e cobertura de requisitos; os resultados efetivamente executados depois estão registrados na seção abaixo.

## Aplicação incremental dos gates

N0 verifica boot, pausa, dependências e determinismo do estado mínimo. N1 exercita T01–T06, a parte hídrica de T16/T22/T24/T25 e contratos utilizados. N2 adiciona T07–T08, T12, T19 vegetal e T23; N3 adiciona T09–T11, T19 animal, T20–T21. T13–T15 usam fixtures pequenas à medida que os contratos existem. N4 reúne T01–T25, incluindo T17–T18 e medições gráficas. Nenhum marco inicial exige implementar os sistemas dos marcos seguintes para executar testes que ainda não se aplicam.

No cenário integrado T18, declarar antes da execução duração, ticks, seeds, estoques iniciais e quais elos causais devem ser observados. A duração deve permitir demonstrar crescimento, consumo, morte e reciclagem, com reprodução verificada nos cenários próprios. Extinção explicada pode passar; uma população que desaparece antes de exercitar a cadeia torna essa demonstração inconclusiva, sem autorizar respawn corretivo.

## Evidência executada nesta branch

O escopo implementado é N0–N2, sem fauna. `dotnet build Game.sln -c Release` compila runner, módulos, cliente DesktopGL e testes. `dotnet test Game.sln -c Release` cobre o vetor RNG L0, repetibilidade e partição de ticks, snapshot imutável, geometria solar, chuva/solo saturado, escoamento D8, crescimento limitado por luz/água/N, decomposição e conservação de massa/N, continuação após save/load no meio do dia e cadência diária. Os casos futuros T09–T11 e a parte animal de T19–T21 permanecem fora do escopo e não foram executados.

Smoke runs adicionais avançaram seeds 1, 2 e 3 por 52.560 ticks cada. Isso evidencia execução longa e terminações finitas para essas seeds; não substitui a instrumentação completa de T01–T25 nem validação de campo. Os 19 testes Release também cobrem a variação real de elevação na malha, a continuidade de bordas entre chunks, a geometria dos afloramentos, a drenagem e a distribuição determinística de flora. Na revisão ecológica v13, seeds 1–3 avançaram até o tick 144 e fecharam água, matéria seca e N dentro das tolerâncias registradas em [20_L0_MODEL_AND_BENCHMARKS](20_L0_MODEL_AND_BENCHMARKS.md).

A build Release do cliente foi aberta e respondeu com o título `ASTERRA - Temperate Valley (L0)`. A captura `genesis-flora-v17-highland-forest.png` e a medição de 60 s estão fora do repositório no diretório de visualizações do Codex; seed 1, 1280 × 720, aquecimento de 5 s: 3541 quadros, média 59.02 FPS, P95 17.20 ms, P99 45.80 ms, CPU 58.2% de um núcleo e working set 181.5 MiB. A imagem mostra a geometria e os bosques ampliados, mas ainda não alcança a densidade vegetal nem as margens de rio da referência.
