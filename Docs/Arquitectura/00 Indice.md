---
tags: [indice]
---
# Alien Farms — Arquitectura del código

Vault generado automáticamente por `Tools/generate_obsidian_docs.py` desde `Assets/Scripts`. **No edites** `Clases/` ni `Modulos/` a mano: se regeneran. Para notas propias crea archivos fuera de esas carpetas.

Tip: abre la **vista de grafo** (Ctrl+G) para ver las dependencias entre clases.

## Dependencias entre módulos
```mermaid
flowchart LR
  Animals["Animals (14)"]
  Audio["Audio (19)"]
  Core["Core (36)"]
  Data["Data (29)"]
  Editor["Editor (9)"]
  Input["Input (8)"]
  Items["Items (9)"]
  Match["Match (20)"]
  Menu["Menu (12)"]
  Networking["Networking (132)"]
  Player["Player (16)"]
  UI["UI (35)"]
  Utilities["Utilities (5)"]
  Zones["Zones (11)"]
  Animals -->|15| Core
  Animals -->|7| Data
  Animals -->|2| Items
  Animals -->|2| Zones
  Audio -->|1| Animals
  Audio -->|23| Core
  Audio -->|19| Data
  Audio -->|1| Items
  Audio -->|1| Match
  Audio -->|3| Networking
  Audio -->|12| Utilities
  Core -->|2| Animals
  Core -->|3| Data
  Core -->|2| Items
  Core -->|2| Match
  Core -->|1| Networking
  Core -->|3| Player
  Data -->|1| Animals
  Data -->|1| Audio
  Data -->|17| Core
  Data -->|2| Items
  Data -->|4| Networking
  Editor -->|4| UI
  Input -->|6| Core
  Input -->|2| Data
  Items -->|2| Animals
  Items -->|18| Core
  Items -->|5| Data
  Items -->|4| Utilities
  Items -->|1| Zones
  Match -->|4| Animals
  Match -->|34| Core
  Match -->|6| Data
  Match -->|1| Items
  Match -->|6| Networking
  Match -->|3| Player
  Match -->|1| UI
  Match -->|1| Zones
  Menu -->|19| Core
  Menu -->|13| Data
  Menu -->|3| Input
  Menu -->|4| Networking
  Menu -->|8| UI
  Networking -->|11| Animals
  Networking -->|144| Core
  Networking -->|55| Data
  Networking -->|4| Input
  Networking -->|9| Items
  Networking -->|8| Match
  Networking -->|29| Player
  Networking -->|1| UI
  Networking -->|4| Zones
  Player -->|3| Animals
  Player -->|38| Core
  Player -->|13| Data
  Player -->|2| Input
  Player -->|1| Items
  Player -->|3| Utilities
  UI -->|2| Audio
  UI -->|48| Core
  UI -->|21| Data
  UI -->|3| Match
  UI -->|4| Networking
  UI -->|1| Player
  Zones -->|8| Core
  Zones -->|4| Data
  Zones -->|3| Utilities
```

## Módulos

- [[Modulo Animals]] — 14 tipos
- [[Modulo Audio]] — 19 tipos
- [[Modulo Core]] — 36 tipos
- [[Modulo Data]] — 29 tipos
- [[Modulo Editor]] — 9 tipos
- [[Modulo Input]] — 8 tipos
- [[Modulo Items]] — 9 tipos
- [[Modulo Match]] — 20 tipos
- [[Modulo Menu]] — 12 tipos
- [[Modulo Networking]] — 132 tipos
- [[Modulo Player]] — 16 tipos
- [[Modulo UI]] — 35 tipos
- [[Modulo Utilities]] — 5 tipos
- [[Modulo Zones]] — 11 tipos

## Transversal
- [[Eventos GameEvents]] — quién publica y quién escucha cada evento global

