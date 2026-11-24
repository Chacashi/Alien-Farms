---
modulo: Networking
namespace: AlienFarms.Networking.Diagnostics
tipo: static
archivo: "Assets/Scripts/Networking/Diagnostics/NetworkStatsFormatter.cs"
tags: [clase, modulo/networking]
---
# NetworkStatsFormatter

> Writes a `NetworkStatsSample` into a reusable char buffer (no allocation; shown with TMP_Text.SetCharArray). Pure, so the layout is unit tested. Output truncates if the buffer is too small. Pool activos/en pool/creados Animales 12/78/90 Bombas 1/2/3 Rx 150 pkt/s cola 2.4 max 41/1024 Frame max 612 ms

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Diagnostics` · **Archivo:** `Assets/Scripts/Networking/Diagnostics/NetworkStatsFormatter.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class NetworkStatsFormatter {
    <<static>>
    +Write(char buffer, NetworkStatsSample sample)$ int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[NetworkPoolStats]], [[NetworkStatsSample]]

## Usado por
[[PoolStatsView]]
