using System;
using UnityEngine;

namespace SmartNPC
{
    [Serializable]
    public class VoiceMessage
    {
        public AudioClip clip;
        public MessageResponse rawResponse;
    }
}