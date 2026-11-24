---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/NameTagView.cs"
tags: [clase, modulo/ui]
---
# NameTagView

> World-space name tag over an avatar with a TextMeshPro 3D label, an optional debug line, and an optional backdrop. A driver (e.g. the online NetworkNameTag) feeds it. The text is rebuilt only when the name or the detail changes; while visible it faces the assigned camera in LateUpdate (parallel to the view plane, no allocation). Lives on the tag's own child, never on the avatar root (the billboard rotates this transform).

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/NameTagView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class NameTagView {
    <<MonoBehaviour>>
    -TMP_Text _label
    -Renderer _backdrop
    -Transform _billboard
    +bool IsVisible
    +SetName(string displayName) void
    +SetDetail(string detail) void
    +SetColor(Color color) void
    +SetVisible(bool visible) void
    +SetViewCamera(Camera viewCamera) void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[NetworkNameTag]]
