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
    public class RootSingleton : IDisposable
    {
        public RootSingleton()
        {
            Console.WriteLine(nameof(RootSingleton) + " created");
        }
        bool isDisposed = false;
        public void Dispose()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(RootSingleton));

            isDisposed = true;
            Console.WriteLine(nameof(RootSingleton) + " disposed");
        }
    }
    public class RootScoped : IDisposable
    {
        public RootScoped()
        {
            Console.WriteLine(nameof(RootScoped) + " created");
        }
        bool isDisposed = false;
        public void Dispose()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(RootScoped));

            isDisposed = true;
            Console.WriteLine(nameof(RootScoped) + " disposed");
        }
    }

    public class RootTrans : IDisposable
    {
        public RootTrans()
        {
            Console.WriteLine(nameof(RootTrans) + " created");
        }
        bool isDisposed = false;
        public void Dispose()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(RootTrans));

            isDisposed = true;
            Console.WriteLine(nameof(RootTrans) + " disposed");
        }
    }

    public class ChildSingleton : IDisposable
    {
        public ChildSingleton()
        {
            Console.WriteLine(nameof(ChildSingleton) + " created");
        }
        bool isDisposed = false;
        public void Dispose()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(ChildSingleton));

            isDisposed = true;
            Console.WriteLine(nameof(ChildSingleton) + " disposed");
        }
    }

    public class ChildScoped : IDisposable
    {
        private readonly RootScoped dep;

        public ChildScoped(RootScoped dep)
        {
            Console.WriteLine(nameof(ChildScoped) + " created");
            this.dep = dep;
        }
        bool isDisposed = false;
        public void Dispose()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(ChildScoped));

            isDisposed = true;
            Console.WriteLine(nameof(ChildScoped) + " disposed");
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
        public void TestChildContainers()
        {

            var host = new FabricHostBuilder()
                .ConfigureServices(serviceCollection =>
                {
                    serviceCollection.AddSingleton<RootSingleton>();
                    serviceCollection.AddTransient<RootTrans>();
                    serviceCollection.AddScoped<RootScoped>();
                  //  serviceCollection.AddSingleton(new ShouldUseParent(typeof(RootScoped)));
                });


            using (var app = host.Build())
            {

                using (var webhostScope = app.Services.CreateScope())
                {
                    var lifetime = webhostScope.ServiceProvider.GetService<ILifetimeScope>();
                    var a = new DefaultServiceProviderFactory(new ServiceProviderOptions { ValidateScopes=true });// ChildServiceProviderFactory(lifetime);

                   

                    var childservices = new ServiceCollection();
                    childservices.AddSingleton(lifetime);
                    childservices.AddParentSingleton<RootSingleton>();
                    childservices.AddSingleton<ChildSingleton>();
                    childservices.AddScoped<ChildScoped>();
                    childservices.AddParentScoped<RootScoped>();
                    childservices.AddParentTransient<RootTrans>();
                    //childservices.AddTransient(sp =>
                    //{
                         
                    //        var scope = sp.GetService<IServiceScope>();
                    //        return scope.ServiceProvider.GetService<RootScoped>();
                        
                    //});
                    
                    // childservices.AddScoped(sp => sp.GetService<RootScoped>());

                    var appServiceProvider = a.CreateServiceProvider(childservices);

                    using (var perRequestScope = appServiceProvider.CreateScope())
                    {
                        var rs = perRequestScope.ServiceProvider.GetService<RootSingleton>();
                        var rs1 = perRequestScope.ServiceProvider.GetService<RootScoped>();
                        var a1 = perRequestScope.ServiceProvider.GetService<RootTrans>();
                        var a2= perRequestScope.ServiceProvider.GetService<RootTrans>();

                    }

                    using (var perRequestScope = appServiceProvider.CreateScope())
                    {
                        var rs = perRequestScope.ServiceProvider.GetService<RootSingleton>();
                        var rs1 = perRequestScope.ServiceProvider.GetService<RootScoped>();
                        var a1 = perRequestScope.ServiceProvider.GetService<RootTrans>();
                        var a2 = perRequestScope.ServiceProvider.GetService<RootTrans>();

                        var test = perRequestScope.ServiceProvider.GetService<ChildScoped>();

                    }



                }

                using (var webhostScope = app.Services.CreateScope())
                {
                    var lifetime = webhostScope.ServiceProvider.GetService<ILifetimeScope>();
                    var a = new DefaultServiceProviderFactory(); // new ChildServiceProviderFactory(lifetime);

                    var childservices = new ServiceCollection();
                    childservices.AddSingleton(lifetime.Resolve<RootSingleton>());
                    childservices.AddSingleton<ChildSingleton>();
                    childservices.AddScoped<ChildScoped>();
                    childservices.AddScoped<RootScoped>();
                    // childservices.AddScoped(sp => sp.GetService<RootScoped>());

                    var appServiceProvider = a.CreateServiceProvider(childservices);

                    using (var perRequestScope = appServiceProvider.CreateScope())
                    {
                        var rs = perRequestScope.ServiceProvider.GetService<RootSingleton>();
                        var rs1 = perRequestScope.ServiceProvider.GetService<RootScoped>();


                    }

                    using (var perRequestScope = appServiceProvider.CreateScope())
                    {
                        var rs = perRequestScope.ServiceProvider.GetService<RootSingleton>();
                        var rs1 = perRequestScope.ServiceProvider.GetService<RootScoped>();


                    }



                }
            }



        }

        [Fact]
        public void TestForwardingScope()
        {
            var hostBuilder = new FabricHostBuilder()
                .ConfigureServices((context,services)=>
                {
                    services.AddSingleton<RootSingleton>();
                    services.AddTransient<RootTrans>();
                    services.AddScoped<RootScoped>();
                });

            using (var app = hostBuilder.Build())
            {

                using (var webhostScope = app.Services.CreateScope())
                {
                    //  var lifetime = webhostScope.ServiceProvider.GetService<ILifetimeScope>();

                    var a = webhostScope.ServiceProvider.GetService<IServiceProviderFactory<IServiceCollection>>();



                    var childservices = new ServiceCollection();                    
                    childservices.AddSingleton<ChildSingleton>();
                    childservices.AddScoped<ChildScoped>();
                 
                    var appServiceProvider = a.CreateServiceProvider(childservices);

                    using (var perRequestScope = appServiceProvider.CreateScope())
                    {
                        var rs = perRequestScope.ServiceProvider.GetRequiredService<RootSingleton>();
                        var rs1 = perRequestScope.ServiceProvider.GetRequiredService<RootScoped>();
                        var a1 = perRequestScope.ServiceProvider.GetRequiredService<RootTrans>();
                        var a2 = perRequestScope.ServiceProvider.GetRequiredService<RootTrans>();

                    }

                    using (var perRequestScope = appServiceProvider.CreateScope())
                    {
                        var rs = perRequestScope.ServiceProvider.GetRequiredService<RootSingleton>();
                        var rs1 = perRequestScope.ServiceProvider.GetRequiredService<RootScoped>();
                        var a1 = perRequestScope.ServiceProvider.GetRequiredService<RootTrans>();
                        var a2 = perRequestScope.ServiceProvider.GetRequiredService<RootTrans>();

                        var test = perRequestScope.ServiceProvider.GetRequiredService<ChildScoped>();

                    }



                }
                 
            }


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

                var outerconfig = host.Services.GetService<IConfiguration>();

                using (var outerScope = host.Services.GetService<IServiceScopeFactory>().CreateScope())
                {
                    var configuration = outerScope.ServiceProvider.GetService<IConfiguration>();
                    var configurationRoot = outerScope.ServiceProvider.GetService<IConfigurationRoot>();

                    Assert.Equal(outerconfig, configuration);
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
