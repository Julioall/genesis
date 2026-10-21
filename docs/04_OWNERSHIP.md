# Matriz de propriedade, fontes, consumidores e pontos de integração

**Escopo:** neve, gelo, camadas de solo e sedimentos abaixo indicam propriedade futura; não exigem estoques ou contratos no L0.

**Regra:** cada estado persistente/mutável tem um proprietário único. Dois domínios podem **influenciar** a mesma grandeza sem ambos **escreverem** a mesma memória. Processos calculam propostas; o proprietário resolve e confirma.

| Grandeza ou estoque | Owner | Consultantes e pedidos | Forma |
|---|---|---|---|
| Tempo, dia, estação astronômica, irradiância acima da atmosfera | Cosmos | todos | snapshot espacial/temporal |
| Temperatura/umidade do ar, vento, nuvens, precipitação gerada | Atmosphere | Soil, SurfaceWater, Botany, Zoology | campos; chuva como fluxo de fronteira |
| Altitude, declividade, forma de relevo | Geosphere.Terrain | Atmosphere, SurfaceWater, Botany | campo e eventos raros |
| Rochas e substrato geológico | Geosphere.Geology | Soil, Geomorphology | campos/receitas |
| Água retida nos poros e camadas do solo | Geosphere.Soil | Botany, Hydrosphere, Atmosphere | estoque e retirada confirmada |
| Nutrientes minerais, matéria orgânica edáfica | Geosphere.Soil | Botany, Ecology | estoque + transferência |
| Água superficial (lago/rio/poça), gelo/neve depositada | Hydrosphere | Botany, Zoology, Geosphere | estoque/fluxo |
| Biomassa viva, sementes, copa e raízes vegetais | Biosphere.Botany | Soil, Zoology, render | estado + consumo via pedido |
| Corpo, reservas, necessidades e posição animais | Biosphere.Zoology | Ecology, render | estado + ações |
| Detritos de origem orgânica e atividade decompositora agregada | Biosphere.Ecology | Soil, Botany | estoque + mineralização/deposição |
| Sedimento ainda incorporado ao terreno | Geosphere.Geomorphology | Hydrosphere | estoque e erosão confirmada |
| Sedimento já transportado na água | Hydrosphere.SurfaceWater | Geomorphology | carga suspensa + deposição por transação |
| Métricas e rastros de transferência | Game.Core ledger/diagnostics | todos | registro imutável, sem authority científica |

## Situações que exigem negociação

**Chuva → água:** Atmosphere publica precipitação por área e intervalo. Hidrosfera recebe água superficial incidente; interceptação da vegetação pode reduzir a fração que chega ao solo. Um coordenador de fluxos confirma uma única distribuição, sem duplicar chuva. No L0 não há reservatório de copa: a fração interceptada evaporada sai pela fronteira, e o restante entra uma vez na superfície.

**Infiltração:** Hydro propõe lâmina incidente; Soil aplica capacidade de infiltração e confirma volume aceito; resto fica em água superficial. Nunca calcular infiltração em ambos de forma independente.

**Evapotranspiração:** atmosfera fornece demanda potencial, Botany propõe absorção/transpiração, Soil confirma água retirada, e o ledger registra saída do recorte modelado. Evitar que o mesmo volume seja debitado duas vezes por Soil e Vegetation.

**Herbivoria:** Zoology solicita biomassa de uma planta/coorte; Botany debita o estoque confirmado; Zoology recebe massa efetiva e converte em reserva/fezes pela eficiência declarada.

**Decomposição:** Botany/Zoology depositam detritos em Ecology; Ecology transforma fração em matéria edáfica/N disponível por transação confirmada por Soil. CO₂/respiração ou perdas de carbono precisam ser fonte/sink externo explícito quando o reservatório atmosférico de carbono não está modelado.

**Sombra:** Cosmos fornece energia acima da atmosfera; Atmosphere publica energia incidente à superfície após nuvens e atenuação topográfica quando incluída. Botany calcula a luz disponível aos patches a partir dessa energia e da cobertura vegetal confirmada. Cada atenuação se aplica uma vez; Botany não altera o campo meteorológico nem depende de sombra visual. Não criar serviço de radiação adicional no L0.

## Critério de conflito

Se duas rotas pretendem retirar o mesmo recurso, processar demandas em lote a partir do **mesmo estado de referência**, alocar saldo com regra documentada (fairness, prioridade ou desempate por ID), aplicar o débito uma só vez e distribuir resultados reais.
