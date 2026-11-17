# Checklist 2.9: build en 2 PCs con Relay

Objetivo: jugar una partida completa por internet entre dos PCs, usando Relay, y comprobar revancha, abandonos, reconexión y estadísticas.

## Preparación

1. Copiar la carpeta `Builds/2.9_Relay/` **entera** a cada PC (no solo el `.exe`: también `AlienFarms_Data/`, `UnityPlayer.dll`, etc.). Se puede pasar en un `.zip`.
2. Abrir `AlienFarms.exe`. Si Windows muestra «Windows protegió su PC», ir a *Más información* > *Ejecutar de todas formas*.
3. Si aparece el aviso del **Firewall de Windows**, marcar *Redes privadas* y *Redes públicas* y pulsar *Permitir acceso*.
4. Usar **redes distintas** si se puede (por ejemplo, una PC en el wifi de casa y la otra con datos del celular compartidos). Así se prueba Relay por internet de verdad.
5. **Evitar el wifi de la universidad**: suele bloquear UDP y Relay no conecta (este build usa UDP/DTLS, no WebSockets).
6. Cada jugador juega en su PC con **teclado+mouse** o con mando. El código de sala se escribe con teclado.
7. El juego arranca a pantalla completa. Con **Alt+Enter** pasa a ventana (útil para ver otras cosas mientras tanto).
8. Si queréis abrir dos copias en **la misma PC** (solo para un ensayo), abrid la segunda con un perfil distinto o compartirán la cuenta: `AlienFarms.exe -ugs-profile pc2`.

## Pasos

| # | Qué hacer | Qué se debe ver |
|---|---|---|
| 1 | En cada PC, pulsar **Enter** (o X en el mando) en el título para entrar. **PC A (host):** Online > Crear sala. **PC B (cliente):** Online > Unirse y escribir el código de 6 caracteres. | Los dos aparecen en la sala con su nombre. No hay errores en pantalla. |
| 2 | Cada uno elige un **personaje distinto** y se marca listo. El host empieza la partida. | El personaje elegido por cada uno es el que aparece en la partida. Los dos cargan el mapa. |
| 3 | Jugar una **partida completa** (5:00 o la duración por defecto). | El timer avanza igual en las dos pantallas. Los animales y los puntos coinciden en ambas. Al final, los dos ven el mismo ganador. |
| 4 | **Revancha** desde la pantalla de resultados. | Empieza una nueva ronda con los mismos jugadores, sin tener que volver a crear la sala. |
| 5 | Durante la partida, **el cliente sale** (menú de pausa > salir, o cerrar el juego). | El host gana por abandono y ve el resultado. |
| 6 | Nueva sala y partida. Durante la partida, **el host sale**. | El cliente ve un aviso de que el host se fue y gana. Vuelve al menú sin quedarse colgado. |
| 7 | Nueva partida. **Desconectar el wifi del cliente** unos 5–10 segundos y volver a conectarlo. | El cliente reconecta solo (o con *Reconectar*) dentro del tiempo de gracia (~20 s) y sigue en la misma ronda. Al terminar, la ronda **cuenta una sola vez** en las estadísticas. |
| 8 | **Cerrar los dos juegos y volver a abrirlos.** | En el título, las estadísticas (victorias, derrotas, partidas) son las mismas que antes de cerrar. |
| 9 | *Opcional:* una **tercera PC** se une a la sala con el código mientras la partida está en curso. | Entra como **espectador**: ve la partida pero no juega. |

## Qué anotar

Para cada paso: **OK** o **falla** y una frase de qué pasó. Además:

- Ping o lag que se note (por ejemplo, «los animales saltan», «el cliente se mueve con retraso»).
- Desincronizaciones: puntos, timer o posiciones distintos entre las dos pantallas.
- Cualquier error o mensaje raro en pantalla (sacad foto o captura).
- La hora aproximada de cada fallo (ayuda a encontrarlo en el log).
- Qué red usaba cada PC (wifi de casa, datos del celular, universidad...).

## Cómo encontrar y enviar `Player.log`

1. **Cerrar el juego** primero (el log se termina de escribir al cerrar).
2. Pulsar `Win + R`, pegar esto y pulsar Enter:
   `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Alien Farms`
3. Copiar `Player.log` y también `Player-prev.log` (es el de la vez anterior: **cada vez que se abre el juego, el log anterior pasa a `Player-prev.log` y el más antiguo se borra**). Si vais a reabrir el juego para el paso 8, copiad antes los logs.
4. Renombrarlos con la PC y el paso, por ejemplo `PC-A_host_paso1-7.log`, y mandarlos junto con las notas.

En el log deben aparecer, al arrancar y entrar en Online, líneas como `[UgsBootstrapper] Signed in as ...` (con `environment 'dev'`) y `Stats loaded ...`.
