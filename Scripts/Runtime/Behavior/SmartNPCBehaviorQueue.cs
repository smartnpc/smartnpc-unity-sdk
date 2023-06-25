using System.Collections.Generic;
using UnityEngine.Events;

namespace SmartNPC
{
    public class SmartNPCBehaviorQueue : BaseEmitter
    {
        private List<SmartNPCBehavior> _queue = new List<SmartNPCBehavior>();

        public readonly UnityEvent<SmartNPCBehavior, List<SmartNPCBehavior>> OnAdd = new UnityEvent<SmartNPCBehavior, List<SmartNPCBehavior>>();
        public readonly UnityEvent<SmartNPCBehavior, UnityAction> OnConsumeBehaviors = new UnityEvent<SmartNPCBehavior, UnityAction>();
        public readonly UnityEvent<SmartNPCAction, UnityAction> OnConsumeActions = new UnityEvent<SmartNPCAction, UnityAction>();
        public readonly UnityEvent<SmartNPCGesture, UnityAction> OnConsumeGestures = new UnityEvent<SmartNPCGesture, UnityAction>();

        public void Add(SmartNPCBehavior item)
        {
            _queue.Add(item);
            
            InvokeOnUpdate(() => {
                if (_queue.Count > 0) OnAdd.Invoke(item, _queue);

                if (_queue.Count == 1) Next();
            });
        }

        public void ConsumeBehaviors(UnityAction<SmartNPCBehavior, UnityAction> handler)
        {
            OnConsumeBehaviors.AddListener(handler);
            
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

        public void ConsumeActions(UnityAction<SmartNPCAction, UnityAction> handler)
        {
            OnConsumeActions.AddListener(handler);
            
            if (_queue.Count > 0)
            {
                InvokeOnUpdate(() => {
                    SmartNPCBehavior item = _queue[0];

                    if (item is SmartNPCAction)
                    {
                        // invoking just this specific handler on purpose when starting to consume
                        handler(item as SmartNPCAction, () => {
                            _queue.Remove(item);

                            Next();
                        });
                    }
                });
            }
        }

        public void ConsumeGestures(UnityAction<SmartNPCGesture, UnityAction> handler)
        {
            OnConsumeGestures.AddListener(handler);
            
            if (_queue.Count > 0)
            {
                InvokeOnUpdate(() => {
                    SmartNPCBehavior item = _queue[0];

                    if (item is SmartNPCGesture)
                    {
                        // invoking just this specific handler on purpose when starting to consume
                        handler(item as SmartNPCGesture, () => {
                            _queue.Remove(item);

                            Next();
                        });
                    }
                });
            }
        }

        public void StopConsumingBehaviors(UnityAction<SmartNPCBehavior, UnityAction> handler)
        {
            OnConsumeBehaviors.RemoveListener(handler);
        }

        public void StopConsumingActions(UnityAction<SmartNPCAction, UnityAction> handler)
        {
            OnConsumeActions.RemoveListener(handler);
        }

        public void StopConsumingGestures(UnityAction<SmartNPCGesture, UnityAction> handler)
        {
            OnConsumeGestures.RemoveListener(handler);
        }

        private void Next()
        {
            if (_queue.Count == 0) return;

            SmartNPCBehavior item = _queue[0];
            
            OnConsumeBehaviors.Invoke(item, () => {
                _queue.Remove(item);

                Next();
            });

            if (item is SmartNPCAction)
            {
                OnConsumeActions.Invoke(item as SmartNPCAction, () => {
                    _queue.Remove(item);

                    Next();
                });
            }
            else if (item is SmartNPCGesture)
            {
                OnConsumeGestures.Invoke(item as SmartNPCGesture, () => {
                    _queue.Remove(item);

                    Next();
                });
            }
        }

        override public void Dispose()
        {
            base.Dispose();
            
            _queue.Clear();

            OnAdd.RemoveAllListeners();
            OnConsumeBehaviors.RemoveAllListeners();
            OnConsumeActions.RemoveAllListeners();
            OnConsumeGestures.RemoveAllListeners();
        }
    }
}