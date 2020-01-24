using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.EventBroker.Domain.Configuration;
using Dfe.Spi.EventBroker.Domain.Subscriptions;
using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.EventBroker.Infrastructure.AzureStorage.Subscriptions
{
    public class TableSubscriptionRepository : ISubscriptionRepository
    {
        private CloudTable _table;

        public TableSubscriptionRepository(SubscriptionConfiguration configuration)
        {
            var storageAccount = CloudStorageAccount.Parse(configuration.StorageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference(configuration.StorageTableName);
        }
        
        public async Task<Subscription[]> GetSubscriptionsToEventAsync(string publisher, string eventType, CancellationToken cancellationToken)
        {
            var condition = TableQuery.GenerateFilterCondition(
                "PartitionKey",
                QueryComparisons.Equal,
                GetPartitionKey(publisher, eventType));
            var query = new TableQuery<SubscriptionEntity>().Where(condition);
            var continuationToken = default(TableContinuationToken);
            var results = new List<Subscription>();

            await _table.CreateIfNotExistsAsync(cancellationToken);
            
            do
            {
                var entities = await _table.ExecuteQuerySegmentedAsync(query, continuationToken, cancellationToken);

                results.Capacity += entities.Results.Count;
                results.AddRange(entities.Results.Select(EntityToModel));

                continuationToken = entities.ContinuationToken;
            } while (continuationToken != null);

            return results.ToArray();
        }

        private Subscription EntityToModel(SubscriptionEntity entity)
        {
            return new Subscription
            {
                Id = entity.Id,
                EndpointUrl = entity.EndpointUrl,
            };
        }

        private string GetPartitionKey(string publisher, string eventType)
        {
            return $"{publisher}:{eventType}";
        }
    }
}