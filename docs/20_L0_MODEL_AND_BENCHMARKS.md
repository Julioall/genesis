# Modelos L0, parâmetros, tolerâncias e metas de desempenho

> A implementação C#/.NET 10 correspondente a este cenário está incluída no checkout atual. Os benchmarks abaixo são registros históricos ligados a builds, commits, seeds, resoluções e hardware próprios; não são garantias gerais. O experimento de 256 × 256 do commit auditado `c6aecbc` deve ser separado do cenário canônico de 64 × 64 usado pelo checkout `dd2cf47`.

**Status:** contrato quantitativo L0 do runtime; N0–N3 têm validação documentada, enquanto N4 permanece aberto. Estes parâmetros não são calibração ecológica de campo. **Escopo:** mapa finito e processos necessários para clima, água, dois grupos vegetais, perfis funcionais de herbívoro/predador e decomposição. A física atmosférica, a fisiologia e os comportamentos são aproximações transparentes de jogo.

As métricas gráficas históricas deste documento descrevem builds do cliente MonoGame e são evidência do protótipo nesta máquina. Elas não comparam técnicas nem aprovam a direção visual final. O gate de apresentação compara as técnicas na mesma cena, máquina e metas dentro do MonoGame, conforme [22_VISUAL_DIRECTION_AND_TECHNOLOGY.md](22_VISUAL_DIRECTION_AND_TECHNOLOGY.md). Não alterar os contratos ou parâmetros científicos para otimizar uma comparação de renderer.

As equações abaixo são o contrato L0 para a implementação futura. Os valores são parâmetros iniciais de engenharia, não constantes universais nem previsões da Terra. Se um gate falhar, ajustar primeiro o processo responsável, registrar seed/configuração e atualizar a versão do cenário. Não acrescentar processos adiados para compensar um parâmetro ruim.

## Cenário e cadência de referência

| Parâmetro | Valor L0 |
|---|---:|
| `scenarioId` | `temperate-valley-l0-v17` |
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

O relevo v10 conserva o campo determinístico v9: base de 62 m, ruído em três escalas (amplitudes de 15, 8 e 2.8 m), uma baixada sinuosa larga e duas cordilheiras curvas que a acompanham por lados opostos. Cada cadeia usa ombros gaussianos de 4.0–5.6 células e uma crista estreita; uma máscara espacial segmenta os maciços e produz selas. As amplitudes são 100–114 m na cadeia principal e 78–88 m na secundária. A baixada reduz em 82% o ruído local, desce 0.42 m por célula no eixo norte-sul e recebe depressões gaussianas de 22 m (largura 13.5 células) e 8 m (largura 2.6 células). Pequenas cristas de 5/4 m quebram as transições. A seed usa tags distintas por escala/feição; a Geosfera calcula o campo nas 64 × 64 células. A malha gráfica é uma subdivisão do snapshot e não altera a resolução física. O planalto usa o contorno `r(θ)=1+0.12 sin(3θ+φ)+0.065 sin(5θ−0.63φ)+0.035 sin(7θ+0.41φ)+0.02 sin(11θ−0.27φ)`, onde `φ` é derivada da seed; sua extensão foi reduzida para formar um afloramento menor. Dentro do contorno, a altitude é o topo plano; fora dele, continua o campo de base. O topo parte de 94–102 m e sobe quando necessário para manter pelo menos 18 m acima do ponto mais alto do terreno-base amostrado no contorno, limitado a 190 m. O desnível segue consultado pelo campo físico e pela drenagem. Células com `z < 75 m` usam perfil de solo *loam*; as demais, *sandy*. Encostas mais íngremes expõem uma faixa maior da textura rochosa.
O relevo v10 conserva o campo determinístico v9: base de 62 m, ruído em três escalas (amplitudes de 15, 8 e 2.8 m), uma baixada sinuosa larga e duas cordilheiras curvas que a acompanham por lados opostos. Cada cadeia usa ombros gaussianos de 4.0–5.6 células e uma crista estreita; uma máscara espacial segmenta os maciços e produz selas. As amplitudes são 100–114 m na cadeia principal e 78–88 m na secundária. A baixada reduz em 82% o ruído local, desce 0.42 m por célula no eixo norte-sul e recebe depressões gaussianas de 22 m (largura 13.5 células) e 8 m (largura 2.6 células). Pequenas cristas de 5/4 m quebram as transições. A seed usa tags distintas por escala/feição; a Geosfera calcula o campo nas 64 × 64 células. A malha gráfica é uma subdivisão do snapshot e não altera a resolução física. O planalto usa o contorno `r(θ)=1+0.12 sin(3θ+φ)+0.065 sin(5θ−0.63φ)+0.035 sin(7θ+0.41φ)+0.02 sin(11θ−0.27φ)`, onde `φ` é derivada da seed; sua extensão foi reduzida para formar um afloramento menor. Dentro do contorno, a altitude é o topo plano; fora dele, continua o campo de base. O topo parte de 94–102 m e sobe quando necessário para manter pelo menos 18 m acima do ponto mais alto do terreno-base amostrado no contorno, limitado a 190 m. O desnível segue consultado pelo campo físico e pela drenagem. Células com `z < 75 m` usam perfil de solo *loam*; as demais, *sandy*. Encostas mais íngremes expõem uma faixa maior da textura rochosa.

O cenário v11 reduz o afloramento para raios de 2.9–3.8 células no eixo leste-oeste e 1.9–2.6 no eixo norte-sul. Para quebrar a silhueta elíptica, o contorno mantém as quatro harmônicas anteriores, adiciona componentes menores de frequência 13 e 19 e ruído de valor coerente com espaçamento de 2 células. A seed determina o detalhe; o topo segue plano e o paredão continua integrado ao campo-base e ao cálculo de drenagem.

O cenário v12 conserva o afloramento principal e acrescenta dois menores sobre as cadeias montanhosas, um a oeste e outro a leste. Os centros, raios e alturas são derivados da seed; seus raios nominais ficam entre 1.05 e 1.70 células. Cada topo permanece plano e tem ao menos 18 m de elevação física sobre o ponto mais alto do terreno-base no contorno. A Hydrosphere calcula a drenagem a partir do campo combinado, e o cliente desenha as três faces em buffers compartilhados.

