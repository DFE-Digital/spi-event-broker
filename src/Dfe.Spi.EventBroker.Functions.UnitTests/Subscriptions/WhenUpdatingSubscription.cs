using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Dfe.Spi.Common.Http.Server;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Application.Subscriptions;
using Dfe.Spi.EventBroker.Domain.Subscriptions;
using Dfe.Spi.EventBroker.Functions.Subscriptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.EventBroker.Functions.UnitTests.Subscriptions
{
    public class WhenUpdatingSubscription
    {
        private Fixture _fixture;
        private Mock<ISubscriptionManager> _subscriptionManagerMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private UpdateSubscription _function;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _fixture = new Fixture();
            _fixture.Register<Uri>(() => new Uri($"https://{_fixture.Create<string>()}.com/{_fixture.Create<string>()}"));

            _subscriptionManagerMock = new Mock<ISubscriptionManager>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _function = new UpdateSubscription(
                _subscriptionManagerMock.Object,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test]
        public async Task ThenItShouldReturnAccepted()
        {
            var httpRequest = TestHelpers.BuildRequestWithBody(new UpdateSubscriptionRequest
            {
                Publisher = _fixture.Create<string>(),
                EventType = _fixture.Create<string>(),
                EndpointUrl = _fixture.Create<Uri>().ToString(),
            });

            var actual = await _function.RunAsync(httpRequest, _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<AcceptedResult>(actual);
        }

        [TestCase(null)]
        [TestCase("1c93fb13-6e9f-4bad-8195-d9c59b533852")]
        public async Task ThenItShouldCallManagerWithSubscriptionDetails(string subscriptionId)
        {
            var request = new UpdateSubscriptionRequest
            {
                SubscriptionId = subscriptionId,
                Publisher = _fixture.Create<string>(),
                EventType = _fixture.Create<string>(),
                EndpointUrl = _fixture.Create<Uri>().ToString(),
            };
            var httpRequest = TestHelpers.BuildRequestWithBody(request);

            await _function.RunAsync(httpRequest, _cancellationToken);

            _subscriptionManagerMock.Verify(m => m.UpdateSubscriptionAsync(
                    It.Is<Subscription>(s =>
                        s.Id == subscriptionId &&
                        s.Publisher == request.Publisher &&
                        s.EventType == request.EventType &&
                        s.EndpointUrl == request.EndpointUrl), _cancellationToken),
                Times.Once);
        }

        [TestCase(null, "event-one", "https://example.com")]
        [TestCase("", "event-one", "https://example.com")]
        [TestCase("publisher-one", null, "https://example.com")]
        [TestCase("publisher-one", "", "https://example.com")]
        [TestCase("publisher-one", "event-one", null)]
        [TestCase("publisher-one", "event-one", "")]
        [TestCase("publisher-one", "event-one", "ftp://example.com")]
        [TestCase("publisher-one", "event-one", "http://example.com")]
        public async Task ThenItShouldReturnBadRequestIfRequestInvalid(string publisher, string eventType,
            string endpointUrl)
        {
            var httpRequest = TestHelpers.BuildRequestWithBody(new UpdateSubscriptionRequest
            {
                Publisher = publisher,
                EventType = eventType,
                EndpointUrl = endpointUrl,
            });

            var actual = await _function.RunAsync(httpRequest, _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<HttpErrorBodyResult>(actual);
            Assert.AreEqual(400, ((HttpErrorBodyResult) actual).StatusCode);
        }
    }
}