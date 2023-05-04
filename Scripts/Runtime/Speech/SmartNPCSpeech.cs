using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;

namespace SmartNPC
{
    public class SmartNPCSpeech : BaseEmitter
    {
        private SmartNPCConnection _connection;
        private bool recording = false;
        private int frequency = 16000;
        private int microphoneBufferLengthSeconds = 60 * 10; // 10 minutes
        private float collectIntervalSeconds = 1f; // also chunk length in seconds
        
        private readonly List<float[]> chunks = new List<float[]>();
        private int chunkBufferSize;
        private float[] chunkBuffer;
        private AudioClip recordingClip;
        private float collectCounter;
        private int lastPosition;

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

            CollectChunk(true);

            recordingClip = null;

            recording = false;
        }

        void Awake()
        {
             // * 1.25 to make sure there's enough space
             // mic position doesn't have accurately fixed increments so the size between chunks varies a bit
            chunkBufferSize = (int) (collectIntervalSeconds * frequency * 1.25);
            chunkBuffer = new float[chunkBufferSize];

            _connection = FindObjectOfType<SmartNPCConnection>();

            _connection.OnReady(SetReady);
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

        void OnDestroy()
        {
            Microphone.End(null);

            chunks.Clear();
        }

        private void CollectChunk(bool force = false)
        {
            int currentPosition = Microphone.GetPosition(null);

            if (!recordingClip.GetData(chunkBuffer, lastPosition)) return;

            lastPosition = currentPosition;

            if (force || IsSilent(chunkBuffer))
            {
                if (chunks.Count > 0)
                {
                    ProcessChunks();

                    chunks.Clear();

                    Microphone.End(null);

                    if (!force)
                    {
                        recordingClip = Microphone.Start(null, true, microphoneBufferLengthSeconds, frequency);

                        lastPosition = Microphone.GetPosition(null);
                    }
                }
            }
            else if (!force)
            {
                float[] floatBufferClone = (float[]) chunkBuffer.Clone();

                chunks.Add(floatBufferClone);
            }
        }

        private byte[] RecordedDataToBytes(float[] data)
        {
            byte[] bytes = new byte[data.Length * 2];
            int index = 0;
            
            foreach (float sample in data)
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

        private string RecordedDataToBase64(float[] data)
        {
            byte[] bytes = RecordedDataToBytes(data);
            byte[] wavBytes = BytesToWavBytes(bytes);

            return Convert.ToBase64String(wavBytes);
        }

        private float DecibelsFromData(float[] data)
        {
            float max = 0;

            for (int i = 0; i < data.Length; i++)
            {
                float value = data[i] * data[i];

                if (max < value) max = value;
            }

            return 20 * Mathf.Log10(Mathf.Abs(max));
        }

        private bool IsSilent(float[] data)
        {
            return DecibelsFromData(data) < -40;
        }

        private string AudioClipToBase64(AudioClip audioClip)
        {
            float[] data = new float[audioClip.samples * audioClip.channels];

            audioClip.GetData(data, 0);

            return RecordedDataToBase64(data);
        }

        private void ProcessChunks()
        {
            List<string> data = chunks.ConvertAll<string>(chunk => RecordedDataToBase64(chunk) );

            Debug.Log("Process chunks: " + chunks.Count);

            _connection.Fetch<SpeechResponse>(new FetchOptions<SpeechResponse> {
                EventName = "speech",
                Data = new SpeechData { data = data },
                OnSuccess = (response) => {
                    Debug.Log("response: " + response);
                },
                OnException = (response) => {
                    throw new Exception("Couldn't process speech");
                }
            });
        }

        public bool IsRecording
        {
            get { return recording; }
        }

        // helper methods, shouldn't be invoked in production

        private void PlayBuffer(float[] floatBuffer)
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            
            AudioClip clip = AudioClip.Create("chunk_" + System.Guid.NewGuid().ToString(), floatBuffer.Length, recordingClip.channels, frequency, false);

            clip.SetData(floatBuffer, 0);

            audioSource.clip = clip;
            
            audioSource.Play();
        }

        private void PlayChunkBuffers()
        {
            int index = 0;

            List<float[]> chunksClone = new List<float[]>(chunks);

            chunksClone.ForEach(chunk => {
                InvokeUtility.Invoke(this, () => {
                    Debug.Log("Play chunk buffer: " + SumBufferData(chunk));

                    PlayBuffer(chunk);
                }, index++);
            });
        }

        private void PlayChunkVoices()
        {
            int index = 0;

            List<float[]> chunksClone = new List<float[]>(chunks);

            chunksClone.ForEach(chunk => {
                InvokeUtility.Invoke(this, async () => {
                    Debug.Log("Play chunk voice: " + SumBufferData(chunk));

                    AudioSource audioSource = await Voice.CreateVoice(gameObject, RecordedDataToBase64(chunk), AudioType.WAV);

                    audioSource.Play();
                }, index++);
            });
        }

        private void PlayFullRecording()
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.clip = recordingClip;

            audioSource.Play();
        }

        private float SumBufferData(float[] data)
        {
            float sum = 0;

            for (int i = 0; i < data.Length; i++)
            {
                float value = data[i] * data[i];

                sum += value;
            }

            return sum;
        }
    }

    public static class InvokeUtility
    {
        public static void Invoke(this MonoBehaviour mb, Action f, float delay)
        {
            mb.StartCoroutine(InvokeRoutine(f, delay));
        }
    
        private static IEnumerator InvokeRoutine(System.Action f, float delay)
        {
            yield return new WaitForSeconds(delay);

            f();
        }
    }
}