using UnityEngine;

namespace RPG.Animation
{
    /// <summary>
    /// Base class cho các animation controller để tối ưu performance và tránh lặp code
    /// </summary>
    public abstract class AnimationControllerBase : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] protected Animator animator;
        [SerializeField] protected bool enableDebugLogging = true; // Enable for debugging attack issues
        
        protected virtual void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
                
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }
        
        /// <summary>
        /// Tối ưu: Chỉ set parameter khi value thay đổi
        /// </summary>
        protected void SetBoolOptimized(int parameterHash, bool value)
        {
            if (animator == null) return;
            
            // Kiểm tra parameter có tồn tại không
            if (!HasParameter(parameterHash)) return;
            
            if (animator.GetBool(parameterHash) != value)
            {
                animator.SetBool(parameterHash, value);
                if (enableDebugLogging)
                    Debug.Log($"SetBool: {parameterHash} = {value}");
            }
        }
        
        /// <summary>
        /// Tối ưu: Chỉ set parameter khi value thay đổi
        /// </summary>
        protected void SetFloatOptimized(int parameterHash, float value, float tolerance = 0.01f)
        {
            if (animator == null) return;
            
            // Kiểm tra parameter có tồn tại không
            if (!HasParameter(parameterHash)) return;
            
            if (Mathf.Abs(animator.GetFloat(parameterHash) - value) > tolerance)
            {
                animator.SetFloat(parameterHash, value);
                if (enableDebugLogging)
                    Debug.Log($"SetFloat: {parameterHash} = {value}");
            }
        }
        
        /// <summary>
        /// Tối ưu: Chỉ set parameter khi value thay đổi
        /// </summary>
        protected void SetIntegerOptimized(int parameterHash, int value)
        {
            if (animator == null) return;
            
            // Kiểm tra parameter có tồn tại không
            if (!HasParameter(parameterHash)) return;
            
            if (animator.GetInteger(parameterHash) != value)
            {
                animator.SetInteger(parameterHash, value);
                if (enableDebugLogging)
                    Debug.Log($"SetInteger: {parameterHash} = {value}");
            }
        }
        
            /// <summary>
    /// Luôn trigger (không cần optimization vì trigger là one-shot)
    /// </summary>
    protected void SetTriggerOptimized(int parameterHash)
    {
        if (animator == null) 
        {
            Debug.LogWarning($"[{gameObject.name}] Animator is null! Cannot set trigger.");
            return;
        }
        
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No RuntimeAnimatorController assigned! Cannot set trigger.");
            return;
        }
        
        // Kiểm tra parameter có tồn tại không
        if (!HasParameter(parameterHash)) 
        {
            string paramName = GetParameterNameFromHash(parameterHash);
            Debug.LogWarning($"[{gameObject.name}] Parameter '{paramName}' (hash: {parameterHash}) not found in animator controller '{animator.runtimeAnimatorController.name}'!");
            return;
        }
        
        try
        {
            animator.SetTrigger(parameterHash);
            if (enableDebugLogging)
                Debug.Log($"[{gameObject.name}] SetTrigger: {GetParameterNameFromHash(parameterHash)} (hash: {parameterHash})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[{gameObject.name}] Error setting trigger {parameterHash}: {e.Message}");
        }
    }
        
        /// <summary>
        /// Kiểm tra parameter có tồn tại trong animator không
        /// </summary>
        protected bool HasParameter(int parameterHash)
        {
            if (animator == null) return false;
            
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.nameHash == parameterHash)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Kiểm tra parameter có tồn tại trong animator không
        /// </summary>
        protected bool HasParameter(string parameterName)
        {
            if (animator == null) return false;
            
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == parameterName)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Kiểm tra animator có valid không
        /// </summary>
        protected bool ValidateAnimator()
        {
            if (animator == null)
            {
                Debug.LogWarning($"Animator is null on {gameObject.name}");
                return false;
            }
            return true;
        }
        
        /// <summary>
        /// Lấy tên parameter từ hash (để debug)
        /// </summary>
        protected string GetParameterNameFromHash(int parameterHash)
        {
            if (animator == null || animator.runtimeAnimatorController == null) 
                return $"Unknown({parameterHash})";
                
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.nameHash == parameterHash)
                    return param.name;
            }
            
            return $"Unknown({parameterHash})";
        }
    }
}