using UnityEngine;

namespace AlienFarms.Player
{
    /// <summary>Shows a spinning ring of stars over the player while it is stunned (blast or tackle).</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerStunIndicator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Movement on this root (stun state).")]
        private PlayerMovement _movement;

        [SerializeField, Tooltip("Child shown while stunned (inactive by default, on the root so it does not turn with the visual).")]
        private Transform _indicator;

        [Header("Tuning")]
        [SerializeField, Min(0f), Tooltip("Spin speed of the indicator around Y (deg/s).")]
        private float _spinSpeed = 180f;

        private bool _shown;

        private void OnEnable()
        {
            _shown = false;
            SetShown(false);
        }

        private void Update()
        {
            if (_movement == null || _indicator == null)
            {
                return;
            }

            bool stunned = _movement.IsStunned;
            if (stunned != _shown)
            {
                SetShown(stunned);
            }

            if (_shown && _spinSpeed > 0f)
            {
                _indicator.Rotate(0f, _spinSpeed * Time.deltaTime, 0f, Space.World);
            }
        }

        private void SetShown(bool shown)
        {
            _shown = shown;
            if (_indicator != null && _indicator.gameObject.activeSelf != shown)
            {
                _indicator.gameObject.SetActive(shown);
            }
        }

        private void OnValidate()
        {
            if (_movement == null)
            {
                TryGetComponent(out _movement);
            }

            if (_indicator == null)
            {
                Debug.LogWarning($"[{nameof(PlayerStunIndicator)}] Missing indicator on '{name}'.", this);
            }
        }
    }
}
