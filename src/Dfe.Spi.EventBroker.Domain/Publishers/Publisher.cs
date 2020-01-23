namespace Dfe.Spi.EventBroker.Domain.Publishers
{
    public class Publisher
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public PublisherEvent[] Events { get; set; }
    }
}