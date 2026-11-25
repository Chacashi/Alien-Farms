# Medición S14 — Voz de proximidad (Vivox)

Fecha: 2026-11-25. MPPM en una PC con UGS real (Sessions + Relay, unión por código). Host P1 en el editor (perfil default) y P2 en el clon (perfil player2), con PlayerIds distintos. Canal posicional `af-<sessionId>` con `Channel3DProperties(30, 1, 1.0, InverseByDistance)`. P2 se movió con un mando virtual y habló con un WAV inyectado (`StartAudioInjection`).

## Tabla de distancias

| Distancia real | Distancia en el HUD | Estado del rival | ¿Rival en el canal? | SpeechDetected / energía del emisor | Ganancia teórica (Inverse, c = 1) |
|---|---|---|---|---|---|
| 4,96 m | 5,0 m | En alcance | Sí (495/495 muestras) | Sí · 0,385 | 0,20 (−14,0 dB) |
| 15,03 m | 15,0 m | En alcance | Sí (509/509) | Sí · 0,385 | 0,067 (−23,5 dB) |
| 35,03 m | 35,0 m | Fuera de alcance | No (0/491) | — · 0 | 0 (fuera del canal) |

- Al alejarse, Vivox quita al participante a ~32–33 m, no justo en 30: la posición se reporta 3 veces por segundo y el servidor tarda un poco en decidir. Al acercarse, el participante vuelve a ~26,6 m.
- `AudioEnergy` mide la energía del que habla **sin atenuar** (0,385 a 5 m y a 15 m). La caída real por distancia se comprueba de oído; de eso sale la columna de inteligibilidad del README.
- `Set3DPosition` se midió a **3,00 Hz** en las dos instancias: 30 envíos en 10 s, con un hueco máximo de 0,34 s.

## Controles

- Silenciar el micrófono (toggle de la UI o la acción ToggleMic con D-pad arriba) cambia `IsInputDeviceMuted` y lanza `OnVoiceMuteChanged`. La inyección de audio ignora el silencio del micrófono, así que eso se verificó a nivel de API.
- Volumen del rival: slider 0 / 0,25 / 1 → `LocalVolume` −50 / −25 / +50.
- Silenciar al rival: `IsMuted` local del participante en true. El rival sigue oyendo a los demás con normalidad.

## Reconexión

Con `DisconnectClient` en Playing, el HUD del host muestra «Sin voz» a los 30 ms y Vivox quita al participante a los 0,38 s. El auto-rejoin devuelve a P2 al mismo canal a los ~9 s, y el host le vuelve a aplicar el volumen (−25) y el silencio que tenía guardados.

## Pendiente de oído (2 PCs con auriculares)

Inteligibilidad (1–5) a 5, 15 y 35 m, y si hubo eco.
