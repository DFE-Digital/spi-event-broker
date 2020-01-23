using System.IO;
using Dfe.Spi.Common.Logging;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.EventBroker.Application.Receive;
using Dfe.Spi.EventBroker.Domain.Configuration;
using Dfe.Spi.EventBroker.Domain.Events;
using Dfe.Spi.EventBroker.Domain.Publishers;
using Dfe.Spi.EventBroker.Functions;
using Dfe.Spi.EventBroker.Infrastructure.AzureStorage.Events;
using Dfe.Spi.EventBroker.Infrastructure.AzureStorage.Publishers;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

            AddConfiguration(services, rawConfiguration);
            AddLogging(services);
            AddManagers(services);
            AddPublishers(services);
            AddEvents(services);
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
        }

        private void AddLogging(IServiceCollection services)
        {
            services.AddLogging();
            services.AddScoped<ILogger>(provider =>
                provider.GetService<ILoggerFactory>().CreateLogger(LogCategories.CreateFunctionUserCategory("Common")));
            services.AddScoped<ILoggerWrapper, LoggerWrapper>();
        }

        private void AddManagers(IServiceCollection services)
        {
            services.AddScoped<IReceiveManager, ReceiveManager>();
        }

        private void AddPublishers(IServiceCollection services)
        {
            services.AddScoped<IPublisherRepository, BlobPublisherRepository>();
        }

        private void AddEvents(IServiceCollection services)
        {
            services.AddScoped<IEventRepository, BlobEventRepository>();
        }
    }
}