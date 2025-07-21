using UnityEngine;

/// <summary>
/// File: SkillModule.cs 
/// Author: Enhanced RPG System
/// Description: Core skill data container and behavior definition for the Enhanced RPG Skill System.
/// Supports Melee, Projectile, Area and Support skills with visualization and execution logic.
/// </summary>
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
    
    [Header("Enhanced Effect Settings")]
    [Tooltip("Custom lifetime for effect (0 = auto-detect from animation)")]
    public float customEffectLifetime = 0f;
    [Tooltip("Offset position for effect relative to impact point")]
    public Vector3 effectPositionOffset = Vector3.zero;
    [Tooltip("Enable enhanced effect features (scaling, screen shake, etc.)")]
    public bool enableEnhancedEffects = true;

    [Header("Animation")]
    [Tooltip("All skills use the existing 'Attack' animation")]
    public string animationTrigger = "Attack";
    public float animationLength = 1f;
    
    [Header("Skill Type")]
    public SkillType skillType = SkillType.Melee;
    
    [Header("Damage Zone")]
    [Tooltip("Optional: Custom damage zone prefab to override auto-generated zones")]
    public GameObject damageZonePrefab;
    
    [Header("Balance")]
    [Range(0f, 1f)]
    public float criticalChance = 0.1f;
    public float criticalMultiplier = 2f;
    
    [Header("Upgrade System")]
    public SkillModule[] upgrades;
    public int maxLevel = 5;
    public int currentLevel = 1;

    [Header("Visualization")]
    [Tooltip("Show damage area indicator when using skill")]
    public bool showDamageArea = true;
    [Tooltip("Color of damage area indicator")]
    public Color damageAreaColor = new Color(1f, 0f, 0f, 0.3f);
    [Tooltip("Duration to show damage area in seconds")]
    public float damageAreaDisplayTime = 1f;
    
    public ISkillExecutor CreateExecutor()
    {
        return skillType switch
        {
            SkillType.Melee => new MeleeSkillExecutor(this),
            SkillType.Projectile => new ProjectileSkillExecutor(this),
            SkillType.Area => new AreaSkillExecutor(this),
            SkillType.Support => new SupportSkillExecutor(this),
            SkillType.Instant => new InstantSkillExecutor(this),
            _ => new MeleeSkillExecutor(this)
        };
    }

    public bool CanPlayerUse(int playerLevel) => playerLevel >= requiredLevel;

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
        
        info += skillType switch
        {
            SkillType.Melee => $"<color=#ff6666>Damage:</color> {damage}\n<color=#66ff66>Range:</color> {range}\n",
            SkillType.Projectile => $"<color=#ff6666>Damage:</color> {damage}\n<color=#66ff66>Range:</color> {range}\n<color=#ffff66>Speed:</color> {speed}\n",
            SkillType.Area => $"<color=#ff6666>Damage:</color> {damage}\n<color=#66ff66>Range:</color> {range}\n<color=#ffff66>Area Radius:</color> {areaRadius}\n",
            SkillType.Support => healAmount > 0 ? $"<color=#66ff66>Heal Amount:</color> {healAmount}\n" : "",
            _ => ""
        };
        
        info += $"<color=#6666ff>Cooldown:</color> {cooldown}s\n";
        info += $"<color=#ffaa44>Mana Cost:</color> {manaCost}\n";
        
        if (stunDuration > 0)
            info += $"<color=#ff66ff>Stun Duration:</color> {stunDuration}s\n";
        
        if (knockbackForce > 0)
            info += $"<color=#ff9966>Knockback Force:</color> {knockbackForce}\n";
        
        return info;
    }

    private void OnValidate()
    {
        ValidateValues();
        if (string.IsNullOrEmpty(description)) GenerateDefaultDescription();
        UpdateDamageAreaColorByType();
    }

    private void ValidateValues()
    {
        damage = Mathf.Max(0f, damage);
        range = Mathf.Max(0.1f, range);
        cooldown = Mathf.Max(0.1f, cooldown);
        manaCost = Mathf.Max(0f, manaCost);
        requiredLevel = Mathf.Max(1, requiredLevel);
        areaRadius = Mathf.Max(0f, areaRadius);
        healAmount = Mathf.Max(0f, healAmount);
    }

    private void GenerateDefaultDescription()
    {
        description = skillType switch
        {
            SkillType.Melee => $"Melee attack dealing {damage} damage in {range} range.",
            SkillType.Projectile => $"Ranged projectile attack dealing {damage} damage with {range} range.",
            SkillType.Area => $"Area attack dealing {damage} damage in {areaRadius} radius.",
            SkillType.Support => healAmount > 0 ? 
                $"Support skill restoring {healAmount} health instantly." :
                "Support skill providing enhancement to the caster.",
            SkillType.Instant => healAmount > 0 ?
                $"Instant heal restoring {healAmount} health immediately." :
                "Instant skill with immediate effect.",
            _ => "Unknown skill type"
        };
    }
    
    private void UpdateDamageAreaColorByType()
    {
        if (!damageAreaColor.Equals(new Color(1f, 0f, 0f, 0.3f))) return;
        
        damageAreaColor = skillType switch
        {
            SkillType.Melee => new Color(1f, 0f, 0f, 0.3f),
            SkillType.Projectile => new Color(1f, 1f, 0f, 0.3f),
            SkillType.Area => new Color(0f, 1f, 1f, 0.3f),
            SkillType.Support => new Color(0f, 1f, 0f, 0.3f),
            SkillType.Instant => new Color(1f, 0f, 1f, 0.3f), // Magenta for instant
            _ => new Color(1f, 0f, 0f, 0.3f)
        };
    }
    
    public string GetStatsText()
    {
        string stats = "";
        
        if (damage > 0) stats += $"Damage: {damage}\n";
        if (healAmount > 0) stats += $"Heal: {healAmount}\n";
        if (range > 0) stats += $"Range: {range}\n";
        if (areaRadius > 0) stats += $"Area: {areaRadius}\n";
        if (stunDuration > 0) stats += $"Stun: {stunDuration}s\n";
        if (knockbackForce > 0) stats += $"Knockback: {knockbackForce}\n";
        
        stats += $"Cooldown: {cooldown}s\n";
        stats += $"Mana: {manaCost}\n";
        stats += $"Level: {requiredLevel}";
        
        return stats;
    }

    public Color GetSkillTypeColor()
    {
        return skillType switch
        {
            SkillType.Melee => Color.red,
            SkillType.Projectile => Color.yellow,
            SkillType.Area => Color.cyan,
            SkillType.Support => Color.green,
            SkillType.Instant => Color.magenta,
            _ => Color.white
        };
    }
    
    public string GetSkillTypeDescription()
    {
        return skillType switch
        {
            SkillType.Melee => "Melee - Auto-generates damage zone around player",
            SkillType.Projectile => "Projectile - Shows range circle and direction arrow",
            SkillType.Area => "Area - Shows damage zone at target location", 
            SkillType.Support => "Support - No damage zone, applies effects directly",
            SkillType.Instant => "Instant - No targeting required, instant execution",
            _ => "Unknown skill type"
        };
    }
    
    public bool RequiresTargetPosition() => skillType == SkillType.Projectile || skillType == SkillType.Area;
    
    public bool ShouldShowRangeIndicator() => skillType == SkillType.Projectile || skillType == SkillType.Area;
    
    public bool IsInstantSkill() => skillType == SkillType.Instant;
}

public enum SkillType
{
    Melee,      // Melee attack - auto-generated damage zone
    Projectile, // Projectile attack - shows range and direction
    Area,       // Area effect - shows target zone
    Support,    // Support skill - no damage zone
    Instant     // Instant skill - no targeting required, instant execution
}