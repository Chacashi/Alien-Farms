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
  Audio["Audio (11)"]
  Core["Core (19)"]
  Data["Data (23)"]
  Editor["Editor (9)"]
  Input["Input (8)"]
  Items["Items (9)"]
  Match["Match (17)"]
  Menu["Menu (4)"]
  Player["Player (15)"]
  UI["UI (23)"]
  Utilities["Utilities (4)"]
  Zones["Zones (11)"]
  Animals -->|15| Core
  Animals -->|7| Data
  Animals -->|2| Items
  Animals -->|2| Zones
  Audio -->|1| Animals
  Audio -->|5| Core
  Audio -->|6| Data
  Audio -->|1| Items
  Audio -->|5| Utilities
  Core -->|2| Animals
  Core -->|2| Data
  Core -->|2| Items
  Data -->|1| Animals
  Data -->|1| Audio
  Data -->|4| Core
  Data -->|2| Items
  Editor -->|4| UI
  Input -->|6| Core
  Input -->|2| Data
  Items -->|2| Animals
  Items -->|18| Core
  Items -->|5| Data
  Items -->|4| Utilities
  Items -->|1| Zones
  Match -->|3| Animals
  Match -->|25| Core
  Match -->|6| Data
  Match -->|1| Items
  Match -->|3| Player
  Match -->|1| UI
  Match -->|1| Zones
  Menu -->|5| Core
  Menu -->|5| Data
  Menu -->|2| Input
  Menu -->|4| UI
  Player -->|3| Animals
  Player -->|38| Core
  Player -->|13| Data
  Player -->|2| Input
  Player -->|1| Items
  Player -->|3| Utilities
  UI -->|2| Audio
  UI -->|26| Core
  UI -->|10| Data
  UI -->|3| Match
  UI -->|1| Player
  Zones -->|8| Core
  Zones -->|4| Data
  Zones -->|3| Utilities
```

## Módulos

- [[Modulo Animals]] — 14 tipos
- [[Modulo Audio]] — 11 tipos
- [[Modulo Core]] — 19 tipos
- [[Modulo Data]] — 23 tipos
- [[Modulo Editor]] — 9 tipos
- [[Modulo Input]] — 8 tipos
- [[Modulo Items]] — 9 tipos
- [[Modulo Match]] — 17 tipos
- [[Modulo Menu]] — 4 tipos
- [[Modulo Player]] — 15 tipos
- [[Modulo UI]] — 23 tipos
- [[Modulo Utilities]] — 4 tipos
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
  class CountdownState
  class Cow
  class IGrabbable{
    <<interface>>
  }
  class IMatchContext{
    <<interface>>
  }
  class IMatchExitHandler{
    <<interface>>
  }
  class IPlayerCommandSource{
    <<interface>>
  }
  class IPlayerRoster{
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
  class MatchManager{
    <<MonoBehaviour>>
  }
  class MatchState{
    <<abstract>>
  }
  class ObstacleField{
    <<MonoBehaviour>>
  }
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
  class PlayerPrefsVolumeStore
  class PlayerStunReceiver{
    <<MonoBehaviour>>
  }
  class PlayerZoneRegistry{
    <<MonoBehaviour>>
  }
  class PlayingState
  class ResultsState
  class Sheep
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
  CountdownState --|> MatchState
  Cow --|> Animal
  IThrowable ..|> IGrabbable
  LocalMatchExitHandler ..|> IMatchExitHandler
  MatchManager ..|> IMatchContext
  ObstacleField ..|> ISimulationRoleTarget
  PlayerGrabber ..|> ISimulationRoleTarget
  PlayerInputHandler ..|> IPlayerCommandSource
  PlayerJoinCoordinator ..|> IPlayerRoster
  PlayerMovement ..|> ISimulationRoleTarget
  PlayerPrefsVolumeStore ..|> IVolumeStore
  PlayerStunReceiver ..|> ISimulationRoleTarget
  PlayerStunReceiver ..|> IStunnable
  PlayerZoneRegistry ..|> IZoneResolver
  PlayingState --|> MatchState
  ResultsState --|> MatchState
  Sheep --|> Animal
  SystemRandomSource ..|> IRandomSource
  WaitingForPlayersState --|> MatchState
```