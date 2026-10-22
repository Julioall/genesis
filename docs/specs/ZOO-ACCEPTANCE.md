# Plano de validação — geração e vida de criaturas

**Regra:** os testes abaixo são critérios por gate. Em 2026-10-22, o gate A técnico Z1 passou (104/104); a suíte completa passou **137/137**, incluindo IK/pose e desvios de rota por elevação/água em Z2, balanço energético N3 em Z3, geometria e pose estática Z4, linhagem uniparental e migração de genomas Z5. O modo visual Z4 ligado à população e derivado de genoma individual foi verificado por captura manual. Há teste de igualdade do save com/sem geração geométrica/postural; câmera, FPS, culling e LOD ainda não foram comparados no cliente. Z2 permanece aberto para calibração por espécie, colisão corporal e locomoção aquática. Z3 permanece aberto para perfis calibrados. Z4 permanece aberto para animação, invariância no cliente e performance. Z5 tem apenas o subgate técnico de genomas/linhagem: reprodução sexual ou compatibilidade por espécie, deriva/seleção em populações e especiação seguem abertas. Confira os relatórios [Z1](../reports/ZOOLOGY_Z1_2026-10-22.md), [Z2](../reports/ZOOLOGY_Z2_2026-10-22.md), [Z3](../reports/ZOOLOGY_Z3_2026-10-22.md), [Z4](../reports/ZOOLOGY_Z4_2026-10-22.md) e [Z5](../reports/ZOOLOGY_Z5_2026-10-22.md).

## Gates

### Gate A — núcleo procedural puro

- Projeto compilável em C# sem MonoGame nem qualquer renderizador.
- Dois protótipos headless de famílias: quadrúpede e peixe, com genomas versionados e anatomia estruturalmente válida para seeds variadas.
- Alteração de cor não altera anatomia ou capacidade fisiológica.
- Export/restore de genoma e espécie sem dependência de assets externos.

### Gate B — animação e adaptação ao terreno

- Quadrúpede caminha sobre declives sob limites de alcance; peixe usa estado de profundidade e não atravessa sólido.
- Rig e IK mantêm comprimentos de membros, animação estável e pose com FPS variável.
- Cliente renderiza com backend selecionado sem dependência em `Zoology`.

### Gate C — ecossistema

- Chuva/seca alteram vegetação/água, animais respondem com mudanças de consumo, movimento, reprodução e mortalidade.
- Predador, herbívoro e peixe têm recursos finitos e impactos verificáveis no ambiente.
- Consumo e morte auditam transferências de matéria/água/energia para domínios responsáveis.
- Extinção não cria indivíduos automaticamente.

### Gate D — herança

- Reprodução cria descendentes versionados, genealogia consistente, herança controlada e mutação medida.
- Testar genealogia, mutação e efeitos por no mínimo algumas sementes e gerações; quantidade definitiva com justificativa estatística.

### Gate E — otimização

- Medir tempos por tick, custo procedural inicial, tempo de pose/raster/transferência GPU, memória do cache e GC.
- Comparar 1/10/100/1000 indivíduos no hardware registrado e cenários com 10/100/1000 criaturas visíveis.
- Não confundir execução fora da câmera com suspensão de metabolismo/simulação.
- LOD ecológico/agregado deve preservar invariantes e ter validação contra modelo individual em cenários de referência.

## Casos numerados

| ID | Cenário | Verificação |
|---|---|---|
| ZT-01 | N seeds mesmo perfil | estabilidade topológica e faixas anatômicas |
| ZT-02 | Mesma seed e versão | mesmo hash canônico de genoma/fenótipo |
| ZT-03 | Seed só de pigmento | anatomia/energia invariantes |
| ZT-04 | Genes herdados | descendência dentro das regras |
| ZT-05 | Morte e decomposição | biomassa contabilizada no reservatório correto |
| ZT-06 | Herbivoria concorrente | consumo total não excede planta disponível |
| ZT-07 | Seca progressiva | comportamento e reservas respondem sem fontes escondidas |
| ZT-08 | Imagem com e sem render | estado simulado idêntico |
| ZT-09 | Diferentes taxas de FPS | estado lógico idêntico |
| ZT-10 | Membro articulado em declive | alcance e contato dentro de tolerâncias |
| ZT-11 | Navegação peixe-rio-lago | acesso e deslocamento coerentes com água |
| ZT-12 | Save/load e versionamento | continuação reproduzida e migração explícita |
| ZT-13 | Controle por jogador vs IA | mesmas restrições corporais e custos |
| ZT-14 | Culling/LOD | resultado ecológico não muda ao afastar câmera |

Todos os testes devem registrar build, seed, cenário, tempo lógico, critérios/tolerâncias fixados antes da execução e evidências.
