# Revisão dos documentos — 21/10/2026

> **Registro histórico:** esta revisão foi feita antes de o runtime C# ser integrado a esta branch. As afirmações abaixo sobre ausência de código, solução e testes descrevem aquele estado em 21/10/2026, não o checkout atual. O estado presente está em [README](../README.md), [roadmap](12_ROADMAP.md) e no [índice de evidências](reports/README.md).

## Revisão 2.8 — direção visual no MonoGame

O usuário retirou a exigência de 2D/top-down/pixel art e pediu uma direção visual com base em duas imagens de referência. Em seguida, confirmou que o projeto deve permanecer na stack MonoGame para que o trabalho seja produzido por IA. Os requisitos visuais e a comparação de técnicas dentro do MonoGame estão em [22_VISUAL_DIRECTION_AND_TECHNOLOGY.md](22_VISUAL_DIRECTION_AND_TECHNOLOGY.md). As imagens recebidas foram copiadas para `docs/references/visual-direction/` para manter os links documentais portáveis.

Foram atualizados escopo, GDD, instruções de entrada, template operacional, prompt mestre, geração procedural, roadmap, gate de plataforma/web, testes, README, índices e registros visuais. A ADR-0004 registra MonoGame como stack fixa e as técnicas visuais que seguem em avaliação; ADR-0002 foi atualizada para manter vigente a escolha de MonoGame e marcar como históricas as restrições antigas de câmera e estilo. Contratos científicos, parâmetros L0 e escopo funcional N0–N3 foram preservados. Os benchmarks MonoGame permanecem como evidência do protótipo e foram qualificados como não comparativos.

**Resultado desta revisão:** orientação documental alinhada; MonoGame confirmado como stack e nenhuma migração iniciada. Não foram alterados código, modelos quantitativos ou parâmetros físicos/biológicos. A comparação de técnicas é qualitativa e documental; a decisão visual de N4 permanece condicionada ao protótipo e à medição da cena dentro do MonoGame. Não foram executados build ou testes de código nesta revisão documental.

O repositório já continha alterações locais antes da revisão. Elas foram preservadas; mudanças preexistentes em código não fazem parte desta entrega documental. Os registros de protótipos locais foram identificados como tal, e o README identifica os comandos e o cenário v17 como parte da implementação local, ausente nesta branch documental.

Validação para publicação: 43 arquivos Markdown inspecionados, 140 links locais conferidos sem destinos ausentes e diff sem erros de whitespace. A checagem confirmou MonoGame como stack do cliente, núcleo C# independente e geração visual procedural. Os benchmarks históricos não foram reexecutados.

## Revisão anterior 2.6 e histórico

As seções restantes registram conclusões das revisões anteriores. Trechos sobre cliente 2D/pixel art, disponibilidade de código, renderer e benchmarks descrevem o pacote e o estado observados então; a revisão 2.8 acima é a referência atual para a direção visual e tecnológica.

## Parecer histórico

A proposta é coerente como monólito modular e simulador ecológico L0: cinco domínios, recursos finitos, fluxos auditáveis, determinismo e separação da apresentação. O escopo original tinha riscos de infraestrutura antecipada e lacunas de contabilidade. Os documentos foram corrigidos para orientar uma implementação incremental sem retirar vegetação, fauna, predação ou decomposição.

**Resultado:** especificação mais consistente para orientar a etapa seguinte; viabilidade, desempenho, estabilidade numérica e fidelidade biológica continuam não demonstrados. Não há fontes C#, solution ou testes executáveis no inventário local. O exemplo de contratos é pseudocontrato C# ilustrativo, não uma API implementada ou compilada.

## Achados e correções

