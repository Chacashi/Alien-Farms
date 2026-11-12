using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AlienFarms.UI
{
    /// <summary>One animal type in a pen inventory HUD: icon plus count. Driven by <see cref="PenInventoryView"/>.</summary>
    [DisallowMultipleComponent]
    public sealed class PenInventorySlotView : MonoBehaviour
    {
        private const string CountFormat = "{0}";

        [Header("References")]
        [SerializeField, Tooltip("Image showing the animal type icon. Left empty in the scene, it takes the AnimalData icon.")]
        private Image _icon;

        [SerializeField, Tooltip("Text showing how many animals of this type are in the pen.")]
        private TMP_Text _count;

        /// <summary>Last count shown by this slot.</summary>
        public int Count { get; private set; }

        /// <summary>Shows the given count (negative values are clamped to 0).</summary>
        public void SetCount(int count)
        {
            Count = count < 0 ? 0 : count;
            if (_count != null)
            {
                _count.SetText(CountFormat, Count);
            }
        }

        /// <summary>Assigns the sprite only when the image has none, so a sprite set in the scene wins.</summary>
        public void SetIconIfEmpty(Sprite sprite)
        {
            if (_icon != null && _icon.sprite == null && sprite != null)
            {
                _icon.sprite = sprite;
            }
        }

        private void OnValidate()
        {
            if (_icon == null)
            {
                Debug.LogWarning($"[{nameof(PenInventorySlotView)}] Missing icon image on '{name}'.", this);
            }

            if (_count == null)
            {
                Debug.LogWarning($"[{nameof(PenInventorySlotView)}] Missing count text on '{name}'.", this);
            }
        }
    }
}
