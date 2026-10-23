# Correção candidata do relevo — JUL-91

**Data:** 22/10/2026  
**Estado:** candidata física passa as métricas geométricas e a suíte de testes; gate anual N3 ainda não passa neste host.

## Método

Baseline reproduzido em worktree destacada de `main` (`513eaba`). SDK .NET `10.0.400`, build Release. Os campos foram medidos nas grades físicas 64×64, 10 m. O P95 comum inclui pares cardinais cujos dois centros diferem menos de 2 m do campo subjacente, excluindo as bordas dos afloramentos. As paredes usam os 160 pontos amostrados em cada contorno.

## Variante

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

## N3 e conservação

Protocolo: 52.560 ticks, cenário com 40 herbívoros e 4 predadores, três execuções Release da seed 1. O gate pede mediana ≤18,25 s. O baseline local mediu `27,701`, `24,381` e `24,378` s (mediana `24,381` s); a candidata mediu `24,352`, `23,071` e `23,758` s (mediana `23,758` s). A candidata foi cerca de 2,6% mais rápida que o baseline neste host, mas ambos excederam o limite. Logo, o throughput não está aprovado; o relatório N3 histórico de 17,058 s foi obtido em outra execução/hardware.

| Seed candidata | Hash anual repetível | Resíduo água (L) | Resíduo matéria seca (g) | Resíduo N (g) |
|---:|---|---:|---:|---:|
| 1 | `294E121C7145CE87B1A1D7E4491EE13093A89B08F7E6CD43958DE394D37D9637` | −2,074×10⁻⁴ | 1,032×10⁻⁴ | 2,794×10⁻⁹ |
| 2 | `3C54A87F17951BA48A82855BF3EB968371FE1EE7A5AC0078CB05596A960EEE45` | −1,496×10⁻⁴ | 1,065×10⁻⁴ | −3,725×10⁻⁹ |
| 3 | `5E98463B449A9693BBB4FE66DDB683BF363F5EB06520E43EB7D099E633A57AB4` | −2,329×10⁻⁵ | 1,351×10⁻⁴ | −1,583×10⁻⁸ |

Os resíduos ficam dentro das tolerâncias anuais do documento 20. A seed 1 produziu o mesmo hash nas três repetições anuais. A suíte Release passa **155/155** e `dotnet build Game.sln -c Release` passou sem avisos ou erros. Quatro testes foram adicionados para os três seeds geométricos e a rejeição de snapshots incompatíveis.

## Limites e rollback

Foram capturadas as seis combinações de seed 1–3 e projeção ortográfica/perspectiva no dia 0 e repetidas no dia 172. O app reportou zero cachoeiras renderizadas em todas. Arquivos em `docs/reports/evidence/jul-91-seed-{1,2,3}-day-{000,172}-{orthographic,perspective}.png`.

Após essas capturas, foi corrigida a malha visual dos afloramentos: o topo e as paredes agora amostram as mesmas transformações de apresentação (costa e bioma) que a superfície do terreno. Antes, essas partes usavam a altitude anterior às transformações, o que podia criar colunas sem correspondência na superfície. A captura de revisão da seed 1 está em `docs/reports/evidence/jul-91-wall-fix-seed-1.png`; build Release e os 155 testes passaram após a mudança. As seeds 2 e 3 ainda precisam de captura com essa correção.

A elegibilidade inicial para vegetação, calculada com as regras atuais de inclinação ≤1 e água superficial ≤150 mm, foi de 2.790→2.875, 2.540→2.533 e 2.739→2.747 células (baseline→candidata nas seeds 1–3). Como indicador conservador de mobilidade terrestre, as células com inclinação ≤0,75 e água ≤150 mm foram 2.416→2.339, 2.244→1.990 e 2.340→2.180. Esse indicador diminui nas três seeds; logo, a candidata ainda não satisfaz o aceite de preservar habitat para fauna. A conectividade também varia por seed e exige uma decisão geomorfológica melhor antes de aprovar. O limite de 40 m descreve a parede amostrada; a grade raster pode ter saltos locais distintos, cobertos separadamente pelas métricas de terreno comum.

Para rollback, remover os multiplicadores de largura e a política de ajuste de raios/cota em `TerrainModel`, restaurando os parâmetros do baseline. Manter a validação do snapshot apenas se o gerador físico continuar versionado sem migração para saves antigos.