| Achado | Consequência | Correção documental |
|---|---|---|
| Prompt operacional manda executar tudo | Risco de implementar durante revisão | Aviso explícito no README, entrada, prompt e template de instruções |
| Árvore extensa e capacidades futuras antecipadas | Scaffold e abstrações sem uso | Criação incremental; sem interfaces de neve/aquíferos no L0 |
| Core compõe mas não pode referenciar módulos | Direção de composição ambígua | Executáveis compõem implementações; Core recebe contratos |
| Streaming, LOD e múltiplas representações muito cedo | Custo de persistência e causalidade | Mapa finito simulado integralmente; chunks apenas visuais inicialmente |
| N animal/seminal e N orgânico do solo ausentes no esquema | Balanço de N incompleto | Estados ou composição derivada explícitos em toda a cadeia |
| Reserva em J sem vínculo material | Energia e biomassa podem ser criadas/duplicadas | Reserva material vinculada a densidade energética; metabolismo com destino |
| Reprodução condicionada apenas a limiar | Filhotes/sementes gratuitos | Transferência de massa, N, água e reserva; falha não debita origem |
| Concessões isoladas de água/N | Retirada desperdiçada ou crescimento sem recurso | Reservar em conjunto, finalizar uso e devolver excedentes |
| Predação, cadáver e decomposição sem partição precisa | Duplicação de massa e nutrientes | Captura única, restos reais e partições independentes de massa/N |
| Ledger cita estoque hídrico vegetal não definido | Reservatório oculto | Plantas como passagem solo → fronteira; água corporal animal explícita |
| Atomicidade e save intermediário indefinidos | Estado parcial e complexidade de rollback | Validação conjunta, commit sequencial e checkpoint no fim do tick |
| Taxas em cadências diferentes | Chuva/recurso aplicado mais de uma vez | Janelas de integração, agenda persistida e teste específico |
| `F0/F1` diverge de `L0/L1`; nomes de cobertura diferem | Terminologia e contratos inconsistentes | Padronização e exemplo de manifesto corrigido |
| Regras de codificação pouco operacionais | Implementação inconsistente | Documento 19, sem criar configurações ou código |
| Spike web e visual sem orçamento | Fundação pode virar segundo produto | Spike limitado e visual mínimo antes de otimizações |

## O que permanece obrigatório

Ambiente causal dos cinco domínios, dois habitats, dois grupos vegetais, herbívoro e predador, sementes/reprodução com custo, morte, reciclagem de nutrientes, inspeção causal, geração procedural, save/load e testes de conservação. Extinção é resultado possível; não estabilizar populações por respawn ou teto técnico oculto.

## Limites e decisões ainda experimentais

Na revisão 2.3, fórmulas, tolerâncias e metas ainda constavam como pendência. A consolidação 2.4 preencheu o contrato quantitativo inicial em `docs/20_L0_MODEL_AND_BENCHMARKS.md`: são heurísticas de engenharia L0, sem calibração de campo, e metas de desempenho ainda não medidas. A validade dos coeficientes, a sensibilidade entre seeds e a coexistência populacional não estão demonstradas. Reprodução funcional e decomposição agregada continuam abstrações deliberadas L0.

Padrões aceitos: monólito modular, owner único, contratos pequenos, funções puras, composição explícita e estados simples de fauna. Não é necessário criar framework de plugins, ECS, event sourcing, infraestrutura distribuída ou todas as DLLs para iniciar. O ledger audita o recorte aberto do modelo; não representa fechamento global de carbono/energia da Terra.

## Validação desta revisão

Leitura dos documentos locais, inspeção do inventário, conferência de referências Markdown locais, consistência dos nomes/termos e atualização do manifesto. Consulta pontual a fontes oficiais MonoGame e FAO e busca bibliográfica no SciSpace para modelos hidrológicos, uso de luz e decomposição; fontes, links e limites estão registrados no documento 15. Não foi feita auditoria remota do legado. A conferência bibliográfica foi pontual, com fontes e limites registrados no documento 15; fórmulas e coeficientes continuam sujeitos à conferência antes de uso científico.

Nenhum prompt principal executado, arquitetura implementada, SDK instalado, build ou teste C# executado. O escopo desta revisão foi documental; o pacote está pronto para orientar a implementação incremental.

## Segunda leitura — consolidação 2.3

**Parecer atual:** os documentos formam uma boa base de especificação para preparar uma implementação incremental. O aceite aqui é de coerência documental; não comprova executabilidade, estabilidade ecológica ou desempenho.

A revisão 2.2 acrescentou ressalvas sem substituir alguns trechos anteriores. Nesta leitura, as correções foram incorporadas diretamente ao escopo, à arquitetura descrita, aos exemplos, ao scheduler e às tabelas do roadmap:

