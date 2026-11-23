using UnityEngine;

namespace AlienFarms.Data
{
    [CreateAssetMenu(fileName = "UiSfxLibrary", menuName = "Alien Farms/Audio/UI Sfx Library")]
    public sealed class UiSfxLibrary : ScriptableObject
    {
        [Header("Create Room")]
        [SerializeField, Tooltip("Plays when an online room creation begins.")]
        private SoundCue _createRoom = new SoundCue();

        [Header("Join Room")]
        [SerializeField, Tooltip("Plays when an online room join begins.")]
        private SoundCue _joinRoom = new SoundCue();

        [Header("Character Confirm")]
        [SerializeField, Tooltip("Plays when a player locks a character pick.")]
        private SoundCue _characterConfirm = new SoundCue();

        [Header("Character Release")]
        [SerializeField, Tooltip("Plays when a player releases a character pick.")]
        private SoundCue _characterRelease = new SoundCue();

        [Header("Player Joined")]
        [SerializeField, Tooltip("Plays when another player joins.")]
        private SoundCue _playerJoined = new SoundCue();

        [Header("Connection Succeeded")]
        [SerializeField, Tooltip("Plays when an online session connects.")]
        private SoundCue _connectionSucceeded = new SoundCue();

        [Header("Connection Lost")]
        [SerializeField, Tooltip("Plays when a connection is lost.")]
        private SoundCue _connectionLost = new SoundCue();

        [Header("Match Suspended")]
        [SerializeField, Tooltip("Plays when a match is suspended.")]
        private SoundCue _matchSuspended = new SoundCue();

        [Header("Player Reconnected")]
        [SerializeField, Tooltip("Plays when a player reconnects.")]
        private SoundCue _playerReconnected = new SoundCue();

        [Header("Match Resumed")]
        [SerializeField, Tooltip("Plays when a match resumes.")]
        private SoundCue _matchResumed = new SoundCue();

        [Header("Match Paused")]
        [SerializeField, Tooltip("Plays when a match is paused.")]
        private SoundCue _matchPaused = new SoundCue();

        [Header("Timing")]
        [SerializeField, Min(0f), Tooltip("Seconds between 'reconnected' and 'match resumed'.")]
        private float _resumeAfterReconnectDelay = 0.6f;

        [SerializeField, Min(0f), Tooltip("Delay of the arrival notice when MainMenu loads after losing the connection.")]
        private float _arrivalCueDelay = 0.3f;

        public float ResumeAfterReconnectDelay => _resumeAfterReconnectDelay;
        public float ArrivalCueDelay => _arrivalCueDelay;

        public SoundCue Get(UiSfxCue cue) => cue switch
        {
            UiSfxCue.CreateRoom => _createRoom,
            UiSfxCue.JoinRoom => _joinRoom,
            UiSfxCue.CharacterConfirm => _characterConfirm,
            UiSfxCue.CharacterRelease => _characterRelease,
            UiSfxCue.PlayerJoined => _playerJoined,
            UiSfxCue.ConnectionSucceeded => _connectionSucceeded,
            UiSfxCue.ConnectionLost => _connectionLost,
            UiSfxCue.MatchSuspended => _matchSuspended,
            UiSfxCue.PlayerReconnected => _playerReconnected,
            UiSfxCue.MatchResumed => _matchResumed,
            UiSfxCue.MatchPaused => _matchPaused,
            _ => null
        };

        private void OnValidate()
        {
            ValidateCue(_createRoom, "Create Room");
            ValidateCue(_joinRoom, "Join Room");
            ValidateCue(_characterConfirm, "Character Confirm");
            ValidateCue(_characterRelease, "Character Release");
            ValidateCue(_playerJoined, "Player Joined");
            ValidateCue(_connectionSucceeded, "Connection Succeeded");
            ValidateCue(_connectionLost, "Connection Lost");
            ValidateCue(_matchSuspended, "Match Suspended");
            ValidateCue(_playerReconnected, "Player Reconnected");
            ValidateCue(_matchResumed, "Match Resumed");
            ValidateCue(_matchPaused, "Match Paused");
            _resumeAfterReconnectDelay = Mathf.Max(0f, _resumeAfterReconnectDelay);
            _arrivalCueDelay = Mathf.Max(0f, _arrivalCueDelay);
        }

        private void ValidateCue(SoundCue cue, string label)
        {
            cue?.Sanitize();
            WarnIfSilent(cue, label);
        }

        private void WarnIfSilent(SoundCue cue, string label)
        {
            if (cue == null || !cue.HasClips)
            {
                Debug.LogWarning($"[{nameof(UiSfxLibrary)}] '{name}' has no clips for {label}; the cue will be silent.", this);
            }
        }
    }
}
