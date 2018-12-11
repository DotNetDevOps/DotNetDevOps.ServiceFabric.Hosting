using Autofac;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DotNetDevOps.ServiceFabric.Hosting
{
    //public class ChildServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
    //{
    //    private ILifetimeScope container;

    //    public ChildServiceProviderFactory(ILifetimeScope container)
    //    {
    //        this.container = container;
    //    }

    //    public IServiceCollection CreateBuilder(IServiceCollection services)
    //    {
    //        return services;
    //    }



    //    public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
    //    {


    //        return new ChildServiceProvider(containerBuilder.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true }), container);
    //    }
    //}


}
