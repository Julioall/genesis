# Modelos L0, parâmetros, tolerâncias e metas de desempenho

**Status:** especificação documental para o primeiro mundo; não é código implementado nem calibração ecológica. **Escopo:** mapa finito do MVP e processos necessários para clima, água, duas plantas funcionais, herbívoro, predador e decomposição. A física atmosférica, a fisiologia e os comportamentos são aproximações transparentes de jogo.

As equações abaixo são o contrato L0 para a implementação futura. Os valores são parâmetros iniciais de engenharia, não constantes universais nem previsões da Terra. Se um gate falhar, ajustar primeiro o processo responsável, registrar seed/configuração e atualizar a versão do cenário. Não acrescentar processos adiados para compensar um parâmetro ruim.

## Cenário e cadência de referência

| Parâmetro | Valor L0 |
|---|---:|
| `scenarioId` | `temperate-valley-l0-v2` |
| Seed canônica | `1` (hexadecimal `0x0000000000000001`) |
| Seeds de integração | `1`, `2`, `3` |
| Grade | 64 × 64 células finitas; simular a grade toda, mesmo fora da câmera |
| Célula | 10 m × 10 m = 100 m²; área total 0.4096 km² |
| Latitude | 45° N |
| Relógio | ano de 365 dias, sem ano bissexto |
| Tick lógico | 10 minutos simulados; 144 ticks/dia; inteiro monotônico |
| Passos de clima e hidrologia | 1 hora, no limite entre ticks 10 min |
| Passo de vegetação e N do solo | 1 hora |
| Passo de ação, movimento e metabolismo animal | 10 minutos |
| Passo de decomposição e reprodução | 1 dia; resultados de reprodução atuam no tick seguinte |
| Experimento integrado | 365 dias, grade completa, 40 herbívoros e 4 predadores iniciais |
| Idade inicial da fauna | idade = maturidade do perfil; cooldown reprodutivo zerado |
| Estado inicial de sementes/detritos | zero; N de SOM = `SOM0 × fração de N orgânico` |

`tick=0` é o estado inicial à meia-noite; campos derivados são avaliados nesse instante sem consumir fluxos. A primeira janela de forçante cobre 00:00–01:00. Em cada `tickIndex>0` divisível por 6, processar a hora concluída `absoluteHour=tickIndex/6−1`, com hora solar central `h=(absoluteHour mod 24)+0.5` e dia `d=1+(floor(absoluteHour/24) mod 365)`. Cada 144º tick conclui o dia; nesse limite executar os processos diários uma vez. Se coincidirem hora/dia, aplicar a hora ambiental, ação animal daquele tick e depois fases diárias conforme [07_SCHEDULER](07_SCHEDULER.md).

Gerar terreno, colocação inicial e eventos climáticos com a função baseada em contador (counter-based) abaixo e tags separadas. A seed é um inteiro sem sinal de 64 bits. Todas as operações de `mix64` usam aritmética unsigned módulo 2⁶⁴:

```
mix64(x):
  z = x + 0x9E3779B97F4A7C15
  z = (z xor (z >> 30)) × 0xBF58476D1CE4E5B9
  z = (z xor (z >> 27)) × 0x94D049BB133111EB
  return z xor (z >> 31)

u01(seed, tag, subjectId, targetId, absoluteHourOrTick, drawIndex):
  a = mix64(seed xor tag)
  b = mix64(a xor subjectId)
  c = mix64(b xor targetId)
  d = mix64(c xor absoluteHourOrTick)
  r = mix64(d xor drawIndex)
  return double(r >> 11) / 9007199254740992.0
```

Tags L0 fixas: terreno=1, colocação de plantas=2, clima=3, colocação de animais=4, predação=5, reprodução=6. `subjectId` e `targetId` são inteiros estáveis (`targetId=0` quando não há segundo objeto); nenhum texto ou ordem de coleção participa do cálculo. Vetor de conferência: `(seed=1, tag=3, subjectId=0, targetId=0, absoluteHour=0, drawIndex=0)` retorna `r=0x7BD5A964748A48D8`, `u=0.48372896864433657`; `drawIndex=1` retorna `r=0xE44005EC9AA4A02C`, `u=0.89160191561190283`. Guardar seed, parâmetros completos, versão dos parâmetros e tempo simulado. Uma mudança de regra ou valor que altere o estado incrementa a versão do cenário. A simulação não varia com FPS, câmera ou carregamento de chunks visuais.

### Topografia e habitats

Para coordenadas inteiras `x,y∈{0,…,63}`, ponto central `c=31.5` e perturbação estável `ε(x,y)∈[−2,2] m` derivada de `ε=4u−2`, com `tag=terreno`, `subjectId=y×64+x`, `targetId=0`, `tick=0`, `drawIndex=0`:

`z(x,y) = 25 + 0.06 × ((x−c)²+(y−c)²) + ε(x,y)` metros.

É uma depressão central em relevo estático, com bordas hidrológicas abertas conforme a regra de escoamento, com diferença de altitude aproximada de 120 m e perturbação determinística. Células com `z < 75 m` usam perfil de solo *loam*; as demais, *sandy*. Colocar gramínea em 55% das células baixas e 30% das altas; colocar grupo lenhoso em 8% das baixas e 20% das altas. Para cada perfil usar `u01(seed,2,y×64+x,profileId,0,0)` (`profileId=1` gramínea, `2` lenhoso); criar patch somente se `u<p`, com `p=0.55`/`0.30` para gramínea e `0.08`/`0.20` para lenhoso em célula baixa/alta. Se criado, iniciar `B=B0`, `Nplant=qN×B0`, `Z=NZ=0`. As proporções servem para produzir dois habitats contrastantes e são heurísticas.

