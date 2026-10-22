# Roadmap para construir a primeira entrega: mundo natural completo

Os marcos N0–N4 são **internos**. Só declare **MVP Natural aprovado** quando todos os gates obrigatórios forem satisfeitos. Cada etapa tem executável/testes e documentação, não apenas arquitetura abstrata.

| Marco | Entrega | Gate |
|---|---|---|
| N0 — Base | Solution `Game.sln`, projetos usados pelo boot, contratos mínimos, agenda sequencial, runner headless, CI e teste pequeno | compila; estado mínimo criado por seed avança ticks; sem dependência gráfica; riscos web registrados, spike até N1 |
| N1 — Física | Cosmos, Atmosphere, Geosphere, Hydrosphere integrados: dia/noite, chuva, temperatura, relevo, solos, água superficial em poças/depressões e saída de borda, infiltração/evaporação | conservação hídrica, resposta à chuva/seca, campos inspecionáveis, determinismo e primeira prova de save/load; conclusão do spike web ou bloqueio/inconclusivo documentado |
| N2 — Botânica/Ecologia | 2 grupos vegetais, estados/biomassa/sementes, resposta água/luz/N, serapilheira e decompositores agregados | biomassa/ciclo de N auditáveis; plantas crescem e morrem em habitats distintos; save preserva novos estoques |
| N3 — Fauna | herbívoro, predador, movimento, competição, consumo finito, metabolismo/custos, reprodução e morte | cadeia alimentar e retorno de detritos sem criação gratuita; cenário 40/4 em 3 seeds por 365 dias com balanços/saves válidos; seed 1 Release ≥20 dias simulados/s |
| N4 — Visual/Integração | cliente MonoGame com técnica visual definida pelo gate, mundo procedural alinhado à referência, camadas de diagnóstico, pausa/velocidade, save/load, cenário persistente, demonstração de cadeia completa | gate integrado, critérios visuais e métricas CPU/GPU/FPS/RAM, reprodução por seed e documentação de limites e plataformas |

## Artefatos a cada marco

Antes de implementar um processo, fechar sua ficha quantitativa usando [20_L0_MODEL_AND_BENCHMARKS.md](20_L0_MODEL_AND_BENCHMARKS.md) como contrato numérico único do cenário L0. Equações, parâmetros, tolerâncias e metas não podem divergir entre domínio, testes e desempenho.

- Manifestos de módulos com owner/capability e fases.
- Ficha científica L0 de cada processo incluído.
- Teste unitário/integração determinística por processo crítico.
- Fixtures de cenário com seed, parâmetros, expected invariants, tolerância.
- `README.md` com comandos reais que compilem/testem/rodarem, nenhuma instrução fictícia.
- Evidência de medição de desempenho; hardware/build/seed/tamanho da cena.
- ADR para decisão durável e relatório de pendências/risco.

## Primeiro cenário de referência

Usar o cenário versionado `temperate-valley-l0-v17` definido em [20_L0_MODEL_AND_BENCHMARKS.md](20_L0_MODEL_AND_BENCHMARKS.md), sem usar nome do jogo: cordilheiras geométricas ramificadas, afloramentos com desnível físico, vale úmido, depressões e soleiras hidrológicas separadas do leito original, manchas reprodutíveis de gramíneas e árvores ou arbustos, herbívoros simples e predadores. Dimensões e duração são parâmetros de teste, não promessas de escala. Comparar 3 sementes diagnósticas para validar variação e 1 seed canônica para regressão; ampliá-las antes de afirmar robustez.

## Gate visual antes de fechar N4

MonoGame é a stack obrigatória e não está em avaliação. Antes de fechar câmera e técnica visual, executar no cliente MonoGame a cena comparativa e os critérios de [direção visual e técnicas de apresentação](22_VISUAL_DIRECTION_AND_TECHNOLOGY.md). Preservar N0–N3 e os contratos quantitativos; a validação pode ocorrer em paralelo à evolução científica. Se a prova atrasar, manter a técnica de apresentação em aberto e não redefinir parâmetros físicos ou biológicos para favorecer uma abordagem gráfica.

**Checkpoint de 22/10/2026:** o benchmark N4 de 60 s desde o dia zero, com simulação ativa, passou nesta máquina. A comparação histórica 3D/híbrida nas duas projeções e a carga visual 10× também passaram as medições locais de 60 s, com câmera fixada durante a amostra. O usuário definiu árvores e objetos naturais como geometria 3D procedural; sprites e billboards de árvore estão fora da direção selecionada. N4 continua aberto: a malha 3D ainda não chega a 3/4 em todas as dimensões; hardware mínimo e outras plataformas não foram validados. Consulte o [relatório N4 de integração visual](reports/N4_VISUAL_INTEGRATION_2026-10-22.md).

## A primeira entrega não precisa incluir

Maré lunar, glaciares, dinâmica de aquífero, solos multicamada, N/P/K completo, erosão geomorfológica complexa, oceanos, animais com redes neurais, genômica, dezenas de espécies, construções humanas e guerras. **Não criar código-esqueleto não exercitado** para sistemas adiados; documentos de capacidade opcional bastam.

## Realismo e incrementos futuros

Aprofundar cada processo por perfil científico versionado (L0→L1→L2), preservando compatibilidade ou migrando explicitamente. Não aumentar todas as variáveis simultaneamente: escolher os elos causais cuja ampliação aumenta impacto observável no mundo.

### Evolução futura de Zoology

Após os marcos N0–N4, evoluir Zoology em módulos com gates independentes conforme o [plano modular de Zoology](specs/ZOOLOGY_MODULE_PLAN.md): baseline N3; espécie/plano corporal/genoma/desenvolvimento; anatomia/locomoção; fisiologia ecológica; pipeline visual derivado; herança/adaptação. Não iniciar a implementação completa desses módulos em paralelo. Cada gate precisa preservar o baseline N3 e demonstrar sua evidência antes de abrir o seguinte. As specs `ZOO-*` são propostas, não funcionalidades já entregues.

## Contenção da curva inicial

N0 é boot determinístico sequencial com contratos usados e teste pequeno, não framework extensível completo. Desktop placeholder não é gate de N0; criar projeto de cada adaptador só ao usá-lo. Save/load deve ter uma prova pequena em N1 e acompanhar novos estoques em N2/N3, evitando descoberta tardia em N4. Medições gráficas começam quando há cliente; N0 mede apenas headless. O spike web tem orçamento máximo proposto de um dia de trabalho; ao exceder, registrar resultado inconclusivo/bloqueio e continuar desktop. Ele não exige implementar segundo cliente completo nem cumprir benchmarks finais em N0.

Antes de ampliar a escala proposta em [escopo](00_SCOPE.md), comprovar o cenário finito com cadeia completa. Mudanças de fidelidade ou infraestrutura precisam indicar qual gate bloqueado resolvem e custo; sem essa justificativa ficam no backlog. Não retirar fauna ou decomposição para declarar MVP concluído.
