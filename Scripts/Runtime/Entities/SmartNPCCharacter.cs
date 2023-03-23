using System;
using System.Collections.Generic;
using UnityEngine;
using Proyecto26;

namespace SmartNPC
{
    public class SmartNPCCharacter : MonoBehaviour
    {
        [SerializeField]
        private string _characterId;

        private SmartNPCConnection _connection;

        private SmartNPCChat _chat = new SmartNPCChat();

        private SmartNPCPlayer _player;
        private SmartNPCCharacterInfo _info;

        private List<Action> OnReadyListeners = new List<Action>();

        private List<Action<RequestException>> OnErrorListeners = new List<Action<RequestException>>();

        void Start()
        {
            if (_characterId == "") throw new Exception("Must specify Character Id");

            _connection = FindObjectOfType<SmartNPCConnection>();

            if (_connection == null) throw new Exception("Must place a connection anywhere in the scene");

            _player = FindObjectOfType<SmartNPCPlayer>();

            if (_player == null) throw new Exception("Must place a player anywhere in the scene");

            _chat.OnReady(OnChatReady);
            _chat.OnMessageHistoryError(DispatchError);

            _connection.OnReady(OnConnectionReady);
            _connection.OnError(DispatchError);
        }

        private void OnChatReady(List<SmartNPCMessage> messages) {
            if (_info != null) OnReadyListeners.ForEach((Action listener) => listener());
        }

        private void OnConnectionReady() {
            GetInfo();

            _chat.Init(_connection, _characterId, _player);
        }

        private void DispatchError(RequestException error) {
            OnErrorListeners.ForEach((Action<RequestException> listener) => listener(error));
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

        public void OnError(Action<RequestException> listener)
        {
            OnErrorListeners.Add(listener);
        }

        public void OffReady(Action listener)
        {
            OnReadyListeners.Remove(listener);
        }

        public void OffError(Action<RequestException> listener)
        {
            OnErrorListeners.Add(listener);
        }

        public void RemoveAllListeners()
        {
            OnReadyListeners.Clear();
            OnErrorListeners.Clear();

            _chat.OffReady(OnChatReady);
            _chat.OffMessageHistoryError(DispatchError);

            _connection.OffReady(OnConnectionReady);
            _connection.OffError(DispatchError);
        }

        public bool IsReady {
            get {
                return _info != null && _chat.IsReady;
            }
        }

        private void GetInfo(RequestCallbacks<SmartNPCCharacterInfo> callbacks = null) {
            _connection.Request<SmartNPCCharacterInfo>(new RequestOptions<SmartNPCCharacterInfo> {
                Method = "GET",
                Uri = "project/" + _connection.Project + "/character/" + _characterId,
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
