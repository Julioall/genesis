# Fechamento da meta modular

**Data:** 22/10/2026\
**Estado:** meta encerrada como revisão e consolidação dos incrementos com evidência. Os gates marcados como abertos continuam pendentes; este fechamento não os reclassifica como aprovados.

## Resultado por módulo

| Marco | Fechamento desta meta | Estado final |
|---|---|---|
| N1 — Cosmos, Atmosphere, Geosphere e Hydrosphere | Contratos e ownership separados, fluxos causais verificados, determinismo e balanços preservados. | **Validado no escopo L0.** O relevo físico continua válido para N1; sua apresentação visual ficou abaixo do gate N4. |
| N2 — Botany/Ecology | Respostas a luz, água e N, transferências de estoque e persistência verificadas. | **Validado no escopo documentado.** Não equivale a calibração de campo. |
| N3 — Zoology | Alimentação, predação, metabolismo, água, mortalidade, reprodução, save/load e throughput anual verificados. | **Validado no escopo L0** para os perfis funcionais atuais; parâmetros são heurísticas de jogo. |
| N4 — apresentação | Desempenho local e escala ensaiada passaram; determinismo visual foi demonstrado. A rubrica visual continua abaixo do gate: terreno 2/4, água 1/4, vegetação 2/4, rochas 2/4 e luz/profundidade 1/4. | **Não aprovado.** Direção de árvores definida como malhas 3D procedurais, sem substituir árvores por sprites/billboards. Relevo, água, copas, rochas e hardware mínimo seguem pendentes. |
| Z1 — espécie, plano corporal e genoma | Contratos versionados, dois protótipos, geração determinística, imutabilidade e mutação limitada validados. | **Gate técnico validado.** Calibração biológica por espécie/população não feita. |
| Z2 — anatomia e locomoção | Pose/IK, rota terrestre, planejamento por profundidade e integração técnica cobertos por 24/24 casos dirigidos. | **Em andamento.** Faltam calibração de limiares por população, colisão volumétrica, correnteza e peixe persistente no N3. |
| Z3 — fisiologia e ecologia | Balanço energético dimensional e integração com reservas cobertos por 10/10 casos dirigidos. | **Em andamento.** Custos e conversões continuam proxies; faltam perfis calibrados, crescimento/reprodução energéticos e ecologia aquática. |
| Z4 — geometria e render | A versão v8 corrige winding local pelo gradiente interpolado no tetraedro. Matriz de 12 combinações; filtro 11/11; última suíte Release registrada 151/151. | **Em andamento.** Corpos ainda genéricos; faltam anatomia calibrada, gait, validação mais ampla de auto-interseções, LOD e custo. |
| Z5 — herança e adaptação | Genomas persistentes, linhagem uniparental determinística, migração de save schema 3, 32 gerações e 1.024 mutações por protótipo. | **Subgate técnico concluído; gate evolutivo em andamento.** Faltam alvo populacional, regras sexuais/reprodutivas, vínculo genótipo-fenótipo e seleção/deriva multigeracionais. |

Os onze manifests refletem esses escopos; `validated` significa somente que o gate descrito naquele manifesto passou. Não significa que a biologia esteja calibrada nem que a apresentação N4 tenha sido aprovada.

## Bloqueios que ficam para metas futuras

1. **N4 — relevo:** cordilheiras estreitas concentram altitude na grade física de 10 m; afloramentos horizontais são inseridos sobre encostas sem limitar a variação vertical da borda. Na seed 1, a borda de um platô varia de 38,7 a 104,8 m sob um topo de 122,8 m, produzindo paredes de 18 a 84,1 m. Alargar cordilheiras ou redistribuir a crista reprova o throughput N3 atual; um teto de altura isolado não corrige o terreno sob o platô. Reabrir com uma meta visual/geomorfológica definida e gate anual de desempenho simultâneo. Ver [diagnóstico do relevo](GEOSPHERE_RELIEF_DIAGNOSTIC_2026-10-22.md).
2. **N4 — água e acabamento visual:** o snapshot não forma canais contínuos suficientes para validar margens e cachoeiras naturais; copas e rochas continuam abaixo de 3/4. Manter fixtures identificadas como apresentação, sem tratá-las como evidência física.
3. **Z2–Z5 — alvo biológico:** ainda não foi escolhida uma população para cada perfil. Soay é candidata para herbívoro quadrúpede, não identidade assumida do agente. O Dryad fornece metadados anônimos e o artigo primário aberto, mas nove downloads pequenos tentados retornaram 401; IDs e chaves entre arquivos seguem sem auditoria. Ver [auditoria de dados Z5](../research/zoology/Z5_INHERITANCE_DATA_AUDIT_2026-10-22.md).
4. **Z2–Z5 — integração evolutiva:** sem população, compatibilidade e medidas de referência, não é seguro transformar os protótipos em reprodução sexual, seleção genética, crescimento ou anatomia representativos. A linhagem técnica uniparental permanece identificada como protótipo.
5. **Plataforma:** N4 foi medido na máquina disponível; memória de GPU por processo e o ensaio em hardware mínimo continuam sem evidência.

## Decisão sobre o início simultâneo

Não há uma decisão registrada que justifique começar todos os módulos juntos. A explicação mais plausível é a busca por uma fatia vertical executável, porque o cenário L0 conecta clima, terreno, água, vegetação e fauna. Isso é uma inferência, não uma decisão histórica comprovada. A arquitetura modular e a sequência por gates foram documentadas para que os próximos incrementos avancem um de cada vez.

## Encerramento

Esta meta fecha o inventário, preserva os módulos que já passaram e deixa os gates não aprovados descritos com evidência e condição de retorno. Nenhum bloqueio foi convertido em aprovação. Novas implementações de N4 ou Z2–Z5 devem começar como metas separadas, na ordem e com os dados de entrada indicados acima.
