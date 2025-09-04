using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Unified save/load system to replace multiple save managers
/// Consolidates PlayerSaveManager, GameSaveManager, and data persistence
/// </summary>
public class UnifiedSaveLoad : MonoBehaviour
{
    [Header("Save Settings")]
    [SerializeField] private string saveFileName = "game_save.dat";
    [SerializeField] private bool enableAutoSave = true;
    [SerializeField] private float autoSaveInterval = 60f; // seconds
    [SerializeField] private bool enableDebugLogging = false;

    private string savePath;
    private float lastAutoSaveTime;
    private bool isLoading = false;

    private void Awake()
    {
        savePath = Path.Combine(Application.persistentDataPath, saveFileName);
        ServiceLocator.RegisterService(this);
    }

    private void Start()
    {
        if (enableAutoSave)
        {
            InvokeRepeating(nameof(AutoSave), autoSaveInterval, autoSaveInterval);
        }
    }

    private void OnApplicationQuit()
    {
        if (enableAutoSave)
        {
            SaveGame();
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause && enableAutoSave)
        {
            SaveGame();
        }
    }

    /// <summary>
    /// Save game data to file
    /// </summary>
    public void SaveGame()
    {
        try
        {
            GameData gameData = new GameData();
            CollectGameData(gameData);

            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(savePath, FileMode.Create))
            {
                formatter.Serialize(stream, gameData);
            }

            if (enableDebugLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("💾 Game saved to: {0}", savePath));
            }
        }
        catch (System.Exception e)
        {
            PerformanceUtils.LogError(PerformanceUtils.FormatString("❌ Save failed: {0}", e.Message));
        }
    }

    /// <summary>
    /// Load game data from file
    /// </summary>
    public bool LoadGame()
    {
        if (!File.Exists(savePath))
        {
            if (enableDebugLogging)
            {
                PerformanceUtils.Log("⚠️ No save file found");
            }
            return false;
        }

        try
        {
            isLoading = true;

            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(savePath, FileMode.Open))
            {
                GameData gameData = formatter.Deserialize(stream) as GameData;
                if (gameData != null)
                {
                    ApplyGameData(gameData);
                }
            }

            if (enableDebugLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("📁 Game loaded from: {0}", savePath));
            }

            return true;
        }
        catch (System.Exception e)
        {
            PerformanceUtils.LogError(PerformanceUtils.FormatString("❌ Load failed: {0}", e.Message));
            return false;
        }
        finally
        {
            isLoading = false;
        }
    }

    /// <summary>
    /// Delete save file
    /// </summary>
    public void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);

            if (enableDebugLogging)
            {
                PerformanceUtils.Log("🗑️ Save file deleted");
            }
        }
    }

    /// <summary>
    /// Check if save file exists
    /// </summary>
    public bool HasSaveFile()
    {
        return File.Exists(savePath);
    }

    /// <summary>
    /// Get save file info
    /// </summary>
    public FileInfo GetSaveFileInfo()
    {
        if (!File.Exists(savePath)) return null;

        return new FileInfo(savePath);
    }

    /// <summary>
    /// Collect all game data for saving
    /// </summary>
    private void CollectGameData(GameData gameData)
    {
        // Collect player data
        var playerController = ServiceLocator.GetService<PlayerController>();
        if (playerController != null)
        {
            var character = playerController.GetComponent<Character>();
            gameData.playerData = new PlayerData
            {
                position = playerController.transform.position,
                health = character != null ? character.CurrentHealth : 100f,
                maxHealth = character != null ? character.MaxHealth : 100f,
                mana = character != null ? character.CurrentMana : 50f,
                maxMana = character != null ? character.MaxMana : 50f,
                level = character != null ? character.Level : 1,
                experience = character != null ? character.Experience : 0f
            };
        }

        // Collect scene data
        gameData.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        gameData.gameTime = Time.time;

        // Collect inventory data
        var inventory = ServiceLocator.GetService<UnifiedInventory>();
        if (inventory != null)
        {
            gameData.inventoryData = new InventoryData
            {
                items = inventory.GetItems(),
                gold = inventory.GetGold()
            };
        }

        // Collect quest data
        var questManager = ServiceLocator.GetService<UnifiedQuest>();
        if (questManager != null)
        {
            gameData.questData = new QuestData
            {
                activeQuests = questManager.GetActiveQuests().Select(q => q.Id).ToList(),
                completedQuests = questManager.GetCompletedQuests().Select(q => q.Id).ToList()
            };
        }

        // Collect settings
        gameData.settingsData = new SettingsData
        {
            masterVolume = AudioListener.volume,
            musicVolume = 1f, // Would get from audio manager
            sfxVolume = 1f    // Would get from audio manager
        };
    }

    /// <summary>
    /// Apply loaded game data
    /// </summary>
    private void ApplyGameData(GameData gameData)
    {
        // Apply player data
        var playerController = ServiceLocator.GetService<PlayerController>();
        if (playerController != null && gameData.playerData != null)
        {
            playerController.transform.position = gameData.playerData.position;
            var character = playerController.GetComponent<Character>();
            if (character != null)
            {
                character.CurrentHealth = gameData.playerData.health;
                character.CurrentMana = gameData.playerData.mana;
                character.Level = gameData.playerData.level;
                character.Experience = gameData.playerData.experience;
            }
        }

        // Apply scene if different
        if (!string.IsNullOrEmpty(gameData.sceneName) &&
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != gameData.sceneName)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameData.sceneName);
        }

        // Apply inventory data
        var inventory = ServiceLocator.GetService<UnifiedInventory>();
        if (inventory != null && gameData.inventoryData != null)
        {
            inventory.SetItemsFromSaveData(gameData.inventoryData.items);
            inventory.SetGold(gameData.inventoryData.gold);
        }

        // Apply quest data
        var questManager = ServiceLocator.GetService<UnifiedQuest>();
        if (questManager != null && gameData.questData != null)
        {
            questManager.SetActiveQuestsFromIds(gameData.questData.activeQuests);
            questManager.SetCompletedQuestsFromIds(gameData.questData.completedQuests);
        }

        // Apply settings
        if (gameData.settingsData != null)
        {
            AudioListener.volume = gameData.settingsData.masterVolume;
            // Apply other settings...
        }
    }

    /// <summary>
    /// Auto-save game periodically
    /// </summary>
    private void AutoSave()
    {
        if (!isLoading && Time.time - lastAutoSaveTime >= autoSaveInterval)
        {
            SaveGame();
            lastAutoSaveTime = Time.time;
        }
    }

    /// <summary>
    /// Save specific data type
    /// </summary>
    public void SaveData<T>(string key, T data)
    {
        try
        {
            string dataPath = Path.Combine(Application.persistentDataPath, key + ".dat");
            BinaryFormatter formatter = new BinaryFormatter();

            using (FileStream stream = new FileStream(dataPath, FileMode.Create))
            {
                formatter.Serialize(stream, data);
            }

            if (enableDebugLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("💾 Data saved: {0}", key));
            }
        }
        catch (System.Exception e)
        {
            PerformanceUtils.LogError(PerformanceUtils.FormatString("❌ Data save failed for {0}: {1}", key, e.Message));
        }
    }

    /// <summary>
    /// Load specific data type
    /// </summary>
    public T LoadData<T>(string key)
    {
        string dataPath = Path.Combine(Application.persistentDataPath, key + ".dat");

        if (!File.Exists(dataPath))
        {
            if (enableDebugLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("⚠️ No data file found for: {0}", key));
            }
            return default(T);
        }

        try
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(dataPath, FileMode.Open))
            {
                T data = (T)formatter.Deserialize(stream);

                if (enableDebugLogging)
                {
                    PerformanceUtils.Log(PerformanceUtils.FormatString("📁 Data loaded: {0}", key));
                }

                return data;
            }
        }
        catch (System.Exception e)
        {
            PerformanceUtils.LogError(PerformanceUtils.FormatString("❌ Data load failed for {0}: {1}", key, e.Message));
            return default(T);
        }
    }
}

/// <summary>
/// Main game data container for saving/loading
/// </summary>
[System.Serializable]
public class GameData
{
    public PlayerData playerData;
    public InventoryData inventoryData;
    public QuestData questData;
    public SettingsData settingsData;
    public string sceneName;
    public float gameTime;
}

/// <summary>
/// Player-specific save data
/// </summary>
[System.Serializable]
public class PlayerData
{
    public Vector2 position;
    public float health;
    public float maxHealth;
    public float mana;
    public float maxMana;
    public int level;
    public float experience;
}

/// <summary>
/// Inventory save data
/// </summary>
[System.Serializable]
public class InventoryData
{
    public List<InventoryItem> items;
    public int gold;
}

/// <summary>
/// Quest system save data
/// </summary>
[System.Serializable]
public class QuestData
{
    public List<string> activeQuests;
    public List<string> completedQuests;
}

/// <summary>
/// Game settings save data
/// </summary>
[System.Serializable]
public class SettingsData
{
    public float masterVolume;
    public float musicVolume;
    public float sfxVolume;
}
