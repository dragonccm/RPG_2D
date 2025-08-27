using UnityEngine;
using RPG.Animation;

/// <summary>
/// Enhanced EnemyAnimatorController với cấu trúc parameters chuẩn
/// và hệ thống animation hoàn chỉnh cho Enemy và Boss
/// </summary>
public class EnemyAnimatorController : AnimationControllerBase
{
    [Header("🎬 Animator Configuration")]
    // Animator được kế thừa từ AnimationControllerBase
    
    [Header("⚙️ Animation Settings")]
    [SerializeField] private float transitionSpeed = 0.1f;
    [SerializeField] private bool enableDebugLogs = false;
    
    [Header("🎵 Audio Settings")]
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip[] footstepSounds;
    
    // Animation state tracking
    private bool isCurrentlyMoving = false;
    private bool isCurrentlyAttacking = false;
    private bool isCurrentlyDead = false;
    private float currentSpeed = 0f;

    protected override void Awake()
    {
        base.Awake();
        InitializeAnimator();
    }
    
    private void InitializeAnimator()
    {
        // animator đã được khởi tạo trong base.Awake()
        if (animator == null)
        {
            Debug.LogError($"[{gameObject.name}] No Animator component found!");
            return;
        }
        
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No Animator Controller assigned!");
            return;
        }
        
