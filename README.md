# Alien Farms

Party game 1v1 (Unity 6000.3, URP, New Input System). Cada jugador lleva animales desde la zona central a su corral; gana quien tenga más al terminar el tiempo. Modo **local** (pantalla dividida) y modo **online** (Netcode for GameObjects + Unity Gaming Services: Authentication, Multiplayer Sessions con Relay, Cloud Save).

- Diseño del juego: `Docs/Game Jam Game Design Document.docx.pdf`
- Arquitectura (bóveda Obsidian): `Docs/Arquitectura/00 Indice.md`
- Plan del modo online: `Docs/Arquitectura/Online/Plan Modo Online.md`

---

## Semana 13 — Áreas de interés (AOI)

**Problema:** el servidor replicaba los 12 animales y la bomba a los dos jugadores aunque estuvieran al otro lado del mapa.

### Regla elegida: distancia horizontal con histéresis

El servidor decide, por cada cliente y cada objeto filtrable (animales y bombas), si ese cliente lo conoce:

| Caso | ¿Lo ve? |
|---|---|
| AOI apagada, o tipo no filtrado | Siempre |
| El cliente es el host (servidor) | Siempre |
| El cliente es el dueño del objeto (lo lleva en la mano) | Siempre |
| Objeto exento (llevado por un jugador; los avatares no se filtran) | Siempre |
| El cliente aún no tiene avatar | Siempre (no se oculta nada a ciegas) |
| Objeto **no visible** | Entra si la distancia XZ < **30 m** |
| Objeto **visible** | Sale si la distancia XZ > **36 m** |

- Los dos radios (30 m para entrar, 36 m para salir) evitan el parpadeo en el borde: un jugador que zigzaguea ±2 m alrededor de 33 m no provoca ni altas ni bajas.
- Se usa la distancia horizontal porque la arena es plana (80×30 m) y los saltos o lanzamientos no deben ocultar objetos.
- Los radios coinciden con los valores de referencia del curso.

### Implementación (`Assets/Scripts/Networking/Interest/`)

- `InterestRules`: la regla, una función pura con tests EditMode.
- `IInterestPolicy`: lo implementan `NetworkAnimal` y `NetworkBomb`. Asignan `NetworkObject.CheckObjectVisibility` **antes del spawn**, así un objeto lejano nunca llega al cliente (no hay alta y baja inmediata).
- `NetworkInterestManager`: solo en el servidor. Revisa cada **5 ticks** y llama `NetworkShow`/`NetworkHide`. Lleva contadores de altas y bajas por cliente.
- `InterestSettings_Default.asset` (ScriptableObject): radios, frecuencia y tipos filtrados. `InterestToggle` permite apagarla fuera de Playing para medir; durante la partida se ignora.
- HUD de diagnóstico (`InterestStatsView`, `KnownObjectsCounter`): muestra los objetos conocidos por el cliente (animales, bombas y otros), «P2 ve», altas y bajas, y Tx/Rx de la ventana. Encima va el **Runtime Network Stats Monitor**.
- Lo que no cambió y siguió funcionando con la AOI: los eventos de la bomba (spawn, mecha, explosión) viajan por `NetworkMatchState`, así que P2 oye la bomba aunque nazca fuera de su área. Al reaparecer, un objeto llega con su estado y su pose actuales (capturado en el corral, en la mano de P1…).

### Medición: 60 s con y sin AOI

Mismo guion las dos veces (P1 quieto en su corral; P2 va de su spawn al centro, al corral de P1, vuelve y hace zigzag), 12 animales + 1 bomba. Datos del Network Profiler (recorders de NGO) y del RNSM. Detalle completo: `Docs/Arquitectura/Online/Medición S13 AOI.md`.

