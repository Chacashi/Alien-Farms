using System;
using AlienFarms.Core;
using AlienFarms.Networking.Session;
using NUnit.Framework;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Multiplayer;
using Unity.Services.Relay;

namespace AlienFarms.Networking.Tests
{
    /// <summary>UGS failures and NGO reasons mapped to localization keys.</summary>
    public sealed class OnlineSessionErrorMapperTests
    {
        [TestCase(SessionError.SessionNotFound, OnlineSessionErrorKeys.SessionNotFound)]
        [TestCase(SessionError.RateLimitExceeded, OnlineSessionErrorKeys.RateLimited)]
        [TestCase(SessionError.SessionDeleted, OnlineSessionErrorKeys.HostLeft)]
        [TestCase(SessionError.NotAuthorized, OnlineSessionErrorKeys.SignInFailed)]
        [TestCase(SessionError.SessionTypeAlreadyExists, OnlineSessionErrorKeys.AlreadyInSession)]
        [TestCase(SessionError.AllocationNotFound, OnlineSessionErrorKeys.RelayFailed)]
        [TestCase(SessionError.NetworkSetupFailed, OnlineSessionErrorKeys.RelayFailed)]
        [TestCase(SessionError.NetworkManagerStartFailed, OnlineSessionErrorKeys.ConnectionFailed)]
        [TestCase(SessionError.Unknown, OnlineSessionErrorKeys.Unknown)]
        [TestCase(SessionError.InvalidMatchmakerTicket, OnlineSessionErrorKeys.Unknown)]
        public void FromSessionError_MapsToKey(SessionError error, string expected)
        {
            Assert.That(OnlineSessionErrorMapper.FromSessionError(error), Is.EqualTo(expected));
        }

        [Test]
        public void FromSessionError_None_IsNoError()
        {
            Assert.That(OnlineSessionErrorMapper.FromSessionError(SessionError.None), Is.Null);
        }

        [Test]
        public void FromSessionError_EveryValue_HasAKey()
        {
            foreach (SessionError error in Enum.GetValues(typeof(SessionError)))
            {
                if (error == SessionError.None)
                {
                    continue;
                }

                Assert.That(OnlineSessionErrorMapper.FromSessionError(error), Does.StartWith("online.error."), error.ToString());
            }
        }

