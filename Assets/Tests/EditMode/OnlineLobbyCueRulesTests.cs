using AlienFarms.Audio;
using AlienFarms.Core;
using AlienFarms.Data;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class OnlineLobbyCueRulesTests
    {
        private const int NoPick = OnlineLobbySeat.NoCharacter;

        [Test]
        public void Diff_FirstPublication_IsSilent()
        {
            var output = new UiSfxCue[4];
            Assert.That(OnlineLobbyCueRules.Diff(default, Room(0, 1), true, output), Is.Zero);
        }

        [TestCase(true, NoPick, 1, UiSfxCue.CharacterConfirm)]
        [TestCase(true, 1, NoPick, UiSfxCue.CharacterRelease)]
        [TestCase(false, NoPick, 1, UiSfxCue.CharacterConfirm)]
        [TestCase(false, 1, NoPick, UiSfxCue.CharacterRelease)]
        public void Diff_PickChanges_EmitCue(bool local, int oldPick, int newPick, UiSfxCue expected)
        {
            OnlineLobbySnapshot previous = local ? Room(oldPick, NoPick) : Room(NoPick, oldPick);
            OnlineLobbySnapshot current = local ? Room(newPick, NoPick) : Room(NoPick, newPick);
            var output = new UiSfxCue[4];

            Assert.That(OnlineLobbyCueRules.Diff(previous, current, true, output), Is.EqualTo(1));
            Assert.That(output[0], Is.EqualTo(expected));
        }

        [TestCase(NoPick, 1)]
        [TestCase(1, NoPick)]
        public void Diff_RemotePicksDisabled_OnlyEmitsLocalCue(int oldRemotePick, int newRemotePick)
        {
            var output = new UiSfxCue[4];

            int count = OnlineLobbyCueRules.Diff(Room(NoPick, oldRemotePick), Room(0, newRemotePick), false, output);

            Assert.That(count, Is.EqualTo(1));
            Assert.That(output[0], Is.EqualTo(UiSfxCue.CharacterConfirm));
        }

        [Test]
        public void Diff_OpenToStarting_IsSilent()
        {
            var output = new UiSfxCue[4];
            Assert.That(OnlineLobbyCueRules.Diff(Room(NoPick, NoPick),
                Room(0, 1, OnlineLobbyPhase.Starting), true, output), Is.Zero);
        }

        [Test]
        public void Diff_SamePickStateWithChangedSeatMetadata_IsSilent()
        {
            var current = new OnlineLobbySnapshot(OnlineLobbyPhase.Open, false, 2,
                new OnlineLobbySeat(PlayerId.Player2, 2, "Renamed", false, true),
                new OnlineLobbySeat(PlayerId.Player1, NoPick, "Local", true, false));
            var output = new UiSfxCue[4];

            Assert.That(OnlineLobbyCueRules.Diff(Room(NoPick, 1), current, true, output), Is.Zero);
        }

        private static OnlineLobbySnapshot Room(int localPick, int remotePick, OnlineLobbyPhase phase = OnlineLobbyPhase.Open)
        {
            return new OnlineLobbySnapshot(phase, false, 2,
                new OnlineLobbySeat(PlayerId.Player1, localPick, "Local", true, true),
                new OnlineLobbySeat(PlayerId.Player2, remotePick, "Remote", false, false));
        }
    }
}
