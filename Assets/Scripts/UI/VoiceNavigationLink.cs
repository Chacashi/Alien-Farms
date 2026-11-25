using System;
using UnityEngine;
using UnityEngine.UI;

namespace AlienFarms.UI
{
    /// <summary>
    /// One explicit navigation edge of a neighbour Selectable that points at the voice controls while they are
    /// visible and at a fallback while they are hidden (local mode or no voice service).
    /// </summary>
    [Serializable]
    public struct VoiceNavigationLink
    {
        public enum Direction
        {
            Up,
            Down,
            Left,
            Right
        }

        [SerializeField] private Selectable _from;
        [SerializeField] private Direction _direction;
        [SerializeField] private Selectable _whenAvailable;
        [SerializeField] private Selectable _whenHidden;

        public void Apply(bool available)
        {
            if (_from == null)
            {
                return;
            }

            Selectable target = available ? _whenAvailable : _whenHidden;
            Navigation navigation = _from.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            switch (_direction)
            {
                case Direction.Up: navigation.selectOnUp = target; break;
                case Direction.Down: navigation.selectOnDown = target; break;
                case Direction.Left: navigation.selectOnLeft = target; break;
                default: navigation.selectOnRight = target; break;
            }

            _from.navigation = navigation;
        }
    }
}
