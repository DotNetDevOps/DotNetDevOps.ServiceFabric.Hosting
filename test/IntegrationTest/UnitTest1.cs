using DotNetDevOps.ServiceFabric.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Autofac;
using Microsoft.Extensions.Options;

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
            return cbuilder.AddInMemoryCollection(new[] { new KeyValuePair<string, string>("dependency",this.dependency.Value), new KeyValuePair<string, string>("MySection:Test1", "ConfigureExample1") });
        }
    }
    public class MyOptions
    {
        public string Test { get; set; }
        public string Test1 { get; set; }
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
                    builder.AddInMemoryCollection(new[] { new KeyValuePair<string, string>("test", "test"), new KeyValuePair<string, string>("MySection:Test", "ConfigureExample") });
                }).ConfigureServices((context, services) =>
                {

                    services.AddSingleton<ConfigurationExtenderExamplerDependency>();
                    services.AddSingleton<IConfigurationBuilderExtension, ConfigurationExtenderExampler>();
                }).Configure<MyOptions>("MySection");
                 
            

            var host = hostBuilder.Build();
            {
                var internalScope = host.Services.GetService<ILifetimeScope>();
                Assert.True(internalScope.IsRegistered<IConfiguration>());

                using (var outerScope = host.Services.GetService<IServiceScopeFactory>().CreateScope())
                {
                    var configuration = outerScope.ServiceProvider.GetService<IConfiguration>();
                    var configurationRoot = outerScope.ServiceProvider.GetService<IConfigurationRoot>();
                 
                    Assert.Equal(configuration, configurationRoot);
                    Assert.Equal("test2", configuration["dependency"]);
                    Assert.Equal("test", configuration["test"]);

                    var internalScope1 = outerScope.ServiceProvider.GetService<ILifetimeScope>();
                    Assert.NotEqual(internalScope, internalScope1);
                    Assert.True(internalScope.IsRegistered<IConfiguration>());

                    using (var scope = outerScope.ServiceProvider.GetService<IServiceScopeFactory>().CreateScope())
                    {
                        var scopedConfiguration = scope.ServiceProvider.GetService<IConfiguration>();

                        Assert.Equal(configuration, scopedConfiguration);
                        Assert.Equal("test2", scopedConfiguration["dependency"]);
                        Assert.Equal("test", scopedConfiguration["test"]);

                        var internalScope2 = scope.ServiceProvider.GetService<ILifetimeScope>();
                        Assert.NotEqual(internalScope, internalScope2);
                        Assert.NotEqual(internalScope1, internalScope2);
                        Assert.True(internalScope.IsRegistered<IConfiguration>());

                        Assert.Equal("ConfigureExample", scope.ServiceProvider.GetService<IOptions<MyOptions>>().Value.Test);
                        Assert.Equal("ConfigureExample1", scope.ServiceProvider.GetService<IOptions<MyOptions>>().Value.Test1);
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