O cenário v13 aumenta a ocupação lenhosa para 30% dos candidatos abaixo de 75 m, 62% na faixa de 75–125 m e 40% acima de 125 m (antes: 27%/40%/12%). O limite de inclinação sobe de 0.70 para 1.0 m/m para incluir os ombros das cordilheiras; células com água superficial continuam excluídas. A seleção espacial mantém manchas determinísticas por seed e deixa faces íngremes expostas.

A ocupação vegetal inicial usa três faixas (<75 m, 75–125 m e ≥125 m) e exclui encostas acima do limite do grupo: gramíneas usam frações de 55%/38%/12%, inclinação máxima 1.0 m/m e água superficial até 150 mm; lenhosas usam 27%/40%/12%, inclinação máxima 0.70 m/m e apenas células secas. Os valores são parâmetros heurísticos para formar prados, florestas de altitude média e cumes mais expostos. Para `profileId=1` (gramínea), calcular `score=0.58 n10+0.27 n4+0.15 u`, usando espaçamentos 10 e 4 células; para `profileId=2` (lenhosa), `score=0.52 n12+0.32 n4+0.16 u`, usando espaçamentos 12 e 4 células. `n12`, `n4`, `n10` e `u` são valores `SpatialUnit`/`u01` derivados da seed; `SpatialUnit` interpola suavemente valores de uma grade de espaçamento indicado. Em cada faixa, ordenar pelo score decrescente e pelo índice da célula crescente para desempate; selecionar exatamente `round(N×p)` células elegíveis. Isso preserva a fração por habitat e forma manchas reprodutíveis. Patches criados começam com `B=B0`, `Nplant=qN×B0`, `Z=NZ=0`; os parâmetros de biomassa permanecem inalterados.

No estado inicial, a Hydrosphere escolhe a descida D8 mais íngreme entre vizinhos, acumula células contribuintes em ordem altimétrica e armazena água nas células de canal e em até três depressões fechadas de maior contribuição. A borda da grade é saída aberta, não lago. Esse estoque de água faz parte do inventário em `tick=0`; ainda não há recarga perene de aquíferos nem erosão de leito.

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

| Perfil | `Smax` (mm) | `Swilt` (mm) | `S0` (mm) | `Ksat` (mm/h) |
|---|---:|---:|---:|---:|
| *sandy* | 100 | 20 | 55 | 20 |
| *loam* | 180 | 35 | 90 | 10 |

`S` é água no solo, `W` água superficial e `P` chuva. `S0` é a água inicial do perfil; `W` e o estoque subterrâneo `G` começam com a drenagem derivada da seed. Células de canal (`F≥12`, onde `F` é a área contribuinte em células) recebem `G0=2600 mm`; esse estoque e a água superficial entram no inventário em `tick=0`, sem serem recriados como fluxo. Cobertura de dossel `Cplant` vem dos patches; interceptação instantânea L0, sem estoque de água na copa:

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

Escoamento superficial é difusão conservativa em vizinhança D8; a direção de drenagem inicial é uma feição geográfica separada que orienta onde `W` é semeado. Em cada hora, cada célula de canal libera água subterrânea como fluxo de base: `qbase=min(G, 0.30 + 0.08×log2(F/12)) mm/h`, com `G-=qbase` e `W+=qbase`. Essa reserva é finita e não recebe recarga neste L0. `dx=10 m`, `Dsurf=2 m²/h`, `a=min(0.02,Dsurf Δh/dx²)` (L0: `a=0.02`) e `H_i=z_i+W_i/1000` em metros. Para cada par não ordenado de células vizinhas, a proposta de lâmina em direção à célula de menor carga é:

`qij = a × 1000 × max(0, H_i−H_j) mm por intervalo de 1 h`.

Converter `qij` em volume multiplicando pela área da célula. Calcular todos os fluxos do mesmo snapshot; se a soma de saídas propostas exceder `W_i`, escalá-las proporcionalmente até `W_i`. Para cada direção D8 ausente na borda, usar célula fantasma com `W=0` e elevação igual à célula de borda, propondo saída `a×W_i` mm/h naquela direção; escalar em conjunto com as demais saídas. Aplicar débitos/créditos simultaneamente. Água que cruza a borda é saída de fronteira; depressões retêm água. O armazenamento inicial em canais é finito e pode secar; não há vazão de base, aquífero, percolação profunda ou retorno de água subterrânea no L0.

**Balanço da água:** `Δ(surface + groundwater + soil + água corporal animal) = chuva − interceptação − evaporação superficial − evaporação do solo − transpiração − saída na borda − excreção animal`; fluxo de base `groundwater→surface`, escoamento entre células e bebida `surface→animal` cancelam no total. Água corporal vegetal não é estoque persistente.

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
- Predador a até 10 m da presa no snapshot de início do tick tem probabilidade de captura `p=1−exp(−0.80 Δd)`, com `Δd=1/144` dia/tick. Sorteio usa `tag=predação`, `subjectId=predatorId`, `targetId=preyId`, `tick` absoluto, `drawIndex=0`; conflitos resolvidos por menor ID de predador e uma morte por presa. Cada predador pode realizar no máximo uma captura bem-sucedida por dia simulado. Persistir `lastCaptureDay`, inicial e neonatal `−1`; comparar com `floor((tickIndex−1)/144)` e atualizar somente no commit da captura. Assim, o tick que encerra o dia ainda usa a cota daquele dia. Ao capturar, a ingestão `I=min(0.015 gDM/(g corpo·d)×(Mpred+Rpred)×1 d, Mprey+Rprey)` usa uma cota diária (não uma cota de 10 minutos). Retirar a fração `s=I/(Mprey+Rprey)` de `Mprey`, `Rprey` e `Nprey=qNprey×Mprey`; `Nintake=s×Nprey`. Todo o remanescente, N correspondente e resíduos da ingestão entram em detritos. Água corporal da presa capturada volta à água superficial de sua célula via Hydrosphere; água no alimento continua omitida no L0. Cancelar pedidos da vítima após morte.
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

