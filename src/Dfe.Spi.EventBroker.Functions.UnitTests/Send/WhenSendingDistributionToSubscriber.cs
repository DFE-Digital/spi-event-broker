using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Http.Server;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Application.Send;
using Dfe.Spi.EventBroker.Domain.Distributions;
using Dfe.Spi.EventBroker.Functions.Send;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Dfe.Spi.EventBroker.Functions.UnitTests.Send
{
    public class WhenSendingDistributionToSubscriber
    {
        private Mock<ISendManager> _sendManagerMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private Mock<IHttpSpiExecutionContextManager> _httpSpiExecutionContextManagerMock;
        private SendDistributionToSubscriber _function;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _sendManagerMock = new Mock<ISendManager>();
            
            _loggerMock = new Mock<ILoggerWrapper>();
            _httpSpiExecutionContextManagerMock = new Mock<IHttpSpiExecutionContextManager>();
            _function = new SendDistributionToSubscriber(
                _sendManagerMock.Object,
                _loggerMock.Object,
                _httpSpiExecutionContextManagerMock.Object);
            
            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldSetupLogger(Distribution distribution)
        {
            await _function.RunAsync(JsonConvert.SerializeObject(distribution), _cancellationToken);

            _httpSpiExecutionContextManagerMock.Verify(l=>l.SetInternalRequestId(It.IsAny<Guid>()),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShoudSendDistributionWithSendManager(Distribution distribution)
        {
            await _function.RunAsync(JsonConvert.SerializeObject(distribution), _cancellationToken);
            
            _sendManagerMock.Verify(m=>m.SendAsync( It.Is<Distribution>(d=>
                    d.Id == distribution.Id &&
                    d.EventId == distribution.EventId &&
                    d.SubscriptionId == distribution.SubscriptionId), _cancellationToken),
                Times.Once);
        }
    }
}