namespace Dfe.Spi.EventBroker.Domain.Configuration
{
    public class EventConfiguration
    {
        public string StorageConnectionString { get; set; }
        public string StorageContainerName { get; set; }
        public string StorageFolderName { get; set; }
    }
}