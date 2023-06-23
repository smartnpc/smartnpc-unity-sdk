using System;
using System.Collections.Generic;

namespace SmartNPC
{
    [Serializable]
    public class RawBehavior
    {
        public string type;
        public List<string> args;
    }
}