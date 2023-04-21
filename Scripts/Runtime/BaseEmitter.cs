using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartNPC
{
    public class BaseEmitter : MonoBehaviour, IDisposable
    {
        private bool ready = false;
        private event EventHandler OnReadyListeners;
        
        private List<Action> invokeOnUpdateActions = new List<Action>();

        void Update() {
            if (invokeOnUpdateActions.Count > 0) {
                invokeOnUpdateActions.ForEach(action => action());

                invokeOnUpdateActions.Clear();
            }
        }

        protected void SetReady()
        {
            ready = true;
            
            OnReadyListeners?.Invoke(this, EventArgs.Empty);
        }

        public void OnReady(Action callback)
        {
            if (ready) callback();
            else {
                EventHandler listener = null;

                listener = (sender, e) => {
                    callback();

                    OnReadyListeners -= listener;
                };

                OnReadyListeners += listener;
            }
        }

        // invoke on update to make sure actions are invoked in the main unity thread
        // unity related actions that are invoked on other threads can throw an exception
        protected void InvokeOnUpdate(Action callback)
        {
            invokeOnUpdateActions.Add(callback);
        }

        public virtual void Dispose()
        {
            invokeOnUpdateActions.Clear();
        }
    }
}