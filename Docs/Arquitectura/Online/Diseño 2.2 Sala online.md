---
tipo: diseño
tarea: 2.2
estado: aprobado
fecha: 2026-11-12
---

# Diseño de la tarea 2.2: sala online en MainMenu

Este diseño baja la fila 2.2 del [[Plan Modo Online]] a clases y cableado concretos. Lo hizo el arquitecto sobre el commit `a9d0c5e`.

## Decisiones (aprobadas por el usuario el 2026-11-12)

- **Elegir un personaje cuenta como listo.** El personaje queda bloqueado y Cancelar lo suelta, igual que en la selección local. `Ready` no viaja por la red: se deriva de `CharacterIndex != NoCharacter`.
- **Estadísticas del título:** partidas, victorias, empates, derrotas, animales anotados y tackles conectados.
- **`TitleStatsView` se arma en 2.2** sobre un `PlayerProgressChannel` que todavía no escribe nadie, así que muestra "—" y un texto guía. En 2.8 `CloudSaveStatsService` pasa a ser el escritor, sin cambios de escena ni de UI.
- **`ConnectionPayload.CharacterIndex`:** desde 2.2 lo ignora `ServerSlotDirectory.TryAssign`, que asigna `NoCharacter`. En 2.3 se quita del wire, se sube `ProtocolVersion` y se eliminan `MatchSession.LocalCharacter` y los `_characters` de `OnlineSessionService`/`DevConnectionPanel`. El host directo del DevConnectionPanel sigue funcionando porque el roster usa como personaje por defecto el índice del asiento.
- **Espectador:** en 2.2 está soportado en la UI y en las reglas. La aprobación sigue en 2 asientos hasta 2.3/2.7, así que la 3ª conexión recibe `room_full`.
- **No hay GameEvents nuevos.** La sala usa canales SO.

## Puente Runtime ↔ Networking

```
Menu (Runtime)                         Data SOs (Runtime)                Networking
OnlineMenuController ──Request*──▶ OnlineSessionChannel ◀──writes── OnlineSessionService (NetworkRoot)
                     ──RequestCharacter/RequestStart──▶ OnlineLobbyChannel ◀──writes── NetworkLobbyState (LobbyState, all peers)
TitleStatsView ◀──reads── PlayerProgressChannel ◀──writes (2.8)── CloudSaveStatsService
```

Networking conoce Data y Core. Menu y UI nunca conocen Networking. Las instancias de los canales nuevos van en `Assets/ScriptableObjects/Networking/OnlineLobbyChannel.asset` y `PlayerProgressChannel.asset`.

## 1. Clases nuevas

### Runtime (`AlienFarms.Runtime`)

