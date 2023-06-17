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
        [SerializeField] public List<SmartNPCAnimation> _gestures = new List<SmartNPCAnimation>();

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
            ApplyAnimations();

            // workaround
            _consumeHandler = async (SmartNPCBehavior behavior, UnityAction next) => {
                await ConsumeHandler(behavior, next);
            };
            
            _character.BehaviorQueue.Consume(_consumeHandler);
        }

        private async Task ConsumeHandler(SmartNPCBehavior behavior, UnityAction next)
        {
            if (behavior.type == SmartNPCBehaviorType.Gesture) await TriggerGesture(behavior);

            // TODO: expression

            next();
        }

        private async Task TriggerGesture(SmartNPCBehavior behavior)
        {
            for (int i = 0; i < _gestures.Count; i++)
            {
                SmartNPCAnimation animation = _gestures[i];

                if (animation.gestureName == behavior.name)
                {
                    if (animation.animationClip != null) await TriggerAnimation("SmartNPC-" + behavior.name + "Trigger");
                    else if (animation.animationTrigger != null && animation.animationTrigger != "") await TriggerAnimation(animation.animationTrigger);

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
                SmartNPCAnimation item = _gestures[i];

                if (item.gestureName != null && item.gestureName != "" && item.animationClip != null)
                {
                    CreateAnimationState(item.gestureName, item.animationClip);
                }
            }
        }

        private void CreateAnimationState(string name, AnimationClip animationClip)
        {
            if (_animatorController.layers.Length == 0) return;

            AnimatorStateMachine stateMachine = _animatorController.layers[0].stateMachine;
            
            if (stateMachine.states.Length == 0) return;
            
            AnimatorState originalState = stateMachine.states[0].state;

            AnimatorState state = stateMachine.AddState("SmartNPC-" + name);

            state.motion = animationClip;

            AnimatorControllerParameter trigger = new AnimatorControllerParameter();

            trigger.type = AnimatorControllerParameterType.Trigger;
            trigger.name = "SmartNPC-" + name + "Trigger";
            
            _animatorController.AddParameter(trigger);
            

            AnimatorStateTransition startTransition = originalState.AddTransition(state);

            startTransition.AddCondition(AnimatorConditionMode.If, 0, trigger.name);

            startTransition.name = "SmartNPC-" + name + "-Transition-Start";
            startTransition.hasExitTime = false; // transition immediately when the condition is met


            AnimatorStateTransition endTransition = state.AddTransition(originalState);

            endTransition.name = "SmartNPC-" + name + "-Transition-End";
            endTransition.hasExitTime = true; // transition once the animation finishes
        }

        public async Task TriggerAnimation(string name)
        {
            Debug.Log("TriggerAnimation: " + name);
            _animator.SetTrigger(name);

            await WaitUntilAnimationFinished();
        }

        public async Task WaitUntilAnimationFinished()
        {
            if (_animator)
            {
                await TaskUtils.WaitUntil(() => _animator.GetCurrentAnimatorStateInfo(0).normalizedTime <= 1.0f);
            }
        }

        override public void Dispose()
        {
            base.Dispose();
            
            if (_character && _character.BehaviorQueue) _character.BehaviorQueue.StopConsuming(_consumeHandler);
        }
    }
}