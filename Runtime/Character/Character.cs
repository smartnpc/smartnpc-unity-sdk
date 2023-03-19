using System;
using UnityEngine;
using UnityEditor;
using Proyecto26;

namespace SmartNPC
{
    public class Character : MonoBehaviour
    {
        [SerializeField]
        private string _characterId;

        private Connection connection;

        public Chat Chat { get; private set; }

        void Start()
        {
            if (_characterId == "") throw new Exception("Must specify Character Id");

            connection = FindObjectOfType<Connection>();

            if (connection == null) throw new Exception("Must place a connection anywhere in the scene");

            Player player = FindObjectOfType<Player>();

            if (player == null) throw new Exception("Must place a player anywhere in the scene");

            Chat = new Chat(connection, _characterId, player);
        }

        public void GetInfo(RequestCallbacks<CharacterInfo> callbacks) {
            connection.Request<CharacterInfo>(new RequestOptions<CharacterInfo> {
                Method = "GET",
                Uri = "project/" + connection.Project + "/character/" + _characterId,
                OnSuccess = callbacks.OnSuccess,
                OnError = callbacks.OnError
            });
        }
    }
}
