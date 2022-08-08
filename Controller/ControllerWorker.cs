using Controller.Models;
using EasyNetQ;
using EasyNetQ.Topology;
using SharedLib;

namespace Controller
{
    public class ControllerWorker : BackgroundService
    {
        private readonly ILogger<ControllerWorker> _logger;
        private readonly IBus _bus;
        private readonly Queue _queue;

        public ControllerWorker(ILogger<ControllerWorker> logger, IBus bus)
        {
            _logger = logger;
            _bus = bus;
            // Controller publishes to command queue - for gateway;
            _queue = new Queue("ControllerCommandQueue");
            _queue.Arguments.Add("x-max-priority", 10);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Controller subscribes to gateway status messages ... :
            using var subscription = await _bus.PubSub.SubscribeAsync<GatewayStatusMessage>(
                    EasyNetQHelper.GetSubscription<GatewayStatusMessage, ControllerWorker>(),
                    HandleStatusMessage,
                    options => options.WithQueueName("ContollerStatusQueue"),
                    stoppingToken);

            // Controller publishes to command queue - for gateway ... :
            int count = 10;
            while (!stoppingToken.IsCancellationRequested && count-- > 0)
            {
                var body = new ControllerCommandMessage
                {
                    Command = $"New command from controller: {count}"
                };
                //use the simple config:
                await _bus.SendReceive.SendAsync(_queue.Name, body, cancellationToken: stoppingToken);
             
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task HandleStatusMessage(GatewayStatusMessage statusMessage, CancellationToken cancellationToken)
        {
            //when command message is received from the gateway, it will be handled here. 
            _logger.LogInformation("CommandMessage received: {Message}", statusMessage.StatusText);
            await Task.Delay(1000, cancellationToken);
            //return Task.CompletedTask;    //?
        }
    }
}