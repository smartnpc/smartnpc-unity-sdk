using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Proyecto26;

namespace SmartNPC
{
    public class Chat
    {
        private Connection _connection;
        private string _characterId;
        private Player _player;

        private List<Action<Message>> OnMessageStartListeners = new List<Action<Message>>();
        private List<Action<Message>> OnMessageProgressListeners = new List<Action<Message>>();
        private List<Action<Message>> OnMessageCompleteListeners = new List<Action<Message>>();
        private List<Action<Message>> OnMessageErrorListeners = new List<Action<Message>>();

        public Chat(Connection connection, string characterId, Player player)
        {
            _connection = connection;
            _characterId = characterId;
            _player = player;
        }

        public void OnMessageStart(Action<Message> listener)
        {
            OnMessageStartListeners.Add(listener);
        }

        public void OnMessageProgress(Action<Message> listener)
        {
            OnMessageProgressListeners.Add(listener);
        }

        public void OnMessageComplete(Action<Message> listener)
        {
            OnMessageCompleteListeners.Add(listener);
        }

        public void OnMessageError(Action<Message> listener)
        {
            OnMessageErrorListeners.Add(listener);
        }

        public void GetMessageHistory(Action<List<Message>> onSuccess)
        {
            _connection.Request<MessageHistoryResponse>(new RequestOptions<MessageHistoryResponse> {
                Method = "GET",
                Uri = "project/" + _connection.Project + "/character/" + _characterId + "/chat/history?playerIdInClient=" + _player.PlayerId,
                OnSuccess = (MessageHistoryResponse response) => onSuccess(response.data),
                OnError = (RequestException error) => {
                    EditorUtility.DisplayDialog("SmartNPC Couldn't get message history", error.Response, "Ok");
                }
            });
        }

        private void DispatchMessageEvent(List<Action<Message>> listeners, Message message) {
            listeners.ForEach((Action<Message> listener) => listener(message));
        }

        public void SendMessage(string message)
        {
            MessageRequestBody body = new MessageRequestBody {
                message = message,
                playerIdInClient = _player.PlayerId,
                playerName = _player.PlayerName
            };

            DispatchMessageEvent(OnMessageStartListeners, new Message { message = message, response = "" });
            DispatchMessageEvent(OnMessageProgressListeners, new Message { message = message, response = "" });

            _connection.Stream(new StreamOptions {
                Method = "POST",
                Uri = "project/" + _connection.Project + "/character/" + _characterId + "/chat/message",
                Body = body,
                OnProgress = (string result, string chunk) => {
                    DispatchMessageEvent(OnMessageProgressListeners, new Message { message = message, response = result, chunk = chunk });
                },
                OnComplete = (string result) => {
                    DispatchMessageEvent(OnMessageProgressListeners, new Message { message = message, response = result });
                },
                OnError = (RequestException error) => {
                    DispatchMessageEvent(OnMessageErrorListeners, new Message { message = message, error = error });
                }
            });
        }
    }
}
