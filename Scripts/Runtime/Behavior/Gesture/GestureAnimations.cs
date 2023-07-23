#if UNITY_EDITOR

using UnityEngine;
using UnityEditor.Animations;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmartNPC
{
    public class GestureAnimations
    {
        public static async Task ApplyGestureAnimations(SmartNPCCharacter character, List<SmartNPCGestureItem> gestures)
        {
            Animator animator = character.GetComponent<Animator>();

            if (!animator)
            {
                Debug.LogWarning("No animator found");

                return;
            }

            AnimatorController animatorController = animator.runtimeAnimatorController as AnimatorController;

            if (!animatorController)
            {
                Debug.LogWarning("No animator controller found");

                return;
            }

            ClearAnimationStates(animatorController);
            ClearTriggers(animatorController);

            await Task.Delay(500);

            CreateAnimationStates(animatorController, gestures);
        }

        private static bool StateExists(AnimatorController animatorController, string name)
        {
            AnimatorStateMachine stateMachine = animatorController.layers[0].stateMachine;
            
            for (int i = 0; i < stateMachine.states.Length; i++)
            {
                AnimatorState state = stateMachine.states[i].state;

                if (state.name == name) return true;
            }

            return false;
        }

        private static void ClearAnimationStates(AnimatorController animatorController)
        {
            AnimatorStateMachine stateMachine = animatorController.layers[0].stateMachine;
            
            ChildAnimatorState[] states = (ChildAnimatorState[]) stateMachine.states.Clone();

            AnimatorState originalState = stateMachine.states[0].state;

            ClearTransitions(originalState); // from transition
            
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;

                if (state.name.IndexOf(SmartNPCGesture.Prefix) != -1)
                {
                    ClearTransitions(state); // to transition

                    stateMachine.RemoveState(state);
                }
            }
        }

        private static void ClearTransitions(AnimatorState state)
        {
            AnimatorStateTransition[] transitions = (AnimatorStateTransition[]) state.transitions.Clone();

            for (int j = 0; j < transitions.Length; j++)
            {
                AnimatorStateTransition transition = transitions[j];

                if (transition.name.IndexOf(SmartNPCGesture.Prefix) != -1) state.RemoveTransition(transition);
            }
        }

        private static void ClearTriggers(AnimatorController animatorController)
        {
            AnimatorControllerParameter[] parameters = (AnimatorControllerParameter[]) animatorController.parameters.Clone();

            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];

                if (parameter.name.IndexOf(SmartNPCGesture.Prefix) != -1) animatorController.RemoveParameter(parameter);
            }
        }

        private static void CreateAnimationStates(AnimatorController animatorController, List<SmartNPCGestureItem> gestures)
        {
            for (int i = 0; i < gestures.Count; i++)
            {
                SmartNPCGestureItem item = gestures[i];

                if (item.gestureName != null && item.gestureName != "" && item.animationClip != null)
                {
                    CreateAnimationState(animatorController, item.gestureName, item.animationClip);

                    if (item.animationTrigger != null && item.animationTrigger != "")
                    {
                        Debug.LogWarning(item.gestureName + " gesture has both Animation Clip and Animation Trigger. Animation Trigger will be ignored.");
                    }
                }
            }
        }

        private static void CreateAnimationState(AnimatorController animatorController, string name, AnimationClip animationClip)
        {
            if (animatorController.layers.Length == 0) return;

            AnimatorStateMachine stateMachine = animatorController.layers[0].stateMachine;
            
            if (stateMachine.states.Length == 0) return;
            
            AnimatorState originalState = stateMachine.states[0].state;

            string stateName = SmartNPCGesture.Prefix + "-" + name;

            if (StateExists(animatorController, stateName)) return;

            AnimatorState state = stateMachine.AddState(stateName);

            state.motion = animationClip;

            AnimatorControllerParameter trigger = new AnimatorControllerParameter();

            trigger.type = AnimatorControllerParameterType.Trigger;
            trigger.name = stateName + "Trigger";
            
            animatorController.AddParameter(trigger);
            

            AnimatorStateTransition startTransition = originalState.AddTransition(state);

            startTransition.AddCondition(AnimatorConditionMode.If, 0, trigger.name);

            startTransition.name = stateName + "-Transition-Start";
            startTransition.hasExitTime = false; // transition immediately when the condition is met


            AnimatorStateTransition endTransition = state.AddTransition(originalState);

            endTransition.name = stateName + "-Transition-End";
            endTransition.hasExitTime = true; // transition once the animation finishes
        }
    }
}

#endif