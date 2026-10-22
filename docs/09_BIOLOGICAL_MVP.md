# Modelos biológicos L0: vegetação, fauna, ecologia e decomposição

## Princípios

Os valores quantitativos do cenário L0 (curvas, estoques, taxas, tolerâncias e metas) são definidos somente em [20_L0_MODEL_AND_BENCHMARKS.md](20_L0_MODEL_AND_BENCHMARKS.md). Este documento preserva os limites biológicos e o comportamento requerido; não adicionar espécie, estágio de vida ou processo que não conste no escopo L0.

A proposta ecológica inclui competição, mortalidade, heterogeneidade e resultados incertos. Não impor equilíbrio populacional por `respawn` artificial nem chamar densidade estática de ecossistema. Garantir recursos finitos, estoques e perdas rastreáveis.

## Botany — produtores primários

No primeiro mundo: **dois grupos funcionais** (gramíneas e árvores, ou gramíneas e arbustos se isso facilitar a execução). Cada grupo tem identidade funcional, faixa térmica, resposta a água e luz, demanda de nitrogênio, biomassa e cobertura agregadas por patch, produção de sementes como fração do crescimento, germinação, mortalidade e produção de detritos. Órgãos aéreos/radiculares e maturação vegetal explícita ficam fora do L0.

Loop de crescimento L0:

1. Consultar irradiância, temperatura, solo hídrico e N de forma imutável.
2. Calcular crescimento potencial e propor demanda conjunta de água/N em unidades explícitas.
3. Receber reservas provisórias dos owners, limitar o incremento pelas concessões e confirmar apenas o consumo efetivo; liberar excedentes na mesma fase.
4. Contabilizar entrada de carbono/matéria externa para nova biomassa, N retirado, água consumida e energia não conservada no modelo.
5. Alocar biomassa, manter reserva, germinar/dispersar sementes e transferir mortos/folhas a detritos.

Para L0, competição por luz e água pode ser por cobertura/patch, não precisa ter folha individual como objeto físico. Perfil funcional é receita imutável; seed controla variação determinística. Nenhum deles representa genótipo no L0. Características expressas respondem ao ambiente; fisiologia é o estado vivo.

## Zoology — consumidores e agentes

Dois perfis funcionais: **herbívoro** (come biomassa vegetal) e **predador** (come herbívoro). Não precisar de espécie terrestre específica para o primeiro gate, mas dar nome de perfil funcional, tamanho, reserva, necessidade de água, idade/maturação, velocidade, campo sensorial aproximado, custos de movimento, reprodução e risco de morte.

Loop animal L0: perceber campos e recursos, buscar alvo, resolver ocupação/movimento com IDs estáveis, solicitar consumo, receber quantidade efetiva, assimilar parte do alimento, gastar manutenção/atividade, lançar excretas/detritos, reproduzir se houver reserva efetiva, envelhecer e morrer. Decisões por regras/estado; **sem aprendizado neural nesta entrega**.

## Ecology — relações e reciclagem

- Competição por água e espaço; disputa por biomassa com consumo não superior ao estoque.
- Predação altera população e disponibilidade de alimento; não atualizar estado do predador e vítima antes do commit atômico da captura.
- Serapilheira/fezes/cadáveres passam a estoque de detritos **de propriedade de Ecology**.
- Decompositores agregados (fungos e microrganismos tratados como atividades/densidade agregadas), afetados por temperatura e umidade. Conversão de detritos/N em matéria orgânica/N do solo por transação com Geosphere.Soil; respiração/perdas registradas.
- Populações podem migrar localmente, reproduzir-se ou extinguir-se. Abundância não deve voltar automaticamente ao valor inicial.

## Variáveis de estado mínimas

| Agente/estoque | Identidade | Propriedades principais |
|---|---|---|
| PlantPatch | `PlantPatchId`, `SpeciesProfileId`, `CellId` | biomassDryGPerM2, nitrogenGPerM2, seedBankGPerM2, seedBankNitrogenGPerM2; cobertura/demanda derivadas |
| Animal | `AnimalId`, `FunctionalProfileId`, posição | ageDays, structuralDryG, reserveDryG, hydrationL; N corporal é derivado da composição do perfil, assim como energia/prontidão |
| Detritus | `CellId` | organicDryGPerM2, nitrogenGPerM2 |
| Soil | `CellId` | soilWaterMm, capacityMm, mineralNGPerM2, organicDryGPerM2, soilOrganicNGPerM2 |

