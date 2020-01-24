using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.EventBroker.Infrastructure.AzureStorage.Distributions
{
    internal class DistributionEntity : TableEntity
    {
        public string Id { get; set; }
        public string SubscriptionId { get; set; }
        public string EventId { get; set; }
    }
}