using UnityEngine;

namespace RPG.Animation
{
    /// <summary>
    /// Handles player-specific animations, including 4-directional movement and footstep events.
    /// This component works in conjunction with the PlayerController.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimatorController : AnimationControllerBase
    {
        private PlayerController playerController;

        // Parameter hashes for performance
        private static readonly int isMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int speedHash = Animator.StringToHash("Speed");
        private static readonly int facingDirectionHash = Animator.StringToHash("FacingDirection");
        private static readonly int attackUpHash = Animator.StringToHash("AttackUp");
        private static readonly int attackDownHash = Animator.StringToHash("AttackDown");
        private static readonly int attackLeftHash = Animator.StringToHash("AttackLeft"); // Used for both Left/Right
        private static readonly int hurtHash = Animator.StringToHash("Hurt");
        private static readonly int dieHash = Animator.StringToHash("Die");

        protected override void Awake()
        {
            base.Awake();
            playerController = GetComponent<PlayerController>();

            // Validate animator parameters on startup
            ValidateAnimatorParameters();
        }

        void Update()
        {
            if (!ValidateAnimator() || playerController == null) return;

            // Handle movement animations
            bool isMoving = playerController.Movement.sqrMagnitude > 0.01f;
            SetBoolOptimized(isMovingHash, isMoving);
            SetFloatOptimized(speedHash, playerController.Movement.magnitude);

            // Handle movement sprite flipping
            if (isMoving)
            {
                HandleMovementFlipping();
            }

            // Handle 4-directional sprite changes if the system is active
            if (playerController.Use4DirectionalAttacks)
            {
                UpdateFacingDirection();
            }
        }
        
        /// <summary>
        /// Handle sprite flipping during movement (Left/Right)
        /// </summary>
        private void HandleMovementFlipping()
        {
            Vector2 movement = playerController.Movement;
            
            // Only flip on horizontal movement (ignore vertical for flipping)
            if (Mathf.Abs(movement.x) > 0.01f)
            {
                var spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null) 
                    spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                    
                if (spriteRenderer != null)
                {
                    // Flip based on horizontal movement direction
                    spriteRenderer.flipX = movement.x < 0; // True for left, False for right
                }
            }
        }

        /// <summary>
        /// Updates the animator's facing direction parameter based on the PlayerController's state.
        /// </summary>
        private void UpdateFacingDirection()
        {
            if (!HasParameter(facingDirectionHash)) return;

            float directionValue = playerController.CurrentFacingDirection switch
            {
                AttackDirection.Up => 2f,
                AttackDirection.Down => -2f,
                AttackDirection.Left => -1f,
                AttackDirection.Right => 1f,
                _ => -2f // Default to Down
            };
            SetFloatOptimized(facingDirectionHash, directionValue);
        }

        /// <summary>
        /// Triggers a directional attack animation with 3-animation system (Up/Down/Horizontal+Flip).
        /// </summary>
        public void TriggerAttack(AttackDirection direction)
        {
            if (!ValidateAnimator())
            {
                Debug.LogError($"[{gameObject.name}] Cannot trigger attack - animator not valid");
                return;
            }

            // Validate required parameters exist
            if (!HasParameter(attackUpHash) || !HasParameter(attackDownHash) || !HasParameter(attackLeftHash))
            {
                Debug.LogError($"[{gameObject.name}] Missing attack animation parameters in Animator Controller");
                return;
            }

            // 3-Animation System: Up, Down, Horizontal (Left/Right shared with flip)
            int triggerHash = direction switch
            {
                AttackDirection.Up => attackUpHash,
                AttackDirection.Down => attackDownHash,
                AttackDirection.Left => attackLeftHash,  // Use Left animation for both Left/Right
                AttackDirection.Right => attackLeftHash, // Use Left animation + flip
                _ => attackDownHash
            };

            // Handle sprite flipping for Left/Right attacks
            HandleSpriteFlipping(direction);

            SetTriggerOptimized(triggerHash);

            if (enableDebugLogging)
                Debug.Log($"🎯 Attack triggered: {direction} (Animation: {GetAnimationName(direction)})");
        }
        
        /// <summary>
        /// Validate that all required animator parameters exist
        /// </summary>
        private void ValidateAnimatorParameters()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                Debug.LogError($"[{gameObject.name}] Animator or AnimatorController is null!");
                return;
            }

            string[] requiredParameters = { "IsMoving", "Speed", "FacingDirection", "AttackUp", "AttackDown", "AttackLeft", "Hurt", "Die" };
            bool allParametersValid = true;

            foreach (string paramName in requiredParameters)
            {
                if (!HasParameter(paramName))
                {
                    Debug.LogError($"[{gameObject.name}] Missing animator parameter: {paramName}");
                    allParametersValid = false;
                }
            }

            if (allParametersValid)
            {
                Debug.Log($"[{gameObject.name}] All animator parameters validated successfully");
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] Some animator parameters are missing! Please check your Animator Controller.");
            }
        }
        
        /// <summary>
        /// Handle sprite flipping for horizontal attacks and movement
        /// </summary>
        private void HandleSpriteFlipping(AttackDirection direction)
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) 
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                
            if (spriteRenderer != null)
            {
                switch (direction)
                {
                    case AttackDirection.Left:
                        spriteRenderer.flipX = true;
                        break;
                    case AttackDirection.Right:
                        spriteRenderer.flipX = false;
                        break;
                    // Up/Down don't flip
                }
            }
        }
        
        /// <summary>
        /// Get animation name for debugging
        /// </summary>
        private string GetAnimationName(AttackDirection direction)
        {
            return direction switch
            {
                AttackDirection.Up => "AttackUp",
                AttackDirection.Down => "AttackDown", 
                AttackDirection.Left => "AttackLeft (Original)",
                AttackDirection.Right => "AttackLeft (Flipped)",
                _ => "AttackDown"
            };
        }

        /// <summary>
        /// Triggers the hurt animation.
        /// </summary>
        public void TriggerHurt()
        {
            if (ValidateAnimator())
            {
                SetTriggerOptimized(hurtHash);
            }
        }

        /// <summary>
        /// Triggers the death animation.
        /// </summary>
        public void TriggerDeath()
        {
            if (ValidateAnimator())
            {
                SetTriggerOptimized(dieHash);
            }
        }

        /// <summary>
        /// Animation Event receiver for footstep sounds.
        /// This function is called by events on the Walk/Run animation clips.
        /// </summary>
        public void OnFootstep()
        {
            // Here you would typically play a footstep sound using a sound manager.
            if (enableDebugLogging)
            {
                Debug.Log("OnFootstep event triggered.");
            }
            // Example: SoundManager.Instance.PlayFootstepSound(transform.position);
        }

        /// <summary>
        /// Test method to verify animation system is working
        /// Call this from PlayerController for debugging
        /// </summary>
        public void TestAnimationSystem()
        {
            Debug.Log($"[{gameObject.name}] Testing animation system...");

            if (!ValidateAnimator())
            {
                Debug.LogError($"[{gameObject.name}] Animator validation failed!");
                return;
            }

            // Test basic parameters
            SetBoolOptimized(isMovingHash, true);
            SetFloatOptimized(speedHash, 1f);
            SetFloatOptimized(facingDirectionHash, 0f);

            Debug.Log($"[{gameObject.name}] Basic parameters set successfully");

            // Test attack triggers
            SetTriggerOptimized(attackDownHash);
            Debug.Log($"[{gameObject.name}] Attack trigger test completed");

            // Reset parameters
            SetBoolOptimized(isMovingHash, false);
            SetFloatOptimized(speedHash, 0f);
        }
    }
}
