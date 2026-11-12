using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// Course topic 2: the object takes the color of whoever owns it. Colors indicator renderers (a ring / halo child,
    /// never the object's own art) with the owner character's <see cref="CharacterTheme.Primary"/> through a
    /// <see cref="MaterialPropertyBlock"/> (no material instances). Server-owned (free, penned, dropped): hidden or
    /// neutral. Runs on every instance and only reacts to <see cref="NetworkCarriable.ViewChanged"/>; when the owner's
    /// avatar or character is not known yet (late join) it retries once per frame until it resolves (no allocation).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CarriableOwnerTint : MonoBehaviour
    {
        // URP Lit / Unlit / Simple Lit color property (the indicator material must use one of them).
        private const string BaseColorProperty = "_BaseColor";
        private static readonly int BaseColorId = Shader.PropertyToID(BaseColorProperty);

        [Header("References")]
        [SerializeField, Tooltip("Network adapter on this root (NetworkAnimal / NetworkBomb).")]
        private NetworkCarriable _carriable;

        [SerializeField, Tooltip("Indicator renderers to color (child ring / halo with a URP Lit or Unlit material). Not the object's own mesh.")]
        private Renderer[] _indicators = new Renderer[0];

        [Header("Look")]
        [SerializeField, Tooltip("Hide the indicators while the server owns the object (otherwise they show the neutral color).")]
        private bool _hideWhenServerOwned = true;

        [SerializeField, Tooltip("Color while the server owns the object (or the owner's character is not known yet).")]
        private Color _neutralColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        private MaterialPropertyBlock _block;
        private bool _pending;
        private bool _hasApplied;
        private bool _appliedVisible;
        private Color _appliedColor;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            if (_carriable == null)
            {
                TryGetComponent(out _carriable);
            }

            // Subscribed for the whole lifetime: this behaviour is only enabled while a retry is pending (its Update)
            // and must still hear the next change while disabled. Pooled instances keep the subscription.
            if (_carriable != null)
            {
                _carriable.ViewChanged += HandleViewChanged;
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (_carriable != null)
            {
                _carriable.ViewChanged -= HandleViewChanged;
            }
        }

        // Only runs while enabled, i.e. while the owner's avatar or character is not known yet.
        private void Update()
        {
            Refresh();
        }

        private void HandleViewChanged(NetworkCarriable carriable)
        {
            Refresh();
        }

        private void Refresh()
        {
            _pending = false;
            CarriableOwner owner = _carriable != null ? _carriable.ResolveOwner() : CarriableOwner.Server;

            switch (owner.Kind)
            {
                case CarriableOwnerKind.Player when TryGetSeatColor(owner, out Color color):
                    Apply(true, color);
                    break;
                case CarriableOwnerKind.Server:
                    Apply(!_hideWhenServerOwned, _neutralColor);
                    break;
                default:
                    // Owner's avatar / character not replicated here yet: neutral until it resolves.
                    _pending = true;
                    Apply(!_hideWhenServerOwned, _neutralColor);
                    break;
            }

            if (enabled != _pending)
            {
                enabled = _pending;
            }
        }

        private bool TryGetSeatColor(CarriableOwner owner, out Color color)
        {
            if (_carriable.Anchor != null && _carriable.Anchor.TryGetPlayer(owner.Seat, out NetworkPlayer player))
            {
                CharacterDefinition character = player.Character;
                if (character != null)
                {
                    color = character.Theme.Primary;
                    return true;
                }
            }

            color = _neutralColor;
            return false;
        }

        private void Apply(bool visible, Color color)
        {
            if (_hasApplied && visible == _appliedVisible && color == _appliedColor)
            {
                return;
            }

            _hasApplied = true;
            _appliedVisible = visible;
            _appliedColor = color;
            if (_indicators == null || _block == null)
            {
                return;
            }

            for (int i = 0; i < _indicators.Length; i++)
            {
                Renderer indicator = _indicators[i];
                if (indicator == null)
                {
                    continue;
                }

                indicator.enabled = visible;
                if (visible)
                {
                    indicator.GetPropertyBlock(_block);
                    _block.SetColor(BaseColorId, color);
                    indicator.SetPropertyBlock(_block);
                }
            }
        }

        private void OnValidate()
        {
            if (_carriable == null)
            {
                TryGetComponent(out _carriable);
            }

            if (_carriable == null)
            {
                Debug.LogWarning($"[{nameof(CarriableOwnerTint)}] Missing NetworkCarriable on '{name}'.", this);
            }

            if (_indicators == null || _indicators.Length == 0)
            {
                Debug.LogWarning($"[{nameof(CarriableOwnerTint)}] No indicator renderers on '{name}'; the owner color is not shown.", this);
                return;
            }

            for (int i = 0; i < _indicators.Length; i++)
            {
                Renderer indicator = _indicators[i];
                if (indicator == null)
                {
                    Debug.LogWarning($"[{nameof(CarriableOwnerTint)}] Indicator {i} is empty on '{name}'.", this);
                }
                else if (indicator.sharedMaterial != null && !indicator.sharedMaterial.HasProperty(BaseColorId))
                {
                    Debug.LogWarning($"[{nameof(CarriableOwnerTint)}] Indicator '{indicator.name}' on '{name}' uses a material without {BaseColorProperty} (use URP Lit / Unlit).", this);
                }
            }
        }
    }
}
