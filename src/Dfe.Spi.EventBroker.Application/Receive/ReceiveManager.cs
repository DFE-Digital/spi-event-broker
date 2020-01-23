using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.EventBroker.Application.Receive
{
    public interface IReceiveManager
    {
        Task ReceiveAsync(string source, string eventType, string payload, CancellationToken cancellationToken);
    }

    public class ReceiveManager : IReceiveManager
    {
        public async Task ReceiveAsync(string source, string eventType, string payload, CancellationToken cancellationToken)
        {
            await ValidateRequestAsync(source, eventType, payload, cancellationToken);
            
            // TODO: Store publication
            
            // TODO: Queue distributions to subscribers
        }

        private Task ValidateRequestAsync(string source, string eventType, string payload,
            CancellationToken cancellationToken)
        {
            // TODO: Check payload valid JSON
            
            // TODO: Check valid system
            
            // TODO: Check valid event type
            
            // TODO: Check payload matches event schema
            
            return Task.CompletedTask;
        }
    }
}