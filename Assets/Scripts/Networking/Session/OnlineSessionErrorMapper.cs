using System;
using AlienFarms.Core;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Services.Relay;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Maps Unity Gaming Services failures and NGO disconnect reasons to <see cref="OnlineSessionErrorKeys"/>. Pure.
    /// Note: the Sessions SDK reports some lobby failures (session full, malformed code) as
    /// <see cref="SessionError.Unknown"/> without an inner exception; for a join they surface as
    /// <see cref="OnlineSessionErrorKeys.JoinFailed"/> (see <see cref="FromJoinException"/>).
    /// </summary>
    public static class OnlineSessionErrorMapper
    {
        public static string FromSessionError(SessionError error)
        {
            switch (error)
            {
                case SessionError.None:
                    return null;
                case SessionError.SessionNotFound:
                case SessionError.InvalidSessionIdentifier:
                case SessionError.NotInLobby:
                    return OnlineSessionErrorKeys.SessionNotFound;
                case SessionError.SessionDeleted:
                    return OnlineSessionErrorKeys.HostLeft;
                case SessionError.RateLimitExceeded:
                    return OnlineSessionErrorKeys.RateLimited;
                case SessionError.NotAuthorized:
                    return OnlineSessionErrorKeys.SignInFailed;
                case SessionError.LobbyAlreadyExists:
                case SessionError.SessionTypeAlreadyExists:
                case SessionError.AlreadySubscribedToLobby:
                case SessionError.SessionConflict:
                    return OnlineSessionErrorKeys.AlreadyInSession;
                case SessionError.AllocationAlreadyExists:
                case SessionError.AllocationNotFound:
                case SessionError.InvalidNetworkConfig:
                case SessionError.NetworkSetupFailed:
                case SessionError.TransportComponentMissing:
                case SessionError.TransportInvalid:
                    return OnlineSessionErrorKeys.RelayFailed;
                case SessionError.NetworkManagerNotInitialized:
                case SessionError.NetworkManagerStartFailed:
                    return OnlineSessionErrorKeys.ConnectionFailed;
                case SessionError.MissingAssembly:
                    return OnlineSessionErrorKeys.NotConfigured;
                default:
                    return OnlineSessionErrorKeys.Unknown;
            }
        }

        /// <summary>Unity services request error code (CommonErrorCodes / service specific).</summary>
        public static string FromRequestErrorCode(int errorCode, string fallbackKey)
        {
            switch (errorCode)
            {
                case CommonErrorCodes.TransportError:
                case CommonErrorCodes.Timeout:
                    return OnlineSessionErrorKeys.NoConnection;
                case CommonErrorCodes.ServiceUnavailable:
                case CommonErrorCodes.ApiMissing:
                    return OnlineSessionErrorKeys.ServicesUnavailable;
                case CommonErrorCodes.TooManyRequests:
                    return OnlineSessionErrorKeys.RateLimited;
                case CommonErrorCodes.NotFound:
                    return OnlineSessionErrorKeys.SessionNotFound;
                default:
                    return fallbackKey ?? OnlineSessionErrorKeys.Unknown;
            }
        }

        /// <summary>Relay request error (allocation, join code). Anything not specific is <see cref="OnlineSessionErrorKeys.RelayFailed"/>.</summary>
        public static string FromRelayReason(RelayExceptionReason reason)
        {
            switch (reason)
            {
                case RelayExceptionReason.NetworkError:
                case RelayExceptionReason.RequestTimeOut:
                case RelayExceptionReason.GatewayTimeout:
                    return OnlineSessionErrorKeys.NoConnection;
                case RelayExceptionReason.RateLimited:
                    return OnlineSessionErrorKeys.RateLimited;
                case RelayExceptionReason.ServiceUnavailable:
                    return OnlineSessionErrorKeys.ServicesUnavailable;
                case RelayExceptionReason.JoinCodeNotFound:
                    return OnlineSessionErrorKeys.SessionNotFound;
                default:
                    return OnlineSessionErrorKeys.RelayFailed;
            }
        }

        /// <summary>
        /// Key for a failed join by code: as <see cref="FromException"/>, except that an unspecific failure is
        /// <see cref="OnlineSessionErrorKeys.JoinFailed"/> (the SDK reports a malformed code or a full session that way).
        /// </summary>
        public static string FromJoinException(Exception exception)
        {
            string key = FromException(exception, OnlineSessionErrorKeys.JoinFailed);
            return key == OnlineSessionErrorKeys.Unknown ? OnlineSessionErrorKeys.JoinFailed : key;
        }

        /// <summary>
        /// Key of a stable NGO disconnect / rejection reason (<see cref="DisconnectReasons"/>); null when the reason is not
        /// one the game sends (raw transport text).
        /// </summary>
        public static string FromDisconnectReason(string reason)
        {
            switch (reason)
            {
                case DisconnectReasons.RoomFull:
                    return OnlineSessionErrorKeys.SessionFull;
                case DisconnectReasons.ProtocolMismatch:
                    return OnlineSessionErrorKeys.ProtocolMismatch;
                case DisconnectReasons.HostDisconnected:
                    return OnlineSessionErrorKeys.HostLeft;
                case DisconnectReasons.InvalidPayload:
                    return OnlineSessionErrorKeys.ConnectionFailed;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Key for any exception thrown by Core, Authentication or Sessions. <paramref name="fallbackKey"/> is used for
        /// request failures with an unrecognized code (e.g. sign-in vs. session context). Null for a cancellation.
        /// </summary>
        public static string FromException(Exception exception, string fallbackKey = OnlineSessionErrorKeys.Unknown)
        {
            switch (exception)
            {
                case null:
                    return null;
                case OperationCanceledException _:
                    return null;
                case SessionException session:
                    // Network start failures wrap the real cause; prefer it when it says more.
                    if (session.Error == SessionError.Unknown && session.InnerException != null)
                    {
                        return FromException(session.InnerException, fallbackKey);
                    }

                    return FromSessionError(session.Error);
                case AuthenticationException auth:
                    return FromRequestErrorCode(auth.ErrorCode, OnlineSessionErrorKeys.SignInFailed);
                case ServicesInitializationException _:
                    return OnlineSessionErrorKeys.ServicesUnavailable;
                case RelayServiceException relay:
                    // Before RequestFailedException (its base): Relay codes are not CommonErrorCodes.
                    return FromRelayReason(relay.Reason);
                case RequestFailedException request:
                    return FromRequestErrorCode(request.ErrorCode, fallbackKey);
                case AggregateException aggregate when aggregate.InnerException != null:
                    return FromException(aggregate.InnerException, fallbackKey);
                default:
                    return fallbackKey ?? OnlineSessionErrorKeys.Unknown;
            }
        }
    }
}
