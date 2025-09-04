using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Unified quest system to replace multiple quest managers
/// Consolidates QuestManager, QuestController, and quest tracking
/// </summary>
public class UnifiedQuest : MonoBehaviour
{
    [Header("Quest Settings")]
    [SerializeField] private bool enableDebugLogging = false;
    [SerializeField] private int maxActiveQuests = 5;

    private Dictionary<string, Quest> allQuests = new Dictionary<string, Quest>();
    private List<Quest> activeQuests = new List<Quest>();
    private List<Quest> completedQuests = new List<Quest>();

    private void Awake()
    {
        ServiceLocator.RegisterService(this);
        InitializeDefaultQuests();
    }

    private void InitializeDefaultQuests()
    {
        // Create some default quests
        CreateQuest("tutorial_combat", "Learn to Fight", "Defeat 5 enemies to learn basic combat", QuestType.Kill, "Enemy", 5);
        CreateQuest("gather_resources", "Collect Herbs", "Gather 10 healing herbs from the forest", QuestType.Collect, "Herb", 10);
        CreateQuest("explore_dungeon", "Explore Dungeon", "Enter the mysterious dungeon", QuestType.Explore, "Dungeon", 1);
        CreateQuest("rescue_villager", "Rescue Mission", "Save the captured villager", QuestType.Rescue, "Villager", 1);
    }

