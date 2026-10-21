# Aproveitamento seletivo do projeto anterior

O repositório `https://github.com/Julioall/Spectators-of-Life` é **referência de leitura**, não dependência operacional de `https://github.com/Julioall/genesis`.

## Itens úteis a analisar

Os caminhos abaixo são pistas do pacote de origem, ainda não auditados nesta revisão local. Confirmar existência e commit antes de reutilizar; a descrição não comprova o estado atual do repositório remoto.

| Origem no legado | Aplicação possível | Restrição |
|---|---|---|
| `docs/10-ecologia-e-biologia.md` | inventário de ciclo de vida, fluxos e cenários ecológicos | especificação, não calibração física automaticamente válida |
| `docs/specs/100-mundo-fisico.md` | unidades água/relevo, borda/fluxos, testes | revisar simplificações e no novo owner Hydro/Soil |
| `docs/specs/200-vegetacao-e-decomposicao.md` | ciclos vegetais, resíduos, nitrogênio, testes | isolar Botany/Ecology/Soil conforme ownership novo |
| `src/core/natural/environment-systems.ts` | regras atuais de clima, água/solo, decomposição/geomorfologia | contém múltiplas responsabilidades acopladas; não portar linha a linha |
| `src/core/natural/vegetation-system.ts` | crescimento, competição, reprodução, recursos | confirmar conversões/estoques e integração por contrato |
| `src/core/natural/types.ts` | nomes de campos e estados de célula | separar por owner; não reconstruir um giant world record mutável |
| `src/core/natural/parameters.ts`, `data/scenarios` | sementes, cenários, parâmetros comparativos | não assumir coeficientes reais calibrados |
| `tests/core/natural-world.test.ts`, `docs/evidence/` | regressão e experimentos de paridade | construir fixtures C# de comportamento, não alegar equivalência sem executar |
| `src/render/`, `data/assets/` | paleta, âncoras, elementos referenciais de composição | não converter PNGs importados em dependência de runtime; conservar apenas como referência documental permitida |
| `AGENTS.md`, `.agents/skills/` | boas práticas de testes e instruções Codex | revisar requisitos herdados específicos de Phaser |

## Processo de aproveitamento

1. Registrar path e commit SHA de origem.
2. Documentar regra algorítmica, entrada, saída, unidades, escala e limitação.
3. Verificar se a formulação é científica, heurística ou apenas visual.
4. Reescrever algoritmos em C# com contratos, estado e owner corretos.
5. Executar fixtures novas no mesmo cenário de referência quando possível; guardar diferenças explicitamente.
6. Nunca dizer que a arquitetura nova "foi migrada" antes de testar.

É permitido preservar o legado intocado; não fazer merge de bases nem apagar documentação histórica.
