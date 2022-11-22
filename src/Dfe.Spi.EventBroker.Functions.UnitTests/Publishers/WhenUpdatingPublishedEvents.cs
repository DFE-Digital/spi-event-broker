using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Application.Publishers;
using Dfe.Spi.EventBroker.Domain.Publishers;
using Dfe.Spi.EventBroker.Functions.Publishers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Dfe.Spi.EventBroker.Functions.UnitTests.Publishers
{
    public class WhenUpdatingPublishedEvents
    {
        private Mock<IPublisherManager> _publisherManagerMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private Mock<IHttpSpiExecutionContextManager> _httpSpiExecutionContextManagerMock;
        private UpdatePublishedEvents _function;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _publisherManagerMock = new Mock<IPublisherManager>();

            _loggerMock = new Mock<ILoggerWrapper>();
            _httpSpiExecutionContextManagerMock = new Mock<IHttpSpiExecutionContextManager>();

            _function = new UpdatePublishedEvents(
                _publisherManagerMock.Object,
                _loggerMock.Object,
                _httpSpiExecutionContextManagerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test]
        public async Task ThenItShouldCallManager()
        {
            var requestDocument = BuildRequestDocument();
            var request = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(requestDocument)),
            };

            await _function.RunAsync(request, _cancellationToken);

            _publisherManagerMock.Verify(m => m.UpdatePublishedEventsAsync(
                It.IsAny<Publisher>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldDeserializePublisherInfo(string code, string name, string description,
            string version)
        {
            var requestDocument = BuildRequestDocument(code, name, description, version);
            var request = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(requestDocument)),
            };

            await _function.RunAsync(request, _cancellationToken);

            _publisherManagerMock.Verify(m => m.UpdatePublishedEventsAsync(
                It.Is<Publisher>(p =>
                    p.Code == code &&
                    p.Name == name &&
                    p.Description == description &&
                    p.Version == version), _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldDeserializeEventInfo(string eventName, string eventDescription)
        {
            var requestDocument = BuildRequestDocument(eventName: eventName, eventDescription: eventDescription, useCommonEventSchema: false);
            var request = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(requestDocument)),
            };

            await _function.RunAsync(request, _cancellationToken);

            var expectedSchema = "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}},\"definitions\":{}}";
            _publisherManagerMock.Verify(m => m.UpdatePublishedEventsAsync(
                It.Is<Publisher>(p =>
                    p.Events.Length == 1 &&
                    p.Events[0].Name == eventName &&
                    p.Events[0].Description == eventDescription &&
                    p.Events[0].Schema == expectedSchema), _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldDeserializeCommonSchema(string eventName, string eventDescription)
        {
            var requestDocument = BuildRequestDocument(eventName: eventName, eventDescription: eventDescription, useCommonEventSchema: true);
            var request = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(requestDocument)),
            };

            await _function.RunAsync(request, _cancellationToken);

            var expectedSchema = "{\"$ref\":\"#/definitions/test-object\",\"definitions\":{\"test-object\":{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}}}";
            _publisherManagerMock.Verify(m => m.UpdatePublishedEventsAsync(
                It.Is<Publisher>(p =>
                    p.Events.Length == 1 &&
                    p.Events[0].Name == eventName &&
                    p.Events[0].Description == eventDescription &&
                    p.Events[0].Schema == expectedSchema), _cancellationToken),
                Times.Once);
        }

        [Test]
        public async Task ThenItShouldReturnAccepted()
        {
            var requestDocument = BuildRequestDocument();
            var request = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(requestDocument)),
            };

            var actual = await _function.RunAsync(request, _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<AcceptedResult>(actual);
        }


        private string BuildRequestDocument(
            string code = "code1", string name = "name1", string description = "description1", string version = "1.2.3",
            string eventName = "event1", string eventDescription = "eventdescription1", bool useCommonEventSchema = false)
        {
            var eventSchema = new JObject(
                new JProperty("type", "object"),
                new JProperty("properties",
                    new JObject(
                        new JProperty("name",
                            new JObject(
                                new JProperty("type", "string"))))));
            
            var eventDef = new JObject(
                new JProperty("description", eventDescription));
            if (!useCommonEventSchema)
            {
                eventDef.Add(new JProperty("schema", eventSchema));
            }
            else
            {
                eventDef.Add(new JProperty("schema", 
                    new JObject(
                        new JProperty("$ref", $"#/definitions/test-object"))));
            }
            
            var infoObject = new JObject(
                new JProperty("code", code),
                new JProperty("name", name),
                new JProperty("description", description),
                new JProperty("version", version));
            var eventsObject = new JObject(
                new JProperty(eventName, eventDef));
            var definitionsObject = new JObject();

            if (useCommonEventSchema)
            {
                definitionsObject.Add(new JProperty("test-object", eventSchema));
            }

            var document = new JObject(
                new JProperty("info", infoObject),
                new JProperty("events", eventsObject),
                new JProperty("definitions", definitionsObject));
            return document.ToString();
        }
    }
}