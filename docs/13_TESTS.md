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
| T17 | render ligado/desligado, projeção, zoom, câmera e carregamento de chunks visuais | estado lógico idêntico; apresentação não consome RNG ecológico nem altera estado |
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

Esses critérios foram originalmente definidos como gates futuros. Os resultados executados desde então e as lacunas ainda abertas estão registrados na seção de evidência e no índice de relatórios abaixo.

## Aplicação incremental dos gates

N0 verifica boot, pausa, dependências e determinismo do estado mínimo. N1 exercita T01–T06, a parte hídrica de T16/T22/T24/T25 e contratos utilizados. N2 adiciona T07–T08, T12, T19 vegetal e T23; N3 adiciona T09–T11, T19 animal, T20–T21. T13–T15 usam fixtures pequenas à medida que os contratos existem. N4 reúne T01–T25, incluindo T17–T18 e medições gráficas. Nenhum marco inicial exige implementar os sistemas dos marcos seguintes para executar testes que ainda não se aplicam.

No cenário integrado T18, declarar antes da execução duração, ticks, seeds, estoques iniciais e quais elos causais devem ser observados. A duração deve permitir demonstrar crescimento, consumo, morte e reciclagem, com reprodução verificada nos cenários próprios. Extinção explicada pode passar; uma população que desaparece antes de exercitar a cadeia torna essa demonstração inconclusiva, sem autorizar respawn corretivo.

## Estado das evidências em 22/10/2026

- **N0–N3:** implementados; o relatório N3 registra o cenário 40/4, três seeds por 365 dias, conservação de água/matéria/N, save/load determinístico e throughput headless dentro do gate. A cadeia alimentar é observável no primeiro dia; populações finais não são alvo de equilíbrio. Consulte [Zoology/N3](reports/ZOOLOGY_N3_2026-10-22.md).
- **Z1–Z5:** há entregas técnicas parciais com limites próprios. Genomas/linhagem não dirigem a ecologia; locomoção e energia não estão calibradas por espécie; corpo e peixe são protótipos visuais. Consulte os relatórios no [índice de baselines](reports/README.md).
- **N4:** cliente e renderer estão implementados. Benchmarks de 60 s passaram na máquina documentada, mas a rubrica visual não passou; hardware mínimo, outras plataformas e integração visual completa continuam pendentes. A cachoeira demonstrativa é fixture de apresentação, não resultado hidráulico. Consulte [N4 — integração visual](reports/N4_VISUAL_INTEGRATION_2026-10-22.md).
- **Lacunas ecológicas:** lago com estágio/extravasamento dinâmico e carcaça persistente ainda não existem. A morte é transferida para detritos agregados; por isso, o teste de carcaça abaixo permanece um gate futuro.

## Gatilhos de experiência ainda a fechar

Estes gates complementam conservação e testes unitários: exigem uma sequência de snapshots/eventos que torne a cadeia causal legível. Não se marcam como concluídos só porque um teste isolado de função passou.

| ID | Experimento observável | Estado neste checkout |
|---|---|---|
| EX-01 | Bacia sob chuva: volume, estágio, soleira e extravasamento | Pendente: topologia e curva de armazenamento existem; lago dinâmico/extravasamento não |
| EX-02 | Recarga seguida de estiagem, com transição gradual de água e habitat | Parcial: clima, solo e superfície variam; não há reservatório de lago dinâmico |
| EX-03 | Perfis vegetais contrastantes sob seca e reidratação, sem ressuscitar árvores | Parcial: biomassa responde à água; estresse persistente/coortes e estado visual de morte não existem |
| EX-04 | Herbívoros buscam água/alimento e movimento respeita obstáculos amostrados | Parcial: comportamento N3 e solver terrestre existem; limites/calibração por espécie permanecem abertos |
| EX-05 | Predação concorrente abate/consome uma presa no máximo uma vez | Validado no escopo N3; detalhes em [Zoology/N3](reports/ZOOLOGY_N3_2026-10-22.md) |
| EX-06 | Acesso de carniceiros acelera consumo e deixa restos/ossada persistentes | Pendente: não há entidade `Carcass` |
| EX-07 | Seca prolongada encadeia recursos, plantas e populações sem resgate artificial | Parcial: cenários anuais foram validados; falta aceite visual/causal de seca e recuperação |
| EX-08 | Save/load durante cheia e após morte preserva continuação e IDs | Parcial: continuidade N3 validada; cheia de lago e carcaça persistente ausentes |
| EX-09 | Câmera/LOD diferentes preservam o hash científico | Parcial: separação visual/lógica está coberta; validação integrada de todos os LODs ainda falta |
| EX-10 | Seeds 1–3 por 365 dias mostram histórias diversas e explicáveis | Parcial: balanços e trajetórias estão documentados; extinções anuais limitam histórias posteriores |
| EX-11 | Política heurística versus neural nas mesmas condições | Opcional futuro; não é requisito de N3/N4 |