Alvos de plataforma desktop definidos pelo usuário em 21/10/2026: **mínimo 8 GB de RAM + 4 GB de VRAM; recomendado para melhor desempenho 16 GB de RAM + 8 GB de VRAM**. A máquina de referência medida tem 15,87 GiB de RAM e uma NVIDIA GeForce RTX 5060 com 8.151 MiB reportados pelo driver; ela corresponde à classe recomendada. Ainda falta validar a carga completa em uma máquina da classe mínima. Registrar versão do SDK/runtime, build Release, hash, resolução, drivers, hardware, seed e configuração em cada medição; os valores da tabela abaixo são metas. Medir headless em CPU, sem log por evento/tick; medir desktop em 1280×720 com mapa 64×64 e camadas de inspeção.

| Marco | Meta objetiva |
|---|---|
| N0 | avançar 1000 ticks do boot headless em ≤5 s, após JIT/aquecimento; sem renderer |
| N3 headless integrado | seed 1, 64×64, 40 herbívoros + 4 predadores iniciais, reprodução ativa; ≥20 dias simulados/s, mediana de 3 execuções Release; cada janela cronometrada começa do estado inicial e termina em 365 dias (≤18.25 s) |
| N4 desktop | ≥30 FPS em média e P95 de quadro ≤33.3 ms, em janela contínua de 60 s; P99 ≤50 ms na mesma cena e máquina; seed 1 desde dia zero, após aquecimento separado, 6 ticks/s real (1 hora simulada/s), enquadramento registrado mostrando toda a grade e camada de água ativa |
| Memória | bytes privados estabilizados ≤512 MiB headless; no desktop, registrar bytes privados, working set e memória de GPU quando disponível. Não há teto fixo de memória privada do processo: validar a experiência na plataforma mínima (8 GB RAM/4 GB VRAM) e ajustar cena, cache e LOD caso a medição revele pressão de memória |
| Persistência | salvar e carregar/validar estado integrado da seed 1 no dia 365 em ≤2 s cada; save do dia 365 ≤10 MiB, excluindo cache recriável de imagem |
| Integridade | nenhuma perda de tick/evento após pause/resume/save; carga não muda o hash canônico da continuação |

### Medição desktop N4 — cenário v4

Executada em Release com SDK .NET 10.0.400, Windows 11 Pro, Intel Core i3-10105, 16 GiB RAM e NVIDIA GeForce RTX 5060 (driver 32.0.16.1088). Seed 1, 64 × 64 células, 1280 × 720, água e câmera fixa ativas; aquecimento de 5 s separado da janela medida de 60 s. Resultado: 3.590 quadros, média de 59.83 FPS, P95 de 17.12 ms, P99 de 25.90 ms, CPU de 60.6% de um núcleo e working set de 154.6 MiB. A amostra cumpre as metas N4 e memória desktop; os 17 testes passaram em Release. SHA256 do assembly `Game.Client.Desktop.dll`: `F13CDD30F7D49C719231027530F8F5CDDC41915DACA3784DCB16EFEBEFAFA6AA`. O modo reproduzível do cliente é `Game.Client.Desktop.exe --benchmark-seconds 60`; ele mede 1280 × 720 automaticamente e imprime os percentis após o aquecimento. O resultado bruto foi guardado fora do repositório em `genesis-render-benchmark-v4-chunked-release.txt`.

### Medição desktop — cenário v5

Executada em Release no mesmo hardware e driver acima, seed 1, 64 × 64 células, 1280 × 720, câmera ortográfica alinhada ao eixo do vale; aquecimento de 5 s e medição contínua de 60 s. Resultado: 3.590 quadros, média de 59.83 FPS, P95 de 20.31 ms, P99 de 30.11 ms, CPU de 61.0% de um núcleo e working set de 145.0 MiB. A amostra cumpre as metas N4; os 17 testes passaram em Release. SHA256 do assembly `Game.Client.Desktop.dll`: `BBA288B77F9A0FFF20DCDF139FE053AE3287BE1DCDD941E780788B9561C165C3`. Resultado bruto: `genesis-render-benchmark-v5-final-release.txt`.

### Medição desktop — cenário v6

Executada em Release no mesmo hardware e driver, seed 1, 64 × 64 células, 1280 × 720, câmera ortográfica alinhada ao eixo do vale; aquecimento de 5 s e medição contínua de 60 s. Resultado: 3.588 quadros, média de 59.78 FPS, P95 de 18.08 ms, P99 de 30.18 ms, CPU de 66.8% de um núcleo e working set de 139.6 MiB. A amostra cumpre as metas N4; os 17 testes passaram em Release. SHA256 do assembly `Game.Client.Desktop.dll`: `06F9917E7EDFEE3CBBAFCBF9090B2454B1530DEA797AEAE5D026FBFAC2B9F457`. Resultado bruto: `genesis-render-benchmark-v6-release.txt`.

### Medição desktop — cenário v7

Executada em Release no mesmo hardware e driver, seed 1, 64 × 64 células, 1280 × 720, câmera ortográfica fixa e simulação a 6 ticks/s; aquecimento de 5 s e janela contínua de 60 s. Resultado: 3.576 quadros, média de 59.60 FPS, P95 de 22.06 ms, P99 de 36.85 ms, CPU de 65.0% de um núcleo e working set de 125.4 MiB. A amostra cumpre as metas N4 e memória desktop. Os 17 testes passaram em Release. SHA256 do assembly `Game.Client.Desktop.dll`: `11970169274F722D3B92859234233526E376C5B1A7AAEA5332BF6D71AF76ECE5`. Resultado bruto: `C:\Users\Julio\.codex\visualizations\2026\10\21\01a15cc0-05ed-78f0-a37b-f0d61e31cb96\genesis-render-benchmark-v7-release.txt`.

### Validação v8 — estado e medição desktop

As seeds 1–3 foram avançadas em Release até o tick 144 (dia 2), em 4096 células. Seed 1: elevação 18,0–155,9 m, 1031 células molhadas, 2572 grupos de gramínea e 1478 lenhosos, hash de estado `57F080F4922924A006C55E78AC70AB39089DFB24E4D7F0AE12062A97019DBEE9`. Seed 2: 19,4–154,5 m, 1143 células molhadas, 2826 gramíneas e 1519 lenhosos, hash `ECD411C3A43DF691183DCC6E51125CB35A3D350B4711DB85659958019B28B02B`. Seed 3: 17,8–148,0 m, 1032 células molhadas, 2895 gramíneas e 1627 lenhosos, hash `007E5ECE1BE23E351A6B2FB6B81C18378E32FEEC90F4DAA381861DD1B553F92B`. Os resíduos máximos absolutos entre as três execuções foram `7.678×10⁻⁷ L` de água, `4.370×10⁻⁶ g` de matéria seca e `2.421×10⁻⁸ gN`; os testes Release passaram (17/17).

