using System;
using Proyecto26;

namespace SmartNPC
{
    public class RequestCallbacks<T> {
        public Action<T> OnSuccess;
        public Action<RequestException> OnError;
    }
}
