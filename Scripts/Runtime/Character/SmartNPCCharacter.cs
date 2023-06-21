using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SmartNPC
{
    public class SmartNPCCharacter : BaseEmitter
    {
        [SerializeField] private string _characterId;
        

        [Header("OVR Lip Sync")]
        [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer;
        [SerializeField] [Range(1, 100)] private int _visemeBlendRange = 1;

        
        private SmartNPCConnection _connection;
        private SmartNPCVoice _voice;
        private SmartNPCCharacterInfo _info;
        private List<SmartNPCMessage> _messages;
        private SmartNPCBehaviorQueue _behaviorQueue;
        private OVRLipSyncContext _lipSyncContext;
        private OVRLipSyncContextMorphTarget _lipSyncContextMorphTarget;
        private string _currentResponse = "";
        private bool _messageInProgress = false; // from message sent until message complete
        private bool _speaking = false; // from message first progress until message complete

        public readonly UnityEvent<SmartNPCMessage> OnMessageStart = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageProgress = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageTextComplete = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageVoiceComplete = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageComplete = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageException = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<List<SmartNPCMessage>> OnMessageHistoryChange = new UnityEvent<List<SmartNPCMessage>>();

        private string[] readyPlayerMeVisemes = {
            "viseme_sil",
            "viseme_PP",
            "viseme_FF",
            "viseme_TH",
            "viseme_DD",
            "viseme_kk",
            "viseme_CH",
            "viseme_SS",
            "viseme_nn",
            "viseme_RR",
            "viseme_aa",
            "viseme_E",
            "viseme_I",
            "viseme_O",
            "viseme_U"
        };

        void Awake()
        {
            if (_characterId == null || _characterId == "") throw new Exception("Must specify Id");

            _voice = GetOrAddComponent<SmartNPCVoice>();
            _behaviorQueue = GetOrAddComponent<SmartNPCBehaviorQueue>();

            _connection = FindObjectOfType<SmartNPCConnection>();
            
            if (!_connection) throw new Exception("No SmartNPCConnection found");

            _connection.OnReady(Init);

            if (_skinnedMeshRenderer) InitLipSync();
        }

        private void InitLipSync()
        {
            _connection.InitLipSync();

            _lipSyncContext = GetOrAddComponent<OVRLipSyncContext>();
            _lipSyncContextMorphTarget = GetOrAddComponent<OVRLipSyncContextMorphTarget>();

            _lipSyncContext.audioLoopback = true;

            _lipSyncContextMorphTarget.skinnedMeshRenderer = _skinnedMeshRenderer;
            _lipSyncContextMorphTarget.visemeBlendRange = _visemeBlendRange;

            SetReadyPlayerMeVisemeToBlendTargets();
        }

        private void SetReadyPlayerMeVisemeToBlendTargets()
        {
            for (int i= 0; i < _skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
            {
                int originalIndex = Array.IndexOf(readyPlayerMeVisemes, _skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i));

                if (originalIndex != -1) _lipSyncContextMorphTarget.visemeToBlendTargets[originalIndex] = i;
            }
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

            List<SmartNPCBehavior> behaviors = new List<SmartNPCBehavior>();

            _currentResponse = "";
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
                SmartNPCMessage value = new SmartNPCMessage { message = message };
                
                if (response.text != "")
                {
                    _currentResponse += response.text;

                    value.chunk = response.text;
                }
                
                if (response.behavior != null) _behaviorQueue.Add(response.behavior);

                value.response = _currentResponse;
                value.behaviors = behaviors;

                emitProgress(value);
            };

            UnityAction<VoiceMessage> emitVoiceProgress = (VoiceMessage response) => {
                _currentResponse += response.rawResponse.text;

                SmartNPCMessage value = new SmartNPCMessage {
                    message = message,
                    response = _currentResponse,
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
                    SmartNPCMessage value = new SmartNPCMessage { message = message, response = _currentResponse };

                    InvokeOnUpdate(() => {
                        OnMessageVoiceComplete.Invoke(value);
                        OnMessageComplete.Invoke(value);

                        _speaking = false;
                        _messageInProgress = false;
                        _currentResponse = "";
                    });

                    _voice.OnVoiceProgress.RemoveListener(emitVoiceProgress);
                    _voice.OnVoiceComplete.RemoveListener(onVoiceComplete);
                };

                UnityAction<VoiceMessage> onPlayLastChunk = null;

                onPlayLastChunk = (VoiceMessage response) => {
                    SmartNPCMessage value = new SmartNPCMessage {
                        message = message,
                        response = _currentResponse,
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

            SmartNPCMessage newMessage = new SmartNPCMessage { message = message, response = _currentResponse, behaviors = behaviors };

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
                    voice = _voice.Enabled,
                    behaviors = _connection.BehaviorsEnabled
                },
                OnProgress = (MessageResponse response) => {
                    if (_voice.Enabled && response.voice != null) InvokeOnUpdate(async () => await _voice.Add(response));
                    else emitTextProgress(response);
                },
                OnComplete = (MessageResponse response) => {
                    SmartNPCMessage value = new SmartNPCMessage { message = message, response = _currentResponse };

                    _messages[_messages.Count - 1] = value;

                    InvokeOnUpdate(() => OnMessageHistoryChange.Invoke(_messages));

                    if (_voice.Enabled) _voice.SetStreamComplete();
                    else
                    {
                        InvokeOnUpdate(() => {
                            _messageInProgress = false;
                            _speaking = false;
                            _currentResponse = "";

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
                        _currentResponse = "";

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
        
        public SmartNPCBehaviorQueue BehaviorQueue
        {
            get { return _behaviorQueue; }
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

        public string CurrentResponse
        {
            get { return _currentResponse; }
        }
        
        override public void Dispose()
        {
            base.Dispose();
            
            _messages.Clear();
            _behaviorQueue.Dispose();

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
