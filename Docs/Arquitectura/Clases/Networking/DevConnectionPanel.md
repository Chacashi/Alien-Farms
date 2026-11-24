---
modulo: Networking
namespace: AlienFarms.Networking.Diagnostics
tipo: MonoBehaviour
archivo: "Assets/Scripts/Networking/Diagnostics/DevConnectionPanel.cs"
tags: [clase, modulo/networking]
---
# DevConnectionPanel

> Development connection panel (logic only; the Canvas lives in the scene). Host / Client / Shutdown are wired to buttons' OnClick. Host and Client switch the session to online and start the root's connection strategy (direct IP); the host then loads the gameplay scene for everyone. The status label is rebuilt only when the state changes.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Diagnostics` · **Archivo:** `Assets/Scripts/Networking/Diagnostics/DevConnectionPanel.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class DevConnectionPanel {
    <<MonoBehaviour>>
    -NetworkRuntimeAnchor _anchor
    -MatchSession _session
    -NetworkSettings _settings
    -string _displayName
    -TMP_Text _statusLabel
    -TMP_InputField _addressField
    -OnlineSessionChannel _sessionChannel
    -TMP_InputField _codeField
    -OnlineLobbyChannel _lobbyChannel
    +SignIn() void
    +CreateSession() void
    +JoinSession() void
    +LeaveSession() void
    ...4 más
  }
  class MatchSession:::external
  <<ScriptableObject>> MatchSession
  class NetworkRuntimeAnchor:::external
  <<ScriptableObject>> NetworkRuntimeAnchor
  class NetworkSettings:::external
  <<ScriptableObject>> NetworkSettings
  class OnlineLobbyChannel:::external
  <<ScriptableObject>> OnlineLobbyChannel
  class OnlineSessionChannel:::external
  <<ScriptableObject>> OnlineSessionChannel
  DevConnectionPanel --> MatchSession
  DevConnectionPanel --> NetworkRuntimeAnchor
  DevConnectionPanel --> NetworkSettings
  DevConnectionPanel --> OnlineLobbyChannel
  DevConnectionPanel --> OnlineSessionChannel
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[ConnectionPayload]], [[DirectConnection]], [[DisplayNameResolver]], [[GameMode]], [[IConnectionStrategy]], [[MatchSession]], [[NetworkRootLifetime]], [[NetworkRuntimeAnchor]], [[NetworkSceneLoader]], [[NetworkSettings]], [[OnlineLobbyChannel]], [[OnlineParticipant]], [[OnlineSessionChannel]], [[OnlineSessionStatus]], [[SceneFlow]], [[SceneNames]], [[Status]], [[UgsBootstrapper]]
