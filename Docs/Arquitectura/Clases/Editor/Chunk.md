---
modulo: Editor
namespace: AlienFarms.Editor
tipo: struct
archivo: "Assets/Scripts/Editor/GrassMeshBuilder.cs"
tags: [clase, modulo/editor]
---
# Chunk

**Módulo:** [[Modulo Editor|Editor]] · **Namespace:** `AlienFarms.Editor` · **Archivo:** `Assets/Scripts/Editor/GrassMeshBuilder.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class Chunk {
    <<struct>>
    +string Name
    +Mesh Mesh
    +int BladeCount
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[GrassBakeSettings]]

## Usado por
[[GrassFieldBaker]]
