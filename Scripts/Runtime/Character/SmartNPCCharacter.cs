using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartNPC
{
    public class SmartNPCCharacter : BaseEmitter
    {
        [SerializeField] private string _characterId;
        private SmartNPCConnection _connection;
        private Voice _voice;
        private SmartNPCCharacterInfo _info;
        private List<SmartNPCMessage> _messages;

        public event EventHandler OnMessageStart;
        public event EventHandler<SmartNPCMessage> OnMessageProgress;
        public event EventHandler OnMessageTextComplete;
        public event EventHandler OnMessageVoiceComplete;
        public event EventHandler OnMessageComplete;
        public event EventHandler<SmartNPCMessage> OnMessageException;
        public event EventHandler<List<SmartNPCMessage>> OnMessageHistoryChange;

        void Awake()
        {
            if (_characterId == null || _characterId == "") throw new Exception("Must specify Id");

            _voice = new Voice(this);

            _connection = FindObjectOfType<SmartNPCConnection>();

            _connection.OnReady(Init);
        }

        override protected void Update()
        {
            base.Update();
            
            _voice.CheckFinishedPlayingChunk();
        }

        private void Init()
        {
            Action onComplete = () => {
                if (_info != null && _messages != null)
                {
                    InvokeOnUpdate(() => OnMessageHistoryChange?.Invoke(this, _messages));

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
            string text = "";

            EventHandler<MessageResponse> emitProgress = (object sender, MessageResponse response) => {
                text += response.text;
                
                SmartNPCMessage value = new SmartNPCMessage { message = message, response = text, chunk = response.text };

                _messages[_messages.Count - 1] = value;

                InvokeOnUpdate(() => {
                    OnMessageProgress?.Invoke(this, value);
                    OnMessageHistoryChange?.Invoke(this, _messages);
                });
            };
            
            if (_voice.Enabled)
            {
                _voice.Reset();

                EventHandler onVoiceComplete = null;
                
                onVoiceComplete = (sender, e) => {
                    InvokeOnUpdate(() => {
                        OnMessageVoiceComplete?.Invoke(this, EventArgs.Empty);
                        OnMessageComplete?.Invoke(this, EventArgs.Empty);
                    });

                    _voice.OnVoiceProgress -= emitProgress;
                    _voice.OnVoiceComplete -= onVoiceComplete;
                };

                EventHandler onPlayLastChunk = null;

                onPlayLastChunk = (sender, e) => {
                    InvokeOnUpdate(() => {
                        OnMessageTextComplete?.Invoke(this, EventArgs.Empty);
                    });

                    _voice.OnPlayLastChunk -= onPlayLastChunk;
                };

                _voice.OnVoiceProgress += emitProgress;
                _voice.OnVoiceComplete += onVoiceComplete;
                _voice.OnPlayLastChunk += onPlayLastChunk;
            }

            SmartNPCMessage newMessage = new SmartNPCMessage { message = message, response = "" };

            _messages.Add(newMessage);

            OnMessageStart?.Invoke(this, EventArgs.Empty);
            OnMessageProgress?.Invoke(this, newMessage);
            OnMessageHistoryChange?.Invoke(this, _messages);

            _connection.Stream(new StreamOptions<MessageResponse> {
                EventName = "message",
                Data = new MessageData {
                    character = _characterId,
                    message = message,
                    voice = _voice.Enabled
                },
                OnProgress = (MessageResponse response) => {
                    if (_voice.Enabled && response.voice != null) InvokeOnUpdate(() => _voice.Add(response));
                    else emitProgress(this, response);
                },
                OnComplete = (MessageResponse response) => {
                    SmartNPCMessage value = new SmartNPCMessage { message = message, response = text };

                    _messages[_messages.Count - 1] = value;

                    InvokeOnUpdate(() => {
                        OnMessageHistoryChange?.Invoke(this, _messages);

                        if (_voice.Enabled) _voice.SetStreamComplete();
                        else
                        {
                            OnMessageTextComplete?.Invoke(this, EventArgs.Empty);
                            OnMessageComplete?.Invoke(this, EventArgs.Empty);
                        }
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
