using UnityEngine;

public class EnemyRangedAttack : MonoBehaviour
{
    private float cooldownMultiplier = 1f;

    // Thi?t l?p h? s? cooldown (dùng cho Elite/Frenzy)
    public void SetCooldownMultiplier(float multiplier)
    {
        cooldownMultiplier = multiplier;
    }

    // This class is for ranged attacks.

    // Có th? m? r?ng thêm logic t?n công t?m xa ? ?ây
}