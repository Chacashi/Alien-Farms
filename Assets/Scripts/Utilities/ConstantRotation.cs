using UnityEngine;

namespace AlienFarms.Utilities
{
    /// <summary>
    /// Spins its transform around a local axis at a constant rate (windmill blades, props). Purely visual: not
    /// networked, every machine rotates it locally.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConstantRotation : MonoBehaviour
    {
        [Header("Rotation")]
        [SerializeField, Tooltip("Local axis to spin around (normalized on validate; a zero vector falls back to forward).")]
        private Vector3 _localAxis = Vector3.forward;

        [SerializeField, Tooltip("Rotation speed in degrees per second (negative spins the other way).")]
        private float _degreesPerSecond = 30f;

        private void Update()
        {
            transform.Rotate(_localAxis, _degreesPerSecond * Time.deltaTime, Space.Self);
        }

        private void OnValidate()
        {
            if (_localAxis.sqrMagnitude < 1e-6f)
            {
                Debug.LogWarning($"[{nameof(ConstantRotation)}] Local axis on '{name}' is zero; using forward.", this);
                _localAxis = Vector3.forward;
                return;
            }

            _localAxis.Normalize();
        }
    }
}
