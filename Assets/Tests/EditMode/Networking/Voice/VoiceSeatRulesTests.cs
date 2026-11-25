using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Networking.Voice;
using UnityEngine;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Voice
{
    public sealed class VoiceSeatRulesTests
    {
        [TestCase("host", PlayerId.Player1)]
        [TestCase("guest", PlayerId.Player2)]
        [TestCase("unknown", PlayerId.None)]
        [TestCase(null, PlayerId.None)]
        public void SeatOf_UsesParticipantRole(string id, PlayerId expected)
        {
            var channel = ScriptableObject.CreateInstance<OnlineSessionChannel>();
            try
            {
                channel.SetParticipants(new[]
                {
                    new OnlineParticipant("guest", "Guest", false, true),
                    new OnlineParticipant("host", "Host", true, false)
                }, 2);

                Assert.That(VoiceSeatRules.SeatOf(id, channel), Is.EqualTo(expected));
            }
            finally
            {
                Object.DestroyImmediate(channel);
            }
        }
    }
}
