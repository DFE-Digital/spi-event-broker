using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Application.Receive;
using Dfe.Spi.EventBroker.Domain.Distributions;
using Dfe.Spi.EventBroker.Domain.Events;
using Dfe.Spi.EventBroker.Domain.Publishers;
using Dfe.Spi.EventBroker.Domain.Subscriptions;
using Moq;
using NJsonSchema;
using NUnit.Framework;

namespace Dfe.Spi.EventBroker.Application.UnitTests.Receive
{
    public class WhenReceivingNewEventPublication
    {
        private const string DefaultSource = "Place";
        private const string DefaultEventType = "Thing";
        private const string DefaultPayload = "{\"prop1\":1}";
        private const string DefaultSchema = "{\"properties\":{\"prop1\":{\"type\": \"integer\"}}}";

        private Mock<IPublisherRepository> _publisherRepositoryMock;
        private Mock<IEventRepository> _eventRepositoryMock;
        private Mock<ISubscriptionRepository> _subscriptionRepositoryMock;
        private Mock<IDistributionRepository> _distributionRepositoryMock;
        private Mock<IDistributionQueue> _distributionQueueMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private ReceiveManager _manager;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _publisherRepositoryMock = new Mock<IPublisherRepository>();
            _publisherRepositoryMock.Setup(r => r.GetPublisherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Publisher
                {
                    Events = new[]
                    {
                        new PublisherEvent
                        {
                            Name = DefaultEventType,
                            Schema = JsonSchema.FromJsonAsync(DefaultSchema).Result,
                        },
                    },
                });

            _eventRepositoryMock = new Mock<IEventRepository>();

            _subscriptionRepositoryMock = new Mock<ISubscriptionRepository>();

            _distributionRepositoryMock = new Mock<IDistributionRepository>();

            _distributionQueueMock = new Mock<IDistributionQueue>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _manager = new ReceiveManager(
                _publisherRepositoryMock.Object,
                _eventRepositoryMock.Object,
                _subscriptionRepositoryMock.Object,
                _distributionRepositoryMock.Object,
                _distributionQueueMock.Object,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test]
        public void ThenItShouldThrowExceptionIfPayloadNotValidJson()
        {
            var actual = Assert.ThrowsAsync<InvalidRequestException>(async () =>
                await _manager.ReceiveAsync(DefaultSource, DefaultEventType, "not-json", _cancellationToken));
            Assert.AreEqual("PAYLOADNOTJSON", actual.Code);
        }

        [Test, AutoData]
        public async Task ThenItShouldGetPublisherDetailsFromRepo(string source)
        {
            await _manager.ReceiveAsync(source, DefaultEventType, DefaultPayload, _cancellationToken);

            _publisherRepositoryMock.Verify(r => r.GetPublisherAsync(source, _cancellationToken),
                Times.Once);
        }

        [Test]
        public void ThenItShouldThrowExceptionIfPublisherNotFound()
        {
            _publisherRepositoryMock.Setup(r => r.GetPublisherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Publisher) null);

            var actual = Assert.ThrowsAsync<InvalidRequestException>(async () =>
                await _manager.ReceiveAsync(DefaultSource, DefaultEventType, DefaultPayload, _cancellationToken));
            Assert.AreEqual("SOURCENOTFOUND", actual.Code);
        }

        [Test, AutoData]
        public void ThenItShouldThrowExceptionIfEventTypeNotFound(string source, string eventType)
        {
            _publisherRepositoryMock.Setup(r => r.GetPublisherAsync(source, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Publisher {Events = new PublisherEvent[0]});

            var actual = Assert.ThrowsAsync<InvalidRequestException>(async () =>
                await _manager.ReceiveAsync(source, eventType, DefaultPayload, _cancellationToken));
            Assert.AreEqual("EVENTNOTFOUND", actual.Code);
        }

        [Test]
        public void ThenItShouldThrowExceptionIfPayloadDoesNotMatchSchema()
        {
            var actual = Assert.ThrowsAsync<InvalidRequestException>(async () =>
                await _manager.ReceiveAsync(DefaultSource, DefaultEventType, "{\"prop1\":true}", _cancellationToken));
            Assert.AreEqual("PAYLOADINVALID", actual.Code);
        }

        [Test]
        public async Task ThenItShouldStoreEvent()
        {
            await _manager.ReceiveAsync(DefaultSource, DefaultEventType, DefaultPayload, _cancellationToken);

            const string guidPattern = "^[a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12}$";
            _eventRepositoryMock.Verify(r => r.StoreAsync(It.Is<Event>(
                        p => Regex.IsMatch(p.Id, guidPattern) && p.Payload == DefaultPayload),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task ThenItShouldGetSubscriptionsForEvent()
        {
            await _manager.ReceiveAsync(DefaultSource, DefaultEventType, DefaultPayload, _cancellationToken);

            _subscriptionRepositoryMock.Verify(r => r.GetSubscriptionsToEventAsync(
                DefaultSource, DefaultEventType, _cancellationToken), Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldCreateDistributionPerSubscription(Subscription[] subscriptions)
        {
            _subscriptionRepositoryMock.Setup(r => r.GetSubscriptionsToEventAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscriptions);

            await _manager.ReceiveAsync(DefaultSource, DefaultEventType, DefaultPayload, _cancellationToken);

            _distributionRepositoryMock.Verify(r => r.CreateAsync(
                    It.IsAny<Distribution>(), It.IsAny<CancellationToken>()),
                Times.Exactly(subscriptions.Length));
            for (var i = 0; i < subscriptions.Length; i++)
            {
                _distributionRepositoryMock.Verify(r => r.CreateAsync(
                        It.Is<Distribution>(d => d.SubscriptionId == subscriptions[i].Id),
                        It.IsAny<CancellationToken>()),
                    Times.Once,
                    $"Expected distribution to be created for subscription {i}");
            }
        }

        [Test, AutoData]
        public async Task ThenItShouldQueueDistributionPerSubscription(Subscription[] subscriptions)
        {
            _subscriptionRepositoryMock.Setup(r => r.GetSubscriptionsToEventAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscriptions);

            await _manager.ReceiveAsync(DefaultSource, DefaultEventType, DefaultPayload, _cancellationToken);

            _distributionQueueMock.Verify(r => r.EnqueueAsync(
                    It.IsAny<Distribution>(), It.IsAny<CancellationToken>()),
                Times.Exactly(subscriptions.Length));
            for (var i = 0; i < subscriptions.Length; i++)
            {
                _distributionQueueMock.Verify(r => r.EnqueueAsync(
                        It.Is<Distribution>(d => d.SubscriptionId == subscriptions[i].Id),
                        It.IsAny<CancellationToken>()),
                    Times.Once,
                    $"Expected distribution to be queued for subscription {i}");
            }
        }
    }
}