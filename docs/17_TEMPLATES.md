# Templates para módulos, processos científicos, contratos e ADRs

## A. Ficha de processo científico

```markdown
# SCI-[NNN] — [processId]
- Owner:
- Submódulo:
- Status: L0 | L1 | L2 | Proposto
- Fonte científica / heurística:
- Escopo, condições de aplicação e simplificações:
- Entradas: (nome, unidade, tipo estoque/taxa/campo, escala, instante)
- Saídas e efeitos: (grandeza, unidade, owner destino)
- Equação/algoritmo:
- Valores iniciais e condições de borda:
- Fonte/sink de matéria/energia:
- Cadência e janela temporal:
- Capacidade/contrato público:
- Sensibilidade e limites:
- Cenários de teste e tolerâncias:
- Versionamento e migração:
```

## B. Manifesto de módulo (ilustrativo)

```json
{
  "moduleId": "game.modules.biosphere",
  "moduleVersion": "0.1.0",
  "contractsProvided": ["IBiomassResourceV1", "IVegetationCoverageV1"],
  "contractsRequired": ["IClimateBasicV1", "ISoilWaterV1", "ISoilNutrientsV1", "ISoilOrganicMatterV1", "ISurfaceWaterV1"],
  "contractsOptional": [],
  "phases": ["ProposeBiologicalDemands", "ApplyBiologicalResults"],
  "stateSchemaVersion": 1
}
```

O manifesto acima representa Biosphere completo em N3. Em N2, declarar apenas as capacidades usadas por Botany/Ecology. O retorno à matéria orgânica do solo é obrigatório para a decomposição definida; uma fixture de consumidor somente hídrico pode ignorar essa capacidade, mas o ciclo ecológico completo não.

## C. ADR

```markdown
# ADR-NNN — [decisão]
- Data:
- Status: proposta | aceita | substituída
- Contexto científico e técnico:
- Opções consideradas:
- Decisão:
- Justificativa e evidências:
- Impacto em owner/contratos/tempo/saves/plataforma:
- Teste que reprova decisão:
- Consequências e migração:
```

## D. Relatório de experimento

```markdown
# EXP-NNN — [cenário]
- Objetivo:
- Commit/build/plataforma/hardware:
- Seed, versão, parâmetros e duração:
- Hipótese e previsão direcional:
- Testes e tolerâncias fixados antes:
- Resultados numéricos e arquivos de evidência:
- Balanço massa/N/água:
- Contraexemplos, seeds que falharam e limitações:
- Decisão: gate aprovado | reprovado | inconclusivo
```

## E. Checklist para novas capacidades

- Owner e versão claros.
- Novas variáveis não redefinem semântica de antigas.
- Um consumidor sem capacidade opcional opera apenas sob política documentada.
- Snapshot não precisa expor estruturas mutáveis privadas.
- Save e schema atualizados quando necessário.
- Contract tests novos e antigos executam.
