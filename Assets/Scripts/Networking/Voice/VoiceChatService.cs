using System;
using System.Threading;
using System.Threading.Tasks;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Networking.Data;
using AlienFarms.Networking.Session;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;

namespace AlienFarms.Networking.Voice
{
    [DisallowMultipleComponent]
    public sealed class VoiceChatService : MonoBehaviour
    {
        [SerializeField] private VoiceChatSettings _settings;
        [SerializeField] private VoiceChatChannel _voice;
        [SerializeField] private OnlineSessionChannel _onlineSession;
        [SerializeField] private OnlineSessionService _sessionService;
        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private NetworkRuntimeAnchor _anchor;

        // Vivox survives NetworkRoot replacement. Finish the old root's leave before the new root can join.
        private static SemaphoreSlim OperationGate = new SemaphoreSlim(1);
        private readonly VoiceParticipantBinding[] _bindings = new VoiceParticipantBinding[2];
        private VoicePositionReporter _reporter;
        private IVivoxService _sdk;
        private OnlineSessionStatus _sessionStatus;
        private string _sessionId;
        private string _desired;
        private string _joining;
        private string _joined;
        private bool _active;
        private bool _busy;
        private bool _dirty;
        private bool _quitting;
        private bool _clientStopped;
        private bool _needsSweep;
        private bool _loginConfigured;
        private bool _recovering;
        private bool _recoveryFailed;
        private bool _awaitingRecoveryLogout;
        private bool _blocked;
        private int _recoveryAttempts;
        private double _retryAt;
        private double _nextRivalSample;
        private bool _heardRival;
        private bool _rivalDisconnected;
        private double? _rivalReturnDeadline;

        private PlayerId LocalSeat => _onlineSession.IsHost ? PlayerId.Player1 : PlayerId.Player2;
        private PlayerId RivalSeat => _onlineSession.IsHost ? PlayerId.Player2 : PlayerId.Player1;
        private bool CanLogin => _active && _sessionStatus >= OnlineSessionStatus.Ready
            && _sessionService.Bootstrapper?.IsSignedIn == true;

        // Enter Play Mode without domain reload: a Vivox task left pending on exit must not keep the gate taken.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => OperationGate = new SemaphoreSlim(1);

        private void OnValidate()
        {
            if (_settings == null || _voice == null || _onlineSession == null ||
                _sessionService == null || _networkManager == null || _anchor == null)
            {
                Debug.LogWarning($"[{nameof(VoiceChatService)}] One or more references are not assigned on '{name}'.", this);
            }
        }

        private void OnEnable()
        {
            if (_settings == null || _voice == null || _onlineSession == null ||
                _sessionService == null || _networkManager == null || _anchor == null)
            {
                return;
            }

            _active = true;
            _quitting = false;
            _clientStopped = false;
            _needsSweep = true;
            _blocked = false;
            ResetRecovery();
            _reporter = new VoicePositionReporter(_anchor, _settings);
            _onlineSession.Changed += HandleSessionChanged;
            _networkManager.OnClientStopped += HandleClientStopped;
            GameEvents.OnPlayerConnectionChanged += HandlePlayerConnectionChanged;
            _voice.SetRivalVolumeRequested += SetRivalVolume;
            _voice.ToggleRivalMuteRequested += ToggleRivalMute;
            _voice.ToggleMicRequested += ToggleMic;
            HandleSessionChanged();
        }

        private void OnDisable()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            _desired = null;
            _onlineSession.Changed -= HandleSessionChanged;
            _networkManager.OnClientStopped -= HandleClientStopped;
            GameEvents.OnPlayerConnectionChanged -= HandlePlayerConnectionChanged;
            _voice.ToggleMicRequested -= ToggleMic;
            _voice.ToggleRivalMuteRequested -= ToggleRivalMute;
            _voice.SetRivalVolumeRequested -= SetRivalVolume;
            SubscribeVivox(false);
            ClearParticipants();
            _voice.SetChannelName(null);
            _voice.SetStatus(VoiceStatus.Off);
            _voice.SetHasDesiredChannel(false);
            _voice.SetLastErrorKey(null);
            Reconcile();
        }

        private void OnApplicationQuit()
        {
            _quitting = true;
            Reconcile();
        }

        private void LateUpdate()
        {
            if (!_active || _quitting)
            {
                return;
            }

            double now = Time.unscaledTimeAsDouble;
            if (_dirty || (_retryAt > 0d && now >= _retryAt))
            {
                Reconcile();
            }

            if (_voice.Status == VoiceStatus.InChannel && _joined != null)
            {
                _reporter.Report(_sdk, _voice.Status, _joined, now);
                if (now >= _nextRivalSample)
                {
                    _nextRivalSample = now + 0.25d;
                    PublishRival();
                }
            }
        }

