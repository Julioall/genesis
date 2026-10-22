# Systema Naturae — Dossiê científico e técnico de criaturas procedurais



---

> **Fonte:** `00_COMECE_AQUI.md`

# Gênesis / Systema Naturae — Pesquisa de criaturas procedurais (v1.0)

**Natureza do documento:** anexo de pesquisa, arquitetura e especificação, não implementação concluída.

**Repositório de destino:** https://github.com/Julioall/genesis
**Repositórios de referência:**
- https://github.com/idlerunner00/procedural-pixel-creatures
- https://github.com/rendragonnn/evolve-creature
- https://github.com/EvolutionGym/evogym
- https://www.framsticks.com/

Este pacote complementa o pacote Systema Naturae v2.1. Respeita a decisão de que `ScientificFoundation` é documentação, não um assembly C#. Nunca usar o nome comercial do jogo em projetos, namespaces ou pastas de código: somente `Game.*`.

## Ordem de leitura

1. `01_PARECER_CIENTIFICO.md` — o que a literatura sustenta e as limitações.
2. `02_PROJETOS_E_BIBLIOGRAFIA.md` — código aberto, licenças e referências científicas.
3. `docs/science/Biology/Morphogenesis-and-Artificial-Life.md` — princípios científicos documentais.
4. `docs/architecture/CREATURE_ARCHITECTURE.md` — propriedade de estado e dependências.
5. `docs/specs/ZOO-100-BODYPLANS-GENOMES.md` — espécies, genes e desenvolvimento.
6. `docs/specs/ZOO-110-ANATOMY-LOCOMOTION.md` — anatomia e animação.
7. `docs/specs/ZOO-120-PHYSIOLOGY-ECOLOGY.md` — ciclo de vida, metabolismo e cadeias ecológicas.
8. `docs/specs/ZOO-130-RENDERING.md` — geração visual agnóstica ao renderizador.
9. `docs/specs/ZOO-140-EVOLUTION.md` — herança, seleção e especiação futura.
10. `docs/tests/ZOO-ACCEPTANCE.md` — cenários de validação e desempenho.
11. `docs/decisions/ADR-0002-CREATURES.md` — decisão registrada.
12. `03_PROMPT_CODEX.md` — tarefa incremental para o Codex.

## Decisões que devem prevalecer

- A fisiologia, a anatomia funcional, o genoma e o estado ecológico pertencem a `Game.Modules.Biosphere/Zoology` (ou namespaces de mesmo propósito); representações puramente gráficas pertencem a `Game.Procedural` e `Game.Rendering`.
- Contratos versionados e dados de observação ficam em `Game.Contracts`.
- Um organismo controlado pelo jogador usa a **mesma biologia** dos demais animais; muda a origem das intenções/comandos.
- Representação visual **não fica presa a 2D, pixel art ou 3D**; o formato final dependerá de avaliação estética/técnica, mantendo geração e simulação independentes.
- O estágio inicial gera **variação plausível dentro de um plano corporal**; mudanças radicais de anatomia e especiação emergente são pesquisa futura.
- A execução da simulação não pode depender de câmera, FPS ou da presença do renderizador.
- Não implementar simultaneamente todo o escopo descrito: seguir gates, explicitar simplificações, preservar rastreabilidade científica e testes.

## Status de evidências

Esta documentação baseia-se na leitura de metadados/artigos científicos acessíveis e de arquivos README/ARCHITECTURE/LICENSE dos repositórios públicos. Os projetos de referência **não foram compilados nem submetidos a benchmark local** nesta pesquisa. Números alegados por esses projetos são declarações de seus mantenedores, não validações independentes nossas. O pacote não altera o repositório do usuário.


---

> **Fonte:** `01_PARECER_CIENTIFICO.md`

# Parecer científico e de engenharia — criaturas naturais procedurais

## Pergunta central

É possível gerar animais procedurais que tenham aparência orgânica e também participem de um ecossistema dinâmico e biologicamente coerente? **Sim, como um modelo computacional com abstrações explícitas.** Não há evidência de que um único algoritmo gere automaticamente anatomia, fisiologia, comportamento, evolução e diversidade zoológica realistas para todos os filos animais.

## O que a literatura sustenta

| Área | Evidência selecionada | Adequação | Limitação |
|---|---|---|---|
| Diversidade de corpos virtuais | Lai et al. (2021), *Virtual Creature Morphology — A Review* | Alta para taxonomia de técnicas | Revisão de geração virtual, não certificação biológica |
| Corpo e controlador evoluindo juntos | Sims (1994), *Evolving Virtual Creatures* | Alta para viabilidade de codificações de corpos e comportamento | Um objetivo artificial de fitness pode produzir soluções pouco biológicas |
| Desenvolvimento codificado indiretamente | Framsticks f4, codificação indireta | Alta como precedente implementacional | É um sistema artificial, não embriologia fiel |
| Locomoção quadrúpede | Coros et al. (2011), *Locomotion Skills for Simulated Quadrupeds* | Alta para organização de marcha/controle | Integração física robusta é computacionalmente custosa |
| Cinemática inversa | Aristidou & Lasenby (2011), FABRIK | Alta para posicionamento de articulações | IK não substitui forças, equilíbrio ou anatomia real |
| Padrões de pelagem | Turing (1952); Murray (1981) | Alta para padrões emergentes de reação-difusão | Igualdade visual com manchas/listras não prova via molecular real de cada espécie |
| Animais autônomos | Tu & Terzopoulos (1994), *Artificial Fishes* | Alta para organização de agente corporificado | Simulação de peixe específica, não generalizável sem adaptação |
| Movimento coletivo | Reynolds (1987), *Flocks, Herds, and Schools* | Alta como modelo simples de comportamento de grupo | Não substitui etologia por espécie |
| Orçamento energético | Kooijman & Lika (2014), sobre Dynamic Energy Budget | Alta para base de manutenção/crescimento/reprodução | Exige calibração se pretendermos fidelidade empírica |

