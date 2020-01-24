using System;
using System.Collections.Generic;
using Dfe.Spi.EventBroker.Functions.Receive;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dfe.Spi.EventBroker.Functions.UnitTests
{
    public class WhenConfiguringFunctionApp
    {
        [Test]
        public void ThenAllFunctionsShouldBeResolvable()
        {
            var functions = GetFunctions();
            var builder = new TestFunctionHostBuilder();
            var configuration = GetTestConfiguration();
            
            var startup = new Startup();
            startup.Configure(builder, configuration);
            // Have to register the function so container can attempt to resolve them
            foreach (var function in functions)
            {
                builder.Services.AddScoped(function);
            }
            var provider = builder.Services.BuildServiceProvider();
            
            foreach (var function in functions)
            {
                Assert.IsNotNull(provider.GetService(function),
                    $"Failed to resolve {function.Name}");
            }
        }

        private IConfigurationRoot GetTestConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Publisher:StorageConnectionString", "UseDevelopmentStorage=true"),
                    new KeyValuePair<string, string>("Publisher:StorageContainerName", "TestPublisher"),
                    new KeyValuePair<string, string>("Event:StorageConnectionString", "UseDevelopmentStorage=true"),
                    new KeyValuePair<string, string>("Event:StorageContainerName", "TestPublisher"),
                    new KeyValuePair<string, string>("Distribution:StorageConnectionString", "UseDevelopmentStorage=true"),
                    new KeyValuePair<string, string>("Distribution:StorageTableName", "TestDistribution"),
                    new KeyValuePair<string, string>("Subscription:StorageConnectionString", "UseDevelopmentStorage=true"),
                    new KeyValuePair<string, string>("Subscription:StorageTableName", "TestDistribution"),
                }).Build();
        }

        private Type[] GetFunctions()
        {
            return new[]
            {
                typeof(ReceiveEventPublication),
            };
        }
        
        private class TestFunctionHostBuilder : IFunctionsHostBuilder
        {
            public TestFunctionHostBuilder()
            {
                Services = new ServiceCollection();
            }
            public IServiceCollection Services { get; }
        }
    }
}