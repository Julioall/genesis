# SPEC-ZOO-140 — Herança, adaptação, evolução e eventual especiação

**Status:** Z5 em andamento. N3 persiste genomas versionados por agente e registra mutação/parentesco de um progenitor sob a regra reprodutiva atual. Isso fecha apenas a integração técnica da linhagem; não modela reprodução sexual de vertebrados nem seleção sobre genes.

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

## Subgate técnico atual

- Fundadores são amostrados de forma determinística por seed do mundo e ID do agente. Herbívoros e predadores usam o plano quadrúpede prototípico Z1; perfil funcional não é identidade de espécie.
- O N3 existente cria cada descendente a partir de um progenitor. O genoma desse progenitor passa pelo operador de mutação limitado Z1; o genoma descendente guarda o `GenomeId` parental. `ParentBId` permanece vazio.
- Saves antigos são migrados ao schema 3 com genomas de fundador reproduzíveis. `IZoologyGeneticsV1` mantém a migração opcional e mediada pelo módulo.
- Nenhum alelo altera os resultados ecológicos atuais. Sobrevivência e reprodução ainda não selecionam variação genotípica.
- Este subgate replica a regra uniparental do protótipo N3 para preservar seus fluxos. Não é uma hipótese sobre a reprodução natural dos vertebrados apresentados.
- Testes técnicos reproduzem uma linhagem por 32 gerações e amostram 1.024 mutações por protótipo para verificar limites e média de desvio próxima de zero; esses valores ainda são parâmetros de engenharia, não taxas biológicas calibradas.
- A triagem SciSpace identificou as ovelhas Soay de St Kilda como primeira candidata para um pacote separado de calibração de herbívoro quadrúpede. O depósito Dryad de Bérénos et al. lista tabelas de peso/esqueleto/captura, variantes de pedigree 1/2, arquivos README e SNPs, com registro público CC0. A listagem de metadados está acessível, mas downloads de arquivos Dryad exigem token bearer; sem ele, a tentativa respondeu 401 e o fluxo de arquivo respondeu 403. A leitura dos READMEs e a correspondência dos IDs entre arquivos ainda não foram verificadas; é necessário validar chaves, faixas etárias, sexos e variáveis antes de derivar qualquer parâmetro. Isso não justifica mapear os agentes genéricos N3 a ovelhas.
- O salmão Chinook tem pedigrees multigeracionais e traços de história de vida, mas é anádromo; os dados encontrados não validam o protótipo de peixe residente em água doce. Falta selecionar referência para predador e validar um conjunto de dados completo por população.
- A escolha entre o subgate uniparental e um modelo sexual/recombinante depende de extrair e validar esses pacotes e declarar regras reprodutivas compatíveis. Consulte a [auditoria de dados Z5](../research/zoology/Z5_INHERITANCE_DATA_AUDIT_2026-10-22.md), que registra o bloqueio atual de acesso aos arquivos Dryad.

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
