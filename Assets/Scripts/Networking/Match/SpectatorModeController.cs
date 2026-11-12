using AlienFarms.Networking.Data;
using AlienFarms.UI;
using UnityEngine;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// Spectator mode of the online Prototype (on OnlineModeRoot). While <see cref="NetworkSeatPresence.IsLocalSpectator"/>
    /// is true on this machine (a pure client admitted without a seat), activates the spectator rig (overview camera,
    /// event system and UI input module, placed in the scene), shows the spectator panel and registers the camera on the
    /// anchor so name tags and labels face it. Undone when spectating ends; disabling this component only unregisters
    /// the camera from the anchor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpectatorModeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Runtime anchor: the spectator camera is registered as the view camera.")]
        private NetworkRuntimeAnchor _anchor;

        [SerializeField, Tooltip("Seat presence on MatchNetwork (tells whether this machine spectates).")]
        private NetworkSeatPresence _presence;

        [SerializeField, Tooltip("SpectatorRig: camera + EventSystem + InputSystemUIInputModule, saved inactive.")]
        private GameObject _spectatorRig;

        [SerializeField, Tooltip("Overview camera inside the spectator rig.")]
        private Camera _spectatorCamera;

        [SerializeField, Tooltip("UI_SpectatorPanel view ('You are watching' + Leave).")]
        private SpectatorPanelView _panel;

        private bool _applied;
        private bool _hasApplied;

        /// <summary>True while spectator mode is on here.</summary>
        public bool IsSpectating => _hasApplied && _applied;

        private void OnEnable()
        {
            if (_presence != null)
            {
                _presence.LocalSpectatorChanged += Apply;
            }

            Apply(_presence != null && _presence.IsLocalSpectator);
        }

        private void OnDisable()
        {
            if (_presence != null)
            {
                _presence.LocalSpectatorChanged -= Apply;
            }

            // Only the anchor outlives the scene: the rig and the panel are left alone (they may be unloading with it),
            // and OnEnable applies the present state again.
            if (_anchor != null && _spectatorCamera != null)
            {
                _anchor.ClearSpectatorCamera(_spectatorCamera);
            }

            _hasApplied = false;
        }

        private void Apply(bool spectating)
        {
            if (_hasApplied && _applied == spectating)
            {
                return;
            }

            _hasApplied = true;
            _applied = spectating;

            // Rig first: the panel focuses its button in the rig's event system.
            if (_spectatorRig != null && _spectatorRig.activeSelf != spectating)
            {
                _spectatorRig.SetActive(spectating);
            }

            if (_panel != null)
            {
                if (spectating)
                {
                    _panel.Show();
                }
                else
                {
                    _panel.Hide();
                }
            }

            if (_anchor == null || _spectatorCamera == null)
            {
                return;
            }

            if (spectating)
            {
                _anchor.SetSpectatorCamera(_spectatorCamera);
            }
            else
            {
                _anchor.ClearSpectatorCamera(_spectatorCamera);
            }
        }

        private void OnValidate()
        {
            if (_anchor == null || _presence == null)
            {
                Debug.LogWarning($"[{nameof(SpectatorModeController)}] Missing NetworkRuntimeAnchor or NetworkSeatPresence on '{name}'.", this);
            }

            if (_spectatorRig == null || _spectatorCamera == null || _panel == null)
            {
                Debug.LogWarning($"[{nameof(SpectatorModeController)}] Missing spectator rig, camera or panel on '{name}'.", this);
            }
            else if (!_spectatorCamera.transform.IsChildOf(_spectatorRig.transform))
            {
                Debug.LogWarning($"[{nameof(SpectatorModeController)}] The spectator camera on '{name}' should live inside the spectator rig.", this);
            }

            if (_spectatorRig != null && (_spectatorRig == gameObject || transform.IsChildOf(_spectatorRig.transform)))
            {
                Debug.LogWarning($"[{nameof(SpectatorModeController)}] The spectator rig on '{name}' contains this controller: it would disable itself.", this);
            }
        }
    }
}
