# S14 — Voz de proximidad (Vivox) y amigos e invitaciones (Friends): spec

## 0. Versiones y decisión de paquetes
| Paquete | Instalado | Uso | Decisión |
|---|---|---|---|
| `com.unity.services.vivox` | 16.13.0 (Core 5.29) | Canal posicional, controles | Se queda |
| `com.unity.services.friends` | 1.1.1 (las diapositivas citan 1.4) | Relaciones, presencia, `MessageAsync` | **Se queda en 1.1.1.** Toda la API que piden las diapositivas existe (ver el resumen). Wire ya está en 1.5.1. Subir arrastra Auth/Core por debajo de `SessionSdkWorkarounds` (reflexión sobre Multiplayer 2.4.1). En el README: «API equivalente a la 1.4 de las diapositivas». |

- Asmdef `AlienFarms.Networking`: agregar `Unity.Services.Vivox` y `Unity.Services.Friends`.
- Tests: `AlienFarms.Networking.Tests` + `Newtonsoft.Json.dll` en `precompiledReferences`, solo para el round-trip del mensaje de invitación.
- Commitear `ProjectSettings/Packages/com.unity.services.vivox/` (hoy sin trackear).
- Vivox con Authentication usa el token provider de Unity (no hace falta `IVivoxTokenProvider`) y el EnvironmentId de Core, que es "dev" vía `UgsBootstrapper`.

## 1. Clases

### Runtime: `AlienFarms.Core` (`Assets/Scripts/Core/`)
| Archivo | Contenido |
|---|---|
| `VoiceStatus.cs` | enum `Off, Initializing, LoggedIn, Joining, InChannel, Recovering, Error` |
| `VoiceFadeModel.cs` | enum `InverseByDistance=0, LinearByDistance=1, ExponentialByDistance=2` (espejo de Vivox, se mapea en Networking) |
| `VoiceRivalState.cs` | enum `NoVoice, InRange, OutOfRange` |
| `VoiceVolumeRules.cs` | Puro: `ToVivox(float slider01) → int [-50,50]`, `ToSlider(int)`, `Clamp`. 0,5 equivale a 0 |
| `VoiceAttenuationModel.cs` | Puro: `Gain(distance, audible, conversational, fade, model)` en [0,1]. 0 si distance > audible. InverseByDistance = c/(c+f·(d−c)), estilo OpenAL (**confirmar de oído**); `ToDecibels` |
| `FriendAvailability.cs` | enum espejo de `Availability` (`Unknown, Online, Busy, Away, Invisible, Offline`) |
| `FriendsServiceStatus.cs` | enum `Off, Initializing, Ready, Error` |
| `FriendEntry.cs` | readonly struct `{UgsPlayerId, DisplayName, Availability, ActivityKey, LastSeenUnixMs}` |
| `FriendRequestEntry.cs` | readonly struct `{UgsPlayerId, DisplayName, bool Incoming}` |
| `SessionInviteOffer.cs` | readonly struct `{FromUgsPlayerId, FromName, Code, SessionId, ExpiresAtRealtime}` |
| `FriendsErrorKeys.cs` | `friends.error.{self, already_friends, request_exists, blocked, not_friend, not_enabled, network, payload, unknown}`, `friends.info.{invite_sent, request_sent, friend_added}` |
| `OnlineSessionErrorKeys.cs` (modificar) | + `InviteExpired = "online.error.invite_expired"` |
| `GameEvents.cs` (modificar) | Ver la sección 2 |

