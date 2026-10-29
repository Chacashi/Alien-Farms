using AlienFarms.Core;
using UnityEngine;

namespace AlienFarms.UI
{
    /// <summary>Shows the "press a button to join" prompt while the lobby is not full.</summary>
    [DisallowMultipleComponent]
    public sealed class JoinPromptView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Root of the join prompt (text lives in the scene). Must not contain this component.")]
        private GameObject _promptRoot;

        [Header("Config")]
        [SerializeField, Min(1), Tooltip("Players needed before the prompt hides. Keep in sync with PlayerInputManager Max Player Count.")]
        private int _maxPlayers = 2;

        // Bit (int)PlayerId set while that player is joined; idempotent against repeated events.
        private int _joinedMask;

        private void OnEnable()
        {
            GameEvents.OnPlayerJoined += HandlePlayerJoined;
            GameEvents.OnPlayerLeft += HandlePlayerLeft;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerJoined -= HandlePlayerJoined;
            GameEvents.OnPlayerLeft -= HandlePlayerLeft;
        }

        private void HandlePlayerJoined(PlayerId playerId)
        {
            if (playerId == PlayerId.None)
            {
                return;
            }

            _joinedMask |= 1 << (int)playerId;
            Refresh();
        }

        private void HandlePlayerLeft(PlayerId playerId)
        {
            if (playerId == PlayerId.None)
            {
                return;
            }

            _joinedMask &= ~(1 << (int)playerId);
            Refresh();
        }

        private void Refresh()
        {
            if (_promptRoot != null)
            {
                _promptRoot.SetActive(CountJoined() < _maxPlayers);
            }
        }

        private int CountJoined()
        {
            int count = 0;
            for (int mask = _joinedMask; mask != 0; mask &= mask - 1)
            {
                count++;
            }

            return count;
        }

        private void OnValidate()
        {
            if (_promptRoot == null)
            {
                Debug.LogWarning($"[{nameof(JoinPromptView)}] Missing prompt root on '{name}'.", this);
            }
            else if (_promptRoot == gameObject || transform.IsChildOf(_promptRoot.transform))
            {
                Debug.LogWarning($"[{nameof(JoinPromptView)}] Prompt root on '{name}' contains this view; hiding it would stop listening to events.", this);
            }
        }
    }
}
