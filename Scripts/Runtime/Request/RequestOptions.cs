using System;

namespace SmartNPC
{
    public class RequestOptions<T> : RequestBaseOptions {
        public Action<T> OnSuccess;
    }
}
