using AlienFarms.Networking.Diagnostics;
using TMPro;
using UnityEngine;

namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// Course topic 2: world-space debug label over a carriable object, "Owner: P2 · IsOwner ✓ · HasAuthority ✗" (owner
    /// as resolved by <see cref="NetworkCarriable.ResolveOwner"/>; IsOwner / HasAuthority of THIS machine). Shown while
    /// somebody claims the object or the server does not own it, and only while <see cref="OwnershipLabelToggle"/>
    /// (default: <see cref="AlienFarms.Data.NetworkSettings.ShowOwnershipLabels"/>) allows it. The text is rebuilt only
    /// on <see cref="NetworkCarriable.ViewChanged"/> (cached strings, no allocation); while shown it faces the anchor's
    /// view camera (local player or spectator) in LateUpdate. In NGO client-server HasAuthority is true only on the server, whoever owns it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CarriableOwnershipLabel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Network adapter on this root (NetworkAnimal / NetworkBomb).")]
        private NetworkCarriable _carriable;

        [SerializeField, Tooltip("World-space TextMeshPro (3D) on its own child above the object; that child is activated only while the label shows.")]
        private TMP_Text _text;

        [SerializeField, Tooltip("Transform turned towards the camera (defaults to the text's own transform).")]
        private Transform _billboard;

        [Header("Look")]
        [SerializeField, Tooltip("Mark for true (e.g. a check mark, only if the label font contains that glyph).")]
        private string _yesMark = "Y";

        [SerializeField, Tooltip("Mark for false (e.g. a cross, only if the label font contains that glyph).")]
        private string _noMark = "N";

        private OwnershipLabelCache _cache;
        private string _shownText;
        private bool _visible;
        private bool _pending;

        private void Awake()
        {
            if (_carriable == null)
            {
                TryGetComponent(out _carriable);
            }

            if (_billboard == null && _text != null)
            {
                _billboard = _text.transform;
            }

            _cache = new OwnershipLabelCache(_yesMark, _noMark);

            // Subscribed for the whole lifetime: this behaviour disables itself while there is nothing to tick
            // (hidden and resolved), and must still hear the next change. Pooled instances keep the subscription.
            if (_carriable != null)
            {
                _carriable.ViewChanged += HandleViewChanged;
            }

            OwnershipLabelToggle.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_carriable != null)
            {
                _carriable.ViewChanged -= HandleViewChanged;
            }

            OwnershipLabelToggle.Changed -= Refresh;
        }

        private void Update()
        {
            if (_pending)
            {
                Refresh();
            }
        }

        private void LateUpdate()
        {
            if (!_visible || _billboard == null)
            {
                return;
            }

            Camera view = ResolveViewCamera();
            if (view != null)
            {
                // Parallel to the view plane: readable from any angle and cheaper than a look-at.
                _billboard.rotation = view.transform.rotation;
            }
        }

        private void HandleViewChanged(NetworkCarriable carriable)
        {
            Refresh();
        }

        private void Refresh()
        {
            _pending = false;
            if (_text == null || _carriable == null)
            {
                SetTicking(false);
                return;
            }

            bool enabledByToggle = OwnershipLabelToggle.IsVisible(_carriable.Settings == null || _carriable.Settings.ShowOwnershipLabels);
            CarriableOwner owner = _carriable.ResolveOwner();
            bool visible = enabledByToggle && _carriable.IsInPlay && CarriableOwnerRules.IsLabelShown(owner, _carriable.Claimant, !_carriable.IsServer);

            if (visible)
            {
                // A client owner whose avatar is not known here yet shows "?" until it resolves.
                _pending = owner.Kind == CarriableOwnerKind.Unresolved;
                string text = _cache.Get(owner, _carriable.IsOwner, _carriable.HasAuthority);
                if (!ReferenceEquals(text, _shownText))
                {
                    _shownText = text;
                    _text.SetText(text);
                }
            }

            _visible = visible;
            GameObject labelObject = _text.gameObject;
            if (labelObject.activeSelf != visible)
            {
                labelObject.SetActive(visible);
            }

            SetTicking(_visible || _pending);
        }

        /// <summary>Update (retry) and LateUpdate (billboard) only run while the label shows or is unresolved.</summary>
        private void SetTicking(bool ticking)
        {
            if (enabled != ticking)
            {
                enabled = ticking;
            }
        }

        // The local avatar's camera, or the spectator's: a spectator sees the labels too.
        private Camera ResolveViewCamera()
        {
            return _carriable.Anchor != null ? _carriable.Anchor.ViewCamera : null;
        }

        private void OnValidate()
        {
            if (_carriable == null)
            {
                TryGetComponent(out _carriable);
            }

            if (_carriable == null)
            {
                Debug.LogWarning($"[{nameof(CarriableOwnershipLabel)}] Missing NetworkCarriable on '{name}'.", this);
            }

            if (_text == null)
            {
                Debug.LogWarning($"[{nameof(CarriableOwnershipLabel)}] Missing TextMeshPro label on '{name}'.", this);
            }
            else if (_text.transform == transform)
            {
                Debug.LogWarning($"[{nameof(CarriableOwnershipLabel)}] The label on '{name}' must be a child: the billboard would rotate the whole object.", this);
            }

            if (_billboard == transform)
            {
                Debug.LogWarning($"[{nameof(CarriableOwnershipLabel)}] Billboard on '{name}' cannot be the object's root.", this);
            }
        }
    }
}
