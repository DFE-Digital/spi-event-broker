using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Http.Server;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Application.Receive;
using Dfe.Spi.EventBroker.Functions.Receive;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.EventBroker.Functions.UnitTests.Receive
{
    public class ReceivingEventPublication
    {
        private Mock<IReceiveManager> _receiveManagerMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private Mock<IHttpSpiExecutionContextManager> _httpSpiExecutionContextManagerMock;
        private ReceiveEventPublication _function;
        private CancellationToken _cancellationToken;
        private DefaultHttpRequest _defaultRequest;

        [SetUp]
        public void Arrange()
        {
            _receiveManagerMock = new Mock<IReceiveManager>();
            _httpSpiExecutionContextManagerMock = new Mock<IHttpSpiExecutionContextManager>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _function = new ReceiveEventPublication(
                _receiveManagerMock.Object,
                _loggerMock.Object,
                _httpSpiExecutionContextManagerMock.Object);


            _defaultRequest = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"))
            };
            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldCallReceiveManager(string source, string eventType, string payload)
        {
            var request = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(payload))
            };

            await _function.RunAsync(request, source, eventType, _cancellationToken);

            _receiveManagerMock.Verify(m => m.ReceiveAsync(source, eventType, payload, _cancellationToken),
                Times.Once);
        }

        [Test]
        public async Task ThenItShouldReturnAccepted()
        {
            var actual = await _function.RunAsync(_defaultRequest, "", "", _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<AcceptedResult>(actual);
        }

        [Test]
        public async Task ThenItShouldReturnBadRequestIfInvalidRequestExceptionThrown()
        {
            _receiveManagerMock.Setup(m =>
                    m.ReceiveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidRequestException("TEST", "unit test"));

            var actual = await _function.RunAsync(_defaultRequest, "", "", _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<HttpErrorBodyResult>(actual);
            Assert.AreEqual(400, (int)((HttpErrorBodyResult) actual).StatusCode);
        }
    }
}