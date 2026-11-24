---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: MonoBehaviour
archivo: "Assets/Scripts/Networking/Session/RelaySessionConnection.cs"
tags: [clase, modulo/networking]
---
# RelaySessionConnection

> Connection through Unity Sessions + Relay: the host creates a private session with a Relay network (join code shown to the other player) and the client joins it by code. NGO is started by `NgoSessionNetworkHandler` on the root's own NetworkManager, after the caller wrote `NetworkConfig.ConnectionData`, so payload and approval work exactly as with `DirectConnection`. Requires a signed-in player (`UgsBootstrapper`). Each create / join returns its own `RelaySessionStart`; the caller `Adopt`s it as the current session or leaves that instance with `LeaveAsync(RelaySessionStart)`.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/RelaySessionConnection.cs`

## Hereda / implementa
`MonoBehaviour`, [[IConnectionStrategy]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class RelaySessionConnection {
    <<MonoBehaviour>>
    -NetworkSettings _settings
    -UnityTransport _transport
    -string _region
    -float _connectTimeoutSeconds
    -float _alreadyInSessionRetrySeconds
    -float _hostLeftWaitSeconds
    -float _reconnectConnectWaitSeconds
    +ISession Session
    +string LastErrorKey
    +bool HasSession
    +bool IsOnline
    +IsSessionMember(string ugsPlayerId) bool
    +RefreshSessionAsync() Task
    ...9 más
  }
  class IConnectionStrategy:::external
  <<interface>> IConnectionStrategy
  class NetworkSettings:::external
  <<ScriptableObject>> NetworkSettings
  class RelaySessionStart:::external
  RelaySessionConnection --> NetworkSettings
  RelaySessionConnection --> RelaySessionStart
  RelaySessionConnection ..|> IConnectionStrategy
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IConnectionStrategy]], [[MembershipMatch]], [[MembershipRules]], [[NetworkSettings]], [[NgoSessionNetworkHandler]], [[OnlineSessionErrorKeys]], [[OnlineSessionErrorMapper]], [[RejoinOfferRules]], [[RelaySessionStart]], [[SessionSdkWorkarounds]]

## Usado por
[[ConnectionApprovalHandler]], [[OnlineSessionService]], [[SessionExit]]
