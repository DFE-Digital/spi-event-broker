using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Domain.Configuration;
using Dfe.Spi.EventBroker.Domain.Events;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;

namespace Dfe.Spi.EventBroker.Infrastructure.AzureStorage.Events
{
    public class BlobEventRepository : IEventRepository
    {
        private readonly EventConfiguration _configuration;
        private readonly ILoggerWrapper _logger;
        private readonly CloudBlobContainer _container;

        public BlobEventRepository(EventConfiguration configuration, ILoggerWrapper logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            var storageAccount = CloudStorageAccount.Parse(configuration.StorageConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            _container = blobClient.GetContainerReference(configuration.StorageContainerName);
        }
        
        public async Task StoreAsync(Event @event, CancellationToken cancellationToken)
        {
            var blob = await GetBlobReferenceAsync($"{@event.Id}.json", false, cancellationToken);
            await blob.UploadTextAsync(JsonConvert.SerializeObject(@event), cancellationToken);
        }


        private async Task<CloudBlockBlob> GetBlobReferenceAsync(string fileName, bool checkExists, CancellationToken cancellationToken)
        {
            CloudBlockBlob blob;

            if (!string.IsNullOrEmpty(_configuration.StorageFolderName))
            {
                var folder = _container.GetDirectoryReference(_configuration.StorageFolderName);
                blob = folder.GetBlockBlobReference(fileName);
                if (checkExists && !await blob.ExistsAsync(cancellationToken))
                {
                    _logger.Debug($"Cannot find blob {fileName} in folder {_configuration.StorageFolderName}");
                    return null;
                }
            }
            else
            {
                blob = _container.GetBlockBlobReference(fileName);
                if (checkExists && !await blob.ExistsAsync(cancellationToken))
                {
                    _logger.Debug($"Cannot find blob {fileName}");
                    return null;
                }
            }

            return blob;
        }
    }
}