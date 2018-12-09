using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;

namespace DotNetDevOps.ServiceFabric.Hosting
{
    public interface IConfigurationBuilderExtension
    {
        IConfigurationBuilder Extend(IConfigurationBuilder cbuilder);
    }

    public class ChildServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
    {
        private ILifetimeScope container;

        public ChildServiceProviderFactory(ILifetimeScope container)
        {
            this.container = container;
        }

        public IServiceCollection CreateBuilder(IServiceCollection services)
        {
            return services;
        }



        public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
        {


            return new ChildServiceProvider(containerBuilder.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true }), container);
        }
    }
    public class ChildServiceProviderScope : IServiceScope
    {
        private ILifetimeScope lifetimeScope;
        private IServiceScope serviceScope;
        private ChildServiceProvider _serviceProvider;

        public ChildServiceProviderScope(ILifetimeScope lifetimeScope, IServiceScope serviceScope)
        {
            this.lifetimeScope = lifetimeScope;
            this.serviceScope = serviceScope;
            _serviceProvider = new ChildServiceProvider(serviceScope.ServiceProvider, lifetimeScope);
        }

        public IServiceProvider ServiceProvider => _serviceProvider;

        public void Dispose()
        {
            this.lifetimeScope.Dispose();
            this.serviceScope.Dispose();
        }
    }
    //public class ShouldUseParent
    //{
    //    private Type type;

    //    public ShouldUseParent(Type type)
    //    {
    //        this.type = type;
    //    }

    //    internal bool Yes(Type serviceType)
    //    {
    //        return serviceType == this.type;
    //    }
    //}

    public class NoneDisposableScope : IDisposable
    {
        private ILifetimeScope lifetimeScope;

        public NoneDisposableScope(ILifetimeScope lifetimeScope)
        {
            this.lifetimeScope = lifetimeScope;
        }

        public void Dispose()
        {
            var stackField = lifetimeScope.Disposer.GetType().GetField("_items", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
           var stack= stackField
               .GetValue(lifetimeScope.Disposer);
            if(stack is Stack<IDisposable> disposables)
            {
                var newStack = new Stack<IDisposable>();
                foreach(var item in disposables.Reverse())
                {
                    if (!_doNotDisposes.Contains(item))
                    {
                        newStack.Push(item);
                    }
                }
                stackField.SetValue(lifetimeScope.Disposer, newStack);
            }
            else
            {
                throw new Exception("autofact changed, please update");
            }

            lifetimeScope.Dispose();

        }

        public T Resolve<T>()
        {
            return Capture( lifetimeScope.Resolve<T>());
        }

        private HashSet<object> _doNotDisposes = new HashSet<object>();

        private T Capture<T>(T t)
        {
            if(t is IDisposable)
            {
                _doNotDisposes.Add(t);
            }
            return t;
        }
    }
    
    public static class ServiceExtensions
    {
        public static IServiceCollection AddParentSingleton<T>(this IServiceCollection services) where T:class
        {
             
            return services.AddSingleton(sp=> sp.GetRequiredService< ILifetimeScope>().Resolve<T>());
        }
        public static IServiceCollection AddParentScoped<T>(this IServiceCollection services) where T : class
        {
            services.TryAddScoped(sp=>new NoneDisposableScope(sp.GetRequiredService<ILifetimeScope>().BeginLifetimeScope()));
            return services.AddScoped(sp=>sp.GetService< NoneDisposableScope>().Resolve<T>());
        }
        public static IServiceCollection AddParentTransient<T>(this IServiceCollection services) where T : class
        {
            services.TryAddScoped(sp => new NoneDisposableScope(sp.GetRequiredService<ILifetimeScope>().BeginLifetimeScope()));
            return services.AddTransient(sp => sp.GetService<NoneDisposableScope>().Resolve<T>());
        }
    }

    public class ChildServiceProvider : IServiceProvider, IServiceScopeFactory
    {
        private IServiceProvider serviceProvider;
        private ILifetimeScope container;
      

        public ChildServiceProvider(IServiceProvider serviceProvider, ILifetimeScope container )
        {
            this.serviceProvider = serviceProvider;
            this.container = container;
           
        }

        public IServiceScope CreateScope()
        {
            return new ChildServiceProviderScope(container.BeginLifetimeScope(), this.serviceProvider.CreateScope());

        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceScopeFactory))
            {
                return this;
            }

           
            var value = this.serviceProvider.GetService(serviceType);
            if (value == null)
                value = container.Resolve(serviceType);
            return value;
        }
    }

    //  public class ServiceProxy
    public class FabricHostBuilder : HostBuilder
    {
        public FabricHostBuilder()
        {
            UseServiceProviderFactory(new AutofacServiceProviderFactory());
            ConfigureContainer<ContainerBuilder>((context, builder) =>
             {
                 builder.Register(c => FabricRuntime.Create()).AsSelf().SingleInstance();
                 builder.Register(c => FabricRuntime.GetActivationContext()).As<ICodePackageActivationContext>().SingleInstance();
                 builder.Register(c => c.Resolve<ICodePackageActivationContext>().GetConfigurationPackageObject("config")).As<ConfigurationPackage>().SingleInstance();
                 builder.Register(c => new FabricClient()).As<FabricClient>().SingleInstance();

                 builder.RegisterInstance(new ConfigurationBuilder()).As<IConfigurationBuilder>();
                 builder.Register(c =>
                 {
                     var extensions = c.Resolve<IEnumerable<IConfigurationBuilderExtension>>();
                     var cbuilder = c.Resolve<IConfigurationBuilder>();
                     cbuilder.AddConfiguration(context.Configuration);
                     foreach (var extension in extensions)
                     {
                         cbuilder = extension.Extend(cbuilder);
                     }
                     return cbuilder.Build();

                 }).AsSelf().As<IConfiguration>().SingleInstance();


             });
          
        }



      


    }


}
