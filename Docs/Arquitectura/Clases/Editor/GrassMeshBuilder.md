---
modulo: Editor
namespace: AlienFarms.Editor
tipo: static
archivo: "Assets/Scripts/Editor/GrassMeshBuilder.cs"
tags: [clase, modulo/editor]
---
# GrassMeshBuilder

> Deterministically generates chunked grass-blade meshes for the AlienFarms/GrassBlades shader. Vertex layout: UV0 = (across, height 0..1), UV1 = blade root (mesh space), COLOR = (height, clump random, blade random, 1), NORMAL = blade facing biased up.

**Módulo:** [[Modulo Editor|Editor]] · **Namespace:** `AlienFarms.Editor` · **Archivo:** `Assets/Scripts/Editor/GrassMeshBuilder.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class GrassMeshBuilder {
    <<static>>
    +GetPlacementRect(Bounds area, float edgeMargin)$ Rect
    +EstimateVertexCount(GrassBakeSettings settings, Bounds area)$ long
    +Build(GrassBakeSettings settings, Bounds area, Vector3 origin)$ List~Chunk~
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[GrassBakeSettings]]

## Usado por
[[GrassFieldBaker]]
