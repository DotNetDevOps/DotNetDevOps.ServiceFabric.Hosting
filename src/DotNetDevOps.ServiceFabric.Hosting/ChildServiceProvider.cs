using Autofac;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DotNetDevOps.ServiceFabric.Hosting
{
    //public class ChildServiceProvider : IServiceProvider, IServiceScopeFactory
    //{
    //    private IServiceProvider serviceProvider;
    //    private ILifetimeScope container;
      

    //    public ChildServiceProvider(IServiceProvider serviceProvider, ILifetimeScope container )
    //    {
    //        this.serviceProvider = serviceProvider;
    //        this.container = container;
           
    //    }

    //    public IServiceScope CreateScope()
    //    {
    //        return new ChildServiceProviderScope(container.BeginLifetimeScope(), this.serviceProvider.CreateScope());

    //    }

    //    public object GetService(Type serviceType)
    //    {
    //        if (serviceType == typeof(IServiceScopeFactory))
    //        {
    //            return this;
    //        }

           
    //        var value = this.serviceProvider.GetService(serviceType);
    //        if (value == null)
    //            value = container.Resolve(serviceType);
    //        return value;
    //    }
    //}


}
