using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SmartNPC
{
    public class BaseEmitter : MonoBehaviour, IDisposable
    {
        private bool ready = false;

        private readonly UnityEvent OnReadyEvent = new UnityEvent();
        
        private List<Action> invokeOnUpdateActions = new List<Action>();

        protected virtual void Update()
        {
            // cloning invokeOnUpdateActions, in case the invoked actions add more items to it
            List<Action> actionsToInvoke = new List<Action>(invokeOnUpdateActions);

            invokeOnUpdateActions.Clear();

            if (actionsToInvoke.Count > 0)
            {
                actionsToInvoke.ForEach(action => action());

                actionsToInvoke.Clear();
            }
        }

        protected void SetReady()
        {
            ready = true;

            InvokeOnUpdate(() => OnReadyEvent.Invoke());
        }

        public void OnReady(Action callback)
        {
            if (ready) callback();
            else
            {
                UnityAction listener = null;

                listener = () => {
                    callback();

                    OnReadyEvent.RemoveListener(listener);
                };

                OnReadyEvent.AddListener(listener);
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
            OnReadyEvent.RemoveAllListeners();
        }
        
        public bool IsReady
        {
            get { return ready; }
        }
    }
}