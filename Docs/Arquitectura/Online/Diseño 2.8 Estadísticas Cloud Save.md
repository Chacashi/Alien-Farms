---
tipo: diseño
tarea: 2.8
estado: aprobado
fecha: 2026-11-12
---

# Diseño 2.8: estadísticas persistentes con Cloud Save

Baja la fila 2.8 del [[Plan Modo Online]] a clases y cableado concretos, sobre `c2512ae`. Verificación: **«Las estadísticas persisten»** (checklist §7). Cubre el tema 9 del curso («el título muestra las estadísticas y al final "Progreso guardado"») y el tema 1 («el score se pierde al terminar; las estadísticas de Cloud Save sobreviven al reinicio»).

## 0. Decisiones (aprobadas el 2026-11-13, con los cambios del usuario en 3 y 7)

1. **Estadísticas guardadas:** las 6 que ya muestra `TitleStatsView` (2.2): partidas, victorias, empates, derrotas, animales anotados y tackles conectados. «Animales anotados» = los animales en el corral propio al terminar la ronda (suma de `PenInventorySnapshot`), no los puntos. Los tackles quedan en 0 hasta la 3.2 (§3.3).
2. **Cada máquina guarda solo las suyas al terminar cada ronda** (`OnMatchEnded`), como dice la matriz de autoridad («Cada cliente (las propias) · Cloud Save Player Data»). El host guarda las suyas como P1. El servidor no escribe las de otros: los Player Data solo los escribe el token de ese jugador. Hacerlas autoritativas exigiría Cloud Code (fuera del alcance). Punto de discusión del curso: **persistencia ≠ autoridad**.
3. **Solo cuenta una ronda vista en vivo:** se registra una ronda solo si esta instancia de Prototype vio la fase Playing. Una reconexión que entra en Results no vuelve a contar (`NetworkMatchState.PumpClient` re-levanta `OnMatchEnded`). Los espectadores nunca guardan.
   - **Abandono = victoria del que se queda** (cambio del usuario). Solo en online y solo en **Playing**. Si el asiento del rival se libera (`Left` o `TimedOut`, ya pasada la gracia de la 2.7), la ronda termina en Results con `MatchResult.EndReason = Forfeit` y `Winner` = el que se quedó, sin importar el marcador. Cuenta como partida y victoria. Los animales se cuentan con el corral de ese momento. Si el rival se va en Countdown, la ronda se aborta como hoy y no cuenta. En local no cambia nada.
   - **El que abandona a propósito** (Salir desde la pausa en Playing, sea host o cliente) registra una **derrota** antes de cerrar la sesión. Esa derrota queda en PlayerPrefs (decisión 7) y se sube en la próxima carga.
   - **Si se cae el host** (`host_left` en Playing), el cliente que queda registra una **victoria** y el menú lo avisa («El anfitrión abandonó: victoria registrada»).
   - **Si alguien se cae sin volver** (`TimedOut`), no registra nada: su máquina no conoce el resultado. Es una limitación aceptada; arreglarla exigiría Cloud Code.