Animais recebem posições iniciais válidas em `[0,640) m` por coordenada, com `tag=colocação de animais`, `subjectId=EntityId`, `targetId=0`, `tick=0`, `drawIndex=0` (leste) e `1` (norte). Idade inicial é igual à maturidade e cooldown é zero; IDs começam em 1: herbívoros 1–40, predadores 41–44; novos IDs seguem em ordem crescente e tentativas diárias usam ordem de ID. Não gerar organismos para repor mortes.

## Equações do ambiente

As funções `clip(x,a,b)=min(b,max(a,x))` e `pos(x)=max(0,x)` são aplicadas nos limites indicados; grandezas inválidas/NaN/Infinity abortam o tick com diagnóstico. Armazenar estoques e acumular balanços em `double`; unidades de matéria seca são massa equivalente, não análise de carbono elemental. Converter graus para radianos antes de calcular seno/cosseno.

### Sol, radiação e temperatura

`h` é hora solar local; o cenário não define longitude ou fuso. Cada estado horário representa o centro do intervalo. Com dia do ano `d∈{1,…,365}`, hora solar `h∈[0,24)`, latitude `φ=45°`:

```
r(d)      = 1 + 0.033 cos(2π(d−3)/365)
δ(d)      = 23.44° sin(2π(d−80)/365)
ω(h)      = 15°(h−12)
sin α     = sinφ sinδ + cosφ cosδ cosω
Rtop      = 1361 r(d) pos(sin α)                         [W/m²]
C         = clip(0.20 + 0.65 exp(−hoursSinceLocalRain/4), 0, 0.85)
Rclear    = Rtop (1 − 0.65 C)
```

`C` é uma cobertura de nuvens heurística por zona climática; `hoursSinceLocalRain` persiste por zona, começa em 24 h, aumenta uma hora após cada janela sem chuva e volta a zero quando há chuva. Usar o valor no início da janela para radiação; a chuva recém-sorteada só altera a cobertura da próxima hora. Não é vapor atmosférico nem estoque de água. `Rclear` é radiação solar atenuada por nuvens; o nome interno pode ser `surfaceShortwave`, desde que a documentação deixe claro que radiação de onda longa não está modelada.

L0 usa o vetor solar local para avaliar a orientação do terreno, sem horizonte distante ou sombra projetada. Com leste, norte e cima como eixos locais:

```
sE = −cosδ sinω
sN = cosφ sinδ − sinφ cosδ cosω
sU = sinα
n = normalize(−∂z/∂E, −∂z/∂N, 1)
Fterrain = clip(max(0, nE sE + nN sN + nU sU) / max(sU, 0.05), 0, 1.20)
Rsurface = Rclear × Fterrain
```

`n` é a normal à superfície obtida dos gradientes centrais da elevação, com diferenças laterais de 10 m; na borda, usar a diferença disponível. À noite (`sin α≤0`), `Rsurface=0`. L0 trata toda energia solar como feixe direto equivalente, sem radiação difusa ou de onda longa. Energia por hora: `Hhour[MJ/m²] = Rsurface[W/m²] × 0.0036 × Δh`, com `Δh=1` hora. Somar 24 horas para `Hday`.

```
Tseason(d,z) = 11 + 12 cos(2π(d−200)/365) − 0.0065 z   [°C]
Tair(d,h,z) = Tseason(d,z) + 5 cos(2π(h−15)/24)       [°C]
RHproxy     = clip(0.35 + 0.45 C + 0.15 S/Smax, 0.20, 0.95)
```

`RHproxy` é somente um índice para inspeção; não fecha balanço de vapor e não força precipitação. O perfil térmico anual e diário é sintético. Nuvens e chuva não alteram temperatura nesta versão.

### Chuva determinística

A grade climática é 4 × 4 zonas; cada zona cobre 16 × 16 células físicas (160 m de lado). Seu ID estável é `tileY×4+tileX`, com `tileX=floor(x/16)` e `tileY=floor(y/16)`. A chuva da hora é compartilhada pela zona, evitando eventos independentes célula a célula.

```
fseason(d) = 1 + 0.40 cos(2π(d−20)/365)
pwet(d)    = 0.045 × fseason(d)
P = 0 mm, se u0 ≥ pwet
P = min(12 mm, −1.5 mm × ln(1−u1)), se u0 < pwet
```

`u0,u1∈[0,1)` vêm da função fixa acima com `tag=clima`, `subjectId=climateTileId`, `targetId=0`, hora absoluta e `drawIndex=0` para `u0`, `1` para `u1`. A média condicional antes do limite é 1.5 mm por hora chuvosa. É chuva prescrita de fronteira, com expectativa média bruta de cerca de 590 mm/ano antes do truncamento; não é uma previsão meteorológica. Chuva entra uma vez na hidrologia da hora correspondente, ainda que seis ticks animais ocorram dentro dela.

### Água do solo e superficial

Estoques hídricos usam milímetros por célula; fluxos são acumulados no intervalo. `1 mm × 1 m² = 1 L`; portanto, `1 mm × 100 m² = 100 L` nesta grade. Tabela de parâmetros:

| Perfil | `Smax` (mm) | `Swilt` (mm) | `S0` (mm) | `W0` (mm) | `Ksat` (mm/h) |
|---|---:|---:|---:|---:|
| *sandy* | 100 | 20 | 55 | 0 | 20 |
| *loam* | 180 | 35 | 90 | 0 | 10 |

