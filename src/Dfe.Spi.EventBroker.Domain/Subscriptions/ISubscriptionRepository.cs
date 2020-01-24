using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.EventBroker.Domain.Subscriptions
{
    public interface ISubscriptionRepository
    {
        Task<Subscription[]> GetSubscriptionsToEventAsync(string publisher, string eventType,
            CancellationToken cancellationToken);
    }
}