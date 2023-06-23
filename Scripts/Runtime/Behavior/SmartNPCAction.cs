using System;

namespace SmartNPC
{
    [Serializable]
    public class SmartNPCAction : SmartNPCBehavior
    {
        public string name;
        public string target;

        public SmartNPCAction(RawBehavior rawBehavior) : base(rawBehavior)
        {
            name = rawBehavior.args[0];
            
            if (rawBehavior.args.Count > 1) target = rawBehavior.args[1];
        }
    }
}