`S` é água no solo, `W` água superficial e `P` chuva; `S0` e `W0` são os estoques no início do cenário. Cobertura de dossel `Cplant` vem dos patches; interceptação instantânea L0, sem estoque de água na copa:

```
Pint = 0.15 × Cplant × P                         # evaporação na fronteira
Pthrough = P − Pint
Wavailable = W + Pthrough
I = min(Wavailable, Ksat × 1 h, Smax − S)
S += I
W = Wavailable − I
```

No mesmo intervalo, distribuir `ET0hour` entre água aberta, solo exposto e vegetação por `Cwater=clip(W/10,0,1)`:

```
qwater = Cwater
qwoody = Cwoody × (1−Cwater)
qgrass = Cgrass × (1−Cwoody) × (1−Cwater)
qplant = qwoody + qgrass = Cplant × (1−Cwater)
qsoil = (1−Cplant) × (1−Cwater)
ET0day = clip(2.5 × pos(Tmean+5)/20 × Hday/12, 0, 5)  [mm/d]
ET0hour = ET0day × Hhour/Hday                             [mm na janela horária]
Ewater  = min(W, ET0hour × qwater)
Esoil   = min(pos(S−Swilt), ET0hour × qsoil)
W -= Ewater
S -= Esoil
Dplant,total = ET0hour × qplant
```

`Tmean=Tseason(d,z)` é a média analítica da curva diária (o cosseno horário tem média zero). Como a forçante climática é determinística por seed, gerar no início do dia as 24 janelas de chuva/cobertura e calcular `Hday` antes dos subpassos; isso não expõe previsão ao jogador nem permite feedback futuro de organismos. Distribuir `ET0hour=ET0day×Hhour/Hday`; a soma das 24 parcelas é `ET0day=clip(2.5×pos(Tmean+5)/20×Hday/12,0,5)` mm/d. Se `Hday=0`, ambas as demandas são zero. `ET0day` é demanda empírica L0, não FAO-56 Penman–Monteith; a formulação padrão FAO exige variáveis meteorológicas que aqui são proxy ou não modeladas. Sol, clima e evapotranspiração não têm calibração de campo.

Após infiltração, `Sbefore` é o solo no início da partição evaporativa. Calcular `Esoil` e então `Savailable=pos(Sbefore−Esoil−Swilt)`. Para perfis vegetais `i`, `Dwater_i=ET0hour×q_i`; se `ΣDwater=0` na célula, conceder zero; caso contrário `grant_i=Dwater_i×min(1,Savailable/ΣDwater)` para todos os perfis daquela célula. Aplicar o débito total uma vez. `fgrantWater_i=1` se `Dwater_i=0`, senão `min(1,grant_i/Dwater_i)`. Calcular `favailable=clip(Savailable/(0.25×(Smax−Swilt)),0,1)` e `fW_i=min(favailable,fgrantWater_i)`. À noite `ET0hour=0`, logo não há transpiração nem fotossíntese.

Escoamento superficial é difusão conservativa em vizinhança D8. `dx=10 m`, `Dsurf=2 m²/h`, `a=min(0.02,Dsurf Δh/dx²)` (L0: `a=0.02`) e `H_i=z_i+W_i/1000` em metros. Para cada par não ordenado de células vizinhas, a proposta de lâmina em direção à célula de menor carga é:

`qij = a × 1000 × max(0, H_i−H_j) mm por intervalo de 1 h`.

Converter `qij` em volume multiplicando pela área da célula. Calcular todos os fluxos do mesmo snapshot; se a soma de saídas propostas exceder `W_i`, escalá-las proporcionalmente até `W_i`. Para cada direção D8 ausente na borda, usar célula fantasma com `W=0` e elevação igual à célula de borda, propondo saída `a×W_i` mm/h naquela direção; escalar em conjunto com as demais saídas. Aplicar débitos/créditos simultaneamente. Água que cruza a borda é saída de fronteira; depressões retêm água. Não há rios permanentes em canal, aquífero, percolação profunda ou retorno de água subterrânea no L0.

**Balanço da água:** `Δ(surface + soil + água corporal animal) = chuva − interceptação − evaporação superficial − evaporação do solo − transpiração − saída na borda − excreção animal`; escoamento entre células e bebida `surface→animal` cancelam no total. Água corporal vegetal não é estoque persistente.

## Vegetação

Estado de cada patch é biomassa seca equivalente `B` e N `Nplant`, em g/m², mais banco `Z` e `NZ` na mesma unidade por área. O total de alimento por célula é o valor por m²×100 m²; consumo animal é em gramas. Detritos e SOM também são g/m²; pools animais são gramas e são divididos por 100 m² ao entrar em pool de célula. Posição e deslocamento do agente movem junto `M`, `R`, N e água corporal entre células, sem alterar seus totais. N é também uma trilha composicional dentro da biomassa; **não somar o ledger de N ao total de matéria seca**. A matéria seca de crescimento é uma conta equivalente de biomassa e sua fonte de carbono é fronteira.

| Perfil | `B0` (g/m²) | `Bmax` (g/m²) | `B50` (g/m²) | `RUE` (g/MJ PAR) | `qN` (gN/gDM) | `KN` (gN/m²) | T mín/ótima/máx (°C) | Demanda hídrica |
|---|---:|---:|---:|---:|---:|---:|---|---:|
| Gramínea | 100 | 600 | 200 | 0.80 | 0.018 | 0.50 | −2 / 20 / 38 | por dossel |
| Lenhoso (árvore/arbusto) | 250 | 1800 | 500 | 0.55 | 0.012 | 0.30 | −10 / 18 / 36 | por dossel |

