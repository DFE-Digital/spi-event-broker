using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Application.Receive;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;

namespace Dfe.Spi.EventBroker.Functions.Receive
{
    public class ReceiveEventPublication
    {
        private readonly IReceiveManager _receiveManager;
        private readonly ILoggerWrapper _logger;

        public ReceiveEventPublication(
            IReceiveManager receiveManager,
            ILoggerWrapper logger)
        {
            _receiveManager = receiveManager;
            _logger = logger;
        }

        [FunctionName("ReceiveEventPublication")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "publish/{source}/{eventType}")]
            HttpRequest req,
            string source,
            string eventType,
            CancellationToken cancellationToken)
        {
            _logger.SetContext(req.Headers);
            
            _logger.Info($"Received event publication for {source}.{eventType}");

            string payload;
            using (var reader = new StreamReader(req.Body))
            {
                payload = await reader.ReadToEndAsync();
            }

            _logger.Info($"Read payload {payload}");

            try
            {
                await _receiveManager.ReceiveAsync(source, eventType, payload, cancellationToken);
                return new AcceptedResult();
            }
            catch (InvalidRequestException ex)
            {
                _logger.Info($"Returning bad request ({ex.Code}) - {ex.Message}");
                return new HttpErrorBodyResult(HttpStatusCode.BadRequest, $"SPI-EVBK-{ex.Code}", ex.Message);
            }
        }
    }
}