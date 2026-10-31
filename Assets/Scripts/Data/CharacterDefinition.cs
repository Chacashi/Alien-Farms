using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>
    /// A playable character: display name, optional portrait and color theme. The 3D visual is a pre-existing child of
    /// the Player prefab matched by this asset (see PlayerAppearance), so no prefab is referenced here.
    /// </summary>
    [CreateAssetMenu(fileName = "Character", menuName = "Alien Farms/Character Definition")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Name shown in the HUD, pause and results screens.")]
        private string _displayName = "Character";

        [SerializeField, Tooltip("Optional portrait. When empty, UI shows a square in the theme's primary color.")]
        private Sprite _portrait;

        [SerializeField, Tooltip("Colors used by the HUD and menus for this character.")]
        private CharacterTheme _theme = new CharacterTheme(Color.white, Color.white, Color.white, Color.black);

        public string DisplayName => _displayName;
        public Sprite Portrait => _portrait;
        public CharacterTheme Theme => _theme;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_displayName))
            {
                Debug.LogWarning($"[{nameof(CharacterDefinition)}] '{name}' has an empty display name.", this);
            }
        }
    }
}