### Runtime: `AlienFarms.Data` (`Assets/Scripts/Data/`)
| Clase | Responsabilidad / campos |
|---|---|
| `VoiceChatSettings` (SO) | `[Min(1)] _audibleDistance = 30`, `[Min(0)] _conversationalDistance = 1`, `[Min(0)] _fadeIntensity = 1f`, `_fadeModel = InverseByDistance`, `[Range(1,4)] _positionUpdateHz = 3`, `_channelPrefix = "af-"`, `_listenerHeight = 1.6f`, `_enableEchoCancellation = true`, `[Range(0,5)] _maxRecoverAttempts = 3`. `OnValidate`: conversational ≤ audible |
| `VoiceChatChannel` (SO de runtime, con contador de generación como `OnlineSessionChannel`) | Estado: `Status`, `ChannelName`, `MicMuted`, `LocalSpeaking`, `RivalSeat`, `RivalState`, `RivalSpeaking`, `RivalVolume` (int, persiste en la sesión de Play), `RivalLocallyMuted`, `RivalDistance` (float, −1 sin avatar), `LastErrorKey`, `IsAvailable` (hay un servicio escuchando). Peticiones: `RequestToggleMic()`, `RequestSetRivalVolume(int)`, `RequestToggleRivalMute()`. Escritura (solo el servicio): `Set*`. Evento `Changed` |
| `FriendsSettings` (SO) | `_inviteTtlSeconds = 120`, `_inviteCooldownSeconds = 5`, `_maxEntries = 16`, `_fallbackRefreshSeconds = 30` (solo si las notificaciones se caen), `_presenceDebounceSeconds = 1` |
| `FriendsChannel` (SO de runtime) | Estado: `Status`, `LocalPlayerId`, arrays fijos `FriendEntry[_max]` / `FriendRequestEntry[_max]` con contadores, `PendingInvite` (`HasInvite`), `NotificationsConnected`, `LastErrorKey`, `LastInfoKey`. Peticiones: `RequestAddFriend(id)`, `RequestAcceptRequest(id)`, `RequestDeclineRequest(id)`, `RequestCancelOutgoing(id)`, `RequestInvite(friendId)`, `RequestRefresh()`, `ConsumeInvite(out offer)`, `DeclineInvite()`. Sin listener → `friends.error.not_enabled` (patrón `Reject` del canal online). Evento `Changed` |

### Runtime: `AlienFarms.Input` y `AlienFarms.UI` / `AlienFarms.Menu`
| Archivo | Responsabilidad |
|---|---|
| `Input/PlayerVoiceHotkey.cs` | En `Player.prefab`, así lo hereda `Player_Net`. `[SerializeField] PlayerInput _playerInput`, `InputActionReference _toggleMic`, `VoiceChatChannel _voice`. Resuelve la acción por id en `_playerInput.actions`, igual que `PlayerInputHandler.Resolve`. `performed` → `_voice.RequestToggleMic()`. En local no hay listener, así que no hace nada |
| `UI/VoiceHudView.cs` | Indicadores: mic (silenciado / hablando), rival (nombre o asiento, hablando, estado, «12,4 m»). Escucha `GameEvents.OnVoice*` y `VoiceChatChannel.Changed`. La distancia se refresca a 4 Hz con `SetCharArray`, como `PoolStatsView` |
| `UI/VoiceControlsView.cs` | `Toggle`/botón de silencio, `Slider` del volumen del rival (0..1 → `VoiceVolumeRules`) y botón «Silenciar rival». Se oculta si `!IsAvailable`. Se reutiliza en la Sala (MainMenu) y en la pausa online |
| `UI/FriendsPanelView.cs` | Tu Id + Copiar (`GUIUtility.systemCopyBuffer`), `TMP_InputField` para el Id + Agregar, filas pre-colocadas en el prefab (sin Instantiate). Muestra errores e info por clave de localización |
| `UI/FriendEntryView.cs` | Fila: nombre, punto de presencia, actividad y botón Invitar (lo habilita `InviteSendRules`) |
| `UI/FriendRequestEntryView.cs` | Fila: nombre/Id, Aceptar/Rechazar (entrante) o Cancelar (saliente) |
| `UI/InviteToastView.cs` | «X te invita» con Aceptar/Rechazar, cuenta regresiva del TTL y foco con mando (`MultiplayerEventSystem`/`EventSystem` actual) |
| `Menu/FriendsMenuController.cs` | Mediador entre `FriendsChannel`, `OnlineSessionChannel` y `MenuFlowController`. Decide con `InviteAcceptRules`. Secuencia la invitación pendiente: Leave → esperar `Ready` → `RequestJoin(code)`. Desde el título: `MenuFlowController.PlayOnline()` y, ya con Ready, `RequestJoin` |
| `Menu/InviteAcceptRules.cs` | Puro: `Decide(OnlineSessionStatus, bool inMatch, bool hasRejoinOffer, bool expired)` → `JoinNow | LeaveThenJoin | DeclineRejoinThenJoin | Blocked(key)` |
| `Menu/InviteSendRules.cs` | Puro: `CanInvite(isHost, status, participantCount, FriendAvailability, inMatch, lastSentAt, now, cooldown)` |

