using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SSP_assignment.Services;
using SSP_assignment.Interfaces;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Register services with DI container
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IImageFetcherService, ImageFetcherService>();
        // Other services can be added here as needed
    })
    .Build();

host.Run();
