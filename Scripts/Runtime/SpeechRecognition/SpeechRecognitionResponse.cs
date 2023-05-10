using System;

namespace SmartNPC
{
    [Serializable]
    public class SpeechRecognitionResponse
    {
        // no emitId on purpose
        public string status;
        public string text;
        public string exception;
    }
}
