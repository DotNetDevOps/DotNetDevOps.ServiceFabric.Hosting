using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Fabric;

namespace DotNetDevOps.ServiceFabric.Hosting
{
    public interface IConfigurationBuilderExtension
    {
        IConfigurationBuilder Extend(IConfigurationBuilder cbuilder);
    }

    //  public class ServiceProxy
    public class FabricHostBuilder : HostBuilder
    {
        public FabricHostBuilder()
        {
            UseServiceProviderFactory(new AutofacServiceProviderFactory());
            ConfigureContainer<ContainerBuilder>((context, builder) =>
             {
                 builder.Register(c => FabricRuntime.Create()).AsSelf().SingleInstance();
                 builder.Register(c => FabricRuntime.GetActivationContext()).As<ICodePackageActivationContext>().SingleInstance();
                 builder.Register(c => c.Resolve<ICodePackageActivationContext>().GetConfigurationPackageObject("config")).As<ConfigurationPackage>().SingleInstance();
                 builder.Register(c => new FabricClient()).As<FabricClient>().SingleInstance();

                 builder.RegisterInstance(new ConfigurationBuilder()).As<IConfigurationBuilder>();
                 builder.Register(c =>
                 {
                     var extensions = c.Resolve<IEnumerable<IConfigurationBuilderExtension>>();
                     var cbuilder = c.Resolve<IConfigurationBuilder>();
                     cbuilder.AddConfiguration(context.Configuration);
                     foreach (var extension in extensions)
                     {
                         cbuilder = extension.Extend(cbuilder);
                     }
                     return cbuilder.Build();

                 }).AsSelf().As<IConfiguration>().SingleInstance();


             });
          
        }



      


    }


}
