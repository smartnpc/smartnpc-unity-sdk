using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartNPC
{
    [Serializable]
    public class RawHistoryMessage
    {
        public string message;
        public string response;
        public List<RawBehavior> behaviors;
    }
}
