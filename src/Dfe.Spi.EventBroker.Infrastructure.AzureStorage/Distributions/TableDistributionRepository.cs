using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.EventBroker.Domain.Configuration;
using Dfe.Spi.EventBroker.Domain.Distributions;
using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.EventBroker.Infrastructure.AzureStorage.Distributions
{
    public class TableDistributionRepository : IDistributionRepository
    {
        private CloudTable _table;

        public TableDistributionRepository(DistributionConfiguration configuration)
        {
            var storageAccount = CloudStorageAccount.Parse(configuration.StorageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference(configuration.StorageTableName);
        }
        
        public async Task CreateAsync(Distribution distribution, CancellationToken cancellationToken)
        {
            await _table.CreateIfNotExistsAsync(cancellationToken);

            var operation = TableOperation.InsertOrReplace(ModelToEntity(distribution));
            await _table.ExecuteAsync(operation, cancellationToken);
        }

        private DistributionEntity ModelToEntity(Distribution distribution)
        {
            return new DistributionEntity
            {
                PartitionKey = distribution.SubscriptionId,
                RowKey = distribution.Id,
                Id = distribution.Id,
                SubscriptionId = distribution.SubscriptionId,
                EventId = distribution.EventId,
            };
        }
    }
}