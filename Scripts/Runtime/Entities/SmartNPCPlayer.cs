using System;
using UnityEngine;

namespace SmartNPC
{
    public class SmartNPCPlayer : MonoBehaviour
    {
        [SerializeField]
        private string _playerId;

        [SerializeField]
        private string _playerName;

        public string PlayerId {
            get {
                return _playerId;
            }
        }

        public string PlayerName {
            get {
                return _playerName;
            }
        }

        void Start()
        {
            if (_playerId == "") throw new Exception("Must specify Player Id");
        }
    }
}
