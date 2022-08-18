using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;

namespace Device
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<DeviceWorker>();
                    services.RegisterEasyNetQ("host=localhost");    // for docker: host.docker.internal. move to config!
                })
            .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                            .ReadFrom.Configuration(hostingContext.Configuration));
    }
}
