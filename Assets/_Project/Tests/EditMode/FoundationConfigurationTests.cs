using NUnit.Framework;
using OneStep.Core.Configuration;
using OneStep.Networking;
using UnityEngine;

namespace OneStep.Tests.EditMode
{
    public sealed class FoundationConfigurationTests
    {
        [Test]
        public void ViewportDefaults_ToNineBySixteenTileFrame()
        {
            var configuration = ScriptableObject.CreateInstance<ViewportConfiguration>();
            Assert.That(configuration.ReferenceWidth, Is.EqualTo(144));
            Assert.That(configuration.ReferenceHeight, Is.EqualTo(256));
            Assert.That(configuration.AssetsPixelsPerUnit, Is.EqualTo(16));
            Object.DestroyImmediate(configuration);
        }

        [Test]
        public void InvitationService_RoundTripsJoinCode()
        {
            var service = new SessionCodeDuelInvitationService();
            var invitation = service.CreateInvitation("ab12cd");
            Assert.That(service.TryParseInvitation(invitation, out var code), Is.True);
            Assert.That(code, Is.EqualTo("AB12CD"));
        }
    }
}
