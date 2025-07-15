using UnityEngine;

[CreateAssetMenu(fileName = "SkillModule", menuName = "RPG/Skill Module")]
public class SkillModule : ScriptableObject
{
    [Header("Basic Information")]
    public string skillName;
    [TextArea(2, 4)]
    public string description;
    public Sprite skillIcon;
    public int requiredLevel = 1;
    
    [Header("Combat Stats")]
    public float damage = 10f;
    public float range = 2f;
    public float speed = 5f;
    public float cooldown = 1f;
    public float manaCost = 10f;
    
    [Header("Special Effects")]
    public float stunDuration = 0f;
    public float knockbackForce = 5f;
    public float healAmount = 0f;
    public float areaRadius = 0f;
    public float chargeTime = 0f;
    
    [Header("Visual & Audio")]
    public Color skillColor = Color.white;
    public AudioClip castSound;
    public AudioClip impactSound;
    public GameObject effectPrefab;
    public GameObject projectilePrefab;
    
    [Header("Animation - Uses Existing 'Attack' Animation")]
    public string animationTrigger = "Attack"; // Always use existing Attack trigger
    public float animationLength = 1f;
    [Tooltip("All skills will use the existing 'Attack' animation parameter")]
    public bool usesExistingAttackAnimation = true;
    
    [Header("Skill Type")]
    public SkillType skillType = SkillType.Melee; // Use existing SkillType enum from SkillData.cs
    
    [Header("Damage Zone Override")]
    [Tooltip("Optional: Custom damage zone prefab to override auto-generated zones")]
    public GameObject damageZonePrefab;
    
    [Header("Balance")]
    [Range(0f, 1f)]
    public float criticalChance = 0.1f;
    public float criticalMultiplier = 2f;
    
    [Header("Upgrade System")]
    public SkillModule[] upgrades; // Skill nâng c?p
    public int maxLevel = 5;
    public int currentLevel = 1;

    [Header("Damage Area Visualization")]
    [Tooltip("Show damage area indicator when using skill")]
    public bool showDamageArea = true;
    [Tooltip("Color of damage area indicator")]
    public Color damageAreaColor = new Color(1f, 0f, 0f, 0.3f);
    [Tooltip("Duration to show damage area in seconds")]
    public float damageAreaDisplayTime = 1f;
    
    /// <summary>
    /// Enhanced method to create skill executor with Support type support
    /// </summary>
    public ISkillExecutor CreateExecutor()
    {
        switch (skillType)
        {
            case SkillType.Melee:
                return new MeleeSkillExecutor(this);
            case SkillType.Projectile:
                return new ProjectileSkillExecutor(this);
            case SkillType.Area:
                return new AreaSkillExecutor(this);
            case SkillType.Support:
                return new SupportSkillExecutor(this);
            // Legacy support
            case SkillType.Stun:
                return new StunSkillExecutor(this);
            case SkillType.Heal:
                return new HealSkillExecutor(this);
            case SkillType.Buff:
                return new BuffSkillExecutor(this);
            default:
                return new MeleeSkillExecutor(this);
        }
    }

    // Helper methods for skill validation
    public bool CanPlayerUse(int playerLevel)
    {
        return playerLevel >= requiredLevel;
    }

    public bool CanExecute(Character user)
    {
        if (user == null) return false;
        
        // Check mana
        if (user.mana != null && user.mana.currentValue < manaCost) return false;
        
        // Check level requirement
        var skillManager = user.GetComponent<ModularSkillManager>();
        if (skillManager != null && skillManager.GetPlayerLevel() < requiredLevel) return false;
        
        return true;
    }

    public string GetSkillInfo()
    {
        string info = $"<b>{skillName}</b>\n";
        info += $"<i>{description}</i>\n\n";
        info += $"<color=#ffdd44>Level Required:</color> {requiredLevel}\n";
        
        // Type-specific information
        switch (skillType)
        {
            case SkillType.Melee:
                info += $"<color=#ff6666>Damage:</color> {damage}\n";
                info += $"<color=#66ff66>Range:</color> {range}\n";
                break;
                
            case SkillType.Projectile:
                info += $"<color=#ff6666>Damage:</color> {damage}\n";
                info += $"<color=#66ff66>Range:</color> {range}\n";
                info += $"<color=#ffff66>Speed:</color> {speed}\n";
                break;
                
            case SkillType.Area:
                info += $"<color=#ff6666>Damage:</color> {damage}\n";
                info += $"<color=#66ff66>Range:</color> {range}\n";
                info += $"<color=#ffff66>Area Radius:</color> {areaRadius}\n";
                break;
                
            case SkillType.Support:
                if (healAmount > 0)
                    info += $"<color=#66ff66>Heal Amount:</color> {healAmount}\n";
                break;
        }
        
        info += $"<color=#6666ff>Cooldown:</color> {cooldown}s\n";
        info += $"<color=#ffaa44>Mana Cost:</color> {manaCost}\n";
        
        if (stunDuration > 0)
            info += $"<color=#ff66ff>Stun Duration:</color> {stunDuration}s\n";
        
        if (knockbackForce > 0)
            info += $"<color=#ff9966>Knockback Force:</color> {knockbackForce}\n";
        
        return info;
    }

    // Validation method for editor
    private void OnValidate()
    {
        // Ensure animation trigger is always "Attack" for compatibility
        if (usesExistingAttackAnimation)
        {
            animationTrigger = "Attack";
        }
        
        // Ensure valid values
        damage = Mathf.Max(0f, damage);
        range = Mathf.Max(0.1f, range);
        cooldown = Mathf.Max(0.1f, cooldown);
        manaCost = Mathf.Max(0f, manaCost);
        requiredLevel = Mathf.Max(1, requiredLevel);
        areaRadius = Mathf.Max(0f, areaRadius);
        healAmount = Mathf.Max(0f, healAmount);
        
        // Auto-generate description if empty
        if (string.IsNullOrEmpty(description))
        {
            GenerateDefaultDescription();
        }
        
        // Auto-set appropriate damage area color based on skill type
        UpdateDamageAreaColorByType();
    }

    private void GenerateDefaultDescription()
    {
        switch (skillType)
        {
            case SkillType.Melee:
                description = $"Melee attack dealing {damage} damage in {range} range. Auto-generates hit zone collider around player.";
                break;
            case SkillType.Projectile:
                description = $"Ranged projectile attack dealing {damage} damage with {range} range. Shows range circle and direction arrow.";
                break;
            case SkillType.Area:
                description = $"Area attack dealing {damage} damage in {areaRadius} radius. Shows target area at mouse position.";
                break;
            case SkillType.Support:
                description = healAmount > 0 ? 
                    $"Support skill restoring {healAmount} health instantly. No damage zones." :
                    "Support skill providing enhancement to the caster. No damage zones.";
                break;
            case SkillType.Stun:
                description = $"Stun attack dealing {damage} damage and stunning for {stunDuration} seconds.";
                break;
            case SkillType.Heal:
                description = $"Healing skill restoring {healAmount} health instantly.";
                break;
            case SkillType.Buff:
                description = "Buff skill providing temporary enhancement to the caster.";
                break;
        }
    }
    
    /// <summary>
    /// Auto-update damage area color based on skill type
    /// </summary>
    private void UpdateDamageAreaColorByType()
    {
        // Only auto-update if using default color
        if (damageAreaColor.Equals(new Color(1f, 0f, 0f, 0.3f)))
        {
            switch (skillType)
            {
                case SkillType.Melee:
                    damageAreaColor = new Color(1f, 0f, 0f, 0.3f); // Red
                    break;
                case SkillType.Projectile:
                    damageAreaColor = new Color(1f, 1f, 0f, 0.3f); // Yellow
                    break;
                case SkillType.Area:
                    damageAreaColor = new Color(0f, 1f, 1f, 0.3f); // Cyan
                    break;
                case SkillType.Support:
                    damageAreaColor = new Color(0f, 1f, 0f, 0.3f); // Green
                    break;
            }
        }
    }
    
    // Get skill stats for UI display
    public string GetStatsText()
    {
        string stats = "";
        
        if (damage > 0)
            stats += $"Damage: {damage}\n";
        if (healAmount > 0)
            stats += $"Heal: {healAmount}\n";
        if (range > 0)
            stats += $"Range: {range}\n";
        if (areaRadius > 0)
            stats += $"Area: {areaRadius}\n";
        if (stunDuration > 0)
            stats += $"Stun: {stunDuration}s\n";
        if (knockbackForce > 0)
            stats += $"Knockback: {knockbackForce}\n";
        
        stats += $"Cooldown: {cooldown}s\n";
        stats += $"Mana: {manaCost}\n";
        stats += $"Level: {requiredLevel}";
        
        return stats;
    }

    /// <summary>
    /// Get skill color based on type - enhanced with Support
    /// </summary>
    public Color GetSkillTypeColor()
    {
        switch (skillType)
        {
            case SkillType.Melee: return Color.red;
            case SkillType.Projectile: return Color.yellow;
            case SkillType.Area: return Color.cyan;
            case SkillType.Support: return Color.green;
            case SkillType.Stun: return Color.magenta;
            case SkillType.Heal: return Color.green;
            case SkillType.Buff: return Color.blue;
            default: return Color.white;
        }
    }
    
    /// <summary>
    /// Get skill type description for UI
    /// </summary>
    public string GetSkillTypeDescription()
    {
        switch (skillType)
        {
            case SkillType.Melee:
                return "C?n chi?n - T? ??ng sinh collider vùng sát th??ng";
            case SkillType.Projectile:
                return "Phóng chiêu - V? vòng tròn t?m xa và m?i tên ch? ???ng";
            case SkillType.Area:
                return "Di?n r?ng - V? vùng sát th??ng t?i v? trí ??t chiêu";
            case SkillType.Support:
                return "H? tr? - Không c?n v? vùng, ch? th?c thi hi?u ?ng";
            case SkillType.Stun:
                return "Choáng (Legacy) - S? d?ng Melee thay th?";
            case SkillType.Heal:
                return "H?i máu (Legacy) - S? d?ng Support thay th?";
            case SkillType.Buff:
                return "Buff (Legacy) - S? d?ng Support thay th?";
            default:
                return "Không xác ??nh";
        }
    }
    
    /// <summary>
    /// Check if skill requires target position (mouse click)
    /// </summary>
    public bool RequiresTargetPosition()
    {
        return skillType == SkillType.Projectile || skillType == SkillType.Area;
    }
    
    /// <summary>
    /// Check if skill should show range indicator
    /// </summary>
    public bool ShouldShowRangeIndicator()
    {
        return skillType == SkillType.Projectile || skillType == SkillType.Area;
    }
}