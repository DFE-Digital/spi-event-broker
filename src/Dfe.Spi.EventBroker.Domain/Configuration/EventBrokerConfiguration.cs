namespace Dfe.Spi.EventBroker.Domain.Configuration
{
    public class EventBrokerConfiguration
    {
        public PublisherConfiguration Publisher { get; set; } = new PublisherConfiguration();
        public EventConfiguration Event { get; set; } = new EventConfiguration();
        public DistributionConfiguration Distribution { get; set; } = new DistributionConfiguration();
        public SubscriptionConfiguration Subscription { get; set; } = new SubscriptionConfiguration();
        public AuthenticationConfiguration Authentication { get; set; } = new AuthenticationConfiguration();
    }
}