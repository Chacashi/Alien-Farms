---
tipo: diseño
tarea: 2.7
estado: aprobado con cambios
fecha: 2026-11-12
---

> **Espectador retirado (Bloque 0, 2026-11-24) — prevalece sobre todo lo demás:** el online tiene solo dos plazas (P1 y P2). Ya no existen `SpectatorSeat`, `ParticipantRoleRules`, `SpectatorModeController`, `SpectatorPanelView` ni la cámara de espectador del anchor; `SeatPresenceBoard` lleva solo P1 y P2 (36 bytes, protocolo 4). Una plaza reservada por la gracia solo la recupera su dueño (mismo UgsPlayerId, ClientId nuevo); un tercero recibe «Sala llena» (`room_full` en NGO o lobby lleno en UGS) y, si llega a NGO, el host lo quita de la sesión UGS para no bloquear la vuelta de P2. El ingreso tardío se demuestra ahora con la reconexión de P2 y con un jugador nuevo tras vencer la gracia.

> **Cambios aprobados por el usuario (2026-11-12) — prevalecen sobre el resto del documento:**
> - Decisiones 1 (salida voluntaria libera al instante) y 2 (reconexión con botón en el Hub): **aprobadas**.
> - Decisión 6 **reemplazada**: el nombre lo **escribe el jugador** en el Hub online (`INP_Name`, TMP_InputField), saneado con `DisplayNameSanitizer`, guardado por dispositivo en `PlayerPrefs` (conveniencia local) y enviado en el `ConnectionPayload` como hoy. Vacío → nombre de UGS autogenerado (`GetPlayerNameAsync(true)`) y, sin UGS, «Player». `OnlineSessionChannel` expone `DisplayName`/`SetDisplayName(string)`.
> - Sección 1 **descartada**: **no hay `ResumeToken`** ni mensaje `AF.Ticket`. La reconexión al asiento en espera sigue siendo por UgsPlayerId (membresía UGS de 2.3); el riesgo de suplantación entre miembros queda **documentado como limitación conocida**. `ConnectionPayload` sigue en protocolo 2. `ReconnectTicketMessenger` se reduce a `LeaveNoticeMessenger` (solo `"AF.Leaving"`). La oferta de reconexión del cliente se arma con su propio sessionId/código (no hace falta que el servidor emita nada). El punto 6 del checklist se elimina; el punto 4 ya no prueba rotación de token.

# Diseño de la tarea 2.7: gracia y suspensión, reconexión, espectador, name tags

Este diseño baja la fila 2.7 del [[Plan Modo Online]] a clases y cableado concretos, sobre el commit `b4429ef`. Gate: **checklist MPPM sin duplicados** (sección 7).

## 0. Decisiones (a aprobar por el usuario)

1. **La gracia aplica solo a una caída** (la conexión se pierde sin aviso). Una **salida voluntaria** (botón Salir) avisa al servidor y libera el asiento al instante: el rival ve «P2 salió» y el match pasa a `WaitingForPlayers` sin esperar 20 s. Si el aviso se pierde, se cae al camino de la gracia (20 s y luego «P2 no volvió»).
2. **La reconexión se hace desde el Hub online.** El cliente caído vuelve al Hub con `connection_lost` y un botón **«Reconectar (n s)»** que dura lo mismo que la gracia. No se reconecta solo dentro de Prototype: así se reutiliza el camino ya probado (unirse + sincronizar la escena) y se ve el clientId nuevo.
   - **Actualización 2.9 (auto-rejoin):** el Hub ya no espera el clic. Mientras dure la oferta, `OnlineSessionService` reintenta solo por el canal (`RequestRejoin`) cada `NetworkSettings.AutoRejoinRetrySeconds` (2 s; también es la espera antes del primer intento), solo desde `Ready` y solo si `Application.internetReachability` indica red (`RejoinOfferRules.ShouldAutoRejoin`). El Hub muestra Busy/«Uniendo» con Cancelar en cada intento. Un fallo sin red/servicio (`no_connection`, `services_unavailable`, `rate_limited`) ya no cae a la unión por código (`RejoinOfferRules.FallsBackToCodeJoin`), y sin red alcanzable `RejoinAsync` devuelve `no_connection`: así la oferta sobrevive hasta que vuelva la red. Esas tres claves conservan la oferta (`KeepsOfferAfterFailure`); `rate_limited` duplica el retraso del siguiente intento (tope 8 s). Salir del Hub al título declina la oferta (`OnlineSessionChannel.RequestDeclineRejoin`); Cancelar en Busy la descarta vía `RequestLeave`. Se desactiva con `NetworkSettings.AutoRejoin = false` (vuelve el comportamiento solo-botón).