## Decisões derivadas (propostas de engenharia, não resultados experimentais)

1. Organismos representados por *SpeciesDefinition* (restrições hereditárias), *Genome*, *BodyPlan*, *DevelopedMorphology*, *PhysiologyState*, *BehaviorState* e *LifeHistory*.
2. `Genome -> Development -> Morphology` é um processo determinado por versão, seed e condições declaradas. Não regenerar anatomia a cada quadro.
3. Separar fenótipo anatômico, fenótipo visual e estado vital; pelagem não define saúde e morte não deve ser inferida da animação.
4. Escolher níveis de fidelidade: L0 regras ecológicas simples; L1 reprodução/crescimento/locomoção/recursos; L2 física articulada e desenvolvimento complexo.
5. Genética inicial com genes tipados, limites por família, herança e mutação; nunca alegar especiação biológica completa com pequenas alterações cosméticas.
6. Evitar treino neural por indivíduo na primeira entrega. Um controlador compartilhado parametrizado com variação individual atende caça, fuga e pastoreio simples.
7. Criaturas híbridas irrestritas só em laboratório de pesquisa, fora dos cenários zoológicos naturais, até existirem restrições anatômicas e reprodutivas verificáveis.

## Critérios de honestidade científica

- **Biologicamente inspirado**: efeitos coerentes em um mundo artificial.
- **Biologicamente plausível**: restrições e relações testadas com literatura e cenários controlados.
- **Calibrado/validado empiricamente**: parâmetros comparados a dados reais e resultados de validação reproduzíveis; não presumir.

As metas de FPS/memória e qualidade estética dependem do hardware, escala do mundo, nível de detalhe, plataforma e protótipo real; não declarar valores como garantidos sem medir.


---

> **Fonte:** `02_PROJETOS_E_BIBLIOGRAFIA.md`

# Referências científicas, projetos e licenciamento

## Projetos open source estudados

### Procedural Pixel Creatures
- URL: https://github.com/idlerunner00/procedural-pixel-creatures
- Stack: C#/.NET + integração Godot.
- Arquitetura documentada: `Core` independente, `Runtime` Godot, quatro RNG streams (anatomy, color, pattern, motion), body plans por família, genes versionados, rig e rasterização, spritesheets e testes do núcleo.
- Reaproveitamento técnico sugerido: analisar `docs/ARCHITECTURE.md`, `addons/procedural_creatures/Core`, testes, contratos de extensão, versionamento, separação CPU/GPU. O projeto foi feito para pixel art; aproveitar conceitos, **não adotar obrigatoriamente a saída visual**.
- Licença do repositório: MIT (ver `LICENSE`); inclui notificações de terceiros para kernels fdlibm e RNG PCG em `THIRD_PARTY_NOTICES.md`. Copiar só após revisar os arquivos específicos e preservar licenças e avisos.
- Mantenedor afirma testes/benchmarks; **não reproduzidos nesta pesquisa**.

### Evolve Creature
- URL: https://github.com/rendragonnn/evolve-creature
- Stack: Python, Pymunk e NEAT; planos bípede, quadrúpede, verme e aranha; corpo rígido e juntas motorizadas.
- Métrica central conforme README: deslocamento horizontal, com penalidades para quedas e retrocesso.
- Utilidade: laboratório de co-design corpo-controle, parametrização de juntas e busca neuroevolutiva. **Não é uma simulação zoológica de ecologia, especiação ou metabolismo**, nem uma solução MonoGame pronta.
- Licença indicada no README: MIT; verificar arquivo de licença da revisão antes de reutilizar código.

### Framsticks
- URLs: https://www.framsticks.com/ ; https://www.framsticks.com/node/294
- Sistema artificial de corpo/cérebro/genes, inclusive codificação indireta f4 por desenvolvimento de células; útil como inspiração de codificações evolutivas e ensaios.
- Verificar condições de licença/distribuição do componente específico antes de incorporar código. Não presumir licença livre por existir documentação pública.

### Evolution Gym
- URL: https://github.com/EvolutionGym/evogym
- Otimização conjunta de morfologia e controle de robôs macios, física 2D e 32 tarefas de benchmark documentadas.
- Aplicação: ensaios de pesquisa do desenvolvimento e locomoção; não é gerador zoológico de mamíferos.
- Licença: MIT conforme LICENSE do repositório; revisar dependências de terceiros.

## Artigos fundamentais — links estáveis

