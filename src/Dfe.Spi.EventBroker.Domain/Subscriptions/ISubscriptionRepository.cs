using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.EventBroker.Domain.Subscriptions
{
    public interface ISubscriptionRepository
    {
        Task<Subscription[]> GetSubscriptionsToEventAsync(string publisher, string eventType,
            CancellationToken cancellationToken);
        Task<Subscription> GetSubscriptionToEventAsync(string publisher, string eventType, string subscriptionId,
            CancellationToken cancellationToken);
        Task UpdateSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken);
    }
}