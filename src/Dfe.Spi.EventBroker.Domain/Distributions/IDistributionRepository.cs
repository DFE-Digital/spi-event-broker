using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.EventBroker.Domain.Distributions
{
    public interface IDistributionRepository
    {
        Task<Distribution> GetAsync(string id, string subscriptionId, CancellationToken cancellationToken);
        Task CreateAsync(Distribution distribution, CancellationToken cancellationToken);
        Task UpdateAsync(Distribution distribution, CancellationToken cancellationToken);
    }
}