Em Release no mesmo hardware e driver, seed 1, 64 × 64 células, 1280 × 720, câmera ortográfica fixa e simulação a 6 ticks/s; aquecimento de 5 s e medição contínua de 60 s. Resultado: 3586 quadros, média de 59.77 FPS, P95 de 17.21 ms, P99 de 37.31 ms, CPU de 38.3% de um núcleo e working set de 136.9 MiB. A amostra cumpre as metas N4 e memória desktop. SHA256 do assembly `Game.Client.Desktop.dll`: `98D17FB4E860557A1963DFFDE4BE3D8FDF9F35A28ED7131E503903BAAAF17861`. Resultado bruto: `C:\Users\Julio\.codex\visualizations\2026\10\21\01a15cc0-05ed-78f0-a37b-f0d61e31cb96\genesis-render-benchmark-v8-release.txt`.

### Validação v9 — terreno, estado e renderização

As seeds 1–3 avançaram até o tick 144 (dia 2), em 4096 células. Seed 1: elevação 18,0–170,1 m, 1068 células com água, 2564 grupos de gramínea e 1417 lenhosos; hash `91BD70CCAF2F97C19BD0711BAD4EFA060FE4AA09B8C0941C97F59AF33DA90B11`. Seed 2: 19,4–168,7 m, 1165 células com água, 2706 gramíneas e 1406 lenhosos; hash `2AD2345DAA40A61A43A00DCE2BDAB28D06C500F2B8D06DF0B7BF0F21A7F888D3`. Seed 3: 17,8–162,6 m, 1071 células com água, 2819 gramíneas e 1479 lenhosos; hash `78A4DCCB09A276B69BEF0A7D34020FC583EAB83F6402B73EFD118AE1E4CFF746`. Resíduos máximos absolutos: `1.089×10⁻⁶ L` de água, `6.048×10⁻⁶ g` de matéria seca e `5.588×10⁻⁹ gN`. Os testes passaram (17/17).

Em Release no mesmo hardware e driver, seed 1, 64 × 64 células, 1280 × 720, câmera ortográfica fixa e simulação a 6 ticks/s; aquecimento de 5 s e janela contínua de 60 s. Resultado: 3586 quadros, média de 59.77 FPS, P95 de 17.23 ms, P99 de 39.43 ms, CPU de 40.4% de um núcleo e working set de 132.5 MiB. Cumpre as metas N4 e memória desktop. SHA256 do assembly `Game.Client.Desktop.dll`: `E46E814694CE783C83FD7E645520329B65C37BED6962B7054F4AC6A25ECFE4EB`. Captura `genesis-flora-v9b.png`; resultado bruto `C:\Users\Julio\.codex\visualizations\2026\10\21\01a15cc0-05ed-78f0-a37b-f0d61e31cb96\genesis-render-benchmark-v9-release.txt`.

### Validação v10 — cobertura lenhosa e estado

As seeds 1–3 avançaram até o tick 144 (dia 2), em 4096 células. Seed 1: elevação 18,0–170,1 m, 1068 células com água, 2564 grupos de gramínea e 1473 lenhosos; hash `5126DD120DBB423F14672F4B74F89FE176793C5AD7405BD1236C52164CD80800`. Seed 2: 19,4–168,7 m, 1166 células com água, 2706 gramíneas e 1442 lenhosos; hash `C1E329FEC3C1938300241061DCFFCE697E849FCDC067FFAF8B46AAE18539989D`. Seed 3: 17,8–162,6 m, 1070 células com água, 2819 gramíneas e 1611 lenhosos; hash `CC73B7CCA43C6EF8DE17E254ABB473609AF483D5B2C9654E289CEE94580DFC89`. Resíduos máximos absolutos: `1.333×10⁻⁶ L` de água, `5.521×10⁻⁶ g` de matéria seca e `2.049×10⁻⁸ gN`. Os testes passaram (17/17).

Em Release no mesmo hardware e driver, seed 1, 64 × 64 células, 1280 × 720, câmera ortográfica fixa e simulação a 6 ticks/s; aquecimento de 5 s e janela contínua de 60 s. Resultado: 3586 quadros, média de 59.77 FPS, P95 de 17.18 ms, P99 de 38.57 ms, CPU de 43.0% de um núcleo e working set de 155.9 MiB. Cumpre as metas N4 e memória desktop. SHA256 do assembly `Game.Client.Desktop.dll`: `1002B4932BA454D064442B1EF6F656CDE0A68006DF6D3FF3E2E6F7B0E744B1FA`. Captura `genesis-flora-v10.png`; resultado bruto `C:\Users\Julio\.codex\visualizations\2026\10\21\01a15cc0-05ed-78f0-a37b-f0d61e31cb96\genesis-render-benchmark-v10-release.txt`.

### Refinamento visual da água — renderizador v11

Sem alterar a seed nem o estado ecológico v10, a margem da água passou a usar ruído coerente em três escalas, e as fitas dos rios ganharam largura assimétrica determinística. O cliente pré-amostra esses campos e reutiliza vértices nas triangulações. Em Release, na mesma máquina e configuração, a medição de 60 s após 5 s de aquecimento obteve 3585 quadros, média de 59.75 FPS, P95 de 17.07 ms, P99 de 40.68 ms, CPU de 46.2% de um núcleo e working set de 203.7 MiB. Cumpre N4 e memória desktop. SHA256 do assembly `Game.Client.Desktop.dll`: `C8846BE150BB7948474D071B2C22FBEB4BF26B1D05C0E369140EF37ABF0E073D`. Captura `genesis-flora-v11-final.png`; resultado bruto `C:\Users\Julio\.codex\visualizations\2026\10\21\01a15cc0-05ed-78f0-a37b-f0d61e31cb96\genesis-render-benchmark-v11-optimized-release.txt`. Os 17 testes passaram em Release.

### Autotile de grama — renderizador v12

