using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;
using System.Text.RegularExpressions;

namespace SmartNPC
{
    public class SmartNPCChat : BaseEmitter
    {
        public const string DefaultMessageFormat = "[%name%]: %message%";
        public const string DefaultErrorFormat = "(Error: %error%)";

        [SerializeField] private SmartNPCCharacter _character;
        [SerializeField] private TextMeshProUGUI _nameTextField;
        [SerializeField] private TMP_InputField _inputTextField;

        [SerializeField]
        [Tooltip("Hold this key to speak to press it to type")]
        private KeyCode _inputKey = KeyCode.LeftControl;


        [Header("Auto Target")]

        [SerializeField]
        [Tooltip("Automatically set to the character that the defined origin is looking at")]
        private SmartNPCChatTargetOrigin _targetOrigin = SmartNPCChatTargetOrigin.None;
        [SerializeField] private int _maxDistance = 5;


        [Header("Speech Recognition")]
        
        [SerializeField] private bool _speechRecognitionEnabled = true;
        [SerializeField] private TextMeshProUGUI _recordingTextField;


        [Header("Text Message")]
        
        [SerializeField] private bool _textMessageEnabled = true;
        [SerializeField] private KeyCode _sendTextMessageKey = KeyCode.Return;
        

        [Header("Subtitles")]
        [SerializeField] private bool _subtitlesEnabled = true;
        [SerializeField] private TextMeshProUGUI _subtitlesTextField;
        [SerializeField] private string _subtitlesFormat = "<mark=#000000aa padding=\"20,20,2,2\">%name%: %message%</mark>";
        [SerializeField] private Color _defaultSubtitleColor = Color.white;
        [SerializeField] private Color _rawSpeechColor = Color.yellow;


        private SmartNPCConnection _connection;

        private bool _hasRecordingText = false;

        public readonly UnityEvent<SmartNPCMessage> OnMessageStart = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageProgress = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageTextComplete = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageVoiceComplete = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageComplete = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageException = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<List<SmartNPCMessage>> OnMessageHistoryChange = new UnityEvent<List<SmartNPCMessage>>();
        
        public readonly UnityEvent<string, bool> OnSubtitlesChange = new UnityEvent<string, bool>();

        void Awake()
        {
            InitInput();

            if (_inputTextField) _inputTextField.enabled = _textMessageEnabled;

            if (_recordingTextField) _recordingTextField.raycastTarget = false;

            if (_nameTextField) _nameTextField.raycastTarget = false;

            SetVisibility(false);
            
            SmartNPCConnection.OnInstanceReady(Init);
        }

        private void Init(SmartNPCConnection connection)
        {
            _connection = connection;

            if (_character) AddListeners();
        }

        private void InitInput()
        {
            if (!FindObjectOfType<EventSystem>())
            {
                gameObject.AddComponent<EventSystem>();

                if (!FindObjectOfType<StandaloneInputModule>()) gameObject.AddComponent<StandaloneInputModule>();
                if (!FindObjectOfType<BaseInput>()) gameObject.AddComponent<BaseInput>();
            }
        }

        override protected void Update()
        {
            base.Update();

            if (!_connection) return;

            try {
                SetPlaceholder();
                ProcessInput();
                SetTarget();
            }
            catch (Exception e) {
                Debug.LogError(e);
            }
        }

        private void ProcessInput()
        {
            if (Input.GetKeyDown(_inputKey) && _character && !_character.MessageInProgress)
            {
                if (_speechRecognitionEnabled && _textMessageEnabled) InvokeUtility.Invoke(this, StartRecordingOrToggleInputFocus, 0.2f);
                else if (_speechRecognitionEnabled) _connection.SpeechRecognition.StartRecording(_character.Info.language);
                else if (_textMessageEnabled) ToggleInputFocus();
            }
            else if (Input.GetKeyUp(_inputKey))
            {
                _connection.SpeechRecognition.StopRecording();
            }

            if (_textMessageEnabled && Input.GetKeyDown(_sendTextMessageKey) && _inputTextField && _inputTextField.text != "")
            {
                SendMessage(_inputTextField.text);

                _inputTextField.text = "";
            }
        }