- Grade única, patches vegetais e mapa finito agora aparecem também no texto principal; LOD, erosão e geração infinita não são exigências iniciais.
- Composição sai da descrição do Core e permanece nos executáveis; N0 não exige desktop placeholder nem todos os assemblies. Save começa em N1.
- Perfil/seed deixam de ser chamados de genótipo; a tabela de estado inclui N de sementes, animais e solo orgânico e separa massa estrutural de reserva material.
- O exemplo de manifesto requer matéria orgânica para o ciclo completo; a fixture hídrica continua independente dessa capacidade.
- Balanço perde o termo genérico de criação; transformações exigem origem/destino. Rejeição esperada e falha inesperada têm comportamentos distintos, sem prometer rollback de todo tick.
- Reservas provisórias, cancelamento de pedidos de presas capturadas, atuação de recém-nascidos e sombra calculada por Botany ficam explícitos.
- Exemplos de contratos e save passam a refletir esses limites. Testes são distribuídos por marco para evitar antecipar N3/N4 em N0.
- Prompt referencia os documentos novos e preserva revisões locais; leitura do prompt segue sem autorizar execução.

Não é necessário acrescentar mais domínios ou camadas agora. A pendência de fichas quantitativas foi fechada documentalmente na atualização abaixo; calibração empírica e medição de desempenho continuam trabalho futuro, antes de qualquer alegação de fidelidade ou velocidade. Essa calibração não exige ampliar o escopo do L0.


## Terceira consolidação — quantitativos L0 (21/10/2026)

A pendência de equações, parâmetros, tolerâncias e metas do primeiro cenário foi fechada documentalmente em [20_L0_MODEL_AND_BENCHMARKS.md](20_L0_MODEL_AND_BENCHMARKS.md). O documento fixa um cenário finito 64×64, ciclos ambientais, dois grupos de plantas, dois perfis animais, decomposição, livros de água/matéria/N, testes numéricos e metas N0–N4. Também corrige as divergências de unidade, cadência e limite de chuva encontradas na revisão.

As curvas e coeficientes são hipóteses de jogo, não dados empíricos de espécies ou localidade. As referências sustentam formas de processo, não os valores selecionados. As metas de desempenho são objetivos a medir em runtime futuro; nenhuma foi executada nesta revisão. A validação continua documental: prompt principal não executado, solução, arquitetura e código não criados.


## Revisão crítica do contrato quantitativo — consolidação 2.5

O cenário passou a `temperate-valley-l0-v2` após correções que alteram resultados: água superficial disponível para infiltração, débito explícito de evaporação do solo, partição hídrica correta, cadência e colonização por sementes, senescência sequencial, alcance da herbivoria e movimento sem ultrapassar o alvo, persistência da cota de captura, idade/cooldown em uma única cadência, recuperação do contador de inanição e nascimento por transferência proporcional de estrutura/reserva. Também foram definidos o denominador do erro anual e a carga do benchmark desktop. As autorias das referências de eficiência de radiação e LPJmL foram corrigidas para Kiniry et al. e Lutz et al.

A revisão anterior não havia identificado essas inconsistências. Conferências aritméticas pontuais de água e nascimento apoiam as correções, mas não substituem testes executáveis dos processos. Permanecem por demonstrar dinâmica ecológica, reprodução efetiva e desempenho. Em particular, alimentação concentrada do predador, prioridade de crescimento e acesso apenas local à água exigem experimento integrado. O parecer é de base documental revisada para implementação incremental, não de modelo validado. Nenhum runtime ou arquitetura foi implementado.

## Consolidação 2.6 — organização e coerência documental (21/10/2026)

A leitura cruzada ajustou o marco N1 para descrever água superficial em poças/depressões e saída de borda, conforme o escopo físico e o contrato L0 v2; rios persistentes seguem adiados. O identificador do cenário no roadmap foi alinhado a `temperate-valley-l0-v2`. O contrato quantitativo não mudou. Os links Markdown locais foram conferidos e resolvem para arquivos existentes; os caminhos documentais foram mantidos porque já são referenciados pelo prompt e pelos índices. A situação da plataforma web também foi revalidada em 21/10/2026 nas fontes oficiais listadas em [14_WEB_GATE.md](14_WEB_GATE.md): a documentação continua sem alvo de navegador e a issue #9515 permanece aberta.
