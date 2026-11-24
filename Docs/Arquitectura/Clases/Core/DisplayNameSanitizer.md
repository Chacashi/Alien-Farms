---
modulo: Core
namespace: AlienFarms.Core
tipo: static
archivo: "Assets/Scripts/Core/DisplayNameSanitizer.cs"
tags: [clase, modulo/core]
---
# DisplayNameSanitizer

> Cleans a player name that came from another machine (connection payload, session player property) before it is stored or shown: drops control and invisible format characters (zero-width, bidi overrides) and the rich text brackets '<' / '>' (TextMeshPro would parse tags), trims surrounding white space and caps it at `MaxLength` characters (never splitting a surrogate pair). A name that is empty afterwards stays empty (callers show their own placeholder). Every cut (`MaxLength` characters, `FitToPayload` bytes) keeps a trailing UGS discriminator ("Name#1234") and shortens the part before it. Pure.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/DisplayNameSanitizer.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class DisplayNameSanitizer {
    <<static>>
    +FitToPayload(string name)$ string
    +Fit(string name, int budget, bool inUtf8Bytes)$ string
    +Sanitize(string name)$ string
    +IsClean(string name)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[ConnectionPayload]], [[DisplayNameResolver]], [[NetworkLobbyState]], [[NetworkPlayer]], [[OnlineMenuController]], [[OnlinePanelView]], [[OnlineSessionChannel]], [[OnlineSessionService]], [[SessionExit]]
