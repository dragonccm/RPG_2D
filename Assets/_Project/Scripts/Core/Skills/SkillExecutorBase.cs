using UnityEngine;
using System.Collections;
using System.Linq;

// Base class cho mọi skill executors (partial: lõi / mục tiêu / chỉ báo vùng / tìm đích kỹ năng)
public abstract partial class SkillExecutorBase : ISkillExecutor
{
    public SkillModule Module { get; protected set; }

    public SkillExecutorBase(SkillModule module)
    {
        Module = module;
    }

    public abstract void Execute(Character user, Vector2 targetPosition);
    
    // Implement ISkillExecutor interface
    public virtual bool CanExecute(Character user)
    {
        return Module.CanExecute(user);
    }
    
    public virtual float GetCooldown()
    {
        return Module.cooldown;
    }
    
    public virtual float GetManaCost()
    {
        return Module.manaCost;
    }
    
    /// <summary>
    /// Enhanced animation trigger using SkillModule.animationTrigger
    /// </summary>
    protected void TriggerAnimation(Character user)
    {
        var animator = user.GetComponent<Animator>();
        if (animator != null)
        {
            // S? d?ng animationTrigger t? Module thay v� hard-coded
            string trigger = !string.IsNullOrEmpty(Module.animationTrigger) ? 
                            Module.animationTrigger : "Attack";
            animator.SetTrigger(trigger);
        }
        
        // Player: báo PlayerController để đồng bộ trạng thái animation (gọi trực tiếp, không reflection)
        var playerController = user.GetComponent<PlayerController>();
        if (playerController != null)
        {
            string trigger = !string.IsNullOrEmpty(Module.animationTrigger) ? Module.animationTrigger : "Attack";
            playerController.TriggerSkillAnimation(Module.skillName, trigger);
        }
    }

    protected void PlayCastSound(Character user)
    {
        if (Module.castSound != null)
        {
            var audioSource = user.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(Module.castSound);
            }
        }
    }
    
    protected void PlayImpactSound(Character user)
    {
        if (Module.impactSound != null)
        {
            var audioSource = user.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(Module.impactSound);
            }
        }
    }
    
    /// <summary>
    /// Enhanced visual effect creation v?i auto-destroy v� collision positioning
    /// </summary>
    protected void CreateVisualEffect(Vector3 position)
    {
        if (Module.effectPrefab != null)
        {
            // S? d?ng Enhanced Effect Manager ?? t?o effect v?i auto-destroy
            VfxService.PlayPrefab(Module.effectPrefab, position, Quaternion.identity, 1f,
                Module.damageAreaDisplayTime > 0f ? Module.damageAreaDisplayTime : -1f);
        }
    }
    
    /// <summary>
    /// T?o effect va ch?m t?i v? tr� ch�nh x�c v?i direction
    /// </summary>
    protected void CreateImpactEffect(Vector3 impactPosition, Vector3 impactDirection, GameObject target = null)
    {
        if (Module.effectPrefab != null)
        {
            // T?o effect t?i v? tr� va ch?m ch�nh x�c
            VfxService.PlayPrefabAimed(Module.effectPrefab, impactPosition, impactDirection, 1f,
                Module.damageAreaDisplayTime > 0f ? Module.damageAreaDisplayTime : -1f);
        }
    }
    
    /// <summary>
    /// T?o effect theo d�i target (cho projectile)
    /// </summary>
    protected void CreateFollowEffect(Transform target, Vector3 offset = default)
    {
        if (Module.effectPrefab != null && target != null)
        {
            VfxService.FollowPrefab(Module.effectPrefab, target, offset, 1f,
                Module.damageAreaDisplayTime > 0f ? Module.damageAreaDisplayTime : -1f);
        }
    }
    
}
