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

        public async Task<Distribution> GetAsync(string id, string subscriptionId, CancellationToken cancellationToken)
        {
            await _table.CreateIfNotExistsAsync(cancellationToken);

            var operation = TableOperation.Retrieve<DistributionEntity>(subscriptionId, id);
            var tableResult = await _table.ExecuteAsync(operation, cancellationToken);
            return EntityToModel((DistributionEntity) tableResult.Result);
        }

        public async Task CreateAsync(Distribution distribution, CancellationToken cancellationToken)
        {
            await InsertOrReplaceAsync(distribution, cancellationToken);
        }

        public async Task UpdateAsync(Distribution distribution, CancellationToken cancellationToken)
        {
            await InsertOrReplaceAsync(distribution, cancellationToken);
        }


        private async Task InsertOrReplaceAsync(Distribution distribution, CancellationToken cancellationToken)
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
                Status = (int) distribution.Status,
                Attempts = distribution.Attempts,
            };
        }

        private Distribution EntityToModel(DistributionEntity entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new Distribution
            {
                Id = entity.Id,
                SubscriptionId = entity.SubscriptionId,
                EventId = entity.EventId,
                Status = (DistributionStatus) entity.Status,
                Attempts = entity.Attempts,
            };
        }
    }
}