O cliente cria uma máscara visual 4 × 4 por célula a partir de biomassa viva, com ruído determinístico em escalas de uma e quatro células; a vizinhança diagonal recorta os cantos dos pequenos agrupamentos. O estado ecológico permanece v10. Na medição Release de 60 s, após 5 s de aquecimento, houve 3584 quadros, média de 59.73 FPS, P95 de 17.09 ms, P99 de 41.40 ms, CPU de 50.3% de um núcleo e working set de 203.5 MiB. Cumpre N4 e memória desktop. Os 18 testes passaram, incluindo repetibilidade da máscara, conexões de vizinhança e ausência de cobertura em células sem biomassa. SHA256 de `Game.Client.Desktop.dll`: `AD57D560C4209D50062DAF6C44BD7118827810E874FDEA7F0DD253F219570D3F`. Captura `genesis-flora-v12-grass-autotile.png`; resultado bruto `C:\Users\Julio\.codex\visualizations\2026\10\21\01a15cc0-05ed-78f0-a37b-f0d61e31cb96\genesis-render-benchmark-v12-grass-autotile.txt`.

### Geografia v11 e renderização v13

As seeds 1–3 avançaram até o tick 144 (dia 2), em 4096 células. Seed 1: elevação 18,0–170,1 m, 1073 células molhadas, 2555 grupos de gramínea e 1459 lenhosos; hash `475040802B1B82B62F1139FEA474B286D2D6537899A6061386E682C6D80D1D56`. Seed 2: 19,4–168,7 m, 1180 células molhadas, 2679 gramíneas e 1448 lenhosos; hash `8021EF4A56BC8DE8C5F4F348A3D06BD8AD16133ABD5D0CED0C1CB577968394BC`. Seed 3: 17,8–162,6 m, 1076 células molhadas, 2824 gramíneas e 1612 lenhosos; hash `1FA3EF74F1D52DC0A201314B6DBE33DD0DDBA95896CBB03C3208CD157E6D3A54`. O maior resíduo entre as seeds foi `1.403×10⁻⁶ L` de água, `5.226×10⁻⁶ g` de matéria seca e `2.421×10⁻⁸ gN`. A suíte Release passou (18/18).

Em Release no mesmo hardware e driver, seed 1, 64 × 64 células, 1280 × 720, câmera ortográfica fixa e simulação a 6 ticks/s; aquecimento de 5 s e medição de 60 s. Resultado: 3576 quadros, média de 59.60 FPS, P95 de 17.45 ms, P99 de 42.06 ms, CPU de 45.7% de um núcleo e working set de 204.8 MiB. A amostra atende às metas desktop N4. SHA256 de `Game.Client.Desktop.dll`: `AD57D560C4209D50062DAF6C44BD7118827810E874FDEA7F0DD253F219570D3F`; SHA256 de `Game.Modules.Geosphere.dll`: `6FC4F03ACB82EDEF2EB7CDA88ADD42E92CC6E2BB8AD349CFBD2340F850DFA23D`. Captura `genesis-flora-v13-weathered-outcrop.png`; resultado bruto `C:\Users\Julio\.codex\visualizations\2026\10\21\01a15cc0-05ed-78f0-a37b-f0d61e31cb96\genesis-render-benchmark-v13-release.txt`.

### Geografia v12 e renderização v14

As seeds 1–3 avançaram até o tick 144 (dia 2), em 4096 células. Seed 1: elevação 18,0–170,1 m, 1077 células molhadas, 2548 grupos de gramínea e 1463 lenhosos; hash `0E1FC3788328EA57A361760B9625A106EC4EE2FBD0407A9B413AAF40CAA87D42`. Seed 2: 19,4–168,7 m, 1189 células molhadas, 2672 gramíneas e 1447 lenhosos; hash `CA9B96C933F65165D7FB8FC0DD9DFF7CD012164CC399C2BD78A6B3D8A49C531A`. Seed 3: 17,8–162,6 m, 1078 células molhadas, 2811 gramíneas e 1594 lenhosos; hash `F52CD952C2CED6A77AD3B802E7BE2A3A946A3AF875FA8ACEBC379EC83AD91E4F`. O maior resíduo entre as seeds foi `1.317×10⁻⁶ L` de água, `4.998×10⁻⁶ g` de matéria seca e `2.235×10⁻⁸ gN`. A suíte Release passou (19/19).

Em Release no mesmo hardware e driver, seed 1, 64 × 64 células, 1280 × 720, câmera ortográfica fixa e simulação a 6 ticks/s; aquecimento de 5 s e medição de 60 s. Resultado: 3539 quadros, média de 58.98 FPS, P95 de 17.10 ms, P99 de 47.02 ms, CPU de 44.5% de um núcleo e working set de 160.6 MiB. A amostra atende às metas desktop N4. SHA256 de `Game.Client.Desktop.dll`: `00B8AFAFC408FA7D68B3860AF90F10ACB703641E4F89063FA8224911CC9B7384`; SHA256 de `Game.Modules.Geosphere.dll`: `CD10175423D0C6BB29A4F0F0AA981EB269FB7A18BC17D28D7EAE14060FA1FAB4`. Captura `genesis-flora-v14-multiple-outcrops.png`; resultado bruto `C:\Users\Julio\.codex\visualizations\2026\10\21\01a15cc0-05ed-78f0-a37b-f0d61e31cb96\genesis-render-benchmark-v14-release.txt`.
### Bosques por patch agregado — renderizador v15

Cada patch lenhoso desenha de 2 a 5 copas, com tamanho e posições reprodutíveis derivados da biomassa, da célula e da seed. É uma representação visual do patch agregado, sem novos organismos ou mudanças no estado ecológico v12. Os 19 testes Release passaram.

Em Release no mesmo hardware e driver, seed 1, 64 × 64 células, 1280 × 720, câmera ortográfica fixa e simulação a 6 ticks/s; aquecimento de 5 s e medição de 60 s. Resultado: 3560 quadros, média de 59.33 FPS, P95 de 17.08 ms, P99 de 45.43 ms, CPU de 48.2% de um núcleo e working set de 160.6 MiB. A amostra atende às metas desktop N4. SHA256 de `Game.Client.Desktop.dll`: `972B4E56B3A28F982BB530F2F7FE04BCC4E30D6E1D5E307AE8B4D795C33FBAA4`. Captura `genesis-flora-v15-canopy-groves.png`; resultado bruto `C:\Users\Julio\.codex\visualizations\2026\10\21\01a15cc0-05ed-78f0-a37b-f0d61e31cb96\genesis-render-benchmark-v15-canopy-groves.txt`.

