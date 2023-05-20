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
        private SmartNPCVoice _voice;
        private SmartNPCCharacterInfo _info;
        private List<SmartNPCMessage> _messages;
        private bool _messageInProgress = false; // message sent until message complete
        private bool _speaking = false; // message first progress until message complete

        public readonly UnityEvent<SmartNPCMessage> OnMessageStart = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageProgress = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageTextComplete = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageVoiceComplete = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageComplete = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageException = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<List<SmartNPCMessage>> OnMessageHistoryChange = new UnityEvent<List<SmartNPCMessage>>();

        void Awake()
        {
            if (_characterId == null || _characterId == "") throw new Exception("Must specify Id");

            _voice = gameObject.AddComponent<SmartNPCVoice>();

            _connection = FindObjectOfType<SmartNPCConnection>();

            _connection.OnReady(Init);
        }

        private void Init()
        {
            Action onComplete = () => {
                if (_info != null && _messages != null)
                {
                    InvokeOnUpdate(() => OnMessageHistoryChange.Invoke(_messages));

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

        public void ClearMessageHistory()
        {
            _connection.Fetch<bool>(new FetchOptions<bool> {
               EventName = "clearmessagehistory",
               Data = new MessageHistoryData { character = _characterId },
               OnSuccess = (bool value) => {
                _messages.Clear();
                
                InvokeOnUpdate(() => OnMessageHistoryChange.Invoke(_messages));
               },
               OnException = (response) => {
                throw new Exception("Couldn't clear message history");
               }
            });
        }

        public new void SendMessage(string message)
        {
            if (!_connection.IsReady) throw new Exception("Connection isn't ready");

            string text = "";

            _messageInProgress = true;

            UnityAction<SmartNPCMessage> emitProgress = (SmartNPCMessage value) => {
                _speaking = true;

                _messages[_messages.Count - 1] = value;

                InvokeOnUpdate(() => {
                    OnMessageProgress.Invoke(value);
                    OnMessageHistoryChange.Invoke(_messages);
                });
            };

            UnityAction<MessageResponse> emitTextProgress = (MessageResponse response) => {
                text += response.text;

                SmartNPCMessage value = new SmartNPCMessage {
                    message = message,
                    response = text,
                    chunk = response.text
                };

                emitProgress(value);
            };

            UnityAction<VoiceMessage> emitVoiceProgress = (VoiceMessage response) => {
                text += response.rawResponse.text;

                SmartNPCMessage value = new SmartNPCMessage {
                    message = message,
                    response = text,
                    chunk = response.rawResponse.text,
                    voiceClip = response.clip
                };

                emitProgress(value);
            };
            
            if (_voice.Enabled)
            {
                _voice.Reset();

                UnityAction<VoiceMessage> onVoiceComplete = null;
                
                onVoiceComplete = (VoiceMessage response) => {
                    SmartNPCMessage value = new SmartNPCMessage { message = message, response = text };

                    InvokeOnUpdate(() => {
                        OnMessageVoiceComplete.Invoke(value);
                        OnMessageComplete.Invoke(value);

                        _speaking = false;
                        _messageInProgress = false;
                    });

                    _voice.OnVoiceProgress.RemoveListener(emitVoiceProgress);
                    _voice.OnVoiceComplete.RemoveListener(onVoiceComplete);
                };

                UnityAction<VoiceMessage> onPlayLastChunk = null;

                onPlayLastChunk = (VoiceMessage response) => {
                    SmartNPCMessage value = new SmartNPCMessage {
                        message = message,
                        response = text,
                        chunk = response.rawResponse.text,
                        voiceClip = response.clip
                    };

                    InvokeOnUpdate(() => OnMessageTextComplete.Invoke(value));

                    _voice.OnPlayLastChunk.RemoveListener(onPlayLastChunk);
                };

                _voice.OnVoiceProgress.AddListener(emitVoiceProgress);
                _voice.OnVoiceComplete.AddListener(onVoiceComplete);
                _voice.OnPlayLastChunk.AddListener(onPlayLastChunk);
            }

            SmartNPCMessage newMessage = new SmartNPCMessage { message = message, response = "" };

            _messages.Add(newMessage);

            InvokeOnUpdate(() => {
                OnMessageStart.Invoke(newMessage);
                OnMessageHistoryChange.Invoke(_messages);
            });

            _connection.Stream(new StreamOptions<MessageResponse> {
                EventName = "message",
                Data = new MessageData {
                    character = _characterId,
                    message = message,
                    voice = _voice.Enabled
                },
                OnProgress = (MessageResponse response) => {
                    if (_voice.Enabled && response.voice != null) InvokeOnUpdate(async () => await _voice.Add(response));
                    else emitTextProgress(response);
                },
                OnComplete = (MessageResponse response) => {
                    SmartNPCMessage value = new SmartNPCMessage { message = message, response = text };

                    _messages[_messages.Count - 1] = value;

                    InvokeOnUpdate(() => OnMessageHistoryChange.Invoke(_messages));

                    if (_voice.Enabled) _voice.SetStreamComplete();
                    else
                    {
                        InvokeOnUpdate(() => {
                            _messageInProgress = false;
                            _speaking = false;

                            OnMessageTextComplete.Invoke(value);
                            OnMessageComplete.Invoke(value);
                        });
                    }
                },
                OnException = (string exception) => {
                    SmartNPCMessage value = new SmartNPCMessage { message = message, exception = exception };

                    _messages[_messages.Count - 1] = value;

                    InvokeOnUpdate(() => {
                        _messageInProgress = false;
                        _speaking = false;

                        OnMessageException.Invoke(value);
                        OnMessageHistoryChange.Invoke(_messages);
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

        public SmartNPCVoice Voice
        {
            get { return _voice; }
        }

        public bool Speaking
        {
            get { return _speaking; }
        }

        public bool MessageInProgress
        {
            get { return _messageInProgress; }
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
