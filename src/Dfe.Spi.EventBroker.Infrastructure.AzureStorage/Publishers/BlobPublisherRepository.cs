using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Domain.Configuration;
using Dfe.Spi.EventBroker.Domain.Publishers;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
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
            var blobContent = await GetBlobContentAsStringAsync(code, cancellationToken);
            if (string.IsNullOrEmpty(blobContent))
            {
                return null;
            }

            var json = JObject.Parse(blobContent);
            var defs = (JObject) json["definitions"];

            var events = new List<PublisherEvent>();
            foreach (var property in ((JObject) json["events"]).Properties())
            {
                var @event = (JObject) property.Value;
                var schemaJson = ((JObject) @event["schema"]).DeepClone();
                if (defs != null)
                {
                    var schemaDefs = new JProperty("definitions", defs.DeepClone());
                    schemaJson.Children().Last().AddAfterSelf(schemaDefs);
                }

                events.Add(new PublisherEvent
                {
                    Name = property.Name,
                    Description = (string) @event["description"],
                    Schema = await JsonSchema.FromJsonAsync(schemaJson.ToString())
                });
            }

            var info = (JObject) json["info"];
            return new Publisher
            {
                Code = (string) info["code"],
                Name = (string) info["name"],
                Description = (string) info["description"],
                Version = (string) info["version"],
                Events = events.ToArray(),
            };
        }

        private async Task<string> GetBlobContentAsStringAsync(string code, CancellationToken cancellationToken)
        {
            CloudBlockBlob blob;
            var fileName = $"{code}.json";

            if (!string.IsNullOrEmpty(_configuration.StorageFolderName))
            {
                var folder = _container.GetDirectoryReference(_configuration.StorageFolderName);
                blob = folder.GetBlockBlobReference(fileName);
                if (!await blob.ExistsAsync(cancellationToken))
                {
                    _logger.Debug($"Cannot find blob {fileName} in folder {_configuration.StorageFolderName}");
                    return null;
                }
            }
            else
            {
                blob = _container.GetBlockBlobReference(fileName);
                if (!await blob.ExistsAsync(cancellationToken))
                {
                    _logger.Debug($"Cannot find blob {fileName}");
                    return null;
                }
            }

            using (var stream = new MemoryStream())
            {
                await blob.DownloadToStreamAsync(stream, cancellationToken);

                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }
    }
}