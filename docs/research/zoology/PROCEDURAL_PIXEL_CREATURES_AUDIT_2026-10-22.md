# Auditoria de reaproveitamento: Procedural Pixel Creatures

**Data:** 2026-10-22\
**Snapshot conferido:** `main` em `a2207c4bd6f1be51d426684cf58f11668def7579` (commit de 2026-10-05). O `refs/heads/main` remoto apontava para o mesmo hash durante esta revisão.\
**Licença publicada:** MIT.\
**Resultado:** a referência contém um framework procedural de criaturas funcional e amplo, não apenas uma galeria de sprites. A reconsulta confirmou que boa parte da infraestrutura de fauna já existe como código C#: famílias, genética, anatomia, rig e locomoção. O Gênesis já cobre alguns desses fundamentos com contratos próprios, mas ainda não tem a variedade de planos corporais nem a animação articulada contínua da referência. O rasterizador em pixel art e o runtime Godot não atendem diretamente ao caminho de malha 3D e ecologia acoplada adotado aqui.

## O que existe de fato na referência

O repositório combina um núcleo C# independente do Godot com um adaptador Godot. O núcleo gera modelos imutáveis, animação, renderização CPU em quadros de pixels e exportação. A arquitetura documenta quatro streams determinísticos (anatomia, cor, padrão e movimento), caches LRU separados por partes do modelo, genes tipados com grupos de ligação, operadores de mutação e cruzamento, rigs semânticos, IK e módulos de locomoção com passo fixo. O README lista nove famílias procedurais e uma extensão de plantfolk.

O código plantfolk é uma família de criaturas vegetais antropomórficas, como mandrágoras, cogumelos e cactos. Seu `RootWalkLocomotion` anima raízes que funcionam como pernas. Isso demonstra a API de extensões e a especialização por genes, mas **não é um gerador de árvores** e não modela a distribuição biológica de raízes expostas por espécie, solo ou erosão.

## Reconsulta solicitada: o que já está pronto para uso como base

O checkout direto de `main` confirmou o mesmo commit `a2207c4bd6f1be51d426684cf58f11668def7579`; portanto, não houve uma nova versão upstream desde o snapshot anterior. A leitura do código confirmou, além da documentação, uma implementação real de nove famílias, 54 genomas de exemplo, `GenomeOps.Mutate`/cruzamento por grupos de ligação, modelo de anatomia imutável, motor em passo fixo, rig semântico, IK e módulos substituíveis de locomoção. O `core_harness` compila as pastas `Core`, `Plantfolk/Core` e testes como um programa .NET sem referenciar assemblies Godot. Isso torna o núcleo uma base C# tecnicamente reutilizável, e não só uma referência visual.

Isso muda a recomendação de “consultar como inspiração” para **avaliar reaproveitamento de implementação antes de construir esses mesmos subsistemas do zero**. Os candidatos mais diretos são o operador de cruzamento com proveniência para Z5 e a cadeia motor → módulo de locomoção → rig/pose para fechar a marcha articulada de Z2/Z4. O núcleo em si não é um pacote pronto plug-and-play para o Gênesis: o harness compila fontes por inclusão, as entidades e unidades diferem dos contratos `CreatureGenomeV1`/`DevelopedBodyV1`, e a saída visual da referência ainda é um rasterizador 2D de pixel art. Reusar trechos exige adaptadores para os contratos, o solver ambiental e as malhas 3D locais, além de preservar `LICENSE` e `THIRD_PARTY_NOTICES.md` conforme a MIT.