| Archivo | Responsabilidad |
|---|---|
| `Core/OnlineLobbyPhase.cs` | enum `Unavailable=0, Open=1, Starting=2` |
| `Core/OnlineLobbySeat.cs` | `readonly struct : IEquatable` con `PlayerId Id`, `int CharacterIndex` (-1 = ninguno), `string DisplayName`, `bool IsLocal`, `bool IsHost` y `HasCharacter` derivado |
| `Core/OnlineSessionErrorKeys.cs` (modificado) | Agrega `online.error.character_taken`, `not_seated`, `cannot_start` y `start_failed` |
| `Core/PlayerProgressStatus.cs` y `Core/PlayerProgressSaveStatus.cs` | `Unavailable, Loading, Loaded, Failed` y `Idle, Saving, Saved, Failed`, un enum por archivo |
| `Core/PlayerStatsSummary.cs` | `readonly struct` con `MatchesPlayed, Wins, Draws, Losses, AnimalsScored, TacklesLanded` |
| `Data/OnlineLobbyChannel.cs` | Canal SO con el mismo patrón que `OnlineSessionChannel`. Ver el detalle abajo |
| `Data/PlayerProgressChannel.cs` | Canal SO. Ver el detalle abajo |
| `Menu/OnlineScreen.cs` | enum `Hub, Join, Room, Busy` |
| `Menu/OnlineMenuRules.cs` | Lógica pura con `static OnlineScreen Resolve(OnlineSessionStatus, OnlineLobbyPhase, bool wantsJoin)` y `static string BusyKey(...)`. SigningIn, Creating, Joining, Leaving y `Starting` llevan a Busy. InSession lleva a Room, con el aviso "Sincronizando…" si el lobby está Unavailable. Ready y SignedOut llevan a Hub, o a Join si `wantsJoin` |
| `Menu/OnlineMenuController.cs` | Controla la fase Online. Ver el detalle abajo |
| `UI/OnlinePanelView.cs` | Vista pura. Ver el detalle abajo |
| `UI/OnlineSeatView.cs` | Se compone sobre `PlayerSlotStatusView`. Campos: `_player`, `_status`, `_name` (TMP), `_hostBadge`, `_youBadge`. Método `Render(in OnlineLobbySeat, bool occupied, CharacterDefinition)` |
| `UI/CharacterCardButton.cs` | `[RequireComponent(Button)]`, `ISelectHandler`, campo `_card` (CharacterCardView), eventos `Clicked`/`Selected(CharacterDefinition)`. Hace un solo `AddListener` en Awake, con method group |
| `UI/TitleStatsView.cs` | Campos `_progress`, `_values` (TMP_Text[]), `_placeholderRoot`, `_savedBadge` y `_emptyHint` (LocalizedString). Redibuja solo cuando llega `Changed` |

#### `OnlineLobbyChannel`

- **Estado** (`[NonSerialized]`): `Phase`, `OnlineLobbySeat[2]` + `TryGetSeat(PlayerId, out seat)`, `LocalPlayerId` (None si es espectador o todavía no tiene asiento), `IsLocalSpectator`, `CanStart` (lo calcula el publicador) y `LastRejectKey`.
- **Eventos:** `Changed`, `CharacterRequested(int)` (-1 = soltar) y `StartRequested`.
- **Pedidos del menú:** `bool RequestCharacter(int)` y `bool RequestStart()`. Sin listener devuelven false y ponen `NotConfigured`.
- **Escritura desde la red:** `SetPhase`, `SetSeats(seats, count, local, spectator, canStart)`, `SetReject(key)` y `ClearReject`. `Changed` se dispara solo si algo cambió.
- **Reset:** contador de generación en `SubsystemRegistration` y `hideFlags |= DontUnloadUnusedAsset`.

#### `PlayerProgressChannel`

- **Estado:** `Status`, `Stats`, `SaveStatus` y `LastErrorKey`.
- **Evento:** `Changed`.
- **Escritura (2.8):** `SetLoading`, `SetLoaded(in PlayerStatsSummary)`, `SetFailed(key)` y `SetSaveStatus`. En 2.2 queda en `Unavailable`.

#### `OnlineMenuController`

- **Referencias** (`[SerializeField]`): `_session` (OnlineSessionChannel), `_lobby` (OnlineLobbyChannel), `_roster` (CharacterRoster) y `_view` (OnlinePanelView).
- **API:**
  - `Enter(PlayerMenuInput owner)` y `Exit()`.
  - `bool HandleCancel()`. Según la pantalla:
    - Join → Hub.
    - Room con personaje → lo suelta.
    - Room sin personaje → foco en Salir.
    - Hub → `ExitRequested`.
    - Busy de Create/Join → `RequestLeave`.
  - Botones: `CreateRoom`, `OpenJoin`, `ConfirmJoin` (lee `_view.CodeText`), `LeaveRoom`, `StartMatch` (`_lobby.RequestStart()`), `Back` y `CancelBusy`.
  - `event Action ExitRequested` y `bool IsLoadingMatch` (true cuando `Phase == Starting`).
- **Suscripciones:** a los `Changed` de los dos canales y a `CardClicked`/`CardSelected` de la vista. Se suscribe en OnEnable y se desuscribe en OnDisable, siempre con method groups.
- **`Enter`** llama `RequestSignIn()`.
- **Redibujo:** `Changed` solo marca `_dirty`. En el `Update` se resuelve la pantalla con `OnlineMenuRules` y se llama `owner.EnterMenu(_view.GetRoot(screen), _view.GetFirstSelected(screen))` en el frame siguiente, nunca dentro de un onClick.
- **Al habilitarse:** si el canal ya está InSession, reconstruye el estado completo desde los canales (cubre R1).