`RUE` é eficiência L0, inferior a valores frequentemente medidos em culturas; não interpretar como valor da espécie real. PAR é 0.48 da radiação solar de onda curta. Usar partição de luz em ordem lenhoso → gramínea:

```
C_i       = 1 − exp(−B_i/B50_i)
Cplant    = 1 − (1−Cgrass)(1−Cwoody)
qwater    = Cwater
qsoil     = (1−Cplant) × (1−Cwater)
PARwoody  = 0.48 × Hhour × Cwoody          [MJ PAR/m² por janela]
PARgrass  = 0.48 × Hhour × Cgrass × (1−Cwoody) [MJ PAR/m² por janela]
qwoody    = Cwoody × (1−Cwater)
qgrass    = Cgrass × (1−Cwoody) × (1−Cwater)
T         = Tair(d,h,z)
fT        = 0                                  para T ≤ Tmin ou T ≥ Tmax
fT        = (T−Tmin)/(Topt−Tmin)              para Tmin < T ≤ Topt
fT        = (Tmax−T)/(Tmax−Topt)              para Topt < T < Tmax
fNavail   = Nmin/(Nmin+KN)
fN        = fNavail
fW        = min(favailable, fgrantWater)
Gcandidate= min(Bmax−B, RUE × PAR_i × fT × fW × clip(1−B/Bmax,0,1)) [gDM/m² por janela]
Nrequest  = qN × Gcandidate × fNavail               [gN/m² por janela]
G         = min(Gcandidate × fNavail, Ngrant_i/qN)   [gDM/m² por janela]
```

`favailable`, `fgrantWater`, `fNavail` e `fN` ficam entre 0 e 1. Solicitar `Nrequest`, atender proporcionalmente entre perfis por célula usando o mesmo snapshot e liberar N não incorporado como Nmin. Para cada perfil, `Nneed_i=Nrequest_i`; se `ΣNneed=0` na célula, conceder zero; caso contrário `Ngrant_i=Nneed_i×min(1,Nmin/ΣNneed)` para todos os perfis da célula. Subtrair a soma concedida de `Nmin` uma vez. `Ngrant_i` é gN/m² por janela; usar `qN×G` na biomassa e devolver excesso a Nmin. A concessão é por célula: o estado final é `Sfinal=Sbefore−Esoil−Σgrant_i`, onde `grant_i` aqui é exclusivamente a concessão hídrica em mm. Esse estado substitui o saldo provisório; não debitar os mesmos fluxos novamente. A transpiração efetiva é `Σgrant_i`, mesmo se N limitar o crescimento; demanda evaporativa e incorporação de biomassa são processos distintos. Uma janela L0 é exatamente 1 h. A produção horária atualiza `B` e sementes uma vez por janela. Se `Nmin=0`, `ΣNneed=0`, então a concessão é zero sem dividir por zero; `G=0`. A demanda hídrica usa `Dwater_i=ET0hour×q_i` (mm); as frações `q_i` somam `qplant=Cplant×(1−Cwater)`. Assim, a soma das demandas vegetais não excede a parcela `qplant` de ET0 e falta completa de água/N ou luz impede crescimento.

Da produção `G`, destinar `seedFrac×G` ao banco de sementes (`seedFrac=0.02` gramínea; `0.01` lenhoso) e o restante à biomassa; repartir o N na mesma proporção `qN`. Germinar uma vez ao dia, com `Δd=1`, fração `1−exp(−0.005 fT fW Δd)` do banco presente no início desse dia, limitada ao saldo atual do banco e ao espaço até `Bmax` após o crescimento horário; usar médias diárias de `fT` e `fW`. Matéria/N não germinados permanecem no banco. Calcular esses fatores também onde `B=0`, para permitir colonização. Transferir matéria e N, sem criação. Mortalidade e senescência são diárias:

```
hplant = 0.001 + 0.03 pos(0.25−fW)/0.25 + 0.03 pos(0.25−fN)/0.25 + 0.05 pout [d⁻¹]
Bdead = B × (1−exp(−hplant Δd))
Bturn = (B−Bdead) × (1−exp(−0.0005 Δd))
```

`fW` e `fN` são médias das 24 frações horárias; `pout` é a fração das horas em que `T≤Tmin` ou `T≥Tmax`. Aplicar senescência uma vez por dia após a última hora do dia.

Transferir `Bdead+Bturn` e seu N para detritos uma vez; reduzir biomassa. Banco perde `1−exp(−0.001 Δd)` por inviabilidade e essa matéria/N passa a detritos. Dispersão L0: 20% das sementes novas distribui-se igualmente aos vizinhos D8 válidos; o restante fica no banco local, sem exportação pela borda. Aplicar dispersão horária simultaneamente, transferindo também `qN` por grama de semente; sementes produzidas/recebidas no dia só germinam a partir do dia seguinte. Ao receber sementes em célula sem patch desse perfil, criar apenas o registro vazio (`B=Nplant=0`) e creditar `Z,NZ` recebidos, sem aplicar `B0`. Guardar no save o saldo de sementes elegíveis e os acumuladores diários. Ordem diária vegetal: germinação, mortalidade, senescência e inviabilidade do banco remanescente, sem segunda germinação. Não há folhas/raízes individuais.

A forma de produção proporcional à radiação interceptada tem fundamento em modelos simples de eficiência do uso da luz; curvas térmicas, limites, RUE e frações desta tabela são hipóteses de jogo e precisam de testes direcionais, não calibração. Fontes e limites estão listados ao fim.

## Animais, energia e interação

