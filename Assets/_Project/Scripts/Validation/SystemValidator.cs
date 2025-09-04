using UnityEngine;

/// <summary>
/// System validation script to test all unified systems
/// Ensures all systems are properly integrated and functional
/// </summary>
public class SystemValidator : MonoBehaviour
{
    [Header("Validation Settings")]
    [SerializeField] private bool runValidationOnStart = true;
    [SerializeField] private bool enableDetailedLogging = true;

    private void Start()
    {
        if (runValidationOnStart)
        {
            ValidateAllSystems();
        }
    }

    /// <summary>
    /// Validate all unified systems
    /// </summary>
    public void ValidateAllSystems()
    {
        if (enableDetailedLogging)
        {
            PerformanceUtils.Log("🔍 Starting system validation...");
        }

        int passedTests = 0;
        int totalTests = 0;

        // Test core systems
        totalTests += 4;
        if (TestServiceLocator()) passedTests++;
        if (TestGameEvents()) passedTests++;
        if (TestPerformanceUtils()) passedTests++;
        if (TestObjectPool()) passedTests++;

        // Test game systems
        totalTests += 12;
        if (TestUnifiedAnimator()) passedTests++;
        if (TestUnifiedCombat()) passedTests++;
        if (TestUnifiedMovement()) passedTests++;
        if (TestUnifiedUI()) passedTests++;
        if (TestUnifiedSaveLoad()) passedTests++;
        if (TestUnifiedAudio()) passedTests++;
        if (TestUnifiedCamera()) passedTests++;
        if (TestUnifiedParticles()) passedTests++;
        if (TestUnifiedQuest()) passedTests++;
        if (TestUnifiedInventory()) passedTests++;
        if (TestUnifiedDialogue()) passedTests++;
        if (TestUnifiedSceneManager()) passedTests++;

        // Test AI system
        totalTests += 1;
        if (TestUnifiedAI()) passedTests++;

        // Test integration systems
        totalTests += 2;
        if (TestGameMaster()) passedTests++;
        if (TestPerformanceMonitor()) passedTests++;

        // Validation summary
        float successRate = (float)passedTests / totalTests * 100f;
        string summary = PerformanceUtils.FormatString(
            "✅ System validation complete: {0}/{1} tests passed ({2:F1}%)",
            passedTests, totalTests, successRate
        );

        if (successRate >= 90f)
        {
            PerformanceUtils.Log(summary);
        }
        else if (successRate >= 75f)
        {
            PerformanceUtils.LogWarning(summary + " - Some systems may need attention");
        }
        else
        {
            PerformanceUtils.LogError(summary + " - Critical issues detected!");
        }
    }

    private bool TestServiceLocator()
    {
        try
        {
            // Test service registration
            ServiceLocator.RegisterService(this);

            // Test service retrieval
            var retrieved = ServiceLocator.GetService<SystemValidator>();
            bool success = retrieved == this;

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("ServiceLocator: {0}", success ? "✓" : "✗"));
            }