| Métrica (host → P2, 60 s) | AOI OFF (media de 2) | AOI ON (media de 3) | Cambio |
|---|---|---|---|
| Animales que conoce P2 al final | 12 | 5,7 | −53 % |
| Objetos conocidos por P2, mínimo | 15 | 8 | — |
| Bytes NGO enviados (Total Bytes Sent) | 698,8 KB | 628,4 KB | **−10,1 %** |
| Bytes de mensajes NGO | 585,4 KB | 515,6 KB | **−11,9 %** |
| Bytes por transporte (Tx de la ventana) | 1043 KB | 1000 KB | −4,1 % |
| Spawns enviados (msgs / bytes) | 1 / 107 B | 13 / 1491 B | coste de las altas |
| Destroys enviados (msgs / bytes) | 0 / 0 B | 12 / 60 B | coste de las bajas |
| Altas / bajas en 60 s | 0 / 0 | 12 / 12 | sin oscilación |
| Bytes de RPC (jugadores) | 366 KB | 366 KB | 0 % |

**Lectura:**
- Ocultar un animal lejano ahorra sus deltas de NetworkTransform. Cada alta cuesta ~110 B (un CreateObject con el estado actual) y cada baja ~5 B: en 60 s, ~1,5 KB de coste contra ~70 KB ahorrados.
- El 55 % del tráfico son RPC de los jugadores, que siempre se ven, y por eso el ahorro total es moderado. En un mapa más grande o con más objetos el ahorro crecería; el siguiente candidato a optimizar es el tráfico de los jugadores, no los animales.

### Pruebas

- Tests EditMode de la regla, la histéresis y los contadores (1497 tests en verde).
- Gate funcional en MPPM (host + clon), 12 de 12 OK:
  - spawn inicial oculto;
  - ida y vuelta sin deslizamientos;
  - pool estable (90 creados);
  - agarre y lanzamiento a distancia;
  - cambio de dueño;
  - bomba con aviso, mecha y stun;
  - marcador estable;
  - reconexión de P2 en Playing sin duplicados;
  - zigzag en el borde sin altas ni bajas;
  - interruptor ignorado en Playing;
  - más de 30 ciclos de ocultar y mostrar;
  - modo local intacto.

### Limitaciones conocidas

- Al ocultar y volver a mostrar un animal capturado, el cliente vuelve a lanzar los eventos locales de captura. Hoy nadie los escucha en el cliente para gameplay (el marcador viene del servidor), pero un futuro efecto de captura en el cliente sonaría en falso.
- La medición se hizo en una sola PC con MPPM: el RTT es simulado por el Relay y las dos instancias comparten CPU.

### Commits de la semana

Bloque 0 (retirar el espectador para dejar 2 asientos): `375375c` … `6272f42`.
AOI: `7a76b7a` reglas con histéresis · `0b90ffe` animales y bombas aceptan la política (protocolo 5) · `302d1c2` NetworkInterestManager · `031a2b7` contador de conocidos en el HUD · `19607e1` montaje en Prototype · `7be4db6` arreglos de la revisión · medición y README.

### Guion del video (3–5 min)

1. **0:00–0:30** Integrantes. El problema: replicar todo a todos.
2. **0:30–1:30** La regla: 30/36 m, distancia XZ y excepciones (host, dueño, llevado). Mostrar `InterestRules` y la asignación de `CheckObjectVisibility` antes del spawn.
3. **1:30–3:00** En vivo, host y cliente lado a lado con el HUD y el RNSM:
   - P2 se aleja y los «Conocidos» bajan;
   - vuelve y los animales reaparecen en su sitio, sin parpadeo;
   - zigzag en el borde: 0 altas y 0 bajas;
   - P1 lanza un animal y P2 lo ve en el aire.
4. **3:00–4:15** La tabla OFF vs ON: qué bajó, qué cuesta un alta y por qué las RPC de los jugadores no cambian.
5. **4:15–5:00** Conclusiones y limitaciones.

---

## Semana 14 — Voz de proximidad (Vivox) y amigos (Friends)

