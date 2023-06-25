using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartNPC
{
    [Serializable]
    public class SmartNPCExpressionConfig
    {
        [SerializeField] public string expressionName;
        [SerializeField] public List<SmartNPCBlendShape> blendShapes;
    }
}
