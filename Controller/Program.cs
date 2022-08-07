using Controller;
using Serilog;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<ControllerWorker>();
        services.RegisterEasyNetQ("host=localhost");
    })
    .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                            .ReadFrom.Configuration(hostingContext.Configuration))
    .Build();

await host.RunAsync();
