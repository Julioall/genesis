# Baselines e evidências

Use este índice para encontrar o estado executável mais recente e as medições canônicas. Os relatórios por iteração continuam disponíveis como histórico; uma captura antiga ou um benchmark de outro commit não substitui o estado atual.

## Baselines do produto

| Tema | Fonte principal | O que demonstra | Limite atual |
|---|---|---|---|
| Parâmetros e escala L0 | [Modelo L0 e benchmarks](../20_L0_MODEL_AND_BENCHMARKS.md) | Unidades, grade, cadências, equações, tolerâncias e histórico quantitativo | L0 é heurístico, não calibrado em campo; comparar medições só com build/cenário equivalentes |
| Cadeia ecológica N3 | [Zoology/N3](ZOOLOGY_N3_2026-10-22.md) | Cenário 40/4, três seeds por 365 dias, balanços, save/load e throughput headless | A população não é calibrada nem equilibrada; não há carcaça persistente |
| Cliente e renderer N4 | [Integração visual N4](N4_VISUAL_INTEGRATION_2026-10-22.md) e [benchmarks N4](N4_BENCHMARKS_2026-10-22.md) | Capturas repetíveis, rubrica visual e janelas locais de desempenho | A rubrica visual falhou; medições foram feitas em uma máquina recomendada, sem validar hardware mínimo ou outras plataformas |
| Hidrologia | [Especificação de hidrologia](../specs/HYDROLOGY.md) | Priority-Flood, depressões, soleiras e estado do solver/renderer | Não há lago dinâmico com estágio/extravasamento; a fixture de cachoeira é somente visual |
| Flora | [Arquitetura de flora](../specs/FLORA_ARCHITECTURE.md) e [revisões N4](N4_VISUAL_INTEGRATION_2026-10-22.md) | Patches ecológicos, malhas, custo e limites visuais | Biomassa lenhosa não informa espécie, idade ou coorte real |

## Subgates de Zoology

- [Z1 — desenvolvimento e genoma](ZOOLOGY_Z1_2026-10-22.md)
- [Z2 — locomoção](ZOOLOGY_Z2_2026-10-22.md)
- [Z3 — orçamento de energia](ZOOLOGY_Z3_2026-10-22.md)
- [Z4 — geometria e integração visual](ZOOLOGY_Z4_2026-10-22.md)
- [Z5 — linhagem e herança prototípica](ZOOLOGY_Z5_2026-10-22.md)

Esses subgates têm estados diferentes. Genoma persistido ou corpo gerado não implica espécie calibrada, anatomia funcional, seleção natural demonstrada ou participação de peixe na população N3.

## Leitura e reprodução

- No checkout `dd2cf47`, a grade canônica é 64 × 64 células de 100 m²; o commit anterior `c6aecbc` usou 256 × 256. Preserve essa distinção ao comparar totais ou custo.
- Cada relatório deve ser lido com seu commit/build, seed, resolução, cenário, hardware, duração de medição e condições de câmera/simulação.
- As métricas de renderização não medem desempenho headless; throughput headless não mede FPS, RAM ou VRAM do cliente.
- Os gates de experiência ainda abertos — entre eles lago dinâmico, estresse/regeneração legíveis e carcaça persistente — estão listados em [Testes](../13_TESTS.md).
- A história completa das avaliações visuais permanece em [relatórios de iteração](.). As capturas não alteram o estado ecológico.
