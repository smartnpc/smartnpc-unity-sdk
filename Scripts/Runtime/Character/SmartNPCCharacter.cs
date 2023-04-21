using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartNPC
{
    public class SmartNPCCharacter : BaseEmitter
    {
        [SerializeField] private string _characterId;
        SmartNPCConnection _connection;
        private SmartNPCCharacterInfo _info;
        List<SmartNPCMessage> _messages = new List<SmartNPCMessage>();

        public event EventHandler<SmartNPCMessage> OnMessageStart;
        public event EventHandler<SmartNPCMessage> OnMessageProgress;
        public event EventHandler OnMessageTextComplete;
        public event EventHandler OnMessageVoiceComplete;
        public event EventHandler OnMessageComplete;
        public event EventHandler<SmartNPCMessage> OnMessageException;
        public event EventHandler<List<SmartNPCMessage>> OnMessageHistoryChange;

        void Awake()
        {
            if (_characterId == null || _characterId == "") throw new Exception("Must specify Id");

            _connection = FindObjectOfType<SmartNPCConnection>();

            _connection.OnReady(Init);
        }

        private void Init()
        {
            Action onComplete = () => {
                if (_info != null && _messages != null)
                {
                    InvokeOnUpdate(() => OnMessageHistoryChange?.Invoke(this, _messages));

                    SetReady();
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
            string text = "";

            EventHandler<MessageResponse> emitProgress = (object sender, MessageResponse response) => {
                text += response.text;

                // TODO: attach AudioClip voice to SmartNPCMessage value?
                
                SmartNPCMessage value = new SmartNPCMessage { message = message, response = text, chunk = response.text };

                _messages[_messages.Count - 1] = value;

                InvokeOnUpdate(() => {
                    OnMessageProgress?.Invoke(this, value);
                    OnMessageHistoryChange?.Invoke(this, _messages);
                });
            };

            SmartNPCVoice voice = GetComponent<SmartNPCVoice>();
            
            if (voice != null)
            {
                voice.Reset();

                EventHandler onVoiceComplete = null;

                onVoiceComplete = (sender, e) => {
                    InvokeOnUpdate(() => {
                        OnMessageVoiceComplete?.Invoke(this, EventArgs.Empty);
                        OnMessageComplete?.Invoke(this, EventArgs.Empty);
                    });

                    voice.OnVoiceProgress -= emitProgress;
                    voice.OnVoiceComplete -= onVoiceComplete;
                };

                voice.OnVoiceProgress += emitProgress;
                voice.OnVoiceComplete += onVoiceComplete;
            }

            SmartNPCMessage newMessage = new SmartNPCMessage { message = message, response = "" };

            _messages.Add(newMessage);

            InvokeOnUpdate(() => {
                OnMessageStart?.Invoke(this, newMessage);
                OnMessageProgress?.Invoke(this, newMessage);
                OnMessageHistoryChange?.Invoke(this, _messages);
            });

            _connection.Stream(new StreamOptions<MessageResponse> {
                EventName = "message",
                Data = new MessageData {
                    character = _characterId,
                    message = message,
                    voice = voice != null
                },
                OnProgress = (MessageResponse response) => {
                    if (voice != null && response.voice != null) InvokeOnUpdate(() => voice.Add(response));
                    else emitProgress(this, response);
                },
                OnComplete = (MessageResponse response) => {
                    SmartNPCMessage value = new SmartNPCMessage { message = message, response = text };

                    _messages[_messages.Count - 1] = value;

                    InvokeOnUpdate(() => {
                        OnMessageTextComplete?.Invoke(this, EventArgs.Empty);
                        OnMessageHistoryChange?.Invoke(this, _messages);

                        if (voice != null) voice.SetStreamComplete();
                        else OnMessageComplete?.Invoke(this, EventArgs.Empty);
                    });
                },
                OnException = (string exception) => {
                    SmartNPCMessage value = new SmartNPCMessage { message = message, exception = exception };

                    _messages[_messages.Count - 1] = value;

                    InvokeOnUpdate(() => {
                        OnMessageException?.Invoke(this, value);
                        OnMessageHistoryChange?.Invoke(this, _messages);
                    });
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
        }
    }
}
