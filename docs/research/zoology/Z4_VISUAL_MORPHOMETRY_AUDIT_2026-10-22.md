# Auditoria SciSpace — morfometria visual Z4

**Data:** 2026-10-22\
**Ferramenta:** pesquisa de artigos SciSpace\
**Uso:** procurar medidas ou superfícies externas de referência para tirar os protótipos genéricos do estado atual. A busca não autoriza copiar medidas entre raças, estágios de vida ou contextos ambientais.

## Soay (*Ovis aries*)

- A busca não encontrou uma superfície externa 3D completa ou um conjunto aberto de medidas externas adultas de Soay que possa sustentar um corpo-plan quadrúpede diretamente.
- O estudo de [Wang et al. (2026), “From rugged isles to managed pastures”](https://doi.org/10.1098/rsos.251123) mede morfologia 3D dos metacarpos de Soay em populações ferais e manejadas. É evidência útil de variação esquelética associada a sexo, ontogenia e ambiente, mas não fornece proporções externas de tronco, cabeça e membros.
- [Zhou et al. (2017), “Reconstruction and Body Size Detection of 3D Sheep Body Model Based on Point Cloud Data”](https://doi.org/10.1007/978-3-030-06179-1_26) reconstrói o corpo por nuvem de pontos e deriva comprimento, altura, altura/largura do quadril e largura do tórax para ovelhas Sunite de 2–3 anos; relata erro relativo médio próximo de 1%. É um método de medição transferível, mas a amostra é outra raça e não calibra Soay.

## Chinook (*Oncorhynchus tshawytscha*)

- [Wessel, Smoker & Joyce (2006)](https://doi.org/10.1577/T04-078.1) comparam juvenis de origem selvagem, hatchery e híbrida. A amostra de hatchery tinha corpo mais comprimido, cabeça mais estreita, maxila mais curta e pedúnculo caudal mais longo/estreito; são diferenças de origem e geração, não um molde universal da espécie.
- [Study of juvenile Chinook migratory phenotypes (2014)](https://doi.org/10.1111/jfb.12482) relata associação entre tática migratória e forma: migrantes de outono e juvenis de locais a jusante tinham corpos mais profundos, cabeças mais curtas e pedúnculos caudais mais profundos. O estudo não separa efeitos genéticos e ambientais.
- [Ecomorphological plasticity of juvenile fall-run Chinook (2016)](https://doi.org/10.1007/s10641-015-0454-9) encontra diferenças de forma ligadas ao ambiente de criação, incluindo expansão dorsoventral do corpo e das nadadeiras. Isso reforça que estágio e habitat precisam acompanhar qualquer receita visual.
- [3DPhenoFish (2021)](https://doi.org/10.24272/J.ISSN.2095-8137.2021.141) usa 18 landmarks e medidas 2D/3D em quatro ciprinídeos, com alta correlação em comparação a medidas manuais. É uma referência metodológica; não é dado de forma para Chinook.

## Decisão para o código

Não preenchi coeficientes do corpo-plan atual com medidas de outra raça de ovelha, de juvenis de salmão ou de populações com ambientes diferentes. Os resultados sustentam landmarks e possíveis eixos de variação, mas ainda falta um conjunto externo padronizado para a população e estágio que o projeto escolher. Os protótipos permanecem genéricos, claramente rotulados e sem alegação de calibração por espécie.

## Dependências abertas

1. Definir espécie e população adultas de referência para herbívoro, predador e peixe.
2. Obter medidas externas comparáveis (ou scans 3D) por sexo e estágio, com licença e chaves de identificação verificáveis.
3. Manter efeitos ambientais e de criação separados dos traços herdados antes de convertê-los em genes ou parâmetros de renderização.

O Z4 agora permite inspecionar o quadrúpede genérico em vistas frontal, lateral e de três quartos. A pesquisa confirma que a próxima passagem visual deve aguardar body-plans específicos ou novos dados; ela não bloqueia a validação geométrica do protótipo atual.
