using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SmartNPC
{
    public class SpeechTester : MonoBehaviour
    {
        // methods that help with development, shouldn't be used in production

        public void PlayBuffer(int channels, int frequency, float[] buffer)
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            
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
                    AudioSource audioSource = await Voice.CreateVoice(gameObject, chunk, AudioType.WAV);

                    audioSource.Play();
                }, index++);
            });
        }

        public void PlayAudioClip(AudioClip clip)
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.clip = clip;

            audioSource.Play();
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