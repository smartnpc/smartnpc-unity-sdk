using System;
using UnityEngine;

namespace SmartNPC
{
    [Serializable]
    public class SmartNPCBlendShape
    {
        [SerializeField] public string name;
        [SerializeField] [Range(0, 1)] public float weight = 0;
    }
}
