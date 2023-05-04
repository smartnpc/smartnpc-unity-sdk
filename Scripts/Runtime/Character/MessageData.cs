using System;

namespace SmartNPC
{
    [Serializable]
    public class MessageData : EmitData
    {
        public string character;
        public string text;
        public string speech;
        public bool voice;
    }
}