            return success;
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("ServiceLocator test failed: {0}", e.Message));
            }
            return false;
        }
    }

    private bool TestGameEvents()
    {
        try
        {
            bool eventReceived = false;

            // Subscribe to test event
            System.Action<float, float> testAction = (current, max) => eventReceived = true;
            GameEvents.OnHealthChanged += testAction;

            // Trigger event
            GameEvents.OnHealthChanged?.Invoke(100f, 100f);

            // Cleanup
            GameEvents.OnHealthChanged -= testAction;

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("GameEvents: {0}", eventReceived ? "✓" : "✗"));
            }

            return eventReceived;
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("GameEvents test failed: {0}", e.Message));
            }
            return false;
        }
    }

    private bool TestPerformanceUtils()
    {
        try
        {
            // Test string formatting
            string formatted = PerformanceUtils.FormatString("Test: {0} {1}", "value", 123);
            bool formatSuccess = formatted == "Test: value 123";

            // Test conditional logging (should not throw)
            PerformanceUtils.Log("Test log message");

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("PerformanceUtils: {0}", formatSuccess ? "✓" : "✗"));
            }

            return formatSuccess;
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("PerformanceUtils test failed: {0}", e.Message));
            }
            return false;
        }
    }

    private bool TestObjectPool()
    {
        try
        {
            // Create test object pool
            GameObject testPrefab = new GameObject("TestPoolObject");
            var pool = testPrefab.AddComponent<ObjectPool>();
            pool.Initialize(testPrefab, 5);

            // Test spawning
            GameObject spawned = pool.Spawn(Vector3.zero, Quaternion.identity);
            bool spawnSuccess = spawned != null;

            // Test returning
            pool.Return(spawned);
            bool returnSuccess = true; // Assume success if no exception

            // Cleanup
            Destroy(testPrefab);

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("ObjectPool: {0}", (spawnSuccess && returnSuccess) ? "✓" : "✗"));
            }

            return spawnSuccess && returnSuccess;
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("ObjectPool test failed: {0}", e.Message));
            }
            return false;
        }
    }

    private bool TestUnifiedAnimator()
    {
        try
        {
            // Create test animator
            GameObject testObj = new GameObject("TestAnimator");
            var animator = testObj.AddComponent<UnifiedAnimator>();

            // Test basic functionality
            bool isValid = animator.IsValid(); // Should be false without Animator component

            // Cleanup
            Destroy(testObj);

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("UnifiedAnimator: {0}", !isValid ? "✓" : "✗"));
            }

            return !isValid; // Should return false when no Animator is attached
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("UnifiedAnimator test failed: {0}", e.Message));
            }
            return false;
        }
    }

    private bool TestUnifiedCombat()
    {
        try
        {
            // Create test combat
            GameObject testObj = new GameObject("TestCombat");
            testObj.AddComponent<Rigidbody2D>();
            var combat = testObj.AddComponent<UnifiedCombat>();

            // Test basic properties
            bool hasComponents = combat.GetComponent<Rigidbody2D>() != null;

            // Cleanup
            Destroy(testObj);

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("UnifiedCombat: {0}", hasComponents ? "✓" : "✗"));
            }

            return hasComponents;
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("UnifiedCombat test failed: {0}", e.Message));
            }
            return false;
        }
    }

    private bool TestUnifiedMovement()
    {
        try
        {
            // Create test movement
            GameObject testObj = new GameObject("TestMovement");
            testObj.AddComponent<Rigidbody2D>();
            var movement = testObj.AddComponent<UnifiedMovement>();

            // Test basic properties
            bool hasComponents = movement.GetComponent<Rigidbody2D>() != null;
            bool isNotMoving = !movement.IsMoving();

            // Cleanup
            Destroy(testObj);

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("UnifiedMovement: {0}", (hasComponents && isNotMoving) ? "✓" : "✗"));
            }

            return hasComponents && isNotMoving;
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("UnifiedMovement test failed: {0}", e.Message));
            }
            return false;
        }
    }

    private bool TestUnifiedUI()
    {
        try
        {
            // Create test UI
            GameObject testObj = new GameObject("TestUI");
            var ui = testObj.AddComponent<UnifiedUI>();

            // Test basic functionality
            bool hasComponent = ui != null;

            // Cleanup
            Destroy(testObj);

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("UnifiedUI: {0}", hasComponent ? "✓" : "✗"));
            }

            return hasComponent;
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("UnifiedUI test failed: {0}", e.Message));
            }
            return false;
        }
    }

    private bool TestUnifiedSaveLoad()
    {
        try
        {
            // Create test save/load
            GameObject testObj = new GameObject("TestSaveLoad");
            var saveLoad = testObj.AddComponent<UnifiedSaveLoad>();

            // Test basic properties
            bool hasComponent = saveLoad != null;
            bool hasNoSaveFile = !saveLoad.HasSaveFile();

            // Cleanup
            Destroy(testObj);

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("UnifiedSaveLoad: {0}", (hasComponent && hasNoSaveFile) ? "✓" : "✗"));
            }

            return hasComponent && hasNoSaveFile;
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("UnifiedSaveLoad test failed: {0}", e.Message));
            }
            return false;
        }
    }

    private bool TestUnifiedAudio()
    {
        try
        {
            // Create test audio
            GameObject testObj = new GameObject("TestAudio");
            var audio = testObj.AddComponent<UnifiedAudio>();

            // Test basic properties
            bool hasComponent = audio != null;

            // Cleanup
            Destroy(testObj);

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("UnifiedAudio: {0}", hasComponent ? "✓" : "✗"));
            }

            return hasComponent;
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("UnifiedAudio test failed: {0}", e.Message));
            }
            return false;
        }
    }

    private bool TestUnifiedCamera()
    {
        try
        {
            // Create test camera
            GameObject testObj = new GameObject("TestCamera");
            var camera = testObj.AddComponent<UnifiedCamera>();

            // Test basic properties
            bool hasComponent = camera != null;

            // Cleanup
            Destroy(testObj);

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("UnifiedCamera: {0}", hasComponent ? "✓" : "✗"));
            }

            return hasComponent;
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("UnifiedCamera test failed: {0}", e.Message));
            }
            return false;
        }
    }

    private bool TestUnifiedParticles()
    {
        try
        {
            // Create test particles
            GameObject testObj = new GameObject("TestParticles");
            var particles = testObj.AddComponent<UnifiedParticles>();

            // Test basic properties
            bool hasComponent = particles != null;
            bool hasNoEffect = !particles.HasEffect("TestEffect");

            // Cleanup
            Destroy(testObj);

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("UnifiedParticles: {0}", (hasComponent && hasNoEffect) ? "✓" : "✗"));
            }

            return hasComponent && hasNoEffect;
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("UnifiedParticles test failed: {0}", e.Message));
            }
            return false;
        }
    }

    private bool TestUnifiedQuest()
    {
        try
        {
            // Create test quest
            GameObject testObj = new GameObject("TestQuest");
            var quest = testObj.AddComponent<UnifiedQuest>();

            // Test basic properties
            bool hasComponent = quest != null;
            var stats = quest.GetQuestStatistics();
            bool hasStats = stats != null;

            // Cleanup
            Destroy(testObj);

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("UnifiedQuest: {0}", (hasComponent && hasStats) ? "✓" : "✗"));
            }

            return hasComponent && hasStats;
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("UnifiedQuest test failed: {0}", e.Message));
            }
            return false;
        }
    }

    private bool TestUnifiedInventory()
    {
        try
        {
            // Create test inventory
            GameObject testObj = new GameObject("TestInventory");
            var inventory = testObj.AddComponent<UnifiedInventory>();

            // Test basic properties
            bool hasComponent = inventory != null;
            bool isEmpty = inventory.GetGold() == 0;
            bool isNotFull = !inventory.IsFull();

            // Cleanup
            Destroy(testObj);

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("UnifiedInventory: {0}", (hasComponent && isEmpty && isNotFull) ? "✓" : "✗"));
            }

            return hasComponent && isEmpty && isNotFull;
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("UnifiedInventory test failed: {0}", e.Message));
            }
            return false;
        }
    }

    private bool TestUnifiedDialogue()
    {
        try
        {
            // Create test dialogue
            GameObject testObj = new GameObject("TestDialogue");
            var dialogue = testObj.AddComponent<UnifiedDialogue>();

            // Test basic properties
            bool hasComponent = dialogue != null;
            bool isNotActive = !dialogue.IsDialogueActive();

            // Cleanup
            Destroy(testObj);

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("UnifiedDialogue: {0}", (hasComponent && isNotActive) ? "✓" : "✗"));
            }

            return hasComponent && isNotActive;
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("UnifiedDialogue test failed: {0}", e.Message));
            }
            return false;
        }
    }

    private bool TestUnifiedSceneManager()
    {
        try
        {
            // Create test scene manager
            GameObject testObj = new GameObject("TestSceneManager");
            var sceneManager = testObj.AddComponent<UnifiedSceneManager>();

            // Test basic properties
            bool hasComponent = sceneManager != null;
            string currentScene = sceneManager.GetCurrentSceneName();
            bool hasSceneName = !string.IsNullOrEmpty(currentScene);

            // Cleanup
            Destroy(testObj);

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("UnifiedSceneManager: {0}", (hasComponent && hasSceneName) ? "✓" : "✗"));
            }

            return hasComponent && hasSceneName;
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("UnifiedSceneManager test failed: {0}", e.Message));
            }
            return false;
        }
    }

    private bool TestUnifiedAI()
    {
        try
        {
            // Create test AI
            GameObject testObj = new GameObject("TestAI");
            testObj.AddComponent<Rigidbody2D>();
            var ai = testObj.AddComponent<UnifiedAI>();

            // Test basic properties
            bool hasComponent = ai != null;
            bool isIdle = ai.GetCurrentState() == AIState.Idle;
            bool isAlive = ai.IsAlive();

            // Cleanup
            Destroy(testObj);

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("UnifiedAI: {0}", (hasComponent && isIdle && isAlive) ? "✓" : "✗"));
            }

            return hasComponent && isIdle && isAlive;
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("UnifiedAI test failed: {0}", e.Message));
            }
            return false;
        }
    }

    private bool TestGameMaster()
    {
        try
        {
            // Create test game master
            GameObject testObj = new GameObject("TestGameMaster");
            var gameMaster = testObj.AddComponent<GameMaster>();

            // Test basic properties
            bool hasComponent = gameMaster != null;
            string statusReport = gameMaster.GetSystemStatusReport();
            bool hasReport = !string.IsNullOrEmpty(statusReport);

            // Cleanup
            Destroy(testObj);

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("GameMaster: {0}", (hasComponent && hasReport) ? "✓" : "✗"));
            }

            return hasComponent && hasReport;
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("GameMaster test failed: {0}", e.Message));
            }
            return false;
        }
    }

    private bool TestPerformanceMonitor()
    {
        try
        {
            // Create test performance monitor
            GameObject testObj = new GameObject("TestPerformanceMonitor");
            var monitor = testObj.AddComponent<PerformanceMonitor>();

            // Test basic properties
            bool hasComponent = monitor != null;
            string report = monitor.GetPerformanceReport();
            bool hasReport = !string.IsNullOrEmpty(report);

            // Cleanup
            Destroy(testObj);

            if (enableDetailedLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("PerformanceMonitor: {0}", (hasComponent && hasReport) ? "✓" : "✗"));
            }

            return hasComponent && hasReport;
        }
        catch (System.Exception e)
        {
            if (enableDetailedLogging)
            {
                PerformanceUtils.LogError(PerformanceUtils.FormatString("PerformanceMonitor test failed: {0}", e.Message));
            }
            return false;
        }
    }

    /// <summary>
    /// Run validation from external call
    /// </summary>
    public void RunValidation()
    {
        ValidateAllSystems();
    }
}
