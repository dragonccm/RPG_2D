#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor Window ?? t?o Animator Controller t? ??ng theo template
/// </summary>
public class AnimatorGeneratorWindow : EditorWindow
{
    [Header("?? Template & Settings")]
    private AnimatorTemplate selectedTemplate;
    private string outputPath = "Assets/_Project/Animators/";
    private string animatorName = "NewAnimatorController";
    private AnimatorType animatorType = AnimatorType.BasicEnemy;
    
    [Header("🎬 Gán Animation Thủ Công")]
    [Tooltip("Animation khi đứng yên")]
    private AnimationClip idleClip;
    
    [Tooltip("Animation khi di chuyển")]
    private AnimationClip walkClip;
    
    [Tooltip("Animation tấn công cơ bản (fallback)")]
    private AnimationClip attackClip;
    
    [Tooltip("Animation khi bị thương")]
    private AnimationClip hurtClip;
    
    [Tooltip("Animation khi chết")]
    private AnimationClip deathClip;
    
    // 3-directional attack animations for player (optimized system)
    [Header("⚔️ 3-Directional Attack Animations (Player + Flip)")]
    [Tooltip("Animation tấn công hướng lên")]
    private AnimationClip attackUpClip;
    
    [Tooltip("Animation tấn công hướng xuống")]
    private AnimationClip attackDownClip;
    
    [Tooltip("Animation tấn công ngang (dùng chung cho trái/phải + flip sprite)")]
    private AnimationClip attackLeftClip;
    
    // 3-directional attack animations for enemies (horizontal shared)
    [Header("⚔️ 3-Directional Attack Animations (Enemies)")]
    [Tooltip("Animation tấn công hướng lên (enemies)")]
    private AnimationClip enemyAttackUpClip;
    
    [Tooltip("Animation tấn công hướng xuống (enemies)")]
    private AnimationClip enemyAttackDownClip;
    
    [Tooltip("Animation tấn công ngang - trái/phải dùng chung (enemies)")]
    private AnimationClip enemyAttackHorizontalClip;
    
    // Boss animations
    [Tooltip("Animation skill đặc biệt 1")]
    private AnimationClip skill1Clip;
    
    [Tooltip("Animation skill đặc biệt 2")]
    private AnimationClip skill2Clip;
    
    [Tooltip("Animation skill đặc biệt 3")]
    private AnimationClip skill3Clip;
    
    [Tooltip("Animation ultimate skill")]
    private AnimationClip ultimateClip;
    
    [Tooltip("Animation teleport")]
    private AnimationClip teleportClip;
    
    [Tooltip("Animation berserk transformation")]
    private AnimationClip berserkClip;
    
    [Header("⚙️ Cài Đặt Tạo Animator")]
    [Tooltip("Tự động thiết lập các transition giữa các animation states")]
    private bool autoSetupTransitions = true;
    
    [Tooltip("Tạo các sub-state machines để tổ chức animation states theo nhóm")]
    private bool createSubStateMachines = true;
    
    [Tooltip("Tự động thêm animation events cho attack và footstep")]
    private bool autoAddAnimationEvents = true;
    
    [Tooltip("Thời gian transition mặc định giữa các states (giây)")]
    private float defaultTransitionDuration = 0.1f;
    
    [Tooltip("Thời điểm attack hit được kích hoạt (0-1, 0.6 = 60% của animation)")]
    private float attackHitFrame = 0.6f;
    
    private Vector2 scrollPosition;
    private bool showAdvancedSettings = false;
    private bool showBossAnimations = false;

    [Header("Player Animation Settings")]
    [Tooltip("Sử dụng một parameter 'FacingDirection' (Float) để quản lý hướng thay vì các state riêng biệt cho idle/walk. Yêu cầu setup Blend Tree trong Animator.")]
    private bool useFacingDirectionParameter = true;
    
    [Tooltip("Tự động gán controller cho các prefabs phù hợp sau khi tạo")]
    private bool autoAssignToPrefabs = true;

    [Tooltip("Sử dụng exit time cho các trạng thái tấn công để đảm bảo animation chạy hết.")]
    private bool useExitTimeForAttacks = true;

    [Tooltip("Tỷ lệ animation phải hoàn thành trước khi thoát (0.9 = 90%)")]
    [Range(0.5f, 1f)]
    private float attackExitTime = 0.9f;
    
    [MenuItem("RPG Tools/🎬 Tạo Animator", priority = 1)]
    public static void ShowWindow()
    {
        var window = GetWindow<AnimatorGeneratorWindow>("Tạo Animator");
        window.titleContent = new GUIContent("🎬 Tạo Animator", "Tool tạo Animator Controller tự động cho enemy và boss");
        window.minSize = new Vector2(400, 650);
        window.Show();
    }
    
    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Space(10);
        
        // Header
        EditorGUILayout.LabelField("🎬 TRÌNH TẠO ANIMATOR CONTROLLER", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Tạo Animator Controller tự động cho enemy và boss với các animation event và transition tối ưu", EditorStyles.helpBox);
        
        GUILayout.Space(10);
        
        // Template Selection
        DrawTemplateSection();
        
        GUILayout.Space(10);
        
        // Output Settings
        DrawOutputSection();
        
        GUILayout.Space(10);
        
        // Animation Assignment
        DrawAnimationSection();
        
        GUILayout.Space(10);
        
        // Advanced Settings
        DrawAdvancedSettings();
        
        GUILayout.Space(20);
        
        // Generation Buttons
        DrawGenerationButtons();
        
        EditorGUILayout.EndScrollView();
    }
    
    void DrawTemplateSection()
    {
        EditorGUILayout.LabelField("📋 Chọn Template", EditorStyles.boldLabel);
        
        using (new EditorGUILayout.VerticalScope("box"))
        {
            selectedTemplate = (AnimatorTemplate)EditorGUILayout.ObjectField(
                new GUIContent("Template Animation", "Chọn template có sẵn để tự động điền các animation clips"), 
                selectedTemplate, typeof(AnimatorTemplate), false);
            
            if (selectedTemplate != null)
            {
                EditorGUILayout.LabelField($"📝 {selectedTemplate.description}", EditorStyles.helpBox);
                
                if (GUILayout.Button("📥 Tải Cài Đặt Template"))
                {
                    LoadTemplateSettings();
                }
            }
            
            GUILayout.Space(5);
            
            animatorType = (AnimatorType)EditorGUILayout.EnumPopup(
                new GUIContent("Loại Animator", "Chọn loại enemy để tối ưu cấu trúc animator"), 
                animatorType);
            
            // Update boss animations visibility
            showBossAnimations = (animatorType == AnimatorType.Boss || animatorType == AnimatorType.AdvancedBoss) ||
                                (animatorType == AnimatorType.Player); // Show skill1 for player level up animation
        }
    }
    