### Bosques de altitude — cenário ecológico v13 / revisão visual v17

A cobertura lenhosa inicial usa frações de 30%/62%/40% nas faixas abaixo de 75 m, de 75–125 m e acima de 125 m. A inclinação máxima para gramíneas e lenhosas é 1.0 m/m; árvores continuam excluídas de células com água superficial, e gramíneas toleram até 150 mm. A seleção é determinística e espacialmente agrupada, preservando faces mais íngremes expostas.

As seeds 1–3 avançaram até o tick 144 (dia 2), em 4096 células. Seed 1: elevação 18,0–170,1 m, 1077 células molhadas, 2548 grupos de gramínea e 1892 lenhosos; hash `06E7C2C89966DF0B928FA90E536B8A9F879B6AA7E1FB0D398240D8299FB4F6E3`. Seed 2: 19,4–168,7 m, 1190 células molhadas, 2672 gramíneas e 2114 lenhosos; hash `8B76E90299471253793E753677C2D3B8A0EA865DECD65F0BB8AA5B25F9F4B06F`. Seed 3: 17,8–162,6 m, 1078 células molhadas, 2811 gramíneas e 2045 lenhosos; hash `1B971F055A251037A6118BDAE812244FB82020F63F54D5B9B0273047BC530CBB`. Resíduos máximos absolutos: `1.471×10⁻⁶ L` de água, `4.498×10⁻⁶ g` de matéria seca e `2.980×10⁻⁸ gN`. A suíte Release passou (19/19).

Em Release, seed 1, 64 × 64 células, 1280 × 720, câmera ortográfica fixa e simulação a 6 ticks/s; aquecimento de 5 s e medição de 60 s. Resultado: 3541 quadros, média de 59.02 FPS, P95 de 17.20 ms, P99 de 45.80 ms, CPU de 58.2% de um núcleo e working set de 181.5 MiB. O cliente manteve aproximadamente 59 FPS; o custo de memória aumentou com os bosques mais densos. SHA256 de `Game.Client.Desktop.dll`: `D2C2F4B64D3F98683A307BB9820683E9F19F42D338C17BD081A28DA5379442F3`. A captura `genesis-flora-v17-highland-forest.png` ainda mostra encostas abertas e rios mais largos que a referência. Resultado bruto: `C:\Users\Julio\.codex\visualizations\2026\10\21\01a15cc0-05ed-78f0-a37b-f0d61e31cb96\genesis-render-benchmark-v17-highland-forest.txt`.

O benchmark de apresentação v17 mede a seed canônica 1 sem fauna, com simulação a 6 ticks/s. O cenário headless `dotnet run --project src/Game.App -c Release -- --n3-scenario --ticks 52560` inicia 40 herbívoros e 4 predadores por 365 dias. O gate N3 foi aprovado: três medições Release de `AdvanceTicks` ficaram em 17,058 s, 17,024 s e 17,121 s (mediana 17,058 s, 21,40 dias simulados/s). O runner imprime `simulation-seconds` após criar o estado inicial e antes do relatório final. O benchmark de renderer continua separado; mudanças futuras devem preservar os balanços, seeds e alvo de throughput sem reduzir quantidade de células ou testes silenciosamente.

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

### Geografia v14 e renderização v18

As seeds 1–3 avançaram até o tick 144 (dia 2), em 4096 células. Seed 1: elevação 18,1–170,2 m, 1079 células com água, 2556 grupos de gramínea e 1867 lenhosos; hash `F64D62792B9075EB7C85C33A11F9BACD2B542128F963C822E93C61A12BBF72AB`. Seed 2: 19,5–169,5 m, 1365 células com água, 2688 gramíneas e 2168 lenhosos; hash `4CFFA315B43A540BFB937AD03DBD5C96E4E22B5F6A730B2B6E3782031FFCD4F9`. Seed 3: 17,7–164,1 m, 1097 células com água, 2829 gramíneas e 2057 lenhosos; hash `BAFC8F9BF9932354ACA60839BB8E60A9C54633D27E6976EADFE21FB14706188F`. Resíduos máximos absolutos: `1.306×10⁻⁶ L` de água, `4.338×10⁻⁶ g` de matéria seca e `2.608×10⁻⁸ gN`. A suíte Release passou (19/19).

Em Release, seed 1, 64 × 64 células, 1280 × 720, câmera ortográfica fixa e simulação a 6 ticks/s; aquecimento de 5 s e medição de 60 s. Resultado: 3538 quadros, média de 58.97 FPS, P95 de 17.26 ms, P99 de 45.64 ms, CPU de 59.2% de um núcleo e working set de 157.5 MiB. SHA256 de `Game.Client.Desktop.dll`: `B957FD099B091850687689060EC95F879EA5278BB86B8E375921ED8CFABF2E34`; de `Game.Modules.Geosphere.dll`: `C5D1FE168F19F4FEA599A2C2E2489EB9CDE5756FD59E43CB15A3768F725C1335`. Captura `genesis-flora-v18-mesorelief.png`, e o resultado bruto está em `C:\Users\Julio\.codex\visualizations\2026\10\21\01a15cc0-05ed-78f0-a37b-f0d61e31cb96\genesis-render-benchmark-v18-release.txt`. A amostra ficou próxima de 59 FPS nesta máquina; não certifica desempenho em outros equipamentos.

### Apresentação v23 — grama, âncoras e composição em profundidade

A revisão aumenta a resolução do autotile para até 8 × 8 subcélulas, acrescenta variação de pigmento de solo e umidade sem alterar biomassa e posiciona cada elemento visual em coordenadas determinísticas dentro da célula. Cada objeto consulta a elevação física; os sprites são compostos de trás para frente conforme a profundidade projetada da câmera, e as posições ficam cacheadas por snapshot, resolução e estação. Essa apresentação é da revisão v23; o cenário físico atual é v15.

