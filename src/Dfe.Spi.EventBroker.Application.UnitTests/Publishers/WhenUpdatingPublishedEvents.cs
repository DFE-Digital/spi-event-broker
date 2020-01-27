using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Application.Publishers;
using Dfe.Spi.EventBroker.Domain.Publishers;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.EventBroker.Application.UnitTests.Publishers
{
    public class WhenUpdatingPublishedEvents
    {
        private Mock<IPublisherRepository> _publisherRepositoryMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private PublisherManager _manager;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _publisherRepositoryMock = new Mock<IPublisherRepository>();
            
            _loggerMock = new Mock<ILoggerWrapper>();
            
            _manager = new PublisherManager(
                _publisherRepositoryMock.Object,
                _loggerMock.Object);
            
            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldUpdatePublisherInRepository(Publisher publisher)
        {
            await _manager.UpdatePublishedEventsAsync(publisher, _cancellationToken);
            
            _publisherRepositoryMock.Verify(r=>r.UpdatePublisherAsync(publisher, _cancellationToken),
                 Times.Once);
        }
    }
}