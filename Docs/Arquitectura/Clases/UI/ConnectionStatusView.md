---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/ConnectionStatusView.cs"
tags: [clase, modulo/ui]
---
# ConnectionStatusView

> HUD connection banner (online). Listens to `GameEvents.OnPlayerConnectionChanged`: Reconnecting: "Waiting for P2 · {character}: n s", counted down locally on unscaled time. The template is built once per state change and the number is written with SetText(format, float) only when the whole second changes (no allocation per frame). Left / TimedOut / back after a drop: a toast that hides after `_toastSeconds`, then the banner returns to any seat that is still reserved. Character names come from `GameEvents.OnPlayerCharacterAssigned` (kept after the avatar despawns). Lives on an always-active object and toggles `_root`.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/ConnectionStatusView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ConnectionStatusView {
    <<MonoBehaviour>>
    -LocalizedString _reconnecting
    -LocalizedString _returned
    -LocalizedString _left
    -LocalizedString _timedOut
    -LocalizedString _player1Label
    -LocalizedString _player2Label
    -float _toastSeconds
    -GameObject _root
    -TMP_Text _label
    +ConnectionBanner Shown
  }
  class LocalizedStringBinding:::external
  class PlayerCharacterCache:::external
  ConnectionStatusView --> LocalizedStringBinding
  ConnectionStatusView --> PlayerCharacterCache
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]], [[ConnectionBanner]], [[ConnectionStatusRules]], [[GameEvents]], [[LocalizationKeys]], [[LocalizedStringBinding]], [[PlayerCharacterCache]], [[PlayerConnectionStatus]], [[PlayerId]]

## Eventos que escucha
[[Eventos GameEvents#OnPlayerCharacterAssigned|OnPlayerCharacterAssigned]], [[Eventos GameEvents#OnPlayerConnectionChanged|OnPlayerConnectionChanged]]
