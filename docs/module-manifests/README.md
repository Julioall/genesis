# Manifests dos módulos

Os arquivos JSON declaram o marco, os processos/estoques de responsabilidade e as capabilities publicadas. `roadmapPhase` indica onde o módulo participa do roadmap; não significa que o gate inteiro esteja aprovado. `coreMediatedInputs` registra trocas coordenadas por Core, sem criar referências entre assemblies de domínio.

Estes onze manifests são metadados estáticos para revisão e testes. O runtime não os carrega, e eles não introduzem descoberta dinâmica ou plugins. Os quatro manifests N1 cobrem Cosmos–Atmosphere–Geosphere–Hydrosphere; o manifest N2 descreve Botany/Ecology. `zoology.n3.json` descreve o submódulo Zoology no mesmo assembly e mantém o escopo L0. `zoology.z1.json`, `zoology.z2.json`, `zoology.z3.json`, `creatures.z4.json` e `zoology.z5.json` registram os gates zoológicos e procedurais em andamento/validados. Z3 contém somente um subgate de contabilidade dimensional de energia; não fornece parâmetros calibrados nem altera o N3. N1, N2 e N3 estão validados no escopo documentado; N3 inclui throughput headless de 21,40 dias simulados/s contra a meta de 20.

`ownedPersistentFields` declara a autoridade pretendida. Parte do armazenamento ainda fica agregada em `WorldCell`; veja [a auditoria modular](../reports/MODULE_AUDIT_2026-10-22.md) para o estado de enforcement no código.
