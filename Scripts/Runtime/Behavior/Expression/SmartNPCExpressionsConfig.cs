using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartNPC
{
    [CreateAssetMenu(fileName = "Expressions Config", menuName = "SmartNPC/Expressions Configuration", order = 0)]
    public class SmartNPCExpressionsConfig : ScriptableObject
    {
        [SerializeField] [Range(0, 1)] public float interpolationSpeed = 0.2f;
        [SerializeField] public List<SmartNPCExpressionItem> expressions = new List<SmartNPCExpressionItem>();
    }
}