    /// <summary>
    /// Create a new quest
    /// </summary>
    public Quest CreateQuest(string id, string title, string description, QuestType type, string target, int requiredAmount)
    {
        if (allQuests.ContainsKey(id))
        {
            PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ Quest with ID '{0}' already exists", id));
            return null;
        }

        Quest quest = new Quest
        {
            Id = id,
            Title = title,
            Description = description,
            Type = type,
            Target = target,
            RequiredAmount = requiredAmount,
            CurrentAmount = 0,
            IsActive = false,
            IsCompleted = false,
            Rewards = new QuestRewards()
        };

        allQuests.Add(id, quest);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("📜 Created quest: {0}", title));
        }

        return quest;
    }

    /// <summary>
    /// Activate a quest
    /// </summary>
    public bool ActivateQuest(string questId)
    {
        if (!allQuests.TryGetValue(questId, out Quest quest))
        {
            PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ Quest not found: {0}", questId));
            return false;
        }

        if (quest.IsActive || quest.IsCompleted)
        {
            return false;
        }

        if (activeQuests.Count >= maxActiveQuests)
        {
            PerformanceUtils.LogWarning("⚠️ Maximum active quests reached");
            return false;
        }

        quest.IsActive = true;
        activeQuests.Add(quest);

        // Trigger quest activated event
        GameEvents.OnQuestActivated?.Invoke(quest);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🎯 Activated quest: {0}", quest.Title));
        }

        return true;
    }

    /// <summary>
    /// Deactivate a quest
    /// </summary>
    public bool DeactivateQuest(string questId)
    {
        Quest quest = activeQuests.Find(q => q.Id == questId);
        if (quest == null)
        {
            return false;
        }

        quest.IsActive = false;
        activeQuests.Remove(quest);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("⏸️ Deactivated quest: {0}", quest.Title));
        }

        return true;
    }

    /// <summary>
    /// Complete a quest
    /// </summary>
    public bool CompleteQuest(string questId)
    {
        Quest quest = activeQuests.Find(q => q.Id == questId);
        if (quest == null || quest.IsCompleted)
        {
            return false;
        }

        quest.IsCompleted = true;
        quest.IsActive = false;
        activeQuests.Remove(quest);
        completedQuests.Add(quest);

        // Grant rewards
        GrantQuestRewards(quest);

        // Trigger quest completed event
        GameEvents.OnQuestCompleted?.Invoke(quest);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("✅ Completed quest: {0}", quest.Title));
        }

        return true;
    }

    /// <summary>
    /// Update quest progress
    /// </summary>
    public void UpdateQuestProgress(string target, int amount = 1)
    {
        foreach (var quest in activeQuests)
        {
            if (quest.Target == target && !quest.IsCompleted)
            {
                quest.CurrentAmount = Mathf.Min(quest.CurrentAmount + amount, quest.RequiredAmount);

                // Check if quest is completed
                if (quest.CurrentAmount >= quest.RequiredAmount)
                {
                    CompleteQuest(quest.Id);
                }
                else
                {
                    // Trigger progress update event
                    GameEvents.OnQuestProgress?.Invoke(quest);
                }

                if (enableDebugLogging)
                {
                    PerformanceUtils.Log(PerformanceUtils.FormatString("📊 Quest progress: {0} ({1}/{2})",
                        quest.Title, quest.CurrentAmount, quest.RequiredAmount));
                }
            }
        }
    }

    /// <summary>
    /// Update quest progress for specific quest
    /// </summary>
    public void UpdateQuestProgress(Quest quest, int amount = 1)
    {
        if (quest != null && !quest.IsCompleted)
        {
            quest.CurrentAmount = Mathf.Min(quest.CurrentAmount + amount, quest.RequiredAmount);

            if (quest.CurrentAmount >= quest.RequiredAmount)
            {
                CompleteQuest(quest.Id);
            }
            else
            {
                GameEvents.OnQuestProgress?.Invoke(quest);
            }

            if (enableDebugLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("📊 Quest progress: {0} ({1}/{2})",
                    quest.Title, quest.CurrentAmount, quest.RequiredAmount));
            }
        }
    }

    /// <summary>
    /// Set active quests from save data (by quest IDs)
    /// </summary>
    public void SetActiveQuestsFromIds(List<string> questIds)
    {
        activeQuests.Clear();
        foreach (var questId in questIds)
        {
            var quest = GetQuest(questId);
            if (quest != null)
            {
                quest.IsActive = true;
                quest.IsCompleted = false;
                activeQuests.Add(quest);
                GameEvents.OnQuestActivated?.Invoke(quest);
            }
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("📋 Loaded {0} active quests from IDs", activeQuests.Count));
        }
    }

    /// <summary>
    /// Set completed quests from save data (by quest IDs)
    /// </summary>
    public void SetCompletedQuestsFromIds(List<string> questIds)
    {
        completedQuests.Clear();
        foreach (var questId in questIds)
        {
            var quest = GetQuest(questId);
            if (quest != null)
            {
                quest.IsActive = false;
                quest.IsCompleted = true;
                completedQuests.Add(quest);
                GameEvents.OnQuestCompleted?.Invoke(quest);
            }
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("📋 Loaded {0} completed quests from IDs", completedQuests.Count));
        }
    }

    /// <summary>
    /// Grant quest rewards
    /// </summary>
    private void GrantQuestRewards(Quest quest)
    {
        if (quest.Rewards == null) return;

        // Grant experience
        if (quest.Rewards.Experience > 0)
        {
            // ServiceLocator.GetService<PlayerController>()?.AddExperience(quest.Rewards.Experience);
            GameEvents.OnExperienceGained?.Invoke(quest.Rewards.Experience);
        }

        // Grant gold
        if (quest.Rewards.Gold > 0)
        {
            // ServiceLocator.GetService<Inventory>()?.AddGold(quest.Rewards.Gold);
            GameEvents.OnGoldGained?.Invoke(quest.Rewards.Gold);
        }

        // Grant items
        if (quest.Rewards.Items != null && quest.Rewards.Items.Count > 0)
        {
            foreach (var item in quest.Rewards.Items)
            {
                // ServiceLocator.GetService<Inventory>()?.AddItem(item.ItemId, item.Quantity);
                GameEvents.OnItemReceived?.Invoke(item.ItemId, item.Quantity);
            }
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🎁 Granted rewards for quest: {0}", quest.Title));
        }
    }

    /// <summary>
    /// Set quest rewards
    /// </summary>
    public void SetQuestRewards(string questId, QuestRewards rewards)
    {
        if (allQuests.TryGetValue(questId, out Quest quest))
        {
            quest.Rewards = rewards;
        }
    }

    /// <summary>
    /// Get quest by ID
    /// </summary>
    public Quest GetQuest(string questId)
    {
        allQuests.TryGetValue(questId, out Quest quest);
        return quest;
    }

    /// <summary>
    /// Get all active quests
    /// </summary>
    public List<Quest> GetActiveQuests()
    {
        return new List<Quest>(activeQuests);
    }

    /// <summary>
    /// Get all completed quests
    /// </summary>
    public List<Quest> GetCompletedQuests()
    {
        return new List<Quest>(completedQuests);
    }

    /// <summary>
    /// Get all available quests
    /// </summary>
    public List<Quest> GetAvailableQuests()
    {
        List<Quest> available = new List<Quest>();
        foreach (var quest in allQuests.Values)
        {
            if (!quest.IsActive && !quest.IsCompleted)
            {
                available.Add(quest);
            }
        }
        return available;
    }

    /// <summary>
    /// Check if quest is active
    /// </summary>
    public bool IsQuestActive(string questId)
    {
        Quest quest = activeQuests.Find(q => q.Id == questId);
        return quest != null;
    }

    /// <summary>
    /// Check if quest is completed
    /// </summary>
    public bool IsQuestCompleted(string questId)
    {
        Quest quest = completedQuests.Find(q => q.Id == questId);
        return quest != null;
    }

    /// <summary>
    /// Get quest progress percentage
    /// </summary>
    public float GetQuestProgress(string questId)
    {
        Quest quest = activeQuests.Find(q => q.Id == questId);
        if (quest == null) return 0f;

        return (float)quest.CurrentAmount / quest.RequiredAmount;
    }

    /// <summary>
    /// Reset quest progress
    /// </summary>
    public void ResetQuestProgress(string questId)
    {
        Quest quest = activeQuests.Find(q => q.Id == questId);
        if (quest != null)
        {
            quest.CurrentAmount = 0;

            if (enableDebugLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("🔄 Reset quest progress: {0}", quest.Title));
            }
        }
    }

    /// <summary>
    /// Fail a quest
    /// </summary>
    public bool FailQuest(string questId)
    {
        Quest quest = activeQuests.Find(q => q.Id == questId);
        if (quest == null) return false;

        quest.IsActive = false;
        activeQuests.Remove(quest);

        // Trigger quest failed event
        GameEvents.OnQuestFailed?.Invoke(quest);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("❌ Failed quest: {0}", quest.Title));
        }

        return true;
    }

    /// <summary>
    /// Get quests by type
    /// </summary>
    public List<Quest> GetQuestsByType(QuestType type)
    {
        List<Quest> quests = new List<Quest>();
        foreach (var quest in allQuests.Values)
        {
            if (quest.Type == type)
            {
                quests.Add(quest);
            }
        }
        return quests;
    }

    /// <summary>
    /// Clear all quest data
    /// </summary>
    public void ClearAllQuests()
    {
        allQuests.Clear();
        activeQuests.Clear();
        completedQuests.Clear();

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("🗑️ Cleared all quest data");
        }
    }

    /// <summary>
    /// Get quest statistics
    /// </summary>
    public QuestStatistics GetQuestStatistics()
    {
        return new QuestStatistics
        {
            TotalQuests = allQuests.Count,
            ActiveQuests = activeQuests.Count,
            CompletedQuests = completedQuests.Count,
            CompletionRate = allQuests.Count > 0 ? (float)completedQuests.Count / allQuests.Count : 0f
        };
    }
}

/// <summary>
/// Quest data structure
/// </summary>
[System.Serializable]
public class Quest
{
    public string Id;
    public string Title;
    public string Description;
    public QuestType Type;
    public string Target;
    public int RequiredAmount;
    public int CurrentAmount;
    public bool IsActive;
    public bool IsCompleted;
    public QuestRewards Rewards;
}

/// <summary>
/// Quest type enumeration
/// </summary>
public enum QuestType
{
    Kill,
    Collect,
    Explore,
    Rescue,
    Deliver,
    Talk,
    Custom
}

/// <summary>
/// Quest rewards structure
/// </summary>
[System.Serializable]
public class QuestRewards
{
    public int Experience;
    public int Gold;
    public List<QuestItem> Items;
}

/// <summary>
/// Quest item structure
/// </summary>
[System.Serializable]
public class QuestItem
{
    public string ItemId;
    public int Quantity;
}

/// <summary>
/// Quest statistics structure
/// </summary>
[System.Serializable]
public class QuestStatistics
{
    public int TotalQuests;
    public int ActiveQuests;
    public int CompletedQuests;
    public float CompletionRate;
}
