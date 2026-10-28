using UnityEngine;

namespace AlienFarms.Core
{
    /// <summary>Holds which local player owns this GameObject hierarchy.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerIdentity : MonoBehaviour
    {
        [SerializeField, Tooltip("Player that owns this character.")]
        private PlayerId _id = PlayerId.Player1;

        public PlayerId Id => _id;

        public void Initialize(PlayerId id) => _id = id;
    }
}
