using System;
using System.Reflection;
using AlienFarms.Networking.Session;
using NUnit.Framework;
using Unity.Services.Multiplayer;

namespace AlienFarms.Networking.Tests
{
    /// <summary>
    /// Guards the private SDK API used by <see cref="SessionSdkWorkarounds"/>: an SDK update that renames or removes it
    /// fails here instead of silently disabling the orphaned-session release.
    /// </summary>
    public sealed class SessionSdkWorkaroundsTests
    {
        [Test]
        public void SessionHandler_HasPrivateParameterlessOnRemovedFromSession()
        {
            Type handlerType = typeof(ISession).Assembly.GetType(SessionSdkWorkarounds.SessionHandlerTypeName, false);
            Assert.That(handlerType, Is.Not.Null, "SDK SessionHandler type not found.");
            Assert.That(typeof(ISession).IsAssignableFrom(handlerType), Is.True, "SessionHandler no longer implements ISession.");

            MethodInfo method = handlerType.GetMethod(
                "OnRemovedFromSession",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);
            Assert.That(method, Is.Not.Null, "SessionHandler.OnRemovedFromSession() not found.");
            Assert.That(method.IsPrivate, Is.True);
        }

        [Test]
        public void IsSupported_IsTrueForInstalledSdk()
        {
            Assert.That(SessionSdkWorkarounds.IsSupported, Is.True);
        }
    }
}
