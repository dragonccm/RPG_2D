using UnityEngine;

/// <summary>
/// Automatically registers core services to ServiceLocator
/// Attach this to a GameObject that persists across scenes
/// </summary>
public class ServiceRegistrar : MonoBehaviour
{
    private void Awake()
    {
        // Register core services
        RegisterCoreServices();
    }

    private void RegisterCoreServices()
    {
        // Register UIManager
        var uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            ServiceLocator.Register(uiManager);
        }

        // Register ModularSkillManager
        var skillManager = FindFirstObjectByType<ModularSkillManager>();
        if (skillManager != null)
        {
            ServiceLocator.Register(skillManager);
        }

        // Register PlayerController
        var playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            ServiceLocator.Register(playerController);
        }

        // Register Character (Player)
        var playerCharacter = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Character>();
        if (playerCharacter != null)
        {
            ServiceLocator.Register(playerCharacter);
        }

        // Register PauseMenu - Note: GameBootstrapper handles this
        // var pauseMenu = FindFirstObjectByType<UI.PauseMenu>();
        // if (pauseMenu != null)
        // {
        //     ServiceLocator.Register(pauseMenu);
        // }

        // Register TargetingSystem
        var targetingSystem = FindFirstObjectByType<TargetingSystem>();
        if (targetingSystem != null)
        {
            ServiceLocator.Register(targetingSystem);
        }

        // Register NearbyHealthDisplay
        var nearbyHealthDisplay = FindFirstObjectByType<NearbyHealthDisplay>();
        if (nearbyHealthDisplay != null)
        {
            ServiceLocator.Register(nearbyHealthDisplay);
        }
    }

    private void OnDestroy()
    {
        // Clear services when scene changes
        ServiceLocator.Clear();
    }
}
