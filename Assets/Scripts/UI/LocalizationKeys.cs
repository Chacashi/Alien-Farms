using AlienFarms.Core;

namespace AlienFarms.UI
{
    /// <summary>
    /// String Table Collection names and entry keys. Code-driven texts use these as LocalizedString defaults;
    /// the editor setup tool seeds every key listed here.
    /// </summary>
    public static class LocalizationKeys
    {
        public const string UiTable = "UI";
        public const string HowToPlayTable = "HowToPlay";

        /// <summary>PlayerPrefs key where the selected locale is persisted.</summary>
        public const string LocalePlayerPrefsKey = "AlienFarms.Locale";

        public const string SpanishCode = "es";
        public const string EnglishCode = "en";

        // UI table.
        public const string CommonBack = "common.back";
        public const string CommonOptions = "common.options";
        public const string CommonHowToPlay = "common.how_to_play";
        public const string CommonBackToTitle = "common.back_to_title";
        public const string CommonP1 = "common.p1";
        public const string CommonP2 = "common.p2";
        public const string CommonPressToJoin = "common.press_to_join";
        public const string TitlePlay = "title.play";
        public const string TitleQuit = "title.quit";
        public const string TitleCredits = "title.credits";
        public const string CreditsHeader = "credits.header";
        public const string CreditsTeam = "credits.team";
        public const string CreditsThanks = "credits.thanks";
        public const string SelectHeader = "select.header";
        public const string SelectHints = "select.hints";
        public const string SelectChoosing = "select.choosing";
        public const string SelectReady = "select.ready";
        public const string SelectTakenBy = "select.taken_by";
        public const string LoadingLabel = "loading.label";
        public const string GameplayJoinPrompt = "gameplay.join_prompt";
        public const string PauseHoldHint = "pause.hold_hint";
        public const string PauseTitle = "pause.title";
        public const string PauseSubtitle = "pause.subtitle";
        public const string PauseResume = "pause.resume";
        public const string PauseRestart = "pause.restart";
        public const string MatchGo = "match.go";
        public const string ResultsWinner = "results.winner";
        public const string ResultsDraw = "results.draw";
        public const string ResultsRematch = "results.rematch";
        public const string OptionsTitle = "options.title";
        public const string OptionsMaster = "options.master";
        public const string OptionsMusic = "options.music";
        public const string OptionsSfx = "options.sfx";
        public const string OptionsMuteAll = "options.mute_all";
        public const string OptionsLanguage = "options.language";

        // UI table: online room (task 2.2).
        public const string TitlePlayOnline = "title.play_online";
        public const string OnlineHeader = "online.header";
        public const string OnlineHubSignedInAs = "online.hub.signed_in_as";
        public const string OnlineHubCreate = "online.hub.create";
        public const string OnlineHubJoin = "online.hub.join";
        public const string OnlineJoinPrompt = "online.join.prompt";
        public const string OnlineJoinConfirm = "online.join.confirm";
        public const string OnlineRoomCode = "online.room.code";
        public const string OnlineRoomStart = "online.room.start";
        public const string OnlineRoomLeave = "online.room.leave";
        public const string OnlineRoomWaitingPlayer = "online.room.waiting_player";
        public const string OnlineRoomWaitingHost = "online.room.waiting_host";
        public const string OnlineRoomNeedPicks = "online.room.need_picks";
        public const string OnlineRoomSyncing = "online.room.syncing";
        public const string OnlineBadgeHost = "online.badge.host";
        public const string OnlineBadgeYou = "online.badge.you";
        public const string OnlineBusySigningIn = "online.busy.signing_in";
        public const string OnlineBusyCreating = "online.busy.creating";
        public const string OnlineBusyJoining = "online.busy.joining";
        public const string OnlineBusyLeaving = "online.busy.leaving";
        public const string OnlineBusyStarting = "online.busy.starting";
        public const string OnlineBusyCancel = "online.busy.cancel";

        // UI table: connection status, rejoin and name (task 2.7).
        public const string OnlineStatusReconnecting = "online.status.reconnecting";
        public const string OnlineStatusReturned = "online.status.returned";
        public const string OnlineStatusLeft = "online.status.left";
        public const string OnlineStatusTimedOut = "online.status.timed_out";
        public const string OnlineHubRejoin = "online.hub.rejoin";
        public const string OnlineHubNameLabel = "online.hub.name_label";
        public const string OnlineHubNamePlaceholder = "online.hub.name_placeholder";

        // UI table: title stats (task 2.2, written from 2.8).
        public const string StatsHeader = "stats.header";
        public const string StatsMatches = "stats.matches";
        public const string StatsWins = "stats.wins";
        public const string StatsDraws = "stats.draws";
        public const string StatsLosses = "stats.losses";
        public const string StatsAnimals = "stats.animals";
        public const string StatsTackles = "stats.tackles";
        public const string StatsEmptyHint = "stats.empty_hint";
        public const string StatsSaved = "stats.saved";

