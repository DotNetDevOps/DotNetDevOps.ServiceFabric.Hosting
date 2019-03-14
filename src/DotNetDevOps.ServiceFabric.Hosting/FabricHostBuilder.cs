using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;

namespace DotNetDevOps.ServiceFabric.Hosting
{
    public class ConsoleArguments
    {
        public string[] Args { get; }

        public ConsoleArguments(string[] args)
        {
            this.Args = args;
        }

        public bool IsServiceFabric => this.Args.Contains("--serviceFabric");
    }

    public interface IDataProtectionStoreService : IService
    {
        Task<string> GetApplicationSasUri();
        Task<string> GetVaultTokenAsync(string authority, string resource, string scope);


    }

    public static class ServiceFabricConfigExtensions
    {
        public static IConfigurationBuilder AddServiceFabricConfig(this IConfigurationBuilder builder, string packageName)
        {
            return builder.Add(new ServiceFabricConfigSource(packageName));
        }
    }

    public class ServiceFabricConfigSource : IConfigurationSource
    {
        public string PackageName { get; set; }

        public ServiceFabricConfigSource(string packageName)
        {
            PackageName = packageName;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {

            return new ServiceFabricConfigurationProvider(PackageName);
        }
    }
    public class ServiceFabricConfigurationProvider : ConfigurationProvider
    {
        private readonly string _packageName;
        private readonly CodePackageActivationContext _context;


        public ServiceFabricConfigurationProvider(string packageName)
        {
            try
            {
                _packageName = packageName;
                _context = FabricRuntime.GetActivationContext();
                _context.ConfigurationPackageModifiedEvent += (sender, e) =>
                {
                    this.LoadPackage(e.NewPackage, reload: true);
                    this.OnReload(); // Notify the change
                };
            }
            catch (Exception)
            {

            }


        }

        public override void Load()
        {
            var config = _context.GetConfigurationPackageObject(_packageName);
            LoadPackage(config);
        }

        private void LoadPackage(ConfigurationPackage config, bool reload = false)
        {
            if (reload)
            {
                Data.Clear();  // Rememove the old keys on re-load
            }
            foreach (var section in config.Settings.Sections)
            {
                foreach (var param in section.Parameters)
                {

                    try
                    {
                        Data[$"{section.Name}:{param.Name}"] = param.IsEncrypted && !string.IsNullOrEmpty(param.Value) ? param.DecryptValue().ToUnsecureString() : param.Value;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Failed to add \"{section.Name}:{param.Name}\" from {param.Value} encryption={param.IsEncrypted}");
                        //logger.LogWarning("Failed to add {key} from {value} encryption={encryption}", $"{section.Name}:{param.Name}",param.Value, param.IsEncrypted);
                    }
                }
            }
        }

    }
    public static class SecureStringExtensions
    {
        /// <summary>
        /// Gets a plaintext string value from a SecureString to use in APIs that don't accept SecureString parameters.
        /// </summary>
        /// <param name="secureString"></param>
        /// <returns></returns>
        public static string ToUnsecureString(this SecureString secureString)
        {
            if (secureString == null)
            {
                throw new ArgumentNullException();
            }

            IntPtr unmanagedString = IntPtr.Zero;

            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
    }
    public static class HostBuilderContextExtensions
    {
        public static ConsoleArguments GetConsoleArguments(this HostBuilderContext context)
        {
            return context.Properties["ConsoleArguments"] as ConsoleArguments;
        }
        public static void SetConsoleArguments(this HostBuilderContext context, ConsoleArguments args)
        {
            context.Properties["ConsoleArguments"] = args;
        }
        public static IDataProtectionStoreService GetDataProtectionStoreService(this HostBuilderContext context)
        {
            if (context.Properties.ContainsKey("ApplicationStorageService"))
            {
                return context.Properties["ApplicationStorageService"] as IDataProtectionStoreService;
            }
            return null;
        }
    }
    
