using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Domain.Subscriptions;

namespace Dfe.Spi.EventBroker.Application.Subscriptions
{
    public interface ISubscriptionManager
    {
        Task<Subscription> UpdateSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken);
    }

    public class SubscriptionManager : ISubscriptionManager
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly ILoggerWrapper _logger;

        public SubscriptionManager(
            ISubscriptionRepository subscriptionRepository,
            ILoggerWrapper logger)
        {
            _subscriptionRepository = subscriptionRepository;
            _logger = logger;
        }

        public async Task<Subscription> UpdateSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken)
        {
            // TODO: Should validate publisher and event type exist; but as it will only be internal for now should be ok.
            //       Worst that can happen is a number of subscriptions are added that will never receive events
            
            if (string.IsNullOrEmpty(subscription.Id))
            {
                subscription.Id = Guid.NewGuid().ToString();
                _logger.Debug($"No subscription id provided. Set to {subscription.Id}");
            }

            _logger.Info($"Storing subscription for {subscription.Publisher}.{subscription.EventType} with id {subscription.Id}");
            await _subscriptionRepository.UpdateSubscriptionAsync(subscription, cancellationToken);
            _logger.Info($"Stored subscription {subscription.Id}");

            return subscription;
        }
    }
}