IDs estáveis, posição contínua em metros, massa estrutural seca `M` e reserva energética seca `R` em gramas. `R` não contém N; o N corporal estrutural é `qN×M`. Energia armazenada é derivada: `Ereserve[MJ]=0.018 MJ/g×R`. `M+R` é a massa seca total; energia não se soma à massa. Água corporal é em litros, idade e déficits em dias. A reserva inicial é `0.10×M`, limite `Rmax=0.20×M`. Inicializar água no alvo `0.65 L/kg×(M+R)/1000`, com `M+R` em gramas e água em litros. Movimento usa velocidade média efetiva, não velocidade máxima.

| Perfil | `M0` estrutural (g DM) | `R0` | `Mmax` estrutural (g DM) | Ingestão-alvo (g DM/d por g de corpo) | `ηdry` assimilação seca | `ηN` assimilação de N | `qN` estrutural | Velocidade efetiva | Maturidade | Cooldown reprodutivo | Massa do filhote |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Herbívoro | 5000 | `0.10×M0` | 8000 | 0.025 | 0.45 | 0.65 | 0.020 | 0.02 m/s | 180 d | 30 d | 5% de `M+R` parental |
| Predador | 12000 | `0.10×M0` | 20000 | 0.015 | 0.70 | 0.80 | 0.025 | 0.05 m/s | 365 d | 60 d | 5% de `M+R` parental |

Os tamanhos e taxas são perfis funcionais inventados para jogabilidade, não animais reais. `M0` inicial é massa estrutural; N inicial é `qN×M0`; `R0=0.10×M0` não contém N. As colunas ηdry e ηN são as eficiências usadas no orçamento alimentar.

- A cada tick, percepção local de raio `30 m`; buscar vizinhos em índice espacial, nunca varrer o mapa todo por indivíduo. Seguir a ordem de fases de [07_SCHEDULER](07_SCHEDULER.md): pedidos e distâncias usam o snapshot do início do tick, capturas são resolvidas antes de ações das vítimas e o deslocamento não cria alimentação no destino antes do próximo tick. Herbívoro escolhe patch com maior `biomassa comestível/(distância+10 m)`; gramínea tem fração comestível 0.80 e grupo lenhoso 0.15. Predador escolhe a presa mais próxima. Empates: menor ID estável.
- Deslocar em direção ao alvo por `min(speed×600 s, distância ao alvo)`, respeitando a borda; sem teleporte e sem pathfinding global. O alvo vegetal fica no centro da célula do patch. Se não houver alvo válido no snapshot, ficar no lugar; não reescolher após commits concorrentes. A percepção só orienta movimento: herbivoria exige que animal e patch estejam na mesma célula física no início do tick.
- Herbívoros propõem no máximo `0.025 gDM/(g corpo·d)×(M+R)×Δd` gramas de matéria seca por janela. A fração comestível (0.80 gramínea, 0.15 lenhoso) multiplica biomassa/N do patch antes do rateio. Converter estoque comestível g/m² para gramas por célula pela área; ratear pedidos simultâneos proporcionalmente, limitar pelo estoque, e confirmar o débito uma vez. Quota não atendida não acumula para o tick seguinte.
- Predador a até 10 m da presa no snapshot de início do tick tem probabilidade de captura `p=1−exp(−0.80 Δd)`, com `Δd=1/144` dia/tick. Sorteio usa `tag=predação`, `subjectId=predatorId`, `targetId=preyId`, `tick` absoluto, `drawIndex=0`; conflitos resolvidos por menor ID de predador e uma morte por presa. Cada predador pode realizar no máximo uma captura bem-sucedida por dia simulado. Persistir `lastCaptureDay`, inicial e neonatal `−1`; comparar com `floor((tickIndex−1)/144)` e atualizar somente no commit da captura. Assim, o tick que encerra o dia ainda usa a cota daquele dia. Ao capturar, a ingestão `I=min(0.015 gDM/(g corpo·d)×(Mpred+Rpred)×1 d, Mprey+Rprey)` usa uma cota diária (não uma cota de 10 minutos). Retirar a fração `s=I/(Mprey+Rprey)` de `Mprey`, `Rprey` e `Nprey=qNprey×Mprey`; `Nintake=s×Nprey`. Todo o remanescente, N correspondente e resíduos da ingestão entram em detritos. Cancelar pedidos da vítima após morte.
- Alimento retirado `I` divide-se em `fecesDry=(1−ηdry)I` e `assimilatedDry=ηdry I`, com `ηdry` da tabela. N acompanha o alimento proporcionalmente, partindo-se por `ηN` da tabela: `Nfeces=(1−ηN)Nintake` entra em detritos, `Nass=ηN×Nintake`. Registrar cada transferência uma vez.

Manutenção simplificada, com `m=(M+R)/1000` kg e `Δd` em dias:

```
Emaint = 0.30 MJ/(kg^0.75·d) × m^0.75 × Δd [MJ]
Emove  = 0.00001 MJ/m × distância_m    [MJ]
Gresp  = (Emaint+Emove)/0.018            [g DM equivalentes]
```

Aplicar o orçamento nesta ordem, em gramas secas equivalentes: `Gfood=min(Gresp,assimilatedDry)`, `Greserve=min(R,Gresp−Gfood)`, `GrespActual=Gfood+Greserve`, `maintenanceDeficit=Gresp−GrespActual`, `R-=Greserve`, `A=assimilatedDry−Gfood`, `ΔM=min(A,Mmax−M,Nass/qN)`, `A-=ΔM`, `M+=ΔM`, `ΔR=min(A,max(0,0.20×M−R))`, `R+=ΔR`, `detritusDry+=A−ΔR`, `Nmin+=Nass−qN×ΔM`. A transferência respiratória de fronteira é `GrespActual`; se `maintenanceDeficit>0`, acumular `Δd` no contador de déficit; caso contrário, zerá-lo. Aumentar `M` antes de calcular o limite de reserva. Morte por inanição após 3 dias consecutivos com manutenção não atendida, desidratação após 2 dias com hidratação abaixo de 40% do alvo, ou idade máxima de 3650 dias. Cadáver seco restante e N estrutural vão a detritos; água corporal restante vai à água superficial uma vez.

