---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: static
archivo: "Assets/Scripts/Networking/Session/UgsProfileResolver.cs"
tags: [clase, modulo/networking]
---
# UgsProfileResolver

> Picks the Unity Authentication profile of this process so every Multiplayer Play Mode instance (and every build started on the same PC) signs in as a different anonymous player. Editor PlayerPrefs are shared by the MPPM clones, so without a profile they would all reuse one session token and one PlayerId. Pure, so it is tested. Priority: an explicit -ugs-profile <name> argument, then the MPPM -name PlayerN argument of a virtual player; the main editor (and a build without arguments) keeps the default profile (null).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/UgsProfileResolver.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class UgsProfileResolver {
    <<static>>
    +Resolve(IReadOnlyList~string~ commandLineArgs, bool isMainEditor)$ string
    +Sanitize(string raw)$ string
    +FindArgumentValue(IReadOnlyList~string~ args, string argument)$ string
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[UgsBootstrapper]]
