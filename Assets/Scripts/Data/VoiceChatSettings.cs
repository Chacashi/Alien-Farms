using AlienFarms.Core;
using UnityEngine;

namespace AlienFarms.Data
{
    [CreateAssetMenu(fileName = "VoiceChatSettings", menuName = "Alien Farms/Networking/Voice Chat Settings")]
    public sealed class VoiceChatSettings : ScriptableObject
    {
        [Header("Positional channel (Channel3DProperties)")]
        [Tooltip("Beyond this distance (m) the speaker is removed from the listener's channel.")]
        [Min(1)] [SerializeField] private int _audibleDistance = 30;
        [Tooltip("Up to this distance (m) the voice plays at full volume; attenuation starts after it.")]
        [Min(1)] [SerializeField] private int _conversationalDistance = 1;
        [Tooltip("How strongly the voice fades past the conversational distance (1 = realistic).")]
        [Min(0)] [SerializeField] private float _fadeIntensity = 1f;
        [Tooltip("Attenuation curve between conversational and audible distance.")]
        [SerializeField] private VoiceFadeModel _fadeModel = VoiceFadeModel.InverseByDistance;

        [Header("Position reporting")]
        [Tooltip("Set3DPosition calls per second (2-4 recommended, never every frame).")]
        [Range(1, 4)] [SerializeField] private int _positionUpdateHz = 3;
        [Tooltip("Listener ear height (m) above the avatar root.")]
        [Min(0)] [SerializeField] private float _listenerHeight = 1.6f;

        [Header("Session")]
        [Tooltip("Prefix for the channel name; the session Id is appended.")]
        [SerializeField] private string _channelPrefix = "af-";
        [Tooltip("Enable Vivox acoustic echo cancellation after login.")]
        [SerializeField] private bool _enableEchoCancellation = true;
        [Tooltip("Re-login attempts after Vivox fails to recover its connection.")]
        [Range(0, 5)] [SerializeField] private int _maxRecoverAttempts = 3;

        public int AudibleDistance => _audibleDistance;
        public int ConversationalDistance => _conversationalDistance;
        public float FadeIntensity => _fadeIntensity;
        public VoiceFadeModel FadeModel => _fadeModel;
        public int PositionUpdateHz => _positionUpdateHz;
        public string ChannelPrefix => _channelPrefix;
        public float ListenerHeight => _listenerHeight;
        public bool EnableEchoCancellation => _enableEchoCancellation;
        public int MaxRecoverAttempts => _maxRecoverAttempts;

        private void OnValidate()
        {
            _audibleDistance = Mathf.Max(1, _audibleDistance);
            _conversationalDistance = Mathf.Clamp(_conversationalDistance, 1, _audibleDistance);
        }
    }
}
