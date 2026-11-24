---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: enumeration
archivo: "Assets/Scripts/Networking/Session/HostShutdownStep.cs"
tags: [clase, modulo/networking]
---
# HostShutdownStep

> What `HostShutdownSequence.Step` asks the root to do on this frame.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/HostShutdownStep.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class HostShutdownStep {
    <<enumeration>>
    Wait
    DisconnectClients
    Shutdown
    Complete
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Wait`, `DisconnectClients`, `Shutdown`, `Complete`

## Usado por
[[HostShutdownSequence]], [[NetworkRootLifetime]], [[Phase]]
