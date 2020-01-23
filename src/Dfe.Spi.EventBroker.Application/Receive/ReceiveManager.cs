using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Domain.Publishers;
using Newtonsoft.Json.Linq;

namespace Dfe.Spi.EventBroker.Application.Receive
{
    public interface IReceiveManager
    {
        Task ReceiveAsync(string source, string eventType, string payload, CancellationToken cancellationToken);
    }

    public class ReceiveManager : IReceiveManager
    {
        private readonly IPublisherRepository _publisherRepository;
        private readonly ILoggerWrapper _logger;

        public ReceiveManager(
            IPublisherRepository publisherRepository,
            ILoggerWrapper logger)
        {
            _publisherRepository = publisherRepository;
            _logger = logger;
        }
        
        public async Task ReceiveAsync(string source, string eventType, string payload, CancellationToken cancellationToken)
        {
            await ValidateRequestAsync(source, eventType, payload, cancellationToken);
            
            // TODO: Store publication
            
            // TODO: Queue distributions to subscribers
        }

        private async Task ValidateRequestAsync(string source, string eventType, string payload,
            CancellationToken cancellationToken)
        {
            JToken jsonPayload;
            try
            {
                jsonPayload = JToken.Parse(payload);
            }
            catch (Exception ex)
            {
                throw new InvalidRequestException("PAYLOADNOTJSON", "Payload is not valid JSON", ex);
            }

            var publisher = await _publisherRepository.GetPublisherAsync(source, cancellationToken);
            if (publisher == null)
            {
                throw new InvalidRequestException("SOURCENOTFOUND", $"Cannot find publisher with code {source}");
            }
            
            var publisherEvent = publisher.Events.SingleOrDefault(e=>
                e.Name.Equals(eventType, StringComparison.InvariantCultureIgnoreCase));
            if (publisherEvent == null)
            {
                throw new InvalidRequestException("EVENTNOTFOUND", $"Cannot find event {eventType} in publisher {source}");
            }
            
            var validationErrors = publisherEvent.Schema.Validate(jsonPayload);
            if (validationErrors.Count > 0)
            {
                var message = validationErrors.Select(ve => ve.ToString()).Aggregate((x, y) => $"{x}\n{y}");
                throw new InvalidRequestException("PAYLOADINVALID", $"Payload does not conform to schema for {eventType} in publisher {source}:\n{message}");
            }
        }
    }
}