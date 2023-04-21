using System;

namespace SmartNPC
{
    [Serializable]
    public class SmartNPCMessage
    {
        public string message;
        // voice?: SmartNPCVoiceQueue;
        public string response;
        public string chunk;
        public string exception;
    }
}