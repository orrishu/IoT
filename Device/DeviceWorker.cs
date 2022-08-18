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
        private bool _faucetOpen = false;
        private int _humidity = 0;
        private const int Min_Humidity_Treshold = 0;    // note: 80% will be the min threshold on the controller
        private const int Max_Humidity_Treshold = 100;

        public DeviceWorker(IBus bus, ILogger<DeviceWorker> logger)
        {
            _bus = bus;
            _logger = logger;
            // Each device publishes to same queue;
            _queue = new Queue("DeviceStatusQueue");
            _queue.Arguments.Add("x-max-priority", 10);
            // Each device should subscribe to exchange with its own topic.
            _exchange = new Exchange("CommandExchange");    //need to create that manually and bind queue to it manually so it will work
            /* test commit */
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Hello");
            
            // Each device should subscribe to exchange with its own topic... : 
            using var subscription = await _bus.PubSub.SubscribeAsync<GatewayCommandMessage>(
                    EasyNetQHelper.GetSubscription<GatewayCommandMessage, DeviceWorker>(),
                    HandleCommand,
                    options => options.WithTopic(_device_id).WithQueueName("GatewayCommandQueue"),
                    stoppingToken);

            //loop endlessly to check the faucet status, send status to controller and hydrate\dehydrate accordingly
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_faucetOpen)
                {
                    _logger.LogInformation("Faucet open, hydrating...");
                    //send the current status:
                    await SendStatusMessage(stoppingToken);
                    //hydrade:
                    await HydrateHumidity(stoppingToken);
                }
                else
                {
                    _logger.LogInformation("Faucet closed, dehydrating...");
                    //send the current status:
                    await SendStatusMessage(stoppingToken);
                    //dehydrate:
                    await DehydrateHumidity(stoppingToken);
                }
                await Task.Delay(1000, stoppingToken);
            }

            //wait until service is stopped
            //this will allow task to run even if GC will dispose;
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            stoppingToken.Register(tcs.SetResult);

            await tcs.Task;
        }

        private async Task SendStatusMessage(CancellationToken stoppingToken)
        {
            var body = new DeviceStatusMessage
            {
                DeviceId = _device_id,
                Humidity = _humidity,
                IsFaucetOpen = _faucetOpen
            };

            try
            {
                //use the simple config:
                await _bus.SendReceive.SendAsync(_queue.Name, body, cancellationToken: stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError("Error in status message publish: {error}", e.Message);
            }
        }

        private async Task HydrateHumidity(CancellationToken stoppingToken)
        {
            int count = 0;
            while (!stoppingToken.IsCancellationRequested && count++ < 100 && _faucetOpen)
            {
                _logger.LogInformation($"HydrateHumidity: current humidity is {_humidity}");
                _humidity++;
                if (_humidity >= Max_Humidity_Treshold) break;
                // note: if the controller (or gateway) will not be accessible,
                // the humidity will keep rising.
                // consider adding a fail-safe op:
                // close faucet if the humidity sensor reaches some other max treshold, e.g. 120%.

                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task DehydrateHumidity(CancellationToken stoppingToken)
        {
            int count = 100;
            while (!stoppingToken.IsCancellationRequested && count-- > 0 && !_faucetOpen)
            {
                _logger.LogInformation($"DehydrateHumidity: current humidity is {_humidity}");
                _humidity--;
                if (_humidity <= Min_Humidity_Treshold)
                {
                    _humidity = 0;  // in real life case, humidity cannot drop below zero. need to init in this case to simulate the real life scenario
                    break;
                }
                else if (_humidity % 10 == 0)
                {
                    //as a precaution, send the current status to controller on every 10% drop.
                    //this will assure that controller wil know the status when dehydration is in process,
                    //and allow to send command when its treshold is reached:
                    await SendStatusMessage(stoppingToken);
                }

                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task HandleCommand(GatewayCommandMessage commandMessage, CancellationToken cancellationToken)
        {
            //when command message is received from the gateway, it will be handled here. 
            _logger.LogInformation("CommandMessage received: {Message}", commandMessage.Command);
            await Task.Delay(1000, cancellationToken);

            if (commandMessage.ShouldOpenFaucet && !_faucetOpen)
            {
                _faucetOpen = true;
            }
            else if (!commandMessage.ShouldOpenFaucet && _faucetOpen)
            {
                _faucetOpen = false;
            }
        }
    }
}