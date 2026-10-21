# Fontes científicas e de engenharia para a arquitetura (não equivalem a validação do jogo)

## Acoplamento e arquitetura

1. Hutton, E. W. H., Piper, M., Tucker, G. E. (2020). **The Basic Model Interface 2.0: A standard interface for coupling numerical models in the geosciences.** JOSS. https://doi.org/10.21105/joss.02317 — Interfaces entre modelos; não é benchmark de C# ou MonoGame.
2. Müller, S. et al. (2025). **FINAM is not a model (v1.0): a new Python-based model coupling framework.** Geoscientific Model Development. https://doi.org/10.5194/gmd-18-4483-2025 — Acoplamento multiescala, adaptação temporal, interfaces e remapeamento; a implementação é Python, não prova direta de nosso runtime.
3. Tan, W. J. et al. (2021). **Causality and Consistency of State Update Schemes in Synchronous Agent-based Simulations.** PADS. https://doi.org/10.1145/3437959.3459262 — causalidade/conflitos na atualização de agentes.

## Natureza, ecologia e modelagem de organismos

4. USGS Water Science School. **Water Cycle** (conteúdo educativo e panorama dos reservatórios). https://www.usgs.gov/special-topics/water-science-school/water-cycle — distinguir estoques/fluxos de água e estados físicos.
5. Anderson, E. et al. (2024). **Water cycle processes**. USGS General Information Product 235. https://doi.org/10.3133/gip235 — processos evaporação, transpiração, condensação, precipitação e infiltração.
6. USGS (2026). **Infiltration and the Water Cycle.** https://www.usgs.gov/water-science-school/science/infiltration-and-water-cycle — umidade, solo, cobertura e escoamento.
7. Grimm, V. et al. (2006). **A standard protocol for describing individual-based and agent-based models.** Ecological Modelling. https://doi.org/10.1016/j.ecolmodel.2006.04.023 — protocolo ODD, descrição de agentes, regras/escala/agenda/experimentos.
8. Prusinkiewicz, P. & Lindenmayer, A. (1990). **The Algorithmic Beauty of Plants.** Livro de referência de modelos de ramificação procedurais, não validação fisiológica automática de qualquer planta de jogo. https://algorithmicbotany.org/papers/#abop
9. FAO-56, Allen et al. (1998). **Crop Evapotranspiration — Guidelines for Computing Crop Water Requirements.** https://www.fao.org/4/x0490e/x0490e00.htm — referência para possível fase L1 de evapotranspiração; não obrigatória no modelo L0.

## Engine e plataforma

10. MonoGame, **Supported Platforms**: https://docs.monogame.net/articles/getting_started/platforms.html
11. MonoGame, **New Web Platform #9515**: https://github.com/MonoGame/MonoGame/issues/9515

## Instruções ao Codex sobre referências

- Conferir fonte e aplicabilidade antes de promover fórmula para produção.
- Anotar se parâmetro provém de literatura, estimativa heurística ou calibração do jogo.
- Registrar intervalo, unidade, versão, propósito e limites; não extrapolar números sem justificativa.
- Citações apoiam o princípio ou fenômeno descrito, **não certificam** que a implementação própria é correta, estável ou rápida.

## Complemento consultado na revisão documental de 20/10/2026

- FAO, [Optimizing Nitrogen use on the farm](https://www.fao.org/4/y5146e/y5146e09.htm): apoia a interação entre disponibilidade hídrica, N e crescimento e a mineralização do N orgânico. Trata de culturas agrícolas; não fornece coeficientes calibrados para os perfis ecológicos fictícios deste MVP.
- Fontes MonoGame acima foram consultadas para separar plataforma oficialmente listada de trabalho em andamento. Uma issue aberta ou roadmap não demonstra compatibilidade do jogo, que permanece sem runtime.

As correções de reprodução, composição de N e transações são requisitos de consistência dos estoques escolhidos, não reprodução integral da fisiologia real nem validação experimental.

## Conferência pontual da segunda leitura

Em 20/10/2026 foram confirmados título/autoria/ano e pertinência geral das seguintes referências em fontes primárias:

- [BMI 2.0 — JOSS](https://joss.theoj.org/papers/10.21105/joss.02317): interfaces de acoplamento; não impõe framework ao MVP.
- [FINAM — GMD](https://gmd.copernicus.org/articles/18/4483/2025/): framework Python e acoplamento de modelos; referência conceitual, sem recomendar adoção no runtime C#.
- [Tan et al. — versão disponibilizada por coautor](https://philipp-andelfinger.net/pdfs/tan2021causality.pdf): causalidade e consistência de atualizações; a ordem concreta das fases do jogo é decisão própria a testar.
- [USGS — Water cycle processes](https://www.usgs.gov/publications/water-cycle-processes-poster): registro do pôster de 2024 e DOI GIP 235.
- [USGS — Infiltration and the Water Cycle](https://www.usgs.gov/water-science-school/science/infiltration-and-water-cycle): fatores qualitativos que influenciam infiltração; não valida o algoritmo L0.

A resolução direta de alguns DOIs falhou na ferramenta; as páginas das editoras/instituição e a versão do coautor permitiram a conferência acima. Não foi realizada revisão integral de todos os artigos nem calibração de modelos.


## Referências do contrato L0 (consulta de 21/10/2026)

SciSpace foi usado para localizar trabalhos sobre balanço diário de água/radiação, eficiência de uso de luz e decomposição; os valores de jogo não foram extraídos como calibração desses estudos.

- Davis et al. (2017), [SPLASH v1.0 — GMD](https://gmd.copernicus.org/articles/10/689/2017/), referência para formas compactas de radiação, evapotranspiração, solo e escoamento.
- Allen et al. (1998), [FAO-56](https://www.fao.org/4/X0490E/X0490E00.htm), padrão de referência para evapotranspiração; o L0 usa deliberadamente um proxy sem vento/vapor medidos.
- Kiniry et al. (1989), [radiation-use efficiency](https://doi.org/10.1016/0378-4290(89)90023-3), apoio à forma proporcional entre PAR interceptada e produção vegetal em culturas específicas.
- Bolker, Pacala & Parton (1998), [simplifying the Century decomposition model](https://doi.org/10.1890/1051-0761(1998)008%5B0425%3ALAOSDI%5D2.0.CO%3B2); Lutz et al. (2019), [LPJmL](https://gmd.copernicus.org/articles/12/2419/2019/), referências para cinética de primeira ordem modulada por condições ambientais.

Essas fontes apoiam escolhas de forma e não testam coeficientes, perfis animais, chuva sintética ou tolerâncias do cenário `temperate-valley-l0-v2`.
