#DotNetDevOps.ServiceFabric.Hosting

[![Build status](https://sinnovations.visualstudio.com/DotNetDevOps/_apis/build/status/ServiceFabricGateway/ServiceFabric%20Hosting)](https://sinnovations.visualstudio.com/DotNetDevOps/_build/latest?definitionId=0)

The library extends the HostBuilder with child containers for each seperate service.

```cs
    var host = new FabricHostBuilder(args)
    //Add fabric configuration provider  (or use .ConfigureDefaultAppConfiguration())
    .ConfigureAppConfiguration((context, configurationBuilder) =>
    {
        configurationBuilder
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();

        if (args.Contains("--serviceFabric"))
        {
            configurationBuilder.AddServiceFabricConfig("Config");
        }
    })
    //Setup services that exists on root, shared in all services
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<IConfigureOptions<OidcClientOptions>, OidcClientOptionsConfigure>();   

        services.WithKestrelHosting<Startup>(Constants.PortalServiceType, Factory);


        services.AddScoped(sp => ServiceProxy.Create<IApplicationStorageService>(new Uri($"fabric:/{sp.GetService<IOptions<IOBoardServiceOptions>>().Value.GatewayApplicationName}/ApplicationStorageService"), listenerName: "V2_1Listener"));
        services.AddScoped(sp => ServiceProxy.Create<IResourceProviderService>(new Uri($"fabric:/{sp.GetService<IOptions<IOBoardServiceOptions>>().Value.GatewayApplicationName}/ResourceProviderService"), listenerName: "V2_1Listener"));
        services.AddScoped(sp => ServiceProxy.Create<IKeyVaultService>(new Uri($"fabric:/{sp.GetService<IOptions<IOBoardServiceOptions>>().Value.GatewayApplicationName}/KeyVaultService"), listenerName: "V2_1Listener"));
        services.AddScoped(sp => ServiceProxy.Create<IAzureADTokenService>(new Uri($"fabric:/{sp.GetService<IOptions<IOBoardServiceOptions>>().Value.GatewayApplicationName}/KeyVaultService"), listenerName: "V2_1Listener"));
 
        services.AddSingleton(new CDNHelper<Startup>("https://cdn.io-board.com/libs", "io-board-portal"));

    })
    //Setup logging
    .ConfigureSerilogging((context, logConfig) =>
                logConfig.MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.File($"trace.log", buffered: true, flushToDiskInterval: TimeSpan.FromSeconds(30), rollOnFileSizeLimit: true, fileSizeLimitBytes: 1024 * 1024 * 32, rollingInterval: RollingInterval.Hour)
                .WriteTo.LiterateConsole(outputTemplate: Constants.LiterateLogTemplate))
    .ConfigureApplicationInsights()
    .Configure<EndpointsOptions<Startup>>("Endpoints")
    .Configure<OidcClientOptions>("OidcClient")
    .Configure<IOBoardServiceOptions>("HostSettings");

	await host.RunConsoleAsync();
				
```