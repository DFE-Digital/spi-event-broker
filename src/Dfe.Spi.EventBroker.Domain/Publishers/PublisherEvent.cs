using NJsonSchema;

namespace Dfe.Spi.EventBroker.Domain.Publishers
{
    public class PublisherEvent
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public JsonSchema Schema { get; set; }
    }
}