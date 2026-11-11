using System.Collections.Generic;
using AlienFarms.UI;

namespace AlienFarms.Editor
{
    /// <summary>
    /// Initial ES/EN values for every localization key. Spanish values match the texts authored in scenes/prefabs
    /// (so "Bind Selected" can match them). Only used to seed missing entries: edit translations in the tables.
    /// </summary>
    public static class LocalizationSeedData
    {
        private const string Ui = LocalizationKeys.UiTable;
        private const string HowTo = LocalizationKeys.HowToPlayTable;

        public static readonly IReadOnlyList<LocalizationSeedEntry> Entries = new[]
        {
            new LocalizationSeedEntry(Ui, LocalizationKeys.CommonBack, "Volver", "Back"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.CommonOptions, "Opciones", "Options"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.CommonHowToPlay, "Cómo jugar", "How to Play"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.CommonBackToTitle, "Volver al título", "Back to Title"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.CommonP1, "J1", "P1"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.CommonP2, "J2", "P2"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.CommonPressToJoin, "Presiona X / Enter para unirte", "Press X / Enter to join"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.TitlePlay, "Jugar", "Play"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.TitleQuit, "Salir", "Quit"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.TitleCredits, "Créditos", "Credits"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.CreditsHeader, "Créditos", "Credits"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.CreditsTeam, "Equipo de desarrollo", "Development Team"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.CreditsThanks, "¡Gracias por jugar!", "Thanks for playing!"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.SelectHeader, "Elige tu personaje", "Pick your character"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.SelectHints,
                "Stick / Flechas: elegir\nX / Enter: confirmar  ·  O / Esc: volver",
                "Stick / Arrows: choose\nX / Enter: confirm  ·  O / Esc: back"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.SelectChoosing, "Eligiendo…", "Choosing…"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.SelectReady, "¡LISTO!", "READY!"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.SelectTakenBy, "Tomado por {0}", "Taken by {0}"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.LoadingLabel, "Cargando…", "Loading…"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.GameplayJoinPrompt,
                "Presiona cualquier botón (mando o teclado) para unirte",
                "Press any button (gamepad or keyboard) to join"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.PauseHoldHint, "Mantén para pausar", "Hold to pause"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.PauseTitle, "Pausa", "Paused"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.PauseSubtitle, "{0} pausó", "{0} paused"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.PauseResume, "Continuar", "Resume"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.PauseRestart, "Reiniciar partida", "Restart Match"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.MatchGo, "¡YA!", "GO!"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.ResultsWinner, "¡Gana {0}!", "{0} wins!"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.ResultsDraw, "¡Empate!", "It's a tie!"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.ResultsRematch, "Revancha", "Rematch"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.OptionsTitle, "Opciones", "Options"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.OptionsMaster, "General", "Master"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.OptionsMusic, "Música", "Music"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.OptionsSfx, "Efectos", "Sound FX"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.OptionsMuteAll, "Silenciar todo", "Mute All"),
            new LocalizationSeedEntry(Ui, LocalizationKeys.OptionsLanguage, "Idioma", "Language"),

            new LocalizationSeedEntry(HowTo, LocalizationKeys.HowToGoal,
                "Lleva animales de la zona central a tu corral. Cada animal vale distintos puntos (la vaca vale más, pero pesa más). Al acabar el tiempo gana quien tenga más puntos en su corral. <color=#B04A0A>¡Ojo: el rival puede robarte, incluso desde tu corral!</color>",
                "Bring animals from the center zone to your pen. Each animal is worth different points (the cow is worth more, but it's heavier). When time runs out, whoever has the most points in their pen wins. <color=#B04A0A>Watch out: your rival can steal from you, even from your own pen!</color>"),
            new LocalizationSeedEntry(HowTo, LocalizationKeys.HowToTips,
                "Cargando no puedes correr. Choca contra un rival que lleve un animal para robárselo. Lanza la bomba para aturdir al rival.",
                "You can't sprint while carrying. Tackle a rival who's carrying an animal to steal it. Throw the bomb to stun your rival."),
            new LocalizationSeedEntry(HowTo, LocalizationKeys.HowToKeyboardHeader, "Teclado y ratón", "Keyboard & Mouse"),
            new LocalizationSeedEntry(HowTo, LocalizationKeys.HowToKeyboardControls,
                "<color=#9A3E08>Mover:</color> WASD\n<color=#9A3E08>Cámara:</color> Ratón\n<color=#9A3E08>Correr:</color> Shift izq.\n<color=#9A3E08>Agarrar:</color> mantén E (suelta para dejarlo)\n<color=#9A3E08>Apuntar:</color> mantén clic derecho\n<color=#9A3E08>Lanzar:</color> apuntando, suelta E\n<color=#9A3E08>Pausa:</color> mantén Esc 3 s\n<color=#9A3E08>Menús:</color> Enter confirmar, Esc volver",
                "<color=#9A3E08>Move:</color> WASD\n<color=#9A3E08>Camera:</color> Mouse\n<color=#9A3E08>Sprint:</color> Left Shift\n<color=#9A3E08>Grab:</color> hold E (release to drop)\n<color=#9A3E08>Aim:</color> hold right click\n<color=#9A3E08>Throw:</color> while aiming, release E\n<color=#9A3E08>Pause:</color> hold Esc 3 s\n<color=#9A3E08>Menus:</color> Enter confirm, Esc back"),
            new LocalizationSeedEntry(HowTo, LocalizationKeys.HowToGamepadHeader, "Mando (PS / Xbox)", "Gamepad (PS / Xbox)"),
            new LocalizationSeedEntry(HowTo, LocalizationKeys.HowToGamepadControls,
                "<color=#9A3E08>Mover:</color> Stick izq.\n<color=#9A3E08>Cámara:</color> Stick der.\n<color=#9A3E08>Correr:</color> L2 / LT\n<color=#9A3E08>Agarrar:</color> mantén R2 / RT (suelta para dejarlo)\n<color=#9A3E08>Apuntar:</color> mantén Cruz / A o L1 / LB\n<color=#9A3E08>Lanzar:</color> apuntando, suelta R2 / RT\n<color=#9A3E08>Pausa:</color> mantén Options / Menu 3 s\n<color=#9A3E08>Menús:</color> Cruz / A confirmar, Círculo / B volver",
                "<color=#9A3E08>Move:</color> Left stick\n<color=#9A3E08>Camera:</color> Right stick\n<color=#9A3E08>Sprint:</color> L2 / LT\n<color=#9A3E08>Grab:</color> hold R2 / RT (release to drop)\n<color=#9A3E08>Aim:</color> hold Cross / A or L1 / LB\n<color=#9A3E08>Throw:</color> while aiming, release R2 / RT\n<color=#9A3E08>Pause:</color> hold Options / Menu 3 s\n<color=#9A3E08>Menus:</color> Cross / A confirm, Circle / B back"),
        };
    }
}