Em Release, seed 1, 64 × 64 células, 1280 × 720, câmera ortográfica fixa e simulação a 6 ticks/s; aquecimento de 5 s e medição de 60 s. Resultado: 3518 quadros, média de 58.63 FPS, P95 de 21.74 ms, P99 de 50.39 ms, CPU de 53.5% de um núcleo e working set de 171.4 MiB. P95 cumpre a meta; P99 excede 50 ms por 0.39 ms nesta amostra. SHA256 de `Game.Client.Desktop.dll`: `204DCDBE327BC2F3DEED33000F40614DE15715E945AEF35657B6E5AC7F7FE6C7`. Captura `genesis-geometric-v23-depth-sort.png`; resultado bruto `genesis-render-benchmark-v23-depth-sort.txt` está no diretório de visualizações do Codex. A captura confirma a malha geométrica e as âncoras sobre o relevo, mas ainda mostra canais largos, cobertura vegetal em manchas e geografia menos variada que a referência.

### Geografia v15 — soerguimento independente e cristas ramificadas

O cenário v15 rompe o acoplamento geométrico entre os eixos das duas cordilheiras e o centro do rio: as cristas principais têm posição, inclinação, curvatura e segmentação determinísticas próprias. A âncora oeste é `x=12..16` células e a leste `x=48..52`; cada eixo varia ±0.12 célula por linha, soma duas curvas senoidais e uma deriva de ruído limitado a ±3.5 células. As larguras dos ombros são 4.0–5.6 células e as amplitudes 78–114 m. Seis cristas secundárias mais baixas, três por lado, partem dos ombros montanhosos e avançam em curvas para as margens do vale; usam largura 1.65–2.35 células e amplitude 15–23 m, com afunilamento longitudinal. O vale meandrante permanece uma feição separada. A altitude combinada continua sendo a única entrada para a drenagem D8, formação da água inicial, inclinação e colocação de flora. As larguras e amplitudes secundárias são parâmetros heurísticos de engenharia; esta implementação não simula tectônica nem erosão.

`dotnet test Game.sln -c Release --nologo`: 22/22 aprovados, incluindo repetibilidade, flexão dos dois eixos de crista, D8 descendente, faces de afloramento e bordas coincidentes de chunks. Seeds 1–3 avançaram até tick 144. Elevações: seed 1 `18.2–161.0 m`, seed 2 `20.2–161.3 m`, seed 3 `17.7–162.8 m`; balanços máximos absolutos nos três casos: água `1.471×10⁻⁶ L`, matéria seca `3.045×10⁻⁶ g`, N `1.490×10⁻⁸ g`. Hashes SHA256: seed 1 `885C9C8C069AA60A8446199A379D9FA5452E41A17FD56E658F285BCDEDF47E41`, seed 2 `36EABA00FC0017277651CC8C3DEE312E979A846F52DBD02178F3F3F9309938DD`, seed 3 `16CBFF582232F5D6F51E0B8BA7D8EB1984B0FEF52BD37915F97DAC4AC8650B21`.

Benchmark Release, seed 1, 1280 × 720, 6 ticks/s, aquecimento de 5 s e medição de 60 s: 3517 quadros, média 58.62 FPS, P95 19.90 ms, P99 48.43 ms, CPU 58.4% de um núcleo e working set 201.5 MiB. Cumpre as metas N4 de FPS, percentis de quadro e memória desktop definidas acima. SHA256 de `Game.Client.Desktop.dll`: `4B958DE7156108C0115AF8B48FF73E9F93E362546DE554E9E3CB4F651D212A1B`; `Game.Modules.Geosphere.dll`: `F0CF904A047F002F4CF5588307218B05B01C92795A9B086B58FB07DC94C40E55`. Captura `genesis-geometric-v24-ridge-network.png` e resultado bruto `genesis-render-benchmark-v24-ridge-network.txt` foram guardados no diretório de visualizações do Codex, fora do repositório. A captura mostra cristas ramificadas e cursos laterais sobre geometria real; ainda há canais largos e bosques em manchas em relação à referência.

### Apresentação v26 — câmera inclinada e drenagem suavizada

O cliente usa ângulo aproximado de 30° da vertical, ainda ortográfico; as fitas visuais dos cursos D8 são interpoladas por Catmull–Rom e amostram a elevação em cada ponto. O modo `--capture` oculta o HUD para produzir uma imagem limpa de revisão. Nada disso muda o cenário v15 nem a simulação.

Em Release, seed 1, 1280 × 720, 6 ticks/s, aquecimento de 5 s e medição de 60 s: 3461 quadros, média 57.67 FPS, P95 30.52 ms, P99 49.53 ms, CPU 57.4% de um núcleo e working set 198.2 MiB. A amostra cumpre N4 e memória desktop. SHA256 de `Game.Client.Desktop.dll`: `CAA672EBB0E35E95707AECAAC15A6231CE4FBDF24D43A569696E32A59E396294`; `Game.Modules.Geosphere.dll`: `F0CF904A047F002F4CF5588307218B05B01C92795A9B086B58FB07DC94C40E55`. Captura `genesis-geometric-v26-camera-depth.png` e resultado bruto `genesis-render-benchmark-v26-camera-depth.txt` ficam no diretório de visualizações do Codex. A inclinação deixa os paredões mais legíveis; a água ainda cobre áreas largas e a floresta mantém bordas em manchas.

### Geografia v16 — relevo e platôs elevados

A v16 eleva as amplitudes das cristas oeste/leste para 170–190 m e 132–150 m; as seis cristas secundárias passam a 24–36 m. Quatro platôs menores seguem os eixos montanhosos, cada um com centro, orientação, contorno e cota derivados da seed. Somados às três feições anteriores, são sete coroas planas com faces laterais; D8, água inicial, gradientes e implantação de flora continuam consumindo o mesmo campo físico da Geosfera. O limite de cota do sampler/coroas sobe de 190 para 340 m. Os valores são parâmetros geométricos L0, sem alegação de calibração geológica.

