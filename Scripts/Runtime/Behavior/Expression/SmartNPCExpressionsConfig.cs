using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartNPC
{
    [CreateAssetMenu(fileName = "Expressions Config", menuName = "SmartNPC/Expressions Configuration", order = 2)]
    public class SmartNPCExpressionsConfig : ScriptableObject
    {
        [SerializeField] [Range(0, 1)] public float InterpolationSpeed = 0.2f;
        [SerializeField] public List<SmartNPCExpressionItem> Expressions = new List<SmartNPCExpressionItem>();
    }
}