Uma vez ao dia, cada animal perde `min(água corporal, 0.08 L/kg/d×(M+R)/1000)` litros por excreção na fronteira e solicita água superficial suficiente para voltar ao alvo `0.65 L/kg×(M+R)/1000` litros. Pedidos na mesma célula física de 100 m² são simultâneos e proporcionais: `grant_i=request_i×min(1,Wliters/Σrequest)` (zero se a soma for zero), onde `Wliters=100×W_mm`; converter os litros concedidos de volta para mm dividindo por 100 L/mm por célula. Água contida no alimento é omitida. Usar `request_i=max(0,alvo−água corporal após excreção)`; redução do alvo não pode gerar pedido negativo. A desidratação é medida após a concessão; abaixo de 40% do alvo soma um dia ao contador, caso contrário zera.

Na fase diária animal, transferir água, resolver mortes por desidratação e então testar reprodução dos sobreviventes em ordem de ID, usando as fases de `07_SCHEDULER`. Idade/cooldown são atualizados exclusivamente por tick; decomposição ocorre depois dos depósitos confirmados. A tentativa diária de reprodução tem probabilidade `1−exp(−0.01)` com `tag=reprodução`, `subjectId=parentId`, `targetId=0`, dia absoluto e `drawIndex=0`; executar uma tentativa por adulto/dia em ordem de ID crescente. Só ocorre se adulto, cooldown vencido, `R≥0.10×M` e a transferência atômica é possível: `Mchild=0.05×Mparent`, `Rchild=0.05×Rparent`, `Nchild=qN×Mchild`; água corporal `0.65 L/kg×(Mchild+Rchild)/1000` litros. Debitar exatamente esses valores do progenitor, conservando a proporção entre estrutura/reserva e o limite `R≤0.20×M` em ambos. O filhote começa com idade zero, cooldown zero e os contadores de déficit zerados, na posição do progenitor. Exigir saldos não negativos e hidratação parental pós-transferência ≥40% do novo alvo; gravar cooldown parental apenas se todos os saldos passarem. O recém-nascido atua no tick seguinte. É reprodução assexuada artificial, sem inferência sobre dinâmica de espécie real.

`Emaint`, ingestão-alvo e assimilação formam um orçamento fictício autocontido. A conversão `18 kJ/g` é valor operacional constante. A energia serve para esforço e reserva da fauna; não alegar balanço termodinâmico do mundo. Movimento e caça são regras locais, sem resposta funcional populacional nem inteligência adaptativa. Idade avança por `Δd` em cada tick; cooldown reprodutivo decresce pelo mesmo intervalo até zero.

## Detritos, decomposição e nitrogênio

Inventários L0 por célula: água superficial/solo; matéria seca equivalente em plantas/banco, animais, detritos e matéria orgânica do solo; N mineral do solo, N orgânico, N de plantas/sementes, N animal e N de detritos. N é composição dos estoques e um ledger químico separado; os totais de matéria seca e N não devem ser somados entre si. N inicial e fontes são declarados; não há fixação biológica, deposição atmosférica, lixiviação ou N atmosférico no L0.

| Solo | Matéria orgânica seca `SOM0` | Fração de N orgânico | `Nmin0` |
|---|---:|---:|---:|
| *sandy* | 500 g/m² | 1.0% | 2.0 gN/m² |
| *loam* | 1500 g/m² | 1.5% | 4.0 gN/m² |

Mortalidade, senescência, sementes inviáveis, fezes e cadáveres só entram em detritos após débitos reais. Há um pool de detrito por célula, em g/m². O N de cada animal é `qN×M`; a reserva `R` não contém N. Para a fração diária usar temperatura média diária e `S` ao fim do dia. Decaimento de primeira ordem, calculado pela solução exata (não Euler, que poderia superconsumir o pool):

```
fTdec = clip(2^((Tsoil−20)/10), 0.25, 2.50)
fWdec = clip((S−Swilt)/(0.5(Smax−Swilt)), 0, 1)
kL    = 0.008 × fTdec × fWdec       [d⁻¹]
fL    = 1−exp(−kL Δd)
decayedDry = detritusDry × fL
decayedN = detritusN × fL
```

Transferir 25% da matéria seca decomposta à matéria orgânica do solo e 75% à fronteira como respiração simplificada. Do N decomposto, transferir 70% para N orgânico do solo e 30% para N mineral; a partição é um parâmetro L0, independente da partição de matéria seca. N orgânico do solo também decai:

```
kSOM = 0.0004 × fTdec × fWdec    [d⁻¹]
fSOM = 1−exp(−kSOM Δd)
SOMloss = SOM × fSOM
Nrelease = soilOrganicN × fSOM
```

Calcular perdas de detritos e SOM a partir do mesmo snapshot de início da fase diária, já incluindo depósitos anteriores de organismos; SOM recém-formada não decai novamente nessa fase. Debitar `decayedDry,decayedN,SOMloss,Nrelease` uma vez e aplicar os créditos correspondentes. `SOMloss` sai como respiração simplificada e `Nrelease` passa a `Nmin`; nenhum dos dois é contado novamente por Ecology. Temperatura do solo é `Tsoil=Tair`; retardamento térmico em profundidade não modelado. O modelo de decomposição usa forma de primeira ordem, mas seus coeficientes/fatores e partições não estão calibrados para local real. `Tsoil` diário é a média de `Tair`; `fWdec` usa o estoque `S` ao fim do dia.

