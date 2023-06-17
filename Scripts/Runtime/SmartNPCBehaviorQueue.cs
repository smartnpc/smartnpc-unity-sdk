using System.Collections.Generic;
using UnityEngine.Events;

namespace SmartNPC
{
    public class SmartNPCBehaviorQueue : BaseEmitter
    {
        private List<SmartNPCBehavior> _queue = new List<SmartNPCBehavior>();

        public readonly UnityEvent<SmartNPCBehavior, List<SmartNPCBehavior>> OnAdd = new UnityEvent<SmartNPCBehavior, List<SmartNPCBehavior>>();
        public readonly UnityEvent<SmartNPCBehavior, UnityAction> OnConsume = new UnityEvent<SmartNPCBehavior, UnityAction>();

        public void Add(SmartNPCBehavior item)
        {
            _queue.Add(item);
            
            InvokeOnUpdate(() => {
                if (_queue.Count > 0) OnAdd.Invoke(item, _queue);

                if (_queue.Count == 1) Next();
            });
        }

        public void Consume(UnityAction<SmartNPCBehavior, UnityAction> handler)
        {
            OnConsume.AddListener(handler);
            
            if (_queue.Count > 0)
            {
                InvokeOnUpdate(() => {
                    SmartNPCBehavior item = _queue[0];

                    // invoking just this specific handler on purpose when starting to consume
                    handler(item, () => {
                        _queue.Remove(item);

                        Next();
                    });
                });
            }
        }

        public void StopConsuming(UnityAction<SmartNPCBehavior, UnityAction> handler)
        {
            OnConsume.RemoveListener(handler);
        }

        private void Next()
        {
            if (_queue.Count == 0) return;

            SmartNPCBehavior item = _queue[0];
            
            OnConsume.Invoke(item, () => {
                _queue.Remove(item);

                Next();
            });
        }

        override public void Dispose()
        {
            base.Dispose();
            
            _queue.Clear();

            OnAdd.RemoveAllListeners();
            OnConsume.RemoveAllListeners();
        }
    }
}