using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.EventBroker.Domain.Distributions
{
    public interface IDistributionRepository
    {
        Task CreateAsync(Distribution distribution, CancellationToken cancellationToken);
    }
}