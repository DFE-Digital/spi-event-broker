using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dfe.Spi.EventBroker.Functions.UnitTests
{
    internal static class TestHelpers
    {
        internal static HttpRequest BuildRequestWithBody(object body)
        {
            var json = JsonConvert.SerializeObject(body,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore,
                });
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(json))
            };
        }
    }
}