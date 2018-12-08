using Autofac;
using System;

namespace DotNetDevOps.ServiceFabric.Hosting
{
    public interface IServiceScopeInitializer
    {
        ILifetimeScope InitializeScope(ILifetimeScope container, Action<ContainerBuilder> action);
    }


}
