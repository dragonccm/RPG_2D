#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public static class AnimatorTransitionHelper
{
    public static AnimatorStateTransition CreateTransition(AnimatorState fromState, AnimatorState toState,
        string conditionParameter, AnimatorConditionMode conditionMode, float threshold = 0f,
        bool hasExitTime = false, float exitTime = 0.9f, float duration = 0.1f)
    {
        var transition = fromState.AddTransition(toState);
        
        if (!string.IsNullOrEmpty(conditionParameter))
        {
            transition.AddCondition(conditionMode, threshold, conditionParameter);
        }
        
        transition.hasExitTime = hasExitTime;
        transition.exitTime = exitTime;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        
        return transition;
    }
    
    public static AnimatorStateTransition CreateAnyStateTransition(AnimatorStateMachine stateMachine,
        AnimatorState targetState, string triggerParameter, float duration = 0.1f)
    {
        var transition = stateMachine.AddAnyStateTransition(targetState);
        transition.AddCondition(AnimatorConditionMode.If, 0, triggerParameter);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        
        return transition;
    }
    
    public static void SetupBasicEnemyTransitions(AnimatorStateMachine stateMachine,
        AnimatorState idle, AnimatorState walk, AnimatorState attack, 
        AnimatorState hurt, AnimatorState death, float defaultDuration = 0.1f)
    {
        // Idle <-> Walk
        CreateTransition(idle, walk, "IsMoving", AnimatorConditionMode.If, 0f, false, 0f, defaultDuration * 2f);
        CreateTransition(walk, idle, "IsMoving", AnimatorConditionMode.IfNot, 0f, false, 0f, defaultDuration * 2f);
        
        // Any State -> Attack
        CreateAnyStateTransition(stateMachine, attack, "Attack", 0.05f);
        
        // Attack -> Idle (with exit time)
        CreateTransition(attack, idle, "", AnimatorConditionMode.If, 0f, true, 0.9f, defaultDuration);
        
        // Any State -> Hurt
        CreateAnyStateTransition(stateMachine, hurt, "Hurt", defaultDuration);
        
        // Hurt -> Idle (with exit time)
        CreateTransition(hurt, idle, "", AnimatorConditionMode.If, 0f, true, 1.0f, defaultDuration);
        
        // Any State -> Death
        CreateAnyStateTransition(stateMachine, death, "Die", 0.2f);
        
        Debug.Log("Setup Basic Enemy transitions completed");
    }
    
    public static void SetupBossTransitions(AnimatorStateMachine stateMachine,
        AnimatorState idle, AnimatorState walk, AnimatorState attack,
        AnimatorState hurt, AnimatorState death,
        AnimatorState skill1, AnimatorState skill2, AnimatorState ultimate,
        AnimatorState teleport, AnimatorState berserk, float defaultDuration = 0.1f)
    {
        // Basic transitions (same as enemy)
        SetupBasicEnemyTransitions(stateMachine, idle, walk, attack, hurt, death, defaultDuration);
        
        // Skill transitions
        if (skill1 != null)
        {
            CreateAnyStateTransition(stateMachine, skill1, "Skill1", defaultDuration);
            CreateTransition(skill1, idle, "", AnimatorConditionMode.If, 0f, true, 0.95f, defaultDuration * 2f);
        }
        
        if (skill2 != null)
        {
            CreateAnyStateTransition(stateMachine, skill2, "Skill2", defaultDuration);
            CreateTransition(skill2, idle, "", AnimatorConditionMode.If, 0f, true, 0.95f, defaultDuration * 2f);
        }
        
        if (ultimate != null)
        {
            CreateAnyStateTransition(stateMachine, ultimate, "UltimateSkill", defaultDuration);
            CreateTransition(ultimate, idle, "", AnimatorConditionMode.If, 0f, true, 0.98f, defaultDuration * 3f);
        }
        
        if (teleport != null)
        {
            CreateAnyStateTransition(stateMachine, teleport, "Teleport", 0.05f);
            CreateTransition(teleport, idle, "", AnimatorConditionMode.If, 0f, true, 1.0f, defaultDuration);
        }
        
        if (berserk != null)
        {
            CreateAnyStateTransition(stateMachine, berserk, "EnterBerserk", defaultDuration);
            CreateTransition(berserk, idle, "", AnimatorConditionMode.If, 0f, true, 1.0f, defaultDuration * 2f);
        }
        
        Debug.Log("Setup Boss transitions completed");
    }
    
    public static void AddAnimationEvents(AnimationClip clip, string functionName, 
        float timePercent, object parameter = null)
    {
        if (clip == null) return;
        
        AnimationEvent animEvent = new AnimationEvent();
        animEvent.functionName = functionName;
        animEvent.time = clip.length * timePercent;
        
        if (parameter != null)
        {
            if (parameter is string)
                animEvent.stringParameter = (string)parameter;
            else if (parameter is float)
                animEvent.floatParameter = (float)parameter;
            else if (parameter is int)
                animEvent.intParameter = (int)parameter;
        }
        
        // Get current events
        var events = AnimationUtility.GetAnimationEvents(clip);
        var eventsList = new System.Collections.Generic.List<AnimationEvent>(events);
        eventsList.Add(animEvent);
        
        // Set updated events
        AnimationUtility.SetAnimationEvents(clip, eventsList.ToArray());
        
        Debug.Log($"Added Animation Event '{functionName}' to {clip.name} at {timePercent:P1}");
    }
    
    public static void SetupBasicAnimationEvents(AnimationClip attackClip, AnimationClip walkClip,
        float attackHitFrame = 0.6f, float footstepFrame = 0.5f)
    {
        if (attackClip != null)
        {
            AddAnimationEvents(attackClip, "OnAttackHit", attackHitFrame);
            AddAnimationEvents(attackClip, "OnAttackComplete", 0.95f);
        }
        
        if (walkClip != null)
        {
            // Add footstep events (multiple)
            AddAnimationEvents(walkClip, "OnFootstep", 0.25f, "Left");
            AddAnimationEvents(walkClip, "OnFootstep", 0.75f, "Right");
        }
    }

    public static void SetupPlayerAttackEvents(AnimationClip attackClip, float attackHitTime)
    {
        if (attackClip == null) return;

        // Clear existing events to avoid duplicates
        var events = AnimationUtility.GetAnimationEvents(attackClip);
        List<AnimationEvent> newEvents = new List<AnimationEvent>();

        // Add attack hit event
        var hitEvent = new AnimationEvent
        {
            functionName = "OnAttackHit",
            time = attackClip.length * attackHitTime
        };
        newEvents.Add(hitEvent);

        // Add action complete event near the end
        var completeEvent = new AnimationEvent
        {
            functionName = "OnActionComplete",
            time = attackClip.length * 0.95f // Slightly before end to ensure it triggers
        };
        newEvents.Add(completeEvent);

        AnimationUtility.SetAnimationEvents(attackClip, newEvents.ToArray());
    }

    public static void SetupFootstepEvents(AnimationClip walkClip)
    {
        if (walkClip == null) return;

        // Clear existing events
        AnimationUtility.SetAnimationEvents(walkClip, new AnimationEvent[0]);

        var events = new List<AnimationEvent>();

        // Add two footstep events for a typical walk cycle
        float firstStep = walkClip.length * 0.25f;
        float secondStep = walkClip.length * 0.75f;

        var event1 = new AnimationEvent
        {
            functionName = "OnFootstep",
            time = firstStep
        };
        events.Add(event1);

        var event2 = new AnimationEvent
        {
            functionName = "OnFootstep",
            time = secondStep
        };
        events.Add(event2);

        AnimationUtility.SetAnimationEvents(walkClip, events.ToArray());
    }
    
    public static void SetupBossAnimationEvents(AnimationClip attackClip, AnimationClip skill1Clip,
        AnimationClip skill2Clip, AnimationClip ultimateClip, AnimationClip teleportClip)
    {
        // Basic attack events
        if (attackClip != null)
        {
            AddAnimationEvents(attackClip, "OnAttackHit", 0.6f);
            AddAnimationEvents(attackClip, "OnAttackComplete", 0.95f);
        }
        
        // Skill 1 events
        if (skill1Clip != null)
        {
            AddAnimationEvents(skill1Clip, "OnSkillCastStart", 0.1f, 1);
            AddAnimationEvents(skill1Clip, "OnSkillCastPoint", 0.6f, 1);
            AddAnimationEvents(skill1Clip, "OnSkillCastComplete", 0.9f, 1);
        }
        
        // Skill 2 events
        if (skill2Clip != null)
        {
            AddAnimationEvents(skill2Clip, "OnSkillCastStart", 0.1f, 2);
            AddAnimationEvents(skill2Clip, "OnSkillCastPoint", 0.5f, 2);
            AddAnimationEvents(skill2Clip, "OnSkillCastComplete", 0.9f, 2);
        }
        
        // Ultimate events
        if (ultimateClip != null)
        {
            AddAnimationEvents(ultimateClip, "OnUltimateCastStart", 0.1f);
            AddAnimationEvents(ultimateClip, "OnUltimateCastPoint", 0.7f);
            AddAnimationEvents(ultimateClip, "OnUltimateCastComplete", 0.95f);
        }
        
        // Teleport events
        if (teleportClip != null)
        {
            AddAnimationEvents(teleportClip, "OnTeleportVanish", 0.3f);
            AddAnimationEvents(teleportClip, "OnTeleportAppear", 0.7f);
        }
    }
    
    public static void OptimizeStatePositions(AnimatorStateMachine stateMachine)
    {
        float x = 200;
        float y = 0;
        float yStep = 100;

        var states = stateMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            var state = states[i];
            state.position = new Vector3(x, y, 0);
            states[i] = state; // Write the modified struct back to the array
            y += yStep;
        }
        stateMachine.states = states; // Assign the modified array back

        // Position any state node
        if (stateMachine.anyStatePosition == Vector3.zero)
        {
            stateMachine.anyStatePosition = new Vector3(0, 0, 0);
        }

        // Position entry node
        if (stateMachine.entryPosition == Vector3.zero)
        {
            stateMachine.entryPosition = new Vector3(0, 100, 0);
        }

        // Position exit node
        if (stateMachine.exitPosition == Vector3.zero)
        {
            stateMachine.exitPosition = new Vector3(0, 200, 0);
        }
    }
}
#endif