1. Lai, G.; Leymarie, F. F.; Latham, W.; Arita, T.; Suzuki, R. (2021). *Virtual Creature Morphology — A Review*. Computer Graphics Forum. https://doi.org/10.1111/cgf.142661
2. Sims, K. (1994). *Evolving Virtual Creatures*. SIGGRAPH. https://doi.org/10.1145/192161.192167
3. Tu, X.; Terzopoulos, D. (1994). *Artificial Fishes: Physics, Locomotion, Perception, Behavior*. SIGGRAPH. https://doi.org/10.1145/192161.192170
4. Coros, S.; Karpathy, A.; Jones, B.; Reveret, L.; van de Panne, M. (2011). *Locomotion Skills for Simulated Quadrupeds*. ACM TOG. https://doi.org/10.1145/1964921.1964954
5. Aristidou, A.; Lasenby, J. (2011). *FABRIK: A Fast, Iterative Solver for the Inverse Kinematics Problem*. Graphical Models. https://doi.org/10.1016/j.gmod.2011.05.003
6. Turing, A. M. (1952; republicado em 1990). *The Chemical Basis of Morphogenesis*. Bulletin of Mathematical Biology. https://doi.org/10.1007/BF02459572
7. Murray, J. D. (1981). *A Pre-pattern Formation Mechanism for Animal Coat Markings*. Journal of Theoretical Biology. https://doi.org/10.1016/0022-5193(81)90334-9
8. Murray, J. D. (1981). *A Model for Generating Aspects of Zebra and Other Mammalian Coat Patterns*. Journal of Theoretical Biology. https://doi.org/10.1016/0022-5193(81)90109-0
9. Reynolds, C. W. (1987). *Flocks, Herds, and Schools: A Distributed Behavioral Model*. SIGGRAPH. https://doi.org/10.1145/37401.37406
10. Kooijman, S. A. L. M.; Lika, K. (2014). *Resource Allocation to Reproduction in Animals*. Biological Reviews. https://doi.org/10.1111/brv.12082
11. Wampler, K.; Popović, Z.; Popović, J. (2014). *Generalizing Locomotion Style to New Animals with Inverse Optimal Regression*. ACM TOG. https://doi.org/10.1145/2601097.2601192
12. Arita, T. et al. (2016). *ALife Approach to Eco-Evo-Devo Using Evolution of Virtual Creatures*. Artificial Life and Robotics. https://doi.org/10.1007/s10015-016-0278-5

## Regras de reutilização

- Referência científica **não é código licenciável** nem demonstra implementação compatível com MonoGame.
- Se houver cópia de trecho de código, registrar arquivo, hash/revisão, licença e atribuição específica.
- Preferir implementação própria e testes independentes dos algoritmos publicados, especialmente quando o projeto-fonte usa outro runtime.
- Registros de pesquisas e estudos são evidências de possibilidades, não validação da nossa implementação.


---

> **Fonte:** `03_PROMPT_CODEX.md`

# Prompt de integração documental e implementação incremental — Gênesis

Trabalhe no repositório `https://github.com/Julioall/genesis`. Leia o pacote `Systema_Naturae_Criaturas_Procedurais_v1` completamente antes de implementar. Este pacote complementa e não substitui a arquitetura modular Systema Naturae.

**Objetivo:** estabelecer a pesquisa científica e especificações de zoologia procedural como base para criar animais naturais autônomos, potencialmente controláveis pelo jogador, cujo corpo, desenvolvimento, fisiologia, comportamento e aparência sejam derivados de contratos e parâmetros versionados.

### Restrições inegociáveis

- Usar `Game.*` em projetos e namespaces, jamais nome comercial do jogo.
- O fundamento científico fica em `docs/science/Biology`, não em um assembly `Game.ScientificFoundation`.
- As regras biológicas ficam sob o proprietário zoológico de `Game.Modules.Biosphere`. `Game.Procedural` trata geometria e padrões visuais; `Game.Rendering` usa adaptadores gráficos, nunca define fisiologia.
- Sem sprites ou modelos externos como requisito para gerar animais.
- A representação gráfica não é fixada a 2D, 2.5D ou 3D por esta especificação.
- Não reusar automaticamente o Godot/Pymunk/NEAT de projetos de referência nem copiar código sem auditoria de licença.
- Não implementar civilizações, profissões ou política.
- Controle do jogador e de IA devem compartilhar regras de organismo, respeitando consumo, reprodução, mortalidade e recursos finitos.

### Trabalho a realizar

1. Audite o estado do repo e os documentos existentes; identifique conflitos e preserve mudanças locais e a branch principal.
2. Integre documentos em caminhos apropriados de `docs/`, atualizando índice e ADR sem destruir decisões existentes.
3. Faça um mapa de migração/construção que distinga pesquisa, código já implementado, testes e funcionalidades pendentes.
4. Crie a fundação contratual para `SpeciesDefinition`, `BodyPlan`, `Genome`, `DevelopedBody`, `PhysiologyState`, `VisualRecipe`, `LocomotionIntent` e `LifeHistory`. Defina unidades e versões formalmente.
5. **Primeiro milestone de código:** núcleo headless C# com duas famílias (quadrúpede e peixe), geração determinística por seed e versionamento; teste de validade anatômica, mutação limitada e separação pigmento/anatomia.
6. Adicione um adaptador visual mínimo apenas após o núcleo funcionar; não bloqueie arquitetura em uma tecnologia de render ainda não validada.
7. **Milestone ecológico:** conectar um herbívoro, um predador e um peixe a água, solo/vegetação e detritos, usando transferências de recursos consolidadas por proprietários; simplifique fisiologia explicitamente quando necessário.
8. Rodar testes documentados e publicar evidências de tempos, memória e conservação; não atribuir aos nossos programas medições provenientes dos repositórios de referência.
9. Isolar pesquisa de coevolução corpo+controlador em futuro experimento; não inseri-la como requisito bloqueante do primeiro mundo natural.
10. Entregar relatório com decisões, testes realizados, testes não realizados, dificuldades, dependências e próximos marcos. Não fazer merge sem autorização.

### Condição de sucesso

Não basta exibir sprites de criaturas: deve existir pelo menos um organismo com anatomia procedural, fisiologia e efeitos demonstráveis sobre recursos do mundo, acompanhado de testes reproduzíveis.


---

> **Fonte:** `docs/architecture/CREATURE_ARCHITECTURE.md`

