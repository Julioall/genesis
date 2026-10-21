# Visão de produto, limites da natureza e primeira entrega

## Visão

Mundo vivo e emergente: atmosfera muda a oferta de água e calor; superfície redistribui água; solo regula reservas e nutrição; plantas crescem, disputam luz, água e nutrientes; animais consomem biomassa e outros animais; organismos deixam detritos e os decompositores devolvem nutrientes. A observação do jogo explica **por que** um evento ocorreu, não só que ocorreu.

O mundo precisa ser natural, procedural e observável, com apresentação capaz de comunicar relevo, volume, água, vegetação, rocha e profundidade. A stack do cliente é C#/.NET com MonoGame; 2D, composição híbrida e 3D estilizado são técnicas elegíveis dentro dela. Não há exigência de pixel art, câmera top-down fixa, sprites ou tiles. A simulação permanece separada da apresentação, e o mapa pode ser maior que a área visível por regiões/chunks visuais. Não exigir simulação molecular ou fidelidade climatológica de pesquisa. Consulte [direção visual e técnicas de apresentação](22_VISUAL_DIRECTION_AND_TECHNOLOGY.md).

## Incluir na primeira entrega funcional

| Área | Escopo inicial mínimo | Fora do MVP |
|---|---|---|
| Cosmos | horas, dia/noite, ano, estação astronômica, sol geométrico | múltiplos corpos gravitacionais, órbitas n-corpos, marés |
| Atmosphere | radiação efetiva, temperatura e chuva espacial/temporal, umidade como proxy | vento e advecção, dinâmica atmosférica de alta resolução, granizo |
| Geosphere | relevo, declive, substrato, 2+ solos, água no solo, nutriente N agregado, matéria orgânica | tectônica, geologia profunda, geoquímica completa |
| Hydrosphere | água superficial em lâmina, infiltração, escoamento local, depressões e saídas de borda, evaporação | canais/rios persistentes, oceanografia, transporte químico detalhado, aquíferos completos, gelo dinâmico |
| Biosphere | gramíneas+árvores ou arbustos; herbívoros+predadores; detritos/decomposição agregada; reprodução e morte | IA neural, genética populacional avançada, doenças complexas, dezenas de espécies |
| Cliente | representação visual procedural, legibilidade dos estados, layers de diagnóstico, inspeção, pausa, aceleração, seed, save/load | animação cinematográfica, editor avançado, online |

## Critérios observáveis para dizer “mundo vivo”

1. Repetir seed reconstitui condições iniciais no mesmo build.
2. Depois da chuva, estoques e escoamento mudam coerentemente, com balanços fechando.
3. Patches vegetais respondem de forma diferenciada a sombra, solo e disponibilidade hídrica.
4. Animais se alimentam de estoques reais; suas populações não são automaticamente mantidas.
5. Morte leva biomassa a detritos e nutrientes retornam, sem criar matéria arbitrariamente.
6. Uma seca prolongada altera vegetação, população herbívora e atividade predadora indiretamente.
7. O usuário observa causa: estado inicial, processos, fluxos, fatores limitantes e estado final.

## Resoluções e fidelidade

No L0, uma grade ambiental comum, plantas por patches por célula/grupo e animais como agentes discretos; cadências diferentes são explícitas no scheduler. Grades meteorológicas próprias e plantas individuais ficam para evolução posterior. L0 é heurístico e testável; L1 exige formulação baseada em processo e comparação com referência. Cada modelo tem ficha contendo **fundamento/hipótese, parâmetros, domínio de validade, processo, unidade, taxa/estoque e teste**.

## Não objetivos explícitos

Nenhum humano/civilização, exploração espacial, economia, política, obrigação de simular toda a física em 3D, precipitação com microfísica de nuvens, oceanos globais, genética molecular ou ecologia exaustiva. A possibilidade de uma apresentação 3D não obriga todos os processos a usar uma simulação volumétrica. Esses aprofundamentos científicos podem ser objeto de fases futuras **sem obrigar a definir contratos gigantes hoje**.

## Limites operacionais do primeiro cenário

O MVP continua exigindo os cinco domínios, dois grupos vegetais, herbívoro, predador e decomposição. Para conter custo, usar mapa finito inteiramente simulado, vegetação em patches por célula/grupo e fauna individual por regras. Não iniciar com mundo infinito, plantas alternando entre agente e patch, streaming de simulação, LOD biológico, pathfinding global ou simulação distribuída.

Antes de N1, registrar no cenário versão, dimensões, área em m² por célula, duração do tick, cadências, duração do experimento e população inicial. O contrato quantitativo L0 está em `docs/20_L0_MODEL_AND_BENCHMARKS.md`: 64 × 64 células de 100 m², tick de 10 minutos, clima/hidrologia/vegetação/N horários, ação animal por tick, decomposição e reprodução diárias, 40 herbívoros e 4 predadores iniciais. Esses números são heurísticos, não densidades ou velocidades biologicamente calibradas; devem ser avaliados em N1–N3 antes de prometer escala ou equilíbrio.

Capacidade de armazenamento não pode eliminar excedentes nem bloquear nascimentos silenciosamente: expandir até o orçamento declarado ou interromper com diagnóstico. Nenhum teto técnico estabiliza a ecologia. Guardar apenas rastros recentes em memória e totais acumulados por reservatório; histórico completo por tick é diagnóstico opt-in.

Uma extensão só entra se resolver falha de aceite, conservação ou causalidade do MVP. Caso contrário, registrar no backlog com custo e impacto e manter fora da entrega. Catálogos futuros descrevem intenções, não obrigam interfaces, pastas ou classes antecipadas.
