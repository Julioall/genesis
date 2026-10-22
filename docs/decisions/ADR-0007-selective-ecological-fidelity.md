# ADR-0007 — fidelidade ecológica seletiva e consequência observável

- **Status:** aceita como critério de produto e implementação incremental
- **Data:** 22/10/2026

## Contexto

Genesis quer ser um simulador de vida natural que o jogador consiga observar e explicar. Reproduzir cada detalhe da biologia, química ou física não é o objetivo. O valor de um processo vem das novas consequências, escolhas ou histórias verificáveis que oferece, considerando o custo de implementação, manutenção, renderização e simulação.

O baseline não é mais somente flora: N0–N3 foram implementados e validados no escopo L0. O checkout atual usa 64 × 64 células de 100 m². O commit de referência da auditoria `c6aecbc` usava uma grade experimental de 256 × 256; a integração posterior `dd2cf47` voltou a 64 × 64. A rubrica visual N4 permanece aberta. A morte animal hoje transfere os restos a detritos agregados; não há corpo persistente.

## Decisão

Antes de ampliar um processo, responder: (1) que experiência observável muda; (2) quais estados e transferências causais são necessários; (3) quem possui cada estado/estoque; (4) se uma agregação ou proxy preserva a experiência; (5) qual fixture e métrica permitem comparar; e (6) qual custo ou limiar manda parar ou simplificar.

Classificar o detalhe pela função para o produto:

| Classe | Regra de decisão | Aplicação ao Genesis |
|---|---|---|
| **E — essencial** | Sem isso, a cadeia causal ou a demonstração-alvo falha | Água/solo, patches vegetais e agentes animais discretos; para a experiência completa, carcaça identificável e persistente |
| **A — agregar/simplificar** | Manter causa e efeito com estado mais barato | Decomposição por taxa ambiental; plantas por patches; água/N por estoques e ledger; atributos visuais podem derivar do snapshot |
| **O — opcional após evidência** | Fazer só depois que um experimento mostrar comportamento novo ou ganho medido | Coortes vegetais, carniceiro especializado, memória animal, herança de traços, redes neurais e resolução espacial híbrida |
| **F — fora do horizonte inicial** | Não criar código, classes vazias ou custo sem requisito aprovado | Microrganismos individuais, órgãos/folhas individuais, dinâmica atmosférica 3D, fluido volumétrico e genoma molecular completo |

Agregação não pode apagar diferenças que mudem as escolhas dos agentes ou o resultado do habitat. Perfis funcionais podem compartilhar um algoritmo se parâmetros versionados criarem respostas contrastantes, testáveis e explicáveis.

## Observabilidade e integridade

Sistemas essenciais precisam publicar estado suficiente para inspeção e, quando um acontecimento for relevante, sua transição, local, tick, causa e transferências. O renderer deriva a aparência desse estado sem mutá-lo. Registrar acontecimentos importantes e contadores; não criar histórico obrigatório por folha, micróbio ou frame.

Nenhuma nova complexidade pode duplicar água, matéria seca ou N. Toda mudança em entidades persistidas exige owner claro, save/load/versionamento e teste de não duplicação. Para carcaças, a morte cria somente o remanescente após consumo; decomposição e necrófagos retiram estoques uma vez; detrito/solo recebem transferências confirmadas pelos owners. O contrato conceitual e os limites estão em [MVP biológico](../09_BIOLOGICAL_MVP.md); este ADR não cria uma implementação C#.

## Sequência e gates

- Preservar o roadmap N0–N4 e o baseline ecológico N3. Não substituir os marcos atuais por uma sequência concorrente.
- Fechar observabilidade ambiental, consistência de água visual/física e contraste de resposta vegetal antes de ampliar fidelidade biológica.
- Fazer carcaça e consumo posterior como fatia própria, após definir owner e ledger, com save/load, corpo persistente e teste de competição.
- Tratar Z1–Z5 como subgates técnicos em andamento, sem afirmar espécie calibrada, anatomia funcional ou seleção natural só porque existem genomas/meshes prototípicos.
- Manter controladores neurais fora do MVP. Qualquer piloto futuro compara inferência compartilhada com heurísticas em mesma arena, sensores, ações, seeds e orçamento. Treino offline, herança de pesos e aprendizado on-line são hipóteses distintas e precisam de gates próprios.

Para cada mudança, registrar experiência, estado mínimo, dependências, custo medido, fixture, captura/telemetria e decisão de avançar ou parar. Separar custo de tick/simulação do custo do renderer; indicar build, seed, cenário/commit, CPU, GPU, memória, resolução, tempo de amostra, P95/P99 e aceleração. Benchmarks locais não certificam outros hardwares.

## Consequências

- A cadeia observável orienta o escopo; a complexidade microscópica não é requisito por si só.
- A arquitetura mantém C#/.NET 10, MonoGame, núcleo headless determinístico, ownership por domínio e renderização sem autoridade científica.
- O gate N4 visual segue aberto; o custo local medido e uma captura bonita não substituem a rubrica visual, hidrológica e de habitat.
- O MVP Natural não se declara completo enquanto a experiência-alvo e seus gates ainda estiverem abertos.

## Revisão

Revisar este ADR quando uma fixture evidenciar que a representação agregada falha em criar a experiência esperada, quando medições mostrarem um gargalo, ou quando houver uma decisão explícita para ampliar o escopo. Manter propostas como propostas até haver implementação e evidência.