#### `OnlinePanelView`

- **Raíces:** `_panelRoot`, `_hubRoot`, `_joinRoot`, `_roomRoot`, `_busyRoot` y `_errorRoot`.
- **Primer seleccionado por pantalla:** `_hubFirst`, `_joinFirst`, `_roomFirst` y `_busyFirst`.
- **Otras referencias:**
  - Textos TMP: `_signedInAs`, `_code`, `_hint`, `_busyLabel` y `_error`.
  - `_codeInput` (TMP_InputField).
  - `_cards`, `_cardButtons` y `_seats`.
  - `_spectatorRow`, `_spectatorName` y `_spectatorBanner`.
  - `_startButton` y `_startRoot`.
  - LocalizedString: `_errorEntry`, `_busyEntry` y `_signedInEntry`.
- **API:**
  - Navegación: `Show`, `Hide`, `ShowScreen`, `GetRoot`, `GetFirstSelected`.
  - Textos: `SetSignedInAs`, `SetCode`, `ShowError(key|null)`, `SetBusy(key)`.
  - Sala: `RenderCard(c, hoverP1, hoverP2, lockedBy)` (delega en `CharacterCardView.Render`), `RenderSeat`, `SetSpectator(isLocal, name)`, `SetStart(visible, interactable)`, `SetCardsInteractable`.
  - `CodeText` y los eventos `CardClicked`/`CardSelected`.
- **Mensaje de error:** se cambia `TableEntryReference` en runtime y el texto llega por `StringChanged`. Solo hay allocation cuando aparece un error.

### Networking (`AlienFarms.Networking`, `Session/`)

| Archivo | Responsabilidad |
|---|---|
| `LobbySlotEntry.cs` | `struct : INetworkSerializable, IEquatable` (unmanaged) con `PlayerId Id` (byte), `ulong ClientId`, `byte CharacterIndex` (`ConnectionPayload.NoCharacter` = ninguno) y `FixedString32Bytes DisplayName` |
| `LobbyRequestResult.cs` | enum `Accepted, Released, Unchanged, RejectedNotSeated, RejectedOutOfRange, RejectedTaken, RejectedClosed` |
| `LobbyRules.cs` | Lógica pura y estática. Ver las firmas abajo |
| `NetworkLobbyState.cs` | `NetworkBehaviour` del prefab `LobbyState`. Ver el detalle abajo |
| `LobbyStateSpawner.cs` | `MonoBehaviour` que vive en la escena MainMenu y no en el root, para que el host directo del DevPanel no cree un lobby. Ver el detalle abajo |

#### `LobbyRules`

```csharp
static LobbyRequestResult EvaluateRequest(LobbySlotEntry[] entries, int count, ulong senderClientId, int requestedIndex, int rosterCount, bool isOpen)
static bool IsTaken(LobbySlotEntry[] entries, int count, byte index, PlayerId except)
static bool CanStart(LobbySlotEntry[] entries, int count, int requiredSeats)   // 2 asientos, ambos con personaje, distintos
static int Reconcile(LobbySlotEntry[] current, int count, ReadOnlySpan<PlayerSlot> seats, LobbySlotEntry[] output)
static string ToErrorKey(LobbyRequestResult r)
```

- `Reconcile` conserva el personaje si el mismo `ClientId` sigue en el asiento. Si cambió el ClientId o el asiento quedó libre, lo resetea.
- Las reglas trabajan sobre un espejo de 2 entradas en el servidor, no sobre la `NetworkList`.
- Gana el primero que pide: las RPC se procesan en serie, así que la segunda recibe `RejectedTaken`.

#### `NetworkLobbyState`

