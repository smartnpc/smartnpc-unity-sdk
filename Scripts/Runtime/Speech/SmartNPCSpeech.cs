using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;

namespace SmartNPC
{
    public class SmartNPCSpeech : BaseEmitter
    {
        // public UnityEvent OnRecordingStart;
        // public UnityEvent OnRecordingEnd;

        private SmartNPCConnection _connection;
        private bool recording = false;

        private int channels = 1;
        [SerializeField] int frequency = 16000;
        [SerializeField] int lengthSec = 1; // 10 (test)
        
        readonly List<float[]> chunks = new List<float[]>(); // TODO: base64 after concatenating chunks
        // Size of audioclip used to collect information, need to be big enough to keep up with collect. 
        int bufferSize;
        float[] floatBuffer;
        AudioClip recordingClip;
        float collectCounter;
        // Last known position in AudioClip buffer.
        int lastPosition;

        // public event EventHandler<string[]> OnPush;

        public void StartRecording()
        {
            if (!_connection.IsReady) throw new Exception("Connection isn't ready");

            Debug.Log("StartRecording");

            lastPosition = Microphone.GetPosition(null);

            chunks.Clear();

            recording = true;

            // OnRecordingStart.Invoke();
        }

        public void StopRecording()
        {
            if (!_connection.IsReady) throw new Exception("Connection isn't ready");

            Microphone.End(null);

            // TODO: push remaining chunks before clearing

            chunks.Clear();

            recording = false;

            // OnRecordingEnd.Invoke();
        }

        void Awake()
        {
            bufferSize = lengthSec * frequency;
            floatBuffer = new float[bufferSize];

            _connection = FindObjectOfType<SmartNPCConnection>();

            _connection.OnReady(SetReady);
        }

        void Start()
        {
            recordingClip = Microphone.Start(null, true, lengthSec, frequency);
            // TODO: in StartRecording instead of Start?
        }

        override protected void Update()
        {
            base.Update();

            if (!recording) return;

            if (!Microphone.IsRecording(null)) StartRecording();

            if (collectCounter <= 0)
            {
                collectCounter = 0.1f; // 0.1f;

                Collect();
            }
            
            collectCounter -= Time.deltaTime;
        }

        void OnDestroy()
        {
            StopRecording();
        }

        private void Collect()
        {
            int currentPosition = Microphone.GetPosition(null);

            if (currentPosition < lastPosition) currentPosition = bufferSize;

            if (currentPosition <= lastPosition) return;

            // int currentSize = currentPosition - lastPosition;

            if (!recordingClip.GetData(floatBuffer, lastPosition)) return;

            lastPosition = currentPosition % bufferSize;

            // TODO: make sure data doesn't repeat itself, that each chunk is only recorded exactly once
            // need to make sure that the positions make sense

            if (IsSilent(floatBuffer))
            {
                if (chunks.Count > 0)
                {
                    //List<string> base64 = chunks.ConvertAll<string>(chunk => RecordedDataToBase64(chunk) );

                    //Process(base64);

                    

                    // OnPush?.Invoke(this, base64.ToArray());



                    /*List<float[]> chunksClone = new List<float[]>(chunks);

                    chunksClone.ForEach(chunk => {
                        Utility.Invoke(this, () => {
                            Debug.Log("Play chunk: " + SumData(chunk));

                            PlayAudioFromBuffer(chunk);
                        }, index++);
                    });*/

                    // TODO: play concatenated properly



                    // PlayAudioFromBuffer(chunks[chunks.Count - 1]);
                    // PlayAudioFromBuffer( ConcatenateChunks() );


                    // chunks.ForEach(chunk => CreateVoice( RecordedDataToBase64(chunk) ));

                    // CreateVoice( RecordedDataToBase64( ConcatenateChunks() ) );

                    ProcessChunks();

                    chunks.Clear();
                }
            }
            else
            {
                chunks.Add(floatBuffer);

                // PlayAudioFromBuffer(floatBuffer);

                // Debug.Log("Play immediately: " + SumData(chunks[chunks.Count - 1]));

                // PlayAudioFromBuffer(chunks[chunks.Count - 1]);

                // Debug.Log("Push chunk: " + chunks.Count);
            }


            // PlayAudioFromBuffer(floatBuffer);
        }

        // temp
        private void PlayAudioFromBuffer(float[] floatBuffer)
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            
            AudioClip clip = AudioClip.Create("chunk_" + System.Guid.NewGuid().ToString(), floatBuffer.Length, channels, frequency, false);

            clip.SetData(floatBuffer, 0);

            audioSource.clip = clip;
            
            audioSource.Play();
        }

        private float[] ConcatenateChunks()
        {
            List<float> result = new List<float>();

            chunks.ForEach(value => result.AddRange(value));

            return result.ToArray();
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
                    writer.Write((ushort) channels);
                    writer.Write(frequency);
                    writer.Write(frequency * channels * 2);
                    writer.Write((ushort) (channels * 2));
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

        private float SumData(float[] data)
        {
            float sum = 0;

            for (int i = 0; i < data.Length; i++)
            {
                float value = data[i] * data[i];

                sum += value;
            }

            return sum;
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

        // temp
        private async Task<AudioSource> CreateVoice(string base64)
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();

            byte[] audioBytes = Convert.FromBase64String(base64);
            string tempPath = Application.persistentDataPath + System.Guid.NewGuid().ToString();

            File.WriteAllBytes(tempPath, audioBytes);

            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(tempPath, AudioType.WAV);

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone) await Task.Yield();

            if (request.result.Equals(UnityWebRequest.Result.ConnectionError)) Debug.LogError(request.error);
            else
            {
                audioSource.clip = DownloadHandlerAudioClip.GetContent(request);

                audioSource.Play();
            }

            File.Delete(tempPath);

            return audioSource;
        }

        /*private void PlayWholeRecording()
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.clip = recordingClip;

            audioSource.Play();
        }*/

        private void ProcessChunks()
        {
            List<string> data = chunks.ConvertAll<string>(chunk => RecordedDataToBase64(chunk) );

            Debug.Log("Process chunks: " + chunks.Count);

            try
            {
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
            catch (Exception e)
            {
                Debug.Log("speech exception: " + e);
            }
        }
    }

    public static class Utility
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