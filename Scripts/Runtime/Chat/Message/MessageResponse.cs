using System;

namespace SmartNPC
{
    [Serializable]
    public class MessageResponse : StreamResponse
    {
        public string text;
        public string voice;
        public string character;
        public RawBehavior behavior;
    }
}
