using System;

namespace SharedLib
{
    public class CommandMessage
    {
        // Command message template (sent from gateway to device(s))
        public string Command { get; set; }
    }

    public class StatusMessage
    {
        // Status message template (sent from device(s) to gateway)
        public string StatusText { get; set; }
    }

    public class ControllerStatusMessage
    {
        // Status message template (sent from gateway to controller)
        public string StatusText { get; set; }
    }

    public class ControllerCommandMessage
    {
        // Command message template - (sent from controller to gateway)
        public string Command { get; set; }
    }
}
