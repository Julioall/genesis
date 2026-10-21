# Mathematics — base teórica e métodos numéricos

**Natureza:** documentação de fundamentos matemáticos. Não criar assembly `Game.ScientificFoundation.Mathematics`.

## Aspectos necessários no mundo inicial

- Grandezas, dimensões e conversão de unidades sem misturar estoque, taxa, fluxo acumulado e diagnóstico.
- Discretização espacial e conversões conservativas de área/volume na grade comum L0; interpolação e remapeamento entre grades só se forem adotadas resoluções distintas posteriormente.
- Métodos numéricos estáveis, limites, tolerâncias e análises de sensibilidade.
- Coordenadas, vetores, gradientes, vizinhanças D4/D8 e conectividade de bacias.
- PRNG determinístico com seeds por contexto, IDs, ordenação estável e reprodução documentada.
- Estatística descritiva e replicação entre seeds para eventos estocásticos.

## Critério de implementação

O processo que usa matemática deve ter **um responsável de domínio**. Helpers matemáticos genéricos, comprovadamente compartilhados, podem morar em `Game.Procedural` ou futura biblioteca `Game.Common.Numerics` aprovada por ADR. Não transformar toda fórmula matemática em serviço central.

## Limites

Igualdade bit a bit entre backends e GPUs não é automática. Comparações entre plataformas exigem testes e tolerâncias explicitadas, separando determinismo lógico e variação numérica.
