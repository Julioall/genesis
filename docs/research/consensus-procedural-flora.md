# Produce a broad but decision-oriented synthesis for procedural-only pixel-art re

> **Estado da recomendação:** síntese criada sob a antiga direção de pixel art procedural. As observações botânicas, modelos de ramificação e técnicas de distribuição continuam como referências algorítmicas; o pipeline de rasterização por pixels e as escolhas visuais específicas não são requisitos vigentes. O cliente permanece em MonoGame; consulte [direção visual e técnicas de apresentação](../22_VISUAL_DIRECTION_AND_TECHNOLOGY.md). Esta pesquisa não valida a simulação nem escolhe a técnica final.

**Question:** Produce a broad but decision-oriented synthesis for procedural-only pixel-art rendering of a natural world, focusing on (1) tree branching architecture and deterministic procedural tree generation, (2) leaf forms and seasonal/phenological stages, (3) grass/grassland patch shapes, (4) plant phenology and environmental responses, and (5) rock composition/silhouette. Emphasize evidence from peer-reviewed botanical/ecological research separately from artistic/computational heuristics. For each evidence-backed claim, give citations, method and limits; discuss practical algorithms that can be used without copying copyrighted implementations or assets. The implementation target is C# MonoGame, but the synthesis should be engine-independent. Include a concise recipe recommendation and explicit uncertainties. Do not claim our model is validated.

# Procedural Generation of **Natural World Landscapes**

