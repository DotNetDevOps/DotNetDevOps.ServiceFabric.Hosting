using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Fabric;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DotNetDevOps.ServiceFabric.Hosting
{
    //  public class ServiceProxy
    public class FabricHostBuilder : HostBuilder
    {

        public FabricHostBuilder(bool useServiceFactoryForwarding=true)
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
                    .AddInMemoryCollection(new[]{ new KeyValuePair<string,string>(HostDefaults.EnvironmentKey, System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")) })
                    .AddEnvironmentVariables();


            });


        }



      


    }


}