**Qué agrega:** chat de voz posicional entre los dos jugadores de una sesión online, y una lista de amigos con presencia. El host puede invitar a un amigo a su sala sin dictarle el código.

Diseño: `Docs/Arquitectura/Online/Diseño S14 Voz y Amigos.md` · Medición: `Docs/Arquitectura/Online/Medición S14 Voz.md` · Gate de amigos: `Docs/Arquitectura/Online/Gate S14 Amigos.md`

### Versiones
- **Vivox 16.13.0** (`com.unity.services.vivox`). La configuración del proyecto está commiteada en `ProjectSettings/Packages/com.unity.services.vivox/`.
- **Friends 1.4.1** (`com.unity.services.friends`). El diseño apuntaba a 1.1.1, pero el paquete que quedó instalado es 1.4.1. Funciona con el Authentication y el Core que ya usamos, y el código se verificó contra su API real (`MessageAsync<T>`, `SetPresenceAsync<T>`, `ForceRelationshipsRefreshAsync`).
- Las dos usan el mismo **PlayerId** de Authentication que Sessions: no hay una identidad aparte por servicio.

### Voz: un canal posicional por sesión
- **Nombre del canal:** `af-<sessionId>`, filtrado al alfabeto de Vivox (`VoiceChannelNameRules`). Host y cliente lo calculan solos a partir del Id de la sesión, sin negociarlo. Otra sesión nunca cae en el mismo canal.
- **Ciclo:** login de Vivox al llegar a `Ready`. Se entra al canal en `InSession` (ya en la Sala) y se sale en `Leaving`/`Ready`, en `OnClientStopped` y en `OnDisable`. El reconciliador (`VoiceChannelPlanner`) tiene una sola operación en vuelo y vuelve a evaluar al terminar: así resiste reconexiones rápidas y rejoins.
- **Posición:** `VoicePositionReporter` envía la pose del avatar local a **3 Hz** con `Set3DPosition` (medido: 3,00 Hz). Solo envía si la pose cambió, con un keep-alive cada 2 s. En la Sala no hay avatar: todos quedan en el origen y se oyen como chat de sala.
- **`Channel3DProperties(30, 1, 1.0, InverseByDistance)`:**
  - **30 m audible:** es el mismo radio de entrada de la AOI de la S13. Lo que ves es lo que oyes.
  - **1 m conversacional:** más o menos media altura del avatar. A esa distancia o menos, volumen pleno.
  - **1,0 de intensidad, Inverse:** es la caída más natural (estilo OpenAL), sin cortes bruscos.
  - La arena mide unos 80×30 m: de corral a corral no se oye. Hay que acercarse al centro para hablar con el rival.

#### Tabla de distancias (MPPM, P2 con WAV inyectado)
| Distancia (HUD) | Ganancia teórica | ¿Rival en el canal? | ¿Se oye? | Inteligibilidad (1–5) | Participant Added/Removed |
|---|---|---|---|---|---|
| 5,0 m | 0,20 (−14,0 dB) | Sí | Sí | pendiente de oído (2 PCs) | sigue Added |
| 15,0 m | 0,067 (−23,5 dB) | Sí | Sí, bajo | pendiente de oído (2 PCs) | sigue Added |
| 35,0 m | 0 | No | No («Fuera de alcance») | — | Removed a ~32–33 m; Added al volver a ~26,6 m |

Vivox quita al participante un poco después de los 30 m: la posición llega 3 veces por segundo y el servidor tarda en decidir. `VoiceRivalStateRules` usa la distancia del avatar para distinguir «Fuera de alcance» de «Sin voz» (desconectado), con histéresis de 5 m y una espera de 3 s.

> **Eco:** en MPPM las dos instancias comparten micrófono y salida en una sola PC. Por eso la medición se hizo con audio inyectado (`StartAudioInjection`) y AEC encendido. La inteligibilidad y el eco reales se evalúan con la build de 2 PCs y auriculares.

