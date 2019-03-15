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
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors;
using DotNetDevOps.ServiceFabric.Hosting.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.KeyVault;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace DotNetDevOps.ServiceFabric.Hosting
{

    public class KeyMatrialStore
    {
        public X509Certificate2[] Certs { get; }

        public KeyMatrialStore(X509Certificate2[] certs)
        {
            this.Certs = certs;
        }
    }

    public static class HostBuilderExtensions
    {
        public static IHostBuilder ConfigureDefaultAppConfiguration(this IHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureAppConfiguration((context, configurationBuilder) =>
                  {
                      configurationBuilder
                          .SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                          .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                          .AddEnvironmentVariables();

                      if (context.GetConsoleArguments().IsServiceFabric)
                      {
                          configurationBuilder.AddServiceFabricConfig("Config");
                      }
                  });
        }
        public static async Task<X509Certificate2[]> GetCerts(this KeyVaultClient Client, string KeyVaultUri,string SecretName)
        {
            //  var certs = await Client.GetSecretAsync("","idsrv");
            //  certs.
            var certsVersions = await Client.GetSecretVersionsAsync(KeyVaultUri, SecretName);

            var secrets = await Task.WhenAll(certsVersions.Where(k => k.Attributes?.Enabled ?? false).Select(k => Client.GetSecretAsync(k.Identifier.Identifier)));

            var certs = secrets.Select(s => new X509Certificate2(Convert.FromBase64String(s.Value), (string)null, X509KeyStorageFlags.MachineKeySet)).ToArray();
            return certs;

        }

        public static IHostBuilder AddGatewayDataProtection(this IHostBuilder hostBuilder)
        {


            return hostBuilder.ConfigureServices((context, services) =>
            {
                var args = context.Properties["ConsoleArguments"] as ConsoleArguments;
                if (args.IsServiceFabric)
                {
                    var dataprotection = context.Properties["ApplicationStorageService"] as IDataProtectionStoreService;
                    var container = new CloudBlobContainer(new Uri(dataprotection.GetApplicationSasUri().GetAwaiter().GetResult()));

                    var certs = new KeyVaultClient(dataprotection.GetVaultTokenAsync)
                    .GetCerts(
                        context.Configuration.GetValue<string>("DataProtectionSettings:KeyVaultUri"), 
                        context.Configuration.GetValue<string>("DataProtectionSettings:SecretName"))
                        .GetAwaiter().GetResult();

                    services.AddSingleton(new KeyMatrialStore(certs));
                    
                

                    services.AddDataProtection()
                         .SetApplicationName(context.Configuration.GetValue<string>("DataProtectionSettings:ApplicationName"))
                          .PersistKeysToAzureBlobStorage(container.GetBlockBlobReference(context.Configuration.GetValue<string>("DataProtectionSettings:ApplicationName") + ".csrf"))
                          .ProtectKeysWithCertificate(certs.First())
                          .UnprotectKeysWithAnyCertificate(certs.Skip(1).ToArray());


                }
                ;
            });
        }

 

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

        public static IHostBuilder WithActor<TActor>(this IHostBuilder builder, ActorServiceSettings settings = null) where TActor : ActorBase
        {
            return builder.WithActor<TActor, ActorService>(
                (servicecontainer, context, actorType, actorFactory) =>
                    new ActorService(context, actorTypeInfo: actorType, actorFactory: actorFactory, settings: settings));
        }
        /// <summary>
        /// Add an actor implementation to the fabric container
        /// </summary>
        /// <typeparam name="TActor"></typeparam>
        /// <typeparam name="TActorService"></typeparam>
        /// <param name="container"></param>
        /// <param name="ActorServiceFactory"></param>
        /// <returns></returns>
        public static IHostBuilder WithActor<TActor, TActorService>(
            this IHostBuilder builder,
            Func<ILifetimeScope, StatefulServiceContext, ActorTypeInformation, Func<ActorService, ActorId, TActor>, TActorService> ActorServiceFactory = null)
            where TActor : ActorBase
            where TActorService : ActorService
        {
            //var logger = container.Resolve<ILoggerFactory>().CreateLogger<TActor>();
            //logger.LogInformation("Registering Actor {ActorName}", typeof(TActor).Name);

            //  container = container.IntializeScope();


           

            builder.ConfigureServices((ctx, services) =>
            {
                if(ActorServiceFactory != null)
                {
                    services.AddSingleton(ActorServiceFactory);
                }
           
                    services.AddSingleton<IHostedService, ActorServiceHost<TActor, TActorService>>();
              
            });

           



            return builder;
        }

        public static IServiceCollection WithStatelessService<TStatelessService>(
            this IServiceCollection services,
            string serviceTypeName,
            Action<ContainerBuilder, StatelessServiceContext> scopedRegistrations = null,
            TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken)) where TStatelessService : StatelessService
        {
            
             return   services.AddSingleton<IHostedService>(sp => new StatelessServiceHost<TStatelessService>(serviceTypeName, sp, timeout, scopedRegistrations));
            
        }

        

        public static IHostBuilder WithStatelessService<TStatelessService>(
            this IHostBuilder host,
            string serviceTypeName, 
            Action<ContainerBuilder, StatelessServiceContext> scopedRegistrations = null, 
            TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken)) where TStatelessService : StatelessService
        {
            return host.ConfigureServices((context, services) =>
            {
                services.WithStatelessService<TStatelessService>(serviceTypeName, scopedRegistrations, timeout, cancellationToken);
               // services.AddSingleton<IHostedService>(sp=>new StatelessServiceHost<TStatelessService>(serviceTypeName,sp,timeout,scopedRegistrations));
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
                ////services.RegisterInstance(new OptionRegistration() {  IConfigureOptionsType = typeof(IConfigureOptions<T>), IOptionsChangeTokenSourceType = typeof(IOptionsChangeTokenSource<T>) });
                services.RegisterInstance(new OptionRegistration {  ServiceType = typeof(IConfigureOptions<T>) , ServiceLifetime = ServiceLifetime.Singleton });
                services.RegisterInstance(new OptionRegistration { ServiceType = typeof(IOptionsChangeTokenSource<T>), ServiceLifetime = ServiceLifetime.Singleton });

            });

            host.ConfigureServices((context, services) =>
            {
              //  services.Configure<T>(context.Configuration.GetSection(sectionName));
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