- **Referencias** (`[SerializeField]`): `_anchor` (NetworkRuntimeAnchor), `_settings`, `_roster` y `_lobbyChannel`.
- **Estado de red:** `NetworkList<LobbySlotEntry> _entries` (se crea en la declaración) y `NetworkVariable<OnlineLobbyPhase> _phase` (la escribe el servidor).
- **Servidor, en `OnNetworkSpawn`:**
  - Arma el estado desde `_anchor.Slots` con `ServerRebuild()`.
  - Escucha `OnClientConnectedCallback` y `OnClientDisconnectCallback` y en los dos casos llama `ServerRebuild()`. Si alguien se desconecta, su personaje queda libre.
  - Escucha `_lobbyChannel.StartRequested`, que lleva a `ServerTryStart()`.
- **RPC:**

```csharp
[Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Everyone)]
void RequestCharacterRpc(byte index, RpcParams p = default)   // sender = p.Receive.SenderClientId; LobbyRules.EvaluateRequest
[Rpc(SendTo.SpecifiedInParams, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Server)]
void CharacterRejectedRpc(byte result, RpcParams p)            // RpcTarget.Single(sender, RpcTargetUse.Temp) -> _lobbyChannel.SetReject(key)
```

- **`ServerTryStart()`:**
  1. Exige `CanStart`.
  2. Llama `slots.TrySetCharacter(id, idx)` para cada entrada.
  3. Pone `_phase = Starting`.
  4. Llama `_anchor.SceneLoader.LoadGameplay()`.
  5. Si la carga falla, vuelve a `Open` y manda `start_failed` al host. Si arranca, hace `NetworkObject.Despawn(true)` y registra el ciclo de vida en el log.
- **En todos los pares** (`OnListChanged`/`OnValueChanged`):
  - Republica el estado en el canal, reutilizando un buffer `OnlineLobbySeat[2]`. La conversión de FixedString a string se hace solo cuando hay cambios.
  - `IsLocal = ClientId == LocalClientId`.
  - El jugador local es espectador si la lista tiene 2 entradas y él no está en ninguna.
- **Pedidos locales:** cada par, host incluido, escucha `_lobbyChannel.CharacterRequested` y llama `RequestCharacterRpc`.
- **`OnNetworkDespawn`:** se desuscribe y publica la fase así: `ShutdownInProgress || !IsListening ? Unavailable : Starting`.

#### `LobbyStateSpawner`

- **Referencias:** `_anchor`, `_session` (OnlineSessionChannel) y `_lobbyPrefab` (NetworkObject).
- **Spawn:** escucha `_session.Changed`. Si la sesión está InSession, es host, `_anchor.IsServer` es true y todavía no hay instancia, hace `Instantiate` y `Spawn(destroyWithScene: true)`.
- **No hace Despawn:** del despawn se encarga el Shutdown de NGO o el propio `NetworkLobbyState` al empezar.

#### Paso de personajes a Prototype

`ServerTryStart` escribe `PlayerSlot.CharacterIndex`, y `NetworkPlayerRoster.TrySpawn` ya lo usa. `NetworkPlayerRoster` y `NetworkPlayer` no cambian.

#### Espectador

- La fila "Espectador: nombre" sale de `OnlineParticipant.IsSpectator`, que viene de Sessions.
- En NGO el espectador no tiene entrada en la lista. Su sala queda de solo lectura: cartas no interactuables, el aviso "Estás mirando", sin botón Empezar y con el foco en Salir.
- Si pide un personaje por RPC, recibe `RejectedNotSeated`.
- Los asientos se toman del lobby NGO y no del orden de Sessions, porque los dos órdenes pueden diferir.

## 2. Cambios a clases existentes

