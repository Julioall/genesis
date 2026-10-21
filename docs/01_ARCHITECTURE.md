# Arquitetura APMA-Natural 2.0 — módulos principais + submódulos

## Princípio central

**Monólito modular** em C#: domínios com autoridades claras de estado, contratos estáveis, funções puras compartilhadas somente quando necessário e Kernel orquestrador. **Realimentação entre fenômenos** é diferente de **dependência circular no código**.

## Estrutura sugerida (ajustável por evidência)

```text
repo-root/
├── README.md
├── AGENTS.md
├── Game.sln
├── Directory.Build.props
├── src/
│   ├── Game.Contracts/
│   ├── Game.Core/                   # Kernel, scheduler, registro e runtime headless
│   ├── Game.World/                  # storage, chunks, snapshots, índices e IDs
│   ├── Game.Procedural/
│   ├── Game.Persistence/
│   ├── Game.Modules.Cosmos/
│   │   ├── OrbitalMechanics/ SolarRadiation/ LunarDynamics/ AstronomicalCycles/
│   ├── Game.Modules.Atmosphere/
│   │   ├── Thermodynamics/ AtmosphericCirculation/ HumidityAndClouds/ Precipitation/
│   ├── Game.Modules.Geosphere/
│   │   ├── Terrain/ Geology/ Soil/ Geomorphology/
│   ├── Game.Modules.Hydrosphere/
│   │   ├── SurfaceWater/ Groundwater/ SnowAndIce/ WaterQuality/
│   ├── Game.Modules.Biosphere/
│   │   ├── Botany/ Zoology/ Ecology/
│   ├── Game.Rendering/
│   ├── Game.Client.Desktop/
│   └── Game.Tools/
├── tests/
│   ├── Game.Tests.Unit/
│   ├── Game.Tests.Contracts/
│   ├── Game.Tests.Integration/
│   ├── Game.Tests.Determinism/
│   └── Game.Tests.Performance/
├── data/                        # parâmetros/receitas, não assets obrigatórios
└── docs/
    ├── README.md
    ├── GDD.md
    ├── 01_ARCHITECTURE.md
    ├── science/
    │   ├── Mathematics.md
    │   ├── Physics.md
    │   ├── Chemistry.md
    │   └── Biology.md
    ├── specs/
    └── decisions/
```

Os diretórios de submódulo são **internos** ao assembly de domínio; não criar projetos por pasta sem um ADR que justifique seu próprio ciclo de vida/estado/cadência. Arquivos futuros não precisam existir como diretórios vazios.

## Separação explícita: documentos versus código

`docs/GDD.md`, `docs/01_ARCHITECTURE.md`, `docs/specs/`, `docs/decisions/` e `docs/science/` não são projetos C#. Não deve existir `src/Game.ScientificFoundation/`, `Game.ScientificFoundation.csproj` nem namespace equivalente exigido pela arquitetura. Processos científicos são documentados em `docs/science` e implementados pelos domínios responsáveis; helpers matemáticos transversais podem ser extraídos para `Game.Procedural` ou `Game.Common.Numerics` apenas mediante necessidade real e ADR. Consulte [ADR-0001](decisions/ADR-0001-Science-Is-Documentation.md).

## Direção de dependências

`docs/science/` → fundamentação científica documental, fora do grafo de assemblies. `Contracts` → bibliotecas padrão e unidades/tipos compartilhados mínimos. `World` e `Core` → Contracts; módulos de domínio → Contracts + utilitários procedurais puros permitidos, jamais outros módulos de domínio. `Rendering` → Contracts e dados de visualização; executáveis headless/desktop → Core e implementações necessárias à composição. Dependência de adaptadores gráficos só no cliente/rendering. **Nenhum `Game.Modules.X` referencia `Game.Modules.Y`.** Dependências exatas no `.csproj` validadas por testes arquiteturais.

## Quem faz o quê

- **Core/Kernel:** sequência, registro explícito de contratos recebidos dos executáveis, filas, coordenação de transações, timestamps, RNG contextual e observabilidade. A composição de implementações concretas pertence aos executáveis. Não conter fórmulas climáticas ou de vida.
- **World:** persistência temporária de campos e entidades de cada domínio via autoridade; não é dicionário público mutável.
- **Contracts:** definições de observação, pedido, confirmação e evento, escala, unidade e tempo.
- **Docs/science:** documenta leis, fórmulas, hipóteses e limites; **não executa código**. A implementação de cada processo pertence a seu módulo `Game.Modules.*`.
- **Procedural:** ruídos, geradores e receitas; separar a geração inicial da evolução do estado vivo.
- **Module:** estados/algoritmos pertencentes a um grande domínio; seus submódulos podem colaborar internamente.
- **Client:** representação e controles; não participa da verdade simulada.

### Fronteira da apresentação

O cliente MonoGame recebe snapshots e converte seus campos em malhas, instâncias, texturas, materiais, partículas ou outras formas visuais. O modelo de dados de apresentação não vira owner de relevo, água ou organismos. A simulação pode manter grades e cadências próprias mesmo quando a apresentação usa geometria tridimensional, interpolação ou níveis de detalhe.

Isolar dependências específicas de MonoGame no cliente gráfico. O core, os módulos, testes científicos, determinismo, save/load e runner headless não referenciam API gráfica, câmera, FPS, GPU ou carregamento visual. Atualizações de apresentação não alteram parâmetros L0 nem estado do mundo.

## Critério para divisão

Criar projeto C# próprio só se houver forte necessidade de versionamento, ciclos separados, testes isolados, ownership independente ou reuso fora do domínio. Normalmente `Botany/Trees` e `Botany/Grass` são estratégias/receitas dentro de `Game.Modules.Biosphere`, não DLLs independentes.

## `Game.Services` não é uma lixeira arquitetural

Usar `Game.Services.Diagnostics` ou `Game.Services.Storage` apenas para serviços infraestruturais claramente delimitados; proibir `GlobalServices` ou `NatureManager` que agreguem clima, solo e vida no mesmo objeto.

## Limite de infraestrutura L0

O desenho acima é uma proposta documental, não uma ordem para criar todos os projetos em N0. Criar cada projeto quando houver comportamento exercitado. Os cinco domínios mantêm suas fronteiras; ferramentas, testes por categoria, World, Persistence e Rendering podem surgir incrementalmente. Não adicionar microserviços, CQRS, event sourcing, ECS genérico, carregamento dinâmico de plugins, barramento distribuído ou reflexão para descoberta de módulos no MVP.

Usar composição explícita no executável headless/desktop: ele pode referenciar Core e implementações dos domínios para registrá-las. Core recebe contratos e não referencia módulos concretos. World fornece armazenamento técnico sem decidir regras nem obter autoridade sobre estoques; módulos mantêm escrita exclusiva. Persistence serializa estados exportados pelos owners e restaura em checkpoint validado. Rendering consome snapshots; comandos de UI são enfileirados para ticks definidos. Isso resolve composição sem ciclos de assemblies.

Capabilities são interfaces pequenas e registro estático em memória; não exigem framework de plugins ou negociação remota. Uma função pura basta para algoritmos sem estado; Strategy só quando houver alternativa real (incluindo o teste de substituição de Soil); máquina de estados simples para fauna; composição em vez de hierarquias por espécie. Não criar interface para cada classe. Ver [regras de codificação](19_CODING_RULES.md).
