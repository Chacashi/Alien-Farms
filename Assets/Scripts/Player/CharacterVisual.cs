using System;
using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.Player
{
    /// <summary>Pairs a character with its pre-built visual root inside the Player prefab.</summary>
    [Serializable]
    public struct CharacterVisual
    {
        [SerializeField, Tooltip("Character this visual represents.")]
        private CharacterDefinition _character;

        [SerializeField, Tooltip("Visual root (child of the player's Visual) activated for this character.")]
        private GameObject _root;

        public CharacterDefinition Character => _character;
        public GameObject Root => _root;
    }
}
