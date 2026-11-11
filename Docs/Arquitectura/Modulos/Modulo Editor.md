---
tags: [modulo, modulo/editor]
---
# Módulo Editor

Carpeta `Assets/Scripts/Editor/` · 9 tipos. Las cajas punteadas son tipos de otros módulos.

Leyenda: `--|>` hereda · `..|>` implementa · `-->` tiene un campo/propiedad de ese tipo. Se muestran campos `[SerializeField]` y la API pública.

## Diagrama de clases
```mermaid
classDiagram
  direction LR
  class Buffers {
    +List~Vector3~ Positions
    +List~Vector3~ Normals
    +List~Vector2~ Uv0
    +List~Vector3~ Uv1
    +List~Color32~ Colors
    +List~int~ Indices
    +Clear() void
  }
  class Chunk {
    <<struct>>
    +string Name
    +Mesh Mesh
    +int BladeCount
  }
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
  class GrassMeshBuilder {
    <<static>>
    +GetPlacementRect(Bounds area, float edgeMargin)$ Rect
    +EstimateVertexCount(GrassBakeSettings settings, Bounds area)$ long
    +Build(GrassBakeSettings settings, Bounds area, Vector3 origin)$ List~Chunk~
  }
  class LocalizationSeedData {
    <<static>>
  }
  class LocalizationSeedEntry {
    <<struct>>
    +string Table
    +string Key
    +string Spanish
    +string English
  }
  class LocalizationSetupMenu {
    <<static>>
    +Setup()$ void
    +BindSelected()$ void
    +SetLocalizedString(LocalizedText target, string table, string key)$ void
  }
  class ViewDrivenTextCollector {
    <<static>>
    +Collect(GameObject root, HashSet~TMP_Text~ result)$ void
  }
  GrassFieldBaker --> GrassBakeSettings
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Tipos

| Tipo | Clase | Descripción |
|---|---|---|
| [[Buffers]] | class |  |
| [[Chunk]] | struct |  |
| [[GrassBakeSettings]] | class | Authoring parameters for `GrassMeshBuilder`. Serialized by `GrassFieldBaker`. |
| [[GrassFieldBaker]] | EditorWindow | Authoring window: bakes a chunked grass-blade field into a mesh asset under Assets/Art/Models/Generated/ and optionally instantiates it in t… |
| [[GrassMeshBuilder]] | static | Deterministically generates chunked grass-blade meshes for the AlienFarms/GrassBlades shader. Vertex layout: UV0 = (across, height 0..1), UV… |
| [[LocalizationSeedData]] | static | Initial ES/EN values for every localization key. Spanish values match the texts authored in scenes/prefabs (so "Bind Selected" can match the… |
| [[LocalizationSeedEntry]] | struct | One string table entry seeded by `LocalizationSetupMenu`. |
| [[LocalizationSetupMenu]] | static | Idempotent localization setup: creates whatever is missing (settings, es/en locales, UI/HowToPlay tables and their entries) and enforces the… |
| [[ViewDrivenTextCollector]] | static | Finds TMP texts referenced by AlienFarms components (views write them from code, so they must never get a static `LocalizedText`). Localized… |

## Depende de
[[Modulo UI|UI]] (4)