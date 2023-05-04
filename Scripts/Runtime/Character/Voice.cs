using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

namespace SmartNPC
{
    public class Voice : MonoBehaviour
    {
        private List<VoiceMessage> queue = new List<VoiceMessage>();
        private SmartNPCConnection _connection;
        private int index = -1;
        private AudioSource current = null;
        private bool _playing = false;
        private bool _streamComplete = false;
        private bool _complete = false;

       public readonly UnityEvent OnVoiceStart = new UnityEvent();
       public readonly UnityEvent<MessageResponse> OnVoiceProgress = new UnityEvent<MessageResponse>();
       public readonly UnityEvent OnVoiceComplete = new UnityEvent();
       public readonly UnityEvent OnPlayLastChunk = new UnityEvent();

        void Awake()
        {
            _connection = FindObjectOfType<SmartNPCConnection>();
        }

        void Update()
        {
            if (current != null && !current.isPlaying)
            {
                Destroy(current);

                current = null;

                OnFinishPlayChunk();
            }
        }

        public void Dispose()
        {
            OnVoiceStart.RemoveAllListeners();
            OnVoiceProgress.RemoveAllListeners();
            OnVoiceComplete.RemoveAllListeners();
            OnPlayLastChunk.RemoveAllListeners();
        }

        public void Reset()
        {
            index = -1;
            current = null;

            queue.Clear();
            
            _playing = false;
            _streamComplete = false;
            _complete = false;
        }

        public async void Add(MessageResponse response)
        {
            AudioSource voice = await CreateVoice(response.voice);

            queue.Add(new VoiceMessage {
                voice = voice,
                rawResponse = response
            });

            if (index < queue.Count - 1) PlayNext();
        }

        private void PlayNext()
        {
            if (_playing) return;

            VoiceMessage item = queue[++index];

            _playing = true;

            if (index == 0) OnVoiceStart.Invoke();

            OnVoiceProgress.Invoke(item.rawResponse);

            if (_streamComplete && index == queue.Count - 1) OnPlayLastChunk.Invoke();

            current = item.voice;

            current.Play();
        }

        private void OnFinishPlayChunk()
        {
            _playing = false;

            if (index < queue.Count - 1) PlayNext();
            else if (_streamComplete)
            {
                current = null;

                _complete = true;

                OnVoiceComplete.Invoke();
            }
        }

        public void SetStreamComplete()
        {
            _streamComplete = true;

            if (index < queue.Count - 1) PlayNext();
        }

        private async Task<AudioSource> CreateVoice(string base64)
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();

            byte[] audioBytes = Convert.FromBase64String(base64);
            string tempPath = Application.persistentDataPath + System.Guid.NewGuid().ToString();

            File.WriteAllBytes(tempPath, audioBytes);

            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(tempPath, AudioType.MPEG);

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone) await Task.Yield();

            if (request.result.Equals(UnityWebRequest.Result.ConnectionError)) Debug.LogError(request.error);
            else
            {
                audioSource.clip = DownloadHandlerAudioClip.GetContent(request);
                audioSource.volume = Volume;
            }

            File.Delete(tempPath);

            return audioSource;
        }

        public bool Enabled
        {
          get { return _connection.VoiceEnabled; }
        }

        public float Volume
        {
          get { return _connection.VoiceVolume; }
        }

        public bool Playing
        {
          get { return _playing; }
        }

        public bool FinishedPlaying
        {
          get { return _complete; }
        }
    }
}
