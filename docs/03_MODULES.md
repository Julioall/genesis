# Catálogo dos cinco domínios: submódulos, escopo e responsabilidades

A classificação é **arquitetural**, baseada nos grandes sistemas naturais, não uma correspondência perfeita com disciplinas acadêmicas. Estado persistente e processos com fortes acoplamentos devem ter owner explícito no documento 04.

## 1. Cosmos — astronomia e forçantes exógenas

| Submódulo | Funções | MVP? |
|---|---|---|
| OrbitalMechanics | rotação, translação e inclinação axial simplificadas | parcial: parâmetros fixos |
| SolarRadiation | geometria da estrela, irradiância no topo da atmosfera | sim |
| LunarDynamics | luas, fases e potenciais de marés | não |
| AstronomicalCycles | dia, ano, duração, posição sazonal | sim |

Tempo astronômico não é a temperatura atmosférica; `seasons` descreve geometria/calendário, não toda a climatologia. Velocidade de apresentação não altera tempo lógico de um tick.

## 2. Atmosphere — estado e processos do ar

| Submódulo | Funções | MVP? |
|---|---|---|
| Thermodynamics | temperatura, pressão simplificada e troca térmica | temperatura sim; pressão avançada não |
| AtmosphericCirculation | vento, massas de ar/circulação | não no cenário L0; adiado |
| HumidityAndClouds | vapor, umidade, cobertura de nuvens | índices e nuvens simplificados |
| Precipitation | taxa/tipo/local da chuva/neve/granizo | chuva sim; neve depois |

Atmosphere decide **quanto** e **quando** precipita, mas não armazena água em rios, solo ou neve acumulada. A irradiância incidente à superfície inclui atenuação de Atmosphere e sombreado topográfico como efeito de geometria/relevo publicado via contrato.

## 3. Geosphere — solo/rochas/terreno

| Submódulo | Funções | MVP? |
|---|---|---|
| Terrain | altitude, declive, orientação, obstáculos | sim |
| Geology | substrato, litologia e minerais, efeitos no solo | substratos simples |
| Soil | perfis, água retida, matéria orgânica, disponibilidade de nutrientes | sim |
| Geomorphology | erosão, deposição, intemperismo, relevo mutável | terreno estático no L0; erosão adiada |

Evitar `Terrain` contendo simultaneamente água superficial e estoque de água do solo. Congelamento do **solo** pertence a Geosphere.Soil quando implementado. O solo é meio físico e químico; detritos orgânicos e atividade decompositora pertencem a Biosphere até sua transferência para reservatório edáfico de matéria orgânica.

## 4. Hydrosphere — água física superficial/solidificada

| Submódulo | Funções | MVP? |
|---|---|---|
| SurfaceWater | lâmina/poças, escoamento, depressões, bordas | sim; rios/canais persistentes depois |
| Groundwater | fluxo subterrâneo/aquíferos/recarga | não, porta futura |
| SnowAndIce | neve depositada, gelo superficial, derretimento, geleiras | não no clima-base; intenção documental apenas |
| WaterQuality | turbidez, temperatura da água, solutos | depois |

A **criosfera** fica distribuída por responsabilidade: neve que precipita é Atmosphere; neve/gelo **depositados** são Hydrosphere.SnowAndIce; solo congelado é Geosphere.Soil; temperatura/energia ambiente são informadas por Atmosphere e Cosmos. Não criar um `Game.Modules.Cryosphere` separado no MVP.

## 5. Biosphere — vida, processos e ecossistemas

| Submódulo | Funções | MVP? |
|---|---|---|
| Botany | fisiologia agregada, cobertura, sementes, crescimento e morte | sim; órgãos/raízes individualizados depois |
| Zoology | animais, energia, hidratação, ações, reprodução, idade e morte | sim, 2 perfis |
| Ecology | interações, competição, detritos biológicos, decomposição agregada, teias, populações | sim |

Fungos e microrganismos são grupos biológicos reais distintos de plantas/animais, mas como implementação inicial serão **processos agregados de Ecology** (sem agentes fúngicos/microbianos individuais). Ecology não toma posse da água do solo nem do nitrogênio inorgânico; envia/recebe fluxos ao submódulo Geosphere.Soil.

## Regras futuras

- Não exigir todas as subpastas e classes futuras na primeira compilação.
- Ativar funcionalidades por `capabilities`/manifestos versionados em vez de métodos que retornam zeros silenciosos.
- O pacote de espécies contém parâmetros/receitas; não precisa modificar Cosmos, Soil ou Hydrosphere para adicionar uma árvore.
- Sistemas humanos ficam fora do catálogo de módulos da primeira entrega.
