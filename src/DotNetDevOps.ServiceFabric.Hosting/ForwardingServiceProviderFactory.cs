using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace DotNetDevOps.ServiceFabric.Hosting
{
    public class ForwardingServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
    {
        private readonly IServiceCollection parent;
        private readonly ILifetimeScope lifetimeScope;

        public ForwardingServiceProviderFactory(IServiceCollection parent, ILifetimeScope lifetimeScope)
        {
            this.parent = parent;
            this.lifetimeScope = lifetimeScope;
        }
        public IServiceCollection CreateBuilder(IServiceCollection services)
        {
            return services;
        }

        public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
        {
            containerBuilder.AddSingleton(lifetimeScope);
            containerBuilder.TryAddScoped(sp => new NoneDisposableScope(lifetimeScope.BeginLifetimeScope()));
            // containerBuilder.AddTransient<), OptionsManager<>>();

            foreach (var parentregistration in parent)
            {
                if(parentregistration.ServiceType == typeof(IOptions<>)){

                }
                if(parentregistration.ServiceType.IsGenericTypeDefinition)
                {
                    continue;
                }

                switch (parentregistration.Lifetime)
                {
                    case ServiceLifetime.Singleton:

                        containerBuilder.AddSingleton(parentregistration.ServiceType, sp => lifetimeScope.Resolve(parentregistration.ServiceType));
                        break;
                    case ServiceLifetime.Scoped:
                        containerBuilder.AddScoped(parentregistration.ServiceType,sp => sp.GetService<NoneDisposableScope>().Resolve(parentregistration.ServiceType));
                        break;
                    case ServiceLifetime.Transient:
                        containerBuilder.AddTransient(parentregistration.ServiceType,sp => sp.GetService<NoneDisposableScope>().Resolve(parentregistration.ServiceType));
                        break;

                }


            }

            var options = lifetimeScope.Resolve<IEnumerable<OptionRegistration>>();
            foreach(var option in options)
            {
                containerBuilder.AddSingleton(option.IConfigureOptionsType, sp => lifetimeScope.Resolve(option.IConfigureOptionsType));
                containerBuilder.AddSingleton(option.IOptionsChangeTokenSourceType, sp => lifetimeScope.Resolve(option.IOptionsChangeTokenSourceType));
            }

            return containerBuilder.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        }
    }


}
