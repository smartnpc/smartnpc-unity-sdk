using System;
using UnityEngine;

namespace SmartNPC
{
    [Serializable]
    public class VoiceMessage
    {
        public AudioSource voice;
        public MessageResponse rawResponse;
    }
}