using System;
using UnityEngine;

namespace SmartNPC
{
    [Serializable]
    public class SmartNPCAPIKey
    {
        [SerializeField] public string KeyId;
        [SerializeField] public string PublicKey;

        public bool Validate()
        {
            return KeyId != null && KeyId != "" && PublicKey != null && PublicKey != "";
        }

        public object GetData()
        {
            return new {
                keyId = KeyId,
                publicKey = PublicKey
            };
        }
    }
}
