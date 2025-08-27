using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Character))]
public class CharacterEditor : Editor
{
    private bool showStats = true;
    private bool showCombatSettings = true;
    private bool showDebugSettings = true;
    
    // God Mode settings
    private bool godModeEnabled = false;
    private float originalHealth = 0f;
    private float originalMana = 0f;
    private bool hadGodMode = false;

    public override void OnInspectorGUI()
    {
        Character character = (Character)target;
        
        // Draw default inspector first
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        
        // === PLAYER CONFIGURATION SECTION ===
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("?? Player Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        // Base stats
        SerializedProperty maxHealthProp = serializedObject.FindProperty("maxHealth");
        SerializedProperty maxManaProp = serializedObject.FindProperty("maxMana");
        SerializedProperty healthRegenProp = serializedObject.FindProperty("healthRegenRate");
        SerializedProperty manaRegenProp = serializedObject.FindProperty("manaRegenRate");
        SerializedProperty defenseProp = serializedObject.FindProperty("defense");
        SerializedProperty attackPowerProp = serializedObject.FindProperty("attackPower");
        SerializedProperty criticalChanceProp = serializedObject.FindProperty("criticalChance");
        SerializedProperty criticalMultiplierProp = serializedObject.FindProperty("criticalMultiplier");
        SerializedProperty moveSpeedProp = serializedObject.FindProperty("moveSpeed");
        SerializedProperty attackSpeedProp = serializedObject.FindProperty("attackSpeed");
        
        EditorGUILayout.PropertyField(maxHealthProp);
        EditorGUILayout.PropertyField(maxManaProp);
        EditorGUILayout.PropertyField(healthRegenProp);
        EditorGUILayout.PropertyField(manaRegenProp);
        EditorGUILayout.PropertyField(defenseProp);
        EditorGUILayout.PropertyField(attackPowerProp);
        EditorGUILayout.PropertyField(criticalChanceProp);
        EditorGUILayout.PropertyField(criticalMultiplierProp);
        EditorGUILayout.PropertyField(moveSpeedProp);
        EditorGUILayout.PropertyField(attackSpeedProp);
        
        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
        
        // Apply changes to serialized properties
        serializedObject.ApplyModifiedProperties();
        
        EditorGUILayout.Space(5);
        
        // === PLAYER STATS SECTION ===
        EditorGUILayout.BeginVertical("box");
        showStats = EditorGUILayout.Foldout(showStats, "?? Player Stats", true, EditorStyles.foldoutHeader);
        
        if (showStats)
        {
            EditorGUILayout.Space(5);
            
            // Health Stats
            EditorGUILayout.LabelField("?? Health", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            if (Application.isPlaying && character.health != null)
            {
                // Runtime - editable values
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Current Health:", GUILayout.Width(100));
                float newCurrentHealth = EditorGUILayout.FloatField(character.health.currentValue);
                if (newCurrentHealth != character.health.currentValue)
                {
                    character.health.currentValue = Mathf.Clamp(newCurrentHealth, 0, character.health.maxValue);
                }
                EditorGUILayout.LabelField($"/ {character.health.maxValue:F1}", GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
                
                // Health percentage bar
                Rect healthBarRect = EditorGUILayout.GetControlRect(false, 20);
                float healthPercent = character.health.maxValue > 0 ? character.health.currentValue / character.health.maxValue : 0;
                EditorGUI.ProgressBar(healthBarRect, healthPercent, $"Health: {character.health.currentValue:F1}/{character.health.maxValue:F1} ({healthPercent:P0})");
                
                // Max Health
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Max Health:", GUILayout.Width(100));
                float newMaxHealth = EditorGUILayout.FloatField(character.health.maxValue);
                if (newMaxHealth != character.health.maxValue && newMaxHealth > 0)
                {
                    character.health.maxValue = newMaxHealth;
                    // Adjust current health if it exceeds new max
                    if (character.health.currentValue > character.health.maxValue)
                        character.health.currentValue = character.health.maxValue;
                }
                EditorGUILayout.EndHorizontal();
                
                // Health Regen
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Health Regen/sec:", GUILayout.Width(100));
                character.health.regenRate = EditorGUILayout.FloatField(character.health.regenRate);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Health stats will appear here during runtime", MessageType.Info);
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
            
            // Mana Stats
            EditorGUILayout.LabelField("?? Mana", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            if (Application.isPlaying && character.mana != null)
            {
                // Runtime - editable values
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Current Mana:", GUILayout.Width(100));
                float newCurrentMana = EditorGUILayout.FloatField(character.mana.currentValue);
                if (newCurrentMana != character.mana.currentValue)
                {
                    character.mana.currentValue = Mathf.Clamp(newCurrentMana, 0, character.mana.maxValue);
                }
                EditorGUILayout.LabelField($"/ {character.mana.maxValue:F1}", GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
                
                // Mana percentage bar
                Rect manaBarRect = EditorGUILayout.GetControlRect(false, 20);
                float manaPercent = character.mana.maxValue > 0 ? character.mana.currentValue / character.mana.maxValue : 0;
                Color manaColor = new Color(0.2f, 0.4f, 1f, 1f); // Blue color for mana
                EditorGUI.DrawRect(manaBarRect, Color.black);
                Rect manaFillRect = new Rect(manaBarRect.x, manaBarRect.y, manaBarRect.width * manaPercent, manaBarRect.height);
                EditorGUI.DrawRect(manaFillRect, manaColor);
                GUI.Label(manaBarRect, $"Mana: {character.mana.currentValue:F1}/{character.mana.maxValue:F1} ({manaPercent:P0})", 
                         new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
                
                // Max Mana
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Max Mana:", GUILayout.Width(100));
                float newMaxMana = EditorGUILayout.FloatField(character.mana.maxValue);
                if (newMaxMana != character.mana.maxValue && newMaxMana > 0)
                {
                    character.mana.maxValue = newMaxMana;
                    // Adjust current mana if it exceeds new max
                    if (character.mana.currentValue > character.mana.maxValue)
                        character.mana.currentValue = character.mana.maxValue;
                }
                EditorGUILayout.EndHorizontal();
                
                // Mana Regen
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Mana Regen/sec:", GUILayout.Width(100));
                character.mana.regenRate = EditorGUILayout.FloatField(character.mana.regenRate);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Mana stats will appear here during runtime", MessageType.Info);
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
            
            // Status Effects
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("?? Status Effects", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Stunned: {(character.isStunned ? "?" : "?")}", GUILayout.Width(100));
                EditorGUILayout.LabelField($"Knocked Back: {(character.isBeingKnockedBack ? "?" : "?")}", GUILayout.Width(120));
                EditorGUILayout.LabelField($"Poisoned: {(character.isPoisoned ? "?" : "?")}");
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Can Move: {(character.CanMove() ? "?" : "?")}", GUILayout.Width(100));
                EditorGUILayout.LabelField($"Is Dead: {(character.IsDead ? "??" : "??")}");
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // === GOD MODE SECTION ===
        if (Application.isPlaying)
        {
            EditorGUILayout.BeginVertical("box");
            
            // God Mode Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("??? God Mode", EditorStyles.boldLabel);
            
            // God Mode Toggle
            bool newGodMode = EditorGUILayout.Toggle(godModeEnabled);
            if (newGodMode != godModeEnabled)
            {
                ToggleGodMode(character, newGodMode);
            }
            EditorGUILayout.EndHorizontal();
            
            if (godModeEnabled)
            {
                EditorGUILayout.HelpBox("God Mode Active: Player is invincible with 99999 Health & 99999 Mana!", MessageType.Warning);
                
                // Show god mode status
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Status:", GUILayout.Width(60));
                EditorGUILayout.LabelField("?? INVINCIBLE", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.yellow } });
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Health:", GUILayout.Width(60));
                EditorGUILayout.LabelField("99999", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.green } });
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Mana:", GUILayout.Width(60));
                EditorGUILayout.LabelField("99999", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.cyan } });
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("God Mode Disabled: Player takes normal damage with standard stats", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space(5);
        
        // === COMBAT SETTINGS SECTION ===
        EditorGUILayout.BeginVertical("box");
        showCombatSettings = EditorGUILayout.Foldout(showCombatSettings, "?? Combat Settings", true, EditorStyles.foldoutHeader);
        
        if (showCombatSettings && Application.isPlaying)
        {
            EditorGUILayout.Space(5);
            
            // Knockback Resistance
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Knockback Resistance:", GUILayout.Width(140));
            float newKnockbackResistance = EditorGUILayout.FloatField(character.KnockbackResistance);
            if (newKnockbackResistance != character.KnockbackResistance)
            {
                character.KnockbackResistance = newKnockbackResistance;
                EditorUtility.SetDirty(character);
            }
            EditorGUILayout.EndHorizontal();
            
            // Combat Effects Toggles
            EditorGUILayout.LabelField("Combat Effects:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Show Damage Numbers:", GUILayout.Width(140));
            bool newShowDamageNumbers = EditorGUILayout.Toggle(character.ShowDamageNumbers);
            if (newShowDamageNumbers != character.ShowDamageNumbers)
            {
                character.ShowDamageNumbers = newShowDamageNumbers;
                EditorUtility.SetDirty(character);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Hit Stop:", GUILayout.Width(140));
            bool newEnableHitStop = EditorGUILayout.Toggle(character.EnableHitStop);
            if (newEnableHitStop != character.EnableHitStop)
            {
                character.EnableHitStop = newEnableHitStop;
                EditorUtility.SetDirty(character);
            }
            EditorGUILayout.EndHorizontal();
            
            // Damage Flash Settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Damage Flash Settings:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Flash Duration:", GUILayout.Width(140));
            float newFlashDuration = EditorGUILayout.FloatField(character.DamageFlashDuration);
            if (newFlashDuration != character.DamageFlashDuration)
            {
                character.DamageFlashDuration = newFlashDuration;
                EditorUtility.SetDirty(character);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Flash Color:", GUILayout.Width(140));
            Color newFlashColor = EditorGUILayout.ColorField(character.DamageFlashColor);
            if (newFlashColor != character.DamageFlashColor)
            {
                character.DamageFlashColor = newFlashColor;
                EditorUtility.SetDirty(character);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // === DEBUG ACTIONS SECTION ===
        if (Application.isPlaying)
        {
            EditorGUILayout.BeginVertical("box");
            showDebugSettings = EditorGUILayout.Foldout(showDebugSettings, "?? Debug Actions", true, EditorStyles.foldoutHeader);
            
            if (showDebugSettings)
            {
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                
                // Heal buttons
                if (GUILayout.Button("Heal 25"))
                {
                    character.Heal(25f);
                }
                if (GUILayout.Button("Heal 50"))
                {
                    character.Heal(50f);
                }
                if (GUILayout.Button("Full Heal"))
                {
                    character.Heal(character.MaxHealth);
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                
                // Damage buttons
                if (GUILayout.Button("Damage 10"))
                {
                    if (!godModeEnabled) character.TakeDamage(10f);
                }
                if (GUILayout.Button("Damage 25"))
                {
                    if (!godModeEnabled) character.TakeDamage(25f);
                }
                if (GUILayout.Button("Damage 50"))
                {
                    if (!godModeEnabled) character.TakeDamage(50f);
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                
                // Mana buttons
                if (GUILayout.Button("Restore Mana 25"))
                {
                    character.RestoreMana(25f);
                }
                if (GUILayout.Button("Full Mana"))
                {
                    character.RestoreMana(character.mana.maxValue);
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                
                // Status effect buttons
                if (GUILayout.Button("Stun 2s"))
                {
                    character.ApplyStun(2f);
                }
                if (GUILayout.Button("Poison 5s"))
                {
                    character.ApplyPoison(5f, 5f);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
        
        // Force repaint during play mode to keep UI updated
        if (Application.isPlaying)
        {
            EditorUtility.SetDirty(target);
            Repaint();
        }
    }
    
    private void ToggleGodMode(Character character, bool enabled)
    {
        godModeEnabled = enabled;
        
        if (enabled)
        {
            // Store original values
            originalHealth = character.health.currentValue;
            originalMana = character.mana.currentValue;
            
            // Set god mode values
            character.health.maxValue = 99999f;
            character.health.currentValue = 99999f;
            character.mana.maxValue = 99999f;
            character.mana.currentValue = 99999f;
            
            hadGodMode = true;
            
            Debug.Log("??? God Mode ENABLED - Player has 99999 Health & 99999 Mana!");
        }
        else
        {
            // Restore original values
            character.health.maxValue = character.MaxHealth;
            character.health.currentValue = hadGodMode ? Mathf.Min(originalHealth, character.MaxHealth) : character.health.currentValue;
            character.mana.maxValue = character.MaxMana;
            character.mana.currentValue = hadGodMode ? Mathf.Min(originalMana, character.MaxMana) : character.mana.currentValue;
            
            hadGodMode = false;
            
            Debug.Log("?? God Mode DISABLED - Player stats restored to normal");
        }
        
        EditorUtility.SetDirty(character);
    }
    
    private void Update()
    {
        // Maintain god mode during play
        if (Application.isPlaying && godModeEnabled)
        {
            Character character = (Character)target;
            if (character != null && character.health != null)
            {
                // Keep health at maximum
                if (character.health.currentValue < character.health.maxValue)
                {
                    character.health.currentValue = character.health.maxValue;
                }
            }
        }
    }
    
    private void OnEnable()
    {
        EditorApplication.update += Update;
    }
    
    private void OnDisable()
    {
        EditorApplication.update -= Update;
        
        // Auto-disable god mode when editor is disabled
        if (godModeEnabled && target != null)
        {
            ToggleGodMode((Character)target, false);
        }
    }
}