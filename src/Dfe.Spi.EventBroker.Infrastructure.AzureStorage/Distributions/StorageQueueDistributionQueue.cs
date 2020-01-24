using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.EventBroker.Domain;
using Dfe.Spi.EventBroker.Domain.Configuration;
using Dfe.Spi.EventBroker.Domain.Distributions;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Newtonsoft.Json;

namespace Dfe.Spi.EventBroker.Infrastructure.AzureStorage.Distributions
{
    public class StorageQueueDistributionQueue : IDistributionQueue
    {
        private CloudQueue _queue;

        public StorageQueueDistributionQueue(DistributionConfiguration configuration)
        {
            var storageAccount = CloudStorageAccount.Parse(configuration.StorageConnectionString);
            var queueClient = storageAccount.CreateCloudQueueClient();
            _queue = queueClient.GetQueueReference(QueueNames.DistributionQueueName);
        }
        
        public async Task EnqueueAsync(Distribution distribution, CancellationToken cancellationToken)
        {
            await _queue.CreateIfNotExistsAsync(cancellationToken);
                
            var message = new CloudQueueMessage(JsonConvert.SerializeObject(distribution));
            await _queue.AddMessageAsync(message, cancellationToken);
        }
    }
}