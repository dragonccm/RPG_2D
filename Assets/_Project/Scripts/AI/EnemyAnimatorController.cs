using UnityEngine;

/// <summary>
/// Lớp điều khiển Animator cho kẻ địch, quản lý các trạng thái và tham số animation.
/// </summary>
public class EnemyAnimatorController : MonoBehaviour
{
   [SerializeField] private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Animator component not found on this GameObject. Animation control will be limited.", this);
        }
    }

    /// <summary>
    /// Đặt tham số float cho Animator.
    /// </summary>
    /// <param name="paramName">Tên tham số.</param>
    /// <param name="value">Giá trị float.</param>
    public void SetFloat(string paramName, float value)
    {
        if (animator != null)
        {
            animator.SetFloat(paramName, value);
        }
    }

    /// <summary>
    /// Đặt tham số boolean cho Animator.
    /// </summary>
    /// <param name="paramName">Tên tham số.</param>
    /// <param name="value">Giá trị boolean.</param>
    public void SetBool(string paramName, bool value)
    {
        if (animator != null)
        {
            animator.SetBool(paramName, value);
        }
    }

    /// <summary>
    /// Kích hoạt trigger cho Animator.
    /// </summary>
    /// <param name="paramName">Tên trigger.</param>
    public void SetTrigger(string paramName)
    {
        animator.SetTrigger(paramName ?? "Attack");
    }

    /// <summary>
    /// Kích hoạt animation di chuyển.
    /// </summary>
    /// <param name="speed">Tốc độ di chuyển.</param>
    public void PlayMoveAnimation(float speed)
    {
        SetFloat("Speed", speed);
    }

    /// <summary>
    /// Kích hoạt animation tấn công.
    /// </summary>
    public void PlayAttackAnimation()
    {      
        SetTrigger("Attack");
    }

    /// <summary>
    /// Kích hoạt animation chết.
    /// </summary>
    public void PlayDeathAnimation()
    {
        SetTrigger("Die");
    }

    /// <summary>
    /// Kích hoạt animation nhàn rỗi.
    /// </summary>
    public void PlayIdleAnimation()
    {
        SetFloat("Speed", 0f);
    }
}