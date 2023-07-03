using System;
using UnityEngine;

namespace SmartNPC
{
    [Serializable]
    public class SmartNPCAdvancedSettings
    {
        public const string DEFAULT_HOST = "wss://api.smartnpc.ai";

        [SerializeField] public string Host;

        public string GetHost()
        {
            return Host != "" ? Host : DEFAULT_HOST;
        }
    }
}
