using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Proyecto26;

namespace SmartNPC
{
    public class SmartNPCConnection : MonoBehaviour
    {
        private const string DEFAULT_HOST = "https://api.smartnpc.ai/v1";

        [Header("Credentials")]

        [SerializeField]
        private string _keyId;

        [SerializeField]
        private string _publicKey;

        [Header("Advanced Settings")]

        [SerializeField]
        private string _host;

        private string token;
        private Token parsedToken;
        
        private List<Action> OnReadyListeners = new List<Action>();
        private List<Action<RequestException>> OnErrorListeners = new List<Action<RequestException>>();

        void Start()
        {
            if (_keyId == "" || _publicKey == "")
            {
                throw new Exception("Must specify Key Id and Public Key");
            }

            Auth();
        }

        public void OnReady(Action listener)
        {
            OnReadyListeners.Add(listener);

            if (IsReady) listener();
        }

        public void OnError(Action<RequestException> listener)
        {
            OnErrorListeners.Add(listener);
        }

        public string Project {
            get {
                return parsedToken?.project;
            }
        }

        public bool IsReady {
            get {
                return token != null;
            }
        }

        public void Request<T>(RequestOptions<T> options) {
            string host = _host != "" ? _host : DEFAULT_HOST;

            Dictionary<string, string> headers = new Dictionary<string, string>();

            if (token != null) headers["Authorization"] = "Bearer " + token;

            RestClient.Request<T>(new RequestHelper { 
                Uri = host + "/" + options.Uri,
                Method = options.Method,
                Params = options.Params,
                Body = options.Body,
                Headers = headers
            })
            .Then(options.OnSuccess)
            .Catch(error => {
                if (options.OnError != null) options.OnError(error as RequestException);
                else throw error;
            });
        }

        public void Stream(StreamOptions options)
        {
            string host = _host != "" ? _host : DEFAULT_HOST;

            Dictionary<string, string> headers = new Dictionary<string, string>();

            if (token != null) headers["Authorization"] = "Bearer " + token;

            RestClient.Request(new RequestHelper { 
                Uri = host + "/" + options.Uri,
                Method = options.Method,
                Params = options.Params,
                Body = options.Body,
                Headers = headers,
                DownloadHandler = (DownloadHandler) new StreamDownloadHandler( new StreamDownloadHandlerOptions {
                    OnProgress = options.OnProgress,
                    OnComplete = options.OnComplete
                })
            })
            .Catch(error => {
                if (options.OnError != null) options.OnError(error as RequestException);
                else throw error;
            });
        }

        private void Auth()
        {
            AuthRequestBody body = new AuthRequestBody {
                keyId = _keyId,
                publicKey = _publicKey
            };

            Request<AuthResponse>(new RequestOptions<AuthResponse> {
                Method = "POST",
                Uri = "key/auth",
                Body = body,
                OnSuccess = (AuthResponse response) => {
                    token = response.token;
                    parsedToken = JsonUtility.FromJson<Token>( DecodeJWT(response.token) );

                    OnReadyListeners.ForEach((Action listener) => listener());
                },
                OnError = (RequestException error) => {
                    OnErrorListeners.ForEach((Action<RequestException> listener) => listener(error));
                }
            });
        }

        private string DecodeJWT(string token)
        {
            var parts = token.Split('.');

            if (parts.Length > 2)
            {
                var decode = parts[1];
                var padLength = 4 - decode.Length % 4;

                if (padLength < 4) decode += new string('=', padLength);
                
                var bytes = System.Convert.FromBase64String(decode);

                return System.Text.ASCIIEncoding.ASCII.GetString(bytes);
            }

            return "";
        }
    }
}
