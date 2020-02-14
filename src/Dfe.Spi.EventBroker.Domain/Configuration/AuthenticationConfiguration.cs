namespace Dfe.Spi.EventBroker.Domain.Configuration
{
    public class AuthenticationConfiguration
    {
        public string TokenEndpoint { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Resource { get; set; }
        public string SubscriptionKey { get; set; }
    }
}