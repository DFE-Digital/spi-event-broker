using System.Threading.Tasks;
using System;
using System.Threading;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Application.Send;
using Dfe.Spi.EventBroker.Domain;
using Dfe.Spi.EventBroker.Domain.Distributions;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Http.Server;

namespace Dfe.Spi.EventBroker.Functions.Send
{
    public class SendDistributionToSubscriber
    {
        private readonly ISendManager _sendManager;
        private readonly ILoggerWrapper _logger;
        private readonly IHttpSpiExecutionContextManager _httpSpiExecutionContextManager;


        public SendDistributionToSubscriber(
            ISendManager sendManager,
            ILoggerWrapper logger,
            IHttpSpiExecutionContextManager httpSpiExecutionContextManager)
        {
            _sendManager = sendManager;
            _logger = logger;
            _httpSpiExecutionContextManager = httpSpiExecutionContextManager;
        }

        [StorageAccount("SPI_Distribution:StorageConnectionString")]
        [FunctionName("SendDistributionToSubscriber")]
        public async Task RunAsync([QueueTrigger(QueueNames.DistributionQueueName)]
            string queueItemContents,
            CancellationToken cancellationToken)
        {
            _httpSpiExecutionContextManager.SetInternalRequestId(Guid.NewGuid());
            _logger.Info($"Starting to process queue item {queueItemContents}");
            
            var distribution = JsonConvert.DeserializeObject<Distribution>(queueItemContents);

            await _sendManager.SendAsync(distribution, cancellationToken);
        }
    }
}