**Balanço N local:** absorção solo→planta, alimento planta→animal, N fecal→detritos, N assimilado não incorporado→Nmin, cadáveres e decomposição são transferências internas; movimento animal é transferência entre células; sementes não são exportadas pela borda. Soma global final de N = inicial (sem fluxos de fronteira de N). Plantas recebem matéria seca/carbono equivalente da fronteira e N do solo. Não inferir massa de CO₂ a partir dessa conta simplificada.

## Tolerâncias e testes numéricos de aceite

Para cada transferência `j`, reportar `e = estoqueFinal−estoqueInicial−Σentradas+Σsaídas`. Definir `τ = max(τabs, 10⁻⁹×Σ|fluxos|)` antes do teste. Usar acumuladores `double`, ordem estável e inventários com unidades uniformes.

| Livro / invariant | `τabs` por commit | Limite relativo |
|---|---:|---:|
| Água, por célula, litros | 10⁻⁶ L | 10⁻⁹ |
| Matéria seca equivalente, gramas | 10⁻⁶ g | 10⁻⁹ |
| Nitrogênio, gramas | 10⁻⁸ gN | 10⁻⁹ |
| Orçamento de energia animal, joules equivalentes | 10⁻³ J | 10⁻⁹ |
| Posição/velocidade/tempo | posição 10⁻⁹ m; hora/tick inteiro exato | — |

Resíduo acima de `τ` reprova o commit/teste e preserva diagnóstico. Não clampar déficit ou estoque negativo para fazê-lo passar; rejeitar estado negativo e investigar origem. Para totais anuais, somar o resíduo do livro e definir `Rref=max(|estoqueInicial|,|estoqueFinal|,Σ|fluxos de fronteira|)`; exigir `|resíduo|≤max(τabs,10⁻⁸×Rref)` após redução ordenada. Fluxos internos cancelam e não inflacionam esse denominador. Determinismo canônico no mesmo build/runtime/arquitetura: hash do estado final idêntico para seed, configuração, comandos e ticks iguais. Comparação entre arquiteturas distintas, se futuramente necessária, usar `max(τabs,10⁻⁹×max(|a|,|b|))` por campo e validar transições direcionais; para totais anuais nulos ou abaixo de `τabs`, aplicar apenas o limite absoluto. Não prometer bits iguais entre plataformas.

| Teste fechado antes de implementar | Aceite esperado |
|---|---|
| Fórmulas solares em noite/meio-dia, solstício e latitude-equador/polo; `u01` contra vetores acima | nenhuma radiação negativa; noite exatamente zero; inclinação anual reproduzível; saídas aleatórias idênticas |
| Injetar 12 mm numa hora numa célula de 100 m² (fixture, no limite da chuva L0), solo saturado | entrada 1200 L; interceptação + infiltração + água superficial fecha; não replicar precipitação em cada tick de 10 min |
| Água empoçada sem chuva, com capacidade no solo | infiltração retira `I` da superfície e credita o solo; `Sfinal=Sbefore−Esoil−Σgrant_i` conserva água |
| Relógio por 144 ticks e save/load perto da meia-noite | idade aumenta exatamente 1 dia; cooldown cai 1 dia; cota de captura não reinicia com save/load |
| Nascimento com reserva parental no limite | massa/N/água conservados; `R≤0.20×M` em progenitor e filhote |
| Sementes em célula sem patch e alvo vegetal em célula vizinha | germinação cria biomassa apenas por transferência; percepção distante não permite comer antes de chegar |
| Duas células e borda da grade | transferência interna cancela no balanço global; água de saída aparece como sink de fronteira |
| Solo com `S=Swilt`, sem reserva de água/N, noite ou T fora da faixa | crescimento exatamente zero; sem recursos negativos |
| Pleno sol versus exatamente 50% de PAR, demais parâmetros iguais e sem limite de B/N/água | produção à meia luz = 50% do controle dentro da tolerância numérica |
| `Nmin=0` versus N suficiente, demais parâmetros iguais | crescimento sem N igual a zero; demanda atendida não supera saldo |
| Seca partindo de solo saturado, sem chuva nem entrada lateral | concessões não superam o saldo; crescimento só deve cair quando o estoque utilizável limitar água, não imediatamente por falta de chuva |
| Duas plantas requisitam mais que o mesmo saldo | soma concedida ≤ saldo; permutar ordem de entrada não altera resultado canônico |
| Herbívoro sem alimento e animal alimentado | reserva nunca é negativa; condição sem alimento reduz energia disponível e pode morrer após o limite declarado |
| Predação concorrente e consumo | no máximo uma morte por presa e uma captura por predador/dia; cota diária, resíduos e cadáver fecham massa e N |
| Sementes, nascimento e save/load | nenhum material/N/água aparece; reprodução rejeitada não debita; continuação pós-save tem hash igual |
| Detrito úmido/quente versus seco/frio | fração de decaimento segue `kL`; cada parcela transferida uma vez e sem N negativo |
| Três seeds no cenário de 365 dias | cadeia ambiente→planta→herbívoro→predador→detrito→N observável; extinção explicada pode ocorrer e não é corrigida por respawn |

