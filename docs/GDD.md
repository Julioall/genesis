# GDD — Mundo natural Systema Naturae

**Status:** visão e recorte de produto. Não é especificação de física nem prova de implementação.

**Estado desta etapa:** N0–N2 foram implementados nesta branch. O recorte executável termina na flora; não inclui fauna e não constitui o MVP Natural completo. A visão de fauna e cadeia alimentar abaixo permanece objetivo futuro.

## Proposta

Simulador observacional 2D top-down de um mundo criado proceduralmente, em que astronomia, clima, relevo, solo, água, plantas e animais interagem por processos interdependentes. O jogador cria um mundo por seed, observa ciclos naturais, ajusta velocidade, inspeciona células e seres vivos e acompanha consequências ambientais sem forçar um resultado narrativo.

## Visão completa da primeira entrega

Mundo físico com dia/noite, estações e chuva; relevo, habitats, infiltração, água superficial e solo; dois grupos vegetais; herbívoros, predadores e decomposição simplificados; recursos finitos; mapas de observação, pausa, save/load e benchmarks reais.

## Fora do escopo inicial

Civilizações, profissões, economia, agricultura humana, política, guerra, redes neurais por animal, microbiologia detalhada, tectônica complexa, oceanografia avançada e simulação física atmosférica tridimensional.

## Princípios de experiência

- Mudanças naturais geram consequências observáveis e auditáveis.
- Ecossistemas podem oscilar, migrar, recuperar-se ou colapsar; não existe população estabilizada artificialmente.
- O visual é pixel art procedural, mas não determina o estado biológico.
- A experiência principal funciona em desktop e o núcleo permanece independente da interface; navegador exige avaliação técnica própria.

## Critérios de aceitação

Ver [Roadmap](12_ROADMAP.md), [MVP físico](08_PHYSICAL_MVP.md), [MVP biológico](09_BIOLOGICAL_MVP.md) e [Testes](13_TESTS.md).

## Recorte executável atual

N0–N1 fornecem um runner headless determinístico, ciclo dia/noite/estação, relevo, clima prescrito, solo e água superficial. N2 acrescenta gramíneas e plantas lenhosas por patches, sementes, respostas L0 e reciclagem agregada. Uma janela MonoGame mínima foi incluída para observar essas saídas, sem executar simulação no desenho. Fauna, relações tróficas e experiência completa de inspeção continuam planejadas; veja o [README](../README.md) para comandos e limites.
