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
        private bool recordingUntilFinishProcessing = false;
        private int frequency = 16000;
        private int microphoneBufferLengthSeconds = 60 * 10; // 10 minutes
        private float collectIntervalSeconds = 1f; // also chunk length in seconds
        
        private int chunkBufferSize;
        private float[] chunkBuffer;
        private AudioClip recordingClip;
        private float collectCounter;
        private int lastPosition;
        private bool processing = false;
        private bool processedFirstChunk = false;
        private string text = "";
        private string _language;

        public readonly UnityEvent<bool> OnStart = new UnityEvent<bool>();
        public readonly UnityEvent OnStartProcessing = new UnityEvent();
        public readonly UnityEvent<string, bool> OnProgress = new UnityEvent<string, bool>();
        public readonly UnityEvent<string> OnFinishing = new UnityEvent<string>();
        public readonly UnityEvent<string> OnComplete = new UnityEvent<string>();
        public readonly UnityEvent OnAbort = new UnityEvent();
        public readonly UnityEvent<string> OnException = new UnityEvent<string>();

        private SpeechRecognitionTester tester;

        public void StartRecording(string language = null, bool recover = false)
        {
            if (recording) return;

            if (!_connection.IsReady) throw new Exception("Connection isn't ready");
            
            recordingClip = Microphone.Start(null, true, microphoneBufferLengthSeconds, frequency);

            lastPosition = Microphone.GetPosition(null);

            recording = true;
            recordingUntilFinishProcessing = false;
            processedFirstChunk = false;
            _language = language;
            text = "";

            collectCounter = 0;

            InvokeOnUpdate(() => OnStart.Invoke(recover));
        }

        public void StopRecording()
        {
            if (!recording || recordingUntilFinishProcessing) return;

            if (!_connection.IsReady) throw new Exception("Connection isn't ready");

            if (processing)
            {
                if (!processedFirstChunk)
                {
                    InvokeOnUpdate(() => OnAbort.Invoke());

                    End();
                }
                else
                {
                    recordingUntilFinishProcessing = true;

                    InvokeOnUpdate(() => OnFinishing.Invoke(text));
                }
            }
            else End();
        }

        private void End()
        {
            Microphone.End(null);

            recordingClip = null;
            recording = false;
            recordingUntilFinishProcessing = false;
            processedFirstChunk = false;
            text = "";
        }

        public void ToggleRecording(string language = null)
        {
            if (recording) StopRecording();
            else StartRecording(language);
        }

        void Awake()
        {
            chunkBufferSize = (int) (collectIntervalSeconds * frequency);
            chunkBuffer = new float[chunkBufferSize];

            SmartNPCConnection.OnInstanceReady(Init);
        }

        private void Init(SmartNPCConnection connection)
        {
            _connection = connection;

            _connection.On("speech", OnSpeechEvent);

            SetReady();
        }

        override protected void Update()
        {
            base.Update();

            if (!_connection || !recording) return;

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

        private void OnSpeechEvent(SocketIOResponse response)
        {
            SpeechRecognitionResponse result = response.GetValue<SpeechRecognitionResponse>();

            if (result.status.Equals(StreamStatus.Start)) {
                processing = true;

                InvokeOnUpdate(() => OnStartProcessing.Invoke());
            }
            else if (result.status == StreamStatus.Progress)
            {
                processedFirstChunk = true;

                text = result.text;

                InvokeOnUpdate(() => {
                    // recover in case aborted before received first response
                    if (!recording)
                    {
                        StartRecording(_language, true);

                        recordingUntilFinishProcessing = true;
                    }

                    OnProgress.Invoke(result.text, recordingUntilFinishProcessing);
                });
            }
            else if (result.status.Equals(StreamStatus.Complete))
            {
                processing = false;

                text = result.text;

                InvokeOnUpdate(() => {
                    OnFinishProcessing();

                    OnComplete.Invoke(result.text);
                });
            }
            else if (result.status.Equals(StreamStatus.Exception))
            {
                processing = false;

                InvokeOnUpdate(() => {
                    OnFinishProcessing();
                    
                    OnException.Invoke(result.exception);
                });
            }
        }

        private void OnFinishProcessing()
        {
            if (recordingUntilFinishProcessing)
            {
                recordingUntilFinishProcessing = false;

                StopRecording();
            }
        }

        private void CollectChunk()
        {
            int currentPosition = Microphone.GetPosition(null);

            if (!recordingClip.GetData(chunkBuffer, lastPosition)) return;

            lastPosition = currentPosition;

            ProcessChunk(chunkBuffer);
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

        private void ProcessChunk(float[] buffer)
        {
            processing = true;

            _connection.Emit("speech", new SpeechRecognitionData { language = _language, data = BufferToBase64(buffer) });
        }

        public bool IsRecording
        {
            get { return recording; }
        }

        public bool IsFinishingRecording
        {
            get { return recordingUntilFinishProcessing; }
        }

        public SmartNPCConnection Connection
        {
            get { return _connection; }
        }
    }
}