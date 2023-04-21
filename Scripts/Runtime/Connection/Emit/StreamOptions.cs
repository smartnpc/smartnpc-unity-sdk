using System;

namespace SmartNPC
{
    public class StreamOptions<T> : BaseEmitOptions
    {
        public Action<T> OnStart;
        public Action<T> OnProgress;
        public Action<T> OnComplete;
    }
}