No esquema L0, massa seca animal total = `structuralDryG + reserveDryG`; `energyReserveJ` deriva da reserva material e não é massa adicional. `biomassDryG` e `nitrogenG` do patch excluem o banco de sementes, contabilizado separadamente. A idade de plantas não é estado L0: patches não representam organismos individuais ou coortes. O esquema é ilustrativo: mudança de semântica/ownership exige ADR; ajustes internos de nomes não.

## Experimentos biológicos de aceite

1. Solo seco versus úmido: mesma espécie/seed inicial cresce em ritmos diferentes por disponibilidade efetiva.
2. Luz plena versus sombra: crescimento limitado por energia efetiva e perfil de tolerância.
3. Herbívoro em área sem alimento: consumo zero; reservas caem; pode morrer.
4. Herbívoros competem por 10 unidades: quantidade total concedida ≤10, ordem reprodutível.
5. Aumento de herbívoros: reduz biomassa consumível e afeta a reprodução; predadores dependem da disponibilidade de presa.
6. Detritos depois da mortalidade aumentam, depois decaem por decomposição e transferem N contabilizado ao solo.
7. Remover predadores altera herbivoria e cobertura vegetal com atrasos de simulação, mas o sinal líquido não deve ser pré-determinado em todas as seeds.
8. Save/load reproduz os estados de sementes, adultos, detritos e reservas de animais no mesmo ambiente.

## Limites declarados

Sem fotossíntese bioquímica completa, ciclos biogeoquímicos globais de C/N (o inventário local de N continua obrigatório), carcaças persistentes, doenças, reprodução sexual/recombinação validadas, aprendizado neural, individualização microbiana, etologia complexa ou espécies fidedignamente calibradas. Existe um protótipo técnico de genoma e herança uniparental em Z5; ele não representa genética populacional calibrada nem altera a ecologia N3.

## Estado da implementação N3

Zoology está implementado como submódulo de `Game.Modules.Biosphere` por `IZoologyV1`; Botany, Ecology, Geosphere e Hydrosphere confirmam os estoques que possuem por suas próprias capabilities. A configuração headless `--n3-scenario` inicia 40 herbívoros e 4 predadores. Os casos quantitativos, a continuação após save/load, três seeds por 365 dias e o gate headless de desempenho foram aprovados; o registro detalhado está em [Zoology/N3](reports/ZOOLOGY_N3_2026-10-22.md).

Esse aceite é limitado ao L0 existente: herbívoro e predador são perfis funcionais, não espécies ou populações calibradas. Depois do checkpoint N3, os subgates técnicos Z1–Z5 acrescentaram genomas versionados e linhagem uniparental prototípica, desenvolvimento, solvers de locomoção/energia e geometria visual. Eles não fazem anatomia ou genética controlar a ecologia N3; seus limites estão no [plano modular de Zoology](specs/ZOOLOGY_MODULE_PLAN.md) e no [índice de evidências](reports/README.md).

## Carcaça animal persistente — lacuna atual

No runtime atual, a morte remove o agente vivo e transfere a matéria seca e o nitrogênio remanescentes para o detrito agregado da célula; a água corporal é devolvida à água superficial. Na predação, a porção consumida é contabilizada antes do depósito do restante. A transferência fecha os balanços, mas não deixa um corpo identificável no mundo: ainda não há `Carcass`, consumo posterior por carniceiros, estágios de decomposição, ossada ou histórico de descoberta.

Uma futura carcaça deve ser uma entidade persistente da Biosphere/Ecology, identificada e serializada, distinta do detrito já transferido. Um contrato de discussão suficiente contém `id`, `sourceAnimalId`, `profileId`, posição, tick de criação, massa seca comestível/resistente e N correspondente. O estágio visual pode ser derivado dos estoques e do tempo. Decomposição e consumo retiram apenas o remanescente, com transferências confirmadas pelos owners; a mesma matéria nunca pode permanecer na carcaça e no detrito. Isto é requisito futuro, não tipo implementado.

