# Medición S13 — Área de interés (AOI) apagada vs encendida

Fecha: 2026-11-24. MPPM en una PC: P1 = host (editor), P2 = cliente (clon). Mismo guion de 60 s (P1 quieto en su corral; P2: spawn → centro → corral de P1 → su corral → zigzag ±2 m en x≈3 → quieto). AOI 30/36 m, revisión cada 5 ticks. 12 animales activos + 1 bomba (13 entidades filtrables), pool `Created` 90 estable en todos los recorridos, 0 frames con discrepancia del invariante (Conocidos = registro activo de P2).

`ON1` fue un recorrido de prueba con el mismo guion; se incluye porque los valores son coherentes.

## Host (P1) — enviado en la ventana de 60 s

| Métrica | OFF-A | OFF-B | ON1 | ON-A | ON-B | Media OFF | Media ON | Cambio |
|---|---|---|---|---|---|---|---|---|
| Total Bytes Sent (profiler NGO) | 680.0 KB | 717.6 KB | 615.6 KB | 611.4 KB | 658.1 KB | 698.8 KB | 628.4 KB | **−10.1 %** |
| Network Messages Bytes Sent | 565.7 KB | 605.0 KB | 505.8 KB | 503.1 KB | 537.8 KB | 585.4 KB | 515.6 KB | **−11.9 %** |
| Tx transporte (HUD) | 1030 KB · 17.2 KB/s | 1056 KB · 17.6 KB/s | 966 KB · 16.1 KB/s | 1012 KB · 16.9 KB/s | 1021 KB · 17.0 KB/s | 1043 KB | 1000 KB | −4.1 % |
| RPC Bytes Sent | 366.0 KB | 366.1 KB | 366.0 KB | 366.0 KB | 366.0 KB | 366 KB | 366 KB | 0 % |
| NetworkVariable Bytes Sent | 308 B | 288 B | 48 B | 188 B | 288 B | 298 B | 175 B | −41 % |
| Object Spawned (msgs / bytes) | 1 / 107 B | 1 / 107 B | 11 / 1227 B | 19 / 2133 B | 10 / 1113 B | 1 / 107 B | 13 / 1491 B | +1290 % |
| Object Destroyed (msgs / bytes) | 0 / 0 B | 0 / 0 B | 9 / 45 B | 19 / 95 B | 8 / 40 B | 0 | 12 / 60 B | — |
| Altas / Bajas (HUD) | 0 / 0 | 0 / 0 | 10 / 9 | 18 / 19 | 9 / 8 | 0 / 0 | 12 / 12 | — |
| «P2 ve» animales final (mín/máx) | 12 (12/12) | 12 (12/12) | 7 (4/12) | 3 (3/12) | 7 (6/12) | 12 | 5.7 | −53 % |

## Cliente (P2) — recibido en la ventana

| Métrica | OFF-A | OFF-B | ON1 | ON-A | ON-B |
|---|---|---|---|---|---|
| Conocidos final (total / anim / bombas) | 16 / 12 / 1 | 16 / 12 / 1 | 11 / 7 / 1 | 7 / 3 / 1 | 11 / 7 / 1 |
| Conocidos mín / máx | 15 / 16 | 15 / 16 | 8 / 16 | 7 / 16 | 9 / 16 |
| Total Bytes Received (profiler) | 679.7 KB | 717.4 KB | 615.6 KB | 611.5 KB | 658.1 KB |
| Rx transporte (HUD) | 1028 KB · 17.2 KB/s | 1052 KB · 17.6 KB/s | 962 KB · 16.1 KB/s | 1009 KB · 16.9 KB/s | 1019 KB · 17.0 KB/s |
| RNSM Total Bytes Received/s (t=60) | 10.9 kB/s | 11.4 kB/s | 9.33 kB/s | 9.51 kB/s | 10.4 kB/s |
| RTT (RNSM) | 228 ms | 299 ms | 215 ms | 206 ms | 211 ms |

## Lectura

- La AOI recorta ~10–12 % de los mensajes NGO y ~4 % del transporte (que incluye cabeceras, acks y el tráfico de jugadores). Los animales fuera de los 36 m dejan de enviar sus deltas de NetworkTransform.
- El coste es pequeño: cada alta pesa ~110 B (CreateObject con estado) y cada baja ~5 B. En 60 s, ~1.5 KB de spawns contra ~70 KB ahorrados.
- Las RPC (~366 KB, el 55 % del total) no cambian: son de los jugadores (input/estado), que siempre están visibles. Ese es el siguiente candidato a optimizar, no los animales.
- La histéresis 30/36 m funciona: altas ≈ bajas, sin oscilación en el zigzag.
- La arena es pequeña (80×30 m), así que P2 tiene casi siempre a la vista la mitad de los animales; en mapas más grandes la reducción sería mayor.
