using System;
using System.Collections.Generic;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;

namespace SmartNPC
{
    public class SmartNPCConnection : BaseEmitter
    {
        private Dictionary<string, List<Action<SocketIOResponse>>> socketListeners = new Dictionary<string, List<Action<SocketIOResponse>>>();
        private SocketIOUnity _socket;
        private SmartNPCSpeechRecognition _speechRecognition;
        private OVRLipSync _lipSync;

        private SmartNPCConnectionConfig _config;
        private string _userId;
        private string _userName;

        private static SmartNPCConnection _instance;
        private static List<Action<SmartNPCConnection>> _instanceReadyListeners = new List<Action<SmartNPCConnection>>();
        
        void Awake()
        {
            _config = Resources.Load<SmartNPCConnectionConfig>("SmartNPC Connection Config");

            if (!_config)
            {
                throw new Exception("Must configure SmartNPC. Go to SmartNPC -> Configuration");
            }

            if (!_config.APIKey.Validate())
            {
                throw new Exception("Must specify Key Id and Public Key. Go to SmartNPC -> Configuration");
            }

            _speechRecognition = FindOrAddObjectOfType<SmartNPCSpeechRecognition>();

            var uri = new Uri( _config.Advanced.GetHost() );

            _socket = new SocketIOUnity(uri, new SocketIOOptions {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                Auth = _config.APIKey.GetData()
            });

            _socket.JsonSerializer = new NewtonsoftJsonSerializer();

            _socket.On(SocketEvent.Auth, (value) => {
                if (_config.User.Id != "") SetUser(_config.User.Id, _config.User.Name);
            });

            _socket.On(SocketEvent.Ready, (value) => {
                SetReady();
            });

            _socket.OnDisconnected += (sender, value) => {
                if (value.Equals("io server disconnect")) Debug.LogError("Connection failed");
            };

            _socket.Connect();

            SetInstance(this);
        }
        
        public void SetUser(string id, string name = "") {
            _userId = id;
            _userName = name;

            Fetch<bool>(new FetchOptions<bool> {
               EventName = "user",
               Data = new UserData {
                id = id,
                name = name,
               },
            });
        }

        public void InitLipSync()
        {
            if (!_lipSync) _lipSync = FindOrAddObjectOfType<OVRLipSync>();
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

                // belongs to another request
                if (streamResponse.emitId != options.Data.emitId) return;

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

        public string UserId
        {
            get { return _userId; }
        }

        public string UserName
        {
            get { return _userName; }
        }

        public SmartNPCConnectionConfig Config
        {
            get { return _config; }
        }

        public SmartNPCSpeechRecognition SpeechRecognition
        {
            get { return _speechRecognition; }
        }

        public static void OnInstanceReady(Action<SmartNPCConnection> callback)
        {
            if (_instance && _instance.IsReady) callback(_instance);
            else _instanceReadyListeners.Add(callback);
        }

        private static void SetInstance(SmartNPCConnection instance)
        {
            if (_instance) throw new Exception("There should only be 1 SmartNPCConnection");

            _instance = instance;

            _instance.OnReady(() => {
                _instanceReadyListeners.ForEach((Action<SmartNPCConnection> action) => action(_instance));

                _instanceReadyListeners.Clear();
            });
        }
    }
}
