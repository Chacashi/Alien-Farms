using AlienFarms.Core;
using AlienFarms.Menu;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class CharacterSelectionTests
    {
        private static CharacterSelection CreateWithBothJoined(int characters = 2)
        {
            var selection = new CharacterSelection(characters);
            selection.Join(PlayerId.Player1);
            selection.Join(PlayerId.Player2);
            return selection;
        }

        [Test]
        public void Join_GivesEachPlayerADifferentHover()
        {
            CharacterSelection selection = CreateWithBothJoined();

            Assert.That(selection.GetHover(PlayerId.Player1), Is.EqualTo(0));
            Assert.That(selection.GetHover(PlayerId.Player2), Is.EqualTo(1));
            Assert.That(selection.JoinedCount, Is.EqualTo(2));
        }

        [Test]
        public void Join_Twice_Fails()
        {
            var selection = new CharacterSelection(2);
            selection.Join(PlayerId.Player1);

            Assert.That(selection.Join(PlayerId.Player1), Is.False);
        }

        [Test]
        public void Join_ThirdPlayer_Fails()
        {
            CharacterSelection selection = CreateWithBothJoined();

            Assert.That(selection.Join((PlayerId)3), Is.False);
            Assert.That(selection.Join(PlayerId.None), Is.False);
            Assert.That(selection.JoinedCount, Is.EqualTo(2));
        }

        [Test]
        public void Join_WithNoCharacters_Fails()
        {
            var selection = new CharacterSelection(0);

            Assert.That(selection.Join(PlayerId.Player1), Is.False);
        }

        [Test]
        public void Confirm_LocksTheHoveredCharacter()
        {
            CharacterSelection selection = CreateWithBothJoined();

            Assert.That(selection.Confirm(PlayerId.Player1), Is.True);
            Assert.That(selection.IsConfirmed(PlayerId.Player1), Is.True);
            Assert.That(selection.GetConfirmedIndex(PlayerId.Player1), Is.EqualTo(0));
            Assert.That(selection.LockedBy(0), Is.EqualTo(PlayerId.Player1));
            Assert.That(selection.LockedBy(1), Is.EqualTo(PlayerId.None));
        }

        [Test]
        public void TakenCharacter_CannotBeHoveredOrConfirmedByOther()
        {
            var selection = new CharacterSelection(2);
            selection.Join(PlayerId.Player1);
            selection.Confirm(PlayerId.Player1);
            selection.Join(PlayerId.Player2);
            selection.Cancel(PlayerId.Player1);
            // Player 2 hovers 1; force both onto 0 then lock it with Player 1.
            selection.Move(PlayerId.Player2, 1);
            Assert.That(selection.GetHover(PlayerId.Player2), Is.EqualTo(0));
            selection.Confirm(PlayerId.Player1);

            Assert.That(selection.GetHover(PlayerId.Player2), Is.EqualTo(1), "Confirm should move the other's hover away.");

            // Even if Player 2 somehow hovered the locked character, confirming it fails.
            Assert.That(selection.Move(PlayerId.Player2, 1), Is.False, "The only other character is locked.");
            Assert.That(selection.Confirm(PlayerId.Player2), Is.True);
            Assert.That(selection.GetConfirmedIndex(PlayerId.Player2), Is.EqualTo(1));
        }

        [Test]
        public void Confirm_MovesOtherPlayersHoverToAFreeCharacter()
        {
            var selection = new CharacterSelection(3);
            selection.Join(PlayerId.Player1);
            selection.Join(PlayerId.Player2);
            selection.Move(PlayerId.Player2, -1);
            Assert.That(selection.GetHover(PlayerId.Player2), Is.EqualTo(0));

            selection.Confirm(PlayerId.Player1);

            Assert.That(selection.GetHover(PlayerId.Player2), Is.Not.EqualTo(0));
        }

        [Test]
        public void Confirm_Twice_Fails()
        {
            CharacterSelection selection = CreateWithBothJoined();
            selection.Confirm(PlayerId.Player1);

            Assert.That(selection.Confirm(PlayerId.Player1), Is.False);
        }

        [Test]
        public void Move_IsCyclic()
        {
            var selection = new CharacterSelection(3);
            selection.Join(PlayerId.Player1);

            Assert.That(selection.Move(PlayerId.Player1, -1), Is.True);
            Assert.That(selection.GetHover(PlayerId.Player1), Is.EqualTo(2));
            Assert.That(selection.Move(PlayerId.Player1, 1), Is.True);
            Assert.That(selection.GetHover(PlayerId.Player1), Is.EqualTo(0));
        }

        [Test]
        public void Move_SkipsCharactersLockedByOthers()
        {
            var selection = new CharacterSelection(3);
            selection.Join(PlayerId.Player1);
            selection.Join(PlayerId.Player2);
            selection.Confirm(PlayerId.Player2); // locks 1

            selection.Move(PlayerId.Player1, 1);

            Assert.That(selection.GetHover(PlayerId.Player1), Is.EqualTo(2));
        }

        [Test]
        public void Move_WhileConfirmed_Fails()
        {
            CharacterSelection selection = CreateWithBothJoined(3);
            selection.Confirm(PlayerId.Player1);

            Assert.That(selection.Move(PlayerId.Player1, 1), Is.False);
            Assert.That(selection.GetHover(PlayerId.Player1), Is.EqualTo(0));
        }

        [Test]
        public void Cancel_UnlocksTheCharacter()
        {
            CharacterSelection selection = CreateWithBothJoined();
            selection.Confirm(PlayerId.Player1);

            Assert.That(selection.Cancel(PlayerId.Player1), Is.True);
            Assert.That(selection.IsConfirmed(PlayerId.Player1), Is.False);
            Assert.That(selection.LockedBy(0), Is.EqualTo(PlayerId.None));
        }

        [Test]
        public void Cancel_WhenNotConfirmed_ReturnsFalse()
        {
            CharacterSelection selection = CreateWithBothJoined();

            Assert.That(selection.Cancel(PlayerId.Player1), Is.False);
        }

        [Test]
        public void Leave_FreesTheLock()
        {
            CharacterSelection selection = CreateWithBothJoined();
            selection.Confirm(PlayerId.Player1);

            Assert.That(selection.Leave(PlayerId.Player1), Is.True);
            Assert.That(selection.IsJoined(PlayerId.Player1), Is.False);
            Assert.That(selection.LockedBy(0), Is.EqualTo(PlayerId.None));
            Assert.That(selection.GetHover(PlayerId.Player1), Is.EqualTo(CharacterSelection.NoCharacter));
        }

        [Test]
        public void AllConfirmed_OnlyWithBothPlayersConfirmed()
        {
            var selection = new CharacterSelection(2);
            selection.Join(PlayerId.Player1);
            selection.Confirm(PlayerId.Player1);
            Assert.That(selection.AllConfirmed, Is.False, "Only one player joined.");

            selection.Join(PlayerId.Player2);
            Assert.That(selection.AllConfirmed, Is.False, "Player 2 not confirmed.");

            selection.Confirm(PlayerId.Player2);
            Assert.That(selection.AllConfirmed, Is.True);
        }

        [Test]
        public void SingleCharacter_OnlyOnePlayerCanLockIt()
        {
            var selection = new CharacterSelection(1);
            Assert.That(selection.Join(PlayerId.Player1), Is.True);
            Assert.That(selection.Join(PlayerId.Player2), Is.True);
            Assert.That(selection.GetHover(PlayerId.Player1), Is.EqualTo(0));
            Assert.That(selection.GetHover(PlayerId.Player2), Is.EqualTo(0));
            Assert.That(selection.Move(PlayerId.Player2, 1), Is.False);

            Assert.That(selection.Confirm(PlayerId.Player1), Is.True);

            Assert.That(selection.Confirm(PlayerId.Player2), Is.False);
            Assert.That(selection.Move(PlayerId.Player2, 1), Is.False);
            Assert.That(selection.AllConfirmed, Is.False);
            Assert.That(selection.LockedBy(0), Is.EqualTo(PlayerId.Player1));
        }

        [Test]
        public void FinalPicks_AreUnique()
        {
            var selection = new CharacterSelection(2);
            selection.Join(PlayerId.Player1);
            selection.Join(PlayerId.Player2);
            selection.Move(PlayerId.Player2, 1); // both hover 0
            selection.Confirm(PlayerId.Player2);
            selection.Confirm(PlayerId.Player1);

            Assert.That(selection.AllConfirmed, Is.True);
            Assert.That(selection.GetConfirmedIndex(PlayerId.Player1), Is.Not.EqualTo(selection.GetConfirmedIndex(PlayerId.Player2)));
        }
    }
}
