using UnityEngine;

namespace AlienFarms.Menu
{
    public static class TitlePromptPulse
    {
        public static float Evaluate(float time, float period, float minAlpha, float maxAlpha)
        {
            float phase = 2f * Mathf.PI * time / Mathf.Max(period, 0.0001f);
            return minAlpha + (maxAlpha - minAlpha) * (0.5f + 0.5f * Mathf.Cos(phase));
        }
    }
}
