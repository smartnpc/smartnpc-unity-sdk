using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

namespace SmartNPC
{
    public class SmartNPCVoice : MonoBehaviour
    {
        private List<VoiceMessage> queue = new List<VoiceMessage>();
        private SmartNPCConnection _connection;
        private int index = -1;
        private bool _playing = false;
        private bool _streamComplete = false;
        private bool _complete = false;
        private AudioSource _audioSource = null;

        public readonly UnityEvent<VoiceMessage> OnVoiceStart = new UnityEvent<VoiceMessage>();
        public readonly UnityEvent<VoiceMessage> OnVoiceProgress = new UnityEvent<VoiceMessage>();
        public readonly UnityEvent<VoiceMessage> OnVoiceComplete = new UnityEvent<VoiceMessage>();
        public readonly UnityEvent<VoiceMessage> OnPlayLastChunk = new UnityEvent<VoiceMessage>();

        void Awake()
        {
            _connection = FindObjectOfType<SmartNPCConnection>();

            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        void Update()
        {
            if (_audioSource != null && _audioSource.clip != null && !_audioSource.isPlaying)
            {
                _audioSource.clip = null;

                OnFinishPlayChunk();
            }
        }

        public void Dispose()
        {
            OnVoiceStart.RemoveAllListeners();
            OnVoiceProgress.RemoveAllListeners();
            OnVoiceComplete.RemoveAllListeners();
            OnPlayLastChunk.RemoveAllListeners();

            Destroy(_audioSource);
        }

        public void Reset()
        {
            index = -1;
            _audioSource.clip = null;

            queue.Clear();
            
            _playing = false;
            _streamComplete = false;
            _complete = false;
        }

        public async Task Add(MessageResponse response)
        {
            AudioClip clip = await CreateVoice(response.voice, AudioType.MPEG);

            queue.Add(new VoiceMessage {
                clip = clip,
                rawResponse = response
            });

            if (index < queue.Count - 1) PlayNext();
        }

        private void PlayNext()
        {
            if (_playing) return;

            VoiceMessage item = queue[++index];

            _playing = true;

            if (index == 0) OnVoiceStart.Invoke(item);

            OnVoiceProgress.Invoke(item);

            if (_streamComplete && index == queue.Count - 1) OnPlayLastChunk.Invoke(item);

            _audioSource.clip = item.clip;

            _audioSource.volume = Volume;

            _audioSource.Play();
        }

        private void OnFinishPlayChunk()
        {
            _playing = false;

            if (index < queue.Count - 1) PlayNext();
            else if (_streamComplete)
            {
                _audioSource.clip = null;

                _complete = true;

                OnVoiceComplete.Invoke(queue[index]);
            }
        }

        public void SetStreamComplete()
        {
            _streamComplete = true;

            if (index < queue.Count - 1) PlayNext();
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

        public AudioSource AudioSource
        {
          get { return _audioSource; }
        }

        static public async Task<AudioClip> CreateVoice(string base64, AudioType audioType)
        {
            AudioClip clip = null;

            byte[] audioBytes = Convert.FromBase64String(base64);
            string tempPath = Application.persistentDataPath + System.Guid.NewGuid().ToString();

            File.WriteAllBytes(tempPath, audioBytes);

            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(tempPath, audioType);

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone) await Task.Yield();

            if (request.result.Equals(UnityWebRequest.Result.ConnectionError)) Debug.LogError(request.error);
            else clip = DownloadHandlerAudioClip.GetContent(request);

            File.Delete(tempPath);

            return clip;
        }
    }
}
