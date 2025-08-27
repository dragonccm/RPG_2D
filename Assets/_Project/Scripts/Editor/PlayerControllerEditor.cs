using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlayerController))]
public class PlayerControllerEditor : Editor
{
    private bool showMovementSettings = true;
    private bool showSkillInfo = true;
    private bool showLevelingInfo = true;
    private bool showDebugActions = true;

    public override void OnInspectorGUI()
    {
        PlayerController playerController = (PlayerController)target;
        
        // Draw default inspector first
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        
        // === MOVEMENT SETTINGS SECTION ===
        EditorGUILayout.BeginVertical("box");
        showMovementSettings = EditorGUILayout.Foldout(showMovementSettings, "?? Movement Settings", true, EditorStyles.foldoutHeader);
        
        if (showMovementSettings)
        {
            EditorGUILayout.Space(5);
            
            if (Application.isPlaying)
            {
                // Current movement info
                EditorGUILayout.LabelField("Movement Status:", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Current Movement:", GUILayout.Width(120));
                EditorGUILayout.LabelField($"({playerController.Movement.x:F2}, {playerController.Movement.y:F2})");
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Is Busy:", GUILayout.Width(120));
                EditorGUILayout.LabelField(playerController.IsBusy() ? "?? Busy" : "? Free", 
                    new GUIStyle(GUI.skin.label) { 
                        normal = { textColor = playerController.IsBusy() ? Color.red : Color.green } 
                    });
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Can Move:", GUILayout.Width(120));
                Character character = playerController.GetComponent<Character>();
                bool canMove = character != null ? character.CanMove() : true;
                EditorGUILayout.LabelField(canMove ? "? Yes" : "? No", 
                    new GUIStyle(GUI.skin.label) { 
                        normal = { textColor = canMove ? Color.green : Color.red } 
                    });
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
                
                // Real-time movement controls
                EditorGUILayout.LabelField("Runtime Movement Controls:", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                
                // Speed adjustment
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Move Speed:", GUILayout.Width(120));
                float newSpeed = EditorGUILayout.FloatField(playerController.moveSpeed);
                if (newSpeed != playerController.moveSpeed && newSpeed > 0)
                {
                    playerController.moveSpeed = newSpeed;
                    EditorUtility.SetDirty(playerController);
                }
                EditorGUILayout.EndHorizontal();
                
                // Movement settings info
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Movement Type:", GUILayout.Width(120));
                EditorGUILayout.LabelField("Responsive (No Smoothing)", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Flip Smooth Time:", GUILayout.Width(120));
                float newFlipSmoothTime = EditorGUILayout.Slider(playerController.flipSmoothTime, 0.01f, 0.5f);
                if (newFlipSmoothTime != playerController.flipSmoothTime)
                {
                    playerController.flipSmoothTime = newFlipSmoothTime;
                    EditorUtility.SetDirty(playerController);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox("Movement status will appear here during runtime", MessageType.Info);
            }
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // === SKILL INFO SECTION ===
        EditorGUILayout.BeginVertical("box");
        showSkillInfo = EditorGUILayout.Foldout(showSkillInfo, "? Skill System Info", true, EditorStyles.foldoutHeader);
        
        if (showSkillInfo)
        {
            EditorGUILayout.Space(5);
            
            ModularSkillManager skillManager = playerController.GetComponent<ModularSkillManager>();
            
            if (skillManager != null)
            {
                if (Application.isPlaying)
                {
                    // Current level and slots
                    EditorGUILayout.LabelField("Skill System Status:", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Current Level:", GUILayout.Width(120));
                    EditorGUILayout.LabelField($"Level {skillManager.GetPlayerLevel()}", EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                    
                    var unlockedSlots = skillManager.GetUnlockedSlots();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Unlocked Slots:", GUILayout.Width(120));
                    EditorGUILayout.LabelField($"{unlockedSlots.Count}/8 slots");
                    EditorGUILayout.EndHorizontal();
                    
                    var availableSkills = skillManager.GetAvailableSkills();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Available Skills:", GUILayout.Width(120));
                    EditorGUILayout.LabelField($"{availableSkills.Count} skills");
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(5);
                    
                    // Show equipped skills
                    if (unlockedSlots.Count > 0)
                    {
                        EditorGUILayout.LabelField("Equipped Skills:", EditorStyles.boldLabel);
                        EditorGUI.indentLevel++;
                        
                        foreach (var slot in unlockedSlots)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"Slot {slot.slotIndex}:", GUILayout.Width(60));
                            
                            if (slot.HasSkill())
                            {
                                string skillName = slot.equippedSkill.skillName;
                                string hotkeyName = slot.GetHotkeyDisplayName();
                                float cooldown = skillManager.GetSkillCooldown(slot.slotIndex);
                                
                                string statusText = cooldown > 0 ? 
                                    $"{skillName} ({hotkeyName}) - Cooldown: {cooldown:F1}s" :
                                    $"{skillName} ({hotkeyName}) - Ready";
                                    
                                EditorGUILayout.LabelField(statusText);
                            }
                            else
                            {
                                EditorGUILayout.LabelField("(Empty)", new GUIStyle(GUI.skin.label) { 
                                    normal = { textColor = Color.gray } 
                                });
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        
                        EditorGUI.indentLevel--;
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Skill information will appear here during runtime", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("ModularSkillManager component not found!", MessageType.Warning);
            }
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // === LEVELING INFO SECTION ===
        EditorGUILayout.BeginVertical("box");
        showLevelingInfo = EditorGUILayout.Foldout(showLevelingInfo, "?? Leveling Controls", true, EditorStyles.foldoutHeader);
        
        if (showLevelingInfo)
        {
            EditorGUILayout.Space(5);
            
            ModularSkillManager skillManager = playerController.GetComponent<ModularSkillManager>();
            
            if (skillManager != null && Application.isPlaying)
            {
                // Level controls
                EditorGUILayout.LabelField("Level Controls:", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Set Level:", GUILayout.Width(80));
                
                int currentLevel = skillManager.GetPlayerLevel();
                int newLevel = EditorGUILayout.IntField(currentLevel, GUILayout.Width(60));
                
                if (GUILayout.Button("Apply", GUILayout.Width(60)))
                {
                    if (newLevel > 0 && newLevel != currentLevel)
                    {
                        skillManager.SetPlayerLevel(newLevel);
                        Debug.Log($"?? Player level set to {newLevel}");
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                // Quick level buttons
                EditorGUILayout.LabelField("Quick Actions:", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+1 Level"))
                {
                    skillManager.SetPlayerLevel(currentLevel + 1);
                    Debug.Log($"?? Level up! Now level {currentLevel + 1}");
                }
                if (GUILayout.Button("+5 Levels"))
                {
                    skillManager.SetPlayerLevel(currentLevel + 5);
                    Debug.Log($"?? Level up! Now level {currentLevel + 5}");
                }
                if (GUILayout.Button("+10 Levels"))
                {
                    skillManager.SetPlayerLevel(currentLevel + 10);
                    Debug.Log($"?? Level up! Now level {currentLevel + 10}");
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Level 10"))
                {
                    skillManager.SetPlayerLevel(10);
                    Debug.Log("?? Set to level 10");
                }
                if (GUILayout.Button("Level 25"))
                {
                    skillManager.SetPlayerLevel(25);
                    Debug.Log("?? Set to level 25");
                }
                if (GUILayout.Button("Level 50"))
                {
                    skillManager.SetPlayerLevel(50);
                    Debug.Log("?? Set to level 50");
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                // Level progression info
                EditorGUILayout.LabelField("Level Progression:", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                
                int nextSlotLevel = ((skillManager.GetUnlockedSlots().Count) * 5) + 1;
                EditorGUILayout.LabelField($"Next slot unlocks at: Level {nextSlotLevel}");
                EditorGUILayout.LabelField($"Levels per slot: 5");
                EditorGUILayout.LabelField($"Max slots: 8");
                
                EditorGUI.indentLevel--;
            }
            else if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Leveling controls will appear here during runtime", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("ModularSkillManager component not found!", MessageType.Warning);
            }
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // === DEBUG ACTIONS SECTION ===
        if (Application.isPlaying)
        {
            EditorGUILayout.BeginVertical("box");
            showDebugActions = EditorGUILayout.Foldout(showDebugActions, "?? Debug Actions", true, EditorStyles.foldoutHeader);
            
            if (showDebugActions)
            {
                EditorGUILayout.Space(5);
                
                // Movement controls
                EditorGUILayout.LabelField("Movement Controls:", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Force Stop"))
                {
                    playerController.ForceStopMovement();
                    Debug.Log("?? Player movement stopped");
                }
                if (GUILayout.Button("Set Busy"))
                {
                    playerController.SetBusy(true);
                    Debug.Log("?? Player set to busy");
                }
                if (GUILayout.Button("Set Free"))
                {
                    playerController.SetBusy(false);
                    Debug.Log("? Player set to free");
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                // Animation tests
                EditorGUILayout.LabelField("Animation Tests:", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Test Attack"))
                {
                    // Call private method using reflection for testing
                    var method = typeof(PlayerController).GetMethod("TriggerAttackAnimation", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (method != null)
                    {
                        method.Invoke(playerController, null);
                        Debug.Log("??? Attack animation triggered from editor");
                    }
                }
                if (GUILayout.Button("Test Skill Anim"))
                {
                    playerController.TriggerSkillAnimation("EditorTest", "Attack");
                    Debug.Log("? Skill animation triggered from editor");
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                // Component validation
                EditorGUILayout.LabelField("Component Validation:", EditorStyles.boldLabel);
                
                if (GUILayout.Button("Validate Components"))
                {
                    bool isValid = playerController.ValidateComponents();
                    if (isValid)
                    {
                        EditorUtility.DisplayDialog("Validation", "? All required components are present!", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Validation", "? Some required components are missing! Check console for details.", "OK");
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        // Runtime tips
        if (!Application.isPlaying)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("?? Tip: Enter Play Mode to see real-time stats and use debug controls!", MessageType.Info);
        }
        else
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("?? Runtime Hotkeys: V = +10 Levels, Tab = Toggle Skill Panel, J = Test Attack", MessageType.Info);
        }
        
        // Force repaint during play mode to keep UI updated
        if (Application.isPlaying)
        {
            EditorUtility.SetDirty(target);
            Repaint();
        }
    }
}