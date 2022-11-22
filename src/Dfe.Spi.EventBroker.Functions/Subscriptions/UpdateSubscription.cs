using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.Models;
using Dfe.Spi.EventBroker.Application.Subscriptions;
using Dfe.Spi.EventBroker.Domain.Subscriptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Dfe.Spi.Common.Http.Server.Definitions;

namespace Dfe.Spi.EventBroker.Functions.Subscriptions
{
    public class UpdateSubscription : FunctionsBase<UpdateSubscriptionRequest>
    {
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly ILoggerWrapper _logger;
        private readonly IHttpSpiExecutionContextManager _httpSpiExecutionContextManager;


        public UpdateSubscription(
            ISubscriptionManager subscriptionManager,
            ILoggerWrapper logger,
            IHttpSpiExecutionContextManager httpSpiExecutionContextManager)
            : base(httpSpiExecutionContextManager, logger)
        {
            _subscriptionManager = subscriptionManager;
            _logger = logger;
            _httpSpiExecutionContextManager = httpSpiExecutionContextManager;
        }

        [FunctionName("UpdateSubscription")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "subscriptions")]
            HttpRequest req,
            CancellationToken cancellationToken)
        {
            _httpSpiExecutionContextManager.SetContext(req.Headers);

            return await ValidateAndRunAsync(req, null, cancellationToken);
        }

        protected override HttpErrorBodyResult GetMalformedErrorResponse(FunctionRunContext runContext)
        {
            return new HttpErrorBodyResult(
                HttpStatusCode.BadRequest,
                "SPI-EVBK-NOTJSON",
                "The supplied body was either empty, or not well-formed JSON.");
        }

        protected override HttpErrorBodyResult GetSchemaValidationResponse(JsonSchemaValidationException validationException, FunctionRunContext runContext)
        {
            return new HttpErrorBodyResult(
                HttpStatusCode.BadRequest,
                "SPI-EVBK-INVALIDREQUEST",
                $"The supplied body was well-formed JSON but it failed validation: {validationException.Message}");
        }


        protected override async Task<IActionResult> ProcessWellFormedRequestAsync(UpdateSubscriptionRequest request, FunctionRunContext runContext,
            CancellationToken cancellationToken)
        {
            var subscription = new Subscription
            {
                Id = request.SubscriptionId,
                Publisher = request.Publisher,
                EventType = request.EventType,
                EndpointUrl = request.EndpointUrl,
            };
            await _subscriptionManager.UpdateSubscriptionAsync(subscription, cancellationToken);
            
            return new AcceptedResult();
        }

    }

    public class UpdateSubscriptionRequest : RequestResponseBase
    {
        public string SubscriptionId { get; set; }
        public string Publisher { get; set; }
        public string EventType { get; set; }
        public string EndpointUrl { get; set; }
    }
}