## Fechamento obrigatório do modelo L0

- **Representação:** um patch por célula/grupo vegetal; cobertura derivada de biomassa/perfil; raízes não têm estado explícito. Não individualizar folhas, raízes ou árvores nem trocar representação conforme câmera. Perfis são receitas imutáveis, não genomas; seed identifica variação determinística, não hereditariedade genética.
- **Crescimento e recursos:** calcular potencial, solicitar água/N em conjunto, limitar crescimento pelas concessões e confirmar consumo final. N não incorporado é devolvido/não debitado na mesma transação; água transpirada pode ser perdida mesmo sem crescimento, se a regra declarar isso. Não reservar recursos eternamente nem consumir todo N solicitado quando outro fator zerar crescimento.
- **Matéria e energia animal:** manter massa estrutural e reserva material separadas conforme o esquema acima. `energyReserveJ` é derivada de reserva material por densidade energética declarada ou vinculada a ela; não somar os dois como estoques materiais independentes. Manutenção consome reserva material com saída respiratória; o L0 não cataboliza tecido estrutural e aplica morte por déficit sustentado conforme os limites quantitativos definidos no documento 20.
- **N em toda a cadeia:** adicionar `animalNitrogenG`, N do banco de sementes e `soilOrganicNGPerM2`. N é composição contida na matéria seca, não massa extra a somar a ela. Herbivoria, predação, excreção, morte e mineralização particionam massa e N separadamente; respiração de carbono não elimina N automaticamente. Usar proporções fixas por perfil no L0 e devolver excesso de N por excreção contabilizada.
- **Reprodução:** sementes retiram massa/N do patch; germinação transfere do banco de sementes ao patch. Filhote retira massa, N, água corporal e reserva do progenitor na mesma transação; custo metabólico tem destino explícito. Maturidade e limiar de reserva são condições, não fontes de matéria. Reprodução assexuada funcional é abstração L0 declarada; não exige sexos, pares ou genética. Falha na criação não debita o progenitor.
- **Predação e morte:** presa capturada apenas uma vez; parte assimilada, parte não assimilada e restos vão aos destinos declarados. Cadáver recebe só o remanescente, nunca a massa inteira já consumida. Entidade morta não age nem reproduz após o commit.
- **Decomposição:** atividade agregada é coeficiente de processo, sem biomassa microbiana separada no L0. Particionar detritos em matéria orgânica edáfica e perda respiratória; particionar seu N em N orgânico do solo e N mineral. A soma dos destinos de cada grandeza, incluindo perdas declaradas, iguala a retirada dentro da tolerância. Soil aplica mineralização lenta do seu próprio N orgânico; Ecology não decompõe o mesmo estoque novamente. Sem nitrificação, desnitrificação ou imobilização microbiana explícitas nesta fase.
- **Água biológica:** plantas não precisam de reservatório hídrico persistente L0: captação/transpiração são passagem contabilizada solo → fronteira. Animais mantêm água corporal e bebem de superfície; desidratação/excreção e água de morte, inclusive por predação, retornam à superfície via Hydrosphere. Água do alimento pode ser omitida declaradamente no L0. Interceptação vegetal inicial é fração evaporada no intervalo, sem reservatório de copa; água não evaporada alcança a superfície.
- **Comportamento:** percepção local limitada, escolha por utilidade ou estados e deslocamento por vizinhança com desempate estável. Distância é em metros e velocidade em m/s; limitar deslocamento pelo tempo transcorrido e pelas barreiras. Sem planejamento global, memória cognitiva, rebanhos complexos ou evolução adaptativa no MVP.

Essas regras fecham abstrações contábeis; não certificam fidelidade fisiológica. Ausência de estoque significa processo omitido/de fronteira declarado, nunca quantidade criada sem origem.

### Composição e parâmetros

N pode ser armazenado ou derivado de composição fixa versionada, desde que toda transferência conserve seu inventário e que mudar perfil não o crie ou elimine. No L0, o N corporal é derivado da massa estrutural (`qN×M`); a reserva energética não contém N. Revisões futuras precisam declarar composição e conversão sem duplicar estoques. Cobertura, demanda hídrica de cada perfil e prontidão reprodutiva animal são diagnósticos derivados, sem estado redundante obrigatório.
