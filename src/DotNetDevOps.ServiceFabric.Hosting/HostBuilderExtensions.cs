﻿using Autofac;
using Microsoft.Extensions.Hosting;
using Microsoft.ServiceFabric.Services.Remoting;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System.Threading;
using Microsoft.ServiceFabric.Services.Runtime;

namespace DotNetDevOps.ServiceFabric.Hosting
{
    public static class HostBuilderExtensions
    {
        private static void noop(ContainerBuilder obj)
        {

        }

        public static ILifetimeScope IntializeScope(this ILifetimeScope container, Action<ContainerBuilder> builder = null)
        {
            if (container.IsRegistered<IServiceScopeInitializer>())
            {
                var child = container.Resolve<IServiceScopeInitializer>().InitializeScope(container, builder ?? noop);


                return child;
            }

            return container.BeginLifetimeScope(configurationAction: (builder ?? noop));
        }

        public static IHostBuilder WithStatelessService<TStatelessService>(
            this IHostBuilder host,
            string serviceTypeName, 
            Action<ContainerBuilder> scopedRegistrations = null, 
            TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken)) where TStatelessService : StatelessService
        {
            return host.ConfigureServices((context, services) =>
            {
                services.AddSingleton(sp=>new StatelessServiceHost<TStatelessService>(serviceTypeName,sp,timeout,scopedRegistrations));
            });
            
        }
        public static IHostBuilder WithStatefullService<TStatefulService>(
            this IHostBuilder host,
            string serviceTypeName,
            Action<ContainerBuilder> scopedRegistrations = null,
            TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken)) where TStatefulService : StatefulService
        {
            return host.ConfigureServices((context, services) =>
            {
                services.AddSingleton(sp => new StatefulServiceHost<TStatefulService>(serviceTypeName, sp, timeout, scopedRegistrations));
            });

        }


        public static IHostBuilder WithServiceProxy<TServiceInterface>(this IHostBuilder host, string serviceName, string listenerName = null)
            where TServiceInterface : class, IService
        {

            return host.ConfigureServices((context, services) =>
            {
                services.AddScoped(sp => ServiceProxy.Create<TServiceInterface>(new Uri(serviceName), listenerName: listenerName));
            });

            
        }
    }


}