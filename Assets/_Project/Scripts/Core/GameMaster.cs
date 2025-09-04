using UnityEngine;

/// <summary>
/// Master system integrator that coordinates all unified systems
/// Provides centralized access and initialization for all game systems
/// </summary>
public class GameMaster : MonoBehaviour
{
    [Header("System References")]
    [SerializeField] private UnifiedSaveLoad saveLoadSystem;
    [SerializeField] private UnifiedUI uiSystem;
    [SerializeField] private UnifiedAudio audioSystem;
    [SerializeField] private UnifiedCamera cameraSystem;
    [SerializeField] private UnifiedParticles particleSystem;
    [SerializeField] private UnifiedQuest questSystem;
    [SerializeField] private UnifiedInventory inventorySystem;
    [SerializeField] private UnifiedDialogue dialogueSystem;
    [SerializeField] private UnifiedSceneManager sceneManager;
    [SerializeField] private InputManager inputManager;

    [Header("Core Components")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameBootstrapper gameBootstrapper;

    [Header("Settings")]
    [SerializeField] private bool autoInitialize = true;
    [SerializeField] private bool enableDebugLogging = true;

    private void Awake()
    {
        if (autoInitialize)
        {
            InitializeGameMaster();
        }
    }

    private void InitializeGameMaster()
    {
        if (enableDebugLogging)
        {
            PerformanceUtils.Log("🎮 Initializing Game Master...");
        }

        // Initialize core systems in order
        InitializeCoreSystems();
        InitializeGameSystems();
        InitializeContentSystems();

        // Register all systems with ServiceLocator
        RegisterSystems();

        // Initialize game state
        InitializeGameState();

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("✅ Game Master initialization complete!");
        }
    }

    private void InitializeCoreSystems()
    {
        // 1. Service Locator - Foundation of all systems (static class, no initialization needed)

        // 2. Game Events - Communication backbone (static class, no initialization needed)

        // 3. Game Bootstrapper - System initialization
        if (gameBootstrapper == null)
        {
            gameBootstrapper = FindObjectOfType<GameBootstrapper>();
            if (gameBootstrapper == null)
            {
                gameBootstrapper = gameObject.AddComponent<GameBootstrapper>();
            }
        }
    }

    private void InitializeGameSystems()
    {
        // 4. Input Manager - Player input handling
        if (inputManager == null)
        {
            inputManager = FindObjectOfType<InputManager>();
            if (inputManager == null)
            {
                GameObject inputObj = new GameObject("InputManager");
                inputManager = inputObj.AddComponent<InputManager>();
            }
        }

        // 5. Save/Load System - Data persistence
        if (saveLoadSystem == null)
        {
            saveLoadSystem = FindObjectOfType<UnifiedSaveLoad>();
            if (saveLoadSystem == null)
            {
                GameObject saveObj = new GameObject("UnifiedSaveLoad");
                saveLoadSystem = saveObj.AddComponent<UnifiedSaveLoad>();
            }
        }

        // 6. Scene Manager - Level management
        if (sceneManager == null)
        {
            sceneManager = FindObjectOfType<UnifiedSceneManager>();
            if (sceneManager == null)
            {
                GameObject sceneObj = new GameObject("UnifiedSceneManager");
                sceneManager = sceneObj.AddComponent<UnifiedSceneManager>();
            }
        }
    }

    private void InitializeContentSystems()
    {
        // 7. UI System - User interface
        if (uiSystem == null)
        {
            uiSystem = FindObjectOfType<UnifiedUI>();
            if (uiSystem == null)
            {
                GameObject uiObj = new GameObject("UnifiedUI");
                uiSystem = uiObj.AddComponent<UnifiedUI>();
            }
        }

        // 8. Audio System - Sound management
        if (audioSystem == null)
        {
            audioSystem = FindObjectOfType<UnifiedAudio>();
            if (audioSystem == null)
            {
                GameObject audioObj = new GameObject("UnifiedAudio");
                audioSystem = audioObj.AddComponent<UnifiedAudio>();
            }
        }

        // 9. Camera System - View management
        if (cameraSystem == null)
        {
            cameraSystem = FindObjectOfType<UnifiedCamera>();
            if (cameraSystem == null)
            {
                GameObject cameraObj = new GameObject("UnifiedCamera");
                cameraSystem = cameraObj.AddComponent<UnifiedCamera>();
            }
        }

        // 10. Particle System - Visual effects
        if (particleSystem == null)
        {
            particleSystem = FindObjectOfType<UnifiedParticles>();
            if (particleSystem == null)
            {
                GameObject particleObj = new GameObject("UnifiedParticles");
                particleSystem = particleObj.AddComponent<UnifiedParticles>();
            }
        }

        // 11. Quest System - Mission management
        if (questSystem == null)
        {
            questSystem = FindObjectOfType<UnifiedQuest>();
            if (questSystem == null)
            {
                GameObject questObj = new GameObject("UnifiedQuest");
                questSystem = questObj.AddComponent<UnifiedQuest>();
            }
        }

        // 12. Inventory System - Item management
        if (inventorySystem == null)
        {
            inventorySystem = FindObjectOfType<UnifiedInventory>();
            if (inventorySystem == null)
            {
                GameObject inventoryObj = new GameObject("UnifiedInventory");
                inventorySystem = inventoryObj.AddComponent<UnifiedInventory>();
            }
        }

        // 13. Dialogue System - NPC conversations
        if (dialogueSystem == null)
        {
            dialogueSystem = FindObjectOfType<UnifiedDialogue>();
            if (dialogueSystem == null)
            {
                GameObject dialogueObj = new GameObject("UnifiedDialogue");
                dialogueSystem = dialogueObj.AddComponent<UnifiedDialogue>();
            }
        }
    }

