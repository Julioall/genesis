# Regras de codificação propostas para a implementação futura

Status: contrato documental; nenhum código ou configuração de build criado nesta revisão.

- C# com nullable habilitado, análise estática padrão e formatação uniforme quando o projeto existir. Fixar SDK/dependências na implementação após conferir suporte; não escolher versões por suposição nesta revisão.
- PascalCase em tipos/métodos/propriedades, camelCase em parâmetros/locais, interfaces com I, métodos assíncronos com Async. Um conceito de domínio por tipo; nomes com unidade para valores físicos. Sem abreviações opacas, classes Manager universais ou números físicos espalhados.
- `double` para grandezas contínuas L0; `long` para tick; IDs estáveis para entidades. Tipos de valor para IDs e unidades nas fronteiras; arrays numéricos internos podem permanecer compactos. Não exigir biblioteca de análise dimensional nem fixed-point sem necessidade.
- Validar NaN, Infinity, limites, unidade e janela nas entradas. Zero e ausência são distintos. Erro de configuração falha antes do avanço; recurso insuficiente produz concessão parcial/recusa; invariante violada interrompe com diagnóstico. Não engolir exceções nem normalizar estoque negativo ocultamente.
- Separar cálculo de propostas, resolução de conflito e aplicação de estado. Funções puras para curvas/taxas; composição de perfis para espécies; estados simples para comportamento. Strategy apenas com variação exercitada, sem interface por classe ou hierarquias taxonômicas extensas.
- Sem singleton mutável global, service locator, referências a renderer no domínio, DateTime/tempo de parede ou Random.Shared na simulação. Entrada/saída assíncrona nas bordas; ordem de commit nunca depende de tarefas ou threads.
- Snapshots realmente imutáveis: não expor array mutável via propriedade somente leitura. Spans têm lifetime de fase e não atravessam await. Estado interno só é alterado por seu owner; estruturas compartilhadas não permitem bypass dessa autoridade.
- PRNG e hashing persistente com algoritmo estável documentado; coleções ordenadas explicitamente antes de decisões/reduções. Perfil de processo declara unidade temporal e integra dt uma única vez.
- Parâmetros em receitas versionadas com unidade, intervalo, origem e valor padrão; nenhum valor heurístico é chamado de calibrado. Registrar processo científico na ficha, sem exigir ficha científica para getters/utilitários triviais.
- Testes priorizam conservação, conflitos, causalidade e continuação após save. Testes arquiteturais verificam dependências reais; não replicar implementação em asserts nem medir FPS sem cliente/hardware declarado.
- Bibliotecas adicionais precisam de uso concreto, licença compatível e custo declarado. Nada de ECS, contêiner DI, mediator, repositório genérico ou banco de dados como requisito automático. O ledger é registro de fluxos, não event sourcing do mundo.
- ADR para mudança de ownership, semântica de contrato, persistência ou fronteira de módulo. Ajustes internos sem mudança de comportamento não exigem ADR individual. Código e docs devem distinguir implementado, proposto e verificado.
