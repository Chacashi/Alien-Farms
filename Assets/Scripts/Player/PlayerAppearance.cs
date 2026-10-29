using UnityEngine;

namespace AlienFarms.Player
{
    /// <summary>Applies per-player visual identity (body material).</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAppearance : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Renderer of the player's body that receives the player color material.")]
        private Renderer _bodyRenderer;

        /// <summary>Assigns the shared material to the body renderer (no material instancing).</summary>
        public void ApplyMaterial(Material material)
        {
            if (_bodyRenderer == null || material == null)
            {
                return;
            }

            _bodyRenderer.sharedMaterial = material;
        }

        private void OnValidate()
        {
            if (_bodyRenderer == null)
            {
                Debug.LogWarning($"[{nameof(PlayerAppearance)}] Missing body renderer on '{name}'.", this);
            }
        }
    }
}
