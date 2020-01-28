namespace Dfe.Spi.EventBroker.Domain.Distributions
{
    public class Distribution
    {
        public string Id { get; set; }
        public string SubscriptionId { get; set; }
        public string EventId { get; set; }
        public DistributionStatus Status { get; set; }
        public int Attempts { get; set; }
    }
}