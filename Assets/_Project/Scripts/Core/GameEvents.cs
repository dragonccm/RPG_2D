using UnityEngine;

/// <summary>
/// Unified event system to replace multiple duplicate event systems
/// Consolidates Character.OnDamageTaken, DamageInterfaces.OnDamageTaken, and Enemy.OnDamageTaken
/// </summary>
public static class GameEvents
{
    // Damage Events
    public static System.Action<IDamageable, float, IDamageDealer> OnDamageDealt;
    public static System.Action<IDamageable, float> OnDamageTaken;
    public static System.Action<IDamageable> OnTargetKilled;
    public static System.Action<GameObject, float> OnDamageBlocked;

    // Healing Events
    public static System.Action<IDamageable, float, GameObject> OnHealed;

    // Death Events
    public static System.Action<IDamageable, IDamageDealer> OnObjectDied;

    // UI Events
    public static System.Action OnSkillPanelOpened;
    public static System.Action OnSkillPanelClosed;
    public static System.Action OnPauseMenuOpened;
    public static System.Action OnPauseMenuClosed;

    // Skill Events
    public static System.Action<int> OnSkillSlotUnlocked;
    public static System.Action<int, SkillModule> OnSkillEquipped;
    public static System.Action<int> OnSkillUnequipped;
    public static System.Action<SkillModule, KeyCode> OnHotkeyChanged;

    // Animation Events
    public static System.Action OnAttackAnimationComplete;
    public static System.Action OnSkillAnimationComplete;

    // Inventory Events
    public static System.Action OnInventoryChanged;
    public static System.Action OnEquipmentChanged;
    public static System.Action<int> OnGoldChanged;

    // Player Events
    public static System.Action<float, float> OnHealthChanged;
    public static System.Action<float, float> OnManaChanged;
    public static System.Action<float, float, int> OnExperienceChanged;
    public static System.Action OnLevelUp;
    public static System.Action OnPlayerDeath;
    public static System.Action<float> OnHealingReceived;

    // Quest Events
    public static System.Action<Quest> OnQuestActivated;
    public static System.Action<Quest> OnQuestCompleted;
    public static System.Action<Quest> OnQuestFailed;
    public static System.Action<Quest> OnQuestProgress;
    public static System.Action<int> OnExperienceGained;
    public static System.Action<int> OnGoldGained;
    public static System.Action<string, int> OnItemReceived;

    // Enemy Events
    public static System.Action<GameObject> OnEnemyDeath;

    // Scene Events
    public static System.Action<string> OnSceneLoaded;
    public static System.Action<float> OnLoadingProgress;

    // Shop Events
    public static System.Action OnShopOpened;

    // Game State Events
    public static System.Action OnGamePaused;
    public static System.Action OnGameResumed;

    /// <summary>
    /// Raise damage taken event
    /// </summary>
    public static void RaiseDamageTaken(IDamageable target, float damage)
    {
        OnDamageTaken?.Invoke(target, damage);
    }

    /// <summary>
    /// Raise damage dealt event
    /// </summary>
    public static void RaiseDamageDealt(IDamageable target, float damage, IDamageDealer dealer)
    {
        OnDamageDealt?.Invoke(target, damage, dealer);
    }

    /// <summary>
    /// Raise target killed event
    /// </summary>
    public static void RaiseTargetKilled(IDamageable target)
    {
        OnTargetKilled?.Invoke(target);
    }

    /// <summary>
    /// Raise healing event
    /// </summary>
    public static void RaiseHealed(IDamageable target, float amount, GameObject source)
    {
        OnHealed?.Invoke(target, amount, source);
    }

    /// <summary>
    /// Raise death event
    /// </summary>
    public static void RaiseObjectDied(IDamageable target, IDamageDealer dealer)
    {
        OnObjectDied?.Invoke(target, dealer);
    }

    /// <summary>
    /// Clear all event subscriptions (useful for scene changes)
    /// </summary>
    public static void ClearAllEvents()
    {
        OnDamageDealt = null;
        OnDamageTaken = null;
        OnTargetKilled = null;
        OnDamageBlocked = null;
        OnHealed = null;
        OnObjectDied = null;
        OnSkillPanelOpened = null;
        OnSkillPanelClosed = null;
        OnPauseMenuOpened = null;
        OnPauseMenuClosed = null;
        OnSkillSlotUnlocked = null;
        OnSkillEquipped = null;
        OnSkillUnequipped = null;
        OnHotkeyChanged = null;
        OnAttackAnimationComplete = null;
        OnSkillAnimationComplete = null;
        OnInventoryChanged = null;
        OnEquipmentChanged = null;
        OnGoldChanged = null;
        OnHealthChanged = null;
        OnManaChanged = null;
        OnExperienceChanged = null;
        OnLevelUp = null;
        OnPlayerDeath = null;
        OnHealingReceived = null;
        OnQuestActivated = null;
        OnQuestCompleted = null;
        OnQuestFailed = null;
        OnQuestProgress = null;
        OnExperienceGained = null;
        OnGoldGained = null;
        OnItemReceived = null;
        OnEnemyDeath = null;
        OnSceneLoaded = null;
        OnLoadingProgress = null;
        OnShopOpened = null;
        OnGamePaused = null;
        OnGameResumed = null;
    }
}
