using System;
using System.Collections.Generic;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;

namespace SmartNPC
{
    public class SmartNPCConnection : BaseEmitter
    {
        [Header("Credentials")]

        [SerializeField] private string _keyId;

        [SerializeField] private string _publicKey;

        [Header("Player")]

        [SerializeField] private string _playerId;

        [SerializeField] private string _playerName;

        [Header("Voice")]
        [SerializeField] private bool _voiceEnabled = true;
        [SerializeField] [Range(0.0f, 1.0f)] private float _voiceVolume = 1;

        [Header("Advanced Settings")]

        [SerializeField] private string _host;

        private const string DEFAULT_HOST = "wss://api.smartnpc.ai";
        private Dictionary<string, List<Action<SocketIOResponse>>> socketListeners = new Dictionary<string, List<Action<SocketIOResponse>>>();
        private SocketIOUnity socket;

        void Awake()
        {
            if (_keyId == null|| _keyId == "" || _publicKey == null || _publicKey == "")
            {
                throw new Exception("Must specify Key Id and Public Key");
            }

            var uri = new Uri(_host != "" ? _host : DEFAULT_HOST);

            socket = new SocketIOUnity(uri, new SocketIOOptions {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                Auth = new {
                    keyId = _keyId,
                    publicKey = _publicKey
                }
            });

            socket.JsonSerializer = new NewtonsoftJsonSerializer();

            socket.On(SocketEvent.Auth, (value) => {
                if (_playerId != "") SetPlayer(_playerId, _playerName);
            });

            socket.On(SocketEvent.Ready, (value) => {
                SetReady();
            });

            socket.OnDisconnected += (sender, value) => {
                if (value.Equals("io server disconnect")) Debug.LogError("Connection failed");
            };

            socket.Connect();
        }
        

        public void SetPlayer(string id, string name = "") {
            _playerId = id;
            _playerName = name;

            Fetch<bool>(new FetchOptions<bool> {
               EventName = "player",
               Data = new PlayerData {
                id = id,
                name = name,
               },
            });
        }

        public void Fetch<T>(FetchOptions<T> options) {
            options.Data.emitId = System.Guid.NewGuid().ToString();

            Action<SocketIOResponse> OnException = null;

            if (options.OnException != null) {
                OnException = (response) => {
                    EmitError error = response.GetValue<EmitError>();

                    if (error.emitId == options.Data.emitId) {
                        Off(SocketEvent.Exception, OnException);

                        options.OnException(error.message);
                    }
                };

                On(SocketEvent.Exception, OnException);
            }

            socket.Emit(options.EventName, (response) => {
                if (OnException != null) Off(SocketEvent.Exception, OnException);

                if (options.OnSuccess != null) options.OnSuccess(response.GetValue<T>());
            }, options.Data);
        }

        public void Stream<T>(StreamOptions<T> options)
        {
            options.Data.emitId = System.Guid.NewGuid().ToString();

            Action<SocketIOResponse> OnException = null;

            Action<SocketIOResponse> listener = null;

            listener = (e) => {
                StreamResponse streamResponse = e.GetValue<StreamResponse>();
                T value = e.GetValue<T>();

                if (streamResponse.status == StreamStatus.Start) {
                    if (options.OnStart != null) options.OnStart(value); 
                }
                else if (streamResponse.status == StreamStatus.Complete) {
                    Off(options.EventName, listener);
                    Off(SocketEvent.Exception, OnException);

                    if (options.OnComplete != null) options.OnComplete(value);
                }
                else {
                    if (options.OnProgress != null) options.OnProgress(value); 
                }
            };

            if (options.OnException != null) {
                OnException = (response) => {
                    EmitError error = response.GetValue<EmitError>();

                    if (error.emitId == options.Data.emitId) {
                        Off(options.EventName, listener);
                        Off(SocketEvent.Exception, OnException);

                        options.OnException(error.message);
                    }
                };

                On(SocketEvent.Exception, OnException);
            }

            On(options.EventName, listener);
            
            socket.Emit(options.EventName, options.Data);
        }

        // 3rd party can only hold one listener per event
        // On and Off allow having multiple listeners per event

        private void On(string eventName, Action<SocketIOResponse> handler) {
            if (!socketListeners.ContainsKey(eventName)) {
                socketListeners.Add(eventName, new List<Action<SocketIOResponse>>());

                socket.On(eventName, (response) => {
                    socketListeners[eventName].ForEach((value) => value(response));
                });
            }

            socketListeners[eventName].Add(handler);
        }

        private void Off(string eventName, Action<SocketIOResponse> handler) {
            if (socketListeners.ContainsKey(eventName)) socketListeners[eventName].Remove(handler);
        }

        override public void Dispose()
        {
            base.Dispose();
            
            socket.Off(SocketEvent.Auth);
            socket.Off(SocketEvent.Ready);

            List<string> socketListenersKeys = new List<string>(socketListeners.Keys);
            
            socketListenersKeys.ForEach((eventName) => socket.Off(eventName));

            socketListenersKeys.Clear();
            socketListeners.Clear();
        }

        public string PlayerId
        {
            get { return _playerId; }
        }

        public string PlayerName
        {
            get { return _playerName; }
        }

        public bool VoiceEnabled
        {
            get { return _voiceEnabled; }
        }

        public float VoiceVolume
        {
            get { return _voiceVolume; }
        }
    }
}
