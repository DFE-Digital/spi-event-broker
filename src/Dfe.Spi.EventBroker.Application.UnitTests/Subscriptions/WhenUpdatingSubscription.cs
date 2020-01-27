using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Application.Subscriptions;
using Dfe.Spi.EventBroker.Domain.Subscriptions;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.EventBroker.Application.UnitTests.Subscriptions
{
    public class WhenUpdatingSubscription
    {
        private Mock<ISubscriptionRepository> _subscriptionRepositoryMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private SubscriptionManager _manager;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _subscriptionRepositoryMock = new Mock<ISubscriptionRepository>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _manager = new SubscriptionManager(
                _subscriptionRepositoryMock.Object,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldUpdateSubscriptionInRepository(Subscription subscription)
        {
            await _manager.UpdateSubscriptionAsync(subscription, _cancellationToken);

            _subscriptionRepositoryMock.Verify(r => r.UpdateSubscriptionAsync(subscription, _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldCreateIdIfOneNotSpecified(Subscription subscription)
        {
            subscription.Id = String.Empty;

            await _manager.UpdateSubscriptionAsync(subscription, _cancellationToken);

            _subscriptionRepositoryMock.Verify(
                r => r.UpdateSubscriptionAsync(It.Is<Subscription>(s => !string.IsNullOrEmpty(s.Id)),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnSubscription(Subscription subscription)
        {
            var actual = await _manager.UpdateSubscriptionAsync(subscription, _cancellationToken);

            Assert.AreEqual(subscription.Id, actual.Id);
            Assert.AreEqual(subscription.Publisher, actual.Publisher);
            Assert.AreEqual(subscription.EventType, actual.EventType);
            Assert.AreEqual(subscription.EndpointUrl, actual.EndpointUrl);
        }
    }
}