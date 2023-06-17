using System;

namespace SmartNPC
{
    [Serializable]
    public class MessageData : EmitData
    {
        public string character;
        public string message;
        public bool voice;
        public bool behaviors;
    }
}
