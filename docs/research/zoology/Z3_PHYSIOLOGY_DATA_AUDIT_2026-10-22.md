# Z3 — auditoria de dados fisiológicos por população

**Consulta:** SciSpace, 2026-10-22.\
**Uso:** mapear fontes primárias e limites antes de atribuir valores aos protótipos. Esta nota registra evidência, não autoriza copiar parâmetros para o runtime.

## Raposa-vermelha (*Vulpes vulpes*)

- Winstanley, Buttemer & Saunders (2003) mediram taxa metabólica de campo e renovação de água em raposas livres de New South Wales com água duplamente marcada. No outono, massas médias reportadas foram 5,6 kg para machos e 5,4 kg para fêmeas; as taxas metabólicas de campo médias foram 2.328 e 1.681 kJ/dia, respectivamente. São medidas sazonais e de uma população australiana, não valores de manutenção para todo mamífero ou toda raposa. [DOI](https://doi.org/10.1046/J.1365-2907.2003.00015.X)
- Winstanley (2000) acompanhou 163 juvenis livres na região de Central Tablelands, NSW. A massa corporal média se aproximou da adulta por volta de 16 semanas nesse conjunto; a gordura foi estimada indiretamente por índice renal, e idade/reprodução foram observadas em um ciclo anual específico. Isso é evidência de crescimento daquela população, não curva universal da espécie. [DOI](https://doi.org/10.1071/WR99072)
- Vogtsberger & Barrett (1973) mediram fluxos energéticos de quatro filhotes mantidos em cercados, com coelhos como alimento. É uma fonte experimental de contabilidade energética, mas o n pequeno, idade e dieta controlada limitam a transferência para animais livres. [DOI](https://doi.org/10.2307/3800315)

## Truta-marrom (*Salmo trutta*)

- Elliott (1976) constrói um orçamento que separa energia ingerida, fezes, excretas, variação de energia corporal e metabolismo; subdivide metabolismo em repouso, atividade e custo de processamento alimentar, relacionando os termos a massa, temperatura e ração. A estrutura é útil para a contabilidade de Z3. A aplicação depende das equações/tabelas e condições experimentais completas, não somente do resumo. [DOI](https://doi.org/10.2307/3590)
- Elliott & Hurley (1999) ajustaram modelo de balanço energético com dados de 210 trutas de incubatório e 252 descendentes de pais selvagens em cinco temperaturas controladas (5, 10, 13, 15 e 18 °C), dieta fixa de invertebrados e oxigênio saturado nos ensaios posteriores. O modelo reporta 12 parâmetros e boa adequação interna; a dieta, oxigenação, origem e regime térmico impedem tratar esses coeficientes como calibrados para qualquer rio. [DOI](https://doi.org/10.1046/J.1365-2427.1999.444483.X)
- Beamish (1964) mediu consumo padrão de oxigênio de várias espécies de água doce, incluindo truta-marrom, em relação a peso e temperatura. O expoente alométrico reportado para truta-marrom foi 0,877 na regressão logarítmica daquele experimento; não substitui uma curva térmica por população. [DOI](https://doi.org/10.1139/Z64-016)
- Hayes, Stark & Shearer (2000) combinaram forrageamento por deriva e bioenergética ao longo da vida e compararam previsões de tamanho por idade com observações de uma população de rio na Nova Zelândia. O estudo mostra a dependência conjunta de temperatura, alimento transportado e custo metabólico; não é um perfil universal de espécie. [DOI](https://doi.org/10.1577/1548-8659(2000)129%3C0315:DATOAW%3E2.0.CO;2)
- Vøllestad, Olsen & Forseth (2002) observaram crescimento em cinco riachos próximos, com sete populações isoladas na Noruega: crescimento dependeu de temperatura e estação e também variou entre locais após controlar massa e idade, com efeitos de densidade e competidor. [DOI](https://doi.org/10.1111/j.1095-8649.2002.tb02494.x)
- Parra et al. (2014) compararam populações espanholas e norueguesas e encontraram diferenças associadas a latitude, altitude e sexo em longevidade, tamanho e idade de maturação e fecundidade. [DOI](https://doi.org/10.1007/s10682-014-9702-2)

## Decisão para o modelo

1. Manter os números atuais de Z1 e N3 classificados como valores de protótipo, sem os converter em parâmetros naturais.
2. Não consolidar uma única ficha fisiológica global para *Vulpes vulpes* ou *Salmo trutta*. A ficha futura deve declarar população, estágio/idade, sexo quando aplicável, dieta, ambiente térmico, unidades, método e intervalo de validade.
3. Implementar primeiro o balanço dimensional (energia separada de matéria seca, água e nitrogênio); depois ajustar coeficientes com tabelas/dados primários completos e comparar série temporal, não só um valor médio.
4. Antes de fechar Z3, escolher as populações que representam o ambiente do jogo. Os protótipos atuais são genéricos e não identificam raposa, truta ou uma população de referência.

## Prioridade energética durante escassez

Uma busca SciSpace adicional comparou modelos de alocação entre manutenção, atividade, crescimento e reprodução:

- Sibly et al. (2013) propõem, para modelos de população baseados em agentes, uma regra de prioridade: manutenção, crescimento, reprodução e armazenamento quando há energia suficiente; abaixo de uma reserva crítica, direcionar energia à manutenção. É uma recomendação de modelagem, não uma lei universal demonstrada para todos os táxons. O artigo também enfatiza massa e temperatura para aquisição/alocação. [DOI](https://doi.org/10.1111/2041-210X.12002)
- Hou et al. (2008) apresentam um modelo comparativo empiricamente fundamentado em dados de aves e mamíferos, repartindo energia entre manutenção, biossíntese, atividade e armazenamento. Isso sustenta separar processos e energia da biomassa, mas o resumo consultado não fornece os parâmetros necessários para calibrar os perfis atuais. [DOI](https://doi.org/10.1126/science.1162302)
- Kooijman & Lika (2014) descrevem a regra de partição DEB por fração κ e compilam estimativas para 276 espécies; a própria variedade de espécies e parâmetros impede usar um único κ como perfil universal. [DOI](https://doi.org/10.1111/brv.12082)

Como decisão de engenharia L0, o N3 agora reserva energia para manutenção basal antes de permitir custo de locomoção; a distância percorrida é limitada pelo saldo após a manutenção. A energia não paga de manutenção é relatada separadamente e alimenta o déficit que pode levar à mortalidade. Esta política simplificada foi inspirada na prioridade discutida por Sibly et al.; não implementa a reserva crítica, a partição de crescimento/reprodução nem a regra κ. A taxa de 10 J/m e os perfis funcionais continuam proxies, portanto a mudança não fecha a calibração Z3.

## Próximo trabalho

Escolher população e estágio de vida; recuperar das fontes primárias as equações e tabelas necessárias; registrar dieta, temperatura, unidades e validade numa ficha versionada; e então criar cenários de calibração. A documentação disponível no SciSpace identifica caminhos de pesquisa; PDFs/tabelas completos ainda precisam de inspeção antes de afirmar calibração.
