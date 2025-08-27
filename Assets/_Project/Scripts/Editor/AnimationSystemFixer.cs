#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

/// <summary>
/// Tool để sửa các vấn đề Animation System trong dự án
/// </summary>
public class AnimationSystemFixer : EditorWindow
{
    [MenuItem("Tools/🔧 Animation System Fixer")]
    public static void ShowWindow()
    {
        GetWindow<AnimationSystemFixer>("🔧 Animation System Fixer");
    }

    void OnGUI()
    {
        GUILayout.Label("🔧 ANIMATION SYSTEM FIXER", EditorStyles.boldLabel);
        GUILayout.Label("Công cụ sửa các vấn đề animation trong dự án", EditorStyles.helpBox);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🎯 1. Tạo Boss Animator Controller", GUILayout.Height(30)))
        {
            CreateBossAnimatorController();
        }
        
        if (GUILayout.Button("🎯 2. Tạo Enemy Animator Controller", GUILayout.Height(30)))
        {
            CreateEnemyAnimatorController();
        }
        
        if (GUILayout.Button("🎯 3. Sửa GameObject.prefab (Player)", GUILayout.Height(30)))
        {
            FixPlayerPrefab();
        }
        
        if (GUILayout.Button("🎯 4. Kiểm tra tất cả Prefabs", GUILayout.Height(30)))
        {
            CheckAllPrefabs();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🚀 SỬA TẤT CẢ", GUILayout.Height(40)))
        {
            FixAllAnimationIssues();
        }
    }

    void CreateBossAnimatorController()
    {
        string path = "Assets/_Project/Animators/BossAnimator.controller";
        
        // Tạo thư mục nếu chưa có
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        
        // Thêm parameters theo AnimationParameters.cs
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Skill", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Berserk", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Teleport", AnimatorControllerParameterType.Trigger);
        
        // Boss-specific parameters
        controller.AddParameter("CastFireball", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AOECast", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("EnterBerserk", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsBerserk", AnimatorControllerParameterType.Bool);
        
        // Health-based parameters
        controller.AddParameter("HealthPercent", AnimatorControllerParameterType.Float);
        controller.AddParameter("PhaseNumber", AnimatorControllerParameterType.Int);
        
        // Create basic states
        var rootStateMachine = controller.layers[0].stateMachine;
        
        var idleState = rootStateMachine.AddState("Idle", new Vector3(300, 0, 0));
        var moveState = rootStateMachine.AddState("Move", new Vector3(300, 100, 0));
        var attackState = rootStateMachine.AddState("Attack", new Vector3(300, 200, 0));
        var hurtState = rootStateMachine.AddState("Hurt", new Vector3(500, 100, 0));
        var deathState = rootStateMachine.AddState("Death", new Vector3(500, 200, 0));
        var teleportState = rootStateMachine.AddState("Teleport", new Vector3(100, 100, 0));
        var fireballState = rootStateMachine.AddState("CastFireball", new Vector3(100, 200, 0));
        var aoeState = rootStateMachine.AddState("AOECast", new Vector3(100, 300, 0));
        var berserkState = rootStateMachine.AddState("EnterBerserk", new Vector3(100, 400, 0));
        
        // Set default state
        rootStateMachine.defaultState = idleState;
        
        // Create basic transitions
        CreateTransition(idleState, moveState, AnimatorConditionMode.Greater, 0.1f, "Speed");
        CreateTransition(moveState, idleState, AnimatorConditionMode.Less, 0.1f, "Speed");
        
        // Any State transitions
        CreateAnyStateTransition(rootStateMachine, attackState, "Attack");
        CreateAnyStateTransition(rootStateMachine, hurtState, "Hurt");
        CreateAnyStateTransition(rootStateMachine, deathState, "Die");
        CreateAnyStateTransition(rootStateMachine, teleportState, "Teleport");
        CreateAnyStateTransition(rootStateMachine, fireballState, "CastFireball");
        CreateAnyStateTransition(rootStateMachine, aoeState, "AOECast");
        CreateAnyStateTransition(rootStateMachine, berserkState, "EnterBerserk");
        
        AssetDatabase.SaveAssets();
        Debug.Log("✅ Đã tạo Boss Animator Controller: " + path);
    }
    
    void CreateEnemyAnimatorController()
    {
        string path = "Assets/_Project/Animators/EnemyAnimator.controller";
        
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        
        // Thêm parameters cơ bản cho Enemy
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        
        // Create basic states
        var rootStateMachine = controller.layers[0].stateMachine;
        
        var idleState = rootStateMachine.AddState("Idle", new Vector3(300, 0, 0));
        var moveState = rootStateMachine.AddState("Move", new Vector3(300, 100, 0));
        var attackState = rootStateMachine.AddState("Attack", new Vector3(300, 200, 0));
        var hurtState = rootStateMachine.AddState("Hurt", new Vector3(500, 100, 0));
        var deathState = rootStateMachine.AddState("Death", new Vector3(500, 200, 0));
        
        rootStateMachine.defaultState = idleState;
        
        // Create transitions
        CreateTransition(idleState, moveState, AnimatorConditionMode.Greater, 0.1f, "Speed");
        CreateTransition(moveState, idleState, AnimatorConditionMode.Less, 0.1f, "Speed");
        
        CreateAnyStateTransition(rootStateMachine, attackState, "Attack");
        CreateAnyStateTransition(rootStateMachine, hurtState, "Hurt");
        CreateAnyStateTransition(rootStateMachine, deathState, "Die");
        
        AssetDatabase.SaveAssets();
        Debug.Log("✅ Đã tạo Enemy Animator Controller: " + path);
    }
    
    void FixPlayerPrefab()
    {
        string prefabPath = "Assets/_Project/Scenes/GameObject.prefab";
        string controllerPath = "Assets/_Project/Animators/Player.controller";
        
        // Load prefab
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("Không tìm thấy GameObject.prefab!");
            return;
        }
        
        // Load controller
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError("Không tìm thấy Player.controller!");
            return;
        }
        
