using UnityEngine;
using System;
using System.Collections.Generic;

namespace SmartNPC
{
    public class SpeechRecognitionTester : MonoBehaviour
    {
        // methods that help with development, shouldn't be used in production

        private AudioSource audioSource;

        void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        public void PlayBuffer(int channels, int frequency, float[] buffer)
        {
            AudioClip clip = AudioClip.Create("chunk_" + System.Guid.NewGuid().ToString(), buffer.Length, channels, frequency, false);

            clip.SetData(buffer, 0);

            audioSource.clip = clip;
            
            audioSource.Play();
        }

        public void PlayChunkBuffers(int channels, int frequency, List<float[]> bufferList)
        {
            int index = 0;

            List<float[]> chunksClone = new List<float[]>(bufferList);

            chunksClone.ForEach(chunk => {
                InvokeUtility.Invoke(this, (Action)(() => {
                    PlayBuffer(channels, frequency, chunk);
                }), index++);
            });
        }

        public void PlayChunkVoices(int channels, int frequency, List<string> base64List)
        {
            int index = 0;

            base64List.ForEach(chunk => {
                InvokeUtility.Invoke(this, async () => {
                    audioSource.clip = await SmartNPCVoice.CreateVoice(chunk, AudioType.WAV);

                    audioSource.Play();
                }, index++);
            });
        }

        public void PlayAudioClip(AudioClip clip)
        {
            audioSource.clip = clip;

            audioSource.Play();
        }
    }
}