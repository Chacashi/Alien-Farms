using System.Reflection;
using AlienFarms.Data;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    public sealed class UiSfxLibraryTests
    {
        [TestCase(UiSfxCue.None, null)]
        [TestCase(UiSfxCue.CreateRoom, "_createRoom")]
        [TestCase(UiSfxCue.JoinRoom, "_joinRoom")]
        [TestCase(UiSfxCue.CharacterConfirm, "_characterConfirm")]
        [TestCase(UiSfxCue.CharacterRelease, "_characterRelease")]
        [TestCase(UiSfxCue.PlayerJoined, "_playerJoined")]
        [TestCase(UiSfxCue.ConnectionSucceeded, "_connectionSucceeded")]
        [TestCase(UiSfxCue.ConnectionLost, "_connectionLost")]
        [TestCase(UiSfxCue.MatchSuspended, "_matchSuspended")]
        [TestCase(UiSfxCue.PlayerReconnected, "_playerReconnected")]
        [TestCase(UiSfxCue.MatchResumed, "_matchResumed")]
        [TestCase(UiSfxCue.MatchPaused, "_matchPaused")]
        public void Get_ReturnsConfiguredCue(UiSfxCue cue, string fieldName)
        {
            var library = ScriptableObject.CreateInstance<UiSfxLibrary>();
            try
            {
                if (fieldName == null)
                {
                    Assert.That(library.Get(cue), Is.Null);
                    return;
                }

                FieldInfo field = typeof(UiSfxLibrary).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null);
                object expected = field.GetValue(library);
                Assert.That(expected, Is.Not.Null);
                Assert.That(library.Get(cue), Is.SameAs(expected));
            }
            finally
            {
                Object.DestroyImmediate(library);
            }
        }
    }
}
