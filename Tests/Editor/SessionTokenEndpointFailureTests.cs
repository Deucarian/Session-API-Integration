using NUnit.Framework;

namespace Deucarian.Session.APIIntegration.Tests
{
    public sealed class SessionTokenEndpointFailureTests
    {
        [Test]
        public void MissingEndpointHasAnActionableSafeDescription()
        {
            var result = SessionTokenEndpointFailures.FromHttpStatus(404);
            Assert.That(result.Error.Message, Does.Contain("No authentication endpoint"));
            Assert.That(result.Error.Message, Does.Contain("404"));
            Assert.That(result.Error.Message, Does.Contain("environment"));
            Assert.That(SessionTokenEndpointFailures.Describe(result.Error.Code), Is.EqualTo(result.Error.Message));
        }

        [TestCase(null)]
        [TestCase("unexpected-token-bearing-code")]
        [TestCase("<b>untrusted markup</b>")]
        public void UnknownCodesAreNeverEchoed(string code)
        {
            string description = SessionTokenEndpointFailures.Describe(code);
            Assert.That(description, Is.EqualTo("Authentication failed. Check the configured endpoint and supplied values, then try again."));
        }

        [Test]
        public void MissingTransportStatusHasAConnectionRepairHint()
        {
            var result = SessionTokenEndpointFailures.FromHttpStatus(null);
            Assert.That(result.Error.Code, Is.EqualTo(SessionTokenEndpointErrorCodes.RequestFailed));
            Assert.That(result.Error.Message, Does.Contain("connection"));
            Assert.That(result.Error.Exception, Is.Null);
        }
    }
}
