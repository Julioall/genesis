# Plataforma, MonoGame e a decisão web antes do investimento irreversível

## Situação verificada em 21/10/2026

MonoGame documenta oficialmente Windows, macOS, Linux, Android e iOS. A proposta `MonoGame.Web` oficial continua em desenvolvimento conforme GitHub issue #9515 (aberta em 26/08/2026). Portanto, **não existe evidência suficiente para prometer a versão browser no mesmo nível do desktop apenas por escolher MonoGame**.

Fontes:
- https://docs.monogame.net/articles/getting_started/platforms.html
- https://github.com/MonoGame/MonoGame/issues/9515
- https://monogame.net/

## Estratégia

1. Usar o núcleo e os domínios já exercitados em N0/N1, compiláveis e testáveis **sem MonoGame**; o spike não exige criar todos os projetos previstos.
2. Construir cliente desktop simples com MonoGame para visualizar mundo e desempenho real.
3. Durante N0/N1, elaborar spike web com mesmo core: criar mundo, avançar ticks, renderizar mapa simples, registrar input e executar no browser real com logs e limitações. Confirmar no momento se rota oficial MonoGame.Web, KNI ou adaptador alternativo é adequada.
4. No spike, verificar boot, ticks e representação mínima; registrar limitações encontradas. Input completo, memória, threads/WASM, shaders/atlas, bundle, save e profiling são critérios de uma futura entrega web, não bloqueadores de N0. Preferir a simulação compartilhada a reescrever as regras.
5. Decidir Go/No-Go web com evidências: build reproduzível, navegador alvo, desempenho, manutenção e custo de portabilidade. Bloqueio documentado impede afirmação pública de multiplataforma completa, não a evolução do desktop.

## Critérios de comparação

- Código C# compartilhado sem `#if` dominando módulos.
- Disponibilidade e licença de bibliotecas de render.
- Limites de memória/threads de browser e integração com .NET/WASM.
- Capacidade de gerar texturas/atlas procedural sem arquivos artísticos externos.
- Performance com 1, 100 e 1000 organismos visíveis, mapa e clima ativos.
- Ferramentas CI e rotina de testes automáticos aplicáveis ao browser.

Nunca alterar a fundamentação científica documental ou os domínios para satisfazer incidentalmente um navegador antes de tentar adaptadores.

## Orçamento da avaliação inicial

O spike N0/N1 tem limite proposto de um dia de trabalho. Ao atingir o limite, registrar evidências, lacunas e Go/No-Go/inconclusivo; não iniciar segundo cliente completo. Benchmarks de 1/100/1000 organismos são comparação posterior quando houver runtime suficiente, não pré-requisito para boot de N0. Verificar novamente o suporte oficial quando a implementação começar.
