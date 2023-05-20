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


        [Header("Subtitles")]
        [SerializeField] private TextMeshProUGUI _subtitlesTextField;
        [SerializeField] private string _subtitlesFormat = "<mark=#000000aa padding=\"20,20,2,2\">%name%: %message%</mark>";
        [SerializeField] private Color _defaultSubtitleColor = Color.white;
        [SerializeField] private Color _rawSpeechColor = Color.yellow;


        [Header("Message History Log")]
        [SerializeField] private TextMeshProUGUI _logTextField;
        [SerializeField] private string _messageFormat = DefaultMessageFormat;
        [SerializeField] private string _errorFormat = DefaultErrorFormat;


        [Header("Speech Recognition")]
        [SerializeField] private bool _speechRecognition = true;
        [SerializeField] private TextMeshProUGUI _recordingTextField;
        [SerializeField] private string _recordingFormat = "<mark=#000000aa padding=\"20,20,2,2\">%content%</mark>";
        [SerializeField] private KeyCode _holdToRecordKey = KeyCode.LeftControl;
        [SerializeField] private Color _holdToRecordColor = Color.white;
        [SerializeField] private Color _recordingColor = Color.red;


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
            _connection = FindObjectOfType<SmartNPCConnection>();

            _connection.OnReady(Init);
        }

        private void Init()
        {
            if (_character) AddListeners();
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

            if (_recordingTextField)
            {
                if (!_speechRecognition || _character && _character.MessageInProgress) SetRecordingText(_holdToRecordColor, "");
                else if (_speechRecognition)
                {
                    if (_connection.SpeechRecognition.IsRecording)
                    {
                        if (_connection.SpeechRecognition.IsFinishingRecording) SetRecordingText(_recordingColor, "Finishing Recording...");
                        else SetRecordingText(_recordingColor, "Recording");
                    }
                    else {
                        String keyName = Regex.Replace(_holdToRecordKey.ToString(), "((?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z]))", " $1").Trim();
                        
                        SetRecordingText(_holdToRecordColor, "Hold [%key%] to Talk".Replace("%key%", keyName));
                    }
                }
            }
        }

        private void SetRecordingText(Color color, string text)
        {
            _recordingTextField.color = color;
            _recordingTextField.text = _recordingFormat.Replace("%content%", text);
        }

        private void SubtitlesChange(string name, string message, bool rawSpeech)
        {
            string text = message != "" ? FormatMessage(name, message, _subtitlesFormat) : "";

            if (_subtitlesTextField)
            {
                _subtitlesTextField.color = rawSpeech ? _rawSpeechColor : _defaultSubtitleColor;
                _subtitlesTextField.text = text;
            }

            OnSubtitlesChange.Invoke(text, rawSpeech);
        }

        private void MessageHistoryLogChange(string text)
        {
            if (_logTextField) _logTextField.text = text;

            OnMessageHistoryLogChange.Invoke(text);
        }

        private void MessageStart(SmartNPCMessage message)
        {
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
            OnMessageComplete.Invoke(message);

            InvokeUtility.Invoke(this, (Action)(() => {
                if (!_hasRecordingText) SubtitlesChange(_character.Info.name, "", false);
            }), 3);
        }

        private void MessageException(SmartNPCMessage message)
        {
            OnMessageException.Invoke(message);
        }

        private void MessageHistoryChange(List<SmartNPCMessage> messages)
        {
            OnMessageHistoryChange.Invoke(messages);

            MessageHistoryLogChange(GetMessageLog(_character, messages, _messageFormat, _errorFormat));
        }

        private void SpeechRecognitionProgress(string text)
        {
            SubtitlesChange(_character.Connection.PlayerName, text, true);

            _hasRecordingText = true;
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

            _connection.SpeechRecognition.OnProgress.AddListener(SpeechRecognitionProgress);
            _connection.SpeechRecognition.OnComplete.AddListener(SendMessage);

            if (_character.Messages != null)
            {
                MessageHistoryLogChange(GetMessageLog(_character, _character.Messages, _messageFormat, _errorFormat));
            }

            _character.OnReady(SetReady);
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

            _connection.SpeechRecognition.OnProgress.RemoveListener(SpeechRecognitionProgress);
            _connection.SpeechRecognition.OnComplete.RemoveListener(SendMessage);

            ResetReady();
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
