using System;

namespace SharedLib
{
    public class MessageTemplate
    {
        public int MessageId { get; set; }
        public string MessageSubject { get; set; }
    }

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
}
