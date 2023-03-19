using System;
using System.Collections.Generic;
using Proyecto26;

namespace SmartNPC
{
    public class RequestCallbacks<T> {
        public Action<T> OnSuccess;
        public Action<RequestException> OnError;
    }
}
