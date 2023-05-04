using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SmartNPC
{
    public class SmartNPCCharacter : BaseEmitter
    {
        [SerializeField] private string _characterId;
        private SmartNPCConnection _connection;
        private Voice _voice;
        private SmartNPCCharacterInfo _info;
        private List<SmartNPCMessage> _messages;

        public readonly UnityEvent OnMessageStart = new UnityEvent();
        public readonly UnityEvent<SmartNPCMessage> OnMessageProgress = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent OnMessageTextComplete = new UnityEvent();
        public readonly UnityEvent OnMessageVoiceComplete = new UnityEvent();
        public readonly UnityEvent OnMessageComplete = new UnityEvent();
        public readonly UnityEvent<SmartNPCMessage> OnMessageException = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<List<SmartNPCMessage>> OnMessageHistoryChange = new UnityEvent<List<SmartNPCMessage>>();

        void Awake()
        {
            if (_characterId == null || _characterId == "") throw new Exception("Must specify Id");

            _voice = gameObject.AddComponent<Voice>();

            _connection = FindObjectOfType<SmartNPCConnection>();

            _connection.OnReady(Init);
        }

        private void Init()
        {
            Action onComplete = () => {
                if (_info != null && _messages != null)
                {
                    OnMessageHistoryChange.Invoke(_messages);

                    if (!IsReady) SetReady();
                }
            };

            FetchInfo(onComplete);
            FetchMessageHistory(onComplete);
        }

        private void FetchInfo(Action onComplete)
        {
            _connection.Fetch<SmartNPCCharacterInfo>(new FetchOptions<SmartNPCCharacterInfo> {
               EventName = "character",
               Data = new CharacterInfoData { id = _characterId },
               OnSuccess = (response) => {
                _info = response;

                onComplete();
               },
               OnException = (response) => {
                throw new Exception("Couldn't get character info");
               }
            });
        }

        private void FetchMessageHistory(Action onComplete)
        {
            _connection.Fetch<MessageHistoryResponse>(new FetchOptions<MessageHistoryResponse> {
               EventName = "messagehistory",
               Data = new MessageHistoryData { character = _characterId },
               OnSuccess = (response) => {
                _messages = response.data;

                onComplete();
               },
               OnException = (response) => {
                throw new Exception("Couldn't get message history");
               }
            });
        }

        public new void SendMessage(string message)
        {
            if (!_connection.IsReady) throw new Exception("Connection isn't ready");

            string text = "";

            UnityAction<MessageResponse> emitProgress = (MessageResponse response) => {
                text += response.text;
                
                SmartNPCMessage value = new SmartNPCMessage { message = message, response = text, chunk = response.text };

                _messages[_messages.Count - 1] = value;

                OnMessageProgress.Invoke(value);
                OnMessageHistoryChange.Invoke(_messages);
            };
            
            if (_voice.Enabled)
            {
                _voice.Reset();

                UnityAction onVoiceComplete = null;
                
                onVoiceComplete = () => {
                    OnMessageVoiceComplete.Invoke();
                    OnMessageComplete.Invoke();

                    _voice.OnVoiceProgress.RemoveListener(emitProgress);
                    _voice.OnVoiceComplete.RemoveListener(onVoiceComplete);
                };

                UnityAction onPlayLastChunk = null;

                onPlayLastChunk = () => {
                    OnMessageTextComplete.Invoke();

                    _voice.OnPlayLastChunk.RemoveListener(onPlayLastChunk);
                };

                _voice.OnVoiceProgress.AddListener(emitProgress);
                _voice.OnVoiceComplete.AddListener(onVoiceComplete);
                _voice.OnPlayLastChunk.AddListener(onPlayLastChunk);
            }

            SmartNPCMessage newMessage = new SmartNPCMessage { message = message, response = "" };

            _messages.Add(newMessage);

            OnMessageStart.Invoke();
            OnMessageProgress.Invoke(newMessage);
            OnMessageHistoryChange.Invoke(_messages);

            _connection.Stream(new StreamOptions<MessageResponse> {
                EventName = "message",
                Data = new MessageData {
                    character = _characterId,
                    text = message,
                    voice = _voice.Enabled
                },
                OnProgress = (MessageResponse response) => {
                    if (_voice.Enabled && response.voice != null) InvokeOnUpdate(() => _voice.Add(response));
                    else emitProgress(response);
                },
                OnComplete = (MessageResponse response) => {
                    SmartNPCMessage value = new SmartNPCMessage { message = message, response = text };

                    _messages[_messages.Count - 1] = value;

                    OnMessageHistoryChange.Invoke(_messages);

                    if (_voice.Enabled) _voice.SetStreamComplete();
                    else
                    {
                        OnMessageTextComplete.Invoke();
                        OnMessageComplete.Invoke();
                    }
                },
                OnException = (string exception) => {
                    SmartNPCMessage value = new SmartNPCMessage { message = message, exception = exception };

                    _messages[_messages.Count - 1] = value;

                    OnMessageException.Invoke(value);
                    OnMessageHistoryChange.Invoke(_messages);
                }
            });
        }

        public SmartNPCCharacterInfo Info
        {
            get { return _info; }
        }

        public List<SmartNPCMessage> Messages
        {
            get { return _messages; }
        }

        public SmartNPCConnection Connection
        {
            get { return _connection; }
        }
        
        override public void Dispose()
        {
            base.Dispose();
            
            _messages.Clear();

            OnMessageStart.RemoveAllListeners();
            OnMessageProgress.RemoveAllListeners();
            OnMessageTextComplete.RemoveAllListeners();
            OnMessageVoiceComplete.RemoveAllListeners();
            OnMessageComplete.RemoveAllListeners();
            OnMessageException.RemoveAllListeners();
            OnMessageHistoryChange.RemoveAllListeners();
        }
    }
}
