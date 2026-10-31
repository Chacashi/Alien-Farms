using AlienFarms.Core;
using UnityEngine.InputSystem;

namespace AlienFarms.Data
{
    /// <summary>
    /// What one player chose before the match: its control scheme, the devices it used (both keyboard and mouse for
    /// KeyboardMouse) and its character. Devices may be null or empty (the player then joins by pressing a button).
    /// </summary>
    public readonly struct SessionSlot
    {
        public SessionSlot(PlayerId id, string controlScheme, InputDevice[] devices, CharacterDefinition character)
        {
            Id = id;
            ControlScheme = controlScheme;
            Devices = devices;
            Character = character;
        }

        public PlayerId Id { get; }
        public string ControlScheme { get; }
        public InputDevice[] Devices { get; }
        public CharacterDefinition Character { get; }

        public bool IsValid => Id != PlayerId.None;
    }
}
