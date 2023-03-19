using System;

namespace SmartNPC
{
    public class StreamDownloadHandlerOptions {
        public Action<string, string> OnProgress;
        public Action<string> OnComplete;
    }
}
