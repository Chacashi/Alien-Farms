using AlienFarms.Networking.Diagnostics;
using AlienFarms.Networking.Interest;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    public sealed class InterestStatsFormatterTests
    {
        private const string HostText =
            "AOI ON 30/36 m \u00b7 cada 5 ticks\n" +
            "Conocidos 41  anim 28 \u00b7 bombas 1 \u00b7 otros 12\n" +
            "P2 ve  anim 14/38 \u00b7 bombas 0/1\n" +
            "Altas 23  Bajas 21  \u00b7 ventana 37 s\n" +
            "Tx ventana 412 KB \u00b7 6.9 KB/s";

        [Test]
        public void HostShowsAllLines()
        {
            Assert.That(Render(CreateSample(isHost: true), InterestStatsFormatter.BufferSize), Is.EqualTo(HostText));
        }

        [Test]
        public void ClientOmitsHostLines()
        {
            const string expected =
                "AOI: servidor\n" +
                "Conocidos 41  anim 28 \u00b7 bombas 1 \u00b7 otros 12\n" +
                "Rx ventana 412 KB \u00b7 6.9 KB/s";
            Assert.That(Render(CreateSample(isHost: false), InterestStatsFormatter.BufferSize), Is.EqualTo(expected));
        }

        [Test]
        public void TrafficDirectionChangesTheLabelAndSampleEquality()
        {
            InterestStatsSample tx = CreateSample(isHost: true);
            InterestStatsSample rx = CreateSample(isHost: true, isTx: false);

            Assert.That(tx.Equals(rx), Is.False);
            Assert.That(Render(rx, InterestStatsFormatter.BufferSize), Does.EndWith("Rx ventana 412 KB \u00b7 6.9 KB/s"));
        }

        [TestCase(0)]
        [TestCase(13)]
        [TestCase(82)]
        public void TruncatesToBufferLength(int capacity)
        {
            Assert.That(Render(CreateSample(isHost: true), capacity), Is.EqualTo(HostText.Substring(0, capacity)));
        }

        private static InterestStatsSample CreateSample(bool isHost, bool? isTx = null)
        {
            var known = new KnownObjectsCounts(28, 1, 12);
            var remote = new InterestCounters(23, 21, 14, 0);
            return new InterestStatsSample(isHost, true, 30f, 36f, 5, known, remote, 38, 1, 37, isTx ?? isHost, 412, 69);
        }

        private static string Render(InterestStatsSample sample, int capacity)
        {
            char[] buffer = new char[capacity];
            int count = InterestStatsFormatter.Write(buffer, sample);
            return new string(buffer, 0, count);
        }
    }
}