### Controles de voz
| Acción | Teclado | Mando | UI |
|---|---|---|---|
| Silenciar o activar mi micrófono | `M` | D-pad ↑ | Toggle en la Sala y en la pausa online |
| Volumen del rival (solo para mí) | — | navegación UI | Slider 0..1 → `LocalVolume` −50..+50 |
| Silenciar al rival (solo para mí) | — | navegación UI | Botón «Silenciar rival» |

HUD online: el estado del micrófono, quién habla y la distancia al rival («12,4 m»), que se refresca a 4 Hz sin allocs. Al reconectar, el host le vuelve a aplicar al rival el volumen y el silencio guardados por asiento.

### Amigos e invitación
1. **Panel de amigos** (Hub y Sala): tu Id con botón **Copiar**, un campo para el Id de un amigo con **Agregar**, solicitudes entrantes (Aceptar/Rechazar) y salientes (Cancelar), y la lista de amigos con su presencia.
2. **Presencia:** `Online` con actividad *Menú en línea* (hub) o *En sala* (room), y `Busy` en partida. Se publica con debounce de 1 s. Al salir se pone `Offline` (best-effort).
3. **Invitar:** el host en la Sala, con lugar libre, pulsa **Invitar** sobre un amigo Online. Se envía `MessageAsync` con `SessionInviteMessage {type, version, code, sessionId, hostName, sentAtUnixMs}`, que pesa menos de 1 KB. Hay una espera de 5 s por amigo.
4. **Recibir:** se valida el mensaje (`InviteRules`): tiene que ser del tipo y la versión esperados, con un código bien formado, de un amigo de la lista y no de uno mismo. Además se comprueba la TTL de 120 s, con 60 s de tolerancia al desfase de reloj. Se guarda una sola invitación pendiente y aparece el aviso «X te invita», con cuenta regresiva.
5. **Aceptar** (`FriendsMenuController` + `InviteJoinSequence`):
   - en el Hub → `RequestJoin(code)`;
   - en otra Sala → primero sale, espera `Ready` y luego entra;
   - con una oferta de reconexión → la rechaza y entra;
   - desde el título → `PlayOnline()` y entra al llegar a `Ready`;
   - si venció → «La invitación venció».

**Gate en MPPM:** 11/11 OK. Ids de prueba: A `LnLCcU3AbOwSKlQwXcbe9tgSRPbR`, B `eXr0K0nGnBTpNUYxzKcux5cQRo9T`. Las amistades duran mientras duren los tokens de esos perfiles.

### Qué no se pudo probar
- La inteligibilidad de oído y el eco: esperan a la build de 2 PCs.
- Presencia `Busy` en partida y «mismo personaje» tras aceptar: no se jugó una partida completa en el gate de amigos.
- Aceptar una invitación estando en otra Sala, o con una oferta de reconexión: necesita una 3.ª sesión. Está cubierto por tests EditMode.
- La caída de notificaciones de Friends y el refresco de respaldo cada 30 s.
- Recibir invitaciones sin haber entrado nunca al online: Friends se inicializa después del sign-in, que hoy ocurre al pulsar «Online».
- Más de 2 PCs.

