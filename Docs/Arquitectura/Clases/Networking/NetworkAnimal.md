---
modulo: Networking
namespace: AlienFarms.Networking.Animals
tipo: class
archivo: "Assets/Scripts/Networking/Animals/NetworkAnimal.cs"
tags: [clase, modulo/networking]
---
# NetworkAnimal

> Network adapter of a pooled `Animal`. Pose: NetworkTransform + NetworkRigidbody (server authority). Server: mirrors `Animal.StateChanged` (and the drop reason) into `AnimalNetState` and drives ownership / parenting while it is carried (`NetworkCarriable`). Clients: the animal is a Replica; the replicated state is applied with `Animal.ApplyReplicatedState` and the matching grab / drop / capture GameEvents are raised here (never on the server: no double events on the host). Inactive is never applied from the state: the despawn (pool handler) takes it out of play.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Animals` · **Archivo:** `Assets/Scripts/Networking/Animals/NetworkAnimal.cs`

## Hereda / implementa
[[NetworkCarriable]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class NetworkAnimal {
    -Animal _animal
    +Animal Animal
    +AnimalNetState ReplicatedState
    +IGrabbable Grabbable
    +PlayerId Claimant
    #Awake() void
    +ServerPrepareForSpawn() void
    +OnNetworkSpawn() void
    +OnNetworkDespawn() void
    #RestoreAuthorityPhysics() void
    #OnValidate() void
  }
  class Animal:::external
  <<abstract>> Animal
  class AnimalNetState:::external
  <<struct>> AnimalNetState
  class IGrabbable:::external
  <<interface>> IGrabbable
  class NetworkCarriable:::external
  <<abstract>> NetworkCarriable
  NetworkAnimal --> Animal
  NetworkAnimal --> AnimalNetState
  NetworkAnimal --> IGrabbable
  NetworkAnimal --|> NetworkCarriable
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Animal]], [[AnimalNetState]], [[AnimalState]], [[DropReason]], [[GameEvents]], [[IGrabbable]], [[NetworkCarriable]], [[PlayerId]], [[SimulationRole]]

## Usado por
[[NetworkAnimalPoolHandler]], [[NetworkPoolLog]], [[NetworkSpawnRegistry]]

## Eventos que publica
[[Eventos GameEvents#OnAnimalCaptured|OnAnimalCaptured]], [[Eventos GameEvents#OnAnimalDropped|OnAnimalDropped]], [[Eventos GameEvents#OnAnimalGrabbed|OnAnimalGrabbed]], [[Eventos GameEvents#OnAnimalLeftZone|OnAnimalLeftZone]]

## Eventos que escucha
[[Eventos GameEvents#OnAnimalDropped|OnAnimalDropped]]
