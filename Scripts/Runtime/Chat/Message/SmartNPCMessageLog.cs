using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace SmartNPC
{
    public class SmartNPCMessageLog : BaseEmitter
    {
        public const string DefaultMessageFormat = "[%name%]: %message%";
        public const string DefaultErrorFormat = "(Error: %error%)";

        [SerializeField] private SmartNPCCharacter _character;
        [SerializeField] private TextMeshProUGUI _textField;
        [SerializeField] private Scrollbar _scrollbar;
        [SerializeField] private string _messageFormat = DefaultMessageFormat;
        [SerializeField] private string _errorFormat = DefaultErrorFormat;

        private SmartNPCConnection _connection;
        
        public readonly UnityEvent<string> OnMessageHistoryLogChange = new UnityEvent<string>();

        void Awake()
        {
            _textField = GetComponentInChildren<TextMeshProUGUI>();

            if (!_textField) throw new Exception("Must specify a TextField");

            _connection = FindObjectOfType<SmartNPCConnection>();

            if (!_connection) throw new Exception("No SmartNPCConnection found");

            _connection.OnReady(Init);
        }

        private void Init()
        {
            if (_character) AddListeners();
        }

        private void ScrollToBottom()
        {
            _scrollbar.value = 0;
        }

        private void MessageHistoryChange(List<SmartNPCMessage> messages)
        {
            string text = GetMessageLog(_character, messages, _messageFormat, _errorFormat);

            _textField.text = text;

            if (_scrollbar)
            {
                InvokeUtility.Invoke(this, ScrollToBottom, 0.1f);
                InvokeUtility.Invoke(this, ScrollToBottom, 0.5f); // in case the first one didn't work
            }

            OnMessageHistoryLogChange.Invoke(text);
        }

        private void AddListeners()
        {
            _character.OnMessageHistoryChange.AddListener(MessageHistoryChange);

            if (_character.Messages != null) MessageHistoryChange(_character.Messages);

            _character.OnReady(SetReady);
        }

        private void RemoveListeners()
        {
            _character.OnMessageHistoryChange.RemoveListener(MessageHistoryChange);

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
            string result = SmartNPCChat.FormatMessage(character.Connection.PlayerName, message.message, messageFormat);

            if (message.exception != null) result += " " + errorFormat.Replace("%error%", message.exception);

            if (message.response != null && message.response != "")
            {
                result += "\n" + SmartNPCChat.FormatMessage(character.Info.name, message.response, messageFormat);
            }

            return result;
        }
    }
}
