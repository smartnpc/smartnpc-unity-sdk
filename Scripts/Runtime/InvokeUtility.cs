using UnityEngine;
using System;
using System.Collections;

namespace SmartNPC
{
    public static class InvokeUtility
    {
        public static void Invoke(this MonoBehaviour mb, Action action, float delay)
        {
            mb.StartCoroutine(InvokeRoutine(action, delay));
        }
    
        private static IEnumerator InvokeRoutine(Action action, float delay)
        {
            yield return new WaitForSeconds(delay);

            action();
        }
    }
}