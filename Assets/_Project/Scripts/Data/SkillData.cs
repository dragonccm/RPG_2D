using UnityEngine;

[CreateAssetMenu(fileName = "SkillData", menuName = "RPG/SkillData")]
public class SkillData : ScriptableObject
{
    [Header("Basic Skill Properties")]
    public string skillName;               // Tên kỹ năng
    public float damage;                   // Sát thương
    public float range;                    // Phạm vi
    public float speed;                    // Tốc độ (cho projectile)
    public float cooldown;                 // Thời gian hồi chiêu
    public float manaCost;                 // Mana tiêu tốn
    
    [Header("Special Effects")]
    public float stunDuration;             // Thời gian choáng (nếu có)
    public float knockbackForce = 5f;      // Lực knockback
    public GameObject effectPrefab;        // Prefab hiệu ứng (như particle hoặc sprite)
    
    [Header("Visual & Audio")]
    public Color skillColor = Color.white; // Màu của skill effect
    public AudioClip castSound;            // Âm thanh khi cast skill
    public AudioClip impactSound;          // Âm thanh khi skill hit target
    
    [Header("Skill Type Settings")]
    public SkillType skillType = SkillType.Melee;
    
    [Header("Area Effect Settings (For AOE skills)")]
    public float areaRadius = 3f;          // Bán kính area effect
    public float chargeTime = 1f;          // Thời gian charge trước khi skill active
    
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;    // Prefab cho projectile
    public float projectileLifetime = 5f;  // Thời gian sống của projectile
    
    [Header("Balance")]
    [Range(0f, 1f)]
    public float criticalChance = 0.1f;    // Tỷ lệ critical hit
    public float criticalMultiplier = 2f;  // Hệ số nhân critical damage
}

/// <summary>
/// Enum định nghĩa các loại kỹ năng theo yêu cầu
/// </summary>
public enum SkillType
{
    Melee,      // Kỹ năng cận chiến - tự động sinh collider vùng sát thương
    Projectile, // Kỹ năng phóng chiêu - vẽ vòng tròn tầm xa và mũi tên chỉ đường
    Area,       // Kỹ năng AoE - vẽ vùng sát thương tại vị trí đặt chiêu
    Support,    // Kỹ năng hỗ trợ - không cần vẽ vùng, chỉ thực thi hiệu ứng
    Stun,       // Skill làm choáng (deprecated - sẽ merge vào Melee/Area)
    Buff,       // Skill tăng buff (deprecated - sẽ merge vào Support)  
    Heal        // Skill hồi máu (deprecated - sẽ merge vào Support)
}