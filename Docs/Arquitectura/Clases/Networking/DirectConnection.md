---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: MonoBehaviour
archivo: "Assets/Scripts/Networking/Session/DirectConnection.cs"
tags: [clase, modulo/networking]
---
# DirectConnection

> Direct IP connection (Multiplayer Play Mode, LAN or the relay fallback): sets the UnityTransport address and port, then starts NGO synchronously.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/DirectConnection.cs`

## Hereda / implementa
`MonoBehaviour`, [[IConnectionStrategy]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class DirectConnection {
    <<MonoBehaviour>>
    -UnityTransport _transport
    -string _address
    -int _port
    -string _listenAddress
    +string Address
    +int Port
    +SetAddress(string address) void
    +StartHostAsync(NetworkManager manager) Task~bool~
    +StartClientAsync(NetworkManager manager) Task~bool~
  }
  class IConnectionStrategy:::external
  <<interface>> IConnectionStrategy
  DirectConnection ..|> IConnectionStrategy
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IConnectionStrategy]]

## Usado por
[[DevConnectionPanel]], [[Status]]