4. **Las partidas locales no cuentan:** dos jugadores comparten una cuenta UGS y no hay a quién atribuirlas.
5. **Clase de acceso Public** en una única clave `stats`: solo la escribe su dueño, la lee cualquiera con su PlayerId. Permite ver las estadísticas del rival en la Sala. **Plan B #2:** `NetworkSettings.ReadRivalStats = false` y `RivalStatsView` desactivado; la escritura sigue siendo Public (una sola ruta).
6. **El título muestra totales optimistas:** lo confirmado más lo pendiente. El aviso de guardado dice si ya está en la nube.
7. **Lo pendiente se guarda en PlayerPrefs** (cambio del usuario). Sobrevive al cierre del juego y se borra solo cuando la nube confirma el guardado. Clave `stats.pending.{ugsId}`, una por cuenta. Se escribe con `PlayerPrefs.Save()` en cada `TryEnqueue` (y al enviar), para que la derrota del que abandona sobreviva al cierre. **Dos segmentos** (revisión de código, JSON v2: `{ "v":2, "batch", 6 estadísticas, "sent", "tail":{ "batch", 6 estadísticas } }`; un v1 se lee como head no enviado):
   - **head** `{delta, batch}`: lo que lleva la próxima escritura. **Se congela al enviarse por primera vez** (`sent`): un reintento reenvía exactamente el mismo contenido con el mismo `batch`, así un `batch` nunca nombra dos contenidos distintos.
   - **tail** `{delta, batch}`: lo que se encola después de enviar el head, aunque ese envío haya fallado.
   - Al recargar o resolver un conflicto: `batch remoto == head.batch` → el head ya se aplicó: se descarta y el tail pasa a head (no enviado). `== tail.batch` → se descartan ambos. Otro → no se aplicó: el head vuelve a no enviado y absorbe el tail (rebase y reenvío).
   - Así no se pierde un delta encolado tras un envío cuya respuesta se perdió, ni al cerrar el juego o desmontar el root con un guardado en vuelo.

## 1. Esquema y reglas de persistencia

Clave `stats` (Player Data, Public), objeto JSON serializado por Newtonsoft dentro del SDK:

    { "v": 1, "matches": 12, "wins": 7, "draws": 1, "losses": 4, "animals": 83, "tackles": 0, "batch": "9f2c…" }

- `v`: versión del esquema (`PlayerStatsCodec.CurrentVersion = 1`). Sin `v` = v1. Una versión **mayor** que la del código pone el ledger en **ReadOnly**: se muestra lo que hay y **nunca se sobrescribe**.
- `batch`: id del último lote aplicado (el `batch` del head enviado), para la idempotencia ante conflictos.
- **Siempre se escriben totales absolutos** (confirmado + head), nunca incrementos: reintentar el mismo lote da el mismo resultado. Implementación: la escritura lleva solo el head (no head+tail): si el tail viajara con el `batch` del tail, un delta encolado mientras ese envío está en vuelo volvería a quedar detrás de un `batch` posiblemente aplicado. El tail sube a head al confirmarse el head y se envía en la escritura siguiente.
- **`WriteLock` (concurrencia optimista):** la primera escritura (clave ausente) va con lock `null`. Ante `CloudSaveConflictException` se recarga y se resuelve con los segmentos (decisión 7): `batch` remoto = head → ya se aplicó (la respuesta se perdió) y es éxito; = tail → ambos aplicados; otro → otro dispositivo escribió antes → rebase sobre lo recargado y reintento.
- **Nunca se guarda sin haber cargado antes:** sumar lo pendiente sobre cero pisaría la nube.
- **Pendiente local (PlayerPrefs):** `Reset(ownerId)` lo lee desde `IPendingStatsStore`. Cada `TryEnqueue` lo agrega (al head si no se envió, si no al tail) y lo persiste; `TryBeginSave` persiste el head como enviado. `CompleteSave` descarta el head y sube el tail. `ApplyLoaded` y `ResolveConflict` resuelven los segmentos contra el `batch` remoto. Un JSON inválido o de versión mayor se ignora con un warning y se borra.
- **ReadOnly** (versión mayor o corrupto): se muestra solo lo confirmado (lo pendiente nunca se guardará) con su clave de error en `PlayerProgressChannel.LastErrorKey`; si hay rondas pendientes, `SaveStatus = Failed`.
- **Saneado al leer:** negativos → 0; lo que supera `int.MaxValue` se recorta; `matches < wins+draws+losses` → se repara `matches`; un tipo inválido → **Corrupt**: ReadOnly, no se sobrescribe.

