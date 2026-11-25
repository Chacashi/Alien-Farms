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
