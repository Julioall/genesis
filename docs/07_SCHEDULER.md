# Orquestração temporal e causalidade sem dependência circular

## Dois grafos diferentes

- **Grafo de código:** acíclico; todos os domínios conhecem contratos e bibliotecas comuns, nunca implementação de outros domínios.
- **Grafo físico/causal:** pode conter retroalimentações: `vegetação → sombra/evapotranspiração → clima/solo → vegetação`. Isso é esperado em ecossistemas.

Evitar resolver uma realimentação recursiva imediatamente por chamadas mútuas; usar fases controladas e estado anterior ou iterações numéricas delimitadas, quando justificadas por análise de estabilidade.

## Agenda inicial proposta

| Fase | Escrever/confirmar | Informações lidas |
|---|---|---|
| 0 | Core: avança tick e determina dia/janela; Cosmos: calcula a amostra de geometria solar solicitada | estado temporal anterior |
| 1 | Atmosphere: tempo e precipitação | sol + campos ambientais anteriores, como cobertura |
| 2 | Biosphere.Botany: interceptação/demanda potencial; Geosphere/Hydro: distribuição de chuva | temperatura, chuva, cobertura prévia |
| 3 | Geosphere.Soil: infiltração/solo; Hydrosphere: escoamento/rios | reservas e propostas no mesmo snapshot de fase |
| 4 | Biosphere: luz derivada da cobertura, propostas de movimento e pedidos de água/N/alimento | snapshot de recursos e posições; sem mutação pública |
| 5 | Owners Soil/Hydro/Botany/Zoology: reservas provisórias e resolução de conflitos | pedidos em lote, saldos e posições estáveis |
| 6 | Owners envolvidos: uso final, liberação de excedentes e commits de consumo/captura; Biosphere: metabolismo/crescimento/morte/reprodução | concessões provisórias e resultados confirmados |
| 7 | Biosphere.Ecology + Soil: detritos/decomposição/mineralização | mortes e deposições, temperatura/umidade |
| 8 | Core: invariantes, commit de snapshots, eventos, métricas e save checkpoint | estados consolidados |

Esse esquema é conceitual. A implementação pode separar chuva em subpassos e ajustar ordem para assegurar conservação, estabilidade e causalidade. Não forçar toda a hidrologia a 1 passo/dia quando chuva intensa exigir passo mais fino.

## Frequências L0 recomendadas (parâmetros, não compromisso científico)

- Render: alvo 30–60 quadros/s real, desacoplado.
- Movimento, alimentação e metabolismo animal: por tick fixo, ajustados por teste de distância/custo; nascimentos só atuam no tick seguinte.
- Atmosfera: atualização horária no cenário inicial; o acumulado entra uma vez no limite horário, conforme o documento 20, sem replicação nos seis ticks animais.
- Água superficial: subpassos por chuva e limites de estabilidade.
- Plantas/nutrientes: hora/dia; decomposição diária no L0.
- Reproduções/populações: por evento ou por dia.

Ao executar uma taxa definida por dia, multiplicar pelo **tempo simulado decorrente**; nunca pelo número de frames. Agendas, PRNG e subpassos necessários devem integrar save.

## Primeira integração de Zoology/N3

O runtime chama Zoology uma vez por tick lógico de 600 s, com posições e recursos do início desse tick. Na fronteira horária, fauna executa antes da atualização horária de clima, solo, água e Botany; a chuva acumulada da hora entra uma vez nesse fechamento. No tick que fecha o dia, a ordem é atualização horária, água diária/morte/reprodução de Zoology e, por fim, decomposição e confirmação de Soil. Captura e pedidos de alimento usam o snapshot anterior ao movimento; filhotes só entram no próximo tick. Esse encadeamento é a primeira implementação N3 e ainda precisa de validação integrada dos critérios quantitativos do documento 20.

## Spatial regridding

L0 usa grade ambiental única. Em evolução posterior, a atmosfera pode usar uma grade regional mais grossa que Soil e Terrain. `sample(field,location)` é adequado para temperatura; para **fluxos de água/matéria**, a transformação entre grids tem de conservar integrais por área e tempo. Definir políticas distintas para campos intensivos (°C, concentração), extensivos (kg, m³) e taxas (mm/h).

## Paralelismo

Começar sequencial/determinístico com dados por lotes; paralelizar após profiling. Proibir resultados dependentes da ordem de `Dictionary`, scheduler real do SO ou thread race. Para paralelismo, ordenar propostas antes da consolidação e testar equivalência tolerada.

## Semântica executável mínima das fases

No L0, tempo lógico inteiro e agenda fixa, execução sequencial. Cada processo recebe a janela exata desde a última execução; fluxo de chuva é consumido uma vez e taxas biológicas integram o intervalo, sem reaplicar chuva horária em cada tick menor. Um campo publicado para uma janela não pode fornecer informação futura fora dela.

Concessões na fase 5 são reservas provisórias; fase 6 finaliza uso combinado, devoluções e transferências atômicas antes de publicar resultados. Pedidos de consumo/captura referem-se às posições do snapshot da fase 4; deslocamento não permite consumir em uma posição ainda não confirmada. Resolver capturas antes de ações de suas vítimas, cancelar seus pedidos e liberar reservas, então aplicar demais consumos e movimentos. Isso evita uma presa consumir alimento reservado e desaparecer antes da contabilização. Entidades mortas na captura não atuam depois. Subpassos hídricos são limitados/configurados; ultrapassar limite de estabilidade aborta com diagnóstico, sem descartar água ou prolongar loop indefinidamente. Começar com grade ambiental única; regridding multiescala só quando necessário. Cobertura usada no clima é a do último estado confirmado; o L0 não promete realimentação meteorológica regional por evapotranspiração.
