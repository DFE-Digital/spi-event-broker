namespace Dfe.Spi.EventBroker.Domain.Distributions
{
    public enum DistributionStatus
    {
        Pending = 1,
        PendingRetry = 2,
        Sent = 3,
        Failed = 4,
    }
}