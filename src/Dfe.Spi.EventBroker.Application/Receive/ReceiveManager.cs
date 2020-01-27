using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Domain.Distributions;
using Dfe.Spi.EventBroker.Domain.Events;
using Dfe.Spi.EventBroker.Domain.Publishers;
using Dfe.Spi.EventBroker.Domain.Subscriptions;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Dfe.Spi.EventBroker.Application.Receive
{
    public interface IReceiveManager
    {
        Task ReceiveAsync(string source, string eventType, string payload, CancellationToken cancellationToken);
    }

    public class ReceiveManager : IReceiveManager
    {
        private readonly IPublisherRepository _publisherRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IDistributionRepository _distributionRepository;
        private readonly IDistributionQueue _distributionQueue;
        private readonly ILoggerWrapper _logger;

        public ReceiveManager(
            IPublisherRepository publisherRepository,
            IEventRepository eventRepository,
            ISubscriptionRepository subscriptionRepository,
            IDistributionRepository distributionRepository,
            IDistributionQueue distributionQueue,
            ILoggerWrapper logger)
        {
            _publisherRepository = publisherRepository;
            _eventRepository = eventRepository;
            _subscriptionRepository = subscriptionRepository;
            _distributionRepository = distributionRepository;
            _distributionQueue = distributionQueue;
            _logger = logger;
        }
        
        public async Task ReceiveAsync(string source, string eventType, string payload, CancellationToken cancellationToken)
        {
            await ValidateRequestAsync(source, eventType, payload, cancellationToken);

            var eventId = Guid.NewGuid().ToString().ToLower();
            await _eventRepository.StoreAsync(new Event
            {
                Id = eventId,
                Publisher = source,
                EventType = eventType,
                Payload = payload,
            }, cancellationToken);

            var subscriptions =
                await _subscriptionRepository.GetSubscriptionsToEventAsync(source, eventType, cancellationToken);
            _logger.Debug($"Found {subscriptions.Length} subscribers to event {source}.{eventType}");
            foreach (var subscription in subscriptions)
            {
                var distribution = new Distribution
                {
                    Id = Guid.NewGuid().ToString(),
                    SubscriptionId = subscription.Id,
                    EventId = eventId,
                    Status = DistributionStatus.Pending,
                    Attempts = 0,
                };
                await _distributionRepository.CreateAsync(distribution, cancellationToken);
                _logger.Debug($"Created distribution with id {distribution.Id} for subscription {subscription.Id} to send event {eventId} ({source}.{eventType})");

                await _distributionQueue.EnqueueAsync(distribution, cancellationToken);
                _logger.Info($"Queued distribution with id {distribution.Id} for subscription {subscription.Id} to send event {eventId} ({source}.{eventType})");
            }
            
            _logger.Info($"Finished receiving and distributing event {eventId} ({source}.{eventType})");
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

            var schema = await JsonSchema.FromJsonAsync(publisherEvent.Schema);
            var validationErrors = schema.Validate(jsonPayload);
            if (validationErrors.Count > 0)
            {
                var message = validationErrors.Select(ve => ve.ToString()).Aggregate((x, y) => $"{x}\n{y}");
                throw new InvalidRequestException("PAYLOADINVALID", $"Payload does not conform to schema for {eventType} in publisher {source}:\n{message}");
            }
        }
    }
}