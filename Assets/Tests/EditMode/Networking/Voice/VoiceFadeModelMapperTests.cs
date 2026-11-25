using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Networking.Voice;
using Unity.Services.Vivox;
using UnityEngine;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Voice
{
    public sealed class VoiceFadeModelMapperTests
    {
        [TestCase(VoiceFadeModel.InverseByDistance, AudioFadeModel.InverseByDistance)]
        [TestCase(VoiceFadeModel.LinearByDistance, AudioFadeModel.LinearByDistance)]
        [TestCase(VoiceFadeModel.ExponentialByDistance, AudioFadeModel.ExponentialByDistance)]
        public void ToVivox_MapsByModel(VoiceFadeModel model, AudioFadeModel expected)
        {
            Assert.That(VoiceFadeModelMapper.ToVivox(model), Is.EqualTo(expected));
        }

        [Test]
        public void ToChannel3DProperties_UsesSettingsDefaults()
        {
            var settings = ScriptableObject.CreateInstance<VoiceChatSettings>();
            try
            {
                Channel3DProperties props = VoiceFadeModelMapper.ToChannel3DProperties(settings);

                Assert.That(props.AudibleDistance, Is.EqualTo(30));
                Assert.That(props.ConversationalDistance, Is.EqualTo(1));
                Assert.That(props.AudioFadeIntensityByDistance, Is.EqualTo(1f));
                Assert.That(props.AudioFadeModel, Is.EqualTo(AudioFadeModel.InverseByDistance));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }
    }
}
