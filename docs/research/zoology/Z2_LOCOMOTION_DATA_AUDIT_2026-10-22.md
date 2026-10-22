# Z2 — auditoria de dados para locomoção

**Data:** 2026-10-22\
**Uso:** separar evidência de campo de parâmetros provisórios do runtime. Esta busca não calibra os perfis genéricos atuais.

## Perguntas e resultados

Foi feita uma busca SciSpace por estudos que quantifiquem inclinação, velocidade e travessia em água para mamíferos quadrúpedes. A busca encontrou relações de locomoção ligadas a espécies e contextos específicos; não encontrou um único limite de declive ou profundidade de vau aplicável a herbívoro/predador genéricos.

### Relação entre declive e deslocamento

- **Íbex-alpino (*Capra ibex*):** Biancardi & Minetti filmaram 54 indivíduos em paredes de barragens cujas inclinações variavam de 123% a quase vertical. As trajetórias médias foram mais suaves que as paredes, com subida em zigue-zague; indivíduos menores alcançaram inclinações maiores. É evidência de que massa, geometria corporal, superfície e escolha da rota afetam o limite observado. Não justifica usar o limite do íbex para quadrúpedes genéricos sobre terreno natural. [Artigo e PDF da revista](https://www.italian-journal-of-mammalogy.it/article/view/11504/pdf) · [Repositório acadêmico](https://air.unimi.it/handle/2434/494509) · DOI: `10.4404/HYSTRIX-28.1-11504`.
- **Pronghorn (*Antilocapra americana*) e tartaruga-do-Texas (*Gopherus berlandieri*):** Chambers et al. (2025) ajustam funções não lineares de velocidade versus declive a rastros GPS. O estudo usa resolução de 30 m para pronghorn e terreno lidar de resolução muito fina para a tartaruga; a tartaruga apresentou tolerância mais ampla a variação local de declive. Isso é evidência contra um único corte abrupto compartilhado entre espécies e escalas. [Springer](https://link.springer.com/article/10.1007/s10980-025-02092-2) · [USGS](https://pubs.usgs.gov/publication/70266220) · DOI: `10.1007/s10980-025-02092-2`.

### Travessia de rios e limite de vau

- Thomas, Larter & Jung (2022) usam GPS de 11 bisões para localizar travessias de rio e modelar a seleção de locais. O resumo relaciona travessias a canais mais estreitos e dias de menor vazão, mas não oferece uma profundidade máxima transferível para um agente genérico. [Journal of Mammalogy](https://doi.org/10.1093/jmammal/gyac011).
- A busca direcionada por limites de profundidade para mamíferos terrestres não encontrou nesta rodada uma medição experimental de vau adequada aos perfis N3. Isso é ausência de evidência na busca executada, não prova de que a literatura não contenha o dado.

## Complemento SciSpace — dados de ovinos

Uma consulta posterior encontrou duas fontes úteis para velocidade e custo em terreno suave, mas não suficiente para fechar os limites atuais:

- Palacios, Plaza e Abecia (2021) analisaram 36.083 geolocalizações de um rebanho de 650 ovinos em 800 ha de pastagem comunal em Zamora ao longo de 2018–2019. O mesor da série de velocidade foi 0,2 m/s, com amplitude circadiana de 0,16 m/s. É velocidade observada em um contexto de pastoreio específico, não velocidade máxima ou parâmetro de um perfil genérico. [Artigo](https://doi.org/10.1016/j.msom.2021.10.028).
- Mulvenna, Marks e Wilson (2022) mediram consumo de oxigênio e aceleração de ovinos domésticos em esteira, andando a 0,8–2,9 km/h em inclinações de −6° a +6°. A relação entre aceleração e energia mudou com o declive; os autores alertam contra transferir uma relação de terreno plano para terreno irregular. Essa faixa não dá suporte ao corte do runtime `MaximumSlopeGrade = 0.75` (75% de declive, aproximadamente 36,9°), e o resumo consultado não fornece tamanho amostral nem profundidade de vau. [Artigo](https://doi.org/10.1186/s40317-022-00281-3).

Esses estudos recuperam dados de velocidade/energia para *Ovis aries* em contextos delimitados, mas não escolhem entre o perfil funcional genérico atual, o protótipo Soay candidato ou uma população do jogo. Não há base para mudar o limite de vau de 0,25 m ou publicar um limiar universal de declive.

## Decisão para o runtime

O protótipo Z2 mantém `MaximumSlopeGrade = 0.75` e `MaximumWadeDepthM = 0.25` como limites de engenharia usados para exercitar a integração. Esses valores não vêm dos artigos acima e não devem ser apresentados como limites biológicos.

A integração tenta primeiro a rota direta amostrada a cada 5 m. Se ela parar por água profunda ou declive acima do limite, `CreatureTerrainRoutePlannerV1` planeja um desvio A* em grade com os mesmos limites e devolve amostras densificadas para validação final pelo solver. Isso cobre desvio no protótipo, mas não calibra seleção de rota, velocidade ou custo para uma espécie; também não representa colisão volumétrica.

## Dependência não resolvida

Antes de calibrar Z2, escolher uma população de referência compatível com cada perfil ecológico e recuperar dados primários de velocidade, resposta ao declive, travessia de água e custos locomotores. O estado atual do projeto fornece perfis funcionais genéricos, sem espécie ou população-alvo; portanto não existe uma correspondência defensável para fixar esses parâmetros. A pesquisa não resolve essa escolha de produto/modelagem.
