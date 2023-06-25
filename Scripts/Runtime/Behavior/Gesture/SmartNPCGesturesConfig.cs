using System.Collections.Generic;
using UnityEngine;

namespace SmartNPC
{
    [CreateAssetMenu(fileName = "Gestures Config", menuName = "SmartNPC/Gestures Configuration", order = 1)]
    public class SmartNPCGesturesConfig : ScriptableObject
    {
        [SerializeField] public bool triggerGestures = true;
        [SerializeField] public List<SmartNPCGestureItem> gestures = new List<SmartNPCGestureItem>();
    }
}