        private void HandleSessionChanged()
        {
            OnlineSessionStatus status = _onlineSession.Status;
            string sessionId = _sessionService.CurrentSession?.Id;
            if (status != OnlineSessionStatus.InSession)
            {
                _clientStopped = false;
            }

            if (status != _sessionStatus || !string.Equals(sessionId, _sessionId, StringComparison.Ordinal))
            {
                _sessionStatus = status;
                _sessionId = sessionId;
                _voice.SetRivalPreferencesSession(sessionId);
                _blocked = false;
                if (status < OnlineSessionStatus.Ready)
                {
                    ResetRecovery();
                }
            }

            string desired = VoiceDesiredChannelRules.Desired(
                _clientStopped ? OnlineSessionStatus.Leaving : status, sessionId, _settings.ChannelPrefix);
            if (desired != _desired)
            {
                _desired = desired;
                _blocked = false;
                _voice.SetHasDesiredChannel(desired != null);
                RefreshStatus();
            }

            // The Sessions participant list and Vivox participant notifications can arrive in either order.
            SyncParticipants();
            Reconcile();
        }

        private void HandleClientStopped(bool wasHost)
        {
            _clientStopped = _onlineSession.Status == OnlineSessionStatus.InSession;
            HandleSessionChanged();
        }

        private void Reconcile()
        {
            _dirty = true;
            if (!_busy)
            {
                _ = ReconcileAsync();
            }
        }

