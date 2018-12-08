using Autofac;
using Microsoft.Extensions.Hosting;
using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetDevOps.ServiceFabric.Hosting
{
    public class StatelessServiceHost<TStatelessService>
        : BackgroundService
        where TStatelessService : StatelessService
    {
        private string serviceTypeName;
        private readonly IServiceProvider serviceProvider;
        private readonly TimeSpan timeout;
        private readonly Action<ContainerBuilder> scopedRegistrations;

        public StatelessServiceHost(
            string serviceTypeName,
            IServiceProvider serviceProvider,
            TimeSpan timeout = default(TimeSpan),
            Action<ContainerBuilder> scopedRegistrations = null)
        {
            this.serviceTypeName = serviceTypeName ?? throw new ArgumentNullException(nameof(serviceTypeName));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.timeout = timeout;
            this.scopedRegistrations = scopedRegistrations;
        }

        private static ILifetimeScope MakeServiceContainer<T>(ILifetimeScope container, T context, Action<ContainerBuilder> scopeRegistrations = null) where T : ServiceContext
        {

            var child = container.IntializeScope(builder =>
            {
                builder.RegisterInstance(context).ExternallyOwned().As<ServiceContext>().AsSelf();

                builder.RegisterInstance(context.CodePackageActivationContext).ExternallyOwned().AsSelf();
                scopeRegistrations?.Invoke(builder);
            });



            return child;
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
