using System;
using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>Colors that identify a playable character in the HUD and menus.</summary>
    [Serializable]
    public struct CharacterTheme
    {
        [SerializeField, Tooltip("Main body color (portrait fallback, frames).")]
        private Color _primary;

        [SerializeField, Tooltip("Bright highlight color (score, stripes, winner text).")]
        private Color _secondary;

        [SerializeField, Tooltip("Color for the character's name text.")]
        private Color _text;

        [SerializeField, Tooltip("Dark accent color (outlines, details).")]
        private Color _accent;

        public CharacterTheme(Color primary, Color secondary, Color text, Color accent)
        {
            _primary = primary;
            _secondary = secondary;
            _text = text;
            _accent = accent;
        }

        public Color Primary => _primary;
        public Color Secondary => _secondary;
        public Color Text => _text;
        public Color Accent => _accent;
    }
}
