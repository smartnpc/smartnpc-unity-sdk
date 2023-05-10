using System;
using UnityEngine;

namespace SmartNPC
{
    [Serializable]
    public class SmartNPCMessage
    {
        public string message;
        public string response;
        public string chunk;
        public string exception;
        public AudioSource voice;
    }
}