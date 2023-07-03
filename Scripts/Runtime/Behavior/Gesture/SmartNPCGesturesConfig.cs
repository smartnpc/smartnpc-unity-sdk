using System.Collections.Generic;
using UnityEngine;

namespace SmartNPC
{
    [CreateAssetMenu(fileName = "Gestures Config", menuName = "SmartNPC/Gestures Configuration", order = 3)]
    public class SmartNPCGesturesConfig : ScriptableObject
    {
        [SerializeField] public bool TriggerGestures = true;
        [SerializeField] public List<SmartNPCGestureItem> Gestures = new List<SmartNPCGestureItem>();
    }
}
