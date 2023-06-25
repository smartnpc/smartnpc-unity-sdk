using System;
using UnityEngine;

namespace SmartNPC
{
    [Serializable]
    public class SmartNPCGestureAnimation
    {
        [SerializeField] public string gestureName;
        [SerializeField]
        [Tooltip("Existing trigger name of an animation. Not needed if you specific an animation clip.")]
        public string animationTrigger;
        [SerializeField]
        [Tooltip("Auto create state, transitions, and trigger from animation clip.")]
        public AnimationClip animationClip;
    }
}
