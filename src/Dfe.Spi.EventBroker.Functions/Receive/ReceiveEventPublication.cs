using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Dfe.Spi.EventBroker.Functions.Receive
{
    public class ReceiveEventPublication
    {
        [FunctionName("ReceiveEventPublication")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "publish/{source}/{eventType}")]
            HttpRequest req,
            string source,
            string eventType)
        {
            return new OkObjectResult(new
            {
                source,
                eventType
            });
        }
    }
}