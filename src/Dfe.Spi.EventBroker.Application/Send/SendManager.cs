using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Domain.Distributions;
using Dfe.Spi.EventBroker.Domain.Events;
using Dfe.Spi.EventBroker.Domain.Subscriptions;
using RestSharp;

namespace Dfe.Spi.EventBroker.Application.Send
{
    public interface ISendManager
    {
        Task SendAsync(Distribution distribution, CancellationToken cancellationToken);
    }

    public class SendManager : ISendManager
    {
        private readonly IEventRepository _eventRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IRestClient _restClient;
        private readonly ILoggerWrapper _logger;

        public SendManager(
            IEventRepository eventRepository,
            ISubscriptionRepository subscriptionRepository,
            IRestClient restClient,
            ILoggerWrapper logger)
        {
            _eventRepository = eventRepository;
            _subscriptionRepository = subscriptionRepository;
            _restClient = restClient;
            _logger = logger;
        }
        
        public async Task SendAsync(Distribution distribution, CancellationToken cancellationToken)
        {
            _logger.Debug($"Reading event {distribution.EventId}");
            var @event = await _eventRepository.GetAsync(distribution.EventId, cancellationToken);
            
            _logger.Debug($"Reading subscription {distribution.SubscriptionId} for {@event.Publisher}.{@event.EventType}");
            var subscription = await _subscriptionRepository.GetSubscriptionToEventAsync(@event.Publisher, @event.EventType,
                distribution.SubscriptionId, cancellationToken);
            
            _logger.Debug($"Sending event {@event.Id} to subscriber {subscription.Id} at {subscription.EndpointUrl}");
            var request = new RestRequest(subscription.EndpointUrl, Method.POST);
            request.AddParameter(string.Empty, @event.Payload, "application/json", ParameterType.RequestBody);
            var response = await _restClient.ExecuteTaskAsync(request, cancellationToken);
            if (!response.IsSuccessful)
            {
                throw new Exception($"Error sending event {@event.Id} to subscriber {subscription.Id} at {subscription.EndpointUrl} " +
                                    $"- {response.StatusCode} {response.Content}");
            }
            _logger.Info($"Sent event {@event.Id} to subscriber {subscription.Id} at {subscription.EndpointUrl}");
        }
        
    }
}