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

        public async Task<Subscription> GetSubscriptionToEventAsync(string publisher, string eventType, string subscriptionId,
            CancellationToken cancellationToken)
        {
            var operation = TableOperation.Retrieve<SubscriptionEntity>(
                GetPartitionKey(publisher, eventType),
                subscriptionId);
            var result = await _table.ExecuteAsync(operation, cancellationToken);
            
            return EntityToModel((SubscriptionEntity) result.Result);
        }

        public async Task UpdateSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken)
        {
            await _table.CreateIfNotExistsAsync(cancellationToken);
            
            var operation = TableOperation.InsertOrReplace(ModelToEntity(subscription));
            await _table.ExecuteAsync(operation, cancellationToken);
        }


        private Subscription EntityToModel(SubscriptionEntity entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new Subscription
            {
                Id = entity.Id,
                Publisher = entity.Publisher,
                EventType = entity.EventType,
                EndpointUrl = entity.EndpointUrl,
            };
        }

        private SubscriptionEntity ModelToEntity(Subscription model)
        {
            return new SubscriptionEntity
            {
                PartitionKey = GetPartitionKey(model.Publisher, model.EventType),
                RowKey = model.Id,
                Id = model.Id,
                Publisher = model.Publisher,
                EventType = model.EventType,
                EndpointUrl = model.EndpointUrl,
            };
        }

        private string GetPartitionKey(string publisher, string eventType)
        {
            return $"{publisher}:{eventType}";
        }
    }
}