# Arquitetura de criaturas — domínio zoológico independente do renderizador

## Pastas propostas (não obrigar 1 assembly para cada pasta)

```text
src/
├── Game.Contracts/
│   ├── Biology/
│   ├── Zoology/
│   └── Ecology/
├── Game.Core/
├── Game.World/
├── Game.Modules.Biosphere/
│   ├── Zoology/
│   │   ├── Species/
│   │   ├── Genomes/
│   │   ├── Development/
│   │   ├── Anatomy/
│   │   ├── Physiology/
│   │   ├── LifeCycle/
│   │   ├── Reproduction/
│   │   └── Behavior/
│   └── Ecology/
├── Game.Procedural/
│   └── Creatures/
│       ├── Geometry/
│       ├── SurfacePatterns/
│       └── VisualRecipes/
├── Game.Rendering/
│   └── Creatures/
└── Game.Client.Desktop/

docs/
├── science/Biology/
├── specs/
├── architecture/
└── decisions/
```

`Game.Procedural` não interpreta metabolismo ou genes biológicos; gera materiais e representação visual a partir do fenótipo e do conjunto de características visuais. Se a morfogênese afetar capacidade funcional (ex.: número de patas), sua regra tem proprietário em `Zoology`, ainda que reutilize matemática genérica.

## Estados e proprietários

| Estado | Proprietário | Consumidores |
|---|---|---|
| Definição de linhagem, parâmetros hereditários e compatibilidade reprodutiva | Zoology/Species | Reproduction, Development, UI |
| Genoma, linhagem, parentesco | Zoology/Genomes | Development, Reproduction |
| Morfologia desenvolvida (comprimentos e topologia funcionais) | Zoology/Anatomy | Locomotion, Physiology, rendering adapter |
| Fase de vida, idade, massa, reservas, hidratação, saúde | Zoology/Physiology & LifeCycle | Ecology, Behavior, UI |
| Posição, habitat atual, interações espaciais | World/Spatial state | Zoology, Geosphere, Hydrosphere, UI |
| Água disponível, plantas e biomassa removível | Hidrosphere / Geosphere / Botany conforme tipo de reservatório | Zoology via contratos; transferência autorizada pelo proprietário |
| Intenção de movimento, alimentação ou reprodução | Zoology/Behavior ou PlayerCommands | Kernel resolver |
| Geometria e textura visual, cache, sombras e animação gráfica | Game.Procedural / Rendering | Cliente |

**Atenção:** um organismo controlado pelo jogador e outro por IA usam as mesmas regras metabólicas, de reprodução e de conservação de matéria. Só o produtor de intenções muda. Regras de controle humano/social permanecem fora do escopo natural.

## Contratos mínimos — pseudocódigo C# conceitual

```csharp
public readonly record struct SpeciesId(string Value);
public readonly record struct CreatureId(ulong Value);
public readonly record struct GenomeVersion(int Major, int Minor);

public sealed record SpeciesDefinition(
    SpeciesId Id,
    string BodyPlanId,
    string GeneticsSchemaId,
    string EcologicalProfileId);

public sealed record GenomeDocument(
    GenomeVersion Version,
    SpeciesId Species,
    ulong AnatomySeed,
    ulong PigmentSeed,
    IReadOnlyDictionary<string, double> Genes);

public interface ICreatureDevelopmentV1
{
    DevelopedBody Develop(SpeciesDefinition species, GenomeDocument genome,
                          DevelopmentContext environment);
}

public interface ICreatureVisualRecipeV1
{
    VisualRecipe Build(DevelopedBody body, PigmentProfile pigment,
                       ulong visualSeed);
}
```

Tipos auxiliares acima são placeholders de contrato a definir, não código que compila isoladamente. Units devem ser tipos de valor ou anotação formal: `LengthM`, `MassKg`, `EnergyJ`, `TimeS` etc. Não serializar interface, GPU buffer ou sprite no save.

## Processo causal por tick (proposta, a testar)

1. Ler snapshot físico/climático do intervalo anterior.
2. Atualizar necessidade metabólica/estado fisiológico do organismo (reservas, perda de água, temperatura aproximada).
3. Processar percepção e formular intenções (não transferir recursos ainda).
4. Resolver deslocamento, interação e acesso espacial ao recurso.
5. Solicitar consumo/transferência ao proprietário de água ou biomassa; consolidar sem gasto duplo.
6. Atualizar assimilação, crescimento, doença, reprodução e mortalidade após as transferências confirmadas.
7. Registrar mortes/detritos, liberar recursos para Ecology e preparar snapshot final.

Loops ecológicos (animal come planta; planta cresce com nutrientes produzidos por decomposição) são **causalmente cíclicos em tempo**, mas não envolvem importações circulares de assemblies.

## Persistência e determinismo

Salvar schema e versão de espécie, genoma, estado anatômico quando necessário, fisiologia dinâmica, estado do controlador, estado do RNG, IDs e genealogia. Na migração, não inferir que o mesmo genoma produzirá geometria idêntica após alterar o gerador: pin de versão e migração explícita. Câmera, FPS e distância de render não alteram a vida do animal.


---

> **Fonte:** `docs/decisions/ADR-0002-CREATURES.md`

# ADR-0002 — Criaturas são biologia funcional com representação procedural separada

- **Status:** proposta para aprovação/implementação no repositório novo.
- **Contexto:** o simulador precisa criar fauna coerente com ecologia, conservação de recursos e variabilidade visual, com suporte futuro a indivíduos controlados pelo jogador.

## Decisão