### Respuestas a las preguntas de las diapositivas
- **D9. ¿Qué canales necesitas?** Uno solo: un canal **posicional** por sesión. Es un 1v1 sin equipos, así que no hace falta un canal de equipo. No queremos oír a jugadores de otras sesiones (por eso el canal lleva el Id de la sesión) ni al rival cuando está lejos (por eso es posicional).
- **D12. ¿Qué problema trae un canal de grupo con quince jugadores?** Todos se oyen a volumen pleno: quince voces se pisan, se pierde la sensación de espacio y se filtra información (oyes al enemigo a 200 m como si estuviera al lado). La voz de proximidad hace que el sonido siga las mismas reglas que la vista.
- **D15. ¿Qué haría un cliente modificado con la posición?** Vivox confía en la posición que manda cada cliente. Uno modificado podría reportarse siempre junto al rival para oírlo en todo el mapa, o esconderse para que no lo oigan. En nuestro 1v1 importa poco, porque la voz no da ventaja táctica fuerte. Importaría en un juego competitivo por equipos. Ahí habría que derivar la posición de la que ya tiene autoridad el servidor NGO (S12), por ejemplo desde un backend que firme la posición, en vez de aceptar la del cliente.
- **D18. ¿Cómo bajar a un compañero sin que lo note? ¿Cuándo bloquear?** Con el volumen y el silencio **locales** del participante (`LocalVolume`, mute local): solo cambian lo que oyes tú, y el otro no recibe ningún aviso. Es lo que hace nuestro slider «Volumen del rival» y el botón «Silenciar rival». Bloquear es para el acoso o el abuso: es persistente y vale para las próximas sesiones, no solo para esta.
- **D21. Si bloqueo en Friends, ¿deja de oírme en Vivox?** No. Son servicios separados: el bloqueo de Friends no llega a Vivox. Para que las dos listas coincidan, al recibir `RelationshipAdded` de tipo *Block* habría que bloquear también a ese PlayerId en Vivox, y desbloquearlo al borrar la relación. Como el PlayerId es el mismo en los dos servicios, el mapeo es directo. Queda como opcional (O1) y no está implementado.
- **D24. Aceptar una invitación estando en otra sesión: ¿qué tiene que pasar primero?** Hay que **salir de la sesión actual** y esperar a `Ready` antes de entrar a la nueva. Es la rama `LeaveThenJoin` de `InviteAcceptRules`. La S9 ayuda a no perder las **estadísticas pendientes de Cloud Save**: se guardan primero en PlayerPrefs y se suben después, así que salir de la sala no las pierde.

### Commits de la semana
- Diseño: `a60c7aa`.
- Voz:
  - `52117fc` paquetes y asmdef;
  - `be92e0a` settings, canal y eventos;
  - `ab9c961` reglas puras;
  - `dbd4372` VoiceChatService, posición y ToggleMic;
  - `56f0e50` montaje;
  - `6885279` arreglos de la QA y medición;
  - `c2328be` HUD y navegación de la pausa.
- Amigos:
  - `062816c` settings, canal y claves;
  - `97922b7` mensaje y reglas;
  - `c9faf68` FriendsSocialService;
  - `42a749b` invitación y FriendsMenuController;
  - `cb7ff55` arreglos de la revisión;
  - `5130ea4` montaje;
  - `f29a1d8` gate en MPPM;
  - README.

### Guion del video (3–5 min)
1. **0:00–0:30** Integrantes. Qué agrega la S14: voz de proximidad y amigos con invitación.
2. **0:30–1:30** Amigos:
   - P2 copia su Id;
   - P1 lo agrega;
   - P2 acepta la solicitud;
   - los dos se ven *En línea · Menú en línea*.
3. **1:30–2:15** Invitación:
   - P1 crea la sala y P2 la ve *En sala*;
   - P1 pulsa **Invitar**, a P2 le llega el aviso y acepta;
   - los dos quedan en la Sala sin dictar el código;
   - mostrar también una invitación vencida.
4. **2:15–3:30** Partida, hablando:
   - los dos en el centro (5 m);
   - P2 se aleja a 15 m: la voz baja y el HUD marca la distancia;
   - P2 llega a 35 m: silencio y «Fuera de alcance»;
   - P2 vuelve.
   - Silenciar el mic con `M`/D-pad ↑ y bajar el volumen del rival.
5. **3:30–4:15** Caída de P2: «Sin voz» y el banner de espera. P2 vuelve, se oye otra vez y conserva el volumen que le había puesto P1.
6. **4:15–5:00**
   - Arquitectura: canal = Id de la sesión, 3 Hz, PlayerId compartido.
   - La tabla 5/15/35 m.
   - Aviso de eco, si lo hubo.
   - Conclusiones.
