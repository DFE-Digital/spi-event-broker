using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.EventBroker.Infrastructure.AzureStorage.Subscriptions
{
    internal class SubscriptionEntity : TableEntity
    {
        public string Id { get; set; }
        public string EndpointUrl { get; set; }
    }
}