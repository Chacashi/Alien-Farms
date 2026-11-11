using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Utilities;
using UnityEngine;

namespace AlienFarms.Player
{
    /// <summary>
    /// Hold-to-aim for throws. Aiming is live: Aim held, grabbing enabled, not stunned and carrying something that can
    /// be thrown. While aiming, movement strafes facing the camera and the camera zooms in. The launch comes from the
    /// camera: planar forward for direction, pitch for elevation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAim : MonoBehaviour
    {
        // Height above the root from which the launch origin must be visible (same as the grabber's drop height).
        private const float ClearanceHeight = 0.6f;

        [Header("Config")]
        [SerializeField, Tooltip("Throw tuning used when the carried object has none of its own.")]
        private ThrowSettings _defaultThrowSettings;

        [Header("References")]
        // Name kept from when it was typed PlayerInputHandler, so existing prefab references stay wired.
        [SerializeField, Tooltip("Input source for this player: a component implementing IPlayerCommandSource (PlayerInputHandler).")]
        private MonoBehaviour _input;

        [SerializeField, Tooltip("Grabber on this root (what is carried).")]
        private PlayerGrabber _grabber;

        [SerializeField, Tooltip("Movement on this root (aim strafe, stun, inherited velocity).")]
        private PlayerMovement _movement;

        [SerializeField, Tooltip("Orbit camera rig (child CameraRig): aim direction and zoom.")]
        private ThirdPersonCamera _camera;

        private IGrabbable _cachedHeld;
        private IThrowable _heldThrowable;
        private bool _appliedAim;
        private int _environmentMask;
        private IPlayerCommandSource _commands;

        /// <summary>True right now if a throw would happen on release (evaluated live).</summary>
        public bool IsAiming => EvaluateAiming();

        /// <summary>The carried object if it is throwable, else null.</summary>
        public IThrowable HeldThrowable => ResolveHeldThrowable();

        /// <summary>Throw tuning of the carried object, else the default. Null only if neither exists.</summary>
        public ThrowSettings ActiveThrowSettings
        {
            get
            {
                IThrowable throwable = ResolveHeldThrowable();
                ThrowSettings own = throwable != null ? throwable.ThrowSettings : null;
                return own != null ? own : _defaultThrowSettings;
            }
        }

        private void Awake()
        {
            _environmentMask = LayerMask.GetMask(LayerNames.Environment);

            // An injected source (SetCommandSource before Awake) wins over the serialized one.
            if (_commands == null)
            {
                _commands = _input as IPlayerCommandSource;
            }
        }

        /// <summary>Replaces the input source (e.g. a network-fed one). Null means Aim is never held.</summary>
        public void SetCommandSource(IPlayerCommandSource source)
        {
            _commands = source;
        }

        private void Update()
        {
            bool aiming = EvaluateAiming();
            if (aiming != _appliedAim)
            {
                ApplyAimMode(aiming);
            }
        }

        private void OnDisable()
        {
            ApplyAimMode(false);
            _cachedHeld = null;
            _heldThrowable = null;
        }

        /// <summary>
        /// Launch origin (the carried object's position) and velocity for the current aim; shared by the throw and its
        /// preview. False when not aiming, without throw settings, or when a wall stands between the player and the
        /// origin: then no arc is shown and releasing Grab does a normal drop (which finds a free spot).
        /// </summary>
        public bool TryGetLaunch(out Vector3 origin, out Vector3 velocity)
        {
            if (_camera == null)
            {
                origin = Vector3.zero;
                velocity = Vector3.zero;
                return false;
            }

            return TryGetLaunchWithView(_camera.Yaw, _camera.Pitch, out origin, out velocity);
        }

        /// <summary>
        /// Same as <see cref="TryGetLaunch"/> but for an explicit orbit view (deg) instead of the camera's current one
        /// (e.g. the view a remote player reported for its throw).
        /// </summary>
        public bool TryGetLaunchWithView(float yaw, float pitch, out Vector3 origin, out Vector3 velocity)
        {
            origin = Vector3.zero;
            velocity = Vector3.zero;

            if (!EvaluateAiming())
            {
                return false;
            }

            ThrowSettings settings = ActiveThrowSettings;
            if (settings == null)
            {
                return false;
            }

            float elevation = BallisticMath.ElevationFromPitch(
                pitch,
                settings.ReferenceCameraPitch,
                settings.BaseElevation,
                settings.PitchInfluence,
                settings.MinElevation,
                settings.MaxElevation);

            Vector3 launchOrigin = _heldThrowable.Position;
            Vector3 clearanceStart = transform.position + Vector3.up * ClearanceHeight;
            if (Physics.Linecast(clearanceStart, launchOrigin, _environmentMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            origin = launchOrigin;
            // Heavy objects fly shorter; the preview uses this same launch, so it shows the real distance.
            float speed = settings.LaunchSpeed * BallisticMath.SpeedScaleForDistance(_heldThrowable.ThrowDistanceMultiplier);
            Vector3 planarForward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            velocity = BallisticMath.LaunchVelocity(planarForward, elevation, speed);
            if (_movement != null)
            {
                velocity += _movement.Velocity * settings.InheritVelocityFactor;
            }

            return true;
        }

        private bool EvaluateAiming()
        {
            if (_commands == null || _grabber == null || _camera == null)
            {
                return false;
            }

            if (!_grabber.IsInputEnabled || !_commands.IsAimHeld || (_movement != null && _movement.IsStunned))
            {
                return false;
            }

            IThrowable throwable = ResolveHeldThrowable();
            return throwable != null && throwable.CanBeThrown;
        }

        /// <summary>The carried object as <see cref="IThrowable"/>; the cast is redone only when the held object changes.</summary>
        private IThrowable ResolveHeldThrowable()
        {
            if (_grabber == null)
            {
                return null;
            }

            IGrabbable held = _grabber.Held;
            if (!ReferenceEquals(held, _cachedHeld))
            {
                _cachedHeld = held;
                _heldThrowable = held as IThrowable;
            }

            // Interface references bypass Unity's overloaded null check.
            if (_heldThrowable is Object unityObject && unityObject == null)
            {
                return null;
            }

            return _heldThrowable;
        }

        private void ApplyAimMode(bool aiming)
        {
            _appliedAim = aiming;

            if (_movement != null)
            {
                _movement.SetAimMode(aiming);
            }

            if (_camera != null)
            {
                _camera.SetAimMode(aiming);
            }
        }

        private void OnValidate()
        {
            if (_defaultThrowSettings == null)
            {
                Debug.LogWarning($"[{nameof(PlayerAim)}] Missing default ThrowSettings on '{name}'.", this);
            }

            if (_input == null && TryGetComponent(out IPlayerCommandSource source))
            {
                _input = source as MonoBehaviour;
            }

            if (_grabber == null)
            {
                TryGetComponent(out _grabber);
            }

            if (_movement == null)
            {
                TryGetComponent(out _movement);
            }

            if (_input == null)
            {
                Debug.LogWarning($"[{nameof(PlayerAim)}] Missing input source on '{name}'.", this);
            }
            else if (!(_input is IPlayerCommandSource))
            {
                Debug.LogWarning($"[{nameof(PlayerAim)}] Input source '{_input.GetType().Name}' on '{name}' does not implement {nameof(IPlayerCommandSource)}.", this);
            }

            if (_grabber == null)
            {
                Debug.LogWarning($"[{nameof(PlayerAim)}] Missing PlayerGrabber on '{name}'.", this);
            }

            if (_camera == null)
            {
                Debug.LogWarning($"[{nameof(PlayerAim)}] Missing ThirdPersonCamera on '{name}'.", this);
            }
        }
    }
}
