using UnityEngine;

/// <summary>
/// Sample Skill Creator - T? ??ng t?o sample skills ?? test h? th?ng
/// </summary>
public class SampleSkillCreator : MonoBehaviour
{
    [Header("Sample Skill Creation")]
    [SerializeField] private bool createSampleSkills = false;
    [SerializeField] private string skillsFolder = "Assets/_Project/Data/Skills/";
    
    [Header("Sample Skills Configuration")]
    [SerializeField] private Sprite defaultSkillIcon;
    [SerializeField] private AudioClip defaultCastSound;
    [SerializeField] private AudioClip defaultImpactSound;
    [SerializeField] private GameObject defaultEffectPrefab;
    [SerializeField] private GameObject defaultProjectilePrefab;

    void Start()
    {
        if (createSampleSkills)
        {
            CreateAllSampleSkills();
            createSampleSkills = false; // Prevent creating multiple times
        }
    }

    /// <summary>
    /// T?o t?t c? sample skills
    /// </summary>
    [ContextMenu("Create Sample Skills")]
    public void CreateAllSampleSkills()
    {
        Debug.Log("?? Creating sample skills for testing...");
        
        CreateMeleeSkills();
        CreateProjectileSkills();
        CreateAreaSkills();
        CreateSupportSkills();
        
        Debug.Log("? Sample skills creation completed!");
    }

    private void CreateMeleeSkills()
    {
        // 1. Basic Sword Strike
        CreateSkill("SwordStrike", SkillType.Melee, new SkillStats
        {
            damage = 25f,
            range = 2.5f,
            cooldown = 1.5f,
            manaCost = 10f,
            criticalChance = 0.15f,
            knockbackForce = 8f,
            requiredLevel = 1
        }, "Basic sword strike dealing moderate damage to nearby enemies.");

        // 2. Power Slam
        CreateSkill("PowerSlam", SkillType.Melee, new SkillStats
        {
            damage = 40f,
            range = 3f,
            cooldown = 3f,
            manaCost = 20f,
            criticalChance = 0.2f,
            knockbackForce = 15f,
            stunDuration = 1f,
            requiredLevel = 5
        }, "Powerful slam attack with stun effect.");

        Debug.Log("?? Created Melee skills: SwordStrike, PowerSlam");
    }

    private void CreateProjectileSkills()
    {
        // 3. Magic Arrow
        CreateSkill("MagicArrow", SkillType.Projectile, new SkillStats
        {
            damage = 20f,
            range = 8f,
            speed = 12f,
            cooldown = 1f,
            manaCost = 8f,
            criticalChance = 0.1f,
            requiredLevel = 2
        }, "Fast magical arrow that travels in straight line.");

        // 4. Fireball
        CreateSkill("Fireball", SkillType.Projectile, new SkillStats
        {
            damage = 35f,
            range = 10f,
            speed = 8f,
            cooldown = 2.5f,
            manaCost = 25f,
            criticalChance = 0.25f,
            knockbackForce = 10f,
            requiredLevel = 8
        }, "Explosive fireball projectile with high damage.");

        Debug.Log("?? Created Projectile skills: MagicArrow, Fireball");
    }

    private void CreateAreaSkills()
    {
        // 5. Lightning Strike
        CreateSkill("LightningStrike", SkillType.Area, new SkillStats
        {
            damage = 30f,
            range = 6f,
            areaRadius = 2.5f,
            cooldown = 3f,
            manaCost = 30f,
            criticalChance = 0.3f,
            stunDuration = 0.5f,
            requiredLevel = 10
        }, "Lightning strike at target location affecting all enemies in area.");

        // 6. Meteor
        CreateSkill("Meteor", SkillType.Area, new SkillStats
        {
            damage = 60f,
            range = 8f,
            areaRadius = 4f,
            cooldown = 8f,
            manaCost = 50f,
            criticalChance = 0.2f,
            knockbackForce = 20f,
            chargeTime = 1.5f,
            requiredLevel = 15
        }, "Devastating meteor impact with large area damage.");

        Debug.Log("?? Created Area skills: LightningStrike, Meteor");
    }

    private void CreateSupportSkills()
    {
        // 7. Heal
        CreateSkill("Heal", SkillType.Support, new SkillStats
        {
            healAmount = 50f,
            cooldown = 5f,
            manaCost = 25f,
            requiredLevel = 3
        }, "Instantly restore health to the caster.");

        // 8. Greater Heal
        CreateSkill("GreaterHeal", SkillType.Support, new SkillStats
        {
            healAmount = 100f,
            cooldown = 10f,
            manaCost = 40f,
            requiredLevel = 12
        }, "Powerful healing spell that restores large amount of health.");

        // 9. Mana Restore
        CreateSkill("ManaRestore", SkillType.Support, new SkillStats
        {
            healAmount = 0f, // Special: This will restore mana instead
            cooldown = 8f,
            manaCost = 0f, // No mana cost for mana restore
            requiredLevel = 6
        }, "Restore mana instead of health. No mana cost.");

        Debug.Log("? Created Support skills: Heal, GreaterHeal, ManaRestore");
    }

    private void CreateSkill(string skillName, SkillType skillType, SkillStats stats, string description)
    {
        // Create SkillModule ScriptableObject
        SkillModule skill = ScriptableObject.CreateInstance<SkillModule>();
        
        // Basic information
        skill.skillName = skillName;
        skill.description = description;
        skill.skillIcon = defaultSkillIcon;
        skill.requiredLevel = stats.requiredLevel;
        
        // Combat stats
        skill.damage = stats.damage;
        skill.range = stats.range;
        skill.speed = stats.speed;
        skill.cooldown = stats.cooldown;
        skill.manaCost = stats.manaCost;
        
        // Special effects
        skill.stunDuration = stats.stunDuration;
        skill.knockbackForce = stats.knockbackForce;
        skill.healAmount = stats.healAmount;
        skill.areaRadius = stats.areaRadius;
        skill.chargeTime = stats.chargeTime;
        
        // Visual & Audio
        skill.skillColor = GetSkillColorByType(skillType);
        skill.castSound = defaultCastSound;
        skill.impactSound = defaultImpactSound;
        skill.effectPrefab = defaultEffectPrefab;
        skill.projectilePrefab = defaultProjectilePrefab;
        
        // Skill type
        skill.skillType = skillType;
        
        // Balance
        skill.criticalChance = stats.criticalChance;
        skill.criticalMultiplier = 2f;
        
        // Damage area visualization
        skill.showDamageArea = true;
        skill.damageAreaColor = GetDamageAreaColorByType(skillType);
        skill.damageAreaDisplayTime = 2f;

#if UNITY_EDITOR
        // Save as asset in editor
        if (!System.IO.Directory.Exists(skillsFolder))
        {
            System.IO.Directory.CreateDirectory(skillsFolder);
        }
        
        string assetPath = $"{skillsFolder}{skillName}.asset";
        UnityEditor.AssetDatabase.CreateAsset(skill, assetPath);
        
        Debug.Log($"?? Created skill asset: {assetPath}");
#endif
    }

    private Color GetSkillColorByType(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Melee: return Color.red;
            case SkillType.Projectile: return Color.yellow;
            case SkillType.Area: return Color.cyan;
            case SkillType.Support: return Color.green;
            default: return Color.white;
        }
    }

    private Color GetDamageAreaColorByType(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Melee: return new Color(1f, 0f, 0f, 0.3f);
            case SkillType.Projectile: return new Color(1f, 1f, 0f, 0.3f);
            case SkillType.Area: return new Color(0f, 1f, 1f, 0.3f);
            case SkillType.Support: return new Color(0f, 1f, 0f, 0.3f);
            default: return new Color(1f, 0f, 0f, 0.3f);
        }
    }

    /// <summary>
    /// Struct ?? ??nh ngh?a stats c?a skill
    /// </summary>
    [System.Serializable]
    public struct SkillStats
    {
        public float damage;
        public float range;
        public float speed;
        public float cooldown;
        public float manaCost;
        public float stunDuration;
        public float knockbackForce;
        public float healAmount;
        public float areaRadius;
        public float chargeTime;
        public float criticalChance;
        public int requiredLevel;
    }
}