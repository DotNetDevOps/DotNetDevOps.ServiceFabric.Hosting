using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetDevOps.ServiceFabric.Hosting
{
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


}
