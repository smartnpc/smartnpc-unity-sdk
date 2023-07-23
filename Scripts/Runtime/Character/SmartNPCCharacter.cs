using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace SmartNPC
{
    public class SmartNPCCharacter : BaseEmitter
    {
        [SerializeField] private string _characterId;
        [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer;
        [SerializeField] private SmartNPCLipSyncConfig _lipSyncConfig;
        [SerializeField] private SmartNPCExpressionsConfig _expressionsConfig;
        [SerializeField] private SmartNPCGesturesConfig _gesturesConfig;
        
        private SmartNPCConnection _connection;
        private SmartNPCVoice _voice;
        private SmartNPCCharacterInfo _info;
        private List<SmartNPCMessage> _messages;
        private SmartNPCBehaviorQueue _behaviorQueue;
        private OVRLipSyncContext _lipSyncContext;
        private OVRLipSyncContextMorphTarget _lipSyncContextMorphTarget;
        private Animator _animator;
        private string _expression;
        private List<string> _expressionsBlendShapes = new List<string>();
        private Dictionary<string, int> _blendShapeIndexes = new Dictionary<string, int>();
        private List<string> _blendShapeNames = new List<string>();
        private Dictionary<string, float> _blendShapeTargetWeights = new Dictionary<string, float>();
        private string _currentResponse = "";
        private bool _messageInProgress = false; // from message sent until message complete
        private bool _speaking = false; // from message first progress until message complete

        private readonly List<string> MiscBlendShapes = new List<string>() {
            "mouthOpen",
            "mouthSmile",
            "eyesClosed",
            "eyesLookUp",
            "eyesLookDown",
            "eyeBlinkLeft",
            "eyeBlinkRight"
        };

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

            SmartNPCConnection.OnInstanceReady(Init);
        }

        override protected void Update()
        {
            base.Update();

            if (!_connection) return;

            AnimateExpression();
        }

        private void Init(SmartNPCConnection connection)
        {
            _connection = connection;

            _voice = GetOrAddComponent<SmartNPCVoice>();
            _behaviorQueue = GetOrAddComponent<SmartNPCBehaviorQueue>();
            _expressionsBlendShapes = GetExpressionsBlendShapes();

            MapBlendShapeIndexes();
            InitLipSync();
            InitGestures();

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

        private void InitGestures()
        {
            if (!_gesturesConfig) return;
            
            _animator = GetComponent<Animator>();

            #if UNITY_EDITOR

            // settting to a var as a workaround to avoid warning for no await
            Task applyGestureAnimationsTask = GestureAnimations.ApplyGestureAnimations(this, _gesturesConfig.Gestures);

            #endif

            if (_gesturesConfig.TriggerGestures)
            {
                _behaviorQueue.ConsumeGestures(async (SmartNPCGesture gesture, UnityAction next) => {
                    await TriggerGesture(gesture);

                    next();
                });
            }
        }

        private void MapBlendShapeIndexes()
        {
            if (!_skinnedMeshRenderer) return;

            for (int i = 0; i < _skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
            {
                string blendShapeName = _skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i);

                _blendShapeIndexes[blendShapeName] = i;

                _blendShapeNames.Add(blendShapeName);
            }
        }

        private void InitLipSync()
        {
            if (!_lipSyncConfig || !_skinnedMeshRenderer) return;

            _connection.InitLipSync();

            _lipSyncContext = GetOrAddComponent<OVRLipSyncContext>();
            _lipSyncContextMorphTarget = GetOrAddComponent<OVRLipSyncContextMorphTarget>();

            _lipSyncContext.audioLoopback = true;

            _lipSyncContextMorphTarget.skinnedMeshRenderer = _skinnedMeshRenderer;
            _lipSyncContextMorphTarget.visemeBlendRange = _lipSyncConfig.VisemeBlendRange;

            SetVisemeToBlendTargets();
        }

        private void SetVisemeToBlendTargets()
        {
            if (!_lipSyncConfig) return;

            List<string> visemes = _lipSyncConfig.VisemeBlendShapes.GetBlendShapes();

            for (int i = 0; i < visemes.Count; i++)
            {
                int blendShapeIndex = _blendShapeIndexes[visemes[i]];

                if (blendShapeIndex != -1) _lipSyncContextMorphTarget.visemeToBlendTargets[i] = blendShapeIndex;
            }
        }

        private List<string> GetExpressionsBlendShapes()
        {
            if (!_expressionsConfig) return new List<string>();

            HashSet<string> result = new HashSet<string>();

            _expressionsConfig.Expressions.ForEach((SmartNPCExpressionItem expression) => {
                expression.blendShapes.ForEach((SmartNPCBlendShape blendShape) => result.Add(blendShape.name));
            });

            return new List<string>(result);
        }

        private void SetBlendShapeWeight(string name, float weight)
        {
            if (!_skinnedMeshRenderer) return;

            if (!_blendShapeIndexes.ContainsKey(name))
            {
                Debug.LogWarning("Blend shape not found: " + name);

                return;
            }
            
            int index = _blendShapeIndexes[name];

            if (index != -1) _skinnedMeshRenderer.SetBlendShapeWeight(index, weight);
        }

        private float GetBlendShapeWeight(string name)
        {
            if (!_skinnedMeshRenderer) return 0f;

            if (!_blendShapeIndexes.ContainsKey(name))
            {
                Debug.LogWarning("Blend shape not found: " + name);

                return 0f;
            }
            
            int index = _blendShapeIndexes[name];

            if (index != -1) return _skinnedMeshRenderer.GetBlendShapeWeight(index);

            return 0f;
        }

        private int FindExpressionIndex(string name)
        {
            return _expressionsConfig.Expressions.FindIndex((SmartNPCExpressionItem expression) => expression.expressionName == name);
        }

        private SmartNPCExpressionItem FindExpressionItem(string name)
        {
            int index = FindExpressionIndex(name);

            return index != -1 ? _expressionsConfig.Expressions[index] : null;
        }

        public List<string> GetUsedBlendShapes()
        {
            List<string> result = new List<string>(MiscBlendShapes);

            if (_expressionsConfig) result.AddRange( GetExpressionsBlendShapes() );
            if (_lipSyncConfig) result.AddRange( _lipSyncConfig.VisemeBlendShapes.GetBlendShapes() );

            return result;
        }

        public List<string> GetUnusedBlendShapes()
        {
            List<string> result = new List<string>(_blendShapeNames);

            GetUsedBlendShapes().ForEach((string name) => result.Remove(name));

            return result;
        }

        public void SetExpression(string name)
        {
            if (!_expressionsConfig || !_skinnedMeshRenderer) return;

            SmartNPCExpressionItem expression = FindExpressionItem(name);

            if (expression == null)
            {
                Debug.LogWarning("Expression not found: " + name);

                return;
            }

            _expression = name;

            // reset all expressions blend shape weights to 0
            _expressionsBlendShapes.ForEach((string blendShapeName) => _blendShapeTargetWeights[blendShapeName] = 0);

            // set current expressions blend shape weights
            expression.blendShapes.ForEach((SmartNPCBlendShape blendShape) => _blendShapeTargetWeights[blendShape.name] = blendShape.weight);
        }

        public void TestNextExpression()
        {
            if (!_expressionsConfig || !_skinnedMeshRenderer) return;
            
            int index = 0;

            if (_expression != null)
            {
                index = FindExpressionIndex(_expression);

                if (index < _expressionsConfig.Expressions.Count - 1) index++;
                else index = 0;
            }

            string name = _expressionsConfig.Expressions[index].expressionName;

            Debug.Log("Expression: " + name);

            SetExpression(name);
        }

        private void AnimateExpression()
        {
            if (!_expressionsConfig || !_skinnedMeshRenderer) return;

            foreach (var (name, targetWeight) in _blendShapeTargetWeights)
            {
                float newWeight = Mathf.Lerp(GetBlendShapeWeight(name), targetWeight, _expressionsConfig.InterpolationSpeed);

                SetBlendShapeWeight(name, newWeight);
            }
        }

        public async Task TriggerGesture(SmartNPCGesture gesture)
        {
            if (!_gesturesConfig) return;

            for (int i = 0; i < _gesturesConfig.Gestures.Count; i++)
            {
                SmartNPCGestureItem item = _gesturesConfig.Gestures[i];

                if (item.gestureName == gesture.name)
                {
                    if (item.animationClip != null) await TriggerAnimation(SmartNPCGesture.Prefix + "-" + gesture.name + "Trigger");
                    else if (item.animationTrigger != null && item.animationTrigger != "") await TriggerAnimation(item.animationTrigger);

                    break;
                }
            }
        }

        public async Task TriggerAnimation(string name)
        {
            if (_animator)
            {
                _animator.SetTrigger(name);

                await WaitUntilAnimationFinished();
            }
        }

        public async Task WaitUntilAnimationFinished()
        {
            if (_animator) await TaskUtils.WaitUntil(() => _animator.GetCurrentAnimatorStateInfo(0).normalizedTime <= 1.0f);
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
                _messages = response.data.ConvertAll<SmartNPCMessage>((RawHistoryMessage rawMessage) => {
                    return new SmartNPCMessage {
                        message = rawMessage.message,
                        response = rawMessage.response,
                        behaviors = rawMessage.behaviors.ConvertAll<SmartNPCBehavior>((RawBehavior rawBehavior) => SmartNPCBehavior.parse(rawBehavior))
                    };
                });

                if (_connection.Config.Behaviors.Enabled) InvokeOnUpdate(() => SetLastExpression(_messages));

                onComplete();
               },
               OnException = (response) => {
                throw new Exception("Couldn't get message history");
               }
            });
        }

        private void SetLastExpression(List<SmartNPCMessage> messages)
        {
            if (_messages.Count == 0) return;

            SmartNPCMessage lastMessage = _messages[_messages.Count - 1];
            List<SmartNPCBehavior> behaviors = lastMessage.behaviors;

            if (behaviors.Count == 0) return;

            SmartNPCBehavior behavior = behaviors.Find((SmartNPCBehavior behavior) => behavior.type == SmartNPCBehaviorType.Expression);

            if (behavior == null) return;
            
            SmartNPCExpression expression = behavior as SmartNPCExpression;

            SetExpression(expression.next);
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

            SmartNPCExpression expression = null;

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
                
                if (response.behavior != null)
                {
                    SmartNPCBehavior behavior = SmartNPCBehavior.parse(response.behavior);

                    if (behavior is SmartNPCExpression)
                    {
                        expression = behavior as SmartNPCExpression;

                        InvokeOnUpdate(() => SetExpression(expression.current));
                    }
                    else _behaviorQueue.Add(behavior);
                }

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
                        _speaking = false;
                        _messageInProgress = false;
                        _currentResponse = "";

                        if (expression != null) SetExpression(expression.next);

                        OnMessageVoiceComplete.Invoke(value);
                        OnMessageComplete.Invoke(value);
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
                    behaviors = _connection.Config.Behaviors.Enabled
                },
                OnProgress = (MessageResponse response) => {
                    if (_voice.Enabled && response.voice != null) InvokeOnUpdate(async () => await _voice.Add(response));
                    else emitTextProgress(response);
                },
                OnComplete = (MessageResponse response) => {
                    SmartNPCMessage value = new SmartNPCMessage { message = message, response = _currentResponse, behaviors = behaviors };

                    _messages[_messages.Count - 1] = value;

                    InvokeOnUpdate(() => OnMessageHistoryChange.Invoke(_messages));

                    if (_voice.Enabled) _voice.SetStreamComplete();
                    else
                    {
                        InvokeOnUpdate(() => {
                            _messageInProgress = false;
                            _speaking = false;
                            _currentResponse = "";

                            if (expression != null) SetExpression(expression.next);

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

        public string Expression
        {
            get { return _expression; }
        }
        
        override public void Dispose()
        {
            base.Dispose();
            
            _messages.Clear();
            _behaviorQueue.Dispose();

            _expressionsBlendShapes.Clear();
            _blendShapeIndexes.Clear();
            _blendShapeNames.Clear();
            _blendShapeTargetWeights.Clear();

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
