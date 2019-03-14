using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DotNetDevOps.ServiceFabric.Hosting
{
    public class ForwardingServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
    {
        private readonly IServiceCollection parent;
        private readonly ILifetimeScope lifetimeScope;
        private readonly ILogger<ForwardingServiceProviderFactory> logger;

        public ForwardingServiceProviderFactory(IServiceCollection parent, ILifetimeScope lifetimeScope, ILogger<ForwardingServiceProviderFactory> logger)
        {
            this.parent = parent;
            this.lifetimeScope = lifetimeScope;
            this.logger = logger;
        }
        public IServiceCollection CreateBuilder(IServiceCollection services)
        {
            return services;
        }
        public static int Count(IEnumerable data)
        {
            ICollection list = data as ICollection;
            if (list != null) return list.Count;
            int count = 0;
            IEnumerator iter = data.GetEnumerator();
            using (iter as IDisposable)
            {
                while (iter.MoveNext()) count++;
            }
            return count;
        }
        public IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(lifetimeScope);
            serviceCollection.TryAddScoped(sp => new NoneDisposableScope(lifetimeScope.BeginLifetimeScope()));
            // containerBuilder.AddTransient<), OptionsManager<>>();


            var options = lifetimeScope.Resolve<IEnumerable<OptionRegistration>>();
            var ignores = options.Where(o => o.ShouldIgnore).ToLookup(k => k.ServiceType);

            var test = parent.GroupBy(k => k.ServiceType).Where(k => k.Count() > 1).ToArray();

            foreach (var parentregistration in parent)
            {

                if (parentregistration.ServiceType.IsGenericTypeDefinition)
                {
                    continue;
                }
                if (ignores.Contains(parentregistration.ServiceType))
                {
                    continue;
                }


                switch (parentregistration.Lifetime)
                {
                    case ServiceLifetime.Singleton:

                        // if the parent has more singletons registed, register them seperatly.
                        {
                            IEnumerable registrations = null;
                            var count = 0;
                            try
                            {
                                var enumerableType = typeof(IEnumerable<>).MakeGenericType(parentregistration.ServiceType);
                                registrations = lifetimeScope.Resolve(enumerableType) as IEnumerable;
                                count = Count(registrations);

                            }
                            catch (Exception ex)
                            {

                            }

                            if (count > 1)
                            {
                                foreach (var instance in registrations)
                                {
                                    serviceCollection.AddSingleton(parentregistration.ServiceType, instance);
                                }
                            }
                            else
                            {

                                serviceCollection.AddSingleton(parentregistration.ServiceType, sp => lifetimeScope.Resolve(parentregistration.ServiceType));
                            }
                        }
                        break;
                    case ServiceLifetime.Scoped:
                        {
                            using (var scope = lifetimeScope.BeginLifetimeScope())
                            {
                                IEnumerable registrations = null;
                                var count = 0;
                                try
                                {
                                    var enumerableType = typeof(IEnumerable<>).MakeGenericType(parentregistration.ServiceType);
                                    registrations = scope.Resolve(enumerableType) as IEnumerable;
                                    count = Count(registrations);

                                }
                                catch (Exception ex)
                                {

                                }

                                if (count > 1)
                                {
                                    logger.LogWarning("Scoped registration for {type} has {count} registrations", parentregistration.ServiceType, count);
                                }

                                serviceCollection.AddScoped(parentregistration.ServiceType, sp => sp.GetService<NoneDisposableScope>().Resolve(parentregistration.ServiceType));

                            }


                        }
                        break;
                    case ServiceLifetime.Transient:
                        {
                            using (var scope = lifetimeScope.BeginLifetimeScope())
                            {
                                IEnumerable registrations = null;
                                var count = 0;
                                try
                                {
                                    var enumerableType = typeof(IEnumerable<>).MakeGenericType(parentregistration.ServiceType);
                                    registrations = scope.Resolve(enumerableType) as IEnumerable;
                                    count = Count(registrations);

                                }
                                catch (Exception ex)
                                {

                                }

                                if (count > 1)
                                {
                                    logger.LogWarning("Transient registration for {type} has {count} registrations", parentregistration.ServiceType, count);
                                }

                            }
                            serviceCollection.AddTransient(parentregistration.ServiceType, sp => sp.GetService<NoneDisposableScope>().Resolve(parentregistration.ServiceType));
                        }
                        break;

                }


            }

            foreach (var option in options.Where(o => !o.ShouldIgnore))
            {
                if (option.ServiceLifetime == ServiceLifetime.Singleton)
                {
                    serviceCollection.AddSingleton(option.ServiceType, sp => lifetimeScope.Resolve(option.ServiceType));
                }

            }

            return serviceCollection.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        }
    }


}
