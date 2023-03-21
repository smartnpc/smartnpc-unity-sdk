using System;
using System.Collections.Generic;
using UnityEditor;
using Proyecto26;

namespace SmartNPC
{
    public class SmartNPCChat
    {
        private SmartNPCConnection _connection;
        private string _characterId;
        private SmartNPCPlayer _player;
        List<SmartNPCMessage> _messages = new List<SmartNPCMessage>();

        private List<Action<SmartNPCMessage>> OnMessageStartListeners = new List<Action<SmartNPCMessage>>();
        private List<Action<SmartNPCMessage>> OnMessageProgressListeners = new List<Action<SmartNPCMessage>>();
        private List<Action<SmartNPCMessage>> OnMessageCompleteListeners = new List<Action<SmartNPCMessage>>();
        private List<Action<SmartNPCMessage>> OnMessageErrorListeners = new List<Action<SmartNPCMessage>>();
        private List<Action<List<SmartNPCMessage>>> OnReadyListeners = new List<Action<List<SmartNPCMessage>>>();
        private List<Action<List<SmartNPCMessage>>> OnMessagesChangeListeners = new List<Action<List<SmartNPCMessage>>>();

        public void Init(SmartNPCConnection connection, string characterId, SmartNPCPlayer player)
        {
            _connection = connection;
            _characterId = characterId;
            _player = player;

            GetMessageHistory();
        }

        public List<SmartNPCMessage> Messages {
            get {
                return _messages;
            }
        }

        public void OnMessageStart(Action<SmartNPCMessage> listener)
        {
            OnMessageStartListeners.Add(listener);
        }

        public void OnMessageProgress(Action<SmartNPCMessage> listener)
        {
            OnMessageProgressListeners.Add(listener);
        }

        public void OnMessageComplete(Action<SmartNPCMessage> listener)
        {
            OnMessageCompleteListeners.Add(listener);
        }

        public void OnMessageError(Action<SmartNPCMessage> listener)
        {
            OnMessageErrorListeners.Add(listener);
        }

        public void OnMessagesChange(Action<List<SmartNPCMessage>> listener)
        {
            OnMessagesChangeListeners.Add(listener);
        }

        public void OnReady(Action<List<SmartNPCMessage>> listener)
        {
            OnReadyListeners.Add(listener);

            if (IsReady) listener(_messages);
        }

        public bool IsReady {
            get {
                return _messages != null;
            }
        }

        private void GetMessageHistory(RequestCallbacks<List<SmartNPCMessage>> callbacks = null)
        {
            _connection.Request<MessageHistoryResponse>(new RequestOptions<MessageHistoryResponse> {
                Method = "GET",
                Uri = "project/" + _connection.Project + "/character/" + _characterId + "/chat/history?playerIdInClient=" + _player.PlayerId,
                OnSuccess = (MessageHistoryResponse response) => {
                    bool initialized = !IsReady;

                    _messages = response.data;

                    if (callbacks?.OnSuccess != null) callbacks.OnSuccess(response.data);

                    if (initialized) DispatchEvent<List<SmartNPCMessage>>(OnReadyListeners, _messages);

                    DispatchEvent<List<SmartNPCMessage>>(OnMessagesChangeListeners, _messages);
                },
                OnError = callbacks?.OnError
            });
        }

        private void DispatchEvent<T>(List<Action<T>> listeners, T message) {
            listeners.ForEach((Action<T> listener) => listener(message));
        }

        public void SendMessage(string message)
        {
            MessageRequestBody body = new MessageRequestBody {
                message = message,
                playerIdInClient = _player.PlayerId,
                playerName = _player.PlayerName
            };

            SmartNPCMessage newMessage = new SmartNPCMessage { message = message, response = "" };

            _messages.Add(newMessage);

            DispatchEvent<SmartNPCMessage>(OnMessageStartListeners, newMessage);
            DispatchEvent<SmartNPCMessage>(OnMessageProgressListeners, newMessage);
            DispatchEvent<List<SmartNPCMessage>>(OnMessagesChangeListeners, _messages);

            _connection.Stream(new StreamOptions {
                Method = "POST",
                Uri = "project/" + _connection.Project + "/character/" + _characterId + "/chat/message",
                Body = body,
                OnProgress = (string result, string chunk) => {
                    SmartNPCMessage value = new SmartNPCMessage { message = message, response = result, chunk = chunk };

                    _messages[_messages.Count - 1] = value;

                    DispatchEvent<SmartNPCMessage>(OnMessageProgressListeners, value);
                    DispatchEvent<List<SmartNPCMessage>>(OnMessagesChangeListeners, _messages);
                },
                OnComplete = (string result) => {
                    SmartNPCMessage value = new SmartNPCMessage { message = message, response = result };

                    _messages[_messages.Count - 1] = value;

                    DispatchEvent<SmartNPCMessage>(OnMessageCompleteListeners, value);
                    DispatchEvent<List<SmartNPCMessage>>(OnMessagesChangeListeners, _messages);
                },
                OnError = (RequestException error) => {
                    SmartNPCMessage value = new SmartNPCMessage { message = message, error = error };

                    _messages[_messages.Count - 1] = value;

                    DispatchEvent<SmartNPCMessage>(OnMessageErrorListeners, value);
                    DispatchEvent<List<SmartNPCMessage>>(OnMessagesChangeListeners, _messages);
                }
            });
        }
    }
}