1. Criar zoologia como subdomínio de `Game.Modules.Biosphere`, com responsabilidades biológicas (espécie, genoma, desenvolvimento anatômico funcional, fisiologia, reprodução e comportamento).
2. Geradores de geometrias/padrões visuais pertencem a `Game.Procedural` e a saída final a `Game.Rendering`, sem dependência reversa.
3. Não adotar Godot, PixiJS, MonoGame nem sprites como exigência do núcleo científico. MonoGame pode ser um adaptador de apresentação, sujeito à validação multiplataforma.
4. Começar com planos corporais restritos e validados; crescimento, reprodução e mortalidade reais no modelo antes de habilitar morfologia irrestrita.
5. Controle de jogador, IA heurística e controle neural (futuro) produzem comandos do mesmo tipo e enfrentam as mesmas limitações do corpo.
6. `Game.ScientificFoundation` não é criado como projeto de código: a base teórica vive em `docs/science/`.
7. A primeira entrega mantém a simulação natural como prioridade; o laboratório evolutivo é opcional, isolado da operação normal.

## Consequências

**Positivas:** módulos testáveis, flexibilidade visual, reprodução auditável, compatibilidade com LOD e saves versionados.

**Custos/riscos:** modelagem de genomas e espécies, necessidade de controle de versões, custo da física animada e limitação do grau de realismo. Demandará testes de propriedades e calibração posterior.

## Alternativas rejeitadas nesta fase

- Redes neurais e simuladores de corpos completos por indivíduo desde o início (custo/complexidade altos).
- Genoma puramente estético sem influência funcional (contraria a simulação de vida).
- Uso de sprites/modelos artesanais como fonte de verdade do organismo.
- Misturar genes, estado atual do animal e características da câmera no mesmo objeto.
- Gerar novas espécies por combinação arbitrária de órgãos de famílias incompatíveis.


---

> **Fonte:** `docs/science/Biology/Morphogenesis-and-Artificial-Life.md`

# Science / Biology — morfogênese computacional e vida artificial

**Status:** fundamentação científica documental; não é uma biblioteca `Game.ScientificFoundation`.

## Conceitos

- **Espécie/população:** conceito ecológico e reprodutivo; no software, iniciar com uma linhagem parametrizada e regras de compatibilidade, não com uma definição biológica universal.
- **Genótipo:** instruções herdáveis e parâmetros de desenvolvimento. Não é sinônimo de sprite nem de estrutura corporal pronta.
- **Fenótipo:** manifestação corporal e fisiológica resultante de genoma, ontogenia e ambiente.
- **Ontogenia:** desenvolvimento de um indivíduo, incluindo crescimento, maturação e senescência.
- **Morfogênese:** formação do corpo e de padrões/estruturas durante o desenvolvimento.
- **Plasticidade fenotípica:** alterações do fenótipo por condições ambientais, sem que se tornem mutações hereditárias automaticamente.
- **Adaptação evolutiva:** mudança herdável em populações ao longo de gerações, sob pressões seletivas; não confundir com aprendizagem individual.
- **Controle corporificado:** a forma do corpo condiciona e restringe movimentos disponíveis, percepção e gasto energético.

## Estratégias computacionais

### Representação direta

Genes descrevem proporções (comprimento de cabeça, membros, tamanho de nadadeira, etc.), dentro de faixa anatômica. Simples e controlável, ideal inicialmente.

### Representação indireta / gramática de desenvolvimento

Genes codificam regras que produzem estruturas, por exemplo segmentação e padrões de ramificação. Potencial de inovação estrutural superior, mas torna mais difícil garantir viabilidade anatômica.

### Codificações gráficas

Grafos para segmentos, juntas, relações pai-filho e tecido; verificar conectividade, ausência de ciclos cinemáticos indevidos, centro de massa e validade de colisores.

### Reação-difusão de pigmentos

Dois campos químicos abstratos com reação e difusão sobre superfície parametrizada podem produzir padrões não uniformes. É um *modelo inspirado* em morfogênese, não prova de mecanismo molecular para cada espécie.

### Biomecânica simplificada

Caracterizar corpo por comprimento, massa, centro de massa, comprimento de membros, comprimento de passo, fase da marcha, custo locomotor e habilidade de atravessar habitats. Usar IK para pose, mas manter separado o sistema que determina deslocamento físico e consumo energético.

### Crescimento

Genes determinam potencial/limites; idade, ingestão, condições térmicas e saúde influenciam a expressão. Mudar escala visual não deve criar biomassa; massa e reserva energética são estados distintos.

## Princípios normativos

1. Não misturar variáveis observadas do ambiente com genes herdáveis.
2. Não permitir híbridos biologicamente implausíveis automaticamente apenas porque os dados possuem campos semelhantes.
3. Não prometer diversidade zoológica ilimitada sem ampliar os planos corporais.
4. O primeiro recorte pode usar regras de crescimento/energia simplificadas, com unidades e limites documentados.
5. Em qualquer etapa, distinguir plausibilidade visual, plausibilidade cinemática, coerência ecológica e validação quantitativa.
6. A representação gráfica poderá ser pixel art 2D, 2.5D ou 3D; a decisão de visual não muda a responsabilidade científica do módulo zoológico.

## Referências principais

Lai et al. 2021; Sims 1994; Tu & Terzopoulos 1994; Turing 1952; Murray 1981; Coros et al. 2011; Kooijman & Lika 2014. DOI e links em `02_PROJETOS_E_BIBLIOGRAFIA.md`.


---

> **Fonte:** `docs/specs/ZOO-100-BODYPLANS-GENOMES.md`

# SPEC-ZOO-100 — Plano corporal, genoma e desenvolvimento

