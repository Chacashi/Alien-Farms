---
modulo: Networking
namespace: AlienFarms.Networking.Player
tipo: class
archivo: "Assets/Scripts/Networking/Player/OwnershipLabelCache.cs"
tags: [clase, modulo/networking]
---
# OwnershipLabelCache

> Every text the ownership debug label can show ("Owner: P2 · IsOwner ✓ · HasAuthority ✗"), built once on first use and then returned by reference: changing the label never allocates.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Player` · **Archivo:** `Assets/Scripts/Networking/Player/OwnershipLabelCache.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OwnershipLabelCache {
    +Get(CarriableOwner owner, bool isOwner, bool hasAuthority) string
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CarriableOwner]], [[CarriableOwnerRules]]

## Usado por
[[CarriableOwnershipLabel]]
