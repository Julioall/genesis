# ADR-0003 — topologia hidrológica sem alterar o leito

- **Status:** aceita
- **Data:** 21/10/2026

## Contexto

A Hydrosphere atual usa D8 de maior declive e difusão de superfície. Depressões sem vizinho inferior podem reter água, mas ainda não têm soleira, capacidade ou curva de nível. Preencher permanentemente o DEM facilitaria obter caminhos até a borda, porém apagaria a forma física que define lagos e armazenamento.

## Decisão

- Manter `WorldCell.ElevationM` como cota original do leito.
- Calcular um campo Priority-Flood independente para conectividade, receptor e área contribuinte até a borda aberta.
- Medir as células deprimidas, cota de spill, capacidade, área molhada e volume por nível a partir do leito original.
- Semear no máximo três pools iniciais abaixo do spill, com superfície nivelada; o preenchimento por chuva e o extravasamento dinâmico aguardam um gate posterior.
- Manter o D8 atual como rede de canais compatível com os consumidores existentes enquanto os fluxos hidráulicos não determinarem rios persistentes.
- Reimplementar a topologia em C# dentro de `Game.Modules.Hydrosphere`; não adicionar dependência de runtime nem copiar código externo.

## Alternativas

- **Alterar o DEM com o campo preenchido:** rejeitada, pois muda o relevo físico para resolver uma questão de conectividade.
- **Usar apenas D8 local e sinks:** insuficiente para determinar a saída e o volume de bacias cercadas por soleiras.
- **Importar ferramenta de processamento de DEM:** adiada; a grade L0 é pequena, e o algoritmo não exige pacote externo em runtime.

## Consequências

- O cenário v17 tem rotas topológicas reprodutíveis e pools iniciais medidos contra elevações originais.
- A árvore de Priority-Flood é uma topologia de pré-processamento, não um solver de água em movimento. As rotas planas não substituem um tratamento hidráulico de fluxo.
- A fração inicial de capacidade dos pools é uma escolha de engenharia e não está calibrada com medições de campo.
- Os próximos gates são enchimento/extravasamento conservativos, infiltração contínua, fluxo por aresta e integração da largura do rio com vazão efetiva.

## Evidência

`dotnet test Game.sln -c Release --nologo` cobre soleira, capacidade, nível intermediário, área submersa, preservação do leito, determinismo e encaminhamento de toda a grade à borda. Os resultados de seed e benchmark estão em [20_L0_MODEL_AND_BENCHMARKS.md](../20_L0_MODEL_AND_BENCHMARKS.md); o estado por gate está em [HYDROLOGY.md](../specs/HYDROLOGY.md).

Referência do algoritmo: [Barnes, Lehman e Mulla (2014), Priority-Flood](https://doi.org/10.1016/j.cageo.2013.04.024).