**Status:** proposta; implementação pendente. **Escopo natural:** organismos não humanos e avatares biológicos controláveis.

## Entidades

- `SpeciesDefinition`: identidade de linhagem, habitat e compatibilidade reprodutiva; definição de parâmetros.
- `BodyPlan`: grafo anatômico de regras/segmentos essenciais, simetria e articulações permitidas.
- `Genome`: genes herdáveis tipados, schema, versão, seeds separadas e pedigree.
- `DevelopmentContext`: idade, condição nutricional, ambiente térmico e fatores de desenvolvimento especificados.
- `DevelopedBody`: estrutura funcional resultante — comprimentos em metros, massas por parte em kg, juntas, limites, mobilidade e sensores.
- `VisualRecipe`: dados derivados para pelagem, superfície, coloração e renderização.

## Etapas determinísticas

1. Selecionar a família/plano corporal, ex.: `vertebrate.quadruped`, `vertebrate.fish`, `vertebrate.bird`.
2. Amostrar genoma dentro de faixas biologicamente plausíveis **daquela família**, com seed estável.
3. Interpretar genes estruturais e de desenvolvimento.
4. Montar topologia anatômica (ossos/segmentos, simetria, articulações, limites e âncoras).
5. Calcular proporções e restrições (alcance dos membros, centro de massa, volume e colisores).
6. Validar anatomia: conectividade, simetria ou assimetria intencional, ângulos, auto-intersecções excessivas, membros fora de alcance e atributos negativos.
7. Produzir fenótipo biológico e `VisualRecipe` com seeds derivadas independentes.

## Política de linhagem

Na entrega inicial, reprodução somente entre genomas cujo `SpeciesDefinition` autorize compatibilidade. Não misturar arbitrariamente aves, quadrúpedes e peixes. Criaturas irrestritas poderão ser geradas em um laboratório separado e explicitamente fantástico/experimental.

## Genes e capacidade

Conjunto inicial exemplificativo (não é a lista única/definitiva):

| Gene | Unidade | Influência |
|---|---|---|
| `body.scale` | adimensional | escala corporal adulta dentro da família |
| `limb.relativeLength` | razão | alcance de passos e silhueta |
| `trunk.relativeLength` | razão | proporção tronco/cabeça |
| `coat.insulation` | escala declarada | isolamento térmico modelado; não criar calor do nada |
| `coat.pigmentBase` | cromaticidade | aspecto visual |
| `coat.patternFrequency` | por comprimento, quando aplicável | tamanho das manchas/faixas |
| `life.growthRate` | 1/dia ou parâmetro do modelo | velocidade de crescimento condicionada por alimento |

Genes não são reservas de energia, fome atual, estado de doença ou idade; esses são estados dinâmicos.

## Aceite

- Mil seeds geram anatomias válidas dentro de cada família testada.
- Mutação moderada preserva limites de família e produz diferenças observáveis.
- Mesma versão e seed resulta em identidade de genoma/estrutura/atributos derivados.
- Alterar a seed de pigmento não altera anatomia, custo locomotor ou fisiologia.
- Crescimento e reprodução não criam biomassa sem entrada declarada.


---

> **Fonte:** `docs/specs/ZOO-110-ANATOMY-LOCOMOTION.md`

# SPEC-ZOO-110 — Anatomia, cinemática e locomoção

## Princípios

Separar três níveis:

1. **Capacidade física/biológica**: corpo e fisiologia determinam potência, velocidade, fadiga e habitat.
2. **Movimento real do organismo**: integrador espacial/colisões confirma posição e gasto.
3. **Pose visual**: animação e IK apresentam o movimento sem alterar estado fisiológico.

## Planos corporais de pesquisa

| Família | Estrutura | Locomoção inicial | Evolução futura |
|---|---|---|---|
| Quadrúpede | Tronco, cabeça, pelve, quatro membros, cauda opcional | caminhada/trote parametrizados, IK 2 segmentos | galope, corrida, salto, equilíbrio com forças |
| Peixe | Cabeça/tronco, coluna flexível, nadadeiras | onda sinusoidal corporal e avanço em água | hidrodinâmica de nadadeiras e manobras avançadas |
| Ave | Tronco, dois membros posteriores, asas, cauda | caminhada e pose aérea visual para testes | voo com sustentação, gasto e aerodinâmica |
| Artrópode | Exoesqueleto segmentado, múltiplos membros | marcha tripod ou alternada | restrições articulares e tipos especializados |
| Serpente | Cadeia axial e cabeça | curvatura, ondulação e contato com terreno | locomoção dependente de atrito e superfície |

Não implementar todas simultaneamente.

## Métodos

- Quadrúpedes: fases cíclicas dos membros, planejamento de contatos, controle simples e FABRIK ou IK de dois segmentos para alinhar patas ao solo.
- Peixes: oscilação progressiva da cadeia axial, controlada por frequência/amplitude e posição, com deslocamento efetivo governado pelo módulo de física/habitat aquático.
- Aves: distinguir animação de asas de simulação de voo; a presença de animação não garante sustentação ou custo energético adequado.
- Vegetação/obstáculos: o corpo funcional define colisores; apenas o visual pode atravessar elementos decorativos quando não interferirem no mundo físico.
- Derivar velocidade funcional e custo locomotor de massa/biomecânica aproximada, não diretamente do tamanho do sprite.

## Interfaces propostas

```csharp
public interface ILocomotionPlannerV1
{
    LocomotionIntent Plan(DevelopedBody anatomy, MovementContext context);
}

public interface IPoseSolverV1
{
    Pose Solve(DevelopedBody anatomy, LocomotionIntent intent,
               GroundContacts contacts, Pose previousPose);
}
```

