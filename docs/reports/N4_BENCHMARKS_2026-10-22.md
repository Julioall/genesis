# N4 — saídas brutas dos benchmarks

**Data:** 22/10/2026\
**Build:** Release, .NET SDK 10.0.400\
**Build dos comparativos visuais:** SHA-256 `495111FC7040E2D09214C48681267C37AC1EB9FF34A8DE8BF4D2A3D2C43E8688`\
**Build revalidado após ajuste da superfície de água:** SHA-256 `DF5F80B68D6E5F0D88036A668E670DC37F11E4CA0B9AA7B00321F14C964BAA88`\
**Build da iteração 56:** SHA-256 `69E8FC10A62EEC21D393AD4472A05CE7D3655807D1C8E1C4DF10BDD7F192223F`\
**Build da iteração 57:** SHA-256 `9DB0F568E248BE7B4336CCB3701FE2AC08589A302CDF4239A5A8F7EAAC408F2D`\
**Hardware:** Windows 11; Intel Core i3-10105; 15,87 GiB RAM; NVIDIA GeForce RTX 5060, driver 610.88, 8.151 MiB de VRAM.

Cada execução teve 5 s de aquecimento e 60 s medidos em 1280×720, VSync ligado. Nas janelas visuais, o jogo ficou pausado no snapshot seed 1/dia 172; câmera e cena foram fixadas. Nos dados de memória, `workingSetMiB` e `privateMemoryMiB` pertencem ao processo. Não foi possível atribuir memória de GPU ao processo individual.

## Cliente padrão — amostra original, simulação ativa

Comando:

```powershell
dotnet src/Game.Client.Desktop/bin/Release/net10.0/Game.Client.Desktop.dll --snapshot-file bin/n4-evaluation-20261022/seed-1-day000.json --benchmark-seconds 60
```

Saída da build anterior:

```text
benchmark=desktop-l0 projection=ORTHOGRAPHIC cameraZoom=1.00 cameraPanM=(0.0,0.0) resolution=1280x720 vsync=True fixedTimeStep=True seed=1 simulationPaused=False ticksPerSecond=6 visualInstanceMultiplier=1 visualSceneBuildMs=102.42 warmupSeconds=5 measuredSeconds=60.0 frames=3576 averageFps=59.58 p95FrameMs=17.30 p99FrameMs=36.42 maxFrameMs=208.16 cpuPercentOfOneCore=38.2 workingSetMiB=209.3 privateMemoryMiB=230.4 maxDrawCountPerFrame=736 maxPrimitivesPerFrame=355114 visibleTrees=621 treeBillboards=0 treeBillboardVertices=0 floraVertices=563544 groundCoverClusters=0 understoryShrubs=0 shadowVertices=0 evaluationRocks=0 faunaSprites=14 faunaLayoutHash=4AD4A1BA1BFD66BA7A9A2624A59662AB3AAACF80A2C9663BF009DC63540C9364 waterBuildMaxMs=31.44 spriteBuildMaxMs=102.42 terrainTextureBuildMaxMs=47.42
```

## Cliente padrão — revalidação final da água, simulação ativa

Comando:

```powershell
dotnet src/Game.Client.Desktop/bin/Release/net10.0/Game.Client.Desktop.dll --snapshot-file bin/n4-evaluation-20261022/seed-1-day000.json --benchmark-seconds 60
```

Saída da build com a superfície de poças por componentes e malha de água em três subdivisões:

```text
benchmark=desktop-l0 projection=ORTHOGRAPHIC cameraZoom=1.00 cameraPanM=(0.0,0.0) resolution=1280x720 vsync=True fixedTimeStep=True seed=1 simulationPaused=False ticksPerSecond=6 visualInstanceMultiplier=1 visualSceneBuildMs=105.74 warmupSeconds=5 measuredSeconds=60.0 frames=3576 averageFps=59.60 p95FrameMs=17.29 p99FrameMs=36.73 maxFrameMs=200.84 cpuPercentOfOneCore=34.4 workingSetMiB=256.1 privateMemoryMiB=278.5 maxDrawCountPerFrame=736 maxPrimitivesPerFrame=355114 visibleTrees=621 treeBillboards=0 treeBillboardVertices=0 floraVertices=563544 groundCoverClusters=0 understoryShrubs=0 shadowVertices=0 evaluationRocks=0 faunaSprites=14 faunaLayoutHash=4AD4A1BA1BFD66BA7A9A2624A59662AB3AAACF80A2C9663BF009DC63540C9364 waterBuildMaxMs=48.61 spriteBuildMaxMs=105.74 terrainTextureBuildMaxMs=44.79
```