## 2. Flujo

    MainMenu (arranque)                          NetworkRoot (persistente)
     OnlineSessionService: signInOnStart ──▶ OnlineSessionChannel.LocalPlayerId
                                              CloudSaveStatsService (Changed): ledger.Reset(ugsId) → LoadOwnAsync
                                              → ledger.ApplyLoaded(record, lock) → PlayerProgressChannel.SetLoaded(displayed)
     TitleStatsView ◀── Changed (sin cambios de código)
     Sala: participante rival ──────────▶ Service: LoadPublicAsync(rivalId) → channel.SetRival(...) → RivalStatsView

    Prototype (online)                       OnlineModeRoot/MatchStats
     GameEvents.OnMatchPhaseChanged ────▶ OnlineMatchStatsRecorder → MatchStatsTally (¿vio Playing en vivo?)
     GameEvents.OnMatchEnded ───────────▶ local = anchor.LocalPlayer.PlayerId (null = espectador → nada)
                                              delta = PlayerStatsRules.ForMatch(local, result, animales, tackles)
                                              anchor.PlayerStats.TryEnqueue(delta, Guid) ──▶ ledger.Changed
                                              Service: TryBeginSave → SaveOwnAsync(total, lock) → CompleteSave(newLock)
     SaveStatusBadgeView (Results) ◀── channel.SaveStatus: Saving → Saved | Failed (reintento con backoff)

    Abandono (online, Playing)
     Rival Left/TimedOut ─▶ PlayingState: el contexto dice ForfeitOnPlayerMissing → Results(Forfeit, Winner = el que queda)
                            NetworkMatchState replica EndReason → OnMatchEnded en el cliente → victoria normal
     Salir en Playing ────▶ OnlineMatchStatsRecorder.RecordForfeitLoss() → TryEnqueue(derrota) + PlayerPrefs.Save → shutdown
     host_left en Playing ▶ OnlineMatchStatsRecorder (OnDisable con la causa) → victoria → aviso en el menú

## 3. Clases nuevas y modificadas

### 3.1 Runtime (`AlienFarms.Runtime`, no conoce Networking)

| Archivo | Cambio / responsabilidad | API |
|---|---|---|
| `Core/PlayerProgressErrorKeys.cs` (nuevo) | Claves de localización de errores de persistencia | `Offline = "stats.error.offline"`, `RateLimited`, `Unavailable`, `SignedOut`, `NewerSchema`, `Corrupt`, `Unknown` |
| `Data/PlayerProgressChannel.cs` (mod.) | Agrega las estadísticas del rival. **Escritor único:** `CloudSaveStatsService` | `RivalStatus`, `RivalStats`, `HasRivalStats`, `SetRivalLoading()`, `SetRival(in PlayerStatsSummary)`, `SetRivalFailed()`, `ClearRival()`. `SetUnavailable` también limpia al rival |
| `Data/NetworkSettings.cs` (mod.) | Header «Cloud Save» | `_readRivalStats` (true), `[Min(1)] _statsSaveAttempts` (3), `[Min(0.5f)] _statsRetryDelaySeconds` (4, ×3 por intento), `_logPersistence`, `_simulateStatsFailure` (solo Editor/Development) |
| `UI/SaveStatusBadgeView.cs` (nuevo) | «Guardando… / Progreso guardado / No se pudo guardar (se reintentará)» en Results. Visible solo con `MatchSession.Mode == Online` y `SaveStatus != Idle`. Redibuja solo con `Changed` | `_progress, _session, _root, _label`, `LocalizedString _saving/_saved/_failed` |
| `UI/RivalStatsView.cs` (nuevo) | «Rival: {0} partidas · {1} V · {2} E · {3} D» en la Sala; oculto si `!HasRivalStats`. `SetText` sin allocs | `_progress, _root, _label`, `LocalizedString _format` |
| `Core/MatchResult.cs` (mod.) | Agrega `MatchEndReason EndReason` (`TimeUp`, `Forfeit`), por defecto `TimeUp`. Constructor extra | `IsForfeit` |
| `Core/MatchEndReason.cs` (nuevo) | `enum : byte { TimeUp = 0, Forfeit = 1 }` | — |
| `Match/IMatchContext.cs` + `MatchManager` (mod.) | `bool ForfeitOnPlayerMissing` (lo pone `GameModeRouter`: true en online) y `EndByForfeit(PlayerId winner)` | — |
| `Match/PlayingState.cs` (mod.) | Si falta un jugador y `ForfeitOnPlayerMissing`: Results con forfeit; si no, `AbortIfPlayerMissing` como hoy | — |
| `Match/MatchResultRules` / `ResultsState` (mod.) | El ganador por abandono no sale del marcador | — |
| `UI/ResultsView` (mod.) | Subtítulo «{0} abandonó» si `IsForfeit` | `LocalizedString _forfeit` |
| `UI/TitleStatsView.cs` | **Sin cambios** | — |
| `UI/LocalizationKeys` + `Editor/LocalizationSeedData` | ES/EN: `stats.saving`, `stats.save_failed`, `stats.rival` ({0}–{3}), 7 claves `stats.error.*` (`stats.saved` ya existe) | Correr `Setup` |