- **`Menu/MenuPhase`:** se agrega `Online = 4`.
- **`Menu/MenuFlowController`:**
  - Agrega la referencia `[SerializeField] OnlineMenuController _online`. `OnValidate` avisa si falta.
  - **`PlayOnline()`:** solo funciona en Title y con `!_isLeaving`. Hace, en orden: `_phase = Online`, `_title.Hide()`, `_lobby.SetJoiningEnabled(false)`, `_online.Enter(owner)` y `RequestApplyAll()`.
  - **Cancel en fase Online:**
    - Si lo pulsa el dueño, va a `_online.HandleCancel()`.
    - Si lo pulsa otro jugador, `RemovePlayer`.
  - **ApplyPlayerState en fase Online:**
    - El foco del dueño lo maneja `OnlineMenuController`.
    - El resto va a `EnterMenu(_select.MenuRoot, null)`.
  - **Si el dueño se va estando en Online:** `_online.Exit()` y después `EnterTitle()`.
  - **`ExitRequested`:** lleva a `EnterTitle()` con `_titleReturnSelection = BTN_PlayOnline` y reactiva el join.
  - **Carga de la partida:** en fase Online no llama nunca a `SceneFlow`, porque la escena la carga NGO. Si `_online.IsLoadingMatch`, pone `_isLeaving = true`.
  - **R1:** `Awake` no hace `Clear()` de `MatchSession` si `OnlineSessionChannel.Status == InSession`.
- **`Networking/Session/ServerSlotDirectory`:** `TryAssign` asigna `NoCharacter` y se agrega `public bool TrySetCharacter(PlayerId, byte)`, que devuelve false si el asiento está vacío.
- **`ConnectionPayload`, `NetworkPlayerRoster` y `NetworkPlayer`:** sin cambios en 2.2.
- **`DevConnectionPanel`:** se agrega la referencia opcional `[SerializeField] OnlineLobbyChannel _lobbyChannel`. Si está asignada, `StartSessionMatch()` llama `_lobbyChannel.RequestStart()`.
- **`NetworkPrefabs.asset`:** se agrega `LobbyState`.
- **`UI/LocalizationKeys` y `Editor/LocalizationSeedData`:** se agregan constantes y textos ES/EN:
  - `title.play_online` y `online.header`.
  - Hub: `online.hub.signed_in_as` ({0}), `online.hub.create`, `online.hub.join`.
  - Unirse: `online.join.prompt`, `online.join.confirm`.
  - Sala: `online.room.code`, `start`, `leave`, `waiting_player`, `waiting_host`, `need_picks`, `spectator`, `syncing`.
  - Insignias: `online.badge.host`, `online.badge.you`.
  - Busy: `online.busy.signing_in`, `creating`, `joining`, `leaving`, `starting`, `cancel`.
  - Estadísticas: `stats.header`, `stats.matches`, `wins`, `draws`, `losses`, `animals`, `tackles`, `stats.empty_hint`, `stats.saved`.
  - Errores: las 16 claves `online.error.*` existentes y las 4 nuevas.
  - Después se corre el menú `Setup` de `LocalizationSetupMenu`.

## 3. Prefabs y escena (scene-builder)

- **`Prefabs/Networking/LobbyState.prefab`:** `NetworkObject` + `NetworkLobbyState`, con referencias a NetworkRuntimeAnchor, NetworkSettings, CharacterRoster_Default y OnlineLobbyChannel. Se registra en `NetworkPrefabs.asset`.
- **`Prefabs/UI/UI_CharacterCard_Online.prefab`:** variante de `UI_CharacterCard` con `Button` (Target Graphic = marco, Navigation Explicit) y `CharacterCardButton`. El candado 🔒 es el `_lockedOverlay` y "listo" es el `_readyStamp`, los dos ya existentes.
- **`Prefabs/UI/UI_OnlinePanel.prefab`:**

