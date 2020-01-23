namespace Dfe.Spi.EventBroker.Domain.Configuration
{
    public class PublisherConfiguration
    {
        public string StorageConnectionString { get; set; }
        public string StorageContainerName { get; set; }
        public string StorageFolderName { get; set; }
    }
}