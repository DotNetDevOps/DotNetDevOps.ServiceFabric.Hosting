using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetDevOps.ServiceFabric.Hosting
{
    public class ConsoleArguments
    {
        public string[] Args { get; }

        public ConsoleArguments(string[] args)
        {
            this.Args = args;
        }

        public bool IsServiceFabric => this.Args.Contains("--serviceFabric");
    }

    public interface IDataProtectionStoreService : IService
    {
        Task<string> GetApplicationSasUri();
        Task<string> GetVaultTokenAsync(string authority, string resource, string scope);


    }

    //  public class ServiceProxy
    public class FabricHostBuilder : HostBuilder
    {

        public FabricHostBuilder(string[] arguments, bool useServiceFactoryForwarding=true)
        {
            var culture = new CultureInfo("en-Us"); // replace en-US with the selected culture or string from the combobox
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            UseServiceProviderFactory(useServiceFactoryForwarding ? 
                new MyAutofacServiceProviderFactory() : 
                new AutofacServiceProviderFactory()
                as IServiceProviderFactory<ContainerBuilder>
            );
            ConfigureServices((context, services) =>
            {
                var args = new ConsoleArguments(arguments); ;

                services.AddSingleton(args);
                context.Properties["ConsoleArguments"] = args;
                if (args.IsServiceFabric)
                    context.Properties["ApplicationStorageService"] =
                        ServiceProxy.Create<IDataProtectionStoreService>(new Uri($"fabric:/{context.Configuration.GetValue<string>("DataProtectionSettings:GatewayApplicationName", "S-Innovations.ServiceFabric.GatewayApplication")}/ApplicationStorageService"), listenerName: "V2_1Listener");

                services.AddSingleton(c => FabricRuntime.Create());
                services.AddSingleton< ICodePackageActivationContext>(c => FabricRuntime.GetActivationContext());
                services.AddSingleton(c => c.GetService<ICodePackageActivationContext>().GetConfigurationPackageObject("config"));
                services.AddSingleton(c => new FabricClient());

                if(useServiceFactoryForwarding)
                    services.AddSingleton<IServiceProviderFactory<IServiceCollection>, ForwardingServiceProviderFactory>();

                 services.AddSingleton< IConfigurationBuilder>(new ConfigurationBuilder());
                services.AddSingleton(c =>
                {
                    var extensions = c.GetService<IEnumerable<IConfigurationBuilderExtension>>();
                    var cbuilder = c.GetService<IConfigurationBuilder>();
                    cbuilder.AddConfiguration(context.Configuration);
                    foreach (var extension in extensions)
                    {
                        cbuilder = extension.Extend(cbuilder);
                    }
                    return cbuilder.Build();

                });
                services.AddSingleton<IConfiguration>(c=>c.GetRequiredService< IConfigurationRoot>());
            });
            ConfigureContainer<ContainerBuilder>((context, builder) =>
             {

                 //builder.Register(c => FabricRuntime.Create()).AsSelf().SingleInstance();
                 //builder.Register(c => FabricRuntime.GetActivationContext()).As<ICodePackageActivationContext>().SingleInstance();
                 //builder.Register(c => c.Resolve<ICodePackageActivationContext>().GetConfigurationPackageObject("config")).As<ConfigurationPackage>().SingleInstance();
                 //builder.Register(c => new FabricClient()).As<FabricClient>().SingleInstance();

                 //builder.RegisterInstance(new ConfigurationBuilder()).As<IConfigurationBuilder>();
                 //builder.Register(c =>
                 //{
                 //    var extensions = c.Resolve<IEnumerable<IConfigurationBuilderExtension>>();
                 //    var cbuilder = c.Resolve<IConfigurationBuilder>();
                 //    cbuilder.AddConfiguration(context.Configuration);
                 //    foreach (var extension in extensions)
                 //    {
                 //        cbuilder = extension.Extend(cbuilder);
                 //    }
                 //    return cbuilder.Build();

                 //}).AsSelf().As<IConfiguration>().SingleInstance();

                 builder.RegisterInstance(new OptionRegistration { ServiceType = typeof(IHostedService), ServiceLifetime = ServiceLifetime.Singleton, ShouldIgnore=true });

             });

            ConfigureHostConfiguration((configurationBuilder) =>
            {

                configurationBuilder
                    .AddCommandLine(arguments)
                    .AddInMemoryCollection(new[]{ new KeyValuePair<string,string>(HostDefaults.EnvironmentKey, System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")) })
                    .AddEnvironmentVariables();


            });


        }



      


    }


}
