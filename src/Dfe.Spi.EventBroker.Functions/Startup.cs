using System.IO;
using Dfe.Spi.Common.Logging;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Application.Publishers;
using Dfe.Spi.EventBroker.Application.Receive;
using Dfe.Spi.EventBroker.Application.Send;
using Dfe.Spi.EventBroker.Application.Subscriptions;
using Dfe.Spi.EventBroker.Domain.Configuration;
using Dfe.Spi.EventBroker.Domain.Distributions;
using Dfe.Spi.EventBroker.Domain.Events;
using Dfe.Spi.EventBroker.Domain.Publishers;
using Dfe.Spi.EventBroker.Domain.Subscriptions;
using Dfe.Spi.EventBroker.Functions;
using Dfe.Spi.EventBroker.Infrastructure.AzureStorage.Distributions;
using Dfe.Spi.EventBroker.Infrastructure.AzureStorage.Events;
using Dfe.Spi.EventBroker.Infrastructure.AzureStorage.Publishers;
using Dfe.Spi.EventBroker.Infrastructure.AzureStorage.Subscriptions;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;

[assembly: FunctionsStartup(typeof(Startup))]
namespace Dfe.Spi.EventBroker.Functions
{
    public class Startup : FunctionsStartup
    {
        private IConfigurationRoot _rawConfiguration;
        private EventBrokerConfiguration _configuration;
        
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var rawConfiguration = BuildConfiguration();
            Configure(builder, rawConfiguration);
        }
        
        public void Configure(IFunctionsHostBuilder builder, IConfigurationRoot rawConfiguration)
        {
            var services = builder.Services;
            
            JsonConvert.DefaultSettings =
                () => new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                };

            AddConfiguration(services, rawConfiguration);
            AddLogging(services);
            AddHttp(services);
            AddManagers(services);
            AddPublishers(services);
            AddEvents(services);
            AddDistributions(services);
            AddSubscriptions(services);
        }

        private IConfigurationRoot BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", true)
                .AddEnvironmentVariables(prefix: "SPI_")
                .Build();
        }

        private void AddConfiguration(IServiceCollection services, IConfigurationRoot rawConfiguration)
        {
            _rawConfiguration = rawConfiguration;
            services.AddSingleton(_rawConfiguration);
            
            _configuration = new EventBrokerConfiguration();
            _rawConfiguration.Bind(_configuration);
            services.AddSingleton(_configuration);
            services.AddSingleton(_configuration.Publisher);
            services.AddSingleton(_configuration.Event);
            services.AddSingleton(_configuration.Distribution);
            services.AddSingleton(_configuration.Subscription);
        }

        private void AddLogging(IServiceCollection services)
        {
            services.AddLogging();
            services.AddScoped<ILogger>(provider =>
                provider.GetService<ILoggerFactory>().CreateLogger(LogCategories.CreateFunctionUserCategory("Common")));
            services.AddScoped<ILoggerWrapper, LoggerWrapper>();
        }

        private void AddHttp(IServiceCollection services)
        {
            services.AddTransient<IRestClient, RestClient>();
        }

        private void AddManagers(IServiceCollection services)
        {
            services.AddScoped<IReceiveManager, ReceiveManager>();
            services.AddScoped<ISendManager, SendManager>();
            services.AddScoped<IPublisherManager, PublisherManager>();
            services.AddScoped<ISubscriptionManager, SubscriptionManager>();
        }

        private void AddPublishers(IServiceCollection services)
        {
            services.AddScoped<IPublisherRepository, BlobPublisherRepository>();
        }

        private void AddEvents(IServiceCollection services)
        {
            services.AddScoped<IEventRepository, BlobEventRepository>();
        }

        private void AddDistributions(IServiceCollection services)
        {
            services.AddScoped<IDistributionRepository, TableDistributionRepository>();
            services.AddScoped<IDistributionQueue, StorageQueueDistributionQueue>();
        }

        private void AddSubscriptions(IServiceCollection services)
        {
            services.AddScoped<ISubscriptionRepository, TableSubscriptionRepository>();
        }
    }
}