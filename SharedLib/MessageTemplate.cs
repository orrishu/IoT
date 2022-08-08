using System;

namespace SharedLib
{
    public class DeviceStatusMessage
    {
        // Status message template (sent from device(s) to gateway)
        public string DeviceId { get; set; }
        public int Humidity { get; set; }
        public bool IsFaucetOpen { get; set; }
        public string StatusText { get; set; }
    }

    public class GatewayStatusMessage
    {
        // Status message template (sent from gateway to controller)
        public string DeviceId { get; set; }
        public int Humidity { get; set; }
        public bool IsFaucetOpen { get; set; }
        public string StatusText { get; set; }
    }

    public class ControllerCommandMessage
    {
        // Command message template - (sent from controller to gateway)
        public string ToDeviceId { get; set; }
        public bool ShouldOpenFaucet { get; set; }
        public string Command { get; set; }
    }

    public class GatewayCommandMessage
    {
        // Command message template (sent from gateway to device(s))
        public string ToDeviceId { get; set; }
        public bool ShouldOpenFaucet { get; set; }
        public string Command { get; set; }
    }
}