    void DrawOutputSection()
    {
        EditorGUILayout.LabelField("📁 Cài Đặt Xuất File", EditorStyles.boldLabel);
        
        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                outputPath = EditorGUILayout.TextField(
                    new GUIContent("Thư Mục Xuất", "Đường dẫn thư mục sẽ lưu Animator Controller"), 
                    outputPath);
                if (GUILayout.Button("📁", GUILayout.Width(30)))
                {
                    string path = EditorUtility.OpenFolderPanel("Chọn thư mục xuất", "Assets", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        outputPath = "Assets" + path.Substring(Application.dataPath.Length) + "/";
                    }
                }
            }
            
            animatorName = EditorGUILayout.TextField(
                new GUIContent("Tên Animator", "Tên file Animator Controller sẽ được tạo"), 
                animatorName);
            
            // Preview output path
            string fullPath = Path.Combine(outputPath, animatorName + ".controller");
            EditorGUILayout.LabelField($"📄 Đường dẫn: {fullPath}", EditorStyles.miniLabel);
        }
    }
    
    void DrawAnimationSection()
    {
        EditorGUILayout.LabelField("🎞️ Animation Clips", EditorStyles.boldLabel);
        
        using (new EditorGUILayout.VerticalScope("box"))
        {
            // Basic Animations (always shown)
            EditorGUILayout.LabelField("🎯 Animation Cơ Bản", EditorStyles.label);
            idleClip = (AnimationClip)EditorGUILayout.ObjectField(
                new GUIContent("Đứng Yên", "Animation khi character không di chuyển"), 
                idleClip, typeof(AnimationClip), false);
            walkClip = (AnimationClip)EditorGUILayout.ObjectField(
                new GUIContent("Di Chuyển", "Animation khi character đang di chuyển"), 
                walkClip, typeof(AnimationClip), false);
            
            // Show attack clip for non-player types or as fallback for player
            if (animatorType != AnimatorType.Player)
            {
                attackClip = (AnimationClip)EditorGUILayout.ObjectField(
                    new GUIContent("Tấn Công", "Animation tấn công cơ bản"), 
                    attackClip, typeof(AnimationClip), false);
            }
            else
            {
                attackClip = (AnimationClip)EditorGUILayout.ObjectField(
                    new GUIContent("Tấn Công (Fallback)", "Animation tấn công dự phòng khi thiếu 4-directional"), 
                    attackClip, typeof(AnimationClip), false);
            }
            
            hurtClip = (AnimationClip)EditorGUILayout.ObjectField(
                new GUIContent("Bị Thương", "Animation khi nhận sát thương"), 
                hurtClip, typeof(AnimationClip), false);
            deathClip = (AnimationClip)EditorGUILayout.ObjectField(
                new GUIContent("Chết", "Animation khi character bị tiêu diệt"), 
                deathClip, typeof(AnimationClip), false);
            
            // 4-directional attack animations (Player only)
            if (animatorType == AnimatorType.Player)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("⚔️ 4-Directional Attack Animations (Player)", EditorStyles.label);
                attackUpClip = (AnimationClip)EditorGUILayout.ObjectField(
                    new GUIContent("Tấn Công Lên", "Animation tấn công hướng lên"), 
                    attackUpClip, typeof(AnimationClip), false);
                attackDownClip = (AnimationClip)EditorGUILayout.ObjectField(
                    new GUIContent("Tấn Công Xuống", "Animation tấn công hướng xuống"), 
                    attackDownClip, typeof(AnimationClip), false);
                attackLeftClip = (AnimationClip)EditorGUILayout.ObjectField(
                    new GUIContent("Tấn Công Ngang", "Animation ngang (dùng chung trái/phải + flip)"), 
                    attackLeftClip, typeof(AnimationClip), false);
            }
            
            // 3-directional attack animations (Enemies)
            if (animatorType != AnimatorType.Player)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("⚔️ 3-Directional Attack Animations (Enemies)", EditorStyles.label);
                enemyAttackUpClip = (AnimationClip)EditorGUILayout.ObjectField(
                    new GUIContent("Tấn Công Lên", "Animation tấn công hướng lên (enemy)"), 
                    enemyAttackUpClip, typeof(AnimationClip), false);
                enemyAttackDownClip = (AnimationClip)EditorGUILayout.ObjectField(
                    new GUIContent("Tấn Công Xuống", "Animation tấn công hướng xuống (enemy)"), 
                    enemyAttackDownClip, typeof(AnimationClip), false);
                enemyAttackHorizontalClip = (AnimationClip)EditorGUILayout.ObjectField(
                    new GUIContent("Tấn Công Ngang", "Animation tấn công hướng ngang - trái/phải dùng chung (enemy)"), 
                    enemyAttackHorizontalClip, typeof(AnimationClip), false);
            }
            
            // Boss Animations (conditional)
            if (showBossAnimations)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("👑 Animation Đặc Biệt Boss", EditorStyles.label);
                skill1Clip = (AnimationClip)EditorGUILayout.ObjectField(
                    new GUIContent("Kỹ Năng 1", "Skill cơ bản của boss (hoặc Level Up cho Player)"), 
                    skill1Clip, typeof(AnimationClip), false);
                skill2Clip = (AnimationClip)EditorGUILayout.ObjectField(
                    new GUIContent("Kỹ Năng 2", "Skill trung cấp của boss"), 
                    skill2Clip, typeof(AnimationClip), false);
                skill3Clip = (AnimationClip)EditorGUILayout.ObjectField(
                    new GUIContent("Kỹ Năng 3", "Skill cao cấp của boss"), 
                    skill3Clip, typeof(AnimationClip), false);
                ultimateClip = (AnimationClip)EditorGUILayout.ObjectField(
                    new GUIContent("Chiêu Cuối", "Ultimate skill của boss"), 
                    ultimateClip, typeof(AnimationClip), false);
                teleportClip = (AnimationClip)EditorGUILayout.ObjectField(
                    new GUIContent("Dịch Chuyển", "Animation teleport hoặc blink"), 
                    teleportClip, typeof(AnimationClip), false);
                berserkClip = (AnimationClip)EditorGUILayout.ObjectField(
                    new GUIContent("Berserk", "Animation khi boss vào trạng thái tức giận"), 
                    berserkClip, typeof(AnimationClip), false);
            }
            
            GUILayout.Space(5);
            
            // Quick assignment buttons
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("🔍 Tự Động Tìm"))
                {
                    AutoFindAnimationClips();
                }
                
                if (GUILayout.Button("🗑️ Xóa Tất Cả"))
                {
                    ClearAllAnimationClips();
                }
                
                if (animatorType == AnimatorType.Player && GUILayout.Button("🎮 Quick Player Setup"))
                {
                    QuickPlayerSetup();
                }
            }
        }
    }
    
    void DrawAdvancedSettings()
    {
        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, 
            new GUIContent("⚙️ Cài Đặt Nâng Cao", "Các tùy chỉnh chi tiết cho Animator"));
        
        if (showAdvancedSettings)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                autoSetupTransitions = EditorGUILayout.Toggle(
                    new GUIContent("Tự Động Transition", "Tự động thiết lập transition giữa các states"), 
                    autoSetupTransitions);
                createSubStateMachines = EditorGUILayout.Toggle(
                    new GUIContent("Tạo Sub-State Machines", "Tạo các nhóm states để dễ quản lý"), 
                    createSubStateMachines);
                autoAddAnimationEvents = EditorGUILayout.Toggle(
                    new GUIContent("Tự Động Events", "Tự động thêm animation events cho attack và footstep"), 
                    autoAddAnimationEvents);
                
                GUILayout.Space(5);
                
                defaultTransitionDuration = EditorGUILayout.Slider(
                    new GUIContent("Thời Gian Transition", "Thời gian chuyển tiếp giữa các animation states (giây)"), 
                    defaultTransitionDuration, 0f, 1f);
                attackHitFrame = EditorGUILayout.Slider(
                    new GUIContent("Thời Điểm Hit", "Tỷ lệ thời gian trong animation mà attack hit được kích hoạt (0-1)"), 
                    attackHitFrame, 0f, 1f);
            }
        }
    }
    
    void DrawGenerationButtons()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("🚀 Tạo Animator", EditorStyles.boldLabel);
            
            // Validation
            bool canGenerate = ValidateInputs();
            
            if (!canGenerate)
            {
                EditorGUILayout.HelpBox("⚠️ Thiếu animation clips cần thiết! Cần ít nhất: Đứng Yên, Di Chuyển, Tấn Công, Bị Thương, Chết", MessageType.Warning);
            }
            
            using (new EditorGUI.DisabledScope(!canGenerate))
            {
                // Main generation button
                if (GUILayout.Button("🎯 TẠO ANIMATOR CONTROLLER", GUILayout.Height(40)))
                {
                    GenerateAnimatorController();
                }
            }
            
            GUILayout.Space(5);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("💾 Tạo Template"))
                {
                    CreateTemplateFromCurrentSettings();
                }
                
                if (GUILayout.Button("🔄 Reset Cài Đặt"))
                {
                    ResetToDefaults();
                }
            }
        }
    }
    
    bool ValidateInputs()
    {
        // Basic required animations
        if (idleClip == null || walkClip == null || attackClip == null || 
            hurtClip == null || deathClip == null)
        {
            return false;
        }
        
        // Valid output path and name
        if (string.IsNullOrEmpty(outputPath) || string.IsNullOrEmpty(animatorName))
        {
            return false;
        }
        
        return true;
    }
    
    void LoadTemplateSettings()
    {
        if (selectedTemplate == null) return;
        
        // Load animation clips
        idleClip = selectedTemplate.idleClip;
        walkClip = selectedTemplate.walkClip;
        attackClip = selectedTemplate.attackClip;
        hurtClip = selectedTemplate.hurtClip;
        deathClip = selectedTemplate.deathClip;
        
        skill1Clip = selectedTemplate.skill1Clip;
        skill2Clip = selectedTemplate.skill2Clip;
        skill3Clip = selectedTemplate.skill3Clip;
        ultimateClip = selectedTemplate.ultimateClip;
        teleportClip = selectedTemplate.teleportClip;
        berserkClip = selectedTemplate.berserkClip;
        
        // Load settings
        animatorType = selectedTemplate.animatorType;
        autoSetupTransitions = selectedTemplate.autoSetupTransitions;
        createSubStateMachines = selectedTemplate.createSubStateMachines;
        defaultTransitionDuration = selectedTemplate.defaultTransitionDuration;
        autoAddAnimationEvents = selectedTemplate.autoAddAnimationEvents;
        attackHitFrame = selectedTemplate.attackHitFrame;
        
        // Update name
        animatorName = selectedTemplate.templateName.Replace(" ", "");
        
        Debug.Log($"✅ Đã tải template: {selectedTemplate.templateName}");
    }
    
    public void AutoFindAnimationClips()
    {
        // Try to find clips based on naming conventions
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets" });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            
            if (clip == null) continue;
            
            string clipName = clip.name.ToLower();
            
            // Basic animations
            if (idleClip == null && (clipName.Contains("idle") || clipName.Contains("stand")))
                idleClip = clip;
            else if (walkClip == null && (clipName.Contains("walk") || clipName.Contains("run") || clipName.Contains("move")))
                walkClip = clip;
            else if (attackClip == null && clipName.Contains("attack") && !clipName.Contains("up") && !clipName.Contains("down") && !clipName.Contains("left") && !clipName.Contains("right"))
                attackClip = clip;
            else if (hurtClip == null && (clipName.Contains("hurt") || clipName.Contains("hit") || clipName.Contains("damage")))
                hurtClip = clip;
            else if (deathClip == null && (clipName.Contains("death") || clipName.Contains("die")))
                deathClip = clip;
            
            // 3-directional attack animations (Up/Down/Horizontal)
            else if (attackUpClip == null && clipName.Contains("attack") && clipName.Contains("up"))
                attackUpClip = clip;
            else if (attackDownClip == null && clipName.Contains("attack") && clipName.Contains("down"))
                attackDownClip = clip;
            else if (attackLeftClip == null && clipName.Contains("attack") && (clipName.Contains("left") || clipName.Contains("horizontal")))
                attackLeftClip = clip;
            
            // 3-directional attack animations
            else if (enemyAttackUpClip == null && clipName.Contains("attack") && clipName.Contains("up"))
                enemyAttackUpClip = clip;
            else if (enemyAttackDownClip == null && clipName.Contains("attack") && clipName.Contains("down"))
                enemyAttackDownClip = clip;
            else if (enemyAttackHorizontalClip == null && (clipName.Contains("attack") && (clipName.Contains("left") || clipName.Contains("right") || clipName.Contains("horizontal"))))
                enemyAttackHorizontalClip = clip;
            
            // Boss animations
            if (showBossAnimations)
            {
                if (skill1Clip == null && (clipName.Contains("skill1") || clipName.Contains("fireball")))
                    skill1Clip = clip;
                else if (skill2Clip == null && (clipName.Contains("skill2") || clipName.Contains("lightning")))
                    skill2Clip = clip;
                else if (skill3Clip == null && clipName.Contains("skill3"))
                    skill3Clip = clip;
                else if (ultimateClip == null && clipName.Contains("ultimate"))
                    ultimateClip = clip;
                else if (teleportClip == null && clipName.Contains("teleport"))
                    teleportClip = clip;
                else if (berserkClip == null && clipName.Contains("berserk"))
                    berserkClip = clip;
            }
        }
        
        Debug.Log("🔍 Đã tìm thấy animation clips tự động theo quy ước đặt tên");
    }
    
    void ClearAllAnimationClips()
    {
        idleClip = walkClip = attackClip = hurtClip = deathClip = null;
        attackUpClip = attackDownClip = attackLeftClip = null;
        enemyAttackUpClip = enemyAttackDownClip = enemyAttackHorizontalClip = null;
        skill1Clip = skill2Clip = skill3Clip = ultimateClip = teleportClip = berserkClip = null;
        
        Debug.Log("🗑️ Đã xóa tất cả animation clips");
    }
    
    public void ResetToDefaults()
    {
        selectedTemplate = null;
        animatorName = "NewAnimatorController";
        animatorType = AnimatorType.BasicEnemy;
        
        ClearAllAnimationClips();
        
        autoSetupTransitions = true;
        createSubStateMachines = true;
        autoAddAnimationEvents = true;
        defaultTransitionDuration = 0.1f;
        attackHitFrame = 0.6f;
        
        Debug.Log("🔄 Đã reset về cài đặt mặc định");
    }
    
    void CreateTemplateFromCurrentSettings()
    {
        AnimatorTemplate template = CreateInstance<AnimatorTemplate>();
        
        // Set properties
        template.templateName = animatorName;
        template.description = $"Template được tạo từ loại {animatorType} với các animation clips tùy chỉnh";
        
        // Animation clips
        template.idleClip = idleClip;
        template.walkClip = walkClip;
        template.attackClip = attackClip;
        template.hurtClip = hurtClip;
        template.deathClip = deathClip;
        
        template.skill1Clip = skill1Clip;
        template.skill2Clip = skill2Clip;
        template.skill3Clip = skill3Clip;
        template.ultimateClip = ultimateClip;
        template.teleportClip = teleportClip;
        template.berserkClip = berserkClip;
        
        // Settings
        template.animatorType = animatorType;
        template.autoSetupTransitions = autoSetupTransitions;
        template.createSubStateMachines = createSubStateMachines;
        template.defaultTransitionDuration = defaultTransitionDuration;
        template.autoAddAnimationEvents = autoAddAnimationEvents;
        template.attackHitFrame = attackHitFrame;
        
        // Save
        string templatePath = Path.Combine(outputPath, animatorName + "_Template.asset");
        AssetDatabase.CreateAsset(template, templatePath);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"?? Created template at: {templatePath}");
    }
    
    void GenerateAnimatorController()
    {
        try
        {
            // Ensure output directory exists
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            
            // Create the animator controller
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(
                Path.Combine(outputPath, animatorName + ".controller"));
            
            // Generate based on type
            switch (animatorType)
            {
                case AnimatorType.Player:
                    GeneratePlayerAnimator(controller);
                    break;
                case AnimatorType.BasicEnemy:
                    GenerateBasicEnemyAnimator(controller);
                    break;
                case AnimatorType.EliteEnemy:
                    GenerateEliteEnemyAnimator(controller);
                    break;
                case AnimatorType.Boss:
                    GenerateBossAnimator(controller);
                    break;
                case AnimatorType.AdvancedBoss:
                    GenerateAdvancedBossAnimator(controller);
                    break;
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Select the created controller
            Selection.activeObject = controller;
            EditorGUIUtility.PingObject(controller);
            
            Debug.Log($"?? Successfully generated Animator Controller: {controller.name}");
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"? Failed to generate Animator Controller: {e.Message}");
        }
    }
    
    void GenerateBasicEnemyAnimator(AnimatorController controller)
    {
        Debug.Log("?? Generating Basic Enemy Animator...");
        
        // Add parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("EnemyAttackUp", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("EnemyAttackDown", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("EnemyAttackHorizontal", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        
        var rootStateMachine = controller.layers[0].stateMachine;
        
        // Create states
        var idleState = rootStateMachine.AddState("Idle");
        var walkState = rootStateMachine.AddState("Walk");
        var attackState = rootStateMachine.AddState("Attack");
        var enemyAttackUpState = rootStateMachine.AddState("EnemyAttackUp");
        var enemyAttackDownState = rootStateMachine.AddState("EnemyAttackDown");
        var enemyAttackHorizontalState = rootStateMachine.AddState("EnemyAttackHorizontal");
        var hurtState = rootStateMachine.AddState("Hurt");
        var deathState = rootStateMachine.AddState("Death");
        
        // Assign motion clips
        idleState.motion = idleClip;
        walkState.motion = walkClip;
        attackState.motion = attackClip;
        enemyAttackUpState.motion = enemyAttackUpClip != null ? enemyAttackUpClip : attackClip;
        enemyAttackDownState.motion = enemyAttackDownClip != null ? enemyAttackDownClip : attackClip;
        enemyAttackHorizontalState.motion = enemyAttackHorizontalClip != null ? enemyAttackHorizontalClip : attackClip;
        
        // Set default state
        rootStateMachine.defaultState = idleState;
        
        if (autoSetupTransitions)
        {
            SetupBasicEnemyTransitions(rootStateMachine, idleState, walkState, attackState, hurtState, deathState);
            Setup3DirectionalAttackTransitions(rootStateMachine, idleState, walkState, enemyAttackUpState, enemyAttackDownState, enemyAttackHorizontalState);
        }
        
        if (autoAddAnimationEvents)
        {
            AddBasicAnimationEvents();
        }
    }
    
    void SetupBasicEnemyTransitions(AnimatorStateMachine stateMachine, 
        AnimatorState idle, AnimatorState walk, AnimatorState attack, 
        AnimatorState hurt, AnimatorState death)
    {
        AnimatorTransitionHelper.SetupBasicEnemyTransitions(
            stateMachine, idle, walk, attack, hurt, death, defaultTransitionDuration);
        
        // Optimize state positions
        AnimatorTransitionHelper.OptimizeStatePositions(stateMachine);
    }
    
    void GenerateBossAnimator(AnimatorController controller)
    {
        Debug.Log("?? Generating Boss Animator...");
        
        // Add all parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Skill1", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Skill2", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Skill3", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("UltimateSkill", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsBerserk", AnimatorControllerParameterType.Bool);
        controller.AddParameter("EnterBerserk", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Teleport", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("HealthPercent", AnimatorControllerParameterType.Float);
        controller.AddParameter("PhaseNumber", AnimatorControllerParameterType.Int);
        controller.AddParameter("IsChanneling", AnimatorControllerParameterType.Bool);
        
        var rootStateMachine = controller.layers[0].stateMachine;
        
        // Create basic states
        var idleState = rootStateMachine.AddState("Idle");
        var walkState = rootStateMachine.AddState("Walk");
        var attackState = rootStateMachine.AddState("Attack");
        var hurtState = rootStateMachine.AddState("Hurt");
        var deathState = rootStateMachine.AddState("Death");
        
        // Boss special states
        AnimatorState skill1State = null, skill2State = null, ultimateState = null;
        AnimatorState teleportState = null, berserkState = null;
        
        if (skill1Clip != null)
        {
            skill1State = rootStateMachine.AddState("Skill1");
            skill1State.motion = skill1Clip;
        }
        
        if (skill2Clip != null)
        {
            skill2State = rootStateMachine.AddState("Skill2");
            skill2State.motion = skill2Clip;
        }
        
        if (ultimateClip != null)
        {
            ultimateState = rootStateMachine.AddState("Ultimate");
            ultimateState.motion = ultimateClip;
        }
        
        if (teleportClip != null)
        {
            teleportState = rootStateMachine.AddState("Teleport");
            teleportState.motion = teleportClip;
        }
        
        if (berserkClip != null)
        {
            berserkState = rootStateMachine.AddState("Berserk");
            berserkState.motion = berserkClip;
        }
        
        // Assign basic motions
        idleState.motion = idleClip;
        walkState.motion = walkClip;
        attackState.motion = attackClip;
        hurtState.motion = hurtClip;
        deathState.motion = deathClip;
        
        // Set default state
        rootStateMachine.defaultState = idleState;
        
        if (autoSetupTransitions)
        {
            SetupBossTransitions(rootStateMachine, idleState, walkState, attackState, 
                hurtState, deathState, skill1State, skill2State, ultimateState, 
                teleportState, berserkState);
        }
        
        if (autoAddAnimationEvents)
        {
            AddBossAnimationEvents();
        }
    }
    
    void SetupBossTransitions(AnimatorStateMachine stateMachine,
        AnimatorState idle, AnimatorState walk, AnimatorState attack,
        AnimatorState hurt, AnimatorState death,
        AnimatorState skill1, AnimatorState skill2, AnimatorState ultimate,
        AnimatorState teleport, AnimatorState berserk)
    {
        AnimatorTransitionHelper.SetupBossTransitions(
            stateMachine, idle, walk, attack, hurt, death,
            skill1, skill2, ultimate, teleport, berserk, defaultTransitionDuration);
        
        // Optimize state positions
        AnimatorTransitionHelper.OptimizeStatePositions(stateMachine);
    }

    void Setup3DirectionalAttackTransitions(AnimatorStateMachine stateMachine, AnimatorState idle, AnimatorState walk,
                                          AnimatorState attackUp, AnimatorState attackDown, AnimatorState attackHorizontal)
    {
        // Attack Up transition
        var anyState = stateMachine.AddAnyStateTransition(attackUp);
        anyState.AddCondition(AnimatorConditionMode.If, 0, "EnemyAttackUp");
        anyState.canTransitionToSelf = false;
        
        // Attack Down transition
        anyState = stateMachine.AddAnyStateTransition(attackDown);
        anyState.AddCondition(AnimatorConditionMode.If, 0, "EnemyAttackDown");
        anyState.canTransitionToSelf = false;
        
        // Attack Horizontal transition (for both left and right)
        anyState = stateMachine.AddAnyStateTransition(attackHorizontal);
        anyState.AddCondition(AnimatorConditionMode.If, 0, "EnemyAttackHorizontal");
        anyState.canTransitionToSelf = false;
        
        // Return to idle from 3-directional attacks
        var idleTransition = attackUp.AddTransition(idle);
        idleTransition.hasExitTime = true;
        idleTransition.exitTime = 0.9f;
        idleTransition.duration = 0.1f;
        
        idleTransition = attackDown.AddTransition(idle);
        idleTransition.hasExitTime = true;
        idleTransition.exitTime = 0.9f;
        idleTransition.duration = 0.1f;
        
        idleTransition = attackHorizontal.AddTransition(idle);
        idleTransition.hasExitTime = true;
        idleTransition.exitTime = 0.9f;
        idleTransition.duration = 0.1f;
    }
    
    void GenerateEliteEnemyAnimator(AnimatorController controller)
    {
        Debug.Log("?? Generating Elite Enemy Animator...");
        GenerateBasicEnemyAnimator(controller);
        // Add additional elite-specific features
    }
    
    void GenerateAdvancedBossAnimator(AnimatorController controller)
    {
        Debug.Log("?? Generating Advanced Boss Animator...");
        GenerateBossAnimator(controller);
        // Add sub-state machines and advanced features
    }
    
    void AddBasicAnimationEvents()
    {
        AnimatorTransitionHelper.SetupBasicAnimationEvents(
            attackClip, walkClip, attackHitFrame, 0.5f);
    }
    
    void AddBossAnimationEvents()
    {
        AnimatorTransitionHelper.SetupBossAnimationEvents(
            attackClip, skill1Clip, skill2Clip, ultimateClip, teleportClip);
    }

    void GeneratePlayerAnimator(AnimatorController controller)
    {
        Debug.Log("🎮 Generating Player Animator with 4-Directional Attacks...");
        
        // Add basic parameters - COMPLETE SET
        AddPlayerParameters(controller);
        
        // Validate and fix parameter types
        ValidateAndFixParameterTypes(controller);

        // Create sub-state machines and states
        if (createSubStateMachines)
        {
            CreatePlayerSubStateMachines(controller);
        }
        else
        {
            CreatePlayerFlatStructure(controller);
        }

        // Add animation events if enabled
        if (autoAddAnimationEvents)
        {
            AddPlayerAnimationEvents();
        }
        
        // Final validation
        ValidateAnimatorController(controller);
        
        // Auto-assign to existing player prefabs if enabled
        if (autoAssignToPrefabs)
        {
            AutoAssignControllerToPrefabs(controller, "Player");
        }

        Debug.Log("✅ Player Animator generation complete - All parameters and states verified!");
    }

    private void AddPlayerParameters(AnimatorController controller)
    {
        // Movement parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        
        // Combat parameters - 3-Animation System (Up/Down/Horizontal+Flip)
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger); // Fallback attack
        controller.AddParameter("AttackUp", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackDown", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackLeft", AnimatorControllerParameterType.Trigger); // Used for both Left/Right with flip
        
        // Status parameters
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        
        // Special parameters
        controller.AddParameter("LevelUp", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("UseSkill", AnimatorControllerParameterType.Trigger);
        
        // Add FacingDirection parameter if using the new system
        if (useFacingDirectionParameter)
        {
            controller.AddParameter("FacingDirection", AnimatorControllerParameterType.Float);
        }
    }

    private void CreatePlayerSubStateMachines(AnimatorController controller)
    {
        var rootStateMachine = controller.layers[0].stateMachine;
        
        // Create sub-state machines
        var movementSM = rootStateMachine.AddStateMachine("Movement", new Vector3(300, 0, 0));
        var combatSM = rootStateMachine.AddStateMachine("Combat", new Vector3(300, 100, 0)); 
        var statusSM = rootStateMachine.AddStateMachine("Status", new Vector3(300, 200, 0));

        // Setup movement states (idle, walk with blend tree)
        SetupMovementStates(movementSM);

        // Setup combat states (4-directional attacks)
        SetupCombatStates(combatSM);

        // Setup status states (hurt, death, levelup)
        SetupStatusStates(statusSM);

        // Set default state
        rootStateMachine.defaultState = movementSM.defaultState;
    }

    private void SetupMovementStates(AnimatorStateMachine movementSM)
    {
        // Create states
        var idleState = movementSM.AddState("Idle", new Vector3(300, 0, 0));
        var walkState = movementSM.AddState("Walk", new Vector3(300, 100, 0));

        if (useFacingDirectionParameter)
        {
            // Create blend trees for 4-directional movement
            idleState.motion = CreateDirectionalBlendTree("Idle", idleClip);
            walkState.motion = CreateDirectionalBlendTree("Walk", walkClip);
        }
        else 
        {
            idleState.motion = idleClip;
            walkState.motion = walkClip;
        }

        // Add transitions
        var idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.hasFixedDuration = true;
        idleToWalk.duration = defaultTransitionDuration;
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.hasFixedDuration = true;
        walkToIdle.duration = defaultTransitionDuration;
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // Set as default state for this sub-state machine
        movementSM.defaultState = idleState;
    }

    private BlendTree CreateDirectionalBlendTree(string name, AnimationClip clip)
    {
        var blendTree = new BlendTree();
        blendTree.name = name;
        blendTree.blendParameter = "FacingDirection";
        blendTree.blendType = BlendTreeType.Simple1D;
        
        // Add motion points for each direction
        // Up = 2, Down = -2, Left = -1, Right = 1
        blendTree.AddChild(clip, 2f);    // Up
        blendTree.AddChild(clip, -2f);   // Down
        blendTree.AddChild(clip, -1f);   // Left  
        blendTree.AddChild(clip, 1f);    // Right

        return blendTree;
    }

    private void SetupCombatStates(AnimatorStateMachine combatSM) 
    {
        // Create attack states - 3-Animation System
        var attackUpState = combatSM.AddState("AttackUp", new Vector3(300, 0, 0));
        var attackDownState = combatSM.AddState("AttackDown", new Vector3(300, 100, 0));
        var attackLeftState = combatSM.AddState("AttackLeft", new Vector3(100, 50, 0)); // Used for both Left/Right

        // Assign clips with fallbacks
        attackUpState.motion = attackUpClip ?? attackClip;
        attackDownState.motion = attackDownClip ?? attackClip;
        attackLeftState.motion = attackLeftClip ?? attackClip; // Shared for Left/Right with sprite flip

        // Setup transitions for each attack state - 3-Animation System
        SetupAttackStateTransitions(combatSM, attackUpState, "AttackUp");
        SetupAttackStateTransitions(combatSM, attackDownState, "AttackDown");
        SetupAttackStateTransitions(combatSM, attackLeftState, "AttackLeft"); // Handles both Left/Right
    }

    private void SetupAttackStateTransitions(AnimatorStateMachine stateMachine, AnimatorState attackState, string triggerName)
    {
        // Transition from any state to attack
        var toAttack = stateMachine.AddAnyStateTransition(attackState);
        toAttack.AddCondition(AnimatorConditionMode.If, 0, triggerName);
        toAttack.duration = 0.1f;
        toAttack.canTransitionToSelf = false;

        // Return to movement state
        if (useExitTimeForAttacks)
        {
            var fromAttack = attackState.AddExitTransition();
            fromAttack.hasExitTime = true;
            fromAttack.exitTime = attackExitTime;
            fromAttack.hasFixedDuration = true;
            fromAttack.duration = 0.1f;
        }
    }

    private void SetupStatusStates(AnimatorStateMachine statusSM)
    {
        // Create states
        var hurtState = statusSM.AddState("Hurt", new Vector3(300, 0, 0));
        var deathState = statusSM.AddState("Death", new Vector3(300, 100, 0));
        var levelUpState = skill1Clip != null ? statusSM.AddState("LevelUp", new Vector3(300, 200, 0)) : null;

        // Assign motions
        hurtState.motion = hurtClip;
        deathState.motion = deathClip;
        if (levelUpState != null)
            levelUpState.motion = skill1Clip;

        // Setup hurt transitions
        var toHurt = statusSM.AddAnyStateTransition(hurtState);
        toHurt.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
        toHurt.duration = 0.05f;
        toHurt.canTransitionToSelf = false;

        var fromHurt = hurtState.AddExitTransition();
        fromHurt.hasExitTime = true;
        fromHurt.exitTime = 0.8f;
        fromHurt.duration = 0.1f;

        // Setup death transition
        var toDeath = statusSM.AddAnyStateTransition(deathState);
        toDeath.AddCondition(AnimatorConditionMode.If, 0, "Die");
        toDeath.duration = 0.1f;
        toDeath.canTransitionToSelf = false;

        // Setup level up transition if available
        if (levelUpState != null)
        {
            var toLevelUp = statusSM.AddAnyStateTransition(levelUpState);
            toLevelUp.AddCondition(AnimatorConditionMode.If, 0, "LevelUp");
            toLevelUp.duration = 0.1f;
            toLevelUp.canTransitionToSelf = false;

            var fromLevelUp = levelUpState.AddExitTransition();
            fromLevelUp.hasExitTime = true;
            fromLevelUp.exitTime = 0.9f;
            fromLevelUp.duration = 0.2f;
        }
    }

    private void CreatePlayerFlatStructure(AnimatorController controller)
    {
        var rootStateMachine = controller.layers[0].stateMachine;

        // Create all states - 3-Animation System
        var idleState = rootStateMachine.AddState("Idle", new Vector3(300, 0, 0));
        var walkState = rootStateMachine.AddState("Walk", new Vector3(300, 100, 0));
        var attackUpState = rootStateMachine.AddState("AttackUp", new Vector3(500, -100, 0));
        var attackDownState = rootStateMachine.AddState("AttackDown", new Vector3(500, 0, 0));
        var attackLeftState = rootStateMachine.AddState("AttackLeft", new Vector3(500, 100, 0)); // Shared for Left/Right
        var hurtState = rootStateMachine.AddState("Hurt", new Vector3(100, 100, 0));
        var deathState = rootStateMachine.AddState("Death", new Vector3(100, 200, 0));
        
        // Assign motions
        if (useFacingDirectionParameter)
        {
            idleState.motion = CreateDirectionalBlendTree("Idle", idleClip);
            walkState.motion = CreateDirectionalBlendTree("Walk", walkClip);
        }
        else
        {
            idleState.motion = idleClip;
            walkState.motion = walkClip;
        }

        attackUpState.motion = attackUpClip ?? attackClip;
        attackDownState.motion = attackDownClip ?? attackClip;
        attackLeftState.motion = attackLeftClip ?? attackClip; // Shared for Left/Right with flip
        hurtState.motion = hurtClip;
        deathState.motion = deathClip;

        // Setup transitions - 3-Animation System
        SetupPlayerFlatTransitions(rootStateMachine, idleState, walkState,
            attackUpState, attackDownState, attackLeftState, null,
            hurtState, deathState, null);

        // Set default state
        rootStateMachine.defaultState = idleState;
    }

    void SetupPlayerFlatTransitions(AnimatorStateMachine stateMachine, 
        AnimatorState idle, AnimatorState walk,
        AnimatorState attackUp, AnimatorState attackDown, AnimatorState attackLeft, AnimatorState attackRight,
        AnimatorState hurt, AnimatorState death, AnimatorState levelUp)
    {
        // Basic movement transitions
        var idleToWalk = idle.AddTransition(walk);
        idleToWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        idleToWalk.duration = defaultTransitionDuration;
        
        var walkToIdle = walk.AddTransition(idle);
        walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        walkToIdle.duration = defaultTransitionDuration;
        
        // 4-directional attack transitions from Any State
        var anyStateAttackUp = stateMachine.AddAnyStateTransition(attackUp);
        anyStateAttackUp.AddCondition(AnimatorConditionMode.If, 0, "AttackUp");
        anyStateAttackUp.canTransitionToSelf = false;
        anyStateAttackUp.duration = 0.1f;
        
        var anyStateAttackDown = stateMachine.AddAnyStateTransition(attackDown);
        anyStateAttackDown.AddCondition(AnimatorConditionMode.If, 0, "AttackDown");
        anyStateAttackDown.canTransitionToSelf = false;
        anyStateAttackDown.duration = 0.1f;
        
        var anyStateAttackLeft = stateMachine.AddAnyStateTransition(attackLeft);
        anyStateAttackLeft.AddCondition(AnimatorConditionMode.If, 0, "AttackLeft");
        anyStateAttackLeft.canTransitionToSelf = false;
        anyStateAttackLeft.duration = 0.1f;
        
        var anyStateAttackRight = stateMachine.AddAnyStateTransition(attackRight);
        anyStateAttackRight.AddCondition(AnimatorConditionMode.If, 0, "AttackRight");
        anyStateAttackRight.canTransitionToSelf = false;
        anyStateAttackRight.duration = 0.1f;
        
        // Return to idle from attacks
        var attackUpToIdle = attackUp.AddTransition(idle);
        attackUpToIdle.hasExitTime = useExitTimeForAttacks;
        if (useExitTimeForAttacks)
        {
            attackUpToIdle.exitTime = attackExitTime;
        }
        attackUpToIdle.duration = 0.1f;
        
        var attackDownToIdle = attackDown.AddTransition(idle);
        attackDownToIdle.hasExitTime = useExitTimeForAttacks;
        if (useExitTimeForAttacks)
        {
            attackDownToIdle.exitTime = attackExitTime;
        }
        attackDownToIdle.duration = 0.1f;
        
        var attackLeftToIdle = attackLeft.AddTransition(idle);
        attackLeftToIdle.hasExitTime = useExitTimeForAttacks;
        if (useExitTimeForAttacks)
        {
            attackLeftToIdle.exitTime = attackExitTime;
        }
        attackLeftToIdle.duration = 0.1f;
        
        var attackRightToIdle = attackRight.AddTransition(idle);
        attackRightToIdle.hasExitTime = useExitTimeForAttacks;
        if (useExitTimeForAttacks)
        {
            attackRightToIdle.exitTime = attackExitTime;
        }
        attackRightToIdle.duration = 0.1f;
        
        // Hurt transitions
        var anyStateHurt = stateMachine.AddAnyStateTransition(hurt);
        anyStateHurt.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
        anyStateHurt.canTransitionToSelf = false;
        anyStateHurt.duration = 0.05f;
        
        var hurtToIdle = hurt.AddTransition(idle);
        hurtToIdle.hasExitTime = true;
        hurtToIdle.exitTime = 0.8f;
        hurtToIdle.duration = 0.1f;
        
        // Death transition
        var anyStateDeath = stateMachine.AddAnyStateTransition(death);
        anyStateDeath.AddCondition(AnimatorConditionMode.If, 0, "Die");
        anyStateDeath.canTransitionToSelf = false;
        anyStateDeath.duration = 0.1f;
        
        // Level up transition (if available)
        if (levelUp != null)
        {
            var anyStateLevelUp = stateMachine.AddAnyStateTransition(levelUp);
            anyStateLevelUp.AddCondition(AnimatorConditionMode.If, 0, "LevelUp");
            anyStateLevelUp.canTransitionToSelf = false;
            anyStateLevelUp.duration = 0.1f;
            
            var levelUpToIdle = levelUp.AddTransition(idle);
            levelUpToIdle.hasExitTime = true;
            levelUpToIdle.exitTime = 0.9f;
            levelUpToIdle.duration = 0.2f;
        }
        
        // Optimize state positions
        AnimatorTransitionHelper.OptimizeStatePositions(stateMachine);
    }
    
    void AddPlayerAnimationEvents()
    {
        // Add events for all attack animations
        if (attackUpClip != null)
            AnimatorTransitionHelper.SetupPlayerAttackEvents(attackUpClip, attackHitFrame);
        if (attackDownClip != null)
            AnimatorTransitionHelper.SetupPlayerAttackEvents(attackDownClip, attackHitFrame);
        if (attackLeftClip != null)
            AnimatorTransitionHelper.SetupPlayerAttackEvents(attackLeftClip, attackHitFrame); // Handles both Left/Right
        
        // Add footstep events for walk animation
        if (walkClip != null)
            AnimatorTransitionHelper.SetupFootstepEvents(walkClip);
    }

    /// <summary>
    /// Quick setup for player with common settings
    /// </summary>
    void QuickPlayerSetup()
    {
        animatorName = "PlayerController";
        animatorType = AnimatorType.Player;
        autoSetupTransitions = true;
        createSubStateMachines = false;
        autoAddAnimationEvents = true;
        defaultTransitionDuration = 0.05f; // Faster transitions for responsive player
        attackHitFrame = 0.5f; // Earlier hit timing for player
        useFacingDirectionParameter = true;
        useExitTimeForAttacks = true;
        attackExitTime = 0.9f;
        
        Debug.Log("🎮 Quick Player Setup applied! Ready for 4-directional attack animations.");
    }
    
    /// <summary>
    /// Validate and fix parameter types for compatibility
    /// </summary>
    void ValidateAndFixParameterTypes(AnimatorController controller)
    {
        // Ensure FacingDirection is Float (for BlendTree compatibility)
        for (int i = 0; i < controller.parameters.Length; i++)
        {
            var param = controller.parameters[i];
            if (param.name == "FacingDirection" && param.type != AnimatorControllerParameterType.Float)
            {
                Debug.LogWarning($"Fixing FacingDirection parameter type: {param.type} → Float");
                controller.RemoveParameter(i);
                controller.AddParameter("FacingDirection", AnimatorControllerParameterType.Float);
                break;
            }
        }
    }
    
    /// <summary>
    /// Final validation of the animator controller
    /// </summary>
    void ValidateAnimatorController(AnimatorController controller)
    {
        Debug.Log("🔍 Validating Animator Controller...");
        
        // Check required parameters for 3-Animation System
        string[] requiredParams = { "Speed", "IsMoving", "Attack", "AttackUp", "AttackDown", "AttackLeft", "Hurt", "Die", "IsDead" };
        
        foreach (string paramName in requiredParams)
        {
            bool found = false;
            foreach (var param in controller.parameters)
            {
                if (param.name == paramName)
                {
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                Debug.LogError($"❌ Missing required parameter: {paramName}");
            }
            else
            {
                Debug.Log($"✅ Parameter found: {paramName}");
            }
        }
        
        // Check if using 4-directional attacks
        if (useFacingDirectionParameter)
        {
            bool facingDirectionFound = false;
            foreach (var param in controller.parameters)
            {
                if (param.name == "FacingDirection" && param.type == AnimatorControllerParameterType.Float)
                {
                    facingDirectionFound = true;
                    break;
                }
            }
            
            if (!facingDirectionFound)
            {
                Debug.LogError("❌ FacingDirection parameter missing or wrong type for BlendTree");
            }
            else
            {
                Debug.Log("✅ FacingDirection parameter correctly configured for BlendTree");
            }
        }
        
        // Check states
        var rootStateMachine = controller.layers[0].stateMachine;
        if (rootStateMachine.states.Length == 0)
        {
            Debug.LogError("❌ No states found in animator controller");
        }
        else
        {
            Debug.Log($"✅ Found {rootStateMachine.states.Length} states in controller");
        }
        
        Debug.Log("🔍 Animator Controller validation complete!");
    }
    
    /// <summary>
    /// Auto-assign the created controller to suitable prefabs
    /// </summary>
    void AutoAssignControllerToPrefabs(AnimatorController controller, string prefabType)
    {
        string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
        int assignedCount = 0;
        
        foreach (string guid in prefabGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab == null) continue;
            
            // Check if this prefab is suitable for the controller type
            bool isMatch = false;
            
            if (prefabType == "Player")
            {
                // Check for PlayerController component or name contains "Player"
                isMatch = prefab.GetComponent<PlayerController>() != null || 
                         prefab.name.ToLower().Contains("player") ||
                         prefab.name.ToLower().Contains("warrior");
            }
            else if (prefabType == "Boss")
            {
                // Check for boss-related components or names
                isMatch = prefab.name.ToLower().Contains("boss") ||
                         prefab.name.ToLower().Contains("agis") ||
                         prefab.GetComponent("agis_wizzar") != null;
            }
            else
            {
                // Enemy check
                isMatch = prefab.GetComponent<EnemyAnimatorController>() != null ||
                         prefab.name.ToLower().Contains("enemy") ||
                         (prefab.name.ToLower().Contains("goblin") || 
                          prefab.name.ToLower().Contains("sheep") ||
                          prefab.name.ToLower().Contains("lancer") ||
                          prefab.name.ToLower().Contains("archer"));
            }
            
            if (isMatch)
            {
                Animator animator = prefab.GetComponentInChildren<Animator>();
                if (animator != null && animator.runtimeAnimatorController == null)
                {
                    animator.runtimeAnimatorController = controller;
                    PrefabUtility.SavePrefabAsset(prefab);
                    assignedCount++;
                    
                    Debug.Log($"✅ Assigned {controller.name} to {prefab.name}");
                }
                else if (animator != null)
                {
                    Debug.Log($"ℹ️ {prefab.name} already has animator controller: {animator.runtimeAnimatorController?.name}");
                }
            }
        }
        
        Debug.Log($"📊 Auto-assigned controller to {assignedCount} prefabs");
    }
}
#endif