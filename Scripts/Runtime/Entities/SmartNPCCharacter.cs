using System;
using UnityEngine;

namespace SmartNPC
{
    public class SmartNPCCharacter : MonoBehaviour
    {
        [SerializeField]
        private string _characterId;

        private SmartNPCConnection connection;

        public SmartNPCChat Chat { get; private set; }

        void Start()
        {
            if (_characterId == "") throw new Exception("Must specify Character Id");

            connection = FindObjectOfType<SmartNPCConnection>();

            if (connection == null) throw new Exception("Must place a connection anywhere in the scene");

            SmartNPCPlayer player = FindObjectOfType<SmartNPCPlayer>();

            if (player == null) throw new Exception("Must place a player anywhere in the scene");

            Chat = new SmartNPCChat(connection, _characterId, player);
        }

        public void GetInfo(RequestCallbacks<SmartNPCCharacterInfo> callbacks) {
            connection.Request<SmartNPCCharacterInfo>(new RequestOptions<SmartNPCCharacterInfo> {
                Method = "GET",
                Uri = "project/" + connection.Project + "/character/" + _characterId,
                OnSuccess = callbacks.OnSuccess,
                OnError = callbacks.OnError
            });
        }
    }
}
