using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartNPC
{
    public class SmartNPCCharacter : MonoBehaviour
    {
        [SerializeField]
        private string _characterId;

        private SmartNPCConnection connection;

        private SmartNPCChat _chat = new SmartNPCChat();

        private SmartNPCCharacterInfo _info;

        private List<Action> OnReadyListeners = new List<Action>();

        void Start()
        {
            if (_characterId == "") throw new Exception("Must specify Character Id");

            connection = FindObjectOfType<SmartNPCConnection>();

            if (connection == null) throw new Exception("Must place a connection anywhere in the scene");

            SmartNPCPlayer player = FindObjectOfType<SmartNPCPlayer>();

            if (player == null) throw new Exception("Must place a player anywhere in the scene");

            _chat.OnReady((List<SmartNPCMessage> messages) => {
                if (_info != null) OnReadyListeners.ForEach((Action listener) => listener());
            });

            connection.OnReady(() => {
                GetInfo();

                _chat.Init(connection, _characterId, player);
            });
        }

        public SmartNPCChat Chat {
            get {
                return _chat;
            }
        }

        public SmartNPCCharacterInfo Info {
            get {
                return _info;
            }
        }

        public void OnReady(Action listener)
        {
            OnReadyListeners.Add(listener);

            if (IsReady) listener();
        }

        public bool IsReady {
            get {
                return _info != null && _chat.IsReady;
            }
        }

        private void GetInfo(RequestCallbacks<SmartNPCCharacterInfo> callbacks = null) {
            connection.Request<SmartNPCCharacterInfo>(new RequestOptions<SmartNPCCharacterInfo> {
                Method = "GET",
                Uri = "project/" + connection.Project + "/character/" + _characterId,
                OnSuccess = (SmartNPCCharacterInfo info) => {
                    _info = info;

                    if (callbacks?.OnSuccess != null) callbacks?.OnSuccess(info);
                    
                    if (_chat.IsReady) OnReadyListeners.ForEach((Action listener) => listener());
                },
                OnError = callbacks?.OnError
            });
        }
    }
}