A janela final passa o gate de média, P95 e P99. A malha com seis subdivisões foi testada e descartada: não trouxe diferença visual perceptível e teve P99 de 67,99 ms, com rebuild máximo de água de 77,19 ms.

### Experimento descartado — seis subdivisões de água

```text
benchmark=desktop-l0 projection=ORTHOGRAPHIC cameraZoom=1.00 cameraPanM=(0.0,0.0) resolution=1280x720 vsync=True fixedTimeStep=True seed=1 simulationPaused=False ticksPerSecond=6 visualInstanceMultiplier=1 visualSceneBuildMs=87.86 warmupSeconds=5 measuredSeconds=60.0 frames=3457 averageFps=57.62 p95FrameMs=17.26 p99FrameMs=67.99 maxFrameMs=238.36 cpuPercentOfOneCore=35.4 workingSetMiB=219.4 privateMemoryMiB=239.5 maxDrawCountPerFrame=736 maxPrimitivesPerFrame=369196 visibleTrees=621 treeBillboards=0 treeBillboardVertices=0 floraVertices=563544 groundCoverClusters=0 understoryShrubs=0 shadowVertices=0 evaluationRocks=0 faunaSprites=14 faunaLayoutHash=4AD4A1BA1BFD66BA7A9A2624A59662AB3AAACF80A2C9663BF009DC63540C9364 waterBuildMaxMs=77.19 spriteBuildMaxMs=100.87 terrainTextureBuildMaxMs=48.74
```

## Perspectiva 3D procedural — amostra aceita

Comando:

```powershell
dotnet src/Game.Client.Desktop/bin/Release/net10.0/Game.Client.Desktop.dll --visual-evaluation --snapshot-file bin/n4-evaluation-20261022/seed-1-day172.json --projection perspective --reference-landscape-view --camera-zoom 3.4 --volume-canopy --distance-aware-tree-lod --benchmark-seconds 60
```

Saída:

```text
benchmark=visual-evaluation projection=PERSPECTIVE cameraZoom=3.40 cameraPanM=(0.0,0.0) resolution=1280x720 vsync=True fixedTimeStep=True seed=1 simulationPaused=True ticksPerSecond=6 visualInstanceMultiplier=1 visualSceneBuildMs=251.63 warmupSeconds=5 measuredSeconds=60.0 frames=3601 averageFps=60.00 p95FrameMs=17.22 p99FrameMs=17.71 maxFrameMs=41.36 cpuPercentOfOneCore=30.0 workingSetMiB=210.8 privateMemoryMiB=239.5 maxDrawCountPerFrame=29 maxPrimitivesPerFrame=443254 visibleTrees=621 treeBillboards=0 treeBillboardVertices=0 floraVertices=716145 groundCoverClusters=2360 understoryShrubs=145 shadowVertices=33990 evaluationRocks=277 faunaSprites=2 faunaLayoutHash=43931487CF2182AFDFFED2A8A701D17DF7959CF8BBA61CA9E689ECD7461CD054 waterBuildMaxMs=0.00 spriteBuildMaxMs=0.00 terrainTextureBuildMaxMs=0.00
```

## Perspectiva 3D procedural — iteração 56, copas subdivididas

Comando:

```powershell
dotnet src/Game.Client.Desktop/bin/Release/net10.0/Game.Client.Desktop.dll --visual-evaluation --snapshot-file bin/n4-evaluation-20261022/seed-1-day172.json --projection perspective --reference-landscape-view --camera-zoom 3.4 --volume-canopy --distance-aware-tree-lod --benchmark-seconds 60
```

Saída da revisão final, após correção da iluminação por vértice:

```text
benchmark=visual-evaluation projection=PERSPECTIVE cameraZoom=3.40 cameraPanM=(0.0,0.0) resolution=1280x720 vsync=True fixedTimeStep=True seed=1 simulationPaused=True ticksPerSecond=6 visualInstanceMultiplier=1 visualSceneBuildMs=285.70 warmupSeconds=5 measuredSeconds=60.0 frames=3600 averageFps=60.00 p95FrameMs=17.25 p99FrameMs=17.78 maxFrameMs=42.35 cpuPercentOfOneCore=30.8 workingSetMiB=287.7 privateMemoryMiB=332.7 maxDrawCountPerFrame=29 maxPrimitivesPerFrame=693968 visibleTrees=621 treeBillboards=0 treeBillboardVertices=0 floraVertices=1468287 groundCoverClusters=2360 understoryShrubs=145 shadowVertices=33990 evaluationRocks=277 faunaSprites=2 faunaLayoutHash=43931487CF2182AFDFFED2A8A701D17DF7959CF8BBA61CA9E689ECD7461CD054 waterBuildMaxMs=0.00 spriteBuildMaxMs=0.00 terrainTextureBuildMaxMs=0.00
```