### 3.2 Networking (`AlienFarms.Networking`), carpeta `Persistence/` (`AlienFarms.Networking.Persistence`)

**Lógica pura (tests EditMode):**

| Archivo | Responsabilidad | API |
|---|---|---|
| `PlayerStatsRecord.cs` | `readonly struct : IEquatable`: `Version`, `Stats`, `LastBatchId` | `static Empty` |
| `PlayerStatsRules.cs` | Acumulación | `ForMatch(PlayerId local, in MatchResult, int animals, int tackles)` (exactamente una de W/D/L), `Add` (satura), `Subtract` (piso 0), `Sanitize` |
| `PlayerStatsCodec.cs` | Serialización y versión | `Key = "stats"`, `CurrentVersion = 1`, `Write(in record, IDictionary<string, object>)`, `Read(IReadOnlyDictionary<string, object>, out record)` → `StatsReadResult { Ok, Missing, NewerSchema, Corrupt }`. Acepta `long`, `int`, `double` entero y string numérico |
| `IPendingStatsStore.cs` + `PendingStatsCodec.cs` | Lectura y escritura del pendiente (`Load(ownerId)`, `Save(ownerId, in pending)`, `Clear(ownerId)`). El codec es puro y testeable | — |
| `PlayerPrefsPendingStatsStore.cs` | Implementación con PlayerPrefs (`stats.pending.{ugsId}`), `PlayerPrefs.Save()` al escribir | — |
| `PlayerStatsLedger.cs` | Modelo de sincronización: dueño, confirmado, lock, pendiente agregado, lote en vuelo, estados. Sin colecciones | `OwnerId`, `LoadState` (`NotLoaded, Loading, Loaded, Failed, ReadOnly`), `SaveState` (`Idle, Saving, Saved, Failed`), `LastErrorKey`, `FailedAttempts`, `Displayed`, `HasPending`; `Reset(ownerId)`, `BeginLoad()`, `ApplyLoaded(...)`, `FailLoad(key)`, `TryEnqueue(delta, deltaId)`, `TryBeginSave(out toWrite, out lock)`, `CompleteSave(newLock)`, `FailSave(key)`, `ResolveConflict(remote, remoteLock)`, `MarkRoundStarted()`, `event Action Changed` |
| `MatchStatsTally.cs` | Qué ronda se registra | `OnPhaseChanged(prev, cur)`: Playing → vio en vivo; Countdown → reinicia. `TryComplete(local, in result, animals, tackles, out delta)`: exige en vivo, `local != None`, una vez por ronda |
| `RivalRules.cs` | A quién leer | `TryFindRival(ReadOnlySpan<OnlineParticipant>, out ugsId)`: espectador local → false; si no, el primer participante que no es local ni espectador |
| `CloudSaveErrorRules.cs` | Motivo → clave y reintentable | `ToKey(CloudSaveExceptionReason)`, `IsRetryable(...)` (NoInternet, TooManyRequests, ServiceUnavailable) |

**Adaptadores y componentes:**