        // Initialize default state
        ResetToIdle();
        
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] EnemyAnimatorController initialized!");
    }
    
    /// <summary>
    /// 🔧 ENHANCED: Đặt tham số float với optimization
    /// </summary>
    public void SetFloat(string paramName, float value)
    {
        SetFloatOptimized(Animator.StringToHash(paramName), value);
    }

    /// <summary>
    /// 🔧 ENHANCED: Đặt tham số boolean với optimization
    /// </summary>
    public void SetBool(string paramName, bool value)
    {
        SetBoolOptimized(Animator.StringToHash(paramName), value);
    }

    /// <summary>
    /// 🔧 ENHANCED: Kích hoạt trigger với optimization
    /// </summary>
    public void SetTrigger(string paramName)
    {
        string triggerToUse = paramName ?? "Attack";
        SetTriggerOptimized(Animator.StringToHash(triggerToUse));
    }
    
    /// <summary>
    /// 🚶 ENHANCED: Animation di chuyển với smooth transition và optimization
    /// </summary>
    public void PlayMoveAnimation(float speed)
    {
        currentSpeed = Mathf.Max(0f, speed);
        bool shouldMove = currentSpeed > 0.1f;
        
        // Update parameters với optimization using static AnimationParameters
        SetFloatOptimized(AnimationParameters.Speed, currentSpeed);
        SetBoolOptimized(AnimationParameters.IsMoving, shouldMove);
        
        // Track state change
        if (shouldMove != isCurrentlyMoving)
        {
            isCurrentlyMoving = shouldMove;
            
            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] Movement: {(shouldMove ? "Moving" : "Idle")} (Speed: {currentSpeed:F2})");
        }
    }

    /// <summary>
    /// ⚔️ ENHANCED: Animation tấn công với state tracking
    /// </summary>
    public void PlayAttackAnimation()
    {
        if (isCurrentlyDead) return;
        
        SetTriggerOptimized(AnimationParameters.Attack);
        isCurrentlyAttacking = true;
        
        // Auto-reset attack state after delay
        StartCoroutine(ResetAttackState());
        
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Playing Attack animation");
    }
    
    /// <summary>
    /// 🩸 Animation bị thương
    /// </summary>
    public void PlayHurtAnimation()
    {
        if (isCurrentlyDead) return;
        
        SetTriggerOptimized(AnimationParameters.Hurt);
        
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Playing Hurt animation");
    }

    /// <summary>
    /// 💀 ENHANCED: Animation chết với state locking
    /// </summary>
    public void PlayDeathAnimation()
    {
        if (isCurrentlyDead) return;
        
        isCurrentlyDead = true;
        
        // Stop all movement
        PlayMoveAnimation(0f);
        
        // Set death parameters
        SetBoolOptimized(AnimationParameters.IsDead, true);
        SetTriggerOptimized(AnimationParameters.Die);
        
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Playing Death animation - State locked");
    }

    /// <summary>
    /// 😴 Kích hoạt animation nhàn rỗi
    /// </summary>
    public void PlayIdleAnimation()
    {
        PlayMoveAnimation(0f);
        
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Playing Idle animation");
    }
    
    /// <summary>
    /// ✨ Animation skill đặc biệt (cho Boss)
    /// </summary>
    public void PlaySkillAnimation(string skillName = null)
    {
        if (isCurrentlyDead) return;
        
        // Use AnimationParameters static class
        int skillToTrigger = string.IsNullOrEmpty(skillName) ? AnimationParameters.Skill : Animator.StringToHash(skillName);
        SetTriggerOptimized(skillToTrigger);
        
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Playing Skill: {skillName ?? "Default"}");
    }
    
    /// <summary>
    /// 🔥 Animation chế độ berserk (cho Boss)
    /// </summary>
    public void SetBerserkMode(bool isBerserk)
    {
        SetBoolOptimized(AnimationParameters.IsBerserk, isBerserk);
        
        if (isBerserk)
        {
            SetTriggerOptimized(AnimationParameters.EnterBerserk);
        }
        
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Berserk mode: {(isBerserk ? "ON" : "OFF")}");
    }
    
    /// <summary>
    /// 🌟 Animation teleport (cho Boss)
    /// </summary>
    public void PlayTeleportAnimation()
    {
        if (isCurrentlyDead) return;
        
        SetTriggerOptimized(AnimationParameters.Teleport);
        
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Playing Teleport animation");
    }
    
    /// <summary>
    /// 🔄 Reset về trạng thái idle
    /// </summary>
    public void ResetToIdle()
    {
        if (!ValidateAnimator()) return;
        
        PlayIdleAnimation();
        isCurrentlyAttacking = false;
        
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Reset to Idle state");
    }
    
    /// <summary>
    /// ✅ Validate animator component
    /// </summary>
    private new bool ValidateAnimator()
    {
        if (animator == null)
        {
            Debug.LogError($"[{gameObject.name}] Animator is null!");
            return false;
        }
        
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError($"[{gameObject.name}] No Animator Controller assigned!");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 🔄 Reset attack state sau delay
    /// </summary>
    private System.Collections.IEnumerator ResetAttackState()
    {
        yield return new WaitForSeconds(1f);
        isCurrentlyAttacking = false;
    }
    
    // Properties
    public bool IsMoving => isCurrentlyMoving;
    public bool IsAttacking => isCurrentlyAttacking;
    public bool IsDead => isCurrentlyDead;
    public float CurrentSpeed => currentSpeed;
    public Animator AnimatorComponent => animator;
    
    /// <summary>
    /// 🧪 Test all animations (Context Menu)
    /// </summary>
    [ContextMenu("🧪 Test All Animations")]
    public void TestAllAnimations()
    {
        StartCoroutine(TestAnimationSequence());
    }
    
    private System.Collections.IEnumerator TestAnimationSequence()
    {
        Debug.Log($"[{gameObject.name}] Testing animations...");
        
        PlayIdleAnimation();
        yield return new WaitForSeconds(1f);
        
        PlayMoveAnimation(5f);
        yield return new WaitForSeconds(2f);
        
        PlayAttackAnimation();
        yield return new WaitForSeconds(1f);
        
        PlayHurtAnimation();
        yield return new WaitForSeconds(1f);
        
        ResetToIdle();
        
        Debug.Log($"[{gameObject.name}] Animation test completed!");
    }

    /// <summary>
    /// 📊 Get animation debug info for Inspector
    /// </summary>
    public string GetAnimationDebugInfo()
    {
        if (!ValidateAnimator()) return "Animator Invalid";
        
        var currentState = animator.GetCurrentAnimatorStateInfo(0);
        var nextState = animator.GetNextAnimatorStateInfo(0);
        
        string debugInfo = $"=== ANIMATION DEBUG ===\n";
        debugInfo += $"Current State: {currentState.shortNameHash}\n";
        debugInfo += $"Normalized Time: {currentState.normalizedTime:F2}\n";
        debugInfo += $"Is Moving: {IsMoving}\n";
        debugInfo += $"Is Attacking: {IsAttacking}\n";
        debugInfo += $"Is Dead: {IsDead}\n";
        debugInfo += $"Current Speed: {CurrentSpeed:F2}\n";
        
        if (nextState.shortNameHash != 0)
        {
            debugInfo += $"Next State: {nextState.shortNameHash}\n";
        }
        
        return debugInfo;
    }
    
    /// <summary>
    /// 🎯 Force trigger specific animation (for testing)
    /// </summary>
    [ContextMenu("🎯 Force Attack")]
    public void ForceAttack() => PlayAttackAnimation();
    
    [ContextMenu("🩸 Force Hurt")]
    public void ForceHurt() => PlayHurtAnimation();
    
    [ContextMenu("💀 Force Death")]
    public void ForceDeath() => PlayDeathAnimation();
    
    [ContextMenu("🔥 Toggle Berserk")]
    public void ToggleBerserk() => SetBerserkMode(!GetBool("IsBerserk"));
    
    /// <summary>
    /// Get current bool parameter value
    /// </summary>
    private bool GetBool(string paramName)
    {
        if (!ValidateAnimator()) return false;
        
        try
        {
            return animator.GetBool(paramName);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 💥 Animation Event: Attack hit frame
    /// </summary>
    public void OnAttackHit()
    {
        Debug.Log($"[{gameObject.name}] Attack Hit Event triggered!");
        
        // Notify other systems about attack hit
        var attackController = GetComponent<EnemyAttackController>();
        if (attackController != null)
        {
            // Use existing Attack method instead of non-existent OnAttackHitEvent
            var target = FindCurrentTarget();
            if (target != null)
            {
                attackController.Attack(target);
            }
        }
    }
    
    /// <summary>
    /// Find current target for attack hit event
    /// </summary>
    private Transform FindCurrentTarget()
    {
        // Try to get target from EnemyAIController
        var aiController = GetComponent<EnemyAIController>();
        if (aiController != null && aiController.playerTarget != null)
        {
            return aiController.playerTarget;
        }
        
        // Try to get target from CoreEnemy
        var coreEnemy = GetComponent<CoreEnemy>();
        if (coreEnemy != null && coreEnemy.CurrentTarget != null)
        {
            return coreEnemy.CurrentTarget;
        }
        
        // Fallback: find nearest player
        var player = GameObject.FindGameObjectWithTag("Player");
        return player?.transform;
    }
    
    /// <summary>
    /// ✅ Animation Event: Attack completed
    /// </summary>
    public void OnAttackComplete()
    {
        isCurrentlyAttacking = false;
        
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Attack Complete Event triggered!");
    }
    
    /// <summary>
    /// 🦶 Animation Event: Footstep sound
    /// </summary>
    public void OnFootstep()
    {
        // Play footstep sound
        var audioSource = GetComponent<AudioSource>();
        if (audioSource != null && footstepSounds != null && footstepSounds.Length > 0)
        {
            // Play random footstep sound
            AudioClip randomClip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            if (randomClip != null)
            {
                audioSource.pitch = Random.Range(0.8f, 1.2f);
                audioSource.PlayOneShot(randomClip);
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Footstep Event triggered!");
    }
    
    /// <summary>
    /// ⚡ Animation Event: Skill cast point
    /// </summary>
    public void OnSkillCast()
    {
        Debug.Log($"[{gameObject.name}] Skill Cast Event triggered!");
        
        // Notify skill system about cast point
        var enemy = GetComponent<CoreEnemy>();
        if (enemy != null)
        {
            // Trigger skill effect at this exact frame
        }
    }
}