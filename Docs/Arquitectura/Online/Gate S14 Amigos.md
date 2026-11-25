# Gate S14 — Amigos e invitaciones (MPPM, 2026-11-25)

Dos instancias: editor principal **A** (perfil `default`) y clon MPPM **B** (perfil `player2`).

| Jugador | UGS PlayerId | Nombre |
|---|---|---|
| A (host) | `LnLCcU3AbOwSKlQwXcbe9tgSRPbR` | AustereDraggingCoat#57943 |
| B (invitado) | `eXr0K0nGnBTpNUYxzKcux5cQRo9T` | ExceptionalDenyingLightbulb#78609 |

| # | Punto | Resultado | Evidencia |
|---|---|---|---|
| 1 | Las dos llegan a Friends Ready, con Ids distintos en el panel | OK | `Ready`, `notif=True` en las dos |
| 2 | Solicitud por Id | OK | A `request_sent`; B la ve entrante y A saliente |
| 3 | Aceptar, rechazar y cancelar | OK | Tres ciclos: `request_declined`, `request_canceled` y `friend_added` (`FriendCount=1`) |
| 4 | Presencia | OK | Primero `Online hub` en las dos; cuando A crea la sala FN8NLT, B ve a A en `Online room`. *Busy/match sin probar: hace falta una partida real* |
| 5 | Invitar desde la Sala | OK | A `invite_sent`; B recibe `PendingInvite` con code FN8NLT y el aviso con cuenta regresiva de 120 s |
| 6 | Aceptar → entrar a la sala | OK | B `Joining` → `InSession FN8NLT parts=2`; A `parts=2` |
| 7 | Cooldown de 5 s y sala llena | OK | `CanInvite=False` y el botón Invitar deshabilitado |
| 8 | Invitación vencida | OK | `online.error.invite_expired`, sin join; también cuando vence la TTL en `LateUpdate` |
| 9 | Rechazar el aviso | OK | `HasInvite=False` y el aviso se oculta |
| 10 | Tres Play seguidos sin domain reload | OK | Friends Ready en cada Play, sin excepciones; la invitación llega a A en el título y aceptarla lo lleva a `PlayOnline` → `InSession` |
| 11 | Consola | OK (Friends) | Sin warnings de `FriendsSocialService` |

## Bugs encontrados y arreglados
1. **Presencia vieja tras Play sin domain reload.** El SDK sobrevive al Play con su caché, y el evento Online del otro jugador se perdía mientras Wire se reconectaba. Ahora, si el SDK ya estaba inicializado, `FriendsSocialService` llama a `ForceRelationshipsRefreshAsync` antes de publicar.
2. **Aceptar desde el título sin dueño del título no avisaba nada.** El error ahora sale en el propio aviso.

## Pendiente / no probado
- Busy/match y «mismo personaje»: necesitan una partida completa. Se ven en el video con la build de 2 PCs.
- Aceptar estando en otra Sala (LeaveThenJoin) y con una oferta de reconexión: hace falta una 3.ª sesión. Están cubiertos por `InviteJoinSequenceTests` e `InviteAcceptRulesTests`.
- Caída de notificaciones → refresco de 30 s: no se forzó.
- Ruido ajeno a Friends al salir de Play sin domain reload: el `FATAL` del websocket de Wire y `TimeoutException` en el logout y leave de Vivox (`VoiceChatService.cs:391/419`).