### Networking: `AlienFarms.Networking.Voice` (`Assets/Scripts/Networking/Voice/`)
| Archivo | Responsabilidad |
|---|---|
| `VoiceChatService.cs` | `[DisallowMultipleComponent]` en `NetworkRoot`. Refs: `VoiceChatSettings`, `VoiceChatChannel`, `OnlineSessionChannel`, `OnlineSessionService` (para `CurrentSession.Id`), `NetworkManager`, `NetworkRuntimeAnchor`. Hace el login de Vivox, el reconciliador de canal, los eventos de Vivox, los bindings de participantes y las peticiones del canal. No toca la posición: la delega en `VoicePositionReporter` |
| `VoicePositionReporter.cs` | Clase C# plana que el servicio posee y llama desde su `LateUpdate`. Usa `VoicePositionThrottle`. Lee `anchor.LocalPlayer.Rig.transform` y `ViewCamera` (forward aplanado). Sin avatar, pose de sala en el origen. Llama `VivoxService.Instance.Set3DPosition(...)` solo si `joinedName != null && Status == InChannel`. Calcula `RivalDistance` con `anchor.TryGetPlayer(rivalSeat)` |
| `VoiceParticipantBinding.cs` | Clase plana por participante: `{VivoxParticipant, PlayerId seat}`. Suscribe `ParticipantSpeechDetected`/`ParticipantMuteStateChanged` con method group y se desuscribe en `Dispose` (sin lambdas que capturen) |
| `VoiceChannelNameRules.cs` | Puro: `Build(prefix, sessionId)`. Filtra al alfabeto de Vivox `[A-Za-z0-9-_.!()+=~]` y recorta a 200 (**confirmar el tope**). null/vacío → null |
| `VoiceDesiredChannelRules.cs` | Puro: `Desired(OnlineSessionStatus, sessionId, prefix)` → nombre solo si `InSession` |
| `VoiceChannelPlanner.cs` | Puro: `Next(desired, joined, busy)` → `VoiceChannelStep {None, Join, Leave, LeaveThenJoin}` |
| `VoicePositionThrottle.cs` | Puro: `(hz)`, `ShouldSend(nowUnscaled)`, `Reset()`. La primera llamada envía |
| `VoiceSeatRules.cs` | Puro: `SeatOf(ugsId, OnlineSessionChannel participants)`: host → P1, otro miembro → P2, desconocido → None |
| `VoiceRivalStateRules.cs` | Puro: `(inChannel, distance, audible)` → `VoiceRivalState` |
| `VoiceFadeModelMapper.cs` | `VoiceFadeModel` → `AudioFadeModel`, y `Channel3DProperties` desde el SO |
| `VoiceErrorMapper.cs` | Excepción → `voice.error.{credentials, login, join, recover_failed}` |

### Networking: `AlienFarms.Networking.Social` (`Assets/Scripts/Networking/Social/`)
| Archivo | Responsabilidad |
|---|---|
| `FriendsSocialService.cs` | En `NetworkRoot`. Refs: `FriendsSettings`, `FriendsChannel`, `OnlineSessionChannel`, `OnlineSessionService`. Inicializa Friends tras el sign-in, publica la lista, la presencia según el estado de la sesión y el envío y la recepción de invitaciones. Nombre elegido para no chocar con `Unity.Services.Friends.FriendsService` |
| `SessionInviteMessage.cs` | `[Serializable] public sealed class`, constructor vacío, campos públicos: `type = "af.invite"`, `version = 1`, `code`, `sessionId`, `hostName`, `sentAtUnixMs`. Pesa menos de 1 KB, muy por debajo del tope de 10 KB |
| `FriendPresenceActivity.cs` | `[Serializable]` con constructor vacío: `{ string state; }` → `"hub" / "room" / "match"` |
| `InviteRules.cs` | Puro: `Validate(msg, localId, senderId, nowUnixMs, ttlMs)` → `Valid | Malformed | WrongVersion | FromSelf | Expired`. Normaliza con `SessionCodeFormat.TryNormalize` |
| `FriendPresenceRules.cs` | Puro: `(OnlineSessionStatus, inMatch)` → `(FriendAvailability, activityState)`. Sala o Hub = Online; partida = Busy |
| `FriendListBuilder.cs` | Puro: llena el buffer fijo desde DTOs (no recibe `Relationship` del SDK, el servicio adapta). Orden: Online, Busy, Away, resto; luego por nombre. Sin LINQ |
| `FriendsErrorMapper.cs` | Puro sobre `int` (códigos de `FriendsErrorCode`): 24008 → self, 24012/24003 → already/request_exists, 24011 → blocked, 24013 → not_friend, 24024 → not_enabled, 24022 → payload, 24602 → network |

## 2. Eventos estáticos (en `GameEvents`, Core)
| Firma | Emite | Escucha |
|---|---|---|
| `static event Action<PlayerId, bool> OnVoiceParticipantChanged` (asiento, en el canal) | `VoiceChatService` en `ParticipantAddedToChannel/Removed` (incluye al propio jugador) | `VoiceHudView`; opcional, `NameTagView` |
| `static event Action<PlayerId, bool> OnVoiceSpeakingChanged` | `VoiceParticipantBinding` → servicio (`SpeechDetected`) | `VoiceHudView`; opcional, ícono en el name tag |
| `static event Action<PlayerId, bool> OnVoiceMuteChanged` (mic propio o silencio local del rival) | `VoiceChatService` | `VoiceHudView`, `VoiceControlsView` |

- Se agregan los `Raise*` y el reset en el método `SubsystemRegistration` que ya existe.
- Solo se emiten en el hilo principal. Los callbacks de Vivox y de Wire ya llegan ahí (**confirmar con un log de `Thread.CurrentThread.ManagedThreadId` en QA**).
- Lo social va por `FriendsChannel.Changed` (un canal de eventos SO): no es estado por asiento y en el menú no hay `PlayerId`.

## 3. Ciclo de vida

### Voz
1. **Login.** `OnlineSessionChannel.Changed` con `Status ≥ Ready` y servicio en `Off` → `VivoxService.Instance.InitializeAsync()` → `LoginAsync(new LoginOptions{ DisplayName = channel.LocalPlayerName })`. El PlayerId sale de Auth y es el mismo de Sessions y Friends. Si `_enableEchoCancellation`, se llama `EnableAcousticEchoCancellation()`. Se reaplica `MicMuted` del canal. Si ya estaba logueado (un root recreado o Play sin domain reload), se reutiliza.
2. **Primer arranque de la sesión de Play.** Si `ActiveChannels` tiene canales y no hay nada deseado → `LeaveAllChannelsAsync()`, para limpiar restos de un Play anterior sin domain reload.
3. **Reconciliador.** `Reconcile()` se dispara en: `Changed` del canal online, `ChannelJoined/Left`, `ConnectionRecovered`, fin de una operación. Un solo `Task` en vuelo a la vez (`_busy`):
   - `desired = VoiceDesiredChannelRules.Desired(status, service.CurrentSession?.Id, prefix)`
   - `Join` → `JoinPositionalChannelAsync(name, ChatCapability.AudioOnly, props)`, con `Status = Joining`.
   - `Leave` → `LeaveChannelAsync(joined)`.
   - `LeaveThenJoin`: si cambió la sesión.
   - Al terminar, `Reconcile()` otra vez (el deseado pudo cambiar mientras tanto).
4. **Entrar.** Pasa en `InSession`, o sea ya en la Sala del MainMenu. El canal es el mismo en la sala y en la partida (lo pide la diapositiva 10). En la sala no hay avatar, así que el reporter manda el origen para todos y se oyen a volumen pleno: es chat de sala. **QA debe confirmar si Vivox necesita una primera `Set3DPosition` para tener audio**; el reporter la manda apenas llega `ChannelJoined`.
5. **Partida.** `VoicePositionReporter` manda la pose a 3 Hz con `Time.unscaledTime`; la pausa online no congela nada.
6. **Salir.** Salida explícita del canal en `Leaving`/`Ready`/`SignedOut`, en `NetworkManager.OnClientStopped` (como la diapositiva) y en `OnDisable` del servicio (fire-and-forget, como `OnlineSessionService.OnDisable`). La voz no sabe de NGO. Logout solo en `OnApplicationQuit`. Auth `SignedOut/Expired` → Vivox desloguea solo (`OnAuthenticationLost`) → `Status = Off`.
7. **Vivox pierde la conexión.** `ConnectionRecovering` → `Recovering`. `Recovered` → `Reconcile`. `FailedToRecover` → re-login con backoff, hasta `_maxRecoverAttempts` → `Error(voice.error.recover_failed)`.

### Interacción con gracia y reconexión (2.7/2.8; el espectador ya no existe)
| Caso | Cliente | Host |
|---|---|---|
| Al cliente se le cae la red en Playing | El root se destruye (`OnlineMatchBootstrap`) → `OnClientStopped`/`OnDisable` → `LeaveChannelAsync`. Si el proceso muere, Vivox expira al participante solo, en decenas de segundos | `ParticipantRemoved` → `OnVoiceParticipantChanged(P2,false)` → HUD «P2 sin voz», junto al banner «Esperando a P2: 20 s» que ya existe |
| El cliente reconecta (`RejoinAsync`, mismo `session.Id`) | InSession → mismo nombre de canal → Join | `ParticipantAdded` → se reaplican `RivalVolume` y `RivalLocallyMuted` guardados por asiento (no en el objeto participante, que es nuevo) |
| TimedOut o abandono | Vuelve al Hub (Ready) → fuera del canal | Sigue InSession y solo en el canal |
| El host sale (`HostShutdownSequence`) | `host_left` → Ready → Leave | Leaving → Leave |
| Revancha | Sin cambios | Sin cambios |
| Fin de partida → Hub online | La sesión termina → Leave | Leave |
| Cruce de los 30 m | `ParticipantRemoved/Added` del rival, porque Vivox filtra por distancia. `VoiceRivalStateRules` lo distingue de una desconexión usando la distancia del avatar → `OutOfRange` | Igual |

### Friends
1. **Inicio.** Con el primer `Status ≥ Ready`: `FriendsService.Instance.InitializeAsync(new InitializeOptions().WithEvents(true).WithMemberPresence(true).WithMemberProfile(true))`. Es idempotente en el SDK. Se suscriben `RelationshipAdded/Deleted`, `PresenceUpdated`, `MessageReceived` y `NotificationsConnectivityChanged`.
2. **Publicar.** Se adaptan `Friends`, `IncomingFriendRequests` y `OutgoingFriendRequests` a DTOs → `FriendListBuilder` → `FriendsChannel`. En cada evento se republica a partir de las listas del SDK, que ya se actualizan solas.
3. **Presencia.** `SetPresenceAsync(availability, new FriendPresenceActivity{state})` en cada cambio que importe (`FriendPresenceRules`), con un debounce de 1 s. Al iniciar, Online. En `OnApplicationQuit`, `SetPresenceAvailabilityAsync(Offline)` sin esperar. El servicio no guarda la presencia solo (diapositiva 19): si el amigo nunca la fija, se ve Unknown.
4. **Notificaciones caídas.** Si `NotificationsConnected == false`, `ForceRelationshipsRefreshAsync` cada 30 s, solo con el panel abierto.
5. **Root recreado.** El SDK sigue inicializado: se resuscriben los eventos y se republica. `OnDisable` desuscribe todo.

### Invitación
1. **Host en la Sala** (`IsHost`, InSession, `ParticipantCount < 2`, fuera de partida) → Invitar a un amigo Online → `FriendsChannel.RequestInvite(id)`. El servicio hace `MessageAsync(id, new SessionInviteMessage{code = channel.Code, sessionId = CurrentSession.Id, hostName, sentAtUnixMs})` y responde con `friends.info.invite_sent` o el error que corresponda. Hay 5 s de espera por amigo.
2. **Invitado.** `MessageReceived` → `GetAs<SessionInviteMessage>()` dentro de try/catch → `InviteRules.Validate` → `FriendsChannel.SetPendingInvite(offer)`. Se guarda una sola invitación: la nueva reemplaza a la vieja.
3. **Aviso** (`InviteToastView`, en la raíz del Canvas del MainMenu). Aceptar → `FriendsMenuController` → `InviteAcceptRules`:
   - `Ready` → `OnlineSessionChannel.RequestJoin(code)`.
   - En otra Sala → `RequestLeave()`, guarda el código y, al llegar `Ready`, `RequestJoin`. Las estadísticas pendientes de Cloud Save (S9/2.8) no se pierden porque van en PlayerPrefs.
   - Con oferta de reconexión → `RequestDeclineRejoin()` y después join.
   - Desde el título → `PlayOnline()` → sign-in → join.
   - En partida → no llega: la UI vive solo en el MainMenu y la presencia es Busy.
4. **Errores.** `JoinSessionByCodeAsync` falla → `OnlineSessionService` ya pone `session_not_found`/`session_full`/`invalid_code` en el Hub. Si venció el TTL local → `online.error.invite_expired`.
5. Opcional: el invitado comprueba, ya en la sesión, que `CurrentSession.Id == offer.SessionId`.

## 4. ScriptableObjects (instancias en `Assets/ScriptableObjects/Networking/`)
- `VoiceChatSettings.asset`: 30 / 1 / 1,0 / InverseByDistance / 3 Hz / "af-" / 1,6 / AEC encendido / 3.
- `VoiceChatChannel.asset`, `FriendsSettings.asset` (120 / 5 / 16 / 30 / 1) y `FriendsChannel.asset`.
- Los canales son de runtime: `hideFlags |= DontUnloadUnusedAsset` y contador de generación, como `OnlineSessionChannel`.

## 5. Escena y prefabs (scene-builder)
1. **`Prefabs/Networking/NetworkRoot.prefab`:**
   - + `VoiceChatService` y + `FriendsSocialService`, en el mismo GameObject que `OnlineSessionService`.
   - Cablear: los SO de arriba, `OnlineSessionChannel.asset`, `OnlineSessionService`, `NetworkManager` y `NetworkRuntimeAnchor.asset`.
2. **`Prefabs/Player/Player.prefab`** (lo hereda `Player_Net`): + `PlayerVoiceHotkey`, con `_playerInput`, la referencia a `Player/ToggleMic` y `VoiceChatChannel.asset`.
3. **`Prefabs/UI/UI_VoiceControls.prefab`** (nuevo): toggle de micrófono, slider con `UI_Slider_Wood`, botón «Silenciar rival» y `VoiceControlsView`. Va en:
   - (a) `UI_OnlinePanel` → pantalla Sala;
   - (b) el panel de pausa de `Prototype`, visible solo si `IsAvailable` (en local se oculta solo). Navegación con mando: Selectables con Navigation explícita.
4. **`Prefabs/UI/UI_VoiceHud.prefab`** (nuevo): en `Prototype` → `OnlineModeRoot`, dentro del Canvas online que ya existe (junto a `ConnectionStatusBanner`), arriba a la izquierda. Íconos de mic y rival, texto de distancia y `VoiceHudView`.
5. **`Prefabs/UI/UI_FriendsPanel.prefab`** (nuevo): dentro de `UI_OnlinePanel`, visible en Hub y Sala. Lleva:
   - tu Id con botón Copiar;
   - un InputField para el Id con botón Agregar;
   - una sección «Solicitudes» con 4 filas pre-colocadas (`FriendRequestEntryView`);
   - una sección «Amigos» con 8 filas en un ScrollRect (`FriendEntryView`);
   - línea de estado y error, y `FriendsPanelView`.
   
   Más filas solo si `_maxEntries` lo pide: se crean en el editor, nunca en runtime.
6. **`Prefabs/UI/UI_InviteToast.prefab`** (nuevo): en la raíz del Canvas del MainMenu, encima de todo, inactivo hasta que haya invitación. `InviteToastView`.
7. **`MainMenu`:** objeto `FriendsMenuController` junto a `OnlineMenuController`. Refs: `FriendsChannel`, `OnlineSessionChannel`, `MenuFlowController`, `FriendsSettings` y las vistas.
8. **Localización** (tabla `Assets/Settings/Localization/Tables/UI`): claves `voice.*`, `friends.*`, `online.error.invite_expired` y las etiquetas de presencia y actividad.
9. Guardar MainMenu y Prototype.

## 6. Input
- En `Assets/Input/AlienFarmsControls.inputactions`, mapa `Player`: acción `ToggleMic` (Button), con `<Keyboard>/m` y `<Gamepad>/dpad/up`. El mapa Player no usa el D-pad.
- La lee el `PlayerInput` del owner, que es el único habilitado en online. El volumen se cambia con el slider: ratón, o navegación UI en la pausa.
- El Id de amigo se escribe o pega solo con teclado (riesgo conocido, igual que el código de sala).

## 7. Tests EditMode
**`AlienFarms.Networking.Tests` → `Voice/`, `Social/`**
- `VoiceChannelNameRulesTests`: Id válido → `af-<id>`; caracteres prohibidos filtrados; tope de largo; null/vacío → null; determinista (el mismo Id da el mismo nombre en host y cliente).
- `VoiceDesiredChannelRulesTests`: solo InSession da nombre; Joining/Leaving/Ready → null.
- `VoiceChannelPlannerTests`: las 4 combinaciones (deseado/unido iguales, null, distinto); `busy` → None.
- `VoicePositionThrottleTests`: 3 Hz → 3 envíos en 1 s simulado; el primero inmediato; `Reset`; hz límite.
- `VoiceSeatRulesTests`: host → P1, miembro → P2, Id desconocido o null → None.
- `VoiceRivalStateRulesTests`: fuera del canal → NoVoice; en el canal y d ≤ 30 → InRange; d > 30 → OutOfRange; sin avatar → InRange si está en el canal.
- `VoiceFadeModelMapperTests`: los 3 modelos; propiedades desde el SO (30, 1, 1, Inverse).
- `InviteRulesTests`: válido; tipo o versión ajenos; código malformado; de uno mismo; vencido en el borde del TTL.
- `SessionInviteMessageTests`: round-trip con Newtonsoft y tamaño serializado menor a 10 240 B.
- `FriendPresenceRulesTests` y `FriendListBuilderTests`: orden por disponibilidad y luego nombre; recorte a `_maxEntries`; sin entradas nulas.
- `FriendsErrorMapperTests`: cada código → su clave; desconocido → unknown.

**`AlienFarms.Tests` (Runtime)**
- `VoiceVolumeRulesTests`: clamp, extremos, round-trip, 0,5 ↔ 0.
- `VoiceAttenuationModelTests`: tabla de referencia con c = 1, f = 1, Inverse:
  - 1 m → 1,0;
  - 5 m → 0,2 (−14,0 dB);
  - 15 m → 0,0667 (−23,5 dB);
  - 30 m → 0,0333;
  - 35 m → 0 (fuera).
  
  También Linear y Exponential monótonos.
- `InviteAcceptRulesTests` y `InviteSendRulesTests`: todos los estados, espera entre envíos, sala llena, no host, amigo Offline.
- `VoiceChatChannelTests` y `FriendsChannelTests`: generación descarta el estado viejo; peticiones sin listener → `not_enabled` / `IsAvailable` false; buffers fijos.
- `GameEventsResetTests`: los 3 eventos nuevos se limpian en el reset.

## 8. Riesgos
| Riesgo | Mitigación |
|---|---|
| Eco y realimentación en MPPM (las dos instancias comparten mic y salida en una PC) | Auriculares + AEC. «Modo medición» de desarrollo (un toggle en `VoiceChatService` o `DebugViewSwitch`, tecla F6): host con salida muda (`MuteOutputDevice`), clon con mic mudo. El video, con la build de 2 PCs (2.9). El README avisa si hubo eco (lo pide la rúbrica) |
| `Set3DPosition` sin login encola la acción y hace un login implícito; sin canal, NRE; usa LINQ | Solo se llama con `Status == InChannel` y el nombre unido. Son ~3 allocs/s, aceptable y documentado |
| Vivox y Friends son estáticos que sobreviven a Play sin domain reload | Limpiar canales viejos al arrancar; `IsLoggedIn` reutilizado; Friends ya inicializado → solo resuscribir. QA: dos Play seguidos sin errores |
| Carreras Join/Leave (reconexión rápida, rejoin) | Reconciliador con una sola operación en vuelo que reevalúa al terminar |
| Credenciales: si Vivox no está enlazado al entorno "dev", ninguna llamada llega | Commitear `ProjectSettings/Packages/com.unity.services.vivox/`; `VoiceErrorMapper` → `voice.error.credentials` visible en el HUD |
| Posición reportada por el cliente (S12: un cliente modificado se oye en todo el mapa) | Riesgo aceptado en 1v1. Va como respuesta en el README |
| Mismo PlayerId en dos instancias | `UgsProfileResolver` ya separa perfiles por instancia MPPM. Las amistades persisten mientras dure el token del perfil; anotar los Id en el README |
| Presencia Unknown si el amigo nunca la fija | Se fija Online al iniciar; Offline al salir (best-effort); se muestra `LastSeen` |
| Límites de frecuencia de Friends | Debounce de presencia, 5 s entre invitaciones, refresco solo como respaldo |
| `FriendsService.Instance` lanza excepción sin Core o sin sign-in | Solo se accede después de `Status ≥ Ready`, dentro de try/catch |
| Invitaciones solo con el MainMenu cargado y después de entrar una vez al online | Documentado. Con `_signInOnStart` en el root, el sign-in se haría ya en el título (decisión del usuario) |
| Canal posicional: puede haber uno solo | Una sola partida a la vez; el planner hace `LeaveThenJoin` |
| No existe `README.md` en la raíz | D1 lo crea, con «Semana 13» si todavía no está |

## 9. Reparto
- **Codex (código):** todo lo de las secciones 1, 2, 6 (JSON del `.inputactions`) y 7, más las referencias de asmdef.
- **Scene-builder:** la sección 5, incluida la tabla de localización.
- **QA:** gate de voz (login con Ids distintos, mismo canal, `ChannelJoined` en la Sala, 3 Hz verificado por log, mute y volumen, caída y vuelta con preferencias reaplicadas, `OutOfRange` a 35 m, dos Play seguidos) y gate de amigos (solicitud por Id, aceptar, presencia Online/Busy, invitar → aviso → aceptar → mismo personaje y sala, código vencido, sala llena).

## 10. Commits (bloques chicos)
| # | Commit | Agente |
|---|---|---|
| V0 | `S14: referencias de Vivox y Friends en asmdef + ProjectSettings de Vivox` | Codex + Orq |
| V1 | `S14: VoiceChatSettings, VoiceChatChannel y eventos de voz en GameEvents (+tests)` | Codex |
| V2 | `S14: reglas puras de voz (nombre de canal, planner, throttle, asiento, atenuación) (+tests)` | Codex |
| V3 | `S14: VoiceChatService: login, canal posicional por sesión y salida en OnClientStopped` | Codex |
| V4 | `S14: Set3DPosition del avatar local a 3 Hz y distancia al rival` | Codex |
| V5 | `S14: silencio de mic, volumen y silencio local del rival, acción ToggleMic` | Codex |
| V6 | `S14: montaje de voz (NetworkRoot, HUD online, controles en Sala y pausa)` | SB |
| V7 | `S14: gate de voz + Docs/Arquitectura/Online/Medición S14 Voz.md (tabla 5/15/35 m)` | QA |
| F1 | `S14: FriendsSettings, FriendsChannel y claves de amigos (+tests)` | Codex |
| F2 | `S14: SessionInviteMessage y reglas de invitación y presencia (+tests)` | Codex |
| F3 | `S14: FriendsSocialService: lista, presencia y solicitudes por Id` | Codex |
| F4 | `S14: invitación por MessageAsync y aceptar → RequestJoin (FriendsMenuController)` | Codex |
| F5 | `S14: montaje de amigos (UI_FriendsPanel, UI_InviteToast, localización)` | SB |
| F6 | `S14: gate de amigos e invitación en MPPM` | QA |
| D1 | `S14: README «Semana 14» y guion del video` | Orq |
| O1 (opc.) | `S14: los bloqueos de Friends se aplican en Vivox (BlockPlayerAsync)` (pregunta de la diapositiva 21) | Codex |
| O2 (opc.) | `S14: inyección de audio de desarrollo para la medición` (`StartAudioInjection`; **confirmar el formato del WAV**) | Codex |

Revisión de código (CR) después de V5 y de F4.

### README «Semana 14» (contenido)
- Versiones: Vivox 16.13.0; Friends 1.1.1 y por qué no 1.4.
- `Channel3DProperties(30, 1, 1.0, InverseByDistance)`, con justificación:
  - 30 = el mismo radio de entrada de la AOI de la S13: lo que ves es lo que oyes;
  - 1 = media altura del avatar;
  - Inverse = la caída más natural;
  - la arena mide 80×30 m, así que de corral a corral no se oye.
- Tabla a 5, 15 y 35 m, con estas columnas:
  - distancia medida en el HUD;
  - ganancia teórica en dB;
  - ¿se oye?;
  - inteligibilidad (1–5);
  - `ParticipantAdded/Removed`.
- Controles disponibles.
- Flujo de amigos e invitación.
- Qué no se pudo probar (p. ej. invitación con el invitado en el título sin sign-in previo, más de 2 PCs, eco).
- Respuestas a las preguntas de las diapositivas 9, 12, 15, 18, 21 y 24.
- Lista de commits.

### Guion del video (3–5 min)
1. **0:00–0:30.** Intro de cada integrante; qué agrega la S14.
2. **0:30–1:30.** Amigos: copiar el Id en P2 → agregarlo desde P1 → P2 acepta la solicitud → los dos aparecen Online.
3. **1:30–2:15.** P1 crea la sala → Invitar a P2 → aviso → Aceptar → los dos en la Sala, sin dictar el código. Mostrar también un código vencido.
4. **2:15–3:30.** Partida: los dos en el centro (5 m) hablando → P2 se aleja a 15 m (la voz baja; el HUD marca la distancia) → 35 m (silencio, «Fuera de alcance») → vuelve. Silenciar el mic y bajar el volumen del rival.
5. **3:30–4:15.** Caída y reconexión de P2: «P2 sin voz» → vuelve → se oye otra vez con su volumen.
6. **4:15–5:00.** Explicación de la arquitectura (canal = Id de la sesión, 3 Hz, PlayerId compartido), la tabla y aviso de eco si lo hubo.

Archivos de referencia:
- `D:/UnityProjects/Alien Farms/Assets/Scripts/Data/OnlineSessionChannel.cs`
- `D:/UnityProjects/Alien Farms/Assets/Scripts/Networking/Session/OnlineSessionService.cs`
- `D:/UnityProjects/Alien Farms/Assets/Scripts/Networking/Data/NetworkRuntimeAnchor.cs`
- `D:/UnityProjects/Alien Farms/Library/PackageCache/com.unity.services.vivox@ef0dde9c9dce/Runtime/IVivoxService.cs`
- `D:/UnityProjects/Alien Farms/Library/PackageCache/com.unity.services.friends@1e7c5a43f152/Runtime/SDK/IFriendsService.cs`