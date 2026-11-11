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

        // HowToPlay table.
        public const string HowToGoal = "howto.goal";
        public const string HowToTips = "howto.tips";
        public const string HowToKeyboardHeader = "howto.keyboard.header";
        public const string HowToKeyboardControls = "howto.keyboard.controls";
        public const string HowToGamepadHeader = "howto.gamepad.header";
        public const string HowToGamepadControls = "howto.gamepad.controls";
    }
}