Os limites de 40 herbívoros e 4 predadores são condição do cenário e da medição, não metas populacionais. O gate integrado fica **inconclusivo** se a população desaparecer antes de ser possível observar uma cadeia, mesmo com balanços corretos; revisar parâmetros e repetir o experimento. Não converter isso em exigência de equilíbrio populacional.

## Metas de desempenho para N0–N4

Referência de medição disponível neste ambiente em 21/10/2026: Windows 11 Pro, Intel Core i3-10105 (4 núcleos/8 threads), 16 GiB RAM, NVIDIA GeForce RTX 5060. Registrar versão do SDK/runtime, build Release, hash, resolução, drivers, hardware, seed e configuração em cada medição; os valores abaixo são metas, **não resultados medidos**. Medir headless em CPU, sem log por evento/tick; medir desktop em 1280×720 com mapa 64×64 e camadas de inspeção.

| Marco | Meta objetiva |
|---|---|
| N0 | avançar 1000 ticks do boot headless em ≤5 s, após JIT/aquecimento; sem renderer |
| N3 headless integrado | seed 1, 64×64, 40 herbívoros + 4 predadores iniciais, reprodução ativa; ≥20 dias simulados/s, mediana de 3 execuções Release; cada janela cronometrada começa do estado inicial e termina em 365 dias (≤18.25 s) |
| N4 desktop | ≥30 FPS em média e P95 de quadro ≤33.3 ms, em janela contínua de 60 s; P99 ≤50 ms na mesma cena e máquina; seed 1 desde dia zero, após aquecimento separado, 6 ticks/s real (1 hora simulada/s), câmera fixa mostrando toda a grade e camada de água ativa |
| Memória | bytes privados estabilizados ≤512 MiB headless e ≤1 GiB desktop após carga de cenário |
| Persistência | salvar e carregar/validar estado integrado da seed 1 no dia 365 em ≤2 s cada; save do dia 365 ≤10 MiB, excluindo cache recriável de imagem |
| Integridade | nenhuma perda de tick/evento após pause/resume/save; carga não muda o hash canônico da continuação |

A meta principal mede a seed canônica 1 com reprodução ativa a partir dos 44 animais iniciais; a população pode variar e não existe teto oculto. Fazer uma execução de aquecimento separada, reiniciar do estado inicial antes de cada uma das três medições e registrar contagem máxima. Usar seeds 1–3 para o gate ecológico; uma fixture fixa de 44 sem reprodução serve apenas para diagnosticar custo. Medir Release quente e frio quando houver runtime; a meta de throughput usa execução quente e relata ambos. Sem cumprir o alvo, localizar primeiro a fase/processo pelo perfilador; não reduzir quantidade de células, agentes ou testes silenciosamente. Ajustar a meta apenas com registro de mudança, causa e cenário preservado.

## Limites que os primeiros experimentos devem avaliar

A cota de uma captura diária é uma regra artificial L0, não uma lei ecológica. A ingestão predatória concentrada em um tick, a prioridade de crescimento antes de recompor reservas e a ausência de busca dirigida por água podem provocar déficits e mortalidade mesmo com recursos no mapa. Água para beber é somente a da célula atual; não há acesso remoto nem respawn. Testar essas interações em N3 antes de aceitar o cenário: fechar os livros não demonstra sobrevivência, reprodução ou uma cadeia alimentar funcional. Primeiro ajustar as regras/parâmetros existentes, sem acrescentar espécies ou infraestrutura.

## Proveniência científica e interpretação

As fontes abaixo apoiam **formas de equação/processo**; os números do cenário e suas curvas continuam parâmetros heurísticos. As buscas do SciSpace ajudaram a localizar SPLASH, eficiência de uso de luz, balanço de água e formulações de decomposição.

- Davis et al. (2017), [SPLASH v1.0, Geoscientific Model Development](https://gmd.copernicus.org/articles/10/689/2017/): algoritmos simples para radiação, evapotranspiração, água no solo e escoamento. O mundo aqui não implementa SPLASH integral nem herda sua validação.
- Allen et al. (1998), [FAO Irrigation and Drainage Paper 56](https://www.fao.org/4/X0490E/X0490E00.htm): referência para evapotranspiração. FAO Penman–Monteith requer radiação, temperatura, vento e vapor; esta especificação escolhe um proxy L0 em vez de alegar implementar FAO-56.
- Kiniry et al. (1989), [radiation-use efficiency in five crop species](https://doi.org/10.1016/0378-4290(89)90023-3): sustenta a forma proporcional entre PAR interceptada e matéria seca em condições específicas de cultura. Não valida os grupos funcionais nem `RUE` L0 daqui.
- Bolker, Pacala & Parton (1998), [simplifying the Century decomposition model](https://doi.org/10.1890/1051-0761(1998)008%5B0425%3ALAOSDI%5D2.0.CO%3B2): mostra que decomposição de primeira ordem pode ser uma formulação parcimoniosa, com limites e estrutura do modelo. O artigo sobre LPJmL 5.0-tillage também descreve cinética de primeira ordem modulada por temperatura/umidade: [Lutz et al. (2019)](https://gmd.copernicus.org/articles/12/2419/2019/).
- A análise de atualização síncrona por Tan et al. (2021), já listada em [pesquisa](15_RESEARCH.md), informa o risco geral de causalidade em agentes; nossas regras de captura e commit são hipóteses que passam pelos testes deste documento.

Não temos calibração climática local, observações de espécie, dados de forrageio, taxas de metabolismo de animais definidos nem cenário geográfico real. Para qualquer alegação L1/L2, escolher um processo, obter fonte/dados próprios, registrar ajuste e sensibilidade, e mudar apenas o contrato/marco correspondente.
