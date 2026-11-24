using System;
using AlienFarms.Core;
using AlienFarms.Networking.Persistence;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Persistence
{
    public sealed class RivalRulesTests
    {
        [Test]
        public void Player_FindsTheOtherSeatedParticipant()
        {
            OnlineParticipant[] participants =
            {
                new OnlineParticipant("host", "Host", true, false),
                new OnlineParticipant("me", "Me", false, true),
            };

            Assert.That(RivalRules.TryFindRival(participants, out string rival), Is.True);
            Assert.That(rival, Is.EqualTo("host"));
        }

        [Test]
        public void Alone_HasNoRival()
        {
            OnlineParticipant[] participants = { new OnlineParticipant("me", "Me", true, true) };

            Assert.That(RivalRules.TryFindRival(participants, out _), Is.False);
            Assert.That(RivalRules.TryFindRival(ReadOnlySpan<OnlineParticipant>.Empty, out _), Is.False);
        }

        [Test]
        public void ParticipantWithoutId_IsSkipped()
        {
            OnlineParticipant[] participants =
            {
                new OnlineParticipant("me", "Me", true, true),
                new OnlineParticipant(null, "Ghost", false, false)
            };

            Assert.That(RivalRules.TryFindRival(participants, out _), Is.False);
        }
    }
}