| Archivo | Responsabilidad |
|---|---|
| `IPlayerStatsStore.cs` | Strategy: `LoadOwnAsync()`, `LoadPublicAsync(playerId)`, `SaveOwnAsync(record, writeLock)` → `Task<StatsStoreResult>` |
| `StatsStoreResult.cs` | `readonly struct`: `Outcome` (`Ok, Conflict, RetryableError, Error`), `Record`, `Read`, `WriteLock`, `ErrorKey`, `RetryAfter` |
| `CloudSavePlayerStatsStore.cs` | Única clase que toca Cloud Save. `CloudSaveConflictException` → Conflict; `CloudSaveRateLimitedException` → Retryable con `RetryAfter`; `CloudSaveException` → `CloudSaveErrorRules`; otras → `Unknown` |
| `SimulatedFailureStatsStore.cs` | Decorador: con `SimulateStatsFailure` (solo `UNITY_EDITOR`/`DEVELOPMENT_BUILD`) devuelve `RetryableError(Offline)` sin red |
| `CloudSaveStatsService.cs` | `MonoBehaviour` en **NetworkRoot**. Escucha `OnlineSessionChannel.Changed` y `ledger.Changed`; carga, guarda, reintenta (`Task.Delay` con backoff y `RetryAfter`, cancelado en `OnDisable`); lee al rival; **único escritor** de `PlayerProgressChannel`. Una I/O a la vez. **Sin `Update`.** `OnEnable` → `Evaluate()` |
| `OnlineMatchStatsRecorder.cs` | `MonoBehaviour` en **Prototype/OnlineModeRoot**. `OnMatchPhaseChanged` + `OnMatchEnded` con `MatchStatsTally`; Countdown/Playing → `ledger.MarkRoundStarted()`; local null = espectador → nada; animales con `PenInventorySnapshot.Get(local, i)`; tackles con `CountLocalTacklesLanded(local)` (0 hasta la 3.2); encola con `Guid.NewGuid().ToString("N")` |
| `NetworkMatchState` (mod.) | Replica `EndReason` junto al resultado | — |
| `Data/NetworkRuntimeAnchor.cs` (mod.) | `PlayerStatsLedger PlayerStats { get; }` (lazy por generación). Sugerencia: factorizar los `if (!IsCurrent)` en `EnsureCurrent()` |

API verificada (Cloud Save 3.4.2):

    using PlayerSaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;
    using Unity.Services.CloudSave.Models.Data.Player;   // LoadOptions, Public*AccessClassOptions
    var data = new Dictionary<string, SaveItem> { [PlayerStatsCodec.Key] = new SaveItem(fields, writeLock) };
    Dictionary<string, string> locks = await CloudSaveService.Instance.Data.Player.SaveAsync(
        data, new PlayerSaveOptions(new PublicWriteAccessClassOptions()));
    var items = await CloudSaveService.Instance.Data.Player.LoadAsync(
        _keys, new LoadOptions(new PublicReadAccessClassOptions()));
    var rival = await CloudSaveService.Instance.Data.Player.LoadAsync(
        _keys, new LoadOptions(new PublicReadAccessClassOptions(rivalUgsId)));
    // items.TryGetValue(Key, out Item it) → it.Value.GetAs<Dictionary<string, object>>(), it.WriteLock

Notas: existe otro `Unity.Services.CloudSave.SaveOptions` (Files) → alias obligatorio. Las llamadas no aceptan `CancellationToken`. `SaveItem.WriteLock = null` escribe sin control de conflicto.

### 3.3 Contrato con la 3.2 (tackles)

`TackleStats` es NV **Read Owner**: solo el dueño conoce su conteo, así que solo él puede persistirlo (une los temas 3 y 9). La 3.2 debe: (1) reiniciar `TackleStats` en el servidor al empezar cada ronda; (2) cambiar solo `OnlineMatchStatsRecorder.CountLocalTacklesLanded(NetworkPlayer local)` para leer `local.State.TackleStats.Landed`. No se usa `OnTackleResolved` para contar (en la 3.2 lo dispara un FX Unreliable). Hoy nadie levanta `OnTackleResolved`.

