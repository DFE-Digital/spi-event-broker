using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Application.Receive;
using Dfe.Spi.EventBroker.Domain.Events;
using Dfe.Spi.EventBroker.Domain.Publishers;
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

            _loggerMock = new Mock<ILoggerWrapper>();

            _manager = new ReceiveManager(
                _publisherRepositoryMock.Object,
                _eventRepositoryMock.Object,
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
                    p => Regex.IsMatch(p.Id, guidPattern) && p.Payload == DefaultPayload), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}