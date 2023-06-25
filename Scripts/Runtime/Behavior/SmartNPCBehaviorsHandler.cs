using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Animations;
using System.Threading.Tasks;
using UnityEngine.Events;

namespace SmartNPC
{
    public class SmartNPCBehaviorsHandler : BaseEmitter
    {
        [SerializeField] public List<SmartNPCGestureAnimation> _gestures = new List<SmartNPCGestureAnimation>();

        private SmartNPCCharacter _character;
        private Animator _animator;
        private AnimatorController _animatorController;
        private UnityAction<SmartNPCBehavior, UnityAction> _consumeHandler;

        void Awake()
        {
            _character = GetComponent<SmartNPCCharacter>();

            if (!_character) throw new Exception("Not attached to a SmartNPCCharacter");

            _animator = _character.GetComponent<Animator>();

            if (!_animator) throw new Exception("No animator found");

            _animatorController = _animator.runtimeAnimatorController as AnimatorController;

            if (!_animatorController) throw new Exception("No animator controller found");

            _character.OnReady(Init);
        }

        private void Init()
        {
            Task applyAnimationsTask = ApplyAnimations(); // workaround to avoid warning for no await

            // workaround
            _consumeHandler = async (SmartNPCBehavior behavior, UnityAction next) => {
                await ConsumeHandler(behavior, next);
            };
            
            _character.BehaviorQueue.Consume(_consumeHandler);
        }

        private async Task ConsumeHandler(SmartNPCBehavior behavior, UnityAction next)
        {
            if (behavior is SmartNPCGesture)
            {
                await TriggerGesture(behavior as SmartNPCGesture);

                next();
            }
        }

        private async Task TriggerGesture(SmartNPCGesture gesture)
        {
            for (int i = 0; i < _gestures.Count; i++)
            {
                SmartNPCGestureAnimation animation = _gestures[i];

                if (animation.gestureName == gesture.name)
                {
                    if (animation.animationClip != null) await _character.TriggerAnimation("SmartNPC-" + gesture.name + "Trigger");
                    else if (animation.animationTrigger != null && animation.animationTrigger != "") await _character.TriggerAnimation(animation.animationTrigger);

                    break;
                }
            }
        }
        
        private async Task ApplyAnimations()
        {
            ClearAnimationStates();
            ClearTriggers();

            await Task.Delay(500);

            CreateAnimationStates();
        }

        private bool StateExists(string name)
        {
            AnimatorStateMachine stateMachine = _animatorController.layers[0].stateMachine;
            
            for (int i = 0; i < stateMachine.states.Length; i++)
            {
                AnimatorState state = stateMachine.states[i].state;

                if (state.name == name) return true;
            }

            return false;
        }

        private void ClearAnimationStates()
        {
            AnimatorStateMachine stateMachine = _animatorController.layers[0].stateMachine;
            
            ChildAnimatorState[] states = (ChildAnimatorState[]) stateMachine.states.Clone();

            AnimatorState originalState = stateMachine.states[0].state;

            ClearTransitions(originalState); // from transition
            
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;

                if (state.name.IndexOf("SmartNPC") != -1)
                {
                    ClearTransitions(state); // to transition

                    stateMachine.RemoveState(state);
                }
            }
        }

        private void ClearTransitions(AnimatorState state)
        {
            AnimatorStateTransition[] transitions = (AnimatorStateTransition[]) state.transitions.Clone();

            for (int j = 0; j < transitions.Length; j++)
            {
                AnimatorStateTransition transition = transitions[j];

                if (transition.name.IndexOf("SmartNPC") != -1) state.RemoveTransition(transition);
            }
        }

        private void ClearTriggers()
        {
            AnimatorControllerParameter[] parameters = (AnimatorControllerParameter[]) _animatorController.parameters.Clone();

            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];

                if (parameter.name.IndexOf("SmartNPC") != -1) _animatorController.RemoveParameter(parameter);
            }
        }

        private void CreateAnimationStates()
        {
            for (int i = 0; i < _gestures.Count; i++)
            {
                SmartNPCGestureAnimation item = _gestures[i];

                if (item.gestureName != null && item.gestureName != "" && item.animationClip != null)
                {
                    CreateAnimationState(item.gestureName, item.animationClip);

                    if (item.animationTrigger != null && item.animationTrigger != "")
                    {
                        Debug.LogWarning(item.gestureName + " gesture has both Animation Clip and Animation Trigger. Animation Trigger will be ignored.");
                    }
                }
            }
        }

        private void CreateAnimationState(string name, AnimationClip animationClip)
        {
            if (_animatorController.layers.Length == 0) return;

            AnimatorStateMachine stateMachine = _animatorController.layers[0].stateMachine;
            
            if (stateMachine.states.Length == 0) return;
            
            AnimatorState originalState = stateMachine.states[0].state;

            string stateName = "SmartNPC-" + name;

            if (StateExists(stateName)) return;

            AnimatorState state = stateMachine.AddState(stateName);

            state.motion = animationClip;

            AnimatorControllerParameter trigger = new AnimatorControllerParameter();

            trigger.type = AnimatorControllerParameterType.Trigger;
            trigger.name = stateName + "Trigger";
            
            _animatorController.AddParameter(trigger);
            

            AnimatorStateTransition startTransition = originalState.AddTransition(state);

            startTransition.AddCondition(AnimatorConditionMode.If, 0, trigger.name);

            startTransition.name = stateName + "-Transition-Start";
            startTransition.hasExitTime = false; // transition immediately when the condition is met


            AnimatorStateTransition endTransition = state.AddTransition(originalState);

            endTransition.name = stateName + "-Transition-End";
            endTransition.hasExitTime = true; // transition once the animation finishes
        }

        override public void Dispose()
        {
            base.Dispose();
            
            if (_character && _character.BehaviorQueue) _character.BehaviorQueue.StopConsuming(_consumeHandler);
        }
    }
}