using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SmartNPC
{
    public class Voice
    {
        private List<VoiceMessage> queue = new List<VoiceMessage>();
        private int index = -1;
        private AudioSource current = null;
        private bool _playing = false;
        private bool _streamComplete = false;
        private bool _complete = false;
        private SmartNPCCharacter _character;

        public event EventHandler OnVoiceStart;
        public event EventHandler<MessageResponse> OnVoiceProgress;
        public event EventHandler OnVoiceComplete;
        public event EventHandler OnPlayLastChunk;

        public Voice(SmartNPCCharacter character)
        {
            _character = character;
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

            if (index == 0) OnVoiceStart?.Invoke(this, EventArgs.Empty);

            OnVoiceProgress?.Invoke(this, item.rawResponse);

            if (_streamComplete && index == queue.Count - 1) OnPlayLastChunk?.Invoke(this, EventArgs.Empty);

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

                OnVoiceComplete?.Invoke(this, EventArgs.Empty);
            }
        }

        public void CheckFinishedPlayingChunk()
        {
            if (current != null && !current.isPlaying)
            {
                current = null;

                OnFinishPlayChunk();
            }
        }

        public void SetStreamComplete()
        {
            _streamComplete = true;

            if (index < queue.Count - 1) PlayNext();
        }

        private async Task<AudioSource> CreateVoice(string base64)
        {
            AudioSource audioSource = _character.gameObject.AddComponent<AudioSource>();

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
          get { return _character.Connection.VoiceEnabled; }
        }

        public float Volume
        {
          get { return _character.Connection.VoiceVolume; }
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
