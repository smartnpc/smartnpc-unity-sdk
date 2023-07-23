using System;

namespace SmartNPC
{
    [Serializable]
    public class SmartNPCGesture : SmartNPCBehavior
    {
        public static readonly string Prefix = "SmartNPC";
        
        public string name;

        public SmartNPCGesture(RawBehavior rawBehavior) : base(rawBehavior)
        {
            name = rawBehavior.args[0];
        }
    }
}