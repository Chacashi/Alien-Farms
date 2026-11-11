---
modulo: Editor
namespace: AlienFarms.Editor
tipo: EditorWindow
archivo: "Assets/Scripts/Editor/GrassFieldBaker.cs"
tags: [clase, modulo/editor]
---
# GrassFieldBaker

> Authoring window: bakes a chunked grass-blade field into a mesh asset under Assets/Art/Models/Generated/ and optionally instantiates it in the scene under "Env_Grass".

**Módulo:** [[Modulo Editor|Editor]] · **Namespace:** `AlienFarms.Editor` · **Archivo:** `Assets/Scripts/Editor/GrassFieldBaker.cs`

## Hereda / implementa
`EditorWindow`

## Diagrama
```mermaid
classDiagram
  direction LR
  class GrassFieldBaker {
    <<EditorWindow>>
    -GrassBakeSettings _settings
    -GameObject _areaReference
    -Material _grassMaterial
    -bool _createInScene
    -ShadowCastingMode _castShadows
    -int _layer
    -SerializedObject _serializedWindow
  }
  class GrassBakeSettings:::external
  GrassFieldBaker --> GrassBakeSettings
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Chunk]], [[GrassBakeSettings]], [[GrassMeshBuilder]]
