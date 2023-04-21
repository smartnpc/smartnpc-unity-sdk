using System;

namespace SmartNPC
{
    public class FetchOptions<T> : BaseEmitOptions
    {
        public Action<T> OnSuccess;
    }
}