    //  public class ServiceProxy
    public class FabricHostBuilder : HostBuilder
    {

        
        public FabricHostBuilder(string[] arguments, bool useServiceFactoryForwarding = true)
        {
            var culture = new CultureInfo("en-Us"); // replace en-US with the selected culture or string from the combobox
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            UseServiceProviderFactory(useServiceFactoryForwarding ?
                new MyAutofacServiceProviderFactory() :
                new AutofacServiceProviderFactory()
                as IServiceProviderFactory<ContainerBuilder>
            );

            var args = new ConsoleArguments(arguments);


            ConfigureAppConfiguration((context, builder) =>
            {
                context.SetConsoleArguments(args);

                if (args.IsServiceFabric)
                    context.Properties["ApplicationStorageService"] =
                        ServiceProxy.Create<IDataProtectionStoreService>(new Uri($"fabric:/{context.Configuration.GetValue<string>("DataProtectionSettings:GatewayApplicationName", "S-Innovations.ServiceFabric.GatewayApplication")}/ApplicationStorageService"), listenerName: "V2_1Listener");

            });

            ConfigureServices((context, services) =>
            {
                 
                services.AddSingleton(args);

             


                if (args.IsServiceFabric)
                {
                    services.AddSingleton(c => FabricRuntime.Create());
                    services.AddSingleton<ICodePackageActivationContext>(c => FabricRuntime.GetActivationContext());
                    services.AddSingleton(c => c.GetService<ICodePackageActivationContext>().GetConfigurationPackageObject("config"));
                }

                services.AddSingleton(c => new FabricClient());

                if (useServiceFactoryForwarding)
                    services.AddSingleton<IServiceProviderFactory<IServiceCollection>, ForwardingServiceProviderFactory>();

                services.AddSingleton<IConfigurationBuilder>(new ConfigurationBuilder());
                services.AddSingleton(c =>
                {
                    var extensions = c.GetService<IEnumerable<IConfigurationBuilderExtension>>();
                    var cbuilder = c.GetService<IConfigurationBuilder>();
                    cbuilder.AddConfiguration(context.Configuration);
                    foreach (var extension in extensions)
                    {
                        cbuilder = extension.Extend(cbuilder);
                    }
                    return cbuilder.Build();

                });
                services.AddSingleton<IConfiguration>(c => c.GetRequiredService<IConfigurationRoot>());
            });
            ConfigureContainer<ContainerBuilder>((context, builder) =>
             {

                 //builder.Register(c => FabricRuntime.Create()).AsSelf().SingleInstance();
                 //builder.Register(c => FabricRuntime.GetActivationContext()).As<ICodePackageActivationContext>().SingleInstance();
                 //builder.Register(c => c.Resolve<ICodePackageActivationContext>().GetConfigurationPackageObject("config")).As<ConfigurationPackage>().SingleInstance();
                 //builder.Register(c => new FabricClient()).As<FabricClient>().SingleInstance();

                 //builder.RegisterInstance(new ConfigurationBuilder()).As<IConfigurationBuilder>();
                 //builder.Register(c =>
                 //{
                 //    var extensions = c.Resolve<IEnumerable<IConfigurationBuilderExtension>>();
                 //    var cbuilder = c.Resolve<IConfigurationBuilder>();
                 //    cbuilder.AddConfiguration(context.Configuration);
                 //    foreach (var extension in extensions)
                 //    {
                 //        cbuilder = extension.Extend(cbuilder);
                 //    }
                 //    return cbuilder.Build();

                 //}).AsSelf().As<IConfiguration>().SingleInstance();

                 builder.RegisterInstance(new OptionRegistration { ServiceType = typeof(IHostedService), ServiceLifetime = ServiceLifetime.Singleton, ShouldIgnore = true });

             });

            ConfigureHostConfiguration((configurationBuilder) =>
            {

                configurationBuilder
                    .AddCommandLine(arguments)
                    .AddInMemoryCollection(new[] { new KeyValuePair<string, string>(HostDefaults.EnvironmentKey, System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")) })
                    .AddEnvironmentVariables();

                if (args.IsServiceFabric)
                {
                    configurationBuilder.AddServiceFabricConfig("Config");
                }

            });


        }






    }


}
