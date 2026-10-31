# Alien Farms — Guía del proyecto (Orquestador)

Party game 1v1 **multijugador local** (Unity 6000.3, URP, New Input System). GDD en `Docs/`.
Loop: cada jugador lleva animales desde la *Animal Zone* central a su zona; timer de 5:00; gana quien tenga más animales en su zona.

## Rol de la sesión principal: Orquestador

La sesión principal **planifica, delega, integra y verifica**. No escribe sistemas grandes de golpe.

1. Leer el GDD / el pedido y partirlo en tareas pequeñas y verificables.
2. Delegar a subagentes (`.claude/agents/`):
   | Agente | Cuándo |
   |---|---|
   | `unity-architect` | Diseñar un sistema nuevo antes de codear (clases, eventos, SOs, dependencias). |
   | `unity-gameplay-programmer` | Escribir/modificar C# de runtime (player, animales, zonas, match, input). |
   | `unity-scene-builder` | Montar escena, prefabs, Canvas/HUD, materiales, cámaras y cablear referencias en el Inspector vía MCP. |
   | `unity-code-reviewer` | Revisar todo código nuevo contra estas reglas antes de dar la tarea por terminada. |
   | `unity-qa-tester` | Compilar, leer consola, correr tests EditMode/PlayMode, probar en Play Mode. |
3. Flujo estándar de una feature: **architect → gameplay-programmer → scene-builder → code-reviewer → qa-tester**. Tareas triviales pueden saltarse pasos.
4. Tareas independientes se lanzan en paralelo; las que tocan los mismos archivos/escena, en serie (dos agentes nunca editan la misma escena a la vez).
5. Al final: verificar que compila sin errores ni warnings nuevos (`read_console`) y resumir al usuario.

## Estructura de carpetas (obligatoria)

```
Assets/
  Scripts/
    Core/          # Bootstrap, GameEvents estáticos, interfaces base, utilidades genéricas
    Input/         # Wrappers del Input System, PlayerInputHandler
    Player/        # Movimiento, sprint, agarre, cámara del jugador
    Animals/       # Animal base + derivados, IA, spawner, pool
    Zones/         # PlayerZone, AnimalZone
    Match/         # MatchManager, timer, puntaje, estados de partida
    UI/            # HUD, menús, pausa, resultados (solo lógica; el Canvas vive en la escena)
    Menu/          # Flujo del MainMenu: título, lobby, selección de personaje
    Items/         # Ítems de partida (bomba stunner)
    Audio/
    Data/          # Clases ScriptableObject (definiciones, no instancias)
    Utilities/     # Extensiones, ObjectPool genérico, helpers
    Editor/        # Solo código de editor (asmdef Editor-only)
  ScriptableObjects/  # Instancias .asset (AnimalData, MatchSettings, PlayerSettings...)
  Prefabs/            # Subcarpetas por dominio: Player/, Animals/, Zones/, UI/, Environment/
  Scenes/
  Materials/
  Art/                # Models/, Textures/, Animations/, Sprites/
  Audio/              # Music/, SFX/
  Input/              # .inputactions
  Settings/           # URP y volúmenes (ya existente)
```
- Nunca dejar assets sueltos en la raíz de `Assets/`. Si falta una carpeta, se crea en el lugar correcto.
- Namespaces reflejan la carpeta: `AlienFarms.Player`, `AlienFarms.Animals`, `AlienFarms.Core`, etc.

## Reglas de código (nivel senior)

**Prohibido**
- `FindObjectOfType`, `FindFirstObjectByType`, `FindObjectsByType`, `GameObject.Find*`, `FindWithTag`, `Resources.Load` para dependencias de gameplay.
- `GetComponent` en `Update`/`FixedUpdate`; cachear en `Awake` o usar `TryGetComponent` en eventos de física.
- Crear Canvas, UI, cámaras, luces o geometría de nivel por código. **Todo lo visible y su layout vive en la escena o en prefabs**, montado vía MCP.
- Posicionar objetos de nivel por código: los puntos (spawns, zonas) son `Transform`/colliders colocados en la escena y referenciados con `[SerializeField]`.
- Old Input Manager (`Input.GetKey`, `Input.GetAxis`).
- Campos `public` para exponer al Inspector; strings mágicos de tags/escenas/animator (usar constantes o `Animator.StringToHash`).
- Singletons como atajo para todo. Se permiten solo con justificación (p. ej. un servicio de audio).
- `Instantiate`/`Destroy` en caliente para objetos frecuentes (animales, VFX) → object pooling (`UnityEngine.Pool.ObjectPool<T>`).
- Allocations por frame (LINQ, lambdas que capturan, `string` concatenado en Update).

**Obligatorio**
- Dependencias por `[SerializeField] private` asignadas en el Inspector (o inyectadas por un componente composition-root de la escena).
- **New Input System** con `PlayerInput` + `PlayerInputManager` para multijugador local (teclado+mouse y mandos). Un `PlayerInput` por jugador; nada de leer dispositivos globales.
- **ScriptableObjects** para datos/config (velocidades, duración del match, tipos de animal) y, cuando aplique, canales de eventos.
- **Eventos estáticos** (`public static event Action<...>`) para comunicación desacoplada entre sistemas (ej. `GameEvents.OnAnimalScored(PlayerId, Animal)`). Siempre llevan el `PlayerId` en el payload (hay 2 jugadores). Suscribir en `OnEnable`, desuscribir en `OnDisable`. Resetear estáticos con `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` (Enter Play Mode sin domain reload).
- Herencia/interfaces con sentido: `IGrabbable`, `Animal` abstracto → `Chicken`, `Cow`...; estados de match con State pattern; Strategy/Observer/Pool/Factory donde resuelvan un problema real.
- Lambdas para callbacks cortos sin captura costosa; preferir method groups cuando haya que desuscribir.
- Física en `FixedUpdate`, movimiento con `Rigidbody`/`CharacterController` coherente; layers y matriz de colisiones en vez de chequeos por tag.
- `[RequireComponent]`, `[DisallowMultipleComponent]`, `[Header]`, `[Tooltip]`, `[Min]/[Range]` para un Inspector claro; validar referencias en `OnValidate`.
- Assembly definitions por módulo (`AlienFarms.Runtime`, `AlienFarms.Editor`, `AlienFarms.Tests`).
- Estilo: C# estándar de Microsoft. `PascalCase` tipos/métodos/propiedades, `_camelCase` campos privados, `camelCase` locales. Una clase por archivo, nombre de archivo = nombre de clase. Código e identificadores en inglés; comentarios breves solo donde aportan.

## Workflow con Unity MCP
- Tras crear/editar scripts: esperar compilación (`mcpforunity://editor/state` → `data.advice.ready_for_tools`) y revisar `read_console` antes de usar los tipos nuevos.
- Guardar la escena después de modificarla.
- Verificar APIs dudosas con `unity_reflect` / `unity_docs` antes de escribirlas.

## Paquetes
Se pueden instalar los que hagan falta (con `manage_packages`), justificándolo. Candidatos: `com.unity.cinemachine` (cámaras tercera persona / split-screen), TextMeshPro (incluido en ugui 2.x).
