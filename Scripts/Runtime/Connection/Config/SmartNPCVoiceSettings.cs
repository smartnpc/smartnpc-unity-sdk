using System;
using UnityEngine;

namespace SmartNPC
{
    [Serializable]
    public class SmartNPCVoiceSettings
    {
        [SerializeField] public bool Enabled = true;
        [SerializeField] [Range(0.0f, 1.0f)] public float Volume = 1;
        [SerializeField] public int MaxDistance = 20;
    }
}