`dotnet test Game.sln -c Release --nologo`: 22/22 aprovados. As seeds 1–3 avançaram até tick 144 (dia 2): seed 1, altitude `18.2–231.8 m`, 138 células com pelo menos 20 mm de água, 2231 grupos de gramínea e 1941 lenhosos, hash `9F3129564AEED8C2D51C7BB43F17A5ACFE604D7146068B98269C82F6B1681BBA`; seed 2, `20.4–246.5 m`, 120 células, 2301 gramíneas e 1763 lenhosos, hash `AB5B6A7B8C7B92A4B8116CFBE1B4335B4A291B2EE21F02A7F17DD682A94B2CF3`; seed 3, `17.7–235.6 m`, 135 células, 2423 gramíneas e 1889 lenhosos, hash `8202F135D060C2EDCDD4A59BF80DF10D980E1EE41124E6CBC86BA93D436061A5`. Os resíduos máximos absolutos foram `6.381×10⁻⁷ L` de água, `5.597×10⁻⁶ g` de matéria seca e `1.770×10⁻⁸ gN`. A captura `genesis-geometric-v27-ridge-tablelands.png` fica no diretório de visualizações do Codex.

Benchmarks desktop Release, seed 1, 1280 × 720, 6 ticks/s, aquecimento de 5 s e janelas de 60 s: amostra 1 — 3495 frames, média 58.24 FPS, P95 24.52 ms, P99 58.97 ms, CPU 47.6% de um núcleo, working set 175.9 MiB; amostra 2 — 2533 frames, média 42.22 FPS, P95 38.71 ms, P99 70.23 ms, CPU 44.0% de um núcleo, working set 172.1 MiB; amostra 3 — 3423 frames, média 57.05 FPS, P95 30.49 ms, P99 61.04 ms; amostra 4 — 3361 frames, média 56.00 FPS, P95 30.01 ms, P99 72.52 ms. As quatro amostras não aprovam o gate N4. SHA256 antes da implementação hidrológica v17: `Game.Client.Desktop.dll` `43EB0706F0BF073BC0603AE40125641E42849EE570B4097B5280DCF9E814EFED`; `Game.Modules.Geosphere.dll` `C29B046533063B1166193913E92E37BF17FA83F999898CF820059CE3AC83E0DB`. Capturas e logs brutos ficam no diretório de visualizações do Codex.

### Hidrologia v17 — soleiras e reservatórios de depressão

O `Hydrosphere` acrescenta um Priority-Flood determinístico a partir da borda aberta do mapa. O campo de elevação preenchida é um resultado topológico separado: `WorldCell.ElevationM` permanece intacto. A implementação expõe receptores/área contribuinte, componentes deprimidas agrupadas por soleira, capacidade até o spill e funções nível–volume–área. A semeadura inicial seleciona até três bacias e instala pools abaixo da soleira com nível constante sobre o fundo irregular. A alteração não simula ainda chuva enchendo essas curvas nem extravasamento dinâmico; o `Route` anterior e a rede D8 continuam ativos.

O cenário passa a `temperate-valley-l0-v17`. `dotnet test Game.sln -c Release --nologo`: 24/24 aprovados, incluindo uma bacia sintética com cota de soleira de 5 m, capacidade de 4.500 m³ e volume/área conferidos em nível intermediário, além de repetibilidade e alcance da borda. Seeds 1–3 avançaram a 144 ticks; elevação: seed 1 `18.2–231.8 m`, 939 células com água, 2220 grupos de gramínea e 1948 lenhosos, hash `BE5B7203E9F557C0A0585462CB9B5D87A803E9E90BD283D9E2522854F8797E49`; seed 2 `20.4–246.5 m`, 1006 células, 2267 gramíneas e 1739 lenhosos, hash `F4E4ED54B19AA047D2C564C543BF7124849180D286E918A0D3699EA98A051E1A`; seed 3 `17.7–235.6 m`, 1070 células, 2426 gramíneas e 1882 lenhosos, hash `71BB92133FC8F6FC1015E5B2F561684E510B892B65D60126FE1603A317C6948C`. Resíduos máximos absolutos foram `6.974×10⁻⁷ L` de água, `6.355×10⁻⁶ g` de matéria seca e `1.676×10⁻⁸ gN`.

`PixelTerrainTexture` calcula linhas independentes em paralelo. A malha detalhada da água passa de 8 para 6 subdivisões por célula e reutiliza a rede D8 entre atualizações horárias; o detalhe do ribbon do rio continua Catmull–Rom. Benchmark Release em seed 1, 1280 × 720, 6 ticks/s, 5 s de aquecimento e 60 s medidos: 3464 quadros, média `57.72 FPS`, P95 `30.35 ms`, P99 `49.22 ms`, CPU `50.0%` de um núcleo e working set `182.4 MiB`; atende o gate de percentis N4 por margem estreita. Os máximos medidos de reconstrução foram `123.41 ms` para malha de água e `140.90 ms` para textura de terreno, embora esses picos não tenham elevado o P99 acima do limite. Amostra curta de 10 s: `59.11 FPS`, P95 `18.11 ms`, P99 `49.71 ms` (referência, não gate). SHA256: cliente `6752E7FAA719E9E576EAC8FA427843AB98A781B6558E5B90922A2F20A6840F5E`, Hydrosphere `3390064859D594D3838F6AFF26149EDEC27E5BE56F5B9624EF9B964663BAEFE4`, Geosphere `C29B046533063B1166193913E92E37BF17FA83F999898CF820059CE3AC83E0DB`. Captura `genesis-hydrology-v17-level-pools.png`; log bruto `C:\Users\Julio\.codex\visualizations\2026\10\21\01a15cc0-05ed-78f0-a37b-f0d61e31cb96\genesis-render-benchmark-v28-hydrology.txt`. O renderer ainda não deriva largura de vazão medida, e os picos de reconstrução seguem como dívida de desempenho.

### Evidência N4 atualizada — 22/10/2026

O gate de desktop foi repetido na build Release atual desde o snapshot seed 1/dia zero, com simulação ativa a 6 ticks/s, camada de água, grade inteira e 1280×720: 59,58 FPS médios, P95 `17,30 ms`, P99 `36,42 ms`, working set `209,3 MiB`, memória privada `230,4 MiB`. O P99 cumpre o limite; houve um pico isolado de quadro de `208,16 ms`. A comparação visual pausada no dia 172 nas duas projeções, a rubrica, determinismo seeds 1–3, escala 10× e os itens ainda pendentes estão no [relatório N4 de integração visual](reports/N4_VISUAL_INTEGRATION_2026-10-22.md). Essa medição não substitui a validação visual, hardware mínimo, memória GPU por processo ou portabilidade.
