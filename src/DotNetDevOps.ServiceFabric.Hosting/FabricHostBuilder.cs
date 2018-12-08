using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Fabric;

namespace DotNetDevOps.ServiceFabric.Hosting
{
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
             });
        }




    }


}
