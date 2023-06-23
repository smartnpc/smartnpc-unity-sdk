using System;
using UnityEngine;

namespace SmartNPC
{
    [Serializable]
    public class SmartNPCBehavior
    {
        public string type;

        public SmartNPCBehavior(RawBehavior rawBehavior)
        {
            type = rawBehavior.type;
        }

        public static SmartNPCBehavior parse(RawBehavior rawBehavior)
        {
            try
            {
                if (rawBehavior.type == SmartNPCBehaviorType.Action) return new SmartNPCAction(rawBehavior);
                if (rawBehavior.type == SmartNPCBehaviorType.Gesture) return new SmartNPCGesture(rawBehavior);
                if (rawBehavior.type == SmartNPCBehaviorType.Expression) return new SmartNPCExpression(rawBehavior);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogError("Failed to parse behavior: " + rawBehavior.type + " - " + String.Join(", ", rawBehavior.args));
            }
            

            return null;
        }
    }
}