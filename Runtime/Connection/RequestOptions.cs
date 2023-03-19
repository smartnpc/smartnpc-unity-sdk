using System;
using Proyecto26;

namespace SmartNPC
{
    public class RequestOptions<T> : RequestBaseOptions {
        public Action<T> OnSuccess;
    }
}
