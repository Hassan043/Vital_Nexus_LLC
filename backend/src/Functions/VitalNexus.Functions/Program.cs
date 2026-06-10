using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using VitalNexus.Application;
using VitalNexus.Infrastructure;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationServices();
        services.AddInfrastructureServices();
        services.AddLogging();
    })
    .Build();

host.Run();
