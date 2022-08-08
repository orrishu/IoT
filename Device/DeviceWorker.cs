using EasyNetQ;
using EasyNetQ.Topology;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedLib;
using System;
using System.Threading;
using System.Threading.Tasks;
using Device.Models;

namespace Device
{
    internal class DeviceWorker : BackgroundService
    {
        private readonly IBus _bus;
        private readonly ILogger<DeviceWorker> _logger;
        private readonly Queue _queue;
        private readonly Exchange _exchange;
        private readonly string _device_id = "Device_1";    // Todo: move to config.

        public DeviceWorker(IBus bus, ILogger<DeviceWorker> logger)
        {
            _bus = bus;
            _logger = logger;
            // Each device publishes to same queue;
            _queue = new Queue("DeviceStatusQueue");
            _queue.Arguments.Add("x-max-priority", 10);
            // Each device should subscribe to exchange with its own topic.
            _exchange = new Exchange("CommandExchange");    //need to create that manually and bind queue to it manually so it will work

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Hello");
            
            // Each device should subscribe to exchange with its own topic... : 
            using var subscription = await _bus.PubSub.SubscribeAsync<GatewayCommandMessage>(
                    EasyNetQHelper.GetSubscription<GatewayCommandMessage, DeviceWorker>(),
                    HandleCommand,
                    options => options.WithTopic(_device_id).WithQueueName("CommandQueue"),
                    stoppingToken);

            // Each device publishes to same queue ... :
            try
            {
                var body = new DeviceStatusMessage
                {
                    StatusText = $"Status from {_device_id}: Hello gateway"
                };
                //use the simple config:
                await _bus.SendReceive.SendAsync(_queue.Name, body, cancellationToken: stoppingToken);

                await Task.Delay(100, stoppingToken);
            }
            catch (Exception e)
            {

            }

            //await cancellationToken.AsTask();

            //this will allow task to run even if GC will dispose;
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            stoppingToken.Register(tcs.SetResult);

            await tcs.Task;
        }

        private async Task HandleCommand(GatewayCommandMessage commandMessage, CancellationToken cancellationToken)
        {
            //when command message is received from the gateway, it will be handled here. 
            _logger.LogInformation("CommandMessage received: {Message}", commandMessage.Command);
            await Task.Delay(1000, cancellationToken);
            //return Task.CompletedTask;    //?
        }
    }
}