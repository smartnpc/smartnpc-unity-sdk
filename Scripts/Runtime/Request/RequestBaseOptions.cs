using System;
using System.Collections.Generic;
using Proyecto26;

namespace SmartNPC
{
    public class RequestBaseOptions {
        public string Uri;
        public string Method;
        public Dictionary<string, string> Params;
        public object Body;
        public Action<RequestException> OnError;
    }
}
