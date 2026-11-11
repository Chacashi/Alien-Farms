---
modulo: Editor
namespace: AlienFarms.Editor
tipo: class
archivo: "Assets/Scripts/Editor/GrassBakeSettings.cs"
tags: [clase, modulo/editor]
---
# GrassBakeSettings

> Authoring parameters for `GrassMeshBuilder`. Serialized by `GrassFieldBaker`.

**Módulo:** [[Modulo Editor|Editor]] · **Namespace:** `AlienFarms.Editor` · **Archivo:** `Assets/Scripts/Editor/GrassBakeSettings.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class GrassBakeSettings {
    -string _fieldName
    -float _chunkSize
    -Bounds _manualArea
    -float _edgeMargin
    -float _clumpsPerSquareMeter
    -int _minBladesPerClump
    -int _maxBladesPerClump
    -float _clumpRadius
    -float _patchiness
    -float _patchScale
    -float _bladeHeight
    -float _heightVariation
    -float _bladeWidth
    ...35 más
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[Buffers]], [[Chunk]], [[GrassFieldBaker]], [[GrassMeshBuilder]]
