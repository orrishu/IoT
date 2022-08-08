using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;
using SharedLib;
using EasyNetQ.Topology;
using Microsoft.Extensions.Logging;

namespace Publisher
{
    class GatewayWorker : BackgroundService
    {
        private IBus _bus;
        private readonly ILogger<GatewayWorker> _logger;
        private readonly Queue _statusQueue;
        private readonly Queue _commandQueue;
        //private readonly Exchange _exchange;

        public GatewayWorker(IBus bus, ILogger<GatewayWorker> logger)
        {
            _bus = bus;
            _logger = logger;
            // The gateway should subscribe to the status queue.
            _statusQueue = new Queue("DeviceStatusQueue");
            _statusQueue.Arguments.Add("x-max-priority", 10);
            // The gateway should subscribe also to the controller command queue.
            _commandQueue = new Queue("ControllerCommandQueue");
            _commandQueue.Arguments.Add("x-max-priority", 10);

            // The gateway should publish to each device by its topic.
            //_exchange = new Exchange("RADIO");    //need to create that manually and bind queue to it manually so it will work
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // The gateway should subscribe to the status queue... :
            //subscribe to status queue. this will also create it if it does not exist
            var sub = await _bus.SendReceive.ReceiveAsync<DeviceStatusMessage>(_statusQueue.Name, OnStatusMessageReceived, x => { }, cancellationToken: stoppingToken);
            //need to register to the dispose of the subscription.
            stoppingToken.Register(sub.Dispose);

            //subscribe to command queue. this will also create it if it does not exist
            var sub2 = await _bus.SendReceive.ReceiveAsync<ControllerCommandMessage>(_commandQueue.Name, OnCommandMessageReceived, x => { }, cancellationToken: stoppingToken);
            //need to register to the dispose of the subscription.
            stoppingToken.Register(sub2.Dispose);
            
        }

        private async Task OnCommandMessageReceived(ControllerCommandMessage commandMessage, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Controller command message received: {Message}", commandMessage.Command);
            await _bus.PubSub.PublishAsync(new GatewayCommandMessage
            {
                Command = $"New command from gateway: {commandMessage.Command}"
            }, "Device_1", cancellationToken: cancellationToken);  // 'Device_1' is hard coded from the only device on POC
        }

        private async Task OnStatusMessageReceived(DeviceStatusMessage statusMessage, CancellationToken cancellationToken)
        {
            // When a status message is received from the device, it will be handled here.
            _logger.LogInformation("Status message received: {Message}", statusMessage.StatusText);
            //send it to the controller:
            var body = new GatewayStatusMessage
            {
                StatusText = $"Status from gateway: Hello controller"
            };
            await _bus.PubSub.PublishAsync(body, cancellationToken: cancellationToken);
            //return Task.CompletedTask;
        }
    }
}
