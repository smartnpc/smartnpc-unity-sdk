using System;

namespace SmartNPC
{
    [Serializable]
    public class SmartNPCGesture : SmartNPCBehavior
    {
        public string name;

        public SmartNPCGesture(RawBehavior rawBehavior) : base(rawBehavior)
        {
            name = rawBehavior.args[0];
        }
    }
}