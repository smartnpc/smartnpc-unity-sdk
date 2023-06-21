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
        [SerializeField] private int _voiceMaxDistance = 20;


        [Header("Behaviors")]
        [SerializeField] private bool _behaviorsEnabled = false;


        [Header("Advanced Settings")]
        [SerializeField] private string _host;


        private const string DEFAULT_HOST = "wss://api.smartnpc.ai";
        private Dictionary<string, List<Action<SocketIOResponse>>> socketListeners = new Dictionary<string, List<Action<SocketIOResponse>>>();
        private SocketIOUnity _socket;
        private SmartNPCSpeechRecognition _speechRecognition;
        private OVRLipSync _lipSync;

        private static int totalConnections = 0;
        
        void Awake()
        {
            if (++totalConnections > 1)
            {
                throw new Exception("There should only be 1 SmartNPCConnection");
            }

            if (_keyId == null || _keyId == "" || _publicKey == null || _publicKey == "")
            {
                throw new Exception("Must specify Key Id and Public Key");
            }

            _speechRecognition = FindOrAddObjectOfType<SmartNPCSpeechRecognition>();

            var uri = new Uri(_host != "" ? _host : DEFAULT_HOST);

            _socket = new SocketIOUnity(uri, new SocketIOOptions {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                Auth = new {
                    keyId = _keyId,
                    publicKey = _publicKey
                }
            });

            _socket.JsonSerializer = new NewtonsoftJsonSerializer();

            _socket.On(SocketEvent.Auth, (value) => {
                if (_playerId != "") SetPlayer(_playerId, _playerName);
            });

            _socket.On(SocketEvent.Ready, (value) => {
                SetReady();
            });

            _socket.OnDisconnected += (sender, value) => {
                if (value.Equals("io server disconnect")) Debug.LogError("Connection failed");
            };

            _socket.Connect();
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

        public void InitLipSync()
        {
            if (_lipSync) return;

            _lipSync = gameObject.GetComponent<OVRLipSync>();
            
            if (_lipSync) _lipSync = gameObject.AddComponent<OVRLipSync>();
        }

        public void Emit(string eventName, params object[] data) {
            _socket.Emit(eventName, data);
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

            _socket.Emit(options.EventName, (response) => {
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
            
            _socket.Emit(options.EventName, options.Data);
        }

        // 3rd party can only hold one listener per event
        // On and Off allow having multiple listeners per event

        public void On(string eventName, Action<SocketIOResponse> handler) {
            if (!socketListeners.ContainsKey(eventName)) {
                socketListeners.Add(eventName, new List<Action<SocketIOResponse>>());

                _socket.On(eventName, (response) => {
                    socketListeners[eventName].ForEach((value) => value(response));
                });
            }

            socketListeners[eventName].Add(handler);
        }

        public void Off(string eventName, Action<SocketIOResponse> handler) {
            if (socketListeners.ContainsKey(eventName)) socketListeners[eventName].Remove(handler);
        }

        override public void Dispose()
        {
            base.Dispose();
            
            _socket.Off(SocketEvent.Auth);
            _socket.Off(SocketEvent.Ready);

            List<string> socketListenersKeys = new List<string>(socketListeners.Keys);
            
            socketListenersKeys.ForEach((eventName) => _socket.Off(eventName));

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

        public float VoiceMaxDistance
        {
            get { return _voiceMaxDistance; }
        }

        public bool BehaviorsEnabled
        {
            get { return _behaviorsEnabled; }
        }

        public SmartNPCSpeechRecognition SpeechRecognition
        {
            get { return _speechRecognition; }
        }
    }
}
