using UnityEngine;
using UnityEngine.SceneManagement;

namespace AlienFarms.Core
{
    /// <summary>
    /// Stateless scene transitions. Every load restores time and audio first, so leaving a paused scene never
    /// carries <see cref="Time.timeScale"/> = 0 or a paused <see cref="AudioListener"/> into the next one.
    /// </summary>
    public static class SceneFlow
    {
        public static bool LoadMainMenu() => Load(SceneNames.MainMenu);

        public static bool LoadGameplay() => Load(SceneNames.Gameplay);

        private static bool Load(string sceneName)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[{nameof(SceneFlow)}] Scene '{sceneName}' is not in Build Settings.");
                return false;
            }

            // Single and synchronous on purpose: small scenes, and nothing must run between both scenes.
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            return true;
        }
    }
}
