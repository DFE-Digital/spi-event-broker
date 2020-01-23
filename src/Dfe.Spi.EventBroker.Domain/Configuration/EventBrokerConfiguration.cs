namespace Dfe.Spi.EventBroker.Domain.Configuration
{
    public class EventBrokerConfiguration
    {
        public PublisherConfiguration Publisher { get; set; } = new PublisherConfiguration();
    }
}