Procedural landscape generation requires decomposing biological structures and environmental interactions into mathematical rules and spatial colonization systems [[1.1]](https://consensus.app/papers/infinite-photorealistic-worlds-using-procedural-raistrick-lipson/baa4c6db491d52a6b0daf15b10c2f4f8/) [[1.2]](https://consensus.app/papers/infinite-photorealistic-worlds-using-procedural-raistrick-lipson/baa4c6db491d52a6b0daf15b10c2f4f8/) [[2.1]](https://consensus.app/papers/a-behavioral-analysis-of-machine-learning-for-intrinsic-3d-lakshan-sandaruwan/3e660686ee915552ad648fc19b5943b8/). Developing deterministic pixel-art pipelines involves balancing botanical architecture, seasonal foliage stages, grassland distribution, and lithic contours [[3.1]](https://consensus.app/papers/learning-to-reconstruct-botanical-trees-from-single-li-kaluzny/dffcfe0404ee5e9fb4512c3445e74686/) [[4.1]](https://consensus.app/papers/procedural-generation-and-rendering-of-realistic-newlands-zauner/a31d2338437b5f73983d2d3e1207436d/) [[5.1]](https://consensus.app/papers/outtree-generation-using-controllable-field-space-tanaka-ito/eba804c31413586c9f44344d9adb29cb/).

## Tree Branching Architecture

Botanical branching architecture emerges from internal hydraulic transport and external physical constraints [[6.1]](https://consensus.app/papers/parametrization-of-biological-assumptions-to-simulate-nauber-hodač/6017e2d8c4ee5e8b99d22c17055c8ed1/) [[7.1]](https://consensus.app/papers/retrieval-of-tree-branch-architecture-attributes-from-li-su/2f8adce353455ea3a1207d1566959cef/). Empirical modeling demonstrates that crown outlines across species (e.g., spruce, pine, oak, poplar) are fundamentally governed by five primary parameters: xylem flow, shedding rate, internal proprioception, gravitas/gravity sensing, and light sensitivity [[6.2]](https://consensus.app/papers/parametrization-of-biological-assumptions-to-simulate-nauber-hodač/6017e2d8c4ee5e8b99d22c17055c8ed1/) [[6.1]](https://consensus.app/papers/parametrization-of-biological-assumptions-to-simulate-nauber-hodač/6017e2d8c4ee5e8b99d22c17055c8ed1/) [[6]](https://consensus.app/papers/parametrization-of-biological-assumptions-to-simulate-nauber-hodač/6017e2d8c4ee5e8b99d22c17055c8ed1/). Among these, gravitational response exerts the largest morphological effect by dictating the divergence between angiosperm and gymnosperm branch angles [[6.3]](https://consensus.app/papers/parametrization-of-biological-assumptions-to-simulate-nauber-hodač/6017e2d8c4ee5e8b99d22c17055c8ed1/). In field LiDAR measurements of species such as poplar, first-order trunk identification aligns with vertical apical dominance, while second-order branches exhibit significantly higher structural variability (coefficient of variation of 29.68%) [[8.1]](https://consensus.app/papers/branch-architecture-reconstruction-and-phenotypic-trait-cai-pang/cb329f6a38265e17be6b0368325f9154/) [[8.2]](https://consensus.app/papers/branch-architecture-reconstruction-and-phenotypic-trait-cai-pang/cb329f6a38265e17be6b0368325f9154/).

**Figure 1.** Model overview: (a) ecosystem as the approximate interplay between the biotic and abiotic environment, (b) GC as a repeated internal calculation of the tree structure and EU as its continuous environmental impact, (c) simulation process from structure initialization to the final 2D representation of the tree and (d) tree shape delimitation using stem landmarks (red) and crown landmarks (blue). [[6]](https://consensus.app/papers/parametrization-of-biological-assumptions-to-simulate-nauber-hodač/6017e2d8c4ee5e8b99d22c17055c8ed1/)

*How to read: The diagram depicts the conceptual growth loop: panel a shows environmental interactions, panel b illustrates structural calculation cycles versus environmental pressure, panel c shows the simulation timeline from seedling to 2D tree, and panel d displays red stem landmarks and blue crown delimitation points.*

For deterministic rendering, grammar systems like Lindenmayer models (L-systems) define hierarchical topology via string-rewriting rules [[9.1]](https://consensus.app/papers/interactive-invigoration-volumetric-modeling-of-trees-li-schwarz/e2b4a3326ff555a988e39b7215584cdc/) [[10.1]](https://consensus.app/papers/simulate-forest-trees-by-integrating-lsystem-and-3d-cad-tanveer-thomas/00df16ee63c05309812e2f419b08a6d9/) [[11.1]](https://consensus.app/papers/latent-lsystems-transformerbased-tree-generator-lee-li/3b6dc52253c75502b3e248a4b1e8fb50/). However, manual rule design remains notoriously difficult for complex morphologies [[9.2]](https://consensus.app/papers/interactive-invigoration-volumetric-modeling-of-trees-li-schwarz/e2b4a3326ff555a988e39b7215584cdc/) [[5.2]](https://consensus.app/papers/outtree-generation-using-controllable-field-space-tanaka-ito/eba804c31413586c9f44344d9adb29cb/) [[11.2]](https://consensus.app/papers/latent-lsystems-transformerbased-tree-generator-lee-li/3b6dc52253c75502b3e248a4b1e8fb50/). Pure L-systems also struggle to replicate realistic volumetric limb expansion without secondary growth modeling [[9.3]](https://consensus.app/papers/interactive-invigoration-volumetric-modeling-of-trees-li-schwarz/e2b4a3326ff555a988e39b7215584cdc/) [[10.2]](https://consensus.app/papers/simulate-forest-trees-by-integrating-lsystem-and-3d-cad-tanveer-thomas/00df16ee63c05309812e2f419b08a6d9/).

To overcome this, computational graphics relies on space colonization algorithms, which iteratively pull growing nodes toward spatial attractor points to naturally simulate space competition and light foraging [[2.2]](https://consensus.app/papers/a-behavioral-analysis-of-machine-learning-for-intrinsic-3d-lakshan-sandaruwan/3e660686ee915552ad648fc19b5943b8/) [[2.3]](https://consensus.app/papers/a-behavioral-analysis-of-machine-learning-for-intrinsic-3d-lakshan-sandaruwan/3e660686ee915552ad648fc19b5943b8/) [[5.3]](https://consensus.app/papers/outtree-generation-using-controllable-field-space-tanaka-ito/eba804c31413586c9f44344d9adb29cb/). For rasterization, discrete nodes can be linked using minimum spanning trees or strand volumes before rendering to 2D sprites [[9.4]](https://consensus.app/papers/interactive-invigoration-volumetric-modeling-of-trees-li-schwarz/e2b4a3326ff555a988e39b7215584cdc/) [[12.1]](https://consensus.app/papers/accelerated-forest-modeling-from-tree-canopy-point-clouds-xu-zhang/f75485bb221e559a836a277b2a815995/).

| Branching Dimension | Peer-Reviewed Botanical Finding | Computational Heuristic / Algorithm | Known Modeling Limits |
| :--- | :--- | :--- | :--- |
| **Branch Topology** | Apical dominance guides primary stem growth [[8.1]](https://consensus.app/papers/branch-architecture-reconstruction-and-phenotypic-trait-cai-pang/cb329f6a38265e17be6b0368325f9154/). | Space colonization with vector fields [[2.3]](https://consensus.app/papers/a-behavioral-analysis-of-machine-learning-for-intrinsic-3d-lakshan-sandaruwan/3e660686ee915552ad648fc19b5943b8/) [[5.4]](https://consensus.app/papers/outtree-generation-using-controllable-field-space-tanaka-ito/eba804c31413586c9f44344d9adb29cb/). | Prone to point density sensitivity [[5.5]](https://consensus.app/papers/outtree-generation-using-controllable-field-space-tanaka-ito/eba804c31413586c9f44344d9adb29cb/). |
| **Limb Thickness** | Secondary vascular growth expands girth [[9.3]](https://consensus.app/papers/interactive-invigoration-volumetric-modeling-of-trees-li-schwarz/e2b4a3326ff555a988e39b7215584cdc/). | Volumetric strands or pipe accumulation [[9.4]](https://consensus.app/papers/interactive-invigoration-volumetric-modeling-of-trees-li-schwarz/e2b4a3326ff555a988e39b7215584cdc/). | Cylindrical models flatten branch junctions [[9.5]](https://consensus.app/papers/interactive-invigoration-volumetric-modeling-of-trees-li-schwarz/e2b4a3326ff555a988e39b7215584cdc/). |
| **Growth Vectors** | Gravitropism differentiates conifers and broadleaves [[6.3]](https://consensus.app/papers/parametrization-of-biological-assumptions-to-simulate-nauber-hodač/6017e2d8c4ee5e8b99d22c17055c8ed1/). | Deterministic directional biasing per branch tier [[6.4]](https://consensus.app/papers/parametrization-of-biological-assumptions-to-simulate-nauber-hodač/6017e2d8c4ee5e8b99d22c17055c8ed1/). | Strong wind overrides intrinsic rules [[8.3]](https://consensus.app/papers/branch-architecture-reconstruction-and-phenotypic-trait-cai-pang/cb329f6a38265e17be6b0368325f9154/). |
| **Sub-Branch Noise** | Higher-order branches display wide variance [[8.2]](https://consensus.app/papers/branch-architecture-reconstruction-and-phenotypic-trait-cai-pang/cb329f6a38265e17be6b0368325f9154/). | Seeded PRNG perturbations on leaf nodes [[10.3]](https://consensus.app/papers/simulate-forest-trees-by-integrating-lsystem-and-3d-cad-tanveer-thomas/00df16ee63c05309812e2f419b08a6d9/). | Real scans lose small branches [[8.4]](https://consensus.app/papers/branch-architecture-reconstruction-and-phenotypic-trait-cai-pang/cb329f6a38265e17be6b0368325f9154/). |

**Figure 1:** Comparison of empirical botanical branching traits against algorithmic generative heuristics.

## Foliage, Phenology, and Grasslands

Vegetation distribution and foliage morphology are shaped by environmental competition and seasonal resource allocation [[2.4]](https://consensus.app/papers/a-behavioral-analysis-of-machine-learning-for-intrinsic-3d-lakshan-sandaruwan/3e660686ee915552ad648fc19b5943b8/) [[11.3]](https://consensus.app/papers/latent-lsystems-transformerbased-tree-generator-lee-li/3b6dc52253c75502b3e248a4b1e8fb50/). Leaf-wood separation data highlights that leaf canopy clusters obscure underlying branching topology during peak seasons, requiring modular decoupling of foliage from branch graphs [[13.1]](https://consensus.app/papers/l1tree-a-novel-algorithm-for-constructing-3d-tree-models-feng-su/8ed6657a949e584594b55b8b3f787087/) [[8.5]](https://consensus.app/papers/branch-architecture-reconstruction-and-phenotypic-trait-cai-pang/cb329f6a38265e17be6b0368325f9154/).

- **Foliage Clustering:** Tree crowns naturally form radial bounding volumes where terminal twigs aggregate leaf clusters, which are simulated by spawning billboard particles or dense alpha-clipped pixel clusters at terminal graph nodes [[3.2]](https://consensus.app/papers/learning-to-reconstruct-botanical-trees-from-single-li-kaluzny/dffcfe0404ee5e9fb4512c3445e74686/) [[4.2]](https://consensus.app/papers/procedural-generation-and-rendering-of-realistic-newlands-zauner/a31d2338437b5f73983d2d3e1207436d/) [[12.2]](https://consensus.app/papers/accelerated-forest-modeling-from-tree-canopy-point-clouds-xu-zhang/f75485bb221e559a836a277b2a815995/).
- **Grassland Distributions:** Open ground cover emerges through competitive spatial scattering; ecosystem simulations and inhomogeneous Poisson processes naturally reproduce clustering patterns where plant groups form cohesive clumps rather than uniform scatters [[1.3]](https://consensus.app/papers/infinite-photorealistic-worlds-using-procedural-raistrick-lipson/baa4c6db491d52a6b0daf15b10c2f4f8/) [[4.1]](https://consensus.app/papers/procedural-generation-and-rendering-of-realistic-newlands-zauner/a31d2338437b5f73983d2d3e1207436d/) [[10.4]](https://consensus.app/papers/simulate-forest-trees-by-integrating-lsystem-and-3d-cad-tanveer-thomas/00df16ee63c05309812e2f419b08a6d9/) [[4]](https://consensus.app/papers/procedural-generation-and-rendering-of-realistic-newlands-zauner/a31d2338437b5f73983d2d3e1207436d/).
- **Phenological Transitions:** Seasonal shifts require altering foliage density and color albedo [[1.4]](https://consensus.app/papers/infinite-photorealistic-worlds-using-procedural-raistrick-lipson/baa4c6db491d52a6b0daf15b10c2f4f8/) [[4.3]](https://consensus.app/papers/procedural-generation-and-rendering-of-realistic-newlands-zauner/a31d2338437b5f73983d2d3e1207436d/). Simulating shedding rates alters canopy volume dynamically between summer leaf-on and winter leaf-off structures [[6.1]](https://consensus.app/papers/parametrization-of-biological-assumptions-to-simulate-nauber-hodač/6017e2d8c4ee5e8b99d22c17055c8ed1/) [[8.6]](https://consensus.app/papers/branch-architecture-reconstruction-and-phenotypic-trait-cai-pang/cb329f6a38265e17be6b0368325f9154/).

**Figure 9.** The initial (random) state of the ecosystem simulation, and the distributed plants after n = 1000 iterations. Clustering of plant species can be seen to emerge, and these clusters remain over time, just changing position. [[4]](https://consensus.app/papers/procedural-generation-and-rendering-of-realistic-newlands-zauner/a31d2338437b5f73983d2d3e1207436d/)

*How to read: The side-by-side plots show an ecosystem spatial simulation at step 0 (uniform random scatter) versus step 1000, where spatial competition produces distinct, persistent clustering patches.*

## Lithic Composition and Silhouette

Geological terrain and rock formations follow branchless dendritic fracture and erosion patterns that can be approximated with fractal mathematics [[5.6]](https://consensus.app/papers/outtree-generation-using-controllable-field-space-tanaka-ito/eba804c31413586c9f44344d9adb29cb/) [[5.7]](https://consensus.app/papers/outtree-generation-using-controllable-field-space-tanaka-ito/eba804c31413586c9f44344d9adb29cb/). Ridge profiles, slope lines, and rock faces mirror inverse space-filling paths governed by surface wear [[5.1]](https://consensus.app/papers/outtree-generation-using-controllable-field-space-tanaka-ito/eba804c31413586c9f44344d9adb29cb/) [[5.8]](https://consensus.app/papers/outtree-generation-using-controllable-field-space-tanaka-ito/eba804c31413586c9f44344d9adb29cb/).

Procedural lithic modeling is achieved by combining sparse convolution noise or multi-octave gradient noise with directional sweeping vectors [[5.8]](https://consensus.app/papers/outtree-generation-using-controllable-field-space-tanaka-ito/eba804c31413586c9f44344d9adb29cb/) [[5.9]](https://consensus.app/papers/outtree-generation-using-controllable-field-space-tanaka-ito/eba804c31413586c9f44344d9adb29cb/). In a 2D pixel engine, rock silhouettes require discrete quantized height bands, sharp normal shifts along planar facets, and edge-erosion passes [[1.5]](https://consensus.app/papers/infinite-photorealistic-worlds-using-procedural-raistrick-lipson/baa4c6db491d52a6b0daf15b10c2f4f8/) [[5.7]](https://consensus.app/papers/outtree-generation-using-controllable-field-space-tanaka-ito/eba804c31413586c9f44344d9adb29cb/) [[8.7]](https://consensus.app/papers/branch-architecture-reconstruction-and-phenotypic-trait-cai-pang/cb329f6a38265e17be6b0368325f9154/).

## Procedural Recipe and Technical Uncertainties

Executing a procedural-only natural scene in C# without asset dependencies involves five sequential passes:

1. **Terrain and Substrate Pass:** Compute elevation and rock silhouettes using layered simplex noise perturbed by sparse convolutional displacement [[5.7]](https://consensus.app/papers/outtree-generation-using-controllable-field-space-tanaka-ito/eba804c31413586c9f44344d9adb29cb/). Quantize lighting values into discrete pixel palettes based on calculated surface normals [[1.5]](https://consensus.app/papers/infinite-photorealistic-worlds-using-procedural-raistrick-lipson/baa4c6db491d52a6b0daf15b10c2f4f8/) [[1.6]](https://consensus.app/papers/infinite-photorealistic-worlds-using-procedural-raistrick-lipson/baa4c6db491d52a6b0daf15b10c2f4f8/).
2. **Grassland Patch Distribution:** Populate ground tiles via an inhomogeneous Poisson process, clumping grass sprites based on simulated soil and moisture maps [[10.5]](https://consensus.app/papers/simulate-forest-trees-by-integrating-lsystem-and-3d-cad-tanveer-thomas/00df16ee63c05309812e2f419b08a6d9/) [[10.4]](https://consensus.app/papers/simulate-forest-trees-by-integrating-lsystem-and-3d-cad-tanveer-thomas/00df16ee63c05309812e2f419b08a6d9/).
3. **Deterministic Tree Graph Generation:** Plant trees at seeded locations, running an internal 2D space colonization routine inside radial bounding limits [[3.2]](https://consensus.app/papers/learning-to-reconstruct-botanical-trees-from-single-li-kaluzny/dffcfe0404ee5e9fb4512c3445e74686/) [[5.3]](https://consensus.app/papers/outtree-generation-using-controllable-field-space-tanaka-ito/eba804c31413586c9f44344d9adb29cb/). Modulate upward phototropism and lateral branching using hardcoded species parameters (conifer vs. broadleaf) [[6.4]](https://consensus.app/papers/parametrization-of-biological-assumptions-to-simulate-nauber-hodač/6017e2d8c4ee5e8b99d22c17055c8ed1/) [[2.1]](https://consensus.app/papers/a-behavioral-analysis-of-machine-learning-for-intrinsic-3d-lakshan-sandaruwan/3e660686ee915552ad648fc19b5943b8/).
4. **Foliage and Phenology Pass:** Evaluate an environmental season variable. For winter, render bare branch skeletons; for spring and summer, scatter foliage clusters across branch tips; for autumn, shift color palettes and prune crown volume using a shedding rate parameter [[6.1]](https://consensus.app/papers/parametrization-of-biological-assumptions-to-simulate-nauber-hodač/6017e2d8c4ee5e8b99d22c17055c8ed1/) [[8.6]](https://consensus.app/papers/branch-architecture-reconstruction-and-phenotypic-trait-cai-pang/cb329f6a38265e17be6b0368325f9154/) [[12.2]](https://consensus.app/papers/accelerated-forest-modeling-from-tree-canopy-point-clouds-xu-zhang/f75485bb221e559a836a277b2a815995/).
5. **Pixel Quantization:** Rasterize all vector branch graphs and foliage discs directly onto an off-screen render target at native low resolution before blitting to the display [[1.7]](https://consensus.app/papers/infinite-photorealistic-worlds-using-procedural-raistrick-lipson/baa4c6db491d52a6b0daf15b10c2f4f8/) [[14.1]](https://consensus.app/papers/computer-simulations-of-biological-growth-patterns-tree-nagel-robert/566a12e51b425d61bf05ceacdfc90f46/).

Several explicit uncertainties remain in this workflow. Real-world tree point clouds reveal extensive morphological noise and branch occlusion that simplified algorithms do not fully capture [[13.2]](https://consensus.app/papers/l1tree-a-novel-algorithm-for-constructing-3d-tree-models-feng-su/8ed6657a949e584594b55b8b3f787087/) [[2.5]](https://consensus.app/papers/a-behavioral-analysis-of-machine-learning-for-intrinsic-3d-lakshan-sandaruwan/3e660686ee915552ad648fc19b5943b8/). Furthermore, mathematical heuristics cannot guarantee biological validity across all microclimates, as severe wind disturbance and asymmetrical lighting systematically invalidate idealized growth assumptions [[8.1]](https://consensus.app/papers/branch-architecture-reconstruction-and-phenotypic-trait-cai-pang/cb329f6a38265e17be6b0368325f9154/) [[8.3]](https://consensus.app/papers/branch-architecture-reconstruction-and-phenotypic-trait-cai-pang/cb329f6a38265e17be6b0368325f9154/). Consequently, the resulting procedural models represent visually plausible approximations rather than biologically validated simulations [[6.5]](https://consensus.app/papers/parametrization-of-biological-assumptions-to-simulate-nauber-hodač/6017e2d8c4ee5e8b99d22c17055c8ed1/) [[2.6]](https://consensus.app/papers/a-behavioral-analysis-of-machine-learning-for-intrinsic-3d-lakshan-sandaruwan/3e660686ee915552ad648fc19b5943b8/) [[2.7]](https://consensus.app/papers/a-behavioral-analysis-of-machine-learning-for-intrinsic-3d-lakshan-sandaruwan/3e660686ee915552ad648fc19b5943b8/).

_These search results were found and analyzed using Consensus, an AI-powered search engine for research. Try it at https://consensus.app. © 2026 Consensus NLP, Inc. Personal, non-commercial use only; redistribution requires copyright holders’ consent._

## References

`1.` Raistrick ARE, Lipson L,  Z, et al. Infinite Photorealistic Worlds Using Procedural Generation. *2023 IEEE/CVF Conference on Computer Vision and Pattern Recognition (CVPR).* 2023. doi:10.1109/cvpr52729.2023.01215

> [1.1] We introduce Infinigen, a procedural generator of photorealistic 3D scenes of the natural world. (Abstract)

> [1.2] Infinigen is entirely procedural: every asset, from shape to texture, is generated from scratch via randomized mathematical rules, using no external source and allowing infinite variation and composition. (Abstract)

> [1.3] 11 Figure 11.Random, non cherry-picked surface scatters.Dense coverage with procedural assets turns any surface into a convincing grassland, sea floor or forest floor environment. (Body)

> [1.4] (i), and material parameters such as Albedo (Body)

> [1.5] (c), Surface Normals (Body)

> [1.6] (j), Lighting Intensity (Body)

> [1.7] 4. Real-time optimized assets (left) often use low res.geometry in conjunction with shading tricks and alpha-masked image textures to give the illusion of geometric detail.Infinigen assets (right) instead model objects in full geometric detail.Bottom left triangles show a random color per mesh face. (Body)

`2.` Lakshan M, Sandaruwan K, De Silva LCS. A Behavioral Analysis of Machine Learning for Intrinsic 3D Tree Branching Structures. *International Journal of Advanced Computer Science and Applications.* 2026. doi:10.14569/ijacsa.2026.0170783

> [2.1] One of the most influential approaches is the L-system framework [2], which represents plant growth using symbolic rewriting rules and has been extended to incorporate phototropism and space competition. (Introduction)

> [2.2] Beneš [3] introduced phototropic plant development models based on light availability, while the space colonization algorithm of Runions et al. (Introduction)

> [2.3] [4] modeled branching as an iterative attraction process guided by available space. (Introduction)

> [2.4] structure and environmental adaptation, including responses to light, obstacles, and neighboring vegetation. (Introduction)

> [2.5] However, learning from real-world scanned trees remains challenging due to noise, missing branches, and reconstruction artifacts, which reduce structural consistency and learning accuracy. (Introduction)

> [2.6] In these applications, vegetation modeling prioritizes visual and structural plausibility over strict biological realism. (Introduction)

> [2.7] Unlike botanical studies, which require accurate growth mechanisms, many virtual environments only demand perceptually convincing structures. (Introduction)

`3.` Li B, Kaluzny J, Klein J, et al. Learning to reconstruct botanical trees from single images. *ACM Transactions on Graphics (TOG).* 2021;40:1 - 15. doi:10.1145/3478513.3480525

> [3.1] The networks simultaneously mask out trees in input photographs, identify a tree's species, and obtain its 3D radial bounding volume - our novel 3D representation for botanical trees. (Abstract)

> [3.2] Radial bounding volumes (RBV) are used to orchestrate a procedural model primed on learned parameters to grow a tree that matches the main branching structure and the overall shape of the captured tree. (Abstract)

`4.` Newlands C, Zauner K. Procedural Generation and Rendering of Realistic, Navigable Forest Environments: An Open-Source Tool. *ArXiv.* 2022;abs/2208.01471. doi:10.48550/arxiv.2208.01471

> [4.1] The system uses specialised L-systems to generate trees which are distributed using an ecosystem simulation algorithm. (Abstract)

> [4.2] Billboards for tree simplification and real-time forest rendering. (Body)

> [4.3] The resulting scene is rendered using a deferred rendering pipeline, a Blinn-Phong lighting model with real-time leaf transparency and post-processing lighting effects. (Abstract)

`5.` Tanaka T, Ito T, Kikuchi T. Out-Tree Generation Using Controllable Field Space Colonization and Application to Modeling. *American Journal of Computer Science and Technology.* 2021. doi:10.11648/j.ajcst.20210402.11

> [5.1] Since space colonization Algorithm and the Controllable Field space colonization Algorithm proposed in this paper are algorithms for generating Out-Tree, if it is possible to map the numerical data of Out-Tree in the same way as sketch-based reproduction of undulations, it will be effective for generating the shapes of imaginary mountains and rivers. (Body)

> [5.2] Another known problem that can occur in L-Systems is the difficulty of finding control points and substitution rules by tracing back the structure. (Introduction)

> [5.3] An extension of the algorithm to reproduce the same algorithm in 3D space was named space colonization [4]. (Body)

> [5.4] By using an induced vector field rotated by quaternions around the normal vector of the starting point of the Out-Tree generation, the algorithm can change the coordinate values of newly generated points, thereby allowing the shape of the branching structure to be manipulated by numerical parameters. (Abstract)

> [5.5] Because the branching structure takes into account the competition of branches in space, the shape of the branch is controlled only by the density of points placed in space to form the branch and the search range of the branch. (Body)

> [5.6] In particular, when we observe mountain ridge lines, valley lines, and the shapes of rivers along mountains, they are often tree structures without closed roads. (Body)

> [5.7] For fine erosion of the terrain, sparse convolutional noise is mainly used [21]. (Body)

> [5.8] In this section, a polygon mesh is created for the Out-Tree by a sweeping process, and the position where undulations are given is specified by giving it RGB values and performing color mapping. (Body)

> [5.9] Sparse convolution noise generates noise as a sum of randomly placed weighted kernels. (Body)

`6.` Nauber T, Hodač L, Wäldchen J, Mäder P. Parametrization of biological assumptions to simulate growth of tree branching architectures. *Tree Physiology.* 2024;44. doi:10.1093/treephys/tpae045

> [6.1] Five biological parameters, namely xylem flow, shedding rate, proprioception, gravitysense and lightsense, most influenced the generated tree branching patterns. (Abstract)

> [6.2] By modifying the parameter values, we aim to generate basic shapes of spruce, pine, oak and poplar. (Abstract)

> [6.3] The largest effect was attributed to gravity, as phenotypic responses to this effect resulted in different growth directions of gymnosperm and angiosperm branching architectures. (Abstract)

> [6.4] Adjusting these five parameters resulted in the different tree shapes of spruce, pine, oak, and poplar. (Abstract)

> [6.5] Plausible visual results (tree shapes) were obtained and could be analyzed with geometric morphometrics. (Body)

`7.` Li Y, Su Y, Zhao X, et al. Retrieval of tree branch architecture attributes from terrestrial laser scan data using a Laplacian algorithm. *Agricultural and Forest Meteorology.* 2020;284:107874. doi:10.1016/j.agrformet.2019.107874

> [7.1] Abstract Tree architecture, defined as the three-dimensional arrangement of tree above-ground elements, directly influences the biological and physical processes of vegetation such as photosynthesis and evapotranspiration. (Abstract)

`8.` Cai S, Pang Y. Branch architecture reconstruction and phenotypic trait analysis of poplar trees using low-cost UAV LiDAR point clouds. *Plant Phenomics.* 2026;8. doi:10.1016/j.plaphe.2026.100180

> [8.1] First, the identification of the first-order branch endpoint relies on the assumption that it corresponds to the highest skeleton endpoint, motivated by apical dominance and competitive light acquisition in vertically growing trees. (Body)

> [8.2] The study also reveals structural variability in branch traits, with the highest variability observed in the second-order branch length (coefficient of variation = 29.68%), and a moderate positive correlation between first- and second-order branch lengths (correlation coefficient = 0.34), providing insights into tree growth patterns. (Abstract)

> [8.3] While generally valid for upright trees with regular crown architectures, this assumption may be violated in leaning trees or individuals with strongly asymmetric or wind-disturbed crowns, where the tallest endpoint can correspond to a higher-order branch. (Body)

> [8.4] Small or occluded branches may be partially omitted or simplified, although the overall branch morphology and topological structure are generally well preserved. (Body)

> [8.5] Its applicability under leaf-on conditions may be limited by foliage occlusion, particularly when using low-cost UAV LiDAR systems with restricted pulse energy. (Body)

> [8.6] Second, the proposed algorithm is designed to reconstruct first- and second-order branches from UAV LiDAR data acquired under leaf-off conditions. (Body)

> [8.7] This algorithm quantifies canopy edge intensity using mathematical morphology and optimizes the cropping boundary using a cumulative intensity approach. (Body)

`9.` Li B, Schwarz N, Palubicki W, Pirk S, Benes B. Interactive Invigoration: Volumetric Modeling of Trees with Strands. *ACM Transactions on Graphics (TOG).* 2024;43:1 - 13. doi:10.1145/3658206

> [9.1] Among these methods, L-systems have emerged as a prominent formal framework for modeling plant structures [Prusinkiewicz 1986]. (Body)

> [9.2] However, although L-systems offer a robust method for generating plant structures, the intricate process of defining production rules to create complex trees is a laborintensive task that requires a significant amount of expertise or applications of machine learning to detect the rules [Guo et al. 2020;Lee et al. 2024]. (Body)

> [9.3] Compared to existing approaches, we focus on both primary and secondary growth of trees, introducing the concepts of interactive invigoration and strand-based modeling, along with a robust meshing algorithm and a set of tree editing operators. (Discussion)

> [9.4] Strands are fixed-size volumetric pipes that define the branching structure. (Abstract)

> [9.5] Most existing methods focus on defining the branching structure of trees based on skeletal graphs, while the surface mesh of branches is most commonly defined as simple cylinders. (Abstract)

`10.` Tanveer M, Thomas A, Wu X, Zhu H. Simulate Forest Trees by Integrating L-System and 3D CAD Files. *2020 3rd International Conference on Information and Computer Technologies (ICICT).* 2020. doi:10.1109/icict50521.2020.00022

> [10.1] It defines the branching pattern through recursively applying certain production rules on a string of symbols. (Introduction)

> [10.2] While Lsystems are commonly used to produce branching structures, they are not sufficient for generating natural looking trees in 3D because of over-simplified assumptions on the geometry of branches, sub-branches and leaves. (Introduction)

> [10.3] Randomization has been added to the placement of all branches, sub branches and leaves. (Body)

> [10.4] To simulate a forest, we adopt Inhomogeneous Poisson process (IPP) to generate random locations of trees. (Introduction)

> [10.5] To simulate a forest, we adopt Inhomogeneous Poisson process to generate random locations of trees. (Introduction)

`11.` Lee JJ, Li B, Benes B. Latent L-systems: Transformer-based Tree Generator. *ACM Transactions on Graphics.* 2023;43:1 - 16. doi:10.1145/3627101

> [11.1] L-systems are string-rewriting procedural systems that encode tree topology and geometry. (Abstract)

> [11.2] L-systems are efficient, but creating the production rules is one of the most critical problems precluding their usage in practice. (Abstract)

> [11.3] [1988] and the interplay of the environment and the plant competition for resources [Palubicki et al. 2009]. (Body)

`12.` Xu J, Zhang Z, Hu X, Ke T. Accelerated forest modeling from tree canopy point clouds via deep learning. *Int. J. Appl. Earth Obs. Geoinformation.* 2024;132:104074. doi:10.1016/j.jag.2024.104074

> [12.1] To form the tree structure, the discrete nodes are connected by using the minimum spanning tree algorithm combined with the adjacency relationship. (Abstract)

> [12.2] The attributes of the node include width, whether it is a leaf node, and leaf node size, which are used for subsequent construction of the tree’s mesh. (Abstract)

`13.` Feng Y, Su Y, Wang J, et al. L1-Tree: A novel algorithm for constructing 3D tree models and estimating branch architectural traits using terrestrial laser scanning data. *Remote Sensing of Environment.* 2024. doi:10.1016/j.rse.2024.114390

> [13.1] Leaf-wood separation emerged as a crucial step influencing the performance of the L 1 -Tree algorithm. (Abstract)

> [13.2] However, the complex canopy structure of trees in natural forests and the presence of occlusion in TLS data pose significant challenges to achieving this goal. (Abstract)

`14.`  N, Robert L. Computer Simulations of Biological Growth Patterns: Tree Modeling Success and Applications. 2021. doi:10.26076/eb98-6176

> [14.1] In this context, the body of work reported here emphasizes the development of a programming methodology for achieving better tree images, rather than the sophistication of the images themselves. (Abstract)
