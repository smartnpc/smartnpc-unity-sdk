using System;

namespace SmartNPC
{
    [Serializable]
    public class SpeechResponse
    {
        // no emitId on purpose
        public string status;
        public string text;
        public string exception;
    }
}
