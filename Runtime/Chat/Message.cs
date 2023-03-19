using System;
using Proyecto26;

namespace SmartNPC
{
    [Serializable]
    public class Message {
        public string message;
        public string response;
        public string chunk;
        public RequestException error;
    }
}
