---
tags: [modulo, modulo/utilities]
---
# Módulo Utilities

Carpeta `Assets/Scripts/Utilities/` · 5 tipos. Las cajas punteadas son tipos de otros módulos.

Leyenda: `--|>` hereda · `..|>` implementa · `-->` tiene un campo/propiedad de ese tipo. Se muestran campos `[SerializeField]` y la API pública.

## Diagrama de clases
```mermaid
classDiagram
  direction LR
  class BallisticMath {
    <<static>>
    +ElevationFromPitch()$ float
    +LaunchVelocity(Vector3 planarForward, float elevationDeg, float speed)$ Vector3
    +SpeedScaleForDistance(float distanceMultiplier)$ float
    +Step(Vector3 position, Vector3 velocity, Vector3 gravity, float dt)$ void
    +PreviewPointCount(float maxTime, float dt, int stepsPerPoint)$ int
  }
  class ConstantRotation {
    <<MonoBehaviour>>
    -Vector3 _localAxis
    -float _degreesPerSecond
  }
  class IRandomSource {
    <<interface>>
    +Range(int minInclusive, int maxExclusive) int
    +Value01() float
  }
  class PlanarMath {
    <<static>>
    +KnockbackDirection(Vector3 center, Vector3 target, Vector3 fallback)$ Vector3
  }
  class SystemRandomSource {
    +Range(int minInclusive, int maxExclusive) int
    +Value01() float
  }
  SystemRandomSource ..|> IRandomSource
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Tipos

| Tipo | Clase | Descripción |
|---|---|---|
| [[BallisticMath]] | static | Pure ballistic helpers shared by the throw and its preview. `Step` integrates exactly like PhysX (semi-implicit Euler), so a preview stepped… |
| [[ConstantRotation]] | MonoBehaviour | Spins its transform around a local axis at a constant rate (windmill blades, props). Purely visual: not networked, every machine rotates it … |
| [[IRandomSource]] | interface | Injectable random number source, so pure logic can be seeded and tested deterministically (never touches the global UnityEngine.Random state… |
| [[PlanarMath]] | static | Pure geometric helpers on the horizontal (XZ) plane. |
| [[SystemRandomSource]] | class | `IRandomSource` backed by its own `System.Random`: the same seed always yields the same sequence, independently of anything else using Unity… |