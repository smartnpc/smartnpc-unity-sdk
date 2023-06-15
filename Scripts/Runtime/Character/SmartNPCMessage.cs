using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartNPC
{
    [Serializable]
    public class SmartNPCMessage
    {
        public string message;
        public string response;
        public List<SmartNPCBehavior> behaviors;
        public AudioClip voiceClip;
        public string chunk;
        public string exception;
    }
}