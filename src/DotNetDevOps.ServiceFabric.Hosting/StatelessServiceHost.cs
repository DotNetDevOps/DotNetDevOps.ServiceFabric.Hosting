using Autofac;
using Microsoft.Extensions.Hosting;
using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors;
using DotNetDevOps.ServiceFabric.Hosting.DependencyInjection;

namespace DotNetDevOps.ServiceFabric.Hosting
{
    public abstract class ServiceHost<TService>: BackgroundService
    {
        protected ILifetimeScope MakeServiceContainer<T>(ILifetimeScope container, T context, Action<ContainerBuilder,T> scopeRegistrations = null) where T : ServiceContext
        {

            var child = container.IntializeScope(builder =>
            {
                builder.RegisterType<TService>().AsSelf();
                builder.RegisterInstance(context).ExternallyOwned().As<ServiceContext>().AsSelf();

                builder.RegisterInstance(context.CodePackageActivationContext).ExternallyOwned().AsSelf();
                scopeRegistrations?.Invoke(builder,context);
            });



            return child;
        }
    }
    public class ActorFactory<TActor> where TActor : ActorBase
    {
        private readonly ILifetimeScope scope;

        public ActorFactory(ILifetimeScope scope)
        {
            this.scope = scope;
        }
        public ActorBase Factory(ActorService service, ActorId id)
        {
            return this.scope.IntializeScope(builder =>
            {
                builder.RegisterInstance(service.Context.CodePackageActivationContext).ExternallyOwned();
                builder.RegisterInstance(service).ExternallyOwned();
                builder.RegisterInstance(id);
            }).Resolve<TActor>();
        }
    }

    public class ActorServiceHost<TActor, TActorService> : BackgroundService
         where TActor : ActorBase
         where TActorService : ActorService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly Func<ILifetimeScope, StatefulServiceContext, ActorTypeInformation, Func<ActorService, ActorId, TActor>, TActorService> actorServiceFactory;

        public ActorServiceHost(IServiceProvider serviceProvider, Func<ILifetimeScope, StatefulServiceContext, ActorTypeInformation, Func<ActorService, ActorId, TActor>, TActorService> actorServiceFactory = null)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.actorServiceFactory = actorServiceFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // container.RegisterType(typeof(TActor), ActorProxyTypeFactory.CreateType<TActor>(), new HierarchicalLifetimeManager());
            await ActorRuntime.RegisterActorAsync<TActor>((context, actorType) =>
            {
                try
                {



                    var serviceContainer = serviceProvider.GetService<ILifetimeScope>().IntializeScope(containerbuilder =>
                    {
                        containerbuilder.RegisterType<TActorService>();
                        containerbuilder.RegisterInstance(context).ExternallyOwned();
                        containerbuilder.RegisterInstance(actorType).ExternallyOwned();

                        containerbuilder.RegisterType<OnActorDeactivateInterceptor>().As<IActorDeactivationInterception>()
                        .InstancePerLifetimeScope();

                        containerbuilder.RegisterType(ActorProxyTypeFactory.CreateType<TActor>()).As(typeof(TActor)).InstancePerLifetimeScope();

                        containerbuilder.RegisterType<ActorFactory<TActor>>().SingleInstance();

                        containerbuilder.Register<Func<ActorService, ActorId, ActorBase>>(ctx => ctx.Resolve<ActorFactory<TActor>>().Factory);


                    });


                    if (actorServiceFactory == null)
                    {
                        return serviceContainer.Resolve<TActorService>();
                    }

                    return actorServiceFactory(serviceContainer, context, actorType, (service, id) =>
                                 serviceContainer
                                     .IntializeScope(builder =>
                                     {
                                         builder.RegisterInstance(service.Context.CodePackageActivationContext).ExternallyOwned();
                                         builder.RegisterInstance(service).ExternallyOwned();
                                         builder.RegisterInstance(id);
                                     }).Resolve<TActor>());
                }
                catch (Exception ex)
                {
                    // logger.LogCritical(new EventId(100, "FailedToCreateActorService"), ex, "Failed to create ActorService for {ActorName}", typeof(TActor).Name);
                    throw;
                }
            });

        }
    }


    public class StatelessServiceHost<TStatelessService>
        : ServiceHost<TStatelessService>
        where TStatelessService : StatelessService
    {
        private string serviceTypeName;
        private readonly IServiceProvider serviceProvider;
        private readonly TimeSpan timeout;
        private readonly Action<ContainerBuilder, StatelessServiceContext> scopedRegistrations;

        public StatelessServiceHost(
            string serviceTypeName,
            IServiceProvider serviceProvider,
            TimeSpan timeout = default(TimeSpan),
            Action<ContainerBuilder,StatelessServiceContext> scopedRegistrations = null)
        {
            this.serviceTypeName = serviceTypeName ?? throw new ArgumentNullException(nameof(serviceTypeName));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.timeout = timeout;
            this.scopedRegistrations = scopedRegistrations;
        }

       


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ServiceRuntime.RegisterServiceAsync(serviceTypeName,
                context =>
                {

                    var child = MakeServiceContainer(serviceProvider.GetService<ILifetimeScope>(), context, scopedRegistrations);
                    
                    try
                    {
                        return child.Resolve<TStatelessService>();

                    }
                    catch (Exception ex)
                    {
                        child.Resolve<ILoggerFactory>().CreateLogger<TStatelessService>().LogWarning(ex, "Throwing at service factory");
                        throw;
                    }

                }, timeout, cancellationToken: stoppingToken
                );
        }
    }


}