        private async Task ReconcileAsync()
        {
            _busy = true;
            await OperationGate.WaitAsync();
            string errorFallback = VoiceErrorMapper.Login;
            try
            {
                while (true)
                {
                    _dirty = false;
                    errorFallback = VoiceErrorMapper.Login;
                    if (_quitting || !_active)
                    {
                        if (_sdk?.IsLoggedIn == true)
                        {
                            if (_quitting)
                            {
                                await _sdk.LogoutAsync();
                            }
                            else if (_joined != null)
                            {
                                await LeaveChannelAsync(_joined);
                            }
                        }

                        _joined = null;
                        _loginConfigured = false;
                        if (!_active || _quitting)
                        {
                            break;
                        }

                        continue;
                    }

                    if (_blocked || (_retryAt > 0d && Time.unscaledTimeAsDouble < _retryAt))
                    {
                        break;
                    }

                    if (_sdk == null)
                    {
                        if (UnityServices.State != ServicesInitializationState.Initialized)
                        {
                            break;
                        }

                        _sdk = VivoxService.Instance;
                        SubscribeVivox(true);
                    }
                    else if (_needsSweep)
                    {
                        // An instance can be re-enabled while its preceding cleanup is still in flight.
                        SubscribeVivox(false);
                        SubscribeVivox(true);
                    }

                    if (_recovering && (_joined == null || _joined == _desired))
                    {
                        break;
                    }

                    if (_needsSweep)
                    {
                        if (_sdk.IsLoggedIn && _sdk.ActiveChannels.Count > 0)
                        {
                            errorFallback = VoiceErrorMapper.Join;
                            await _sdk.LeaveAllChannelsAsync();
                        }

                        _needsSweep = false;
                        continue;
                    }

                    if (_retryAt > 0d)
                    {
                        _retryAt = 0d;
                        _recoveryAttempts++;
                    }

                    if (_awaitingRecoveryLogout && _sdk.IsLoggedIn)
                    {
                        // Failed recovery may arrive just before the SDK's LoggedOut event.
                        ScheduleRecovery();
                        break;
                    }

                    _awaitingRecoveryLogout = false;
                    if (CanLogin && (!_sdk.IsLoggedIn || !_loginConfigured))
                    {
                        if (!_sdk.IsLoggedIn)
                        {
                            if (!_recoveryFailed)
                            {
                                _voice.SetStatus(VoiceStatus.Initializing);
                            }

                            await _sdk.InitializeAsync();
                            if (!CanLogin || _quitting)
                            {
                                continue;
                            }

                            await _sdk.LoginAsync(new LoginOptions { DisplayName = _onlineSession.LocalPlayerName });
                            if (!_sdk.IsLoggedIn && CanLogin && !_quitting)
                            {
                                throw new InvalidOperationException("Vivox refused login. Check the project Vivox credentials.");
                            }

                            continue;
                        }

                        if (_settings.EnableEchoCancellation)
                        {
                            _sdk.EnableAcousticEchoCancellation();
                        }

                        ApplyMic();
                        _loginConfigured = true;
                    }

                    string target = CanLogin ? _desired : null;
                    errorFallback = VoiceErrorMapper.Join;
                    switch (VoiceChannelPlanner.Next(target, _joined, false))
                    {
                        case VoiceChannelStep.Leave:
                        case VoiceChannelStep.LeaveThenJoin:
                            string previous = _joined;
                            RefreshStatus();
                            await LeaveChannelAsync(previous);
                            continue;
                        case VoiceChannelStep.Join:
                            _joining = target;
                            RefreshStatus();
                            try
                            {
                                await _sdk.JoinPositionalChannelAsync(target, ChatCapability.AudioOnly,
                                    VoiceFadeModelMapper.ToChannel3DProperties(_settings));
                                if (_joined != target && _sdk.IsLoggedIn && _sdk.ActiveChannels.ContainsKey(target))
                                {
                                    HandleChannelJoined(target);
                                    SyncParticipants();
                                }
                            }
                            finally
                            {
                                _joining = null;
                            }

                            if (_joined == null && CanLogin && !_recovering && _retryAt == 0d && _active &&
                                _desired == target)
                            {
                                throw new InvalidOperationException("Vivox completed the join without an active channel.");
                            }

                            continue;
                    }

                    if (CanLogin && _sdk.IsLoggedIn)
                    {
                        ResetRecovery();
                        _voice.SetLastErrorKey(null);
                    }

                    RefreshStatus();
                    if (!_dirty)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (_quitting)
            {
                _dirty = false;
            }
            catch (Exception exception)
            {
                if (_active && !_quitting)
                {
                    string key = VoiceErrorMapper.FromException(exception, errorFallback);
                    if (_recoveryFailed && key != VoiceErrorMapper.Credentials)
                    {
                        ScheduleRecovery();
                    }
                    else
                    {
                        SetError(key);
                    }
                }

                // Do not include SDK exception messages, which can contain account details.
                Debug.LogWarning($"[{nameof(VoiceChatService)}] Vivox operation failed ({exception.GetType().Name}).", this);
            }
            finally
            {
                OperationGate.Release();
                _busy = false;
            }

            if (_dirty)
            {
                Reconcile();
            }
        }

        private async Task LeaveChannelAsync(string channel)
        {
            try
            {
                if (_sdk.IsLoggedIn)
                {
                    await _sdk.LeaveChannelAsync(channel);
                }
            }
            catch (OperationCanceledException) when (_quitting)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(VoiceChatService)}] Vivox leave failed ({exception.GetType().Name}).", this);
            }
            finally
            {
                HandleChannelLeft(channel);
            }
        }

        private void SubscribeVivox(bool subscribe)
        {
            if (_sdk == null)
            {
                return;
            }

            if (subscribe)
            {
                _sdk.LoggedOut += HandleLoggedOut;
                _sdk.ChannelJoined += HandleChannelJoined;
                _sdk.ChannelLeft += HandleChannelLeft;
                _sdk.ConnectionRecovering += HandleRecovering;
                _sdk.ConnectionRecovered += HandleRecovered;
                _sdk.ConnectionFailedToRecover += HandleFailedToRecover;
                _sdk.ParticipantAddedToChannel += HandleParticipantAdded;
                _sdk.ParticipantRemovedFromChannel += HandleParticipantRemoved;
            }
            else
            {
                _sdk.LoggedOut -= HandleLoggedOut;
                _sdk.ChannelJoined -= HandleChannelJoined;
                _sdk.ChannelLeft -= HandleChannelLeft;
                _sdk.ConnectionRecovering -= HandleRecovering;
                _sdk.ConnectionRecovered -= HandleRecovered;
                _sdk.ConnectionFailedToRecover -= HandleFailedToRecover;
                _sdk.ParticipantAddedToChannel -= HandleParticipantAdded;
                _sdk.ParticipantRemovedFromChannel -= HandleParticipantRemoved;
            }
        }

        private void HandleLoggedOut()
        {
            _awaitingRecoveryLogout = false;
            _loginConfigured = false;
            _recovering = false;
            HandleChannelLeft(_joined);
        }

        private void HandleRecovering()
        {
            _recovering = true;
            RefreshStatus();
        }

        private void HandleRecovered()
        {
            _recovering = false;
            _blocked = false;
            if (_joined != null && !_sdk.ActiveChannels.ContainsKey(_joined))
            {
                HandleChannelLeft(_joined);
            }

            _reporter.Reset();
            RefreshStatus();
            SyncParticipants();
            if (_sdk.IsLoggedIn)
            {
                ApplyMic();
                VoiceParticipantBinding rival = _bindings[(int)RivalSeat - 1];
                if (rival != null)
                {
                    rival.Participant.SetLocalVolume(_voice.RivalVolume);
                    ApplyRivalMute(rival.Participant);
                    HandleSpeech(rival);
                }
            }

            _dirty = true;
        }

        private void HandleFailedToRecover()
        {
            _recovering = false;
            _recoveryFailed = true;
            _awaitingRecoveryLogout = _sdk.IsLoggedIn;
            _loginConfigured = false;
            HandleChannelLeft(_joined);
            ScheduleRecovery();
        }

        private void ScheduleRecovery()
        {
            if (_recoveryAttempts >= _settings.MaxRecoverAttempts)
            {
                SetError(VoiceErrorMapper.RecoverFailed);
                return;
            }

            _retryAt = Time.unscaledTimeAsDouble + (1 << _recoveryAttempts);
            RefreshStatus();
        }

        private void ResetRecovery()
        {
            _recovering = false;
            _recoveryFailed = false;
            _awaitingRecoveryLogout = false;
            _recoveryAttempts = 0;
            _retryAt = 0d;
        }

        private void SetError(string key)
        {
            _blocked = true;
            _retryAt = 0d;
            _voice.SetStatus(VoiceStatus.Error);
            _voice.SetChannelName(null);
            _voice.SetLastErrorKey(key);
        }

        private void HandleChannelJoined(string channel)
        {
            if (channel == null || channel != _joining)
            {
                return;
            }

            _joined = channel;
            _nextRivalSample = 0d;
            if (_active && !_quitting)
            {
                _reporter.Reset();
                RefreshStatus();
                _reporter.Report(_sdk, _voice.Status, _joined, Time.unscaledTimeAsDouble);
            }

            _dirty = true;
        }

        private void HandleChannelLeft(string channel)
        {
            if (channel != _joined)
            {
                return;
            }

            _joined = null;
            if (_active)
            {
                ClearParticipants();
                RefreshStatus();
            }

            _dirty = true;
        }

        private void RefreshStatus()
        {
            if (!_active || _quitting || _blocked)
            {
                return;
            }

            bool inChannel = CanLogin && !_recovering && _joined != null && _joined == _desired &&
                _sdk?.IsLoggedIn == true;
            _voice.SetChannelName(inChannel ? _joined : null);
            if (!CanLogin)
            {
                _voice.SetStatus(VoiceStatus.Off);
            }
            else if (inChannel)
            {
                _voice.SetStatus(VoiceStatus.InChannel);
            }
            else if (_recovering || _recoveryFailed)
            {
                _voice.SetStatus(VoiceStatus.Recovering);
            }
            else if (_joining != null && _joining == _desired)
            {
                _voice.SetStatus(VoiceStatus.Joining);
            }
            else
            {
                _voice.SetStatus(_sdk?.IsLoggedIn == true ? VoiceStatus.LoggedIn : VoiceStatus.Off);
            }
        }

        private void SyncParticipants()
        {
            if (_joined == null || _joined != _desired || _sdk?.IsLoggedIn != true ||
                !_sdk.ActiveChannels.TryGetValue(_joined, out var participants))
            {
                return;
            }

            for (int i = 0; i < participants.Count; i++)
            {
                HandleParticipantAdded(participants[i]);
            }
        }

        private void HandleParticipantAdded(VivoxParticipant participant)
        {
            if (_joined == null || participant.ChannelName != _joined || _joined != _desired)
            {
                return;
            }

            PlayerId seat = participant.IsSelf ? LocalSeat : VoiceSeatRules.SeatOf(participant.PlayerId, _onlineSession);
            if (seat == PlayerId.None || (!participant.IsSelf && seat == LocalSeat))
            {
                return;
            }

            int index = (int)seat - 1;
            if (ReferenceEquals(_bindings[index]?.Participant, participant))
            {
                return;
            }

            RemoveBinding(index);
            var binding = new VoiceParticipantBinding(participant, seat, HandleSpeech, HandleParticipantMute);
            _bindings[index] = binding;
            if (!participant.IsSelf)
            {
                _heardRival = !_rivalDisconnected;
                participant.SetLocalVolume(_voice.RivalVolume);
                ApplyRivalMute(participant);
            }

            PublishRival();
            GameEvents.RaiseVoiceParticipantChanged(seat, true);
            HandleSpeech(binding);
        }

        private void HandleParticipantRemoved(VivoxParticipant participant)
        {
            for (int i = 0; i < _bindings.Length; i++)
            {
                if (ReferenceEquals(_bindings[i]?.Participant, participant))
                {
                    RemoveBinding(i);
                    PublishRival();
                    return;
                }
            }
        }

        private void RemoveBinding(int index)
        {
            VoiceParticipantBinding binding = _bindings[index];
            if (binding == null)
            {
                return;
            }

            _bindings[index] = null;
            binding.Dispose();
            SetSpeaking(binding.Seat, binding.Participant.IsSelf, false);
            GameEvents.RaiseVoiceParticipantChanged(binding.Seat, false);
        }

        private void ClearParticipants()
        {
            for (int i = 0; i < _bindings.Length; i++)
            {
                RemoveBinding(i);
            }

            _heardRival = false;
            _rivalDisconnected = false;
            _rivalReturnDeadline = null;
            _voice.SetRivalSeat(PlayerId.None);
            _voice.SetRivalState(VoiceRivalState.NoVoice);
            _voice.SetRivalDistance(-1f);
        }

        private void HandlePlayerConnectionChanged(PlayerId seat, PlayerConnectionStatus status,
            float graceSeconds, bool isLocalSeat)
        {
            if (isLocalSeat || seat != RivalSeat || _joined == null)
            {
                return;
            }

            _rivalDisconnected = status != PlayerConnectionStatus.Connected;
            if (_rivalDisconnected)
            {
                _heardRival = false;
            }
            else if (_bindings[(int)seat - 1] != null)
            {
                _heardRival = true;
            }

            PublishRival();
        }

        private void PublishRival()
        {
            PlayerId seat = RivalSeat;
            float distance = _reporter.RivalDistance(seat);
            bool present = _bindings[(int)seat - 1] != null;
            var (state, returnDeadline) = VoiceRivalStateRules.Evaluate(present, _heardRival,
                !_rivalDisconnected, distance, _settings.AudibleDistance, Time.unscaledTimeAsDouble, _rivalReturnDeadline);
            _rivalReturnDeadline = returnDeadline;
            _voice.SetRivalSeat(seat);
            _voice.SetRivalDistance(distance);
            _voice.SetRivalState(state);
        }

        private void HandleSpeech(VoiceParticipantBinding binding)
        {
            bool isSelf = binding.Participant.IsSelf;
            bool muted = isSelf ? _voice.MicMuted : _voice.RivalLocallyMuted;
            SetSpeaking(binding.Seat, isSelf, !muted && !binding.Participant.IsMuted && binding.Participant.SpeechDetected);
        }

        private void SetSpeaking(PlayerId seat, bool isSelf, bool speaking)
        {
            if ((isSelf ? _voice.LocalSpeaking : _voice.RivalSpeaking) == speaking)
            {
                return;
            }

            if (isSelf)
            {
                _voice.SetLocalSpeaking(speaking);
            }
            else
            {
                _voice.SetRivalSpeaking(speaking);
            }

            GameEvents.RaiseVoiceSpeakingChanged(seat, speaking);
        }

        private void HandleParticipantMute(VoiceParticipantBinding binding) => HandleSpeech(binding);

        private void ToggleMic()
        {
            bool muted = !_voice.MicMuted;
            _voice.SetMicMuted(muted);
            if (_sdk?.IsLoggedIn == true && !_recovering && !_recoveryFailed)
            {
                ApplyMic();
            }

            VoiceParticipantBinding local = _bindings[(int)LocalSeat - 1];
            if (local != null)
            {
                HandleSpeech(local);
            }

            GameEvents.RaiseVoiceMuteChanged(LocalSeat, muted);
        }

        private void ApplyMic()
        {
            if (_voice.MicMuted)
            {
                _sdk.MuteInputDevice();
            }
            else
            {
                _sdk.UnmuteInputDevice();
            }
        }

        private void SetRivalVolume(int volume)
        {
            _voice.SetRivalVolume(volume);
            if (_voice.Status == VoiceStatus.InChannel)
            {
                _bindings[(int)RivalSeat - 1]?.Participant.SetLocalVolume(volume);
            }
        }

        private void ToggleRivalMute()
        {
            bool muted = !_voice.RivalLocallyMuted;
            _voice.SetRivalLocallyMuted(muted);
            VoiceParticipantBinding rival = _bindings[(int)RivalSeat - 1];
            if (rival != null && _voice.Status == VoiceStatus.InChannel)
            {
                ApplyRivalMute(rival.Participant);
                HandleSpeech(rival);
            }

            GameEvents.RaiseVoiceMuteChanged(RivalSeat, muted);
        }

        private void ApplyRivalMute(VivoxParticipant participant)
        {
            if (_voice.RivalLocallyMuted)
            {
                participant.MutePlayerLocally();
            }
            else
            {
                participant.UnmutePlayerLocally();
            }
        }
    }
}