3. **Durante la suspensión** (Countdown, Playing o Results) el state machine no avanza, el reloj queda congelado en todas las instancias y **los controles de P1 se congelan**. Los animales y las mechas de las bombas siguen corriendo (aceptado).
4. **Al reanudar:** en Playing se reescribe `RoundEndTime = now + RemainingTime` y se reactivan los controles. En Countdown la cuenta se reinicia (`RestartRound`). En Results se continúa igual.
5. **El avatar del jugador caído se despawnea** (igual que hoy) y su pose queda en `PlayerSlot.SavedPose`. Al volver se spawnea un `Player_Net` nuevo en esa pose. No se transfiere el ownership del avatar viejo: el código de owner de input, motion y cámara supone que el ownership está fijo desde el spawn.
6. **Nombres = `AuthenticationService.PlayerName`**, autogenerado por UGS (`GetPlayerNameAsync(autoGenerate: true)`, formato «Nombre#1234»). Se muestran completos y saneados. El `_displayName` serializado («Player») queda como fallback para la conexión directa de desarrollo.

## 1. Seguridad del asiento reservado: token de reanudación en lugar de AllocationId

**Verificado (Transport 2.7.4 / NGO 2.14 / Multiplayer Services 2.4.1):**
- El host **sí puede leer el AllocationId real** de una conexión. `UnityTransport.GetEndpoint(clientId)` devuelve el `NetworkEndpoint` que `RelayLayer` crea con `FromAllocationId.ToNetworkEndpoint()`, es decir, los primeros 16 bytes del endpoint son el `RelayAllocationId`. La conversión (`AsRelayAllocationId`) es `internal`: habría que reinterpretar la memoria con `UnsafeUtility.As`, un layout sin documentar.
- **Lo que falla es la otra punta.** No hay con qué comparar ese valor de forma confiable:
  - `IReadOnlyPlayer.AllocationId` lo fija `NetworkModule` con `CurrentPlayer.SetAllocationId` **después** de `StartNetworkHandlerAsync`, que espera a que NGO apruebe la conexión. Exigirlo en la aprobación produce un deadlock.
  - `SessionHandler.SavePlayerDataAsync` solo envía `Properties`: **el AllocationId nunca llega al lobby**, así que el host no lo ve.
  - Publicarlo nosotros como propiedad del jugador exige tener el `ISession` antes de `StartClient`, y `JoinSessionByCodeAsync` no lo entrega hasta terminar.

**Alternativa elegida: `ResumeToken`.**
- Son 128 bits aleatorios (`RandomNumberGenerator`). El servidor los emite **solo al dueño del asiento** por un mensaje con nombre (sobre Relay va cifrado con DTLS).
- Para reclamar un asiento en espera hay que presentar el token. Un miembro legítimo que declare el UgsPlayerId de otro no lo tiene, así que recibe `reconnect_denied`.
- El token **rota en cada reconexión** (no se puede reusar) y además permite reconectar por `DirectConnection` sin UGS, útil para MPPM.
- Se mantienen la membresía UGS de la 2.3 y `already_connected`.

## 2. Flujo

```
Caída de P2 (sin aviso)                       Servidor (host)
 transport timeout (~6 s) ───────────────▶ ConnectionApprovalHandler: MarkDisconnected → SeatReserved(P2)
                                            NetworkPlayerRoster: guarda pose, suelta lo cargado, Despawn
                                            ReconnectGraceCoordinator: MatchManager.SetSuspended(true),
                                              NetworkMatchState.ServerFreezeClock(), presencia = Reconnecting(graceEnd)
 Cliente: Hub + "Reconectar (n s)"          Todos: GameEvents.OnPlayerConnectionChanged(P2, Reconnecting, 20)
 Reconectar → payload con ResumeToken ───▶ ApprovalRules: asiento en espera + token correcto → Reconnect (clientId nuevo)
                                            Roster: TrySpawn en SavedPose, mismo personaje → PlayerSpawned(P2, restored)
                                            Coordinator: presencia = Connected, ServerResumeClock() (RoundEndTime reescrito),
                                              SetSuspended(false)
 Sin volver en 20 s                         Approval.Update: ReleaseExpired → SeatReleased(P2, TimedOut)
                                            Coordinator: presencia = TimedOut, SetSuspended(false) → el estado aborta a Waiting
                                            (opcional) host: RemovePlayerAsync(ugsId) en la sesión
Salida voluntaria de P2 ────"AF.Leaving"──▶ Approval: Release al desconectar → SeatReleased(P2, Left) (sin gracia)
```

**Respaldo de la salida voluntaria:** el cliente hace `LeaveAsync` de la sesión al desmontar el root. Si el host recibe `PlayerHasLeft(ugsId)` y ese asiento está en espera, lo libera como `Left`.

## 3. Clases nuevas y modificadas

### 3.1 Runtime (`AlienFarms.Runtime`, no conoce Networking)

| Archivo | Cambio / responsabilidad | API |
|---|---|---|
| `Core/PlayerConnectionStatus.cs` (nuevo) | enum `byte`: `Connected=0, Reconnecting=1, Left=2, TimedOut=3` | — |
| `Core/GameEvents.cs` (mod.) | **Cambia la firma** de `OnPlayerConnectionChanged` | `static event Action<PlayerId, PlayerConnectionStatus, float> OnPlayerConnectionChanged` (float = segundos de gracia restantes, 0 si no aplica). `RaisePlayerConnectionChanged(...)`. El reset de estáticos ya existe |
| `Match/MatchResumeAction.cs` (nuevo) | enum `None, EnableControls, RestartCountdown` | — |
| `Match/MatchSuspendRules.cs` (nuevo, puro) | Qué fases se suspenden y qué hacer al reanudar | `static bool SuspendsIn(MatchPhase)` (Countdown, Playing, Results). `static MatchResumeAction OnResume(MatchPhase phase, bool playersComplete)`: con `!playersComplete` → None (el estado aborta solo) |
| `Match/MatchManager.cs` (mod.) | `SetSuspended` idempotente: al suspender apaga los controles; al reanudar aplica `OnResume(CurrentPhase, JoinedCount >= 2)` (EnableControls o `RestartRound()`) | Firma igual. `RemainingTime` ya existe |
| `Player/PlayerRig.cs` (mod.) | Coloca al jugador en una pose guardada sin pasar por el spawn | `void PlaceAt(Vector3 position, float yaw)`: ForceDrop, `Movement.Teleport`, `Camera.SnapYaw` |
| `Data/OnlineSessionChannel.cs` (mod.) | Oferta de reconexión para el Hub | `bool HasRejoinOffer`, `string RejoinCode`, `double RejoinExpiresAt` (realtime), `SetRejoinOffer(code, expiresAt)`, `ClearRejoinOffer()`, `bool RequestRejoin()`, `event Action RejoinRequested`. Sin listener → `NotConfigured` |
| `Data/NetworkSettings.cs` (mod.) | Tooltip de `AllowSpectator` actualizado. Nuevos `_showNameTags` (true) y `_nameTagShowsClientId` (debug: muestra «client N» en el tag para el tema 9) | Getters |
| `Core/OnlineSessionErrorKeys.cs` (mod.) | `connection_lost`, `reconnect_denied`, `rejoin_expired` | — |
| `Menu/OnlineMenuRules.cs` + `OnlineMenuController.cs` (mod.) | El Hub muestra `BTN_Rejoin` con su cuenta atrás (`SetText(fmt, float)`, redibujo a 1 Hz) mientras la oferta sea válida | `RejoinRoom()` (OnClick). `static bool ShowsRejoin(bool hasOffer, double expiresAt, double now)` |
| `UI/OnlinePanelView.cs` (mod.) | `_rejoinButton`, `_rejoinLabel` | `SetRejoin(bool visible, int seconds)` |
| `UI/ConnectionStatusView.cs` (nuevo) | Banner del HUD. Escucha `OnPlayerConnectionChanged` y `OnPlayerCharacterAssigned` (cachea el nombre del personaje). Reconnecting: «Esperando a P2 · {personaje}: {n} s», cuenta local con `unscaledDeltaTime` y `SetText` solo al cambiar el segundo. Left/TimedOut: toast que se oculta tras `_toastSeconds`. Connected tras una espera: «P2 volvió» | `_root`, `_label` (TMP), `LocalizedString _reconnecting/_returned/_left/_timedOut` (la plantilla se arma una vez por cambio de estado), `[Min] _toastSeconds` |
| `UI/ConnectionStatusRules.cs` (nuevo, puro) | Qué mostrar según el estado y los segundos | `static ConnectionBanner Resolve(PlayerConnectionStatus, float graceLeft, bool wasReconnecting)` |
| `UI/NameTagView.cs` (nuevo) | Vista world-space (TMP 3D) sobre el avatar, con billboard en `LateUpdate` hacia la cámara que se le asigna | `SetName(string)`, `SetDetail(string)` (línea de debug, null = oculta), `SetColor(Color)`, `SetVisible(bool)`, `SetViewCamera(Camera)` |
| `UI/SpectatorPanelView.cs` (nuevo) | «Estás mirando» + BTN_Leave (OnClick → `MatchExit.ReturnToTitle` en la escena) | `Show()`, `Hide()` |
| `UI/LocalizationKeys` + `Editor/LocalizationSeedData` | ES/EN: `online.status.reconnecting` ({0} jugador, {1} s), `online.status.returned`, `online.status.left`, `online.status.timed_out`, `online.spectator.watching`, `online.spectator.leave`, `online.hub.rejoin` ({0} s), las 3 claves `online.error.*` nuevas | Después se corre `Setup` |

### 3.2 Networking (`AlienFarms.Networking`)

**Session/**

| Archivo | Cambio / responsabilidad | API |
|---|---|---|
| `ResumeToken.cs` (nuevo) | `readonly struct : IEquatable` con `ulong Hi, Lo`. `IsValid` (≠ 0) | `static ResumeToken Create(RandomNumberGenerator rng)` |
| `ConnectionPayload.cs` (mod.) | Agrega `ulong ResumeHi, ResumeLo` después de `UgsPlayerId`. **Protocolo 3** (`NetworkSettings._protocolVersion = 3`). `MaxSerializedSize` 102 → **118** | `ResumeToken Resume { get; }`. `Create(protocol, name, ugsId, in ResumeToken resume = default)` |
| `PlayerSlot.cs` (mod.) | Campo `ResumeToken Token` | — |
| `DisconnectReasons.cs` / `OnlineSessionErrorMapper.cs` (mod.) | `ReconnectDenied = "reconnect_denied"`. Una caída sin motivo pasa a ser `connection_lost` (antes `host_left`; con el host realmente caído, el rejoin falla luego con `host_left`) | — |
| `ApprovalRules.cs` (mod.) | Regla nueva en el bloque de asientos: un asiento **en espera** se reclama si `payload.Resume == seat.Token` (si los dos traen UgsPlayerId, también deben coincidir). Si coincide el UgsPlayerId pero falta el token o es incorrecto → `Reject(reconnect_denied)`. `already_connected`, el espectador y `room_full` no cambian | `const string ReasonReconnectDenied`. `Evaluate(...)` con la misma firma (el token viaja en el payload) |
| `ServerSlotDirectory.cs` (mod.) | El token lo inyecta el llamador (sigue siendo puro) | `TryApply(in decision, clientId, in payload, in ResumeToken issued)`: AssignSeat guarda `issued`; Reconnect exige el token del payload y **lo rota** a `issued`. `bool TryGetToken(PlayerId, out ResumeToken)`. `bool ReleaseSeat(PlayerId)` (conectado o en espera). `bool TryFindAwaiting(in FixedString64Bytes ugsId, out PlayerId)`. `int CountAwaiting`. `bool ClearSavedPose(PlayerId)` |
| `ConnectionApprovalHandler.cs` (mod.) | Ciclo de vida de la reserva. **Gracia apagada por defecto** (en la sala del MainMenu se libera al instante, como hoy); la enciende el coordinator de Prototype. Al desconectar: si el cliente avisó que se iba → `Release`, si no y la gracia está encendida → `MarkDisconnected` + `SeatReserved`, y si no → `Release`. `Update` (solo si hay asientos en espera) → `ReleaseExpired` → `SeatReleased(id, TimedOut)`. Genera el token al aprobar y, en `OnClientConnectedCallback`, lo envía con el messenger | `bool GraceEnabled`, `void SetGraceEnabled(bool)`. `void NotifyLeaving(ulong clientId)` (un `HashSet<ulong>` preasignado, se limpia al desconectar). `void ReleaseLeftMember(string ugsId)` (respaldo por la sesión). `float GraceLeft(PlayerId)`. `double Now` (realtime). `event Action<PlayerId> SeatReserved`. `event Action<PlayerId, PlayerConnectionStatus> SeatReleased` (Left o TimedOut) |
| `ReconnectTicketMessenger.cs` (nuevo, en NetworkRoot) | Mensajes con nombre de `CustomMessagingManager`: `"AF.Ticket"` (servidor → cliente: token + asiento, ReliableSequenced) y `"AF.Leaving"` (cliente → servidor, sin cuerpo). Registra en `OnServerStarted`/`OnClientStarted` y desregistra en los Stopped | Servidor: `SendTicket(ulong clientId, in ResumeToken, PlayerId)`. Cliente: guarda el ticket en el anchor; `void SendLeavingNotice()`. Nombres como `const string` |
| `ReconnectTicket.cs` (nuevo) | `struct`: `ResumeToken Token`, `PlayerId Seat`, `string SessionId`, `string Code`, `double ExpiresAt` (realtime, `double.MaxValue` mientras siga conectado) | — |
| `ReconnectTicketRules.cs` (nuevo, puro) | `static bool IsUsable(in ReconnectTicket t, double now)`, `static ReconnectTicket Arm(in ReconnectTicket t, double dropTime, float grace)` | — |
| `OnlineSessionService.cs` (mod.) | (a) Nombre: tras el sign-in usa `Bootstrapper.PlayerName ?? _displayName`. (b) `BuildPayload()` agrega el token si hay un ticket usable. (c) Ante una caída propia (`HandleClientStopped` sin Leave, motivo vacío): `Arm` del ticket + `_channel.SetRejoinOffer(code, expiresAt)` con el error `connection_lost`. (d) `RejoinRequested` → estado Joining → `RelaySessionConnection.RejoinAsync`. (e) Una salida voluntaria o la expiración borran el ticket y la oferta. (f) Host: `PlayerHasLeft(ugsId)` → `approval.ReleaseLeftMember(ugsId)` | — |
| `RelaySessionConnection.cs` (mod.) | `internal Task<RelaySessionStart> RejoinAsync(NetworkManager, string sessionId, string code, CancellationToken)`: primero `MultiplayerService.Instance.ReconnectToSessionAsync(sessionId, new ReconnectSessionOptions{ Type = SessionType }.WithNetworkHandler(handler))`; si falla porque ya no es miembro o la sesión no existe, `JoinSessionByCodeAsync(code)`. `string SessionId`. `Task TryRemovePlayerAsync(string ugsId)` (host, best-effort, no lanza) | — |
| `UgsBootstrapper.cs` (mod.) | Después de `SignInAnonymouslyAsync`: `PlayerName = await auth.GetPlayerNameAsync(true)` (si falla → null y se loguea) | `string PlayerName` |
| `Data/NetworkRuntimeAnchor.cs` (mod.) | Ticket de reconexión (sobrevive al desmontaje del root, se descarta por generación). Cámara de vista local | `SetReconnectTicket(in ReconnectTicket)`, `bool TryGetReconnectTicket(out ReconnectTicket)`, `ClearReconnectTicket()`. `Camera ViewCamera` (= `LocalPlayer.ViewCamera` o la cámara de espectador). `SetSpectatorCamera(Camera)`, `ClearSpectatorCamera(Camera)` |
| `Diagnostics/DevConnectionPanel.cs` (mod.) | El payload directo incluye el token del anchor. Botón opcional «Simular caída» con `NetworkSimulator.NetworkEventsApi.Disconnect()`/`Reconnect()` | — |

**Player/**

| Archivo | Cambio / responsabilidad | API |
|---|---|---|
| `NetworkPlayerRoster.cs` (mod.) | Al desconectarse alguien: `slots.TrySavePose(id, pos, yaw)` antes de `DropCarried` y del Despawn. Un espectador no tiene avatar: se ignora sin warning. `TrySpawn`: con `SavedPose` → `rig.PlaceAt` + `MarkTeleport` + `ClearSavedPose`. Personaje sin elegir → `CharacterPickRules` | `event Action<PlayerId, bool> PlayerSpawned` (bool = restaurado desde una reserva) |
| `CharacterPickRules.cs` (nuevo, puro) | Personaje por defecto de un asiento nuevo: el índice del asiento, o el siguiente libre si coincide con el del otro asiento | `static int Pick(int seatIndex, int otherCharacter, int rosterCount)` |
| `NetworkPlayer.cs` (mod.) | Escucha `_displayName.OnValueChanged`. Cachea el string saneado (alloc solo al cambiar) | `string DisplayNameText`, `event Action<NetworkPlayer> DisplayNameChanged` |
| `NetworkNameTag.cs` (nuevo) | Maneja la `NameTagView` del `Player_Net`: nombre (fallback `CarriableOwnerRules.SeatTag`), color Primary del personaje, oculto en el propio owner y con `ShowNameTags` apagado. Detalle de debug «client N» si `NameTagShowsClientId`. Cámara = `anchor.ViewCamera` (compara la referencia por frame, sin alloc) | `[SerializeField] _player, _view, _anchor, _settings` |
| `CarriableOwnershipLabel.cs` (mod.) | `ResolveViewCamera` → `anchor.ViewCamera` (el espectador también ve las etiquetas) | — |

**Match/**

| Archivo | Cambio / responsabilidad | API |
|---|---|---|
| `SeatPresence.cs` / `SeatPresenceBoard.cs` (nuevos) | `struct : INetworkSerializable, IEquatable`. `SeatPresence {ulong ClientId; byte Status; double GraceEndTime}` (hora del servidor). `SeatPresenceBoard {SeatPresence P1, P2; ulong SpectatorClientId; bool HasSpectator}` | `Get(PlayerId)`, `With(PlayerId, in SeatPresence)`, `IsSpectator(ulong clientId)` |
| `SeatPresenceRules.cs` (nuevo, puro) | Diferencia entre dos tableros: qué `(PlayerId, status, graceLeft)` hay que publicar | `static int Diff(in SeatPresenceBoard prev, in SeatPresenceBoard cur, double now, Span<PresenceChange> output)` |
| `NetworkSeatPresence.cs` (nuevo, en MatchNetwork) | `NetworkVariable<SeatPresenceBoard>` (escribe el servidor). **Emisor único** de `OnPlayerConnectionChanged`: el servidor lo levanta al escribir, los clientes puros en `Update` comparando contra lo último aplicado. Al spawnear, el cliente (incluido el espectador tardío) publica el estado actual: así lo ve desde el primer frame | `ServerWrite(in SeatPresenceBoard)`, `SeatPresenceBoard Board`, `bool IsLocalSpectator`, `event Action<bool> LocalSpectatorChanged` |
| `NetworkMatchState.cs` (mod.) | Nueva `NetworkVariable<float> _frozenRemaining` (-1 = corre). `ServerWriteSnapshot` la incluye. Los clientes muestran el valor congelado | `ServerFreezeClock()` (`_frozenRemaining = RemainingTime`). `ServerResumeClock()` (`_roundEndTime = now + RemainingTime`, luego `_frozenRemaining = -1`, en el mismo tick) |
| `MatchClock.cs` (mod.) | Sobrecarga con el valor congelado | `DisplaySeconds(phase, roundEndTime, now, fullDuration, float frozenRemaining)` (≥ 0 → `Ceil(frozen)` en Playing) |
| `ReconnectGraceRules.cs` (nuevo, puro) | `static double GraceEndServerTime(double serverNow, double realtimeNow, double disconnectedAt, float grace)`, `static bool AnyAwaiting(ReadOnlySpan<PlayerSlot>)` | — |
| `ReconnectGraceCoordinator.cs` (nuevo, OnlineModeRoot, solo servidor) | Composition del ciclo de gracia en la escena. En `Start` (servidor): `approval.SetGraceEnabled(true)` y se suscribe a `SeatReserved`, `SeatReleased`, `roster.PlayerSpawned` y `OnClientConnected`/`OnClientDisconnect` (para el espectador). Reserva → `SetSuspended(true)` si `SuspendsIn(phase)` + `ServerFreezeClock`. Restaurado / Left / TimedOut → si ya no queda nadie en espera: `ServerResumeClock` + `SetSuspended(false)`. Siempre reconstruye el tablero desde `anchor.Slots`. En TimedOut → `TryRemovePlayerAsync(ugsId)`. En `OnDestroy` → `SetGraceEnabled(false)`. Fuera del servidor se deshabilita | `[SerializeField] _anchor, _settings, _match (MatchManager), _matchState, _presence, _roster` |
| `SpectatorModeController.cs` (nuevo, OnlineModeRoot) | Cliente puro: con `presence.IsLocalSpectator` activa `_spectatorRig` (Camera, EventSystem + InputSystemUIInputModule) y `_panel`, y llama `anchor.SetSpectatorCamera`. Al desactivarse la limpia | `[SerializeField] _anchor, _presence, _spectatorRig (GameObject), _spectatorCamera, _panel (SpectatorPanelView)` |
| `OnlineMatchExitHandler.cs` (mod.) | `ReturnToTitle` en un cliente puro: `messenger.SendLeavingNotice()` y `anchor.ClearReconnectTicket()` antes del `Teardown` (NGO vacía la cola de envío antes de cerrar; si el aviso se pierde, actúa el respaldo de la sesión o la gracia) | — |

## 4. Eventos

| Evento | Firma | Emite | Escuchan |
|---|---|---|---|
| `GameEvents.OnPlayerConnectionChanged` (**firma nueva**) | `Action<PlayerId, PlayerConnectionStatus, float>` | `NetworkSeatPresence`, una vez por cambio y por instancia (el servidor al escribir, los clientes al aplicar) | `ConnectionStatusView` (HUD). Solo se usaba en tests: se actualiza `GameEventsNetworkTests` |
| `ConnectionApprovalHandler.SeatReserved` / `SeatReleased` (instancia) | `Action<PlayerId>` / `Action<PlayerId, PlayerConnectionStatus>` | Approval (root, servidor) | `ReconnectGraceCoordinator` |
| `NetworkPlayerRoster.PlayerSpawned` (instancia) | `Action<PlayerId, bool>` | Roster (servidor) | `ReconnectGraceCoordinator` |
| `NetworkSeatPresence.LocalSpectatorChanged` (instancia) | `Action<bool>` | Presence (clientes) | `SpectatorModeController` |
| `OnlineSessionChannel.RejoinRequested` | `Action` | Menú | `OnlineSessionService` |

No hay otros estáticos nuevos. `OnPlayerJoined`/`OnPlayerLeft` se siguen levantando como hoy en el despawn y el respawn.

## 5. Escena y prefabs (scene-builder)

**SB1, NetworkRoot y assets:**
- `Prefabs/Networking/NetworkRoot.prefab`:
  - `UnityTransport.m_DisconnectTimeoutMS` **30000 → 6000**. Hoy supera los 20 s de gracia; si QA ve caídas falsas durante la carga, subirlo a 10000.
  - Agregar `ReconnectTicketMessenger` y cablearlo en `ConnectionApprovalHandler._messenger`, `OnlineSessionService._messenger` y `NetworkRootLifetime` (que lo publica en el anchor para el exit handler).
- `ScriptableObjects/Networking/NetworkSettings.asset`: `ProtocolVersion 3`, `MaxPlayers 3`, `AllowSpectator ✓`, `ShowNameTags ✓`, `NameTagShowsClientId ✓` (en dev).

**SB2, Prototype y Player_Net:**
- `Prefabs/Player/Player_Net.prefab` (variante): hijo `NameTag` a ~2,3 m sobre la raíz, con `TextMeshPro` 3D (LiberationSans, centrado, 2 líneas, tamaño legible a 15 m) + `NameTagView`. En la raíz, `NetworkNameTag` (`_player`, `_view` = NameTag, `_anchor`, `_settings`). **No va en `Player.prefab`**: el modo local no lleva name tags.
- `MatchNetwork` (in-scene): agregar `NetworkSeatPresence`.
- `Modes/OnlineModeRoot/`:
  - `ReconnectGraceCoordinator`: refs a NetworkRuntimeAnchor, NetworkSettings, `Match` (MatchManager), MatchNetwork (NetworkMatchState y NetworkSeatPresence) y NetworkPlayerRoster.
  - `SpectatorModeController`: refs a `SpectatorRig`, su Camera, `UI_SpectatorPanel` y presence.
  - `SpectatorRig` (**inactivo**): `Camera` con vista general del mapa, colocada a mano en la escena, más un `EventSystem` + `InputSystemUIInputModule`. Sin AudioListener si el `AudioListener` raíz de la escena ya está activo (verificarlo para no tener dos).
  - `UI_SpectatorPanel`: Canvas Screen Space Overlay + GraphicRaycaster, con `SpectatorPanelView`, `TXT_Watching` (`online.spectator.watching`) y `BTN_Leave` (OnClick → `Match/MatchExit.ReturnToTitle`). Inactivo.
- `HUD`: prefab nuevo `Prefabs/UI/ConnectionStatusBanner` (panel superior centrado, TMP, oculto) con `ConnectionStatusView`, instanciado una vez en `HUD/` (no por jugador).

**SB3, MainMenu:**
- `UI_OnlinePanel/Panel/Hub/BTN_Rejoin` (`UI_Button_Wood`, debajo de BTN_Join) → `OnlineMenuController.RejoinRoom`, con navegación recableada y `_rejoinButton`/`_rejoinLabel` en `OnlinePanelView`.
- Correr Localization `Setup`.

## 6. Subtareas

| # | Agente | Contenido | Depende de | Verificación y tests EditMode |
|---|---|---|---|---|
| A | GP | Runtime: `PlayerConnectionStatus`, firma de `GameEvents`, `MatchSuspendRules` + `MatchManager.SetSuspended`, `PlayerRig.PlaceAt`, rejoin en `OnlineSessionChannel`, claves de error y `NetworkSettings` | — | Compila. `MatchSuspendRulesTests` (cada fase × completo/incompleto), `MatchStateMachineTests` (suspendido no aborta y al reanudar incompleto aborta a Waiting), `GameEventsNetworkTests` actualizado, `OnlineSessionChannelTests` (oferta, expiración, sin listener) |
| B | GP | `ResumeToken`, `ConnectionPayload` v3, `PlayerSlot.Token`, `ServerSlotDirectory` (token, rotación, `ReleaseSeat`, `TryFindAwaiting`), `ApprovalRules` (`reconnect_denied`), `DisconnectReasons` y mapper (`connection_lost`) | — (paralelo a A) | `ApprovalRulesTests`: token correcto → Reconnect; UGS correcto sin token o con token incorrecto → `reconnect_denied`; token correcto con otro UGS → denegado; reconexión directa solo con token; expirado → asiento libre; `already_connected` intacto. `ServerSlotDirectoryTests` (rotación, IsKnown, ReleaseSeat). `ConnectionPayloadTests` (118 bytes, round-trip, payload v2 → `protocol_mismatch`, límites). `ResumeTokenTests` |
| C | GP | Root: gracia en `ConnectionApprovalHandler` (toggle, expiración, aviso de salida, respaldo por la sesión, eventos), `ReconnectTicketMessenger`, `ReconnectTicket(Rules)`, anchor (ticket y cámara), `OnlineSessionService` (nombre UGS, token, rejoin, `connection_lost`), `RelaySessionConnection.RejoinAsync`/`TryRemovePlayerAsync`, `UgsBootstrapper.PlayerName`, `OnlineMatchExitHandler`, `DevConnectionPanel` | A, B | `ReconnectTicketRulesTests`, `OnlineSessionErrorMapperTests` (motivo vacío → `connection_lost`). MPPM corto por conexión directa: caída simulada → log «seat P2 reserved», a los 20 s «released (TimedOut)» |
| D | GP | Prototype: `SeatPresence(Board)` + `SeatPresenceRules`, `NetworkSeatPresence`, `NetworkMatchState` freeze/resume + `MatchClock`, `ReconnectGraceRules`, `ReconnectGraceCoordinator`, cambios del roster + `CharacterPickRules`, `NetworkPlayer.DisplayNameChanged` | A, B, C | `SeatPresenceSerializationTests` (round-trip y tamaño), `SeatPresenceRulesTests` (cada transición, espectador, sin cambios = 0), `MatchClockTests` (congelado y reanudado), `ReconnectGraceRulesTests`, `CharacterPickRulesTests` |
| E | GP | Vistas: `ConnectionStatusView` + `ConnectionStatusRules`, `NameTagView`, `NetworkNameTag`, `SpectatorPanelView`, `SpectatorModeController`, `CarriableOwnershipLabel` (cámara), `OnlineMenuRules`/`Controller`/`PanelView` (rejoin), localización | A, D | `ConnectionStatusRulesTests`, `OnlineMenuRulesTests` (`ShowsRejoin`) |
| SB1 | SB | NetworkRoot (timeout, messenger) y `NetworkSettings.asset` | C | Sin warnings de `OnValidate` |
| SB2 | SB | Player_Net + NameTag, MatchNetwork, OnlineModeRoot (coordinator, espectador), banner del HUD | D, E, SB1 | Escena guardada; el modo local sigue igual |
| SB3 | SB | MainMenu `BTN_Rejoin` + Localization Setup | E, SB2 (en serie: un SB a la vez) | Escena guardada |
| CR | CR | Revisión de B–E y SB | — | Sin bloqueantes. Revisar: un solo emisor por evento, sin `Find*` ni alloc por frame (banner y tag con `SetText` numérico) |
| QA | QA | EditMode completo, regresión local, gate MPPM (sección 7) | todo | Checklist |

A y B en paralelo; C → D → E en serie (comparten el roster, el anchor y los handlers); los SB en serie.

## 7. Checklist del gate MPPM (host + cliente + 3ª instancia; Relay y luego directo)

**Cómo simular una caída:** `NetworkSimulator.NetworkEventsApi.Disconnect()` en el `NetworkRoot` del clon, con `execute_code` o el botón del DevConnectionPanel. Se espera ~6 s de detección.

1. **Nombres:** el Hub dice «Conectado como Nombre#1234», distinto en cada instancia; los asientos de la sala muestran esos nombres; en Prototype, el tag del rival flota sobre él en host y cliente (el propio no se ve), con «client N»; el espectador ve los dos.
2. **Espectador tardío:** la 3ª instancia se une por código en mitad de Playing. Log de aprobación «spectator». Desde el primer frame ve el score, el timer (±1 s respecto al host), el inventario de los corrales, los obstáculos, los name tags y las posiciones. **Sin avatar** (2 `Player_Net` en la jerarquía de las 3 instancias) y sin input de juego. BTN_Leave lo lleva al Hub. Una 4ª instancia recibe `room_full`.
3. **Caída en Playing:** en ≤ ~6 s, el host y el espectador ven «Esperando a P2 · {personaje}: 20 s» descontando. El timer queda congelado en todas las instancias. P1 queda congelado. Lo que P2 cargaba cae al suelo, sin duplicarse ni destruirse. El cliente queda en el Hub con `connection_lost` y «Reconectar (n s)».
4. **Reconexión dentro de la gracia:** log «Reconnect … client N» con un clientId **nuevo**. P2 reaparece **en su pose**, con el mismo personaje, y su corral y su score intactos. El timer se reanuda **desde el valor congelado** en las 3 instancias, sin saltos. Exactamente **2 `Player_Net`** y 2 entradas en el anchor. `created` del pool sin cambios, 0 «POOL GREW». El banner muestra «P2 volvió». Repetirlo 2 veces seguidas: el token rotado funciona y el viejo ya no sirve.
5. **Sin volver:** a los 20 s aparece «P2 no volvió» y el host y el espectador pasan a `WaitingForPlayers`. El asiento queda libre y el miembro se quita de la sesión. Un cliente nuevo que se une por código toma P2 con un personaje **distinto** al de P1. «Reconectar» ya expiró: se ocultó o da `rejoin_expired`.
6. **Suplantación:** cubierta por EditMode. En MPPM, con `execute_code` se fuerza un payload con el UgsPlayerId del asiento en espera y sin token: `reconnect_denied` y la gracia sigue corriendo.
7. **Salida voluntaria desde Results:** el host ve «P2 salió» **al instante** y pasa a Waiting, sin esperar 20 s.
8. **Caída en Countdown:** el match se suspende y, al reconectar, la cuenta se reinicia.
9. **Caída en Results:** banner y revancha deshabilitada. Al reconectar, el cliente ve la pantalla de resultados y la revancha funciona.
10. **Caída o salida del espectador:** sin suspensión ni banner.
11. **Regresión:** partida local completa; partida online completa con revancha; 0 errores y 0 warnings nuevos en todas las instancias; EditMode en verde.

## 8. Riesgos

- **R1: la reconexión por el SDK.** No está verificado que `ReconnectToSessionAsync` vuelva a arrancar la red (Relay join + `StartClient`) con nuestro handler. Si no lo hace, queda solo `JoinSessionByCodeAsync`. QA lo prueba primero en C. Tampoco está verificado que un miembro que sigue en el lobby pueda volver a unirse por código; si no, se usa `ReconnectToSession` o el host lo quita al expirar.
- **R2: timeout de 6 s.** Un parón de más de 6 s (carga de escena + prewarm, ver 2.5) podría parecer una caída. La gracia lo cubre (vuelve con el ticket) y se puede subir a 10 s.
- **R3: carrera de detección.** Si el cliente reintenta antes de que el servidor detecte la caída, recibe `already_connected`. El botón Reconectar sigue visible y se reintenta.
- **R4: el aviso `AF.Leaving` es best-effort.** El respaldo es `PlayerHasLeft` de la sesión, y después la gracia.
- **R5: HUD durante la gracia.** `OnPlayerLeft` llega al despawn: verificar que el `JoinPromptView` del HUD no muestre «Pulsa para unirte» en online durante la gracia. Si lo hace, `GameModeRouter` lo deshabilita en online.
- **R6: dos AudioListener** si el rig de espectador trae el suyo y el de la raíz ya está activo. SB lo verifica.
- **R7: allocations.** Cero por frame: el banner y el tag usan `SetText(fmt, float)` y las plantillas se arman solo al cambiar el estado. El token se genera una vez por conexión.
- **R8: Plan B #1.** Si el tiempo aprieta se quita el espectador (SpectatorRig y el panel se quedan inactivos, `AllowSpectator` se apaga). La reconexión ya demuestra el ingreso tardío.