        private void StartRecordingOrToggleInputFocus()
        {
            if (Input.GetKey(_inputKey)) _connection.SpeechRecognition.StartRecording(_character.Info.language);
            else ToggleInputFocus();
        }

        private void ToggleInputFocus()
        {
            if (!_inputTextField) return;

            if (_inputTextField.isFocused) DeactivateInputField();
            else _inputTextField.ActivateInputField();
        }

        private void DeactivateInputField()
        {
            // workaround. _inputTextField.DeactivateInputField() and EventSystem.current.SetSelectedGameObject(null) don't work
            _inputTextField.interactable = false;

            InvokeUtility.Invoke(this, EnableInputField, 0.2f);
        }

        private void EnableInputField()
        {
            _inputTextField.interactable = true;
        }

        private void SetPlaceholder()
        {
            if (!_inputTextField) return;
            
            _inputTextField.placeholder.GetComponent<TextMeshProUGUI>().text = _inputTextField.isFocused ? GetTypingPlaceholder() : GetIdlePlaceholder();
        }

        private string GetIdlePlaceholder()
        {
            string result = "";

            if (_speechRecognitionEnabled)
            {
                result += "Hold [%inputKey%] to Speak".Replace("%inputKey%", GetKeyName(_inputKey));
            }

            if (_textMessageEnabled)
            {
                if (_speechRecognitionEnabled) result += " or press it to Type";
                else result += "Press [%inputKey%] to Type".Replace("%inputKey%", GetKeyName(_inputKey));
            }
            
            return result;
        }

        private string GetTypingPlaceholder()
        {
            return "Type a message and press [%sendTextMessageKey%] to Send".Replace("%sendTextMessageKey%", GetKeyName(_sendTextMessageKey));
        }

        private void SetVisibility(bool visible)
        {
            float alpha = visible ? 1f : 0f;

            if (_nameTextField) _nameTextField.gameObject.GetComponent<CanvasRenderer>().SetAlpha(alpha);

            SetInputTextVisibility(visible, true);
        }

        private void SetActive(bool active)
        {
            float alpha = active ? 1f : 0.5f;

            if (_nameTextField) _nameTextField.gameObject.GetComponent<CanvasRenderer>().SetAlpha(alpha);

            if (_inputTextField) _inputTextField.interactable = active;
        }

        private void SetInputTextVisibility(bool visible, bool includeBackground = false)
        {
            if (!_inputTextField) return;

            float alpha = visible ? 1f : 0f;

            _inputTextField.enabled = visible; // must be before setting alpha

            if (includeBackground) _inputTextField.gameObject.GetComponent<CanvasRenderer>().SetAlpha(alpha);

            TextMeshProUGUI[] children = _inputTextField.GetComponentsInChildren<TextMeshProUGUI>();

            for (int i = 0; i < children.Length; i++)
            {
                children[i].gameObject.GetComponent<CanvasRenderer>().SetAlpha(alpha);
            }
        }

        private void SetRecordingText(string value)
        {
            SetInputTextVisibility(value == "");

            _recordingTextField.text = value;
        }

        private string GetKeyName(KeyCode keyCode)
        {
            return Regex.Replace(keyCode.ToString(), "((?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z]))", " $1").Trim();
        }

        private void SubtitlesChange(string name, string message, bool rawSpeech)
        {
            string text = message != "" ? FormatMessage(name, message, _subtitlesFormat) : "";

            if (_subtitlesEnabled && _subtitlesTextField)
            {
                _subtitlesTextField.color = rawSpeech ? _rawSpeechColor : _defaultSubtitleColor;
                _subtitlesTextField.text = text;
            }

            OnSubtitlesChange.Invoke(text, rawSpeech);
        }

        private void MessageStart(SmartNPCMessage message)
        {
            SetActive(false);

            OnMessageStart.Invoke(message);
        }

        private void MessageProgress(SmartNPCMessage message)
        {
            OnMessageProgress.Invoke(message);

            SubtitlesChange(_character.Info.name, message.response, false);

            _hasRecordingText = false;
        }