## 4. Eventos

| Evento | Emite | Escuchan |
|---|---|---|
| `GameEvents.OnMatchPhaseChanged` / `OnMatchEnded` (existentes) | `MatchManager` (host) / `NetworkMatchState` (clientes), un emisor por instancia | `OnlineMatchStatsRecorder` |
| `PlayerStatsLedger.Changed` (instancia) | Ledger | `CloudSaveStatsService` |
| `OnlineSessionChannel.Changed` (existente) | `OnlineSessionService` | `CloudSaveStatsService` |
| `PlayerProgressChannel.Changed` (existente) | `CloudSaveStatsService` (único) | `TitleStatsView`, `SaveStatusBadgeView`, `RivalStatsView` |

Sin estáticos nuevos.

## 5. Escena y prefabs (scene-builder)

- **SB1, NetworkRoot:** `CloudSaveStatsService` en `Prefabs/Networking/NetworkRoot.prefab` con `NetworkSettings`, `OnlineSessionChannel`, `PlayerProgressChannel`, `NetworkRuntimeAnchor`. `_signInOnStart` sigue en ✓. `NetworkSettings.asset`: ReadRivalStats ✓, SaveAttempts 3, RetryDelay 4, LogPersistence ✓, SimulateStatsFailure ✗.
- **SB2, Prototype:** `Modes/OnlineModeRoot/MatchStats` con `OnlineMatchStatsRecorder` (`_anchor`, `_spawnSettings` = el mismo `AnimalSpawnSettings` del spawner, `_settings`). `ResultsPanelRoot/SaveStatusBadge` (TMP bajo el marcador) con `SaveStatusBadgeView` (`_session` = `MatchSession.asset`, `_progress`); se oculta solo en local.
- **SB3, MainMenu:** `UI_OnlinePanel.prefab` → `Room/RivalStats` (TMP bajo `Seats`) con `RivalStatsView`, inactivo hasta tener datos. `TitleStats` no se toca. Correr Localization `Setup`.
- **SB3b, MainMenu:** `OnlineMenuController._progress` = `PlayerProgressChannel.asset` (aviso «stats.host_left_win»; opcional: sin él se ve «El anfitrión cerró la sala»).

## 6. Subtareas

| # | Agente | Contenido | Depende de | Verificación |
|---|---|---|---|---|
| A | GP | Runtime: error keys, rival en el canal, `NetworkSettings`, `SaveStatusBadgeView`, `RivalStatsView`, localización | — | `PlayerProgressChannelTests` ampliado |
| B | GP | Puro: record, rules, codec, ledger, tally, rival rules, error rules | — (paralelo a A) | Tests de rules, codec, ledger, tally, rival y error rules |
| C | GP | Store, decorador, `CloudSaveStatsService`, anchor `PlayerStats`, `OnlineMatchStatsRecorder` | A, B | Compila; login → «stats loaded (v1)» |
| SB1–SB3 | SB | NetworkRoot, Prototype, UI_OnlinePanel + Localization (en serie) | C | Escenas y prefabs guardados; local igual |
| CR | CR | Revisión | — | Único escritor del canal, nada de UGS en Runtime, sin `Update` ni allocs por frame, `this == null` tras cada `await`, alias de `SaveOptions` |
| QA | QA | EditMode, regresión local, gate MPPM §7 | todo | Checklist |

## 7. Checklist del gate MPPM (Relay; host + cliente + 3ª instancia)

