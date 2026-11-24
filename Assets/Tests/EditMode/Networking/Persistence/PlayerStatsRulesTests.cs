using AlienFarms.Core;
using AlienFarms.Networking.Persistence;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Persistence
{
    public sealed class PlayerStatsRulesTests
    {
        [Test]
        public void ForMatch_Win_CountsOneMatchAndOneWin()
        {
            var result = new MatchResult(PlayerId.Player1, 9, 3);

            PlayerStatsSummary delta = PlayerStatsRules.ForMatch(PlayerId.Player1, result, 4, 2);

            Assert.That(delta, Is.EqualTo(new PlayerStatsSummary(1, 1, 0, 0, 4, 2)));
        }

        [Test]
        public void ForMatch_Loss_CountsOneLoss()
        {
            var result = new MatchResult(PlayerId.Player1, 9, 3);

            Assert.That(PlayerStatsRules.ForMatch(PlayerId.Player2, result, 1, 0), Is.EqualTo(new PlayerStatsSummary(1, 0, 0, 1, 1, 0)));
        }

        [Test]
        public void ForMatch_Draw_CountsOneDraw()
        {
            var result = new MatchResult(PlayerId.None, 5, 5);

            Assert.That(PlayerStatsRules.ForMatch(PlayerId.Player2, result, 3, 0), Is.EqualTo(new PlayerStatsSummary(1, 0, 1, 0, 3, 0)));
        }

        [Test]
        public void ForMatch_ForfeitWinnerBehindOnScore_StillWins()
        {
            var result = new MatchResult(PlayerId.Player2, 10, 1, MatchEndReason.Forfeit);

            Assert.That(PlayerStatsRules.ForMatch(PlayerId.Player2, result, 1, 0).Wins, Is.EqualTo(1));
            Assert.That(PlayerStatsRules.ForMatch(PlayerId.Player1, result, 4, 0).Losses, Is.EqualTo(1));
        }

        [Test]
        public void ForMatch_NoLocalSeat_RecordsNothing()
        {
            var result = new MatchResult(PlayerId.Player1, 1, 0);

            Assert.That(PlayerStatsRules.IsZero(PlayerStatsRules.ForMatch(PlayerId.None, result, 5, 5)), Is.True);
        }

        [Test]
        public void ForMatch_NegativeCounts_AreZero()
        {
            var result = new MatchResult(PlayerId.Player1, 1, 0);

            PlayerStatsSummary delta = PlayerStatsRules.ForMatch(PlayerId.Player1, result, -3, -1);

            Assert.That(delta.AnimalsScored, Is.Zero);
            Assert.That(delta.TacklesLanded, Is.Zero);
        }

        [Test]
        public void Add_Saturates()
        {
            var a = new PlayerStatsSummary(int.MaxValue, 1, 0, 0, int.MaxValue - 1, 0);
            var b = new PlayerStatsSummary(5, 1, 0, 0, 5, 0);

            PlayerStatsSummary sum = PlayerStatsRules.Add(a, b);

            Assert.That(sum.MatchesPlayed, Is.EqualTo(int.MaxValue));
            Assert.That(sum.AnimalsScored, Is.EqualTo(int.MaxValue));
            Assert.That(sum.Wins, Is.EqualTo(2));
        }

        [Test]
        public void Subtract_FloorsAtZero()
        {
            var a = new PlayerStatsSummary(2, 1, 0, 1, 3, 0);
            var b = new PlayerStatsSummary(1, 2, 0, 0, 5, 0);

            Assert.That(PlayerStatsRules.Subtract(a, b), Is.EqualTo(new PlayerStatsSummary(1, 0, 0, 1, 0, 0)));
        }

        [Test]
        public void Sanitize_ClearsNegativesAndRepairsMatches()
        {
            var raw = new PlayerStatsSummary(2, 3, 1, -4, -2, 7);

            Assert.That(PlayerStatsRules.Sanitize(raw), Is.EqualTo(new PlayerStatsSummary(4, 3, 1, 0, 0, 7)));
        }

        [Test]
        public void Sanitize_KeepsMoreMatchesThanDecided()
        {
            var raw = new PlayerStatsSummary(10, 1, 1, 1, 0, 0);

            Assert.That(PlayerStatsRules.Sanitize(raw), Is.EqualTo(raw));
        }

        [Test]
        public void WinAndLoss_HelpersCountOneMatch()
        {
            Assert.That(PlayerStatsRules.Win(2, 0), Is.EqualTo(new PlayerStatsSummary(1, 1, 0, 0, 2, 0)));
            Assert.That(PlayerStatsRules.Loss(0, 1), Is.EqualTo(new PlayerStatsSummary(1, 0, 0, 1, 0, 1)));
        }
    }
}
