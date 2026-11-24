---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: interface
archivo: "Assets/Scripts/Networking/Session/IConnectionStrategy.cs"
tags: [clase, modulo/networking]
---
# IConnectionStrategy

> How a session is started: direct IP (`DirectConnection`) or, later, Sessions + Relay. Asynchronous so a relay allocation fits the same contract. The caller writes `NetworkConfig.ConnectionData` first.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/IConnectionStrategy.cs`

## Derivados / implementaciones
[[DirectConnection]], [[RelaySessionConnection]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class IConnectionStrategy {
    <<interface>>
    +StartHostAsync(NetworkManager manager) Task~bool~
    +StartClientAsync(NetworkManager manager) Task~bool~
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[DevConnectionPanel]], [[DirectConnection]], [[NetworkRootLifetime]], [[NetworkRuntimeAnchor]], [[RelaySessionConnection]], [[Status]]
