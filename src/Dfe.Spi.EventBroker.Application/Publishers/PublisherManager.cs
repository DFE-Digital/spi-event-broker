using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Domain.Publishers;

namespace Dfe.Spi.EventBroker.Application.Publishers
{
    public interface IPublisherManager
    {
        Task UpdatePublishedEventsAsync(Publisher publisher, CancellationToken cancellationToken);
    }
    public class PublisherManager : IPublisherManager
    {
        private readonly IPublisherRepository _publisherRepository;
        private readonly ILoggerWrapper _logger;

        public PublisherManager(
            IPublisherRepository publisherRepository,
            ILoggerWrapper logger)
        {
            _publisherRepository = publisherRepository;
            _logger = logger;
        }
        
        public async Task UpdatePublishedEventsAsync(Publisher publisher, CancellationToken cancellationToken)
        {
            await _publisherRepository.UpdatePublisherAsync(publisher, cancellationToken);
            _logger.Info($"Created/updated publisher {publisher.Code} with {publisher.Events.Length} events");
        }
    }
}