using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Application.Send;
using Dfe.Spi.EventBroker.Domain.Distributions;
using Dfe.Spi.EventBroker.Domain.Events;
using Dfe.Spi.EventBroker.Domain.Subscriptions;
using Moq;
using NUnit.Framework;
using RestSharp;

namespace Dfe.Spi.EventBroker.Application.UnitTests.Send
{
    public class WhenSending
    {
        private Mock<IEventRepository> _eventRepositoryMock;
        private Mock<ISubscriptionRepository> _subscriptionRepositoryMock;
        private Mock<IRestClient> _restClientMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private SendManager _manager;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _eventRepositoryMock = new Mock<IEventRepository>();
            _eventRepositoryMock.Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Event());

            _subscriptionRepositoryMock = new Mock<ISubscriptionRepository>();
            _subscriptionRepositoryMock.Setup(r => r.GetSubscriptionToEventAsync(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Subscription());

            _restClientMock = new Mock<IRestClient>();
            _restClientMock.Setup(c => c.ExecuteTaskAsync(It.IsAny<IRestRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RestResponse
                {
                    StatusCode = HttpStatusCode.OK,
                    ResponseStatus = ResponseStatus.Completed,
                });

            _loggerMock = new Mock<ILoggerWrapper>();

            _manager = new SendManager(
                _eventRepositoryMock.Object,
                _subscriptionRepositoryMock.Object,
                _restClientMock.Object,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldGetEventForDistribution(Distribution distribution)
        {
            await _manager.SendAsync(distribution, _cancellationToken);

            _eventRepositoryMock.Verify(r => r.GetAsync(distribution.EventId, _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldGetSubscriptionForEventAndDistribution(Distribution distribution, Event @event)
        {
            _eventRepositoryMock.Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(@event);

            await _manager.SendAsync(distribution, _cancellationToken);

            _subscriptionRepositoryMock.Verify(r => r.GetSubscriptionToEventAsync(
                    @event.Publisher, @event.EventType, distribution.SubscriptionId, _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldPostEventDataToSubscriber(Distribution distribution, Event @event,
            Subscription subscription)
        {
            _eventRepositoryMock.Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(@event);
            _subscriptionRepositoryMock.Setup(r => r.GetSubscriptionToEventAsync(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription);

            await _manager.SendAsync(distribution, _cancellationToken);

            _restClientMock.Verify(c => c.ExecuteTaskAsync(It.Is<RestRequest>(r =>
                        r.Method == Method.POST &&
                        r.Resource == subscription.EndpointUrl &&
                        r.Parameters.Single(p => p.Type == ParameterType.RequestBody).Value == @event.Payload),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public void ThenItShouldThrowAnExceptionIfNonSuccessResponseRecieved(Distribution distribution)
        {
            _restClientMock.Setup(c => c.ExecuteTaskAsync(It.IsAny<IRestRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RestResponse
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    ResponseStatus = ResponseStatus.Completed,
                });

            Assert.ThrowsAsync<Exception>(async () =>
                await _manager.SendAsync(distribution, _cancellationToken));
        }
    }
}