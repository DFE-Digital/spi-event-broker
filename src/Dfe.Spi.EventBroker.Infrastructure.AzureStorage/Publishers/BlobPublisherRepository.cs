using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Domain.Configuration;
using Dfe.Spi.EventBroker.Domain.Publishers;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Dfe.Spi.EventBroker.Infrastructure.AzureStorage.Publishers
{
    public class BlobPublisherRepository : IPublisherRepository
    {
        private readonly PublisherConfiguration _configuration;
        private readonly ILoggerWrapper _logger;
        private readonly CloudBlobContainer _container;

        public BlobPublisherRepository(PublisherConfiguration configuration, ILoggerWrapper logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            var storageAccount = CloudStorageAccount.Parse(configuration.StorageConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            _container = blobClient.GetContainerReference(configuration.StorageContainerName);
        }

        public async Task<Publisher> GetPublisherAsync(string code, CancellationToken cancellationToken)
        {
            var fileName = $"{code}.json";
            var blob = await GetBlobReferenceAsync(fileName, true, cancellationToken);

            using (var stream = new MemoryStream())
            {
                await blob.DownloadToStreamAsync(stream, cancellationToken);

                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    var jsonString = await reader.ReadToEndAsync();
                    return JsonConvert.DeserializeObject<Publisher>(jsonString);
                }
            }
        }

        public async Task UpdatePublisherAsync(Publisher publisher, CancellationToken cancellationToken)
        {
            var fileName = $"{publisher.Code}.json";
            var blob = await GetBlobReferenceAsync(fileName, false, cancellationToken);

            var content = JsonConvert.SerializeObject(publisher);
            await blob.UploadTextAsync(content, cancellationToken);
        }

        private async Task<CloudBlockBlob> GetBlobReferenceAsync(string fileName, bool checkBlobExists, CancellationToken cancellationToken)
        {
            CloudBlockBlob blob;

            await _container.CreateIfNotExistsAsync(cancellationToken);

            if (!string.IsNullOrEmpty(_configuration.StorageFolderName))
            {
                var folder = _container.GetDirectoryReference(_configuration.StorageFolderName);
                blob = folder.GetBlockBlobReference(fileName);
                if (checkBlobExists && !await blob.ExistsAsync(cancellationToken))
                {
                    _logger.Debug($"Cannot find blob {fileName} in folder {_configuration.StorageFolderName}");
                    return null;
                }
            }
            else
            {
                blob = _container.GetBlockBlobReference(fileName);
                if (checkBlobExists && !await blob.ExistsAsync(cancellationToken))
                {
                    _logger.Debug($"Cannot find blob {fileName}");
                    return null;
                }
            }

            return blob;
        }
    }
}