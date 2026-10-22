# ADR-0006 — evolução modular de Zoology e separação do visual

- **Status:** aceita como direção de arquitetura e sequência; subgates técnicos Z1–Z5 iniciados, com estado parcial nos relatórios
- **Data:** 22/10/2026

## Contexto

O pacote de pesquisa de criaturas procedurais reúne genoma, desenvolvimento, anatomia, fisiologia, ecologia, renderização e evolução. O repositório implementa o escopo ecológico L0 N3. Desde a decisão original, subgates técnicos Z1–Z5 acrescentaram protótipos de genoma, desenvolvimento, locomoção, energia e geometria, sem completar fenótipos ou calibrá-los como espécies reais. Promover o pacote inteiro de uma vez misturaria hipóteses e protótipos com o baseline N3 validado e contrariaria o roadmap incremental registrado em ADR-0005.

Também é necessário distinguir o perfil funcional do L0 (herbívoro/predador) de uma espécie com genoma, e distinguir um desenho de criatura de sua anatomia, fisiologia e estado ecológico.

## Decisão

1. `Game.Modules.Biosphere` permanece dono da biologia e do estado de Zoology. Não criar assemblies automaticamente para `Species`, `Anatomy`, `Physiology` ou cada pasta conceitual.
2. `Game.Procedural` e adaptadores de apresentação podem gerar/consumir geometria, padrões, pose e cache, sem definir genes, metabolismo ou regras funcionais.
3. A simulação e seus contratos não dependem de câmera, FPS, MonoGame ou presença do renderer. MonoGame continua sendo a stack do cliente segundo ADR-0004.
4. O estado e o comportamento N3 L0 continuam baseline validado. Genomas persistidos, mutação uniparental e geometrias são protótipos Z1–Z5; ainda não demonstram espécies calibradas, fisiologia anatômica, evolução populacional nem integração de peixes à população N3.
5. O trabalho segue a sequência Z0–Z5 em [Zoology modular](../specs/ZOOLOGY_MODULE_PLAN.md), avançando uma fatia por vez e mantendo N0–N4 independentes. Os relatórios em [baselines e evidências](../reports/README.md) registram o que cada subgate já implementou e o que permanece aberto. Contratos/persistência só mudam quando o próximo gate exigir.
6. Os nomes e números de ADR que aparecem no pacote importado são locais ao pacote; a governança de decisões do repositório usa sua própria sequência.
7. A bibliografia e as alegações do anexo servem como referências de pesquisa. Não são resultados de benchmark, validação experimental ou evidência de implementação deste repositório.

## Consequências

- Mantemos a cadeia L0 funcional sem chamar o modelo atual de anatomia/genética.
- Genomas/desenvolvimento e regras ecológicas permanecem headless, versionados e reproduzíveis; backend visual pode mudar sem migrar o estado zoológico.
- Cada novo processo biológico precisa documentar proprietário, unidades, entradas/saídas, hipóteses, versão e teste antes de integrar o runtime.
- O plano de aceitação é progressivo. Os gates de evolução e espécies não passam automaticamente com os testes L0 atuais.

## Alternativas consideradas

- Implementar todos os capítulos do pacote imediatamente: rejeitada, pois amplia várias responsabilidades sem um gate por etapa.
- Tratar `VisualRecipe` ou sprite como estado animal: rejeitada, pois mistura apresentação com fenômenos biológicos.
- Criar uma assembly por pasta proposta no pacote: rejeitada nesta fase; não há evidência de que cada pasta exija um boundary de deployment.
- Descartar pesquisa evolutiva: rejeitada; fica documentada e isolada até que genoma, desenvolvimento e reprodução herdável tenham gates próprios.

## Revisão

Revisar quando um subgate Z1–Z5 fechar uma evidência importante ou quando um gate mostrar que as fronteiras atuais impedem capacidade necessária. Qualquer alteração em parâmetros quantitativos L0 também atualiza primeiro `docs/20_L0_MODEL_AND_BENCHMARKS.md`.
