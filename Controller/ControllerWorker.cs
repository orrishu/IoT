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
        private const int Min_Humidity_Treshold = 80;
        private const int Max_Humidity_Treshold = 100;

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

            //wait until service is stopped
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            stoppingToken.Register(tcs.SetResult);

            await tcs.Task;
        }

        private async Task HandleStatusMessage(GatewayStatusMessage statusMessage, CancellationToken cancellationToken)
        {
            //when command message is received from the gateway, it will be handled here. 
            _logger.LogInformation("CommandMessage received: {id}, {humidity}%, faucet open: {faucet}", 
                statusMessage.DeviceId, statusMessage.Humidity, statusMessage.IsFaucetOpen);

            if (!statusMessage.IsFaucetOpen && statusMessage.Humidity <= Min_Humidity_Treshold)
            {
                // Controller publishes to command queue - for gateway ... :
                var body = new ControllerCommandMessage
                {
                    Command = $"New command from controller: open the faucet...", 
                    ShouldOpenFaucet = true, 
                    ToDeviceId = statusMessage.DeviceId
                };
                //use the simple config:
                await _bus.SendReceive.SendAsync(_queue.Name, body, cancellationToken: cancellationToken);

                await Task.Delay(100, cancellationToken);
            }
            else if (statusMessage.IsFaucetOpen && statusMessage.Humidity >= Max_Humidity_Treshold)
            {
                var body = new ControllerCommandMessage
                {
                    Command = $"New command from controller: close the faucet!",
                    ShouldOpenFaucet = false,
                    ToDeviceId = statusMessage.DeviceId
                };
                //use the simple config:
                await _bus.SendReceive.SendAsync(_queue.Name, body, cancellationToken: cancellationToken);

                await Task.Delay(100, cancellationToken);
            }
            await Task.Delay(1000, cancellationToken);
            //return Task.CompletedTask;    //?
        }
    }
}