using System;

namespace SmartNPC
{
    [Serializable]
    public class EmitError : EmitData
    {
        public string status;
        public string message;
    }
}
