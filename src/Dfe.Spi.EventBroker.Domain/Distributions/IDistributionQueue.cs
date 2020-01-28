using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.EventBroker.Domain.Distributions
{
    public interface IDistributionQueue
    {
        Task EnqueueAsync(Distribution distribution, CancellationToken cancellationToken);
    }
}