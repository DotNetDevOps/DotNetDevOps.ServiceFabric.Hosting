using Autofac;
using Microsoft.Extensions.Hosting;
using Microsoft.ServiceFabric.Services.Remoting;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System.Threading;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using System.Fabric;

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
            Action<ContainerBuilder, StatelessServiceContext> scopedRegistrations = null, 
            TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken)) where TStatelessService : StatelessService
        {
            return host.ConfigureServices((context, services) =>
            {
                services.AddSingleton<IHostedService>(sp=>new StatelessServiceHost<TStatelessService>(serviceTypeName,sp,timeout,scopedRegistrations));
            });
            
        }
        public static IHostBuilder WithStatefullService<TStatefulService>(
            this IHostBuilder host,
            string serviceTypeName,
            Action<ContainerBuilder,StatefulServiceContext> scopedRegistrations = null,
            TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken)) where TStatefulService : StatefulService
        {
            return host.ConfigureServices((context, services) =>
            {
                services.AddSingleton<IHostedService>(sp => new StatefulServiceHost<TStatefulService>(serviceTypeName, sp, timeout, scopedRegistrations));
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

        /// <summary>
        /// Configure using a subsection name for T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="container"></param>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        public static IHostBuilder Configure<T>(this IHostBuilder host, string sectionName) where T : class
        {
            host.ConfigureContainer<ContainerBuilder>(services =>
            {
                services.Register((c) => new ConfigurationChangeTokenSource<T>(c.Resolve<IConfigurationRoot>().GetSection(sectionName))).As<IOptionsChangeTokenSource<T>>().SingleInstance();
                services.Register((c) => new ConfigureFromConfigurationOptions<T>(c.Resolve<IConfigurationRoot>().GetSection(sectionName))).As<IConfigureOptions<T>>().SingleInstance();
                //services.RegisterInstance(new OptionRegistration() {  IConfigureOptionsType = typeof(IConfigureOptions<T>), IOptionsChangeTokenSourceType = typeof(IOptionsChangeTokenSource<T>) });
                services.RegisterInstance(new OptionRegistration {  ServiceType = typeof(IConfigureOptions<T>) , ServiceLifetime = ServiceLifetime.Singleton });
                services.RegisterInstance(new OptionRegistration { ServiceType = typeof(IOptionsChangeTokenSource<T>), ServiceLifetime = ServiceLifetime.Singleton });

            });

            host.ConfigureServices((context, services) =>
            {
               
            });
           
            return host;

           

        }
    }
    public class OptionRegistration
    {
      //  public Type IConfigureOptionsType {get;set;}
      //  public Type IOptionsChangeTokenSourceType { get; set; }


        public Type ServiceType { get; set; }
        public ServiceLifetime ServiceLifetime { get; set; }

        public bool ShouldIgnore { get; set; }
    }

}