```
UI_OnlinePanel            [RectTransform, OnlinePanelView]   (siempre activo)
└─ Panel (UI_Panel_Wood)  -> _panelRoot (inactivo por defecto)
   ├─ TXT_Header (LocalizedText online.header)
   ├─ TXT_SignedInAs                                   -> _signedInAs
   ├─ Hub   -> _hubRoot : BTN_Create (first), BTN_Join, BTN_Back        (UI_Button_Wood)
   ├─ Join  -> _joinRoot: TXT_Prompt, INP_Code (TMP_InputField, límite 6, mayúsculas) (first), BTN_JoinConfirm, BTN_Back
   ├─ Room  -> _roomRoot:
   │   ├─ CodeBlock: TXT_CodeLabel, TXT_Code (grande)  -> _code
   │   ├─ Cards (HorizontalLayout): UI_CharacterCard_Online × roster (first = primera carta)
   │   ├─ Seats: Seat_P1, Seat_P2  [OnlineSeatView] (UI_PlayerSlotStatus + TXT_Name + Badge_Host + Badge_You; _emptyEntry -> online.room.waiting_player)
   │   ├─ SpectatorRow (TXT_Name, badge), SpectatorBanner
   │   ├─ TXT_Hint, BTN_Start (-> _startButton/_startRoot), BTN_Leave
   ├─ Busy  -> _busyRoot (bloquea raycasts): Spinner, TXT_BusyStatus, BTN_CancelBusy (first)
   └─ ErrorBanner -> _errorRoot: TXT_Error
```

- **MainMenu.unity:**
  - **UI:**
    - Instancia de `UI_OnlinePanel` en `--- UI ---/MenuCanvas/`, entre CharacterSelect y LoadingOverlay.
    - `TitleScreen/.../Buttons/BTN_PlayOnline` debajo de BTN_Play, con texto `title.play_online`, OnClick a `MenuFlowController.PlayOnline` y la navegación recableada.
    - `TitleScreen/.../TitleStats` con `TitleStatsView`, que muestra las 6 estadísticas y lee de `PlayerProgressChannel.asset`.
  - **Managers:**
    - `--- Managers ---/OnlineMenu` con `OnlineMenuController`, enlazado a `MenuFlowController._online`.
    - `--- Managers ---/LobbyStateSpawner`.
  - **Cableado:** los OnClick del panel van a los métodos de `OnlineMenuController`, y se asigna `DevTools/UI_DevConnectionPanel._lobbyChannel`.

## 4. Navegación y estados visibles

- **Input:** se usa el mapa UI que ya existe (Navigate/Submit/Cancel) con `PlayerMenuInput` y su `MultiplayerEventSystem`, acotado a `GetRoot(screen)`. No hacen falta acciones nuevas.
- **Jugadores:** en modo online hay un solo jugador local por máquina. Si hay un segundo jugador local, su Cancel lo saca.
- **Sala:**
  - Seleccionar una carta mueve el cursor local, P1 o P2 según el asiento.
  - Submit pide ese personaje.
  - La navegación se salta Start mientras no esté interactuable.
  - Las cartas tomadas por el otro jugador quedan no interactuables (la navegación las salta; el candado se sigue viendo). El foco inicial y el que queda al soltar el personaje va a la primera carta libre, o a Leave si no hay ninguna.
  - Si el foco queda en un Selectable que deja de ser interactuable (Start deshabilitado, carta tomada), vuelve al foco por defecto. El host con personaje espera en Leave hasta que Start se habilita y entonces el foco pasa a Start.
  - Submit en `INP_Code` (Enter, o Submit del mando mientras se edita) confirma el Join.
- **Busy:** muestra una etiqueta según el estado y un botón Cancelar. Cancelar un Join en curso vuelve al Hub (no a la pantalla de código).
- **Errores:** el ErrorBanner muestra `LastErrorKey`/`LastRejectKey` hasta la siguiente acción. El error que queda al volver al Hub (por ejemplo `host_left`) se muestra al entrar. Abrir Join, Crear sala o Volver limpian el error mostrado.
- **Avisos:** "Sincronizando…" si hay sesión pero el lobby todavía no apareció, y "Cargando partida…" con `Starting`. El texto guía muestra "Esperando jugador", "Esperando al anfitrión" o "Elijan personaje".