        private void MessageTextComplete(SmartNPCMessage message)
        {
            OnMessageTextComplete.Invoke(message);
        }

        private void MessageVoiceComplete(SmartNPCMessage message)
        {
            OnMessageVoiceComplete.Invoke(message);
        }

        private void MessageComplete(SmartNPCMessage message)
        {
           SetActive(true);
            
            OnMessageComplete.Invoke(message);

            InvokeUtility.Invoke(this, (Action)(() => {
                if (!_hasRecordingText) SubtitlesChange(_character ? _character.Info.name : "", "", false);
            }), 3);
        }

        private void MessageException(SmartNPCMessage message)
        {
            SetActive(true);
            
            OnMessageException.Invoke(message);
        }

        private void MessageHistoryChange(List<SmartNPCMessage> messages)
        {
            OnMessageHistoryChange.Invoke(messages);
        }

        private void SpeechRecognitionStart(bool recover)
        {
            _inputTextField.enabled = false;

            SetRecordingText("Recording");
        }

        private void SpeechRecognitionProgress(string text, bool finishing)
        {
            string value = text;

            if (finishing) value += "...";

            SetRecordingText(value);

            SubtitlesChange(_character.Connection.UserName, text, true);

            _hasRecordingText = true;
        }

        private void SpeechRecognitionFinishing(string text)
        {
            string value;

            if (text != "") value = text + "...";
            else value = "Finishing Recording...";

            SetRecordingText(value);
        }

        private void SpeechRecognitionComplete(string text)
        {
            _inputTextField.enabled = true;

            SetRecordingText("");
            SendMessage(text);
        }

        private void SpeechRecognitionAbort()
        {
            _inputTextField.enabled = true;

            SetRecordingText("");
        }

        private void SpeechRecognitionException(string exception)
        {
            SpeechRecognitionAbort();
        }

        public new void SendMessage(string text)
        {
            if (_character) {
                SubtitlesChange(_character.Connection.UserName, text, false);

                _connection.SpeechRecognition.StopRecording();

                _character.SendMessage(text);
            }
        }

        public void ClearMessageHistory()
        {
            if (_character) _character.ClearMessageHistory();
        }

        private void SetTarget()
        {
            if (_targetOrigin == SmartNPCChatTargetOrigin.Camera) Character = GetCameraTarget();
            else if (_targetOrigin == SmartNPCChatTargetOrigin.Player) Character = GetPlayerTarget();
        }

        private SmartNPCCharacter GetCameraTarget()
        {
            if (!Camera.main) throw new Exception("Auto Target: No main camera found");

            Vector3 cameraCenter = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, Camera.main.nearClipPlane));

