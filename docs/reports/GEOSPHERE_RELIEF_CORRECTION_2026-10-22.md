# Correção candidata do relevo — JUL-91

**Data da atualização:** 23/10/2026
**Estado:** variante visual atual aprovada nos índices geométricos e de habitat das seeds 1–3. Desempenho N3 foi adiado pelo usuário e não bloqueia a JUL-91.

## Método

Baseline reproduzido em worktree destacada de `main` (`513eaba`). SDK .NET `10.0.400`, build Release. Os campos foram medidos nas grades físicas 64×64, 10 m. O P95 comum inclui pares cardinais cujos dois centros diferem menos de 2 m do campo subjacente, excluindo as bordas dos afloramentos. As paredes usam os 160 pontos amostrados em cada contorno.

## Variante histórica (amplitude original)

- Largura gaussiana dos ombros das duas cordilheiras: multiplicador `1.45`; amplitude e eixos preservados.
- Afloramentos mantêm posição e forma determinísticas. O gerador reduz seus raios somente até obter paredes inteiramente entre 18 m e 40 m. A cota da coroa é limitada por esses extremos.
- O cliente valida a grade do snapshot contra o gerador atual e rejeita incompatibilidade com erro explicativo. O save original permanece intacto.

## Métricas por seed

| Seed | Baseline P95 comum (m) | Candidata P95 (m) | Redução | Baseline máx. comum (m) | Candidata máx. comum (m) | Paredes candidatas (m) |
|---:|---:|---:|---:|---:|---:|---:|
| 1 | 16,44 | 14,47 | 12,0% | 39,96 | 38,47 | 18–40 |
| 2 | 18,37 | 15,80 | 14,0% | 41,18 | 31,10 | 18–40 |
| 3 | 16,84 | 14,71 | 12,6% | 38,82 | 28,45 | 18–40 |

As três seeds mantêm sete afloramentos separados. Os testes também preservam as duas cristas sinuosas, relevo máximo acima de 180 m, determinismo e comportamento D8.

## Histórico de N3 da variante anterior

Protocolo: 52.560 ticks, cenário com 40 herbívoros e 4 predadores, três execuções Release da seed 1. O gate pede mediana ≤18,25 s. O baseline local mediu `27,701`, `24,381` e `24,378` s (mediana `24,381` s); a candidata mediu `24,352`, `23,071` e `23,758` s (mediana `23,758` s). A candidata foi cerca de 2,6% mais rápida que o baseline neste host, mas ambos excederam o limite. Logo, o throughput não está aprovado; o relatório N3 histórico de 17,058 s foi obtido em outra execução/hardware.

| Seed candidata | Hash anual repetível | Resíduo água (L) | Resíduo matéria seca (g) | Resíduo N (g) |
|---:|---|---:|---:|---:|
| 1 | `294E121C7145CE87B1A1D7E4491EE13093A89B08F7E6CD43958DE394D37D9637` | −2,074×10⁻⁴ | 1,032×10⁻⁴ | 2,794×10⁻⁹ |
| 2 | `3C54A87F17951BA48A82855BF3EB968371FE1EE7A5AC0078CB05596A960EEE45` | −1,496×10⁻⁴ | 1,065×10⁻⁴ | −3,725×10⁻⁹ |
| 3 | `5E98463B449A9693BBB4FE66DDB683BF363F5EB06520E43EB7D099E633A57AB4` | −2,329×10⁻⁵ | 1,351×10⁻⁴ | −1,583×10⁻⁸ |

Os resíduos ficam dentro das tolerâncias anuais do documento 20. A seed 1 produziu o mesmo hash nas três repetições anuais. A suíte Release passa **155/155** e `dotnet build Game.sln -c Release` passou sem avisos ou erros. Quatro testes foram adicionados para os três seeds geométricos e a rejeição de snapshots incompatíveis.

## Histórico: limites da variante anterior (amplitude original)

Foram capturadas as seis combinações de seed 1–3 e projeção ortográfica/perspectiva no dia 0 e repetidas no dia 172. O app reportou zero cachoeiras renderizadas em todas. Arquivos em `docs/reports/evidence/jul-91-seed-{1,2,3}-day-{000,172}-{orthographic,perspective}.png`.

Após essas capturas, foi corrigida a malha visual dos afloramentos: o topo e as paredes agora amostram as mesmas transformações de apresentação (costa e bioma) que a superfície do terreno. Antes, essas partes usavam a altitude anterior às transformações, o que podia criar colunas sem correspondência na superfície. A captura de revisão da seed 1 está em `docs/reports/evidence/jul-91-wall-fix-seed-1.png`; build Release e os 155 testes passaram após a mudança. As seeds 2 e 3 ainda precisam de captura com essa correção.