1. **Primera carga:** cada instancia loguea su UGS id y «stats loaded»; una cuenta sin datos muestra 0, no «—».
2. **Sala:** cada jugador ve «Rival: …» del otro; el espectador no.
3. **Partida completa** (timer acortado): en Results «Guardando…» → «Progreso guardado» en host y cliente; el espectador ni muestra ni guarda.
4. **Vuelta al Hub/título:** partidas +1; victoria/derrota/empate correctos; animales = corral final.
5. **Persisten:** salir y volver a entrar a Play en las 3 instancias → mismo UGS id y mismas estadísticas.
6. **Revancha:** la segunda ronda suma +1 (batch distinto).
7. **Reconexión (2.7):** caída en Playing y vuelta antes del fin → cuenta una vez; caída y vuelta en Results → no cuenta doble.
7b. **Abandono:** el cliente sale en Playing → el host ve Results «{rival} abandonó» y suma una victoria; el cliente suma una derrota. El host sale en Playing → el cliente suma una victoria y ve el aviso. El cliente se cae y no vuelve → el host gana; el cliente no suma nada. El rival se va en Countdown → no cuenta.
8. **Sin conexión:** `SimulateStatsFailure` en el cliente → «No se pudo guardar (se reintentará)», total optimista, reintentos con backoff. **Cerrar el juego con el pendiente**, volver a abrirlo sin la falla → se sube la suma completa y desaparece `stats.pending.*`. Reiniciar lo confirma, sin sumar dos veces.
9. **Regresión local:** sin llamadas a Cloud Save, sin aviso, título sin cambios. 0 errores y 0 warnings nuevos. EditMode en verde.

## 8. Riesgos

- **R1:** Cloud Save debe estar habilitado en el Dashboard (entorno `dev`); si no, Unauthorized/NotFound. QA lo verifica primero.
- **R2:** identidad anónima: persiste mientras se mantenga el perfil de Authentication (cada clon MPPM tiene el suyo).
- **R3:** desmontaje del root durante un guardado: ledger (anchor) y canal (SO) siguen vivos; la generación descarta resultados al salir de Play.
- **R4:** se confía en el cliente; la mitigación real es Cloud Code (fuera del alcance). Se documenta para el curso.
- **R5:** tackles en 0 hasta la 3.2.
- **R6:** límites de tasa: 1 guardado por ronda + reintentos; se respeta `RetryAfter`.
- **R7:** tipos numéricos de `GetAs<Dictionary<string, object>>` (`long`/`double`), cubiertos por el codec.
- **R8:** comprobar `this == null` e identidad del ledger tras cada `await`.
- **R9:** cero allocs por frame; solo por ronda o carga.
- **R11:** PlayerPrefs en clones MPPM: la clave lleva el UGS id, así que no chocan aunque se compartan. El cliente puede editar PlayerPrefs; es la misma confianza de R4.
- **R12:** la victoria por abandono cambia la regla de partida online (antes volvía a Waiting). Revisar que la revancha desde Results no se active con un solo jugador (`IsRematchReady` ya exige 2).
- **R10:** Plan B #2: `ReadRivalStats = false` y `Room/RivalStats` inactivo.
- **R13 (aceptado):** caída del host sin razón `host_disconnected` (`ConnectionLost`: crash del host, red caída, o la sesión borrada sin que NGO informe la razón): el cliente no registra victoria ni ve el aviso. Solo un `host_disconnected` explícito (el host sale con Salir y el root desconecta a los clientes) cuenta como victoria. El aviso «stats.host_left_win» lo levanta el ledger (`RaiseHostLeftWinNotice`), lo publica `CloudSaveStatsService` en `PlayerProgressChannel.HasHostLeftWinNotice`, y `OnlineMenuController.GetErrorKey` (único lector) lo muestra en lugar de `online.error.host_left` y lo consume con `RequestConsumeHostLeftWinNotice()`. El recorder no escribe en `OnlineSessionChannel`.
- **R14 (aceptado):** creación concurrente de la clave. La primera escritura va con `writeLock = null` (sin control de conflicto): si dos dispositivos de la misma cuenta la crean a la vez, el último pisa al primero y se pierde lo de uno. En 1v1 con una cuenta por máquina es improbable. Lo mismo con un tercer dispositivo que escriba entre un envío ambiguo y su resolución: el `batch` remoto dejaría de ser el del head y ese head podría contarse dos veces.
