---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/NetworkSettings.cs"
tags: [clase, modulo/data]
---
# NetworkSettings

> Tuning of the online mode (sessions, input, interpolation, prediction and lag compensation). Primitive fields only, so it lives in the runtime assembly and is read by the networking layer.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/NetworkSettings.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class NetworkSettings {
    <<ScriptableObject>>
    -int _protocolVersion
    -string _environmentName
    -float _reconnectGraceSeconds
    -float _hostLeaveNoticeSeconds
    -bool _autoRejoin
    -float _autoRejoinRetrySeconds
    -int _inputRedundancy
    -int _inputBufferTarget
    -float _interpolationDelay
    -float _reconcileThreshold
    -float _visualSmoothingRate
    -bool _predictionDefault
    -float _maxRewindSeconds
    ...44 más
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[ApprovalPolicy]], [[CloudSaveStatsService]], [[ConnectionApprovalHandler]], [[DevConnectionPanel]], [[Mode]], [[NetworkCarriable]], [[NetworkCarry]], [[NetworkLobbyState]], [[NetworkNameTag]], [[NetworkPlayerInput]], [[NetworkPlayerMotion]], [[NetworkRootLifetime]], [[OnlineMatchStatsRecorder]], [[OnlineSessionService]], [[PlayerStatsSync]], [[PoolStatsView]], [[ReconnectGraceCoordinator]], [[RelaySessionConnection]], [[SessionExit]], [[Status]]