A elegibilidade inicial para vegetação, calculada com as regras atuais de inclinação ≤1 e água superficial ≤150 mm, foi de 2.790→2.875, 2.540→2.533 e 2.739→2.747 células (baseline→candidata nas seeds 1–3). Como indicador conservador de mobilidade terrestre, as células com inclinação ≤0,75 e água ≤150 mm foram 2.416→2.339, 2.244→1.990 e 2.340→2.180. Esse indicador diminui nas três seeds; logo, a candidata ainda não satisfaz o aceite de preservar habitat para fauna. A conectividade também varia por seed e exige uma decisão geomorfológica melhor antes de aprovar. O limite de 40 m descreve a parede amostrada; a grade raster pode ter saltos locais distintos, cobertos separadamente pelas métricas de terreno comum.

Para rollback, remover os multiplicadores de largura e a política de ajuste de raios/cota em `TerrainModel`, restaurando os parâmetros do baseline. Manter a validação do snapshot apenas se o gerador físico continuar versionado sem migração para saves antigos.

## Histórico: medição temporária de throughput — variante 1,35

Por decisão do usuário, o aceite de throughput específico da JUL-91 fica temporariamente em **pelo menos 5 dias simulados por segundo** (365 dias em até 73 s). O gate geral N3 de 20 dias/s do documento 20 não foi alterado.

Na variante local com `RidgeWidthMultiplier=1.35`, três execuções Release sequenciais da seed 1, cenário 40 herbívoros + 4 predadores e 52.560 ticks mediram 27,902 s, 25,020 s e 28,164 s; mediana **27,902 s**, equivalente a **13,08 dias/s**. Todas produziram o mesmo hash `386668E739F047542C29EC11374AA21F964CD8180CBE37A6FB91E02D7755B40E`. Os resíduos observados foram água `−2,353×10⁻⁴ L`, matéria seca `1,419×10⁻⁴ g` e nitrogênio `−1,118×10⁻⁸ g`, dentro das tolerâncias do documento 20. O SDK é .NET 10.0.400 e o processador é Intel Core i3-10105; a máquina corresponde à referência descrita no documento 20.

Esta medição é histórica e pertence à variante 1,35. O usuário definiu depois que desempenho não será foco da JUL-91; o benchmark anual não bloqueia esta entrega e o gate geral do documento 20 não foi alterado.

## Variante visual selecionada — largura 1,45, amplitude 0,80

A seleção mantém eixos, espigões, ruídos e formas determinísticas dos afloramentos. As larguras das cordilheiras são 1,45× o baseline; as amplitudes de ambas são 0,80×. Os sete afloramentos são ajustados deterministicamente para paredes físicas de 18–40 m.

| Seed | P95 comum (m) | Redução vs. baseline | Máximo comum (m) | Área elegível à vegetação (baseline → atual) | Habitat terrestre ≤0,75 (baseline → atual) | Maior componente D8 atual |
|---:|---:|---:|---:|---:|---:|---:|
| 1 | 12,37 | 24,8% | 31,61 | 2.790 → 3.170 | 2.416 → 2.681 | 2.631 |
| 2 | 13,18 | 28,3% | 25,69 | 2.540 → 2.917 | 2.244 → 2.339 | 2.188 |
| 3 | 12,46 | 26,0% | 23,43 | 2.739 → 3.106 | 2.340 → 2.546 | 2.464 |

As medidas usam a grade 64×64 de 10 m. Diferenças comuns excluem as bordas dos afloramentos. A elegibilidade usa as regras atuais: água superficial ≤150 mm, declive ≤1,0 para vegetação e ≤0,75 para habitat terrestre conservador. O maior componente usa vizinhança D8 e cobre pelo menos 90% das células conservadoramente elegíveis nas três seeds. Os máximos comuns ficam abaixo dos baselines 39,96 / 41,18 / 38,82 m. Os testes verificam continuidade das duas cristas, sete afloramentos separados e paredes de 18–40 m.

As novas capturas do cliente para seeds 1–3, no dia 0 e no dia 172, em ortográfica e perspectiva, estão em `docs/reports/evidence/jul-91-final-seed-{1,2,3}-{day000,day172}-{orthographic,perspective}.png`. Todas reportam zero cachoeiras renderizadas; a correspondência entre afloramentos e superfície foi conferida nas três seeds.

A suíte Release passou em **165/165 testes**, incluindo determinismo, continuação após save/load, incompatibilidade visual de snapshots, saltos comuns, habitats e paredes. O benchmark anual N3 não foi repetido para esta variante final, conforme a prioridade visual definida pelo usuário.

Rollback: restaurar `RidgeWidthMultiplier` e `RidgeAmplitudeMultiplier` para 1,0 em `TerrainModel`. Manter a validação de compatibilidade dos snapshots porque a mudança do campo físico altera saves antigos.