        // UI table: cloud stats save badge, rival stats, forfeit (task 2.8).
        public const string StatsSaving = "stats.saving";
        public const string StatsSaveFailed = "stats.save_failed";
        /// <summary>{0} = matches, {1} = wins, {2} = draws, {3} = losses.</summary>
        public const string StatsRival = "stats.rival";
        /// <summary>Menu notice after the host left during Playing (the client recorded a win).</summary>
        public const string StatsHostLeftWin = "stats.host_left_win";
        /// <summary>Results subtitle on a forfeit; {0} = the player who left.</summary>
        public const string ResultsForfeit = "results.forfeit";

        // UI table: cloud stats errors. Values come from PlayerProgressErrorKeys (single source of truth).
        public const string StatsErrorOffline = PlayerProgressErrorKeys.Offline;
        public const string StatsErrorRateLimited = PlayerProgressErrorKeys.RateLimited;
        public const string StatsErrorUnavailable = PlayerProgressErrorKeys.Unavailable;
        public const string StatsErrorSignedOut = PlayerProgressErrorKeys.SignedOut;
        public const string StatsErrorNewerSchema = PlayerProgressErrorKeys.NewerSchema;
        public const string StatsErrorCorrupt = PlayerProgressErrorKeys.Corrupt;
        public const string StatsErrorUnknown = PlayerProgressErrorKeys.Unknown;

        // UI table: voice chat (S14).
        public const string VoiceStateNoVoice = "voice.state.no_voice";
        public const string VoiceStateInRange = "voice.state.in_range";
        public const string VoiceStateOutOfRange = "voice.state.out_of_range";
        public const string VoiceErrorCredentials = "voice.error.credentials";
        public const string VoiceErrorLogin = "voice.error.login";
        public const string VoiceErrorJoin = "voice.error.join";
        public const string VoiceErrorRecoverFailed = "voice.error.recover_failed";
        public const string VoiceLabelMic = "voice.label.mic";
        public const string VoiceLabelRivalVolume = "voice.label.rival_volume";
        public const string VoiceLabelMuteRival = "voice.label.mute_rival";
        public const string VoiceLabelRivalMuted = "voice.label.rival_muted";

        // UI table: online errors. Values come from OnlineSessionErrorKeys (single source of truth).
        public const string OnlineErrorSignInFailed = OnlineSessionErrorKeys.SignInFailed;
        public const string OnlineErrorServicesUnavailable = OnlineSessionErrorKeys.ServicesUnavailable;
        public const string OnlineErrorNoConnection = OnlineSessionErrorKeys.NoConnection;
        public const string OnlineErrorRateLimited = OnlineSessionErrorKeys.RateLimited;
        public const string OnlineErrorInvalidCode = OnlineSessionErrorKeys.InvalidCode;
        public const string OnlineErrorSessionNotFound = OnlineSessionErrorKeys.SessionNotFound;
        public const string OnlineErrorSessionFull = OnlineSessionErrorKeys.SessionFull;
        public const string OnlineErrorProtocolMismatch = OnlineSessionErrorKeys.ProtocolMismatch;
        public const string OnlineErrorAlreadyConnected = OnlineSessionErrorKeys.AlreadyConnected;
        public const string OnlineErrorNotSessionMember = OnlineSessionErrorKeys.NotSessionMember;
        public const string OnlineErrorHostLeft = OnlineSessionErrorKeys.HostLeft;
        public const string OnlineErrorConnectionLost = OnlineSessionErrorKeys.ConnectionLost;
        public const string OnlineErrorReconnectDenied = OnlineSessionErrorKeys.ReconnectDenied;
        public const string OnlineErrorRejoinExpired = OnlineSessionErrorKeys.RejoinExpired;
        public const string OnlineErrorRelayFailed = OnlineSessionErrorKeys.RelayFailed;
        public const string OnlineErrorConnectionFailed = OnlineSessionErrorKeys.ConnectionFailed;
        public const string OnlineErrorJoinFailed = OnlineSessionErrorKeys.JoinFailed;
        public const string OnlineErrorAlreadyInSession = OnlineSessionErrorKeys.AlreadyInSession;
        public const string OnlineErrorBusy = OnlineSessionErrorKeys.Busy;
        public const string OnlineErrorNotConfigured = OnlineSessionErrorKeys.NotConfigured;
        public const string OnlineErrorUnknown = OnlineSessionErrorKeys.Unknown;
        public const string OnlineErrorCharacterTaken = OnlineSessionErrorKeys.CharacterTaken;
        public const string OnlineErrorNotSeated = OnlineSessionErrorKeys.NotSeated;
        public const string OnlineErrorCannotStart = OnlineSessionErrorKeys.CannotStart;
        public const string OnlineErrorStartFailed = OnlineSessionErrorKeys.StartFailed;

        // HowToPlay table.
        public const string HowToGoal = "howto.goal";
        public const string HowToTips = "howto.tips";
        public const string HowToKeyboardHeader = "howto.keyboard.header";
        public const string HowToKeyboardControls = "howto.keyboard.controls";
        public const string HowToGamepadHeader = "howto.gamepad.header";
        public const string HowToGamepadControls = "howto.gamepad.controls";
    }
}
