using UnityEngine;

/// <summary>
/// Component để setup Animation Events cho characters không có custom handlers
/// </summary>
[System.Serializable]
public class AnimationEventSetup : MonoBehaviour
{
    [Header("🎬 Animation Event Configuration")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool autoSetupOnStart = true;
    
    [Header("🎵 Audio Configuration")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private AudioClip actionCompleteSound;
    
    private PlayerController playerController;
    private Character character;
    private Animator animator;

    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupComponents();
        }
    }
    
    void SetupComponents()
    {
        // Cache components
        playerController = GetComponent<PlayerController>();
        character = GetComponent<Character>();
        animator = GetComponent<Animator>();
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Animation Event Setup completed");
    }
    
    #region Standard Animation Event Handlers
    /// <summary>
    /// Called when attack animation hits
    /// </summary>
    public void OnAttackHit()
    {
        if (enableDebugLogs)
            Debug.Log($"🗡️ [{gameObject.name}] Attack Hit Event");
            
        PlaySound(attackSound);
        
        // Trigger damage logic if needed
        if (character != null)
        {
            // Attack hit logic here
        }
    }
    
    /// <summary>
    /// Called when any action completes
    /// </summary>
    public void OnActionComplete()
    {
        if (enableDebugLogs)
            Debug.Log($"✅ [{gameObject.name}] Action Complete Event");
            
        PlaySound(actionCompleteSound);
        
        // Reset busy state for PlayerController
        if (playerController != null)
        {
            playerController.SetBusy(false);
        }
    }
    
    /// <summary>
    /// Called on footstep events
    /// </summary>
    public void OnFootstep()
    {
        if (enableDebugLogs)
            Debug.Log($"👣 [{gameObject.name}] Footstep Event");
            
        PlaySound(footstepSound);
    }
    
    /// <summary>
    /// Generic animation event handler
    /// </summary>
    public void OnAnimationEvent()
    {
        if (enableDebugLogs)
            Debug.Log($"🎬 [{gameObject.name}] Generic Animation Event");
    }
    
    /// <summary>
    /// Animation start handler
    /// </summary>
    public void OnAnimationStart()
    {
        if (enableDebugLogs)
            Debug.Log($"▶️ [{gameObject.name}] Animation Start Event");
    }
    
    /// <summary>
    /// Animation end handler
    /// </summary>
    public void OnAnimationEnd()
    {
        if (enableDebugLogs)
            Debug.Log($"⏹️ [{gameObject.name}] Animation End Event");
    }
    
    /// <summary>
    /// Animation midpoint handler
    /// </summary>
    public void OnAnimationMidpoint()
    {
        if (enableDebugLogs)
            Debug.Log($"🎯 [{gameObject.name}] Animation Midpoint Event");
    }
    #endregion
    
    #region Boss-Specific Event Handlers
    /// <summary>
    /// Boss spell cast event
    /// </summary>
    public void OnSpellCast()
    {
        if (enableDebugLogs)
            Debug.Log($"🔮 [{gameObject.name}] Spell Cast Event");
    }
    
    /// <summary>
    /// Boss teleport event
    /// </summary>
    public void OnTeleport()
    {
        if (enableDebugLogs)
            Debug.Log($"✨ [{gameObject.name}] Teleport Event");
    }
    
    /// <summary>
    /// Boss berserk mode event
    /// </summary>
    public void OnBerserkActivate()
    {
        if (enableDebugLogs)
            Debug.Log($"🔥 [{gameObject.name}] Berserk Activate Event");
    }
    #endregion
    
    #region Utility Methods
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// Setup this component via code
    /// </summary>
    public void SetupAnimationEvents(bool debug = true, AudioSource audio = null)
    {
        enableDebugLogs = debug;
        if (audio != null)
            audioSource = audio;
            
        SetupComponents();
    }
    #endregion
    
    #region Editor Helpers
    [ContextMenu("🔧 Auto-Setup Audio Source")]
    void AutoSetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.7f;
            
            Debug.Log($"✅ Added AudioSource to {gameObject.name}");
        }
        else
        {
            Debug.Log($"ℹ️ AudioSource already exists on {gameObject.name}");
        }
    }
    
    [ContextMenu("📊 Debug Animation Events")]
    void DebugAnimationEvents()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
            
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            Debug.Log($"🎬 [{gameObject.name}] Animation Controller: {animator.runtimeAnimatorController.name}");
            Debug.Log($"  📝 Controller has {animator.runtimeAnimatorController.animationClips.Length} animation clips");
            
            // Note: Animation events can only be inspected in editor
            // Use the Animation Events Fixer tool in editor for detailed event inspection
        }
        else
        {
            Debug.LogWarning($"❌ No Animator Controller found on {gameObject.name}");
        }
    }
    #endregion
}