## Perspectiva 3D procedural — iteração 57, contato das rochas

Comando:

```powershell
dotnet src/Game.Client.Desktop/bin/Release/net10.0/Game.Client.Desktop.dll --visual-evaluation --snapshot-file bin/n4-evaluation-20261022/seed-1-day172.json --projection perspective --reference-landscape-view --camera-zoom 3.4 --volume-canopy --distance-aware-tree-lod --benchmark-seconds 60
```

Saída da revisão combinada das copas subdivididas e rochas conformadas ao relevo:

```text
benchmark=visual-evaluation projection=PERSPECTIVE cameraZoom=3.40 cameraPanM=(0.0,0.0) resolution=1280x720 vsync=True fixedTimeStep=True seed=1 simulationPaused=True ticksPerSecond=6 visualInstanceMultiplier=1 visualSceneBuildMs=290.91 warmupSeconds=5 measuredSeconds=60.0 frames=3600 averageFps=59.98 p95FrameMs=17.21 p99FrameMs=17.59 maxFrameMs=47.80 cpuPercentOfOneCore=29.2 workingSetMiB=286.5 privateMemoryMiB=328.9 maxDrawCountPerFrame=29 maxPrimitivesPerFrame=696787 visibleTrees=621 treeBillboards=0 treeBillboardVertices=0 floraVertices=1468287 groundCoverClusters=2360 understoryShrubs=145 shadowVertices=33990 evaluationRocks=277 faunaSprites=2 faunaLayoutHash=43931487CF2182AFDFFED2A8A701D17DF7959CF8BBA61CA9E689ECD7461CD054 waterBuildMaxMs=0.00 spriteBuildMaxMs=0.00 terrainTextureBuildMaxMs=0.00
```

## Perspectiva híbrida — LOD volumétrico e billboards

Comando:

```powershell
dotnet src/Game.Client.Desktop/bin/Release/net10.0/Game.Client.Desktop.dll --visual-evaluation --snapshot-file bin/n4-evaluation-20261022/seed-1-day172.json --projection perspective --reference-landscape-view --camera-zoom 3.4 --hybrid-tree-lod --benchmark-seconds 60
```

Saída:

```text
benchmark=visual-evaluation projection=PERSPECTIVE cameraZoom=3.40 cameraPanM=(0.0,0.0) resolution=1280x720 vsync=True fixedTimeStep=True seed=1 simulationPaused=True ticksPerSecond=6 visualInstanceMultiplier=1 visualSceneBuildMs=149.95 warmupSeconds=5 measuredSeconds=60.0 frames=3600 averageFps=60.00 p95FrameMs=17.19 p99FrameMs=17.89 maxFrameMs=45.10 cpuPercentOfOneCore=29.8 workingSetMiB=188.7 privateMemoryMiB=204.6 maxDrawCountPerFrame=33 maxPrimitivesPerFrame=278226 visibleTrees=621 treeBillboards=619 treeBillboardVertices=11142 floraVertices=209919 groundCoverClusters=2360 understoryShrubs=145 shadowVertices=33990 evaluationRocks=277 faunaSprites=2 faunaLayoutHash=43931487CF2182AFDFFED2A8A701D17DF7959CF8BBA61CA9E689ECD7461CD054 waterBuildMaxMs=0.00 spriteBuildMaxMs=0.00 terrainTextureBuildMaxMs=0.00
```

## Perspectiva 3D — população visual 10×

Comando:

```powershell
dotnet src/Game.Client.Desktop/bin/Release/net10.0/Game.Client.Desktop.dll --visual-evaluation --snapshot-file bin/n4-evaluation-20261022/seed-1-day172.json --projection perspective --reference-landscape-view --camera-zoom 3.4 --volume-canopy --distance-aware-tree-lod --visual-instance-multiplier 10 --benchmark-seconds 60
```

Saída:

```text
benchmark=visual-evaluation projection=PERSPECTIVE cameraZoom=3.40 cameraPanM=(0.0,0.0) resolution=1280x720 vsync=True fixedTimeStep=True seed=1 simulationPaused=True ticksPerSecond=6 visualInstanceMultiplier=10 visualSceneBuildMs=1221.66 warmupSeconds=5 measuredSeconds=60.0 frames=3600 averageFps=59.98 p95FrameMs=17.17 p99FrameMs=17.64 maxFrameMs=50.06 cpuPercentOfOneCore=29.9 workingSetMiB=768.6 privateMemoryMiB=1071.6 maxDrawCountPerFrame=29 maxPrimitivesPerFrame=2509612 visibleTrees=6210 treeBillboards=0 treeBillboardVertices=0 floraVertices=6711435 groundCoverClusters=2360 understoryShrubs=145 shadowVertices=235194 evaluationRocks=277 faunaSprites=2 faunaLayoutHash=43931487CF2182AFDFFED2A8A701D17DF7959CF8BBA61CA9E689ECD7461CD054 waterBuildMaxMs=0.00 spriteBuildMaxMs=0.00 terrainTextureBuildMaxMs=0.00
```

## Ortográfica 3D procedural

Comando:

```powershell
dotnet src/Game.Client.Desktop/bin/Release/net10.0/Game.Client.Desktop.dll --visual-evaluation --snapshot-file bin/n4-evaluation-20261022/seed-1-day172.json --projection orthographic --reference-landscape-view --camera-zoom 2.8 --volume-canopy --distance-aware-tree-lod --benchmark-seconds 60
```

Saída:

```text
benchmark=visual-evaluation projection=ORTHOGRAPHIC cameraZoom=2.80 cameraPanM=(0.0,0.0) resolution=1280x720 vsync=True fixedTimeStep=True seed=1 simulationPaused=True ticksPerSecond=6 visualInstanceMultiplier=1 visualSceneBuildMs=260.12 warmupSeconds=5 measuredSeconds=60.0 frames=3600 averageFps=59.98 p95FrameMs=17.17 p99FrameMs=17.63 maxFrameMs=51.67 cpuPercentOfOneCore=29.8 workingSetMiB=215.5 privateMemoryMiB=244.8 maxDrawCountPerFrame=29 maxPrimitivesPerFrame=459760 visibleTrees=621 treeBillboards=0 treeBillboardVertices=0 floraVertices=765591 groundCoverClusters=2360 understoryShrubs=145 shadowVertices=33990 evaluationRocks=277 faunaSprites=2 faunaLayoutHash=43931487CF2182AFDFFED2A8A701D17DF7959CF8BBA61CA9E689ECD7461CD054 waterBuildMaxMs=0.00 spriteBuildMaxMs=0.00 terrainTextureBuildMaxMs=0.00
```

## Ortográfica híbrida — LOD volumétrico e billboards

Comando:

```powershell
dotnet src/Game.Client.Desktop/bin/Release/net10.0/Game.Client.Desktop.dll --visual-evaluation --snapshot-file bin/n4-evaluation-20261022/seed-1-day172.json --projection orthographic --reference-landscape-view --camera-zoom 2.8 --hybrid-tree-lod --benchmark-seconds 60
```

Saída:

```text
benchmark=visual-evaluation projection=ORTHOGRAPHIC cameraZoom=2.80 cameraPanM=(0.0,0.0) resolution=1280x720 vsync=True fixedTimeStep=True seed=1 simulationPaused=True ticksPerSecond=6 visualInstanceMultiplier=1 visualSceneBuildMs=151.41 warmupSeconds=5 measuredSeconds=60.0 frames=3601 averageFps=60.00 p95FrameMs=17.25 p99FrameMs=17.84 maxFrameMs=43.19 cpuPercentOfOneCore=30.9 workingSetMiB=184.2 privateMemoryMiB=200.0 maxDrawCountPerFrame=33 maxPrimitivesPerFrame=295632 visibleTrees=621 treeBillboards=616 treeBillboardVertices=11088 floraVertices=262119 groundCoverClusters=2360 understoryShrubs=145 shadowVertices=33990 evaluationRocks=277 faunaSprites=2 faunaLayoutHash=43931487CF2182AFDFFED2A8A701D17DF7959CF8BBA61CA9E689ECD7461CD054 waterBuildMaxMs=0.00 spriteBuildMaxMs=0.00 terrainTextureBuildMaxMs=0.00
```

## Amostra 3D anômala, repetida

A primeira janela de 60 s da perspectiva 3D registrou 2.515 quadros, 41,92 FPS médios, P95 37,05 ms, P99 46,97 ms, máximo 49,17 ms e câmera `zoom=3.40`, `pan=(0,0)`. Uma segunda execução com a mesma configuração deu a saída aceita acima (3.601 quadros, média de 60 FPS). A amostra inicial não foi reproduzida; ela permanece registrada para não ocultar a variação observada.