- **Volver de una partida online:** al terminar una partida online (el cliente cae por `host_left`/`host_disconnected` en `OnlineMatchBootstrap`, host o cliente salen con `OnlineMatchExitHandler.ReturnToTitle`, o el fin de partida), MainMenu abre directamente el **Hub online** (con el error de sesión, si lo hay), no el Title. El lado de red llama `MatchSession.RequestReturnToOnlineMenu()` justo después de `Clear()` y antes de cargar MainMenu; `MenuFlowController.Awake` lo consume (`ConsumeReturnToOnlineMenu`, antes de su `Clear()`) y, si `OnlineMenuRules.OpensOnlineOnLoad(inSession, returnRequested)`, usa el mismo camino que R1 (`_online.Enter(null)`; el primer jugador que se une es el dueño y luego se deshabilita el join). El flag es runtime (generación de `MatchSession`), no se pone en partidas locales ni al salir al Title desde el Hub online.

## 5. Sub-tareas

| # | Agente | Contenido | Depende de | Verificación |
|---|---|---|---|---|
| A | GP | Structs y enums de Core, `OnlineLobbyChannel`, `PlayerProgressChannel`, claves de error y seed de localización, y los cambios en `ServerSlotDirectory` | — | Compila; tests de los canales y de los asientos |
| B | GP | `LobbySlotEntry`, `LobbyRequestResult`, `LobbyRules`, `NetworkLobbyState`, `LobbyStateSpawner` y el cambio en DevConnectionPanel | A | Tests de LobbyRules y de serialización |
| C | GP | `OnlineScreen`, `OnlineMenuRules`, `OnlineMenuController`, `OnlinePanelView`, `OnlineSeatView`, `CharacterCardButton`, `TitleStatsView`, `MenuPhase` y `MenuFlowController` | A | Tests de OnlineMenuRules; la selección local sigue intacta |
| SB1 | SB | Prefab LobbyState, `NetworkPrefabs.asset`, instancias de los SO y Localization Setup | B (los SO solo necesitan A) | Sin referencias faltantes |
| SB2 | SB | Las dos variantes de UI, el botón del título, TitleStats y el cableado de MainMenu | C, SB1 | Escena guardada y sin warnings de `OnValidate` |
| CR | CR | Revisión de B + C + SB | — | Sin bloqueantes |
| QA | QA | EditMode, regresión local y MPPM con host, cliente y 3ª instancia por Relay | todo | Checklist abajo |

B y C se pueden hacer en paralelo. SB2 va en serie, porque es la única tarea que toca la escena.

**Tests EditMode:**
- `LobbyRulesTests` cubre:
  - personaje libre, tomado o repetido (Unchanged);
  - soltar (-1);
  - fuera de rango, incluido 254;
  - sin asiento;
  - fase cerrada;
  - gana el primero que pide;
  - `CanStart` con 0, 1 y 2 asientos;
  - `Reconcile`.
- `LobbySlotEntrySerializationTests`.
- `ServerSlotDirectoryTests`: `TrySetCharacter` y que `TryAssign` ignora el payload.
- `OnlineLobbyChannelTests`.
- `PlayerProgressChannelTests`.
- `OnlineMenuRulesTests`.

**Checklist MPPM:**
- Crear la sala y unirse por código.
- Si los dos piden el mismo personaje casi a la vez, gana uno y el otro ve el candado 🔒 y el aviso.
- Start queda deshabilitado hasta que los dos eligen.
- Prototype carga con el personaje correcto en las dos instancias.
- Soltar el personaje o desconectarse lo libera.
- Si el host sale, el cliente ve `host_left`.
- Un código inválido se rechaza.
- La 3ª instancia recibe `room_full`.
- Todo se puede navegar con mando.

## 6. Riesgos

- **R1:** la sincronización de escena al unirse a MainMenu (`ClientSynchronizationMode.Single`) podría recargar MainMenu en el cliente y limpiar `MatchSession`. Se mitiga reconstruyendo el estado desde los canales y evitando el `Clear()` en `Awake` si la sesión está InSession. QA lo prueba primero.
- **R2:** el orden entre el NV `Starting` y el despawn no está garantizado. Lo cubre la heurística de `OnNetworkDespawn`.
- **R3:** el orden de Sessions puede no coincidir con los asientos de NGO. Se usa el lobby NGO.
- **R4:** con mando no se puede escribir el código y hace falta teclado. Se documenta en el texto guía.
- **R5:** Escape dentro del `TMP_InputField` vuelve al Hub. Es aceptable y QA lo confirma.
- **R6:** no hay allocations por frame, porque todo se redibuja por evento.

