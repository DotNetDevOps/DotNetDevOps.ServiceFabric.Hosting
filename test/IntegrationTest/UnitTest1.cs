using DotNetDevOps.ServiceFabric.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
namespace DotNetDevOps.ServiceFabric.Hosting.IntegrationTest
{
    public interface IMyService : IService
    {

    }
    public class ConfigurationExtenderExamplerDependency
    {
        public string Value { get; set; }= "test2";
    }
    public class ConfigurationExtenderExampler : IConfigurationBuilderExtension
    {
        private readonly ConfigurationExtenderExamplerDependency dependency;

        public ConfigurationExtenderExampler(ConfigurationExtenderExamplerDependency dependency)
        {
            this.dependency = dependency;
        }
        public IConfigurationBuilder Extend(IConfigurationBuilder cbuilder)
        {
            return cbuilder.AddInMemoryCollection(new[] { new KeyValuePair<string, string>("dependency",this.dependency.Value) });
        }
    }
    
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var host = new FabricHostBuilder()
                .WithServiceProxy< IMyService>("aa","a"); 


        }

        [Fact]
        public void TestUseConfiguration()
        {
            var hostBuilder = new FabricHostBuilder()
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.AddInMemoryCollection(new[] { new KeyValuePair<string, string>("test", "test") });
                }).ConfigureServices((context,services)=>
                {
                    services.AddSingleton<ConfigurationExtenderExamplerDependency>();
                    services.AddSingleton<IConfigurationBuilderExtension, ConfigurationExtenderExampler>();
                });
            

            var host = hostBuilder.Build();
            {
                using (var outerScope = host.Services.GetService<IServiceScopeFactory>().CreateScope())
                {
                    var configuration = outerScope.ServiceProvider.GetService<IConfiguration>();
                    var configurationRoot = outerScope.ServiceProvider.GetService<IConfigurationRoot>();
                    Assert.Equal(configuration, configurationRoot);
                    Assert.Equal("test2", configuration["dependency"]);
                    Assert.Equal("test", configuration["test"]);

                    using (var scope = outerScope.ServiceProvider.GetService<IServiceScopeFactory>().CreateScope())
                    {
                        var scopedConfiguration = scope.ServiceProvider.GetService<IConfiguration>();

                        Assert.Equal(configuration, scopedConfiguration);
                        Assert.Equal("test2", scopedConfiguration["dependency"]);
                        Assert.Equal("test", scopedConfiguration["test"]);
                    }
                }
            }

           
            //Validating nothing is disposed at scope
            {
                var configuration = host.Services.GetService<IConfiguration>();
                var configurationRoot = host.Services.GetService<IConfigurationRoot>();
                Assert.Equal(configuration, configurationRoot);
                Assert.Equal("test2", configuration["dependency"]);
                Assert.Equal("test", configuration["test"]);
            }

        }
    }
}