Tipos auxiliares são propostos; sua definição concreta e as unidades são tarefa de implementação. `Solve` modifica pose, não reservas corporais nem água.

## Citações

- Aristidou & Lasenby (2011), FABRIK, https://doi.org/10.1016/j.gmod.2011.05.003
- Coros et al. (2011), quadrupeds, https://doi.org/10.1145/1964921.1964954
- Tu & Terzopoulos (1994), fish, https://doi.org/10.1145/192161.192170
- Wampler et al. (2014), transferring locomotion style, https://doi.org/10.1145/2601097.2601192

## Aceite

- Comprimento de segmento preservado por IK dentro de tolerâncias.
- Patas apoiadas acompanham altura local e não deslizam excessivamente em velocidade estável, dentro de tolerância medida.
- Movimento lógico igual sob taxas de FPS distintas.
- Um peixe não atravessa terreno sólido e um quadrúpede não nada sem capacidade definida.
- Mesmo corpo sob níveis de fome/fadiga distintos pode produzir comportamentos/velocidades diferentes sem alterar sua forma permanentemente.


---

> **Fonte:** `docs/specs/ZOO-120-PHYSIOLOGY-ECOLOGY.md`

# SPEC-ZOO-120 — Fisiologia, recursos e integração ecológica

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

## Testes

1. Uma criatura que não encontra água pode reduzir suas reservas e morrer por causas verificáveis.
2. Herbívoro reduz exatamente o recurso vegetal consumido, considerando assimilação/rejeitos.
3. Predação transfere biomassa; a morte produz detritos contabilizados.
4. Temperatura altera gasto quando a capacidade é modelada; não altera genes.
5. Reprodução reduz recursos parentais; filhotes têm genealogia, massa e estágio inicial definidos.
6. Mesma seed e mesmas condições externas dão mesmo estado e eventos no mesmo ambiente numericamente controlado.


---

> **Fonte:** `docs/specs/ZOO-130-RENDERING.md`

# SPEC-ZOO-130 — Geometria, padrões e renderização procedural

## Decisão

O Gênesis não ficará preso a 2D, 2.5D, pixel art nem 3D. O renderer é substituível. A topologia anatômica funcional, materiais e receitas visuais são dados neutros à API gráfica. A decisão de aparência é independente da física e da fisiologia.

## Pipeline gráfico

`DevelopedBody + PigmentProfile + VisualSeed -> VisualRecipe -> Geometry/Surface -> AnimationPose -> RendererAdapter`.

- Geometria parametrizada: segmentos, cápsulas, elipsoides, volumes implícitos/SDF e superfícies conectadas a um rig sem topologia inválida.
- Textura procedural: cor base, gradientes, ruído orientado, manchas e listras por reação-difusão, padrões e características de material (pelos, escamas, penas em níveis de detalhe).
- Rig: juntas semânticas conectadas à anatomia; âncoras de pé, olho, asa, cauda etc.
- Projeção: o mesmo modelo anatômico pode ser rasterizado em pixels ou convertido em malha 3D, sujeito às limitações de cada backend.
- Cache: genoma/versionamento/renderizador/LOD nas chaves; se o genoma muda só em pigmento, não recomputar o esqueleto.

## Referência concreta de engenharia

`idlerunner00/procedural-pixel-creatures` (`docs/ARCHITECTURE.md`) documenta um pipeline independente: seeds separadas por anatomia/cor/padrão/movimento, construção única do modelo imutável, pose de animação, rasterização CPU, cache LRU e adaptador Godot. Esse é um **bom padrão de separação de responsabilidades**, mas a escolha de pixel art e as limitações de seu rasterizador não são exigências para o Gênesis.

## Reaction-Diffusion — uso recomendado

Aplicar campos abstratos de pigmentos sobre a superfície UV ou sobre domínio espacial parametrizado. A espécie define intervalos e máscaras; a seed produz variações. Conservar padrões aderidos ao corpo durante animação e crescimento. Evitar simplesmente amostrar ruído no espaço de câmera, pois as manchas devem acompanhar o animal.

Referências: Turing (1952) https://doi.org/10.1007/BF02459572 ; Murray (1981) https://doi.org/10.1016/0022-5193(81)90109-0 .

## LOD e execução

- `SimLOD`: nível de detalhe comportamental/ecológico (sempre conservar estoques relevantes).
- `RenderLOD`: frequência/quantidade de detalhes visuais, sem alterar resultados ecológicos.
- Render em lote, cache de materiais e texturas, geração assíncrona, budget e cancelamento, sem acessar API de GPU fora do thread correto.
- Não recarregar modelos a cada frame. Gerar no spawn, alteração de estágio perceptível ou mudança permanente de fenótipo.

## Testes

- Alterar FPS, câmera, LOD visual ou ocultar criatura não altera estado científico.
- Mesmo genoma + versão + seed + renderizador produz saída equivalente nos ambientes testados.
- Não usar sprites artesanais obrigatórios e não carregar dependências remotas no runtime.
- Medir custo por criatura visível, memória do cache, churn de alocação, render de 10/100/1000 animais; **metas a estabelecer após baseline**, não resultados prévios.


---

> **Fonte:** `docs/specs/ZOO-140-EVOLUTION.md`

# SPEC-ZOO-140 — Herança, adaptação, evolução e eventual especiação

## Distinções obrigatórias

