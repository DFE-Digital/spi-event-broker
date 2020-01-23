using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.EventBroker.Domain.Publishers
{
    public interface IPublisherRepository
    {
        Task<Publisher> GetPublisherAsync(string code, CancellationToken cancellationToken);
    }
}