using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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


        [Header("Speech Recognition")]
        
        [SerializeField] private bool _speechRecognition = true;
        [SerializeField] private KeyCode _holdToRecordKey = KeyCode.LeftControl;
        [SerializeField] private TextMeshProUGUI _recordingTextField;


        [Header("Text Message")]
        
        [SerializeField] private bool _textMessage = true;
        [SerializeField] private KeyCode _sendTextMessageKey = KeyCode.Return;
        

        [Header("Subtitles")]
        [SerializeField] private bool _subtitles = true;
        [SerializeField] private TextMeshProUGUI _subtitlesTextField;
        [SerializeField] private string _subtitlesFormat = "<mark=#000000aa padding=\"20,20,2,2\">%name%: %message%</mark>";
        [SerializeField] private Color _defaultSubtitleColor = Color.white;
        [SerializeField] private Color _rawSpeechColor = Color.yellow;


        [Header("Message History Log")]
        [SerializeField] private bool _messageHistoryLog = false;
        [SerializeField] private TextMeshProUGUI _logTextField;
        [SerializeField] private string _messageFormat = DefaultMessageFormat;
        [SerializeField] private string _errorFormat = DefaultErrorFormat;


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
        public readonly UnityEvent<string> OnMessageHistoryLogChange = new UnityEvent<string>();

        void Awake()
        {
            SetVisibility(false);

            _connection = FindObjectOfType<SmartNPCConnection>();

            if (!_connection) throw new Exception("No SmartNPCConnection found");

            _connection.OnReady(Init);
        }

        private void Init()
        {
            if (_character) AddListeners();

            if (_inputTextField)
            {
                _inputTextField.placeholder.GetComponent<TextMeshProUGUI>().text = GetPlaceholder();

                _inputTextField.enabled = _textMessage;
            }

            if (_recordingTextField) _recordingTextField.raycastTarget = false;

            if (_nameTextField) _nameTextField.raycastTarget = false;
        }

        override protected void Update()
        {
            base.Update();

            if (Input.GetKeyDown(_holdToRecordKey) && _character && !_character.MessageInProgress)
            {
                if (_speechRecognition) _connection.SpeechRecognition.StartRecording();
            }
            else if (Input.GetKeyUp(_holdToRecordKey))
            {
                _connection.SpeechRecognition.StopRecording();
            }

            if (_textMessage && Input.GetKeyDown(_sendTextMessageKey) && _inputTextField.text != "")
            {
                SendMessage(_inputTextField.text);

                _inputTextField.text = "";

                if (_inputTextField) _inputTextField.ActivateInputField(); // maintain focus
            }
        }

        private string GetPlaceholder()
        {
            string result = "";

            if (_speechRecognition)
            {
                result += "Hold [%recordKey%] to Talk".Replace("%recordKey%", GetKeyName(_holdToRecordKey));
            }

            if (_textMessage)
            {
                if (_speechRecognition) result += " or ";

                result += "Type a message and press [%sendKey%] to Send".Replace("%sendKey%", GetKeyName(_sendTextMessageKey));
            }
            
            return result;
        }

        private void SetVisibility(bool visible)
        {
            float alpha = visible ? 1f : 0f;

            if (_nameTextField) _nameTextField.gameObject.GetComponent<CanvasRenderer>().SetAlpha(alpha);

            SetInputTextVisibility(visible, true);
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

            if (_subtitles && _subtitlesTextField)
            {
                _subtitlesTextField.color = rawSpeech ? _rawSpeechColor : _defaultSubtitleColor;
                _subtitlesTextField.text = text;
            }

            OnSubtitlesChange.Invoke(text, rawSpeech);
        }

        private void MessageHistoryLogChange(string text)
        {
            if (_messageHistoryLog && _logTextField) _logTextField.text = text;

            OnMessageHistoryLogChange.Invoke(text);
        }

        private void MessageStart(SmartNPCMessage message)
        {
            SetVisibility(false);

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
           SetVisibility(true);
            
            OnMessageComplete.Invoke(message);

            InvokeUtility.Invoke(this, (Action)(() => {
                if (!_hasRecordingText) SubtitlesChange(_character.Info.name, "", false);
            }), 3);
        }

        private void MessageException(SmartNPCMessage message)
        {
            SetVisibility(true);
            
            OnMessageException.Invoke(message);
        }

        private void MessageHistoryChange(List<SmartNPCMessage> messages)
        {
            OnMessageHistoryChange.Invoke(messages);

            MessageHistoryLogChange(GetMessageLog(_character, messages, _messageFormat, _errorFormat));
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

            SubtitlesChange(_character.Connection.PlayerName, text, true);

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
                SubtitlesChange(_character.Connection.PlayerName, text, false);

                _connection.SpeechRecognition.StopRecording();

                _character.SendMessage(text);
            }
        }

        public void ClearMessageHistory()
        {
            if (_character) _character.ClearMessageHistory();
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

            if (_character.Messages != null)
            {
                MessageHistoryLogChange(GetMessageLog(_character, _character.Messages, _messageFormat, _errorFormat));
            }

            _character.OnReady(OnCharacterReady);

            bool textInvisible = (!_speechRecognition && !_textMessage) || !_character || _character.MessageInProgress;

            SetVisibility(!textInvisible);
        }

        private void OnCharacterReady()
        {
            if (_nameTextField) _nameTextField.text = _character.Info.name;

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

            ResetReady();

            if (_nameTextField) _nameTextField.text = "";

            SetVisibility(false);
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

        public bool SpeechRecognition
        {
            get { return _speechRecognition; }
            
            set {
                if (value == _speechRecognition) return;

                _speechRecognition = value;
            }
        }
        
        override public void Dispose()
        {
            base.Dispose();
            
            RemoveListeners();
        }


        public static string GetMessageLog(
            SmartNPCCharacter character,
            List<SmartNPCMessage> messages,
            string messageFormat = DefaultMessageFormat,
            string errorFormat = DefaultErrorFormat
        )
        {
            return string.Join("\n", messages.ConvertAll<string>(message => GetMessageText(character, message, messageFormat, errorFormat)));
        }

        private static string GetMessageText(SmartNPCCharacter character, SmartNPCMessage message, string messageFormat, string errorFormat)
        {
            string result = FormatMessage(character.Connection.PlayerName, message.message, messageFormat);

            if (message.exception != null) result += " " + errorFormat.Replace("%error%", message.exception);

            if (message.response != null && message.response != "")
            {
                result += "\n" + FormatMessage(character.Info.name, message.response, messageFormat);
            }

            return result;
        }

        private static string FormatMessage(string name, string message, string format)
        {
            return format.Replace("%name%", name).Replace("%message%", message);
        }
    }
}
