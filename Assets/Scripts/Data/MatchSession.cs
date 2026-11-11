using System;
using AlienFarms.Core;
using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>
    /// Runtime-only ScriptableObject that carries the players' choices (devices, scheme, character) from the
    /// MainMenu to the gameplay scene. Nothing is serialized: data written during a previous play session is
    /// discarded through a generation counter bumped on every play start (works without domain reload).
    /// Online, <see cref="Mode"/> is <see cref="GameMode.Online"/> and the local player's pick is <see cref="LocalCharacter"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "MatchSession", menuName = "Alien Farms/Match Session (Runtime)")]
    public sealed class MatchSession : ScriptableObject
    {
        public const int RequiredPlayers = SessionData.MaxPlayers;

        private static int s_generation;

        [NonSerialized] private readonly SessionData _data = new SessionData();
        [NonSerialized] private int _generation = -1;
        [NonSerialized] private GameMode _mode = GameMode.Local;
        [NonSerialized] private CharacterDefinition _localCharacter;

        /// <summary>Both players have a slot written during this play session.</summary>
        public bool IsReady => IsCurrent && _data.IsComplete(RequiredPlayers);

        /// <summary>How the gameplay scene must be driven. Local unless set during this play session.</summary>
        public GameMode Mode => IsCurrent ? _mode : GameMode.Local;

        /// <summary>Online: character picked by the player on this machine (null when none was picked).</summary>
        public CharacterDefinition LocalCharacter => IsCurrent ? _localCharacter : null;

        private bool IsCurrent => _generation == s_generation;

        private void OnEnable()
        {
            // Nothing references this asset between scene loads while its data is in flight: keep it in memory.
            hideFlags |= HideFlags.DontUnloadUnusedAsset;
        }

        public void SetSlot(SessionSlot slot)
        {
            EnsureCurrent();
            _data.Set(slot);
        }

        public void SetMode(GameMode mode)
        {
            EnsureCurrent();
            _mode = mode;
        }

        public void SetLocalCharacter(CharacterDefinition character)
        {
            EnsureCurrent();
            _localCharacter = character;
        }

        public bool TryGetSlot(PlayerId id, out SessionSlot slot)
        {
            if (!IsCurrent)
            {
                slot = default;
                return false;
            }

            return _data.TryGet(id, out slot);
        }

        public void Clear()
        {
            _data.Clear();
            _mode = GameMode.Local;
            _localCharacter = null;
            _generation = s_generation;
        }

        private void EnsureCurrent()
        {
            if (!IsCurrent)
            {
                Clear();
            }
        }

        // Enter Play Mode without domain reload keeps this asset loaded: invalidate data from the previous session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_generation++;
        }
    }
}
