using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.EventBroker.Domain.Events
{
    public interface IEventRepository
    {
        Task StoreAsync(Event @event, CancellationToken cancellationToken);
        Task<Event> GetAsync(string id, CancellationToken cancellationToken);
    }
}