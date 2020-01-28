namespace Dfe.Spi.EventBroker.Domain.Subscriptions
{
    public class Subscription
    {
        public string Id { get; set; }
        public string Publisher { get; set; }
        public string EventType { get; set; }
        public string EndpointUrl { get; set; }
    }
}