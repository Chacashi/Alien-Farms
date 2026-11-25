using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Networking.Data;
using Unity.Services.Vivox;
using UnityEngine;

namespace AlienFarms.Networking.Voice
{
    internal sealed class VoicePositionReporter
    {
        private readonly NetworkRuntimeAnchor _anchor;
        private readonly VoicePositionThrottle _throttle;
        private readonly float _listenerHeight;
        private Transform _previousRig;

        public VoicePositionReporter(NetworkRuntimeAnchor anchor, VoiceChatSettings settings)
        {
            _anchor = anchor;
            _throttle = new VoicePositionThrottle(settings.PositionUpdateHz);
            _listenerHeight = settings.ListenerHeight;
        }

        public void Reset() => _throttle.Reset();

        public void Report(IVivoxService sdk, VoiceStatus status, string joinedName, double now)
        {
            if (status != VoiceStatus.InChannel || joinedName == null || !sdk.IsLoggedIn)
            {
                return;
            }

            Transform rig = LocalRig();
            if (!ReferenceEquals(rig, _previousRig))
            {
                _previousRig = rig;
                Reset();
            }

            if (!_throttle.ShouldSend(now))
            {
                return;
            }

            Vector3 position = Vector3.zero;
            Vector3 forward = Vector3.forward;
            if (rig != null)
            {
                position = rig.position + Vector3.up * _listenerHeight;
                Camera camera = _anchor.ViewCamera;
                Vector3 facing = camera != null ? camera.transform.forward : rig.forward;
                facing.y = 0f;
                if (facing.sqrMagnitude > 0.0001f)
                {
                    forward = facing.normalized;
                }
            }

            sdk.Set3DPosition(position, position, forward, Vector3.up, joinedName);
        }

        public float RivalDistance(PlayerId seat)
        {
            Transform local = LocalRig();
            if (local == null || !_anchor.TryGetPlayer(seat, out var rival) || rival.Rig == null)
            {
                return -1f;
            }

            return Vector3.Distance(local.position, rival.Rig.transform.position);
        }

        private Transform LocalRig()
        {
            var player = _anchor.LocalPlayer;
            return player != null && player.Rig != null ? player.Rig.transform : null;
        }
    }
}
