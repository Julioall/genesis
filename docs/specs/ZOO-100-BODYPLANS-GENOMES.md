# SPEC-ZOO-100 — Plano corporal, genoma e desenvolvimento

**Status:** gate técnico Z1 validado em 2026-10-22; calibração biológica por espécie continua pendente. **Escopo natural:** organismos não humanos e avatares biológicos controláveis.

## Implementação Z1 e limite de validade

`Game.Modules.Biosphere` fornece `ICreatureDevelopmentV1` e `CreatureDevelopmentModelV1`; os contratos versionados estão em `Game.Contracts/CreatureGeneticsContracts.cs`. A versão atual gera headless dois **protótipos de software** (quadrúpede terrestre e peixe de água doce), genomas tipados, planos anatômicos imutáveis, pedigree de mutação e uma receita visual independente do renderer. A integração com os agentes/saves L0, locomoção confirmada, fisiologia e renderização continua em gates posteriores.

Os números embutidos nos protótipos são envelopes de engenharia para validar determinismo, unidades, limites, topologia e mutação. Não são parâmetros medidos de uma espécie ou família natural, nem permitem chamar os protótipos de modelos biologicamente calibrados. Estudos de campo de raposa-vermelha e truta-marrom mostram por que a população, sexo e idade precisam acompanhar cada calibração: [raposas juvenis, n=163](https://connectsci.au/wr/article-abstract/27/5/525/39431/Morphological-change-and-fat-accumulation-in?redirectedFrom=fulltext) e [trutas do riacho Kan](https://doi.org/10.1163/157075607781753056). Nenhum dado dessas populações foi copiado como parâmetro do gerador.

## Entidades

- `SpeciesDefinition`: identidade de linhagem, habitat e compatibilidade reprodutiva; definição de parâmetros.
- `BodyPlan`: grafo anatômico de regras/segmentos essenciais, simetria e articulações permitidas.
- `Genome`: genes herdáveis tipados, schema, versão, seeds separadas e pedigree.
- `DevelopmentContext`: idade, condição nutricional, ambiente térmico e fatores de desenvolvimento especificados.
- `DevelopedBody`: estrutura funcional resultante — comprimentos em metros, massas por parte em kg, juntas, limites, mobilidade e sensores.
- `VisualRecipe`: dados derivados para pelagem, superfície, coloração e renderização.

## Etapas determinísticas

1. Selecionar a família/plano corporal, ex.: `vertebrate.quadruped`, `vertebrate.fish`, `vertebrate.bird`.
2. Amostrar genoma dentro de envelopes declarados e versionados, com seed estável. Só chamar essas faixas de biologicamente plausíveis após calibrá-las para espécies/populações nomeadas.
3. Interpretar genes estruturais e de desenvolvimento.
4. Montar topologia anatômica (ossos/segmentos, simetria, articulações, limites e âncoras).
5. Calcular proporções e restrições (alcance dos membros, centro de massa, volume e colisores).
6. Validar anatomia: conectividade, simetria ou assimetria intencional, ângulos, auto-intersecções excessivas, membros fora de alcance e atributos negativos.
7. Produzir fenótipo biológico e `VisualRecipe` com seeds derivadas independentes.

## Política de linhagem

Na entrega inicial, reprodução somente entre genomas cujo `SpeciesDefinition` autorize compatibilidade. Não misturar arbitrariamente aves, quadrúpedes e peixes. Criaturas irrestritas poderão ser geradas em um laboratório separado e explicitamente fantástico/experimental.

## Genes, unidades e envelopes atuais

Conjunto inicial exemplificativo (não é a lista única/definitiva):

| Gene | Unidade | Influência |
|---|---|---|
| `body.scale` | adimensional | escala corporal adulta dentro da família |
| `limb.relativeLength` | razão | alcance de passos e silhueta |
| `trunk.relativeLength` | razão | proporção tronco/cabeça |
| `coat.insulationIndex` | índice normalizado 0–1 | isolamento térmico modelado; não criar calor do nada |
| `coat.pigmentHueDegrees` | graus 0–360 | aspecto visual; pigmento não altera anatomia |
| `coat.patternFrequency` | por comprimento, quando aplicável | tamanho das manchas/faixas |
| `life.growthRate` | 1/dia ou parâmetro do modelo | velocidade de crescimento condicionada por alimento |

Genes não são reservas de energia, fome atual, estado de doença ou idade; esses são estados dinâmicos.

Os registros atuais `prototype.quadruped.generalist.v1` e `prototype.freshwater.fish.v1` têm escalas adultas de referência (respectivamente 1,25 m/15 kg/365 dias e 0,45 m/0,60 kg/200 dias) e envelopes gênicos fechados no catálogo. Esses valores, incluindo taxa de crescimento, continuam sendo **valores artificiais de teste**. O schema rejeita envelopes inválidos; isso garante consistência interna, não plausibilidade zoológica. A escala de massa usa uma relação cúbica simplificada e também não é um modelo alométrico calibrado.

`DevelopedBodyV1` contém slots de partes, articulações e âncoras sensoriais que formam um grafo determinístico. O corpo atual não modela órgãos, geometria volumétrica, colisões, interseções, distribuição anatômica de massa medida ou desempenho locomotor. `NominalStrideLengthM` e `PropulsionSurfaceAreaProxyM2` são proxies geométricos para consumo posterior por Z2, não previsões validadas de velocidade ou força.

Os deslocamentos dos segmentos são relativos à parte pai; o deslocamento da raiz está no espaço local do corpo. A composição recursiva segue o grafo pai e é usada pelo builder de geometria.

## Aceite

- Mil seeds geram anatomias válidas dentro de cada família testada.
- Mutação moderada preserva limites de família e produz diferenças observáveis.
- Mesma versão e seed resulta em identidade de genoma/estrutura/atributos derivados.
- Alterar a seed de pigmento não altera anatomia, custo locomotor ou fisiologia.
- Crescimento e reprodução não criam biomassa sem entrada declarada.

### Evidência do gate técnico Z1

`dotnet test Game.sln -c Release --nologo` passou 104/104 testes em 2026-10-22. `CreatureDevelopmentTests` verifica 1.000 seeds por protótipo, invariantes topológicas/massas, determinismo, mutação limitada a 5% do intervalo por alelo, isolamento do pigmento, export/restore e entradas inválidas. Isso valida os contratos e envelopes de software descritos acima; não valida calibração empírica, locomoção nem integração com população/saves.
