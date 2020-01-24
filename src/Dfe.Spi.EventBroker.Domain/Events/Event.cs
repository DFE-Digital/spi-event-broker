namespace Dfe.Spi.EventBroker.Domain.Events
{
    public class Event
    {
        public string Id { get; set; }
        public string Publisher { get; set; }
        public string EventType { get; set; }
        public string Payload { get; set; }
    }
}