| Subsistema da referência | Pode adiantar no Gênesis | Ainda precisa ser feito aqui |
|---|---|---|
| Genomas, streams, genes tipados, mutação, cruzamento e linhagem | Z1/Z5: lógica de operadores, compatibilidade, grupos de ligação e proveniência | Mapear genes para espécies/populações naturais, manter schema/migrações locais e conectar alelos a fenótipos e resultados ecológicos |
| Anatomia, blocos reutilizáveis, rig e IK | Z1/Z2: organização de topologia, âncoras, pose e interface de locomoção | Integrar dimensões/unidades e limites com `DevelopedBodyV1`, habitat e estado persistente do N3 |
| Motor em passo fixo e módulos de marcha, voo, nado e serpentino | Z2/Z4: algoritmo para sair da postura estática e produzir pose articulada contínua por indivíduo | Aplicar pose às superfícies da malha 3D, validar contato com terreno/água e manter simulação ecológica independente do renderer |
| Especificação de superfícies, padrões, renderizador e exportação | Ideias de separação entre pigmento, padrão e anatomia | O renderizador é 2D/pixel art; não fornece a malha 3D nem resolve o gate visual do Gênesis |
| Área de teste com relevo, habitats e criaturas móveis | Referência para demonstração de ambiente e integração de locomoção | Não fornece o modelo ecológico N3 de transferências de biomassa/energia, fisiologia calibrada ou dinâmica populacional do projeto |
| Plantfolk/rootwalk | Exemplo de extensão de família e locomoção especializada | Não serve como biologia de árvores nem como regra universal de raízes expostas |

**Conclusão revisada:** sim, já existe ali uma parcela grande da engenharia de criaturas que estamos desenvolvendo. O maior valor imediato está em Z2 e Z5; Z1 também tem referência madura, embora o Gênesis já tenha o gate técnico básico. Não se deve reimplementar um motor completo de marcha ou cruzamento sem primeiro testar o encaixe desses componentes. Isso não fecha Z3 (fisiologia/ecologia) nem valida espécies naturais, e tampouco substitui a saída 3D procedimental exigida no Gênesis.

## Comparação com o Gênesis

| Área | Já existe no Procedural Pixel Creatures | Estado correspondente no Gênesis | O que a comparação indica |
|---|---|---|---|
| Limites de módulos | Núcleo puro separado do runtime gráfico; estágios de geração, pose e renderização separados | `Game.Contracts`, `Game.Modules.Biosphere`, `Game.Procedural` e o adaptador MonoGame já separam desenvolvimento, pose, geometria e exibição | O princípio arquitetural já está aplicado. Não é necessário substituir a arquitetura local. |
| Determinismo e identidade | Seeds independentes para anatomia, cor, padrão e movimento, com versão de gerador | Z1 persiste `AnatomySeed` e `PigmentSeed`; `PatternSeed` é derivada da seed de pigmento na receita visual. Ainda não há stream herdado próprio de movimento | O isolamento entre anatomia e pigmento já funciona. Quatro streams só devem ser adicionados em uma versão de contrato/genoma com migração explícita. |
| Cache de geração | `CreatureFactory` mantém caches LRU separados para anatomia, paleta, superfície, movimento e modelo, chaveados por conteúdo relevante e versão | O modo visual N3 usa cache cliente por `(GenomeId, DevelopmentStep)` para buffers de malha; ele não substitui um cache de modelo por stream nem é o cache LRU do framework | Separar caches pode evitar recalcular partes independentes quando genes visuais ou de movimento mudarem. Medir antes de ampliar a implementação. |
| Genética extensível | Schema de genes por família, arquétipos, grupos de ligação, mutação e cruzamento com proveniência | Z1 tem duas espécies-protótipo e sete campos de gene tipados; a mutação é uniparental e limitada. N3 persiste o genoma prototípico, mas seus alelos ainda não dirigem toda a ecologia | A arquitetura de schemas/compatibilidade é adaptável. Genes e faixas da referência não constituem dados de espécies naturais. |
| Plano corporal | Nove famílias com blocos anatômicos reutilizáveis, esqueleto e `MotionRig` semântico | Z1 tem protótipos de quadrúpede e peixe com grafo imutável de partes, juntas e sensores; Z4 transforma esses dados em malha 3D renderer-neutral | O padrão de topologia semântica já está presente. A principal expansão é adicionar corpos de espécies/populações calibradas, não copiar famílias fantásticas. |
| Locomoção e pose | Motor individual de passo fixo, rig articulado, IK e módulos de marcha, voo, natação, movimento serpentino e outros | Z2 tem IK de quadrúpede, pose axial de peixe e planejamento de rotas. No render integrado, Z4 aplica uma postura estática de apoio; ainda não há marcha contínua aplicada à geometria da população N3 | Este é o maior delta técnico aproveitável: converter intenção de movimento em pose articulada reproduzível e aplicar essa pose à malha sem alterar estado ecológico. |
| Apresentação | Rasterizador CPU de pixels lógicos, texturas pixel art, quadros e spritesheets | O usuário requer apresentação procedural 3D; Z4 usa superfícies e malhas, com adaptador MonoGame | Não portar o rasterizador, frames/spritesheets nem o runtime Godot para o caminho 3D. Reaproveitar conceitos de anatomia, pose e cache sem herdar a representação 2D. |
| Árvores e raízes | Plantfolk gera criaturas vegetais com raízes-limbs e coroa estilizada | A flora do Gênesis já atribui potencial de exposição radicular por perfil de árvore | Plantfolk não resolve crescimento arbóreo ou exposição radicular ambiental. Preservar a regra local por espécie e aprofundar a biologia no módulo de flora. |

## Decisões para a sequência modular

1. **Z1:** manter o contrato V1 validado. Considerar streams de padrão e movimento em uma evolução versionada, com migração de saves; não remodelar genomas existentes só para imitar a API da referência.
2. **Z2/Z4:** antes de escrever outro motor de marcha, fazer uma prova de integração pequena com `CreatureMotor`, `ILocomotionModule`/`LeggedLocomotion` e `RigDriver` (ou portar somente o algoritmo necessário), convertendo a pose para `DevelopedBodyV1` e a malha 3D. O critério é movimento reproduzível com contato coerente, sem deixar o renderer alterar posição, energia ou estado ecológico.
3. **Z4:** conservar malha procedural 3D. O cache atual atende à vista N3, mas deve ser avaliado separadamente de um cache LRU por estágio quando houver variantes de pigmento, movimento e LOD.
4. **Z5:** avaliar `GenomeOps` de cruzamento e seus grupos de ligação/proveniência contra o contrato persistente local antes de expandir o operador uniparental. Esses algoritmos não resolvem compatibilidade reprodutiva natural, calibração biológica nem o acesso às tabelas de populações reais.
5. **Flora:** não transpor o modelo plantfolk para árvores. A base grossa, assimetria e exposição de raízes precisam depender da espécie e do ambiente modelado, como a solicitação do usuário já estabeleceu.

> Nenhum código, recurso gráfico ou runtime foi copiado do repositório nesta revisão. A licença MIT permite reutilização futura desde que seus avisos e atribuições sejam preservados; isso não transforma os valores ou formas do gerador em evidência biológica.

## Fontes

- [README e escopo funcional](https://github.com/idlerunner00/procedural-pixel-creatures/blob/a2207c4bd6f1be51d426684cf58f11668def7579/README.md)
- [Arquitetura, streams, cache, anatomia e animação](https://github.com/idlerunner00/procedural-pixel-creatures/blob/a2207c4bd6f1be51d426684cf58f11668def7579/docs/ARCHITECTURE.md)
- [Modelo e fábrica com caches por estágio](https://github.com/idlerunner00/procedural-pixel-creatures/blob/a2207c4bd6f1be51d426684cf58f11668def7579/addons/procedural_creatures/Core/Framework/CreatureModel.cs)
- [Família plantfolk](https://github.com/idlerunner00/procedural-pixel-creatures/blob/a2207c4bd6f1be51d426684cf58f11668def7579/addons/procedural_creatures_plantfolk/Core/PlantfolkFamily.cs) e [extensão/rootwalk](https://github.com/idlerunner00/procedural-pixel-creatures/blob/a2207c4bd6f1be51d426684cf58f11668def7579/addons/procedural_creatures_plantfolk/Core/PlantfolkExtension.cs)
- [Licença MIT](https://github.com/idlerunner00/procedural-pixel-creatures/blob/a2207c4bd6f1be51d426684cf58f11668def7579/LICENSE)
- Implementação local comparada: [`CreatureGeneticsContracts.cs`](../../../src/Game.Contracts/CreatureGeneticsContracts.cs), [`CreatureDevelopmentModelV1.cs`](../../../src/Game.Modules.Biosphere/CreatureDevelopmentModelV1.cs), [`CreaturePoseSolverV1.cs`](../../../src/Game.Modules.Biosphere/CreaturePoseSolverV1.cs), [`CreatureGeometryBuilderV1.cs`](../../../src/Game.Procedural/CreatureGeometryBuilderV1.cs), [`CreatureGeometryMonoGameAdapter.cs`](../../../src/Game.Client.Desktop/CreatureGeometryMonoGameAdapter.cs).
