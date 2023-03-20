using System;

namespace SmartNPC
{
    public class StreamOptions : RequestBaseOptions {
        public Action<string, string> OnProgress;
        public Action<string> OnComplete;
    }
}
