using System;

namespace SmartNPC
{
    [Serializable]
    public class MessageRequestBody {
        public string message;
        public string playerIdInClient;
        public string playerName;
    }
}
