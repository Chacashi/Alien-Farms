---
tags: [modulo, modulo/items]
---
# Módulo Items

Carpeta `Assets/Scripts/Items/` · 9 tipos. Las cajas punteadas son tipos de otros módulos.

Leyenda: `--|>` hereda · `..|>` implementa · `-->` tiene un campo/propiedad de ese tipo. Se muestran campos `[SerializeField]` y la API pública.

## Diagrama de clases
```mermaid
classDiagram
  direction LR
  class Bomb {
    <<MonoBehaviour>>
    -Rigidbody _rb
    -SphereCollider _collider
    -GameObject _armedIndicator
    +event Action~Bomb~ StateChanged
    +BombState State
    +SimulationRole Role
    +PlayerId Thrower
    +bool CanBeGrabbed
    +bool CanBeThrown
    +ThrowSettings ThrowSettings
    +float CollisionRadius
    +float HalfHeight
    +PlayerId Holder
    ...11 más
  }
  class BombBlastRules {
    <<static>>
    +ShouldStun(PlayerId thrower, PlayerId target, float sqrDistanceToTarget, float radius)$ bool
    +ShouldDetonateOnContact(BombState state, bool isThrowerBody)$ bool
    +HasTimedOut(float armedTime, float maxArmedTime)$ bool
  }
  class BombBlastVfx {
    <<MonoBehaviour>>
    -Renderer _renderer
    +Play(Vector3 position, float radius, float duration, IObjectPool~BombBlastVfx~ pool) void
  }
  class BombBlastVfxPlayer {
    <<MonoBehaviour>>
    -BombData _data
    -Transform _container
  }
  class BombPlacement {
    <<static>>
    +RandomPointInArea(Bounds area, float edgeMargin, IRandomSource rng)$ Vector3
  }
  class BombSpawnSchedule {
    +bool IsRunning
    +float TimeUntilNext
    +Start(float initialDelay) void
    +Stop() void
    +Tick(float deltaTime, int activeCount, int maxActive) bool
    +OnSpawned(float nextInterval) void
    +OnSpawnFailed(float retryDelay) void
    +NextInterval(Vector2 range, IRandomSource rng)$ float
  }
  class BombSpawner {
    <<MonoBehaviour>>
    -BombSpawnSettings _settings
    -BombData _data
    -AnimalZone _animalZone
    -Transform _bombsContainer
    +int ActiveCount
    +SimulationRole Role
    +ResetBombs() void
    +SetSimulationRole(SimulationRole role) void
    +SetNetworkHook(IBombNetworkHook hook) void
    +GetForReplica(Vector3 position) Bomb
    +ReleaseToPool(Bomb bomb) void
  }
  class BombState {
    <<enumeration>>
    Inactive
    Idle
    Carried
    Armed
  }
  class IBombNetworkHook {
    <<interface>>
    +OnSpawned(Bomb bomb) void
    +TryDespawn(Bomb bomb) bool
  }
  class AnimalZone:::external
  <<MonoBehaviour>> AnimalZone
  class BombData:::external
  <<ScriptableObject>> BombData
  class BombSpawnSettings:::external
  <<ScriptableObject>> BombSpawnSettings
  class IRandomSource:::external
  <<interface>> IRandomSource
  class ISimulationRoleTarget:::external
  <<interface>> ISimulationRoleTarget
  class IThrowable:::external
  <<interface>> IThrowable
  class ThrowSettings:::external
  <<ScriptableObject>> ThrowSettings
  Bomb --> BombData
  Bomb --> IBombNetworkHook
  Bomb --> ThrowSettings
  Bomb ..|> ISimulationRoleTarget
  Bomb ..|> IThrowable
  BombBlastVfxPlayer --> BombBlastVfx
  BombBlastVfxPlayer --> BombData
  BombSpawner --> AnimalZone
  BombSpawner --> Bomb
  BombSpawner --> BombData
  BombSpawner --> BombSpawnSchedule
  BombSpawner --> BombSpawnSettings
  BombSpawner --> IBombNetworkHook
  BombSpawner --> IRandomSource
  BombSpawner ..|> ISimulationRoleTarget
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Tipos

| Tipo | Clase | Descripción |
|---|---|---|
| [[Bomb]] | MonoBehaviour | Pooled stun bomb. Inert while lying around or dropped (Idle): anyone can carry it, it never explodes. Thrown, it is Armed and explodes on it… |
| [[BombBlastRules]] | static | Pure rules of the stun bomb (who gets stunned, when it explodes). No Unity state, fully testable. |
| [[BombBlastVfx]] | MonoBehaviour | Pooled blast effect: a sphere that expands to the blast size and fades out, then returns to its pool. |
| [[BombBlastVfxPlayer]] | MonoBehaviour | Plays a pooled `BombBlastVfx` on every `GameEvents.OnBombDetonated`. |
| [[BombPlacement]] | static | Pure placement helper for bomb spawns. |
| [[BombSpawnSchedule]] | class | Pure spawn timer for bombs. Counts down only while running and while there is a free slot (the timer pauses while the max number of bombs is… |
| [[BombSpawner]] | MonoBehaviour | Scene composition root for stun bombs: owns the bomb pool and drops bombs at random free spots of the Animal Zone while a round is playing (… |
| [[BombState]] | enumeration | Lifecycle of a pooled `Bomb`. |
| [[IBombNetworkHook]] | interface | Lets a network layer follow the authority's bomb lifecycle. Without a hook, bombs go straight back to their pool on despawn (local mode). |

## Depende de
[[Modulo Animals|Animals]] (2), [[Modulo Core|Core]] (18), [[Modulo Data|Data]] (5), [[Modulo Utilities|Utilities]] (4), [[Modulo Zones|Zones]] (1)