            return GetTarget(cameraCenter, Camera.main.transform.forward);
        }

        private SmartNPCCharacter GetPlayerTarget()
        {
            SmartNPCPlayer player = FindObjectOfType<SmartNPCPlayer>();

            if (!player)  throw new Exception("Auto Target: No SmartNPCPlayer found");

            Collider collider = player.GetComponent<Collider>();

            if (!collider) throw new Exception("Auto Target: SmartNPCPlayer must have a collider");

            return GetTarget(collider.bounds.center, player.transform.forward);
        }

        private SmartNPCCharacter GetTarget(Vector3 origin, Vector3 direction)
        {
            RaycastHit hit;

            if (Physics.SphereCast(origin, 1f, direction, out hit, _maxDistance))
            {
                GameObject target = hit.transform.gameObject;

                SmartNPCCharacter character = target.GetComponent<SmartNPCCharacter>();

                if (character && character.IsReady) return character;
            }

            return null;
        }

        private void AddListeners()
        {
            _character.OnMessageStart.AddListener(MessageStart);
            _character.OnMessageProgress.AddListener(MessageProgress);
            _character.OnMessageTextComplete.AddListener(MessageTextComplete);
            _character.OnMessageVoiceComplete.AddListener(MessageVoiceComplete);
            _character.OnMessageComplete.AddListener(MessageComplete);
            _character.OnMessageException.AddListener(MessageException);
            _character.OnMessageHistoryChange.AddListener(MessageHistoryChange);

            _connection.SpeechRecognition.OnStart.AddListener(SpeechRecognitionStart);
            _connection.SpeechRecognition.OnProgress.AddListener(SpeechRecognitionProgress);
            _connection.SpeechRecognition.OnFinishing.AddListener(SpeechRecognitionFinishing);
            _connection.SpeechRecognition.OnComplete.AddListener(SpeechRecognitionComplete);
            _connection.SpeechRecognition.OnAbort.AddListener(SpeechRecognitionAbort);
            _connection.SpeechRecognition.OnException.AddListener(SpeechRecognitionException);

            _character.OnReady(OnCharacterReady);
        }

        private void OnCharacterReady()
        {
            if (!_character) return;
            
            if (_character.Messages != null) OnMessageHistoryChange.Invoke(_character.Messages);

            if (_nameTextField) _nameTextField.text = _character.Info.name;

            SetVisibility(true);
            SetActive(!_character.MessageInProgress);
            SubtitlesChange(_character.Info.name, _character.CurrentResponse, false);
            
            SetReady();
        }

        private void RemoveListeners()
        {
            _hasRecordingText = false;

            _character.OnMessageStart.RemoveListener(MessageStart);
            _character.OnMessageProgress.RemoveListener(MessageProgress);
            _character.OnMessageTextComplete.RemoveListener(MessageTextComplete);
            _character.OnMessageVoiceComplete.RemoveListener(MessageVoiceComplete);
            _character.OnMessageComplete.RemoveListener(MessageComplete);
            _character.OnMessageException.RemoveListener(MessageException);
            _character.OnMessageHistoryChange.RemoveListener(MessageHistoryChange);

            _connection.SpeechRecognition.OnStart.RemoveListener(SpeechRecognitionStart);
            _connection.SpeechRecognition.OnProgress.RemoveListener(SpeechRecognitionProgress);
            _connection.SpeechRecognition.OnFinishing.RemoveListener(SpeechRecognitionFinishing);
            _connection.SpeechRecognition.OnComplete.RemoveListener(SpeechRecognitionComplete);
            _connection.SpeechRecognition.OnAbort.RemoveListener(SpeechRecognitionAbort);
            _connection.SpeechRecognition.OnException.RemoveListener(SpeechRecognitionException);


            if (_nameTextField) _nameTextField.text = "";

            SetVisibility(false);
            SubtitlesChange(_character && _character.Info ? _character.Info.name : "", "", false);

            ResetReady();
        }

        public bool InputTextFieldFocused
        {
            get { return _inputTextField.isFocused; }
        }

        public bool IsRecording
        {
            get { return _connection.SpeechRecognition && _connection.SpeechRecognition.IsRecording; }
        }

        public bool CharacterSpeaking
        {
            get { return _character && _character.Speaking; }
        }

        public bool CharacterMessageInProgress
        {
            get { return _character && _character.MessageInProgress; }
        }

        public bool InteractionInProgress
        {
            get { return InputTextFieldFocused || IsRecording || CharacterMessageInProgress; }
        }



        public SmartNPCConnection Connection
        {
            get { return _connection; }
        }

        public SmartNPCCharacter Character
        {
            get { return _character; }
            
            set {
                if (value == _character) return;

                if (_character) RemoveListeners();
                
                _character = value;

                if (_character) AddListeners();
            }
        }

        public SmartNPCChatTargetOrigin TargetOrigin
        {
            get { return _targetOrigin; }
            
            set { _targetOrigin = value; }
        }

        public bool SpeechRecognition
        {
            get { return _speechRecognitionEnabled; }
            
            set {
                if (value == _speechRecognitionEnabled) return;

                _speechRecognitionEnabled = value;
            }
        }
        
        override public void Dispose()
        {
            base.Dispose();
            
            RemoveListeners();
        }
        

        public static string FormatMessage(string name, string message, string format)
        {
            return format.Replace("%name%", name).Replace("%message%", message);
        }
    }
}
