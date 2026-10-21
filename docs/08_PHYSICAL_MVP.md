# Catálogo de processos físicos do MVP e extensões planejadas

Abaixo estão **níveis L0**, suficientes para iniciar uma simulação causal. Não são calibrações terrestres. Equações, parâmetros, tolerâncias e metas do cenário inicial ficam centralizados em [20_L0_MODEL_AND_BENCHMARKS.md](20_L0_MODEL_AND_BENCHMARKS.md); este catálogo define responsabilidade e escopo. Cada item terá ficha de processo e teste de limite usando o template 17.

| Processo | Entradas | Saídas/estado | Responsável | L0 / observação |
|---|---|---|---|---|
| Calendário/dia-noite | tick, duração do dia/ano, latitude e inclinação | hora, estação, ângulo/altura solar | Cosmos | geometria simplificada verificável; nem toda estação significa mesmo clima |
| Insolação potencial | ângulo solar, constante do cenário | energia acima da atmosfera | Cosmos | considerar noite como ausência de luz |
| Radiação efetiva | insolação, nuvens, relevo/sombra | fluxo/energia na superfície | Atmosphere + campo Terrain | atenuação parametrizada, sem duplicar dono |
| Temperatura local | estação, altitude, radiação e perturbações | temperatura °C | Atmosphere | gradiente altitudinal configurável, não universal |
| Umidade relativa | temperatura, chuva recente, parâmetros | fração [0,1] | Atmosphere | proxy L0; sem conservação fechada de vapor |
| Chuva espacial | estação, umidade/probabilidade, ruído por seed | mm acumulados por janela | Atmosphere | gera entrada externa de água; não cria rio diretamente |
| Declive e rota de drenagem | campo de elevação, borda | direção e gradiente | Geosphere.Terrain | D8 pode servir no MVP; depressões tratadas sem loop infinito |
| Estoque superficial | água antes, precipitação líquida, escoamentos | água em lâmina mm ou m³/célula | Hydrosphere | capacidade/borda explícitas; poças podem secar |
| Infiltração | incidente, capacidade hidráulica, saturação | água efetiva aceita pelo solo e remanescente superficial | Geosphere.Soil | fórmula de capacidade L0, negociada com Hydro |
| Água retida/evaporada | água solo, demanda evaporativa, cobertura | solo mm e perdas | Geosphere.Soil | remover apenas concedido; água não é umidade relativa |
| Rio/lago | topografia, água, fluxos | rede/volume/drenagem | Hydrosphere | canais D8, água inicial em trechos/depressões e descarga finita de aquífero; sem recarga subterrânea, erosão ou vazão perene ilimitada |
| Nitrogênio no solo | estoque mineral, retirada vegetal, mineralização | g N/m² | Geosphere.Soil | N agregado, não N/P/K completo no MVP |
| Matéria orgânica edáfica | detritos transferidos/decomposição | g matéria seca/m² | Geosphere.Soil | transação de Ecology; não materializar nutrientes gratuitamente |

## Efeitos importantes da natureza

- **Topografia** influencia escoamento, radiação por exposição quando modelada, variação térmica e habitat.
- **Chuva** influencia água superficial, umidade do solo, crescimento de plantas e risco de alagamento.
- **Cobertura vegetal** influencia interceptação e sombreamento, limita erosão futura e controla entradas de detritos.
- **Solo** limita crescimento por retenção hídrica e disponibilidade de nutrientes.
- **Água** limita sobrevivência animal e reprodução; fluxo pode alterar acesso local/habitat.

## Não implementar agora, mas deixar portas científicas claras

1. Pressão atmosférica dinâmica e vento fluidodinâmico 3D.
2. Neve/gelo hidrológicos: documentar a intenção de `ISnowPack`, sem interface executável no L0; entrada da Atmosphere e estado em Hydrosphere.
3. Recarga dinâmica de aquífero e troca profunda entre Soil/Hydrosphere, sem criar dois owners.
4. Erosão e sedimentos conservativos: trilha científica futura após teste hídrico robusto.
5. Fosfato, potássio, microbioma, geoquímica detalhada e água de qualidade.
6. Marés, oceanos globais, tectonismo ativo e ciclos astronômicos complexos.

O gate de avanço verifica fenômenos orientados por mudanças ambientais, não a semelhança visual do terreno sozinho.
