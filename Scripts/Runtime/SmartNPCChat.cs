using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace SmartNPC
{
    public class SmartNPCChat : BaseEmitter
    {
        [SerializeField] private SmartNPCCharacter _character;
        [SerializeField] private bool _speechRecognition = false;

        [SerializeField] private TextMeshProUGUI _subtitles;

        [Header("Message History Log")]
        [SerializeField] private TextMeshProUGUI _messageHistoryLog;
        [SerializeField] private string _messageFormat = "[%name%]: %message%";
        [SerializeField] private string _errorFormat = "(Error: %error%)";

        private SmartNPCSpeechRecognition _speechRecognitionInstance;

        private bool _hasRecordingText = false;

        public readonly UnityEvent<SmartNPCMessage> OnMessageStart = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageProgress = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageTextComplete = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageVoiceComplete = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageComplete = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<SmartNPCMessage> OnMessageException = new UnityEvent<SmartNPCMessage>();
        public readonly UnityEvent<List<SmartNPCMessage>> OnMessageHistoryChange = new UnityEvent<List<SmartNPCMessage>>();
        
        public readonly UnityEvent<string, bool> OnSubtitlesChange = new UnityEvent<string, bool>(); // text, raw
        public readonly UnityEvent<string> OnMessageHistoryLogChange = new UnityEvent<string>();

        void Start()
        {
            _speechRecognitionInstance = FindObjectOfType<SmartNPCSpeechRecognition>();

            if (_character) AddListeners();
        }

        private void SubtitlesChange(string text, bool raw)
        {
            if (_subtitles)
            {
                _subtitles.color = raw ? Color.yellow : Color.white;
                _subtitles.text = text;
            }

            OnSubtitlesChange.Invoke(text, false);
        }

        private void MessageHistoryLogChange(string text)
        {
            if (_messageHistoryLog) _messageHistoryLog.text = text;

            OnMessageHistoryLogChange.Invoke(text);
        }

        private void MessageStart(SmartNPCMessage message)
        {
            OnMessageStart.Invoke(message);

            _speechRecognitionInstance.StopRecording();
        }

        private void MessageProgress(SmartNPCMessage message)
        {
            OnMessageProgress.Invoke(message);

            SubtitlesChange(_character.Info.name + ": " + message.response, false);

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

            _speechRecognitionInstance.StartRecording();

            InvokeUtility.Invoke(this, (Action)(() => {
                if (!_hasRecordingText) SubtitlesChange("", false);
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
            SubtitlesChange(_character.Connection.PlayerName + ": " + text, true);

            _hasRecordingText = true;
        }

        private void SpeechRecognitionComplete(string text)
        {
            SubtitlesChange(_character.Connection.PlayerName + ": " + text, false);

            _character.SendMessage(text);
        }

        public new void SendMessage(string text)
        {
            _character.SendMessage(text);
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

            if (_speechRecognition)
            {
                _speechRecognitionInstance.OnProgress.AddListener(SpeechRecognitionProgress);
                _speechRecognitionInstance.OnComplete.AddListener(SpeechRecognitionComplete);

                _speechRecognitionInstance.OnReady(() => _speechRecognitionInstance.StartRecording());
            }
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

            if (_speechRecognition)
            {
                _speechRecognitionInstance.OnProgress.RemoveListener(SpeechRecognitionProgress);
                _speechRecognitionInstance.OnComplete.RemoveListener(SpeechRecognitionComplete);

                _speechRecognitionInstance.StopRecording();
            }
        }

        public SmartNPCCharacter Character
        {
            get { return _character; }
            
            set {
                if (_character) RemoveListeners();
                
                _character = value;

                if (_character) AddListeners();
            }
        }

        public bool SpeechRecognition
        {
            get { return _speechRecognitionInstance; }
        }
        
        override public void Dispose()
        {
            base.Dispose();
            
            RemoveListeners();
        }


        public static string GetMessageLog(
            SmartNPCCharacter character,
            List<SmartNPCMessage> messages,
            string messageFormat = "[%name%]: %message%",
            string errorFormat = "(Error: %error%)"
        )
        {
            return string.Join("\n", messages.ConvertAll<string>(message => GetMessageText(character, message, messageFormat, errorFormat)));
        }

        private static string GetMessageText(SmartNPCCharacter character, SmartNPCMessage message, string messageFormat, string errorFormat)
        {
            string result = GetMessageLine(character.Connection.PlayerName, message.message, messageFormat);

            if (message.exception != null) result += " " + errorFormat.Replace("%error%", message.exception);

            if (message.response != null && message.response != "")
            {
                result += "\n" + GetMessageLine(character.Info.name, message.response, messageFormat);
            }

            return result;
        }

        private static string GetMessageLine(string name, string text, string format)
        {
            return format.Replace("%name%", name).Replace("%message%", text);
        }
    }
}
