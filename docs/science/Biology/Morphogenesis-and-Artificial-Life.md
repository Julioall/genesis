# Science / Biology — morfogênese computacional e vida artificial

**Status:** fundamentação científica documental; não é uma biblioteca `Game.ScientificFoundation`.

## Conceitos

- **Espécie/população:** conceito ecológico e reprodutivo; no software, iniciar com uma linhagem parametrizada e regras de compatibilidade, não com uma definição biológica universal.
- **Genótipo:** instruções herdáveis e parâmetros de desenvolvimento. Não é sinônimo de sprite nem de estrutura corporal pronta.
- **Fenótipo:** manifestação corporal e fisiológica resultante de genoma, ontogenia e ambiente.
- **Ontogenia:** desenvolvimento de um indivíduo, incluindo crescimento, maturação e senescência.
- **Morfogênese:** formação do corpo e de padrões/estruturas durante o desenvolvimento.
- **Plasticidade fenotípica:** alterações do fenótipo por condições ambientais, sem que se tornem mutações hereditárias automaticamente.
- **Adaptação evolutiva:** mudança herdável em populações ao longo de gerações, sob pressões seletivas; não confundir com aprendizagem individual.
- **Controle corporificado:** a forma do corpo condiciona e restringe movimentos disponíveis, percepção e gasto energético.

## Estratégias computacionais

### Representação direta

Genes descrevem proporções (comprimento de cabeça, membros, tamanho de nadadeira, etc.), dentro de faixa anatômica. Simples e controlável, ideal inicialmente.

### Representação indireta / gramática de desenvolvimento

Genes codificam regras que produzem estruturas, por exemplo segmentação e padrões de ramificação. Potencial de inovação estrutural superior, mas torna mais difícil garantir viabilidade anatômica.

### Codificações gráficas

Grafos para segmentos, juntas, relações pai-filho e tecido; verificar conectividade, ausência de ciclos cinemáticos indevidos, centro de massa e validade de colisores.

### Reação-difusão de pigmentos

Dois campos químicos abstratos com reação e difusão sobre superfície parametrizada podem produzir padrões não uniformes. É um *modelo inspirado* em morfogênese, não prova de mecanismo molecular para cada espécie.

### Biomecânica simplificada

Caracterizar corpo por comprimento, massa, centro de massa, comprimento de membros, comprimento de passo, fase da marcha, custo locomotor e habilidade de atravessar habitats. Usar IK para pose, mas manter separado o sistema que determina deslocamento físico e consumo energético.

### Crescimento

Genes determinam potencial/limites; idade, ingestão, condições térmicas e saúde influenciam a expressão. Mudar escala visual não deve criar biomassa; massa e reserva energética são estados distintos.

## Princípios normativos

1. Não misturar variáveis observadas do ambiente com genes herdáveis.
2. Não permitir híbridos biologicamente implausíveis automaticamente apenas porque os dados possuem campos semelhantes.
3. Não prometer diversidade zoológica ilimitada sem ampliar os planos corporais.
4. O primeiro recorte pode usar regras de crescimento/energia simplificadas, com unidades e limites documentados.
5. Em qualquer etapa, distinguir plausibilidade visual, plausibilidade cinemática, coerência ecológica e validação quantitativa.
6. A representação gráfica poderá ser pixel art 2D, 2.5D ou 3D; a decisão de visual não muda a responsabilidade científica do módulo zoológico.

## Referências principais

Lai et al. 2021; Sims 1994; Tu & Terzopoulos 1994; Turing 1952; Murray 1981; Coros et al. 2011; Kooijman & Lika 2014. DOI e links em `02_PROJETOS_E_BIBLIOGRAFIA.md`.