        // Tìm Animator component trong prefab
        Animator animator = prefab.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("Không tìm thấy Animator component trong GameObject.prefab!");
            return;
        }
        
        // Gán controller
        animator.runtimeAnimatorController = controller;
        
        // Lưu prefab
        PrefabUtility.SavePrefabAsset(prefab);
        Debug.Log("✅ Đã sửa GameObject.prefab - gán Player.controller");
    }
    
    void CheckAllPrefabs()
    {
        string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
        int fixedCount = 0;
        int totalChecked = 0;
        
        foreach (string guid in prefabGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab == null) continue;
            
            Animator animator = prefab.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                totalChecked++;
                
                if (animator.runtimeAnimatorController == null)
                {
                    Debug.LogWarning($"❌ {prefab.name} thiếu Animator Controller: {path}");
                    
                    // Tự động gán controller phù hợp
                    if (prefab.name.ToLower().Contains("player") || prefab.GetComponent<PlayerController>() != null)
                    {
                        var playerController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/_Project/Animators/Player.controller");
                        if (playerController != null)
                        {
                            animator.runtimeAnimatorController = playerController;
                            PrefabUtility.SavePrefabAsset(prefab);
                            fixedCount++;
                            Debug.Log($"✅ Đã gán Player.controller cho {prefab.name}");
                        }
                    }
                    else if (prefab.name.ToLower().Contains("boss") || prefab.name.ToLower().Contains("agis"))
                    {
                        var bossController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/_Project/Animators/BossAnimator.controller");
                        if (bossController != null)
                        {
                            animator.runtimeAnimatorController = bossController;
                            PrefabUtility.SavePrefabAsset(prefab);
                            fixedCount++;
                            Debug.Log($"✅ Đã gán BossAnimator.controller cho {prefab.name}");
                        }
                    }
                    else
                    {
                        var enemyController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/_Project/Animators/EnemyAnimator.controller");
                        if (enemyController != null)
                        {
                            animator.runtimeAnimatorController = enemyController;
                            PrefabUtility.SavePrefabAsset(prefab);
                            fixedCount++;
                            Debug.Log($"✅ Đã gán EnemyAnimator.controller cho {prefab.name}");
                        }
                    }
                }
                else
                {
                    // Kiểm tra controller có parameters không
                    var controller = animator.runtimeAnimatorController as AnimatorController;
                    if (controller != null && controller.parameters.Length == 0)
                    {
                        Debug.LogWarning($"⚠️ {prefab.name} có controller trống: {path}");
                    }
                }
            }
        }
        
        Debug.Log($"📊 Kiểm tra hoàn tất: {totalChecked} prefabs có Animator, đã sửa {fixedCount} prefabs");
    }
    
    void FixAllAnimationIssues()
    {
        Debug.Log("🚀 Bắt đầu sửa tất cả vấn đề animation...");
        
        CreateBossAnimatorController();
        CreateEnemyAnimatorController();
        FixPlayerPrefab();
        CheckAllPrefabs();
        
        Debug.Log("✅ Hoàn tất sửa tất cả vấn đề animation!");
    }
    
    // Helper methods
    void CreateTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold, string parameter)
    {
        var transition = from.AddTransition(to);
        transition.AddCondition(mode, threshold, parameter);
        transition.hasFixedDuration = true;
        transition.duration = 0.1f;
    }
    
    void CreateAnyStateTransition(AnimatorStateMachine stateMachine, AnimatorState state, string trigger)
    {
        var transition = stateMachine.AddAnyStateTransition(state);
        transition.AddCondition(AnimatorConditionMode.If, 0, trigger);
        transition.canTransitionToSelf = false;
        transition.duration = 0.1f;
    }
}
#endif