## Jerarquías principales
```mermaid
classDiagram
  direction BT
  class Animal{
    <<abstract>>
  }
  class AnimalData{
    <<ScriptableObject>>
  }
  class AnimalSpawner{
    <<MonoBehaviour>>
  }
  class Bomb{
    <<MonoBehaviour>>
  }
  class BombSpawner{
    <<MonoBehaviour>>
  }
  class Chicken
  class ChickenData
  class CloudSavePlayerStatsStore
  class CountdownState
  class Cow
  class DirectConnection{
    <<MonoBehaviour>>
  }
  class IAnimalNetworkHook{
    <<interface>>
  }
  class IBombNetworkHook{
    <<interface>>
  }
  class IConnectionStrategy{
    <<interface>>
  }
  class IGrabbable{
    <<interface>>
  }
  class IMatchContext{
    <<interface>>
  }
  class IMatchExitHandler{
    <<interface>>
  }
  class IPendingStatsStore{
    <<interface>>
  }
  class IPlayerCommandSource{
    <<interface>>
  }
  class IPlayerRigLookup{
    <<interface>>
  }
  class IPlayerRoster{
    <<interface>>
  }
  class IPlayerStatsStore{
    <<interface>>
  }
  class IRandomSource{
    <<interface>>
  }
  class IScareable{
    <<interface>>
  }
  class ISimulationRoleTarget{
    <<interface>>
  }
  class IStunnable{
    <<interface>>
  }
  class IThrowable{
    <<interface>>
  }
  class IVolumeStore{
    <<interface>>
  }
  class IZoneResolver{
    <<interface>>
  }
  class LocalMatchExitHandler
  class LocalPlayerRigLookup{
    <<MonoBehaviour>>
  }
  class MatchManager{
    <<MonoBehaviour>>
  }
  class MatchState{
    <<abstract>>
  }
  class NetworkAnimal
  class NetworkBomb
  class NetworkCarriable{
    <<abstract>>
  }
  class NetworkCommandSource
  class NetworkPlayerRoster{
    <<MonoBehaviour>>
  }
  class NetworkSpawnRegistry{
    <<MonoBehaviour>>
  }
  class ObstacleField{
    <<MonoBehaviour>>
  }
  class OnlineMatchExitHandler
  class PlayerGrabber{
    <<MonoBehaviour>>
  }
  class PlayerInputHandler{
    <<MonoBehaviour>>
  }
  class PlayerJoinCoordinator{
    <<MonoBehaviour>>
  }
  class PlayerMovement{
    <<MonoBehaviour>>
  }
  class PlayerPrefsPendingStatsStore
  class PlayerPrefsVolumeStore
  class PlayerStunReceiver{
    <<MonoBehaviour>>
  }
  class PlayerZoneRegistry{
    <<MonoBehaviour>>
  }
  class PlayingState
  class RelaySessionConnection{
    <<MonoBehaviour>>
  }
  class ResultsState
  class Sheep
  class SimulatedFailureStatsStore
  class SystemRandomSource
  class WaitingForPlayersState
  Animal ..|> IScareable
  Animal ..|> ISimulationRoleTarget
  Animal ..|> IThrowable
  AnimalSpawner ..|> ISimulationRoleTarget
  Bomb ..|> ISimulationRoleTarget
  Bomb ..|> IThrowable
  BombSpawner ..|> ISimulationRoleTarget
  Chicken --|> Animal
  ChickenData --|> AnimalData
  CloudSavePlayerStatsStore ..|> IPlayerStatsStore
  CountdownState --|> MatchState
  Cow --|> Animal
  DirectConnection ..|> IConnectionStrategy
  IPlayerRoster ..|> IPlayerRigLookup
  IThrowable ..|> IGrabbable
  LocalMatchExitHandler ..|> IMatchExitHandler
  LocalPlayerRigLookup ..|> IPlayerRigLookup
  MatchManager ..|> IMatchContext
  NetworkAnimal --|> NetworkCarriable
  NetworkBomb --|> NetworkCarriable
  NetworkCommandSource ..|> IPlayerCommandSource
  NetworkPlayerRoster ..|> IPlayerRoster
  NetworkSpawnRegistry ..|> IAnimalNetworkHook
  NetworkSpawnRegistry ..|> IBombNetworkHook
  ObstacleField ..|> ISimulationRoleTarget
  OnlineMatchExitHandler ..|> IMatchExitHandler
  PlayerGrabber ..|> ISimulationRoleTarget
  PlayerInputHandler ..|> IPlayerCommandSource
  PlayerJoinCoordinator ..|> IPlayerRoster
  PlayerMovement ..|> ISimulationRoleTarget
  PlayerPrefsPendingStatsStore ..|> IPendingStatsStore
  PlayerPrefsVolumeStore ..|> IVolumeStore
  PlayerStunReceiver ..|> ISimulationRoleTarget
  PlayerStunReceiver ..|> IStunnable
  PlayerZoneRegistry ..|> IZoneResolver
  PlayingState --|> MatchState
  RelaySessionConnection ..|> IConnectionStrategy
  ResultsState --|> MatchState
  Sheep --|> Animal
  SimulatedFailureStatsStore ..|> IPlayerStatsStore
  SystemRandomSource ..|> IRandomSource
  WaitingForPlayersState --|> MatchState
```