        [TestCase(DisconnectReasons.RoomFull, OnlineSessionErrorKeys.SessionFull)]
        [TestCase(DisconnectReasons.ProtocolMismatch, OnlineSessionErrorKeys.ProtocolMismatch)]
        [TestCase(DisconnectReasons.AlreadyConnected, OnlineSessionErrorKeys.AlreadyConnected)]
        [TestCase(DisconnectReasons.HostDisconnected, OnlineSessionErrorKeys.HostLeft)]
        [TestCase(DisconnectReasons.InvalidPayload, OnlineSessionErrorKeys.ConnectionFailed)]
        [TestCase(DisconnectReasons.NotSessionMember, OnlineSessionErrorKeys.NotSessionMember)]
        [TestCase(DisconnectReasons.ReconnectDenied, OnlineSessionErrorKeys.ReconnectDenied)]
        [TestCase(DisconnectReasons.ConnectionLost, OnlineSessionErrorKeys.ConnectionLost)]
        public void FromDisconnectReason_KnownReason_MapsToKey(string reason, string expected)
        {
            Assert.That(OnlineSessionErrorMapper.FromDisconnectReason(reason), Is.EqualTo(expected));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Disconnected due to transport failure")]
        public void FromDisconnectReason_NormalizedDropWithoutReason_IsConnectionLost(string raw)
        {
            Assert.That(OnlineSessionErrorMapper.FromDisconnectReason(DisconnectReasons.Normalize(raw)), Is.EqualTo(OnlineSessionErrorKeys.ConnectionLost));
        }

        [Test]
        public void FromDisconnectReason_ExplicitHostShutdown_IsStillHostLeft()
        {
            Assert.That(OnlineSessionErrorMapper.FromDisconnectReason(DisconnectReasons.Normalize(DisconnectReasons.HostDisconnected)), Is.EqualTo(OnlineSessionErrorKeys.HostLeft));
        }

        [Test]
        public void FromDisconnectReason_RawTransportText_IsNull()
        {
            Assert.That(OnlineSessionErrorMapper.FromDisconnectReason("Disconnected due to transport failure"), Is.Null);
            Assert.That(OnlineSessionErrorMapper.FromDisconnectReason(null), Is.Null);
        }

        [Test]
        public void FromException_SessionException_UsesItsError()
        {
            var exception = new SessionException("not found", SessionError.SessionNotFound, null);

            Assert.That(OnlineSessionErrorMapper.FromException(exception), Is.EqualTo(OnlineSessionErrorKeys.SessionNotFound));
        }

        [Test]
        public void FromException_UnknownSessionErrorWithCause_UsesTheCause()
        {
            var cause = new RequestFailedException(CommonErrorCodes.TransportError, "offline");
            var exception = new SessionException("wrapped", SessionError.Unknown, cause);

            Assert.That(OnlineSessionErrorMapper.FromException(exception), Is.EqualTo(OnlineSessionErrorKeys.NoConnection));
        }

        [Test]
        public void FromException_AuthenticationFailure_IsSignInFailed()
        {
            RequestFailedException exception = AuthenticationException.Create(AuthenticationErrorCodes.InvalidSessionToken, "bad token");

            Assert.That(exception, Is.InstanceOf<AuthenticationException>());
            Assert.That(OnlineSessionErrorMapper.FromException(exception), Is.EqualTo(OnlineSessionErrorKeys.SignInFailed));
        }

        [TestCase(CommonErrorCodes.TransportError, OnlineSessionErrorKeys.NoConnection)]
        [TestCase(CommonErrorCodes.Timeout, OnlineSessionErrorKeys.NoConnection)]
        [TestCase(CommonErrorCodes.ServiceUnavailable, OnlineSessionErrorKeys.ServicesUnavailable)]
        [TestCase(CommonErrorCodes.TooManyRequests, OnlineSessionErrorKeys.RateLimited)]
        public void FromException_RequestFailure_MapsItsCode(int code, string expected)
        {
            Assert.That(OnlineSessionErrorMapper.FromException(new RequestFailedException(code, "x")), Is.EqualTo(expected));
        }

        [Test]
        public void FromException_RequestFailureWithUnknownCode_UsesFallback()
        {
            var exception = new RequestFailedException(CommonErrorCodes.Unknown, "x");

            Assert.That(OnlineSessionErrorMapper.FromException(exception, OnlineSessionErrorKeys.SignInFailed), Is.EqualTo(OnlineSessionErrorKeys.SignInFailed));
        }

        [Test]
        public void FromException_ServicesInitialization_IsServicesUnavailable()
        {
            Assert.That(OnlineSessionErrorMapper.FromException(new ServicesInitializationException("no project")), Is.EqualTo(OnlineSessionErrorKeys.ServicesUnavailable));
        }

        [Test]
        public void FromException_Cancellation_IsNoError()
        {
            Assert.That(OnlineSessionErrorMapper.FromException(new OperationCanceledException()), Is.Null);
        }

        [Test]
        public void FromException_Aggregate_UsesInner()
        {
            var exception = new AggregateException(new SessionException("x", SessionError.RateLimitExceeded, null));

            Assert.That(OnlineSessionErrorMapper.FromException(exception), Is.EqualTo(OnlineSessionErrorKeys.RateLimited));
        }

        [Test]
        public void FromException_Other_UsesFallback()
        {
            Assert.That(OnlineSessionErrorMapper.FromException(new InvalidOperationException()), Is.EqualTo(OnlineSessionErrorKeys.Unknown));
            Assert.That(OnlineSessionErrorMapper.FromException(new InvalidOperationException(), OnlineSessionErrorKeys.RelayFailed), Is.EqualTo(OnlineSessionErrorKeys.RelayFailed));
        }

        [TestCase(RelayExceptionReason.AllocationNotFound, OnlineSessionErrorKeys.RelayFailed)]
        [TestCase(RelayExceptionReason.NoSuitableRelay, OnlineSessionErrorKeys.RelayFailed)]
        [TestCase(RelayExceptionReason.Unknown, OnlineSessionErrorKeys.RelayFailed)]
        [TestCase(RelayExceptionReason.NetworkError, OnlineSessionErrorKeys.NoConnection)]
        [TestCase(RelayExceptionReason.RateLimited, OnlineSessionErrorKeys.RateLimited)]
        [TestCase(RelayExceptionReason.JoinCodeNotFound, OnlineSessionErrorKeys.SessionNotFound)]
        public void FromException_RelayFailure_MapsItsReason(RelayExceptionReason reason, string expected)
        {
            var exception = new RelayServiceException(reason, "relay", null);

            Assert.That(OnlineSessionErrorMapper.FromException(exception), Is.EqualTo(expected));
        }

        [Test]
        public void FromException_RelayFailureWrappedInUnknownSessionError_IsRelayFailed()
        {
            var cause = new RelayServiceException(RelayExceptionReason.InvalidRequest, "relay", null);
            var exception = new SessionException("wrapped", SessionError.Unknown, cause);

            Assert.That(OnlineSessionErrorMapper.FromException(exception), Is.EqualTo(OnlineSessionErrorKeys.RelayFailed));
        }

        [Test]
        public void FromJoinException_UnknownSessionError_IsJoinFailed()
        {
            var exception = new SessionException("lobby refused", SessionError.Unknown, null);

            Assert.That(OnlineSessionErrorMapper.FromJoinException(exception), Is.EqualTo(OnlineSessionErrorKeys.JoinFailed));
            Assert.That(OnlineSessionErrorMapper.FromJoinException(new InvalidOperationException()), Is.EqualTo(OnlineSessionErrorKeys.JoinFailed));
        }

        [TestCase("session is full")]
        [TestCase("Lobby is FULL")]
        public void FromJoinException_UnknownSessionErrorSayingFull_IsSessionFull(string message)
        {
            // What Sessions SDK 2.x throws for a full lobby: LobbyFull converted to Unknown, only the message survives.
            var exception = new SessionException(message, SessionError.Unknown, null);

            Assert.That(OnlineSessionErrorMapper.FromJoinException(exception), Is.EqualTo(OnlineSessionErrorKeys.SessionFull));
            Assert.That(OnlineSessionErrorMapper.FromJoinException(new AggregateException(exception)), Is.EqualTo(OnlineSessionErrorKeys.SessionFull));
        }

        [Test]
        public void FromJoinException_LobbyFullReason_IsSessionFull()
        {
            var lobby = new LobbyServiceException(LobbyExceptionReason.LobbyFull, "whatever");
            var wrapped = new SessionException("start failed", SessionError.Unknown, lobby);

            Assert.That(OnlineSessionErrorMapper.FromJoinException(lobby), Is.EqualTo(OnlineSessionErrorKeys.SessionFull));
            Assert.That(OnlineSessionErrorMapper.FromJoinException(wrapped), Is.EqualTo(OnlineSessionErrorKeys.SessionFull));
        }

        [Test]
        public void FromJoinException_FullInASpecificError_KeepsItsKey()
        {
            // Text is only trusted for the unspecific error.
            var exception = new SessionException("rate limit full", SessionError.RateLimitExceeded, null);

            Assert.That(OnlineSessionErrorMapper.FromJoinException(exception), Is.EqualTo(OnlineSessionErrorKeys.RateLimited));
            Assert.That(OnlineSessionErrorMapper.IsSessionFull(null), Is.False);
        }

        [Test]
        public void FromJoinException_SpecificError_KeepsItsKey()
        {
            var exception = new SessionException("not found", SessionError.SessionNotFound, null);

            Assert.That(OnlineSessionErrorMapper.FromJoinException(exception), Is.EqualTo(OnlineSessionErrorKeys.SessionNotFound));
            Assert.That(OnlineSessionErrorMapper.FromJoinException(new OperationCanceledException()), Is.Null);
        }
    }
}
