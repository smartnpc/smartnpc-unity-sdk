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

        private List<Action<SmartNPCMessage>> OnMessageStartListeners = new List<Action<SmartNPCMessage>>();
        private List<Action<SmartNPCMessage>> OnMessageProgressListeners = new List<Action<SmartNPCMessage>>();
        private List<Action<SmartNPCMessage>> OnMessageCompleteListeners = new List<Action<SmartNPCMessage>>();
        private List<Action<SmartNPCMessage>> OnMessageErrorListeners = new List<Action<SmartNPCMessage>>();

        public SmartNPCChat(SmartNPCConnection connection, string characterId, SmartNPCPlayer player)
        {
            _connection = connection;
            _characterId = characterId;
            _player = player;
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

        public void GetMessageHistory(Action<List<SmartNPCMessage>> onSuccess)
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

        private void DispatchMessageEvent(List<Action<SmartNPCMessage>> listeners, SmartNPCMessage message) {
            listeners.ForEach((Action<SmartNPCMessage> listener) => listener(message));
        }

        public void SendMessage(string message)
        {
            MessageRequestBody body = new MessageRequestBody {
                message = message,
                playerIdInClient = _player.PlayerId,
                playerName = _player.PlayerName
            };

            DispatchMessageEvent(OnMessageStartListeners, new SmartNPCMessage { message = message, response = "" });
            DispatchMessageEvent(OnMessageProgressListeners, new SmartNPCMessage { message = message, response = "" });

            _connection.Stream(new StreamOptions {
                Method = "POST",
                Uri = "project/" + _connection.Project + "/character/" + _characterId + "/chat/message",
                Body = body,
                OnProgress = (string result, string chunk) => {
                    DispatchMessageEvent(OnMessageProgressListeners, new SmartNPCMessage { message = message, response = result, chunk = chunk });
                },
                OnComplete = (string result) => {
                    DispatchMessageEvent(OnMessageProgressListeners, new SmartNPCMessage { message = message, response = result });
                },
                OnError = (RequestException error) => {
                    DispatchMessageEvent(OnMessageErrorListeners, new SmartNPCMessage { message = message, error = error });
                }
            });
        }
    }
}
