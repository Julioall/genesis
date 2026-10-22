# GDD — Mundo natural Systema Naturae

**Status:** visão e recorte de produto. Não é especificação de física nem prova de implementação.

**Estado desta etapa (22/10/2026):** N0–N3 estão implementados e validados no escopo L0 segundo os relatórios registrados. O cliente MonoGame e a integração N4 estão em andamento; o gate visual ainda não passou. A visão de mundo natural completo continua sendo objetivo de produto, não uma declaração de MVP aprovado.

## Proposta

Simulador observacional de um mundo natural criado proceduralmente, em que astronomia, clima, relevo, solo, água, plantas e animais interagem por processos interdependentes. O jogador cria um mundo por seed, observa ciclos naturais, pausa ou acelera o tempo, acompanha um organismo ou região, inspeciona por que um evento ocorreu e compara estados antes/depois. Em alguns meses simulados, a meta é observar histórias explicáveis — como seca, deslocamento da fauna, mortalidade e recuperação após chuva — sem forçar acontecimentos narrativos. A representação deve comunicar profundidade, volume e relações naturais em uma região ampla. MonoGame é a stack obrigatória; a direção visual 3D procedural está escolhida, mas sua aprovação permanece aberta; veja a [direção visual](22_VISUAL_DIRECTION_AND_TECHNOLOGY.md).

## Visão completa da primeira entrega

Mundo físico com dia/noite, estações e chuva; relevo, habitats, infiltração, água superficial e solo; dois grupos vegetais; herbívoros, predadores e decomposição simplificados; recursos finitos; mapas de observação, pausa, save/load e benchmarks reais.

## Fora do escopo inicial

Civilizações, profissões, economia, agricultura humana, política, guerra, redes neurais por animal, microbiologia detalhada, tectônica complexa, oceanografia avançada e simulação física atmosférica tridimensional.

## Princípios de experiência

- Mudanças naturais geram consequências observáveis e auditáveis.
- Ecossistemas podem oscilar, migrar, recuperar-se ou colapsar; não existe população estabilizada artificialmente.
- O conteúdo natural visível é procedural e não determina o estado biológico; o estilo e a dimensionalidade da representação são decisões de apresentação.
- A experiência principal funciona em desktop e o núcleo permanece independente da interface; navegador exige avaliação técnica própria.

## Critérios de aceitação

Ver [Roadmap](12_ROADMAP.md), [MVP físico](08_PHYSICAL_MVP.md), [MVP biológico](09_BIOLOGICAL_MVP.md) e [Testes](13_TESTS.md).

## Recorte executável atual

N0–N2 fornecem a base física e a botânica agregada. N3 acrescenta herbívoros e predadores funcionais, movimento, alimentação, reprodução, mortalidade e balanços de matéria/N/água; o cenário 40/4 foi validado por três seeds anuais e save/load. A morte animal ainda deposita matéria em detritos agregados, sem carcaça persistente. O cliente MonoGame observa snapshots sem autoridade sobre a simulação; seu gate visual permanece aberto. Consulte o [README](../README.md), os [relatórios de evidência](reports/README.md) e o [modelo biológico](09_BIOLOGICAL_MVP.md).
