using AlienFarms.Core;
using AlienFarms.Networking.Data;
using AlienFarms.Player;
using UnityEngine;

namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// Online <see cref="IPlayerRigLookup"/> for the shared menus (pause, results): only the avatar this machine owns is
    /// found, so a menu is routed to the local player and never touches remote avatars. Lives under OnlineModeRoot and is
    /// injected into the PlayerMenuRouter by the GameModeRouter.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalPlayerRigLookup : MonoBehaviour, IPlayerRigLookup
    {
        [Header("Config")]
        [SerializeField, Tooltip("Runtime anchor where the owned NetworkPlayer registers itself.")]
        private NetworkRuntimeAnchor _anchor;

        public bool TryGetRig(PlayerId id, out PlayerRig rig)
        {
            NetworkPlayer local = _anchor != null ? _anchor.LocalPlayer : null;
            rig = local != null && id != PlayerId.None && local.PlayerId == id ? local.Rig : null;
            return rig != null;
        }

        private void OnValidate()
        {
            if (_anchor == null)
            {
                Debug.LogWarning($"[{nameof(LocalPlayerRigLookup)}] Missing NetworkRuntimeAnchor on '{name}'.", this);
            }
        }
    }
}
