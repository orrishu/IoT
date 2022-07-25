using EasyNetQ;
using EasyNetQ.Topology;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedLib;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Receiver
{
    internal class Worker : BackgroundService
    {
        private readonly IBus _bus;
        private readonly ILogger<Worker> _logger;
        private readonly Queue _queue;
        //private readonly Exchange _exchange;

        public Worker(IBus bus, ILogger<Worker> logger)
        {
            _bus = bus;
            _logger = logger;
            _queue = new Queue("ExampleQueue");
            _queue.Arguments.Add("x-max-priority", 10);
            //Each device should subscribe to exchange with its own topic.
            //_exchange = new Exchange("CommandQueue");    //need to create that manually and bind queue to it manually so it will work

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Hello");
            //subscribe to queue. this will also create it if it does not exist
            var sub = await _bus.SendReceive.ReceiveAsync<MessageTemplate>(_queue.Name, OnMessageReceived, x => { }, cancellationToken: stoppingToken);
            //need to register to the dispose of the subscription.
            stoppingToken.Register(sub.Dispose);
            //this will allow task to run even if GC will dispose;
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            stoppingToken.Register(tcs.SetResult);

            await tcs.Task;
        }

        private Task OnMessageReceived(MessageTemplate emailMessage, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Message received: {MessageId}", emailMessage.MessageId);
            return Task.CompletedTask;
        }
    }
}