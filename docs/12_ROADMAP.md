# Roadmap para construir a primeira entrega: mundo natural completo

Os marcos N0–N4 são **internos**. Só declare **MVP Natural aprovado** quando todos os gates obrigatórios forem satisfeitos. Cada etapa tem executável/testes e documentação, não apenas arquitetura abstrata.

| Marco | Entrega | Gate |
|---|---|---|
| N0 — Base | Solution `Game.sln`, projetos usados pelo boot, contratos mínimos, agenda sequencial, runner headless, CI e teste pequeno | compila; estado mínimo criado por seed avança ticks; sem dependência gráfica; riscos web registrados, spike até N1 |
| N1 — Física | Cosmos, Atmosphere, Geosphere, Hydrosphere integrados: dia/noite, chuva, temperatura, relevo, solos, água superficial em poças/depressões e saída de borda, infiltração/evaporação | conservação hídrica, resposta à chuva/seca, campos inspecionáveis, determinismo e primeira prova de save/load; conclusão do spike web ou bloqueio/inconclusivo documentado |
| N2 — Botânica/Ecologia | 2 grupos vegetais, estados/biomassa/sementes, resposta água/luz/N, serapilheira e decompositores agregados | biomassa/ciclo de N auditáveis; plantas crescem e morrem em habitats distintos; save preserva novos estoques |
| N3 — Fauna | herbívoro, predador, movimento, competição, consumo finito, metabolismo/custos, reprodução e morte | cadeias alimentares e retorno de detritos sem criação gratuita; cenário de extinção possível; save preserva fauna/agenda |
| N4 — Visual/Integração | cliente MonoGame top-down, sprites e mapa procedural-only, camadas de diagnóstico, pausa/velocidade, save/load, cenário persistente, demonstração de cadeia completa | gate integrado, métricas CPU/FPS/RAM, reprodução por seed e documentação de limitações |

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

Usar o cenário versionado `temperate-valley-l0-v15` definido em [20_L0_MODEL_AND_BENCHMARKS.md](20_L0_MODEL_AND_BENCHMARKS.md), sem usar nome do jogo: cordilheiras geométricas ramificadas, afloramentos com desnível físico, vale úmido, drenagem, manchas reprodutíveis de gramíneas e árvores ou arbustos, herbívoros simples e predadores. Dimensões e duração são parâmetros de teste, não promessas de escala. Comparar 3 sementes diagnósticas para validar variação e 1 seed canônica para regressão; ampliá-las antes de afirmar robustez.

## A primeira entrega não precisa incluir

Maré lunar, glaciares, dinâmica de aquífero, solos multicamada, N/P/K completo, erosão geomorfológica complexa, oceanos, animais com redes neurais, genômica, dezenas de espécies, construções humanas e guerras. **Não criar código-esqueleto não exercitado** para sistemas adiados; documentos de capacidade opcional bastam.

## Realismo e incrementos futuros

Aprofundar cada processo por perfil científico versionado (L0→L1→L2), preservando compatibilidade ou migrando explicitamente. Não aumentar todas as variáveis simultaneamente: escolher os elos causais cuja ampliação aumenta impacto observável no mundo.

## Contenção da curva inicial

N0 é boot determinístico sequencial com contratos usados e teste pequeno, não framework extensível completo. Desktop placeholder não é gate de N0; criar projeto de cada adaptador só ao usá-lo. Save/load deve ter uma prova pequena em N1 e acompanhar novos estoques em N2/N3, evitando descoberta tardia em N4. Medições gráficas começam quando há cliente; N0 mede apenas headless. O spike web tem orçamento máximo proposto de um dia de trabalho; ao exceder, registrar resultado inconclusivo/bloqueio e continuar desktop. Ele não exige implementar segundo cliente completo nem cumprir benchmarks finais em N0.

Antes de ampliar a escala proposta em [escopo](00_SCOPE.md), comprovar o cenário finito com cadeia completa. Mudanças de fidelidade ou infraestrutura precisam indicar qual gate bloqueado resolvem e custo; sem essa justificativa ficam no backlog. Não retirar fauna ou decomposição para declarar MVP concluído.