    private void RegisterSystems()
    {
        // Register all systems with ServiceLocator
        ServiceLocator.RegisterService(inputManager);
        ServiceLocator.RegisterService(saveLoadSystem);
        ServiceLocator.RegisterService(sceneManager);
        ServiceLocator.RegisterService(uiSystem);
        ServiceLocator.RegisterService(audioSystem);
        ServiceLocator.RegisterService(cameraSystem);
        ServiceLocator.RegisterService(particleSystem);
        ServiceLocator.RegisterService(questSystem);
        ServiceLocator.RegisterService(inventorySystem);
        ServiceLocator.RegisterService(dialogueSystem);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("📋 All systems registered with ServiceLocator");
        }
    }

    private void InitializeGameState()
    {
        // Load saved game or start new game
        if (saveLoadSystem != null && saveLoadSystem.HasSaveFile())
        {
            saveLoadSystem.LoadGame();
            if (enableDebugLogging)
            {
                PerformanceUtils.Log("📁 Game loaded from save file");
            }
        }
        else
        {
            StartNewGame();
            if (enableDebugLogging)
            {
                PerformanceUtils.Log("🎮 Starting new game");
            }
        }

        // Initialize player
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }

        if (playerController != null)
        {
            ServiceLocator.RegisterService(playerController);
        }

        // Set up camera target
        if (cameraSystem != null && playerController != null)
        {
            cameraSystem.SetTarget(playerController.transform);
        }

        // Play default background music
        if (audioSystem != null)
        {
            audioSystem.PlayDefaultBGM();
        }
    }

    private void StartNewGame()
    {
        // Initialize default game state
        if (inventorySystem != null)
        {
            inventorySystem.AddItem("sword_basic", 1);
            inventorySystem.AddItem("potion_health", 3);
            inventorySystem.AddGold(100);
        }

        if (questSystem != null)
        {
            questSystem.ActivateQuest("tutorial_combat");
        }
    }

    /// <summary>
    /// Get system by type
    /// </summary>
    public T GetSystem<T>() where T : MonoBehaviour
    {
        return ServiceLocator.GetService<T>();
    }

    /// <summary>
    /// Check if all systems are ready
    /// </summary>
    public bool AreAllSystemsReady()
    {
        return ServiceLocator.AreAllServicesRegistered();
    }

    /// <summary>
    /// Get system status report
    /// </summary>
    public string GetSystemStatusReport()
    {
        string report = "=== Game Master System Status ===\n";

        report += "Service Locator: ✓ (Static)\n";
        report += "Game Events: ✓ (Static)\n";
        report += PerformanceUtils.FormatString("Input Manager: {0}\n", inputManager != null ? "✓" : "✗");
        report += PerformanceUtils.FormatString("Save/Load System: {0}\n", saveLoadSystem != null ? "✓" : "✗");
        report += PerformanceUtils.FormatString("Scene Manager: {0}\n", sceneManager != null ? "✓" : "✗");
        report += PerformanceUtils.FormatString("UI System: {0}\n", uiSystem != null ? "✓" : "✗");
        report += PerformanceUtils.FormatString("Audio System: {0}\n", audioSystem != null ? "✓" : "✗");
        report += PerformanceUtils.FormatString("Camera System: {0}\n", cameraSystem != null ? "✓" : "✗");
        report += PerformanceUtils.FormatString("Particle System: {0}\n", particleSystem != null ? "✓" : "✗");
        report += PerformanceUtils.FormatString("Quest System: {0}\n", questSystem != null ? "✓" : "✗");
        report += PerformanceUtils.FormatString("Inventory System: {0}\n", inventorySystem != null ? "✓" : "✗");
        report += PerformanceUtils.FormatString("Dialogue System: {0}\n", dialogueSystem != null ? "✓" : "✗");
        report += PerformanceUtils.FormatString("Player Controller: {0}\n", playerController != null ? "✓" : "✗");

        return report;
    }

    /// <summary>
    /// Pause game
    /// </summary>
    public void PauseGame()
    {
        Time.timeScale = 0f;
        if (audioSystem != null)
        {
            audioSystem.PauseAll();
        }
        GameEvents.OnGamePaused?.Invoke();
    }

    /// <summary>
    /// Resume game
    /// </summary>
    public void ResumeGame()
    {
        Time.timeScale = 1f;
        if (audioSystem != null)
        {
            audioSystem.ResumeAll();
        }
        GameEvents.OnGameResumed?.Invoke();
    }

    /// <summary>
    /// Save game
    /// </summary>
    public void SaveGame()
    {
        if (saveLoadSystem != null)
        {
            saveLoadSystem.SaveGame();
        }
    }

    /// <summary>
    /// Load game
    /// </summary>
    public void LoadGame()
    {
        if (saveLoadSystem != null)
        {
            saveLoadSystem.LoadGame();
        }
    }

    /// <summary>
    /// Quit game
    /// </summary>
    public void QuitGame()
    {
        SaveGame();
        Application.Quit();
    }

    /// <summary>
    /// Restart game
    /// </summary>
    public void RestartGame()
    {
        if (sceneManager != null)
        {
            sceneManager.RestartScene();
        }
    }

    private void Update()
    {
        // Handle global input
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Time.timeScale > 0f)
            {
                PauseGame();
            }
            else
            {
                ResumeGame();
            }
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            SaveGame();
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            LoadGame();
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }
}