- Crescimento: alteração de um organismo ao longo de sua vida.
- Plasticidade: efeito do ambiente sobre o fenótipo, não necessariamente herdável.
- Aprendizagem: alteração do estado/comportamento na vida de um indivíduo.
- Herança: transferência de variantes genéticas segundo regras reprodutivas.
- Seleção natural no modelo: diferenças na sobrevivência/reprodução correlacionadas com variantes herdáveis em um ambiente.
- Especiação: processo populacional complexo, não renomear espécies automaticamente quando um gene ultrapassa limiar arbitrário.

## Primeira implementação

1. Iniciar com espécies definidas por templates, não com geração livre de novas espécies.
2. Genótipos versionados com genes tipados, valores permitidos e grupos de ligação.
3. Reprodução com regras explícitas: compatibilidade, recombinação quando apropriada, mutações limitadas, genealogia.
4. Registrar desempenho ecológico real (idade reprodutiva, descendentes viáveis, energia, causas de morte), sem impor um fitness único externo como “percorrer mais distância”.
5. Sem seleção global para maximizar uma pontuação e sem criação artificial de populações para manter equilíbrio.

## Pesquisa futura — coevolução forma-controle

Isolar em experimentos reprodutíveis, inspirados por Sims, EvoGym e Evolve Creature. Testar corpo sem controlador otimizado, controlador com corpo fixo e coevolução completa, com controle de seed, número de indivíduos, duração, energia e limitações do simulador. A capacidade de andar rapidamente não equivale a aptidão ecológica.

**Especiação futura:** exige genealogia, compatibilidade reprodutiva, isolamento e análise de persistência populacional; caso o jogo crie uma nova classe por heurística, chamá-la de classe procedural, não necessariamente espécie evoluída biologicamente.

## Testes

- Descendência apresenta relação mensurável com pais quando herança está ativa.
- Mutação/desvio tem distribuição e limites testados, sem NaN.
- O custo de reprodução é debitado das reservas; indivíduos mortos não reproduzem.
- Repetir evolução por mesmas seeds preserva estado e genealogia no mesmo ambiente.
- As linhagens podem extinguir-se; cenários de seleção podem mostrar ganho/perda de variantes sem impor convergência obrigatória.
- A troca de versão do esquema genético rejeita dados incompatíveis ou aciona migração explícita.


---

> **Fonte:** `docs/tests/ZOO-ACCEPTANCE.md`

# Plano de validação — geração e vida de criaturas

**Regra:** os testes abaixo são critérios propostos; nenhum foi executado neste estudo.

## Gates

### Gate A — núcleo procedural puro

- Projeto compilável em C# sem MonoGame nem qualquer renderizador.
- Duas famílias funcionais: quadrúpede e peixe, com genomas versionados e anatomia válida para seeds variadas.
- Alteração de cor não altera anatomia ou capacidade fisiológica.
- Export/restore de genoma e espécie sem dependência de assets externos.

### Gate B — animação e adaptação ao terreno

- Quadrúpede caminha sobre declives sob limites de alcance; peixe usa estado de profundidade e não atravessa sólido.
- Rig e IK mantêm comprimentos de membros, animação estável e pose com FPS variável.
- Cliente renderiza com backend selecionado sem dependência em `Zoology`.

### Gate C — ecossistema

- Chuva/seca alteram vegetação/água, animais respondem com mudanças de consumo, movimento, reprodução e mortalidade.
- Predador, herbívoro e peixe têm recursos finitos e impactos verificáveis no ambiente.
- Consumo e morte auditam transferências de matéria/água/energia para domínios responsáveis.
- Extinção não cria indivíduos automaticamente.

### Gate D — herança

- Reprodução cria descendentes versionados, genealogia consistente, herança controlada e mutação medida.
- Testar genealogia, mutação e efeitos por no mínimo algumas sementes e gerações; quantidade definitiva com justificativa estatística.

### Gate E — otimização

- Medir tempos por tick, custo procedural inicial, tempo de pose/raster/transferência GPU, memória do cache e GC.
- Comparar 1/10/100/1000 indivíduos no hardware registrado e cenários com 10/100/1000 criaturas visíveis.
- Não confundir execução fora da câmera com suspensão de metabolismo/simulação.
- LOD ecológico/agregado deve preservar invariantes e ter validação contra modelo individual em cenários de referência.

## Casos numerados

| ID | Cenário | Verificação |
|---|---|---|
| ZT-01 | N seeds mesmo perfil | estabilidade topológica e faixas anatômicas |
| ZT-02 | Mesma seed e versão | mesmo hash canônico de genoma/fenótipo |
| ZT-03 | Seed só de pigmento | anatomia/energia invariantes |
| ZT-04 | Genes herdados | descendência dentro das regras |
| ZT-05 | Morte e decomposição | biomassa contabilizada no reservatório correto |
| ZT-06 | Herbivoria concorrente | consumo total não excede planta disponível |
| ZT-07 | Seca progressiva | comportamento e reservas respondem sem fontes escondidas |
| ZT-08 | Imagem com e sem render | estado simulado idêntico |
| ZT-09 | Diferentes taxas de FPS | estado lógico idêntico |
| ZT-10 | Membro articulado em declive | alcance e contato dentro de tolerâncias |
| ZT-11 | Navegação peixe-rio-lago | acesso e deslocamento coerentes com água |
| ZT-12 | Save/load e versionamento | continuação reproduzida e migração explícita |
| ZT-13 | Controle por jogador vs IA | mesmas restrições corporais e custos |
| ZT-14 | Culling/LOD | resultado ecológico não muda ao afastar câmera |

Todos os testes devem registrar build, seed, cenário, tempo lógico, critérios/tolerâncias fixados antes da execução e evidências.
