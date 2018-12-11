using Autofac;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DotNetDevOps.ServiceFabric.Hosting
{
    //public class ChildServiceProviderScope : IServiceScope
    //{
    //    private ILifetimeScope lifetimeScope;
    //    private IServiceScope serviceScope;
    //    private ChildServiceProvider _serviceProvider;

    //    public ChildServiceProviderScope(ILifetimeScope lifetimeScope, IServiceScope serviceScope)
    //    {
    //        this.lifetimeScope = lifetimeScope;
    //        this.serviceScope = serviceScope;
    //        _serviceProvider = new ChildServiceProvider(serviceScope.ServiceProvider, lifetimeScope);
    //    }

    //    public IServiceProvider ServiceProvider => _serviceProvider;

    //    public void Dispose()
    //    {
    //        this.lifetimeScope.Dispose();
    //        this.serviceScope.Dispose();
    //    }
    //}


}
