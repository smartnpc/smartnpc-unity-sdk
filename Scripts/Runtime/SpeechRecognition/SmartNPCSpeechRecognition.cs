using UnityEngine;
using System;
using System.IO;
using SocketIOClient;
using UnityEngine.Events;

namespace SmartNPC
{
    public class SmartNPCSpeechRecognition : BaseEmitter
    {
        private SmartNPCConnection _connection;
        private bool recording = false;
        private int frequency = 16000;
        private int microphoneBufferLengthSeconds = 60 * 10; // 10 minutes
        private float collectIntervalSeconds = 1f; // also chunk length in seconds
        
        private int chunkBufferSize;
        private float[] chunkBuffer;
        private AudioClip recordingClip;
        private float collectCounter;
        private int lastPosition;
        private bool processing = false;

        public readonly UnityEvent OnStart = new UnityEvent();
        public readonly UnityEvent<string> OnProgress = new UnityEvent<string>();
        public readonly UnityEvent<string> OnComplete = new UnityEvent<string>();
        public readonly UnityEvent<string> OnException = new UnityEvent<string>();

        private SpeechRecognitionTester tester;

        public void StartRecording()
        {
            if (recording) return;

            if (!_connection.IsReady) throw new Exception("Connection isn't ready");

            recordingClip = Microphone.Start(null, true, microphoneBufferLengthSeconds, frequency);

            lastPosition = Microphone.GetPosition(null);

            recording = true;

            collectCounter = 0;
        }

        public void StopRecording()
        {
            if (!recording) return;

            if (!_connection.IsReady) throw new Exception("Connection isn't ready");

            Microphone.End(null);

            recordingClip = null;

            recording = false;
        }

        public void ToggleRecording()
        {
            if (recording) StopRecording();
            else StartRecording();
        }

        void Awake()
        {
            chunkBufferSize = (int) (collectIntervalSeconds * frequency);
            chunkBuffer = new float[chunkBufferSize];

            _connection = FindObjectOfType<SmartNPCConnection>();

            _connection.OnReady(Init);
        }

        private void Init() {
            _connection.On("speech", OnSpeechEvent);

            SetReady();
        }

        override protected void Update()
        {
            base.Update();

            if (!recording) return;

            if (collectCounter <= 0)
            {
                collectCounter = collectIntervalSeconds;

                CollectChunk();
            }
            
            collectCounter -= Time.deltaTime;
        }

        override public void Dispose()
        {
            base.Dispose();

            Microphone.End(null);

            _connection.Off("speech", OnSpeechEvent);
        }

        private void OnSpeechEvent(SocketIOResponse response) {
            SpeechRecognitionResponse result = response.GetValue<SpeechRecognitionResponse>();

            if (result.status.Equals(StreamStatus.Start)) InvokeOnUpdate(() => OnStart.Invoke());
            else if (result.status == StreamStatus.Progress)
            {
                processing = true;

                InvokeOnUpdate(() => OnProgress.Invoke(result.text));
            }
            else if (result.status.Equals(StreamStatus.Complete))
            {
                processing = false;

                InvokeOnUpdate(() => OnComplete.Invoke(result.text));
            }
            else if (result.status.Equals(StreamStatus.Exception))
            {
                processing = false;

                InvokeOnUpdate(() => OnException.Invoke(result.exception));
            }
        }

        private void CollectChunk()
        {
            int currentPosition = Microphone.GetPosition(null);

            if (!recordingClip.GetData(chunkBuffer, lastPosition)) return;

            lastPosition = currentPosition;

            if (processing || !IsSilent(chunkBuffer)) ProcessChunk(chunkBuffer);
        }

        private byte[] BufferToBytes(float[] buffer)
        {
            byte[] bytes = new byte[buffer.Length * 2];
            int index = 0;
            
            foreach (float sample in buffer)
            {
                short convertedSample = (short) (sample * short.MaxValue);

                BitConverter.GetBytes(convertedSample).CopyTo(bytes, index);

                index += 2;
            }

            return bytes;
        }

        private byte[] BytesToWavBytes(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                    writer.Write(36 + bytes.Length);
                    writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
                    writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                    writer.Write(16);
                    writer.Write((ushort) 1);
                    writer.Write((ushort) recordingClip.channels);
                    writer.Write(frequency);
                    writer.Write(frequency * recordingClip.channels * 2);
                    writer.Write((ushort) (recordingClip.channels * 2));
                    writer.Write((ushort) 16);
                    writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                    writer.Write(bytes.Length);
                    writer.Write(bytes);
                }

                return stream.ToArray();
            }
        }

        private string BufferToBase64(float[] buffer)
        {
            byte[] bytes = BufferToBytes(buffer);
            byte[] wavBytes = BytesToWavBytes(bytes);

            return Convert.ToBase64String(wavBytes);
        }

        private float DecibelsFromBuffer(float[] buffer)
        {
            float max = 0;

            for (int i = 0; i < buffer.Length; i++)
            {
                float value = buffer[i] * buffer[i];

                if (max < value) max = value;
            }

            return 20 * Mathf.Log10(Mathf.Abs(max));
        }

        private bool IsSilent(float[] buffer)
        {
            return DecibelsFromBuffer(buffer) < _connection.SpeechRecognitionMinimumDecibels;
        }

        private void ProcessChunk(float[] buffer)
        {
            _connection.Emit("speech", new SpeechRecognitionData { data = BufferToBase64(buffer) });
        }

        public bool IsRecording
        {
            get { return recording; }
        }
    }
}