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
    class Worker : BackgroundService
    {
        private IBus _bus;
        private readonly ILogger<Worker> _logger;
        private readonly Queue _queue;
        //private readonly Exchange _exchange;

        public Worker(IBus bus, ILogger<Worker> logger)
        {
            _bus = bus;
            _logger = logger;
            // The gateway should subscribe to the status queue.
            _queue = new Queue("StatusQueue");
            _queue.Arguments.Add("x-max-priority", 10);
            // The gateway should publish to each device by its topic.
            //_exchange = new Exchange("RADIO");    //need to create that manually and bind queue to it manually so it will work
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // The gateway should subscribe to the status queue... :
            //subscribe to queue. this will also create it if it does not exist
            var sub = await _bus.SendReceive.ReceiveAsync<StatusMessage>(_queue.Name, OnStatusMessageReceived, x => { }, cancellationToken: stoppingToken);
            //need to register to the dispose of the subscription.
            stoppingToken.Register(sub.Dispose);

            int count = 10;
            while (!stoppingToken.IsCancellationRequested && count-- > 0)
            {
                //The gateway should publish to each device by its topic ... :
                // Todo: check how device ids will be known to the gateway.
                // also... check with Amir, it somehow works although I forgot to add the exchange here :)
                await _bus.PubSub.PublishAsync(new CommandMessage { 
                    Command = $"New command from gateway: {count}" 
                }, "Device_1", cancellationToken: stoppingToken);  // 'Device_1' is hard coded from the only device on POC

                await Task.Delay(1000, stoppingToken);
            }
        }

        private Task OnStatusMessageReceived(StatusMessage statusMessage, CancellationToken cancellationToken)
        {
            // When a status message is received from the device, it will be handled here.
            _logger.LogInformation("Status message received: {Message}", statusMessage.StatusText);
            return Task.CompletedTask;
        }
    }
}
