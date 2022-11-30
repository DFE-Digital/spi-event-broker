using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Application.Publishers;
using Dfe.Spi.EventBroker.Domain.Publishers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Dfe.Spi.EventBroker.Functions.Publishers
{
    public class UpdatePublishedEvents
    {
        private readonly IPublisherManager _publisherManager;
        private readonly ILoggerWrapper _logger;
        private readonly IHttpSpiExecutionContextManager _httpSpiExecutionContextManager;

        public UpdatePublishedEvents(
            IPublisherManager publisherManager,
            ILoggerWrapper logger,
            IHttpSpiExecutionContextManager httpSpiExecutionContextManager)
        {
            _publisherManager = publisherManager;
            _logger = logger;
            _httpSpiExecutionContextManager = httpSpiExecutionContextManager;
        }

        [FunctionName("UpdatePublishedEvents")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "events")]
            HttpRequest req,
            CancellationToken cancellationToken)
        {
            _httpSpiExecutionContextManager.SetContext(req.Headers);

            var rootObject = await ReadAndValidateRequestAsync(req);

            var publisher = ConvertJsonToPublisher(rootObject);

            await _publisherManager.UpdatePublishedEventsAsync(publisher, cancellationToken);
            
            return new AcceptedResult();
        }

        
        private async Task<JObject> ReadAndValidateRequestAsync(HttpRequest request)
        {
            var schema = await GetRequestSchemaAsync();
            var body = await new StreamReader(request.Body).ReadToEndAsync();

            var validationErrors = schema.Validate(body);
            if (validationErrors.Count > 0)
            {
                throw new JsonSchemaValidationException(validationErrors);
            }

            return JObject.Parse(body);
        }

        private async Task<JsonSchema> GetRequestSchemaAsync()
        {
            var assembly = GetType().Assembly;
            var resourcePath = assembly.GetManifestResourceNames()
                .Single(x => x.EndsWith("update-published-events-body.json"));
            using (var stream = assembly.GetManifestResourceStream(resourcePath))
            using (var reader = new StreamReader(stream))
            {
                var json = await reader.ReadToEndAsync();
                return await JsonSchema.FromJsonAsync(json);
            }
        }

        private Publisher ConvertJsonToPublisher(JObject rootObject)
        {
            var defs = (JObject) rootObject["definitions"];
            
            var events = new List<PublisherEvent>();
            foreach (var property in ((JObject) rootObject["events"]).Properties())
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
                    Schema = schemaJson.ToString(Formatting.None)
                });
            }
            
            var info = (JObject) rootObject["info"];
            return new Publisher
            {
                Code = (string) info["code"],
                Name = (string) info["name"],
                Description = (string) info["description"],
                Version = (string) info["version"],
                Events = events.ToArray(),
            };
        }
    }
}