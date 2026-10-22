# SPEC-ZOO-120 — Fisiologia, recursos e integração ecológica

**Status:** o balanço em joules está integrado ao metabolismo N3 de manutenção e movimento; a manutenção basal é priorizada no protótipo L0 e o percurso é limitado pelo saldo de energia. Perfis fisiológicos calibrados, alocação energética de crescimento/reprodução e organismo aquático continuam abertos. `ICreatureEnergyBudgetV1` fecha o orçamento por intervalo sem prever demandas nem converter joules em massa.

## Objetivo

O animal é um organismo com necessidades, recursos e história de vida, não apenas uma entidade que se move. Deve responder ao ambiente simulado.

## Estoques por indivíduo

- Massa estrutural (kg ou g): não confundir com energia disponível.
- Reserva energética (J): recebida por assimilação alimentar, usada em manutenção e atividades.
- Água corporal (kg ou L): entradas por ingestão/alimento e perdas fisiológicas.
- Idade/maturidade/reprodução; estado de saúde e integridade física.
- Temperatura corporal simplificada, quando exigida pelo perfil.

**Conta básica de energia utilizável no intervalo:**

`E_final = E_inicial + E_assimilada - E_manutencao - E_movimento - E_termorregulacao - E_crescimento - E_reproducao - E_outras_perdas`.

**Biomassa/matéria:** ingestão remove matéria da presa/recurso e a distribui entre material assimilado, excretas, reservas/estrutura e saídas externas declaradas. Energia e matéria nunca são somadas entre si. Mecanismos bioquímicos não simulados exigem fronteiras explícitas.

## Acoplamento do ambiente

| Entrada | Contrato fornecedor | Influência |
|---|---|---|
| Temperatura, chuva, vento | Atmosphere/Climate | atividade, conforto e custo termorregulatório |
| Altitude/declive/obstáculos | Geosphere/Terrain | acesso e custo de locomoção |
| Água disponível, profundidade, correnteza | Hydrosphere | hidratação, habitat aquático e deslocamento |
| Plantas e frutos | Biosphere/Botany | dieta e biomassa comestível disponível |
| Detritos e decomposição | Biosphere/Ecology | retorno de matéria após morte e excreção |
| Interações com outras criaturas | Biosphere/Zoology/Ecology | predação, competição, reprodução, cooperação |

Todas as retiradas de um reservatório são mediadas por transação/solicitação; jamais subtrair diretamente um estado pertencente a outro módulo.

## Primeiro cenário de vida natural

Uma região com rio e várzea, arbustos e gramíneas e pelo menos:
- um herbívoro terrestre simples, capaz de comer plantas e beber água;
- um predador oportunista, capaz de consumir herbívoros;
- um organismo aquático simples, ligado à disponibilidade de água;
- decomposição agregada de cadáveres/excretas e retorno de nutrientes ao solo.

O sistema precisa permitir morte por escassez sem *respawn* silencioso. Quando a espécie desaparece, é um possível resultado do modelo e deve ser registrado.

## Controles básicos e evolução de fidelidade

L0: limites de fome, sede, custo por distância, ciclo de vida, reprodução com custo e mortalidade explícita. L1: termo-regulação simples, crescimento gradual condicionado por ingestão, doenças simplificadas e competição. L2: parâmetro energético calibrado, ciclos por tecido, ecologia comportamental e resposta ambiental aprofundada.

Inspiração: Kooijman & Lika (2014) https://doi.org/10.1111/brv.12082. O modelo simplificado não deve ser chamado de DEB completo.

## Subgate de contabilidade disponível

`CreatureEnergyBudgetModelV1` recebe joules de reserva inicial, energia bruta alimentar, eficiência de assimilação e demandas para manutenção, atividade, processamento digestivo, termorregulação, perda excretória, crescimento, reprodução e outros processos. Retorna energia assimilada, energia fecal, consumo total, reserva final não-negativa e demanda não atendida. A identidade é:

`reserva_inicial + energia_bruta_alimentar = reserva_final + energia_consumida + energia_fecal`.

O N3 converte a reserva seca disponível e o alimento seco para joules usando o valor L0 já existente de `18.000 J/g`. O protótipo reserva a demanda de manutenção basal antes de locomoção; a distância máxima permitida usa o saldo depois dessa manutenção e o proxy de custo atual `10 J/m`. Assim, quando a energia disponível não cobre a manutenção, o agente não gasta energia em movimento. A demanda basal sem cobertura é reportada separadamente; só ela aumenta o déficit de manutenção. A energia consumida retorna a gramas de respiração, e o saldo mantém a reserva seca derivada sem criar outro estoque persistente.

Esse acoplamento ainda herda o fator de assimilação, a conversão de energia, custo de atividade e demanda simplificada do N3; são aproximações de engenharia, não coeficientes calibrados. Priorizar manutenção antes de atividade é a política explícita deste protótipo, inspirada por recomendações de modelos ABM, não uma regra universal por espécie. As outras categorias de demanda ficam em zero enquanto não houver modelo. Crescimento e reprodução continuam alocados pelo balanço de matéria seca, não por custo em joules. O resultado não distribui déficit entre órgãos nem prediz fisiologia completa. Matéria seca, nitrogênio e água permanecem em contas próprias.

Uma auditoria de dados SciSpace está em [Z3 — dados por população](../research/zoology/Z3_PHYSIOLOGY_DATA_AUDIT_2026-10-22.md). Ela identifica estudos primários úteis e a recomendação de explicitar prioridades de energia, mas exige selecionar uma população e recuperar tabelas/equações completas antes de substituir os proxies atuais ou definir custos de crescimento/reprodução.

## Testes

1. Uma criatura que não encontra água pode reduzir suas reservas e morrer por causas verificáveis.
2. Herbívoro reduz exatamente o recurso vegetal consumido, considerando assimilação/rejeitos.
3. Predação transfere biomassa; a morte produz detritos contabilizados.
4. Temperatura altera gasto quando a capacidade é modelada; não altera genes.
5. Reprodução reduz recursos parentais; filhotes têm genealogia, massa e estágio inicial definidos.
6. Mesma seed e mesmas condições externas dão mesmo estado e eventos no mesmo ambiente numericamente controlado.
