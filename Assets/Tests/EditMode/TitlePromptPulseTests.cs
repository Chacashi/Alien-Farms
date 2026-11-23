using AlienFarms.Menu;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class TitlePromptPulseTests
    {
        private const float Period = 1.6f;
        private const float MinAlpha = 0.3f;
        private const float MaxAlpha = 1f;

        [TestCase(0f, MaxAlpha)]
        [TestCase(Period / 2f, MinAlpha)]
        [TestCase(Period, MaxAlpha)]
        public void EvaluateReachesExpectedAlpha(float time, float expected)
        {
            Assert.That(TitlePromptPulse.Evaluate(time, Period, MinAlpha, MaxAlpha),
                Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void EvaluateStaysWithinAlphaRange()
        {
            for (int i = 0; i <= 200; i++)
            {
                float time = i * Period / 100f;
                Assert.That(TitlePromptPulse.Evaluate(time, Period, MinAlpha, MaxAlpha),
                    Is.InRange(MinAlpha, MaxAlpha));
            }
        }

        [Test]
        public void EvaluateWithZeroPeriodReturnsFiniteAlpha()
        {
            Assert.That(TitlePromptPulse.Evaluate(0.25f, 0f, MinAlpha, MaxAlpha),
                Is.InRange(MinAlpha, MaxAlpha));
        }
    }
}
