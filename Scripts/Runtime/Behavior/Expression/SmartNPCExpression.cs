using System;

namespace SmartNPC
{
    [Serializable]
    public class SmartNPCExpression : SmartNPCBehavior
    {
        public string previous;
        public string current;
        public string next;

        public SmartNPCExpression(RawBehavior rawBehavior) : base(rawBehavior)
        {
            previous = rawBehavior.args[0];
            current = rawBehavior.args[1];
            next = rawBehavior.args[2];
        }
    }
}