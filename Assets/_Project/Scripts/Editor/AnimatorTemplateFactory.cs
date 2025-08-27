#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Helper ?? t?o c�c template m?u cho Animator Generator
/// </summary>
public static class AnimatorTemplateFactory
{
    [MenuItem("RPG Tools/📋 Tạo Templates Mẫu")]
    public static void CreateSampleTemplates()
    {
        CreateBasicEnemyTemplate();
        CreateEliteEnemyTemplate();
        CreateBossTemplate();
        CreateAdvancedBossTemplate();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("✅ Đã tạo tất cả templates mẫu trong Assets/_Project/Templates/");
    }
    
    static void CreateBasicEnemyTemplate()
    {
        var template = ScriptableObject.CreateInstance<AnimatorTemplate>();
        
        template.templateName = "Kẻ Địch Cơ Bản";
        template.description = "Template chuẩn cho enemy thường: Goblin, Orc, Skeleton, Wolf. Bao gồm 5 animation cơ bản với transition tối ưu cho enemy đơn giản.";
        template.animatorType = AnimatorType.BasicEnemy;
        template.autoSetupTransitions = true;
        template.createSubStateMachines = false;
        template.defaultTransitionDuration = 0.1f;
        template.autoAddAnimationEvents = true;
        template.attackHitFrame = 0.6f;
        template.footstepFrame = 0.5f;
        
        CreateTemplateAsset(template, "BasicEnemyTemplate");
    }
    
    static void CreateEliteEnemyTemplate()
    {
        var template = ScriptableObject.CreateInstance<AnimatorTemplate>();
        
        template.templateName = "Kẻ Địch Cao Cấp";
        template.description = "Template cho elite enemy với animations mượt mà hơn và transition nhanh hơn. Phù hợp cho enemy có nhiều kỹ năng đặc biệt.";
        template.animatorType = AnimatorType.EliteEnemy;
        template.autoSetupTransitions = true;
        template.createSubStateMachines = false;
        template.defaultTransitionDuration = 0.08f;
        template.autoAddAnimationEvents = true;
        template.attackHitFrame = 0.7f;
        template.footstepFrame = 0.5f;
        
        CreateTemplateAsset(template, "EliteEnemyTemplate");
    }
    
    static void CreateBossTemplate()
    {
        var template = ScriptableObject.CreateInstance<AnimatorTemplate>();
        
        template.templateName = "Boss";
        template.description = "Template cho boss với skills đặc biệt và nhiều phases. Bao gồm sub-state machines để tổ chức các animation states phức tạp.";
        template.animatorType = AnimatorType.Boss;
        template.autoSetupTransitions = true;
        template.createSubStateMachines = true;
        template.defaultTransitionDuration = 0.15f;
        template.autoAddAnimationEvents = true;
        template.attackHitFrame = 0.65f;
        template.footstepFrame = 0.3f;
        
        CreateTemplateAsset(template, "BossTemplate");
    }
    
    static void CreateAdvancedBossTemplate()
    {
        var template = ScriptableObject.CreateInstance<AnimatorTemplate>();
        
        template.templateName = "Boss Cao Cấp";
        template.description = "Template cho raid boss với hệ thống phức tạp và nhiều phases. Tối ưu cho boss có nhiều kỹ năng và trạng thái đặc biệt.";
        template.animatorType = AnimatorType.AdvancedBoss;
        template.autoSetupTransitions = true;
        template.createSubStateMachines = true;
        template.defaultTransitionDuration = 0.2f;
        template.autoAddAnimationEvents = true;
        template.attackHitFrame = 0.7f;
        template.footstepFrame = 0.25f;
        
        CreateTemplateAsset(template, "AdvancedBossTemplate");
    }
    
    static void CreateTemplateAsset(AnimatorTemplate template, string fileName)
    {
        string folderPath = "Assets/_Project/Templates";
        
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Templates");
        }
        
        string assetPath = $"{folderPath}/{fileName}.asset";
        AssetDatabase.CreateAsset(template, assetPath);
        
        Debug.Log($"📁 Đã tạo template: {assetPath}");
    }
    
    /*
    [MenuItem("RPG Tools/?? Quick Create Basic Enemy Animator")]
    public static void QuickCreateBasicEnemyAnimator()
    {
        // Open the tool and let user configure manually
        var window = EditorWindow.GetWindow(typeof(AnimatorGeneratorWindow), false, "Quick Basic Enemy");
        window.Show();
        window.Focus();
        
        Debug.Log("?? Opened Quick Create for Basic Enemy Animator");
    }
    
    [MenuItem("RPG Tools/?? Quick Create Boss Animator")]
    public static void QuickCreateBossAnimator()
    {
        // Open the tool and let user configure manually
        var window = EditorWindow.GetWindow(typeof(AnimatorGeneratorWindow), false, "Quick Boss");
        window.Show();
        window.Focus();
        
        Debug.Log("?? Opened Quick Create for Boss Animator");
    }
    */
}
#endif