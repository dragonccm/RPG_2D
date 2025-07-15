using UnityEngine;

/// <summary>
/// File: SampleSkillCreator.cs
/// Author: Enhanced RPG System
/// Description: Automatically creates sample skills for testing the Enhanced RPG Skill System.
/// Skills are created as SkillModule assets with proper visualization settings for each type.
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

    private void Start()
    {
        if (createSampleSkills)
        {
            CreateAllSampleSkills();
            createSampleSkills = false;
        }
    }

    [ContextMenu("Create Sample Skills")]
    public void CreateAllSampleSkills()
    {
        CreateMeleeSkills();
        CreateProjectileSkills();
        CreateAreaSkills();
        CreateSupportSkills();
    }

    private void CreateMeleeSkills()
    {
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
    }

    private void CreateProjectileSkills()
    {
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
    }

    private void CreateAreaSkills()
    {
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
    }

    private void CreateSupportSkills()
    {
        CreateSkill("Heal", SkillType.Support, new SkillStats
        {
            healAmount = 50f,
            cooldown = 5f,
            manaCost = 25f,
            requiredLevel = 3
        }, "Instantly restore health to the caster.");

        CreateSkill("GreaterHeal", SkillType.Support, new SkillStats
        {
            healAmount = 100f,
            cooldown = 10f,
            manaCost = 40f,
            requiredLevel = 12
        }, "Powerful healing spell that restores large amount of health.");

        CreateSkill("ManaRestore", SkillType.Support, new SkillStats
        {
            healAmount = 0f,
            cooldown = 8f,
            manaCost = 0f,
            requiredLevel = 6
        }, "Restore mana instead of health. No mana cost.");
    }

    private void CreateSkill(string skillName, SkillType skillType, SkillStats stats, string description)
    {
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
        
        // Skill type specific settings
        skill.skillType = skillType;
        skill.criticalChance = stats.criticalChance;
        skill.criticalMultiplier = 2f;
        
        // Visualization settings based on skill type
        ConfigureSkillVisualization(skill, skillType);

#if UNITY_EDITOR
        // Save as asset in editor
        if (!System.IO.Directory.Exists(skillsFolder))
        {
            System.IO.Directory.CreateDirectory(skillsFolder);
        }
        
        string assetPath = $"{skillsFolder}{skillName}.asset";
        UnityEditor.AssetDatabase.CreateAsset(skill, assetPath);
#endif
    }

    private void ConfigureSkillVisualization(SkillModule skill, SkillType skillType)
    {
        // Configure visualization based on skill type
        switch (skillType)
        {
            case SkillType.Melee:
                skill.showDamageArea = true;
                skill.damageAreaColor = new Color(1f, 0f, 0f, 0.3f);
                skill.damageAreaDisplayTime = 0.5f;
                break;
                
            case SkillType.Projectile:
                skill.showDamageArea = true;
                skill.damageAreaColor = new Color(1f, 1f, 0f, 0.3f);
                skill.damageAreaDisplayTime = 0.3f;
                break;
                
            case SkillType.Area:
                skill.showDamageArea = true;
                skill.damageAreaColor = new Color(0f, 1f, 1f, 0.3f);
                skill.damageAreaDisplayTime = 2f;
                break;
                
            case SkillType.Support:
                skill.showDamageArea = false;
                skill.damageAreaColor = new Color(0f, 1f, 0f, 0.3f);
                skill.damageAreaDisplayTime = 0f;
                break;
        }
    }

    private Color GetSkillColorByType(SkillType skillType)
    {
        return skillType switch
        {
            SkillType.Melee => Color.red,
            SkillType.Projectile => Color.yellow,
            SkillType.Area => Color.cyan,
            SkillType.Support => Color.green,
            _ => Color.white
        };
    }

    [System.Serializable]
    private struct SkillStats
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