## 7. Cableado final para SB2 (según el código implementado)

**`--- Managers ---/MenuFlow` (MenuFlowController), en el header "Online":**
- `_online`: el nuevo `--- Managers ---/OnlineMenu`
- `_onlineSession`: `OnlineSessionChannel.asset`
- `_playOnlineButton`: `BTN_PlayOnline`

**`--- Managers ---/OnlineMenu` (OnlineMenuController):**
- `_session`: `OnlineSessionChannel.asset`
- `_lobby`: `OnlineLobbyChannel.asset`
- `_roster`: el mismo `CharacterRoster_Default` que usa MenuFlowController
- `_view`: `UI_OnlinePanel`

**`--- Managers ---/LobbyStateSpawner` (LobbyStateSpawner):**
- `_anchor`: `NetworkRuntimeAnchor.asset`
- `_session`: `OnlineSessionChannel.asset`
- `_lobbyChannel`: `OnlineLobbyChannel.asset`
- `_lobbyPrefab`: el NetworkObject de `Prefabs/Networking/LobbyState.prefab`

**UI_OnlinePanel (OnlinePanelView):**
- Las entradas localizadas `_errorEntry`, `_busyEntry`, `_signedInEntry` y `_hintEntry` ya traen valores por defecto. No hay que tocarlas.
- Raíces:
  - `_panelRoot` = Panel. No debe contener a la vista y arranca inactivo.
  - `_hubRoot`, `_joinRoot`, `_roomRoot`.
  - `_busyRoot`, que bloquea los raycasts.
  - `_errorRoot` = ErrorBanner.
- Primera selección de cada pantalla:
  - `_hubFirst` = BTN_Create
  - `_joinFirst` = INP_Code
  - `_roomFirst` = la primera carta
  - `_busyFirst` = BTN_CancelBusy
- Botón de salir de la sala: `_roomLeave` = BTN_Leave.
- Textos TMP, sin `LocalizedText`: `_signedInAs`, `_code`, `_hint`, `_busyLabel` y `_error`.
- `_codeInput`: TMP_InputField con límite de 6 caracteres y en mayúsculas.
- Sala:
  - `_cards` (CharacterCardView[]) y `_cardButtons` (CharacterCardButton[]), los dos del mismo largo.
  - `_seats` (OnlineSeatView[2]).
  - `_spectatorRow`, `_spectatorName` y `_spectatorBanner`.
  - `_startButton` y `_startRoot`; este último es opcional.

**OnlineSeatView (Seat_P1 y Seat_P2):**
- `_player`, `_status` (el PlayerSlotStatusView del mismo objeto), `_name`, `_hostBadge` y `_youBadge`.
- En el PlayerSlotStatusView, `_emptyEntry` = `online.room.waiting_player` y `_player` igual al del asiento.

**UI_CharacterCard_Online (variante de UI_CharacterCard):**
- Agrega `Button` y `CharacterCardButton`, con `_card` = su CharacterCardView.
- Navegación Automatic dentro de Room.
- No lleva OnClick; el listener lo registra el propio componente.

**TitleStats (TitleStatsView):**
- `_progress` = `PlayerProgressChannel.asset`.
- `_values[6]` en este orden: partidas, victorias, empates, derrotas, animales y tackles.
- Opcionales: `_placeholderRoot`, `_hint` (TMP sin LocalizedText) y `_savedBadge`.

**OnClick de los botones:**

| Botón | Método |
|---|---|
| BTN_PlayOnline | `MenuFlowController.PlayOnline` |
| BTN_Create | `OnlineMenuController.CreateRoom` |
| BTN_Join (Hub) | `OpenJoin` |
| BTN_JoinConfirm | `ConfirmJoin` |
| BTN_Back (Hub y Join) | `Back` |
| BTN_Leave | `LeaveRoom` |
| BTN_Start | `StartMatch` |
| BTN_CancelBusy | `CancelBusy` |
