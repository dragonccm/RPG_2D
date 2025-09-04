using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Bootstrapper that initializes core systems and registers services
/// Attach this to a GameObject in your first scene
/// </summary>
public class GameBootstrapper : MonoBehaviour
{
    [Header("Service Registration")]
    [SerializeField] private bool autoRegisterServices = true;
    [SerializeField] private bool enableDevelopmentLogging = false;

    private void Awake()
    {
        // Set development logging flag
        PerformanceUtils.EnableDevelopmentLogging(enableDevelopmentLogging);

        if (autoRegisterServices)
        {
            RegisterAllServices();
        }

        PerformanceUtils.Log("🎮 Game Bootstrapper initialized successfully!");
    }

    private void RegisterAllServices()
    {
        // Core Services
        RegisterService<UIManager>();
        RegisterService<ModularSkillManager>();
        RegisterService<PlayerController>();
        RegisterService<Character>("Player");
        RegisterService<UI.PauseMenu>();
        RegisterService<TargetingSystem>();
        RegisterService<NearbyHealthDisplay>();

        // UI Services
        RegisterService<SkillPanelUI>();
        RegisterService<SkillDetailUI>();
        RegisterService<PlayerUI>();

        PerformanceUtils.Log("✅ All core services registered successfully!");
    }

    private void RegisterService<T>() where T : UnityEngine.Object
    {
        var service = FindFirstObjectByType<T>();
        if (service != null)
        {
            ServiceLocator.Register(service);
            PerformanceUtils.Log(PerformanceUtils.FormatString("📋 Registered service: {0}", typeof(T).Name));
        }
        else
        {
            // Special handling for UI.PauseMenu - create one if it doesn't exist
            if (typeof(T) == typeof(UI.PauseMenu))
            {
                service = CreateBasicPauseMenu() as T;
                if (service != null)
                {
                    ServiceLocator.Register(service);
                    PerformanceUtils.Log("📋 Created and registered basic UI.PauseMenu");
                    return;
                }
            }

            PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ Service not found: {0}", typeof(T).Name));
        }
    }

    private void RegisterService<T>(string tag) where T : UnityEngine.Object
    {
        var obj = GameObject.FindGameObjectWithTag(tag);
        if (obj != null)
        {
            var service = obj.GetComponent<T>();
            if (service != null)
            {
                ServiceLocator.Register(service);
                PerformanceUtils.Log(PerformanceUtils.FormatString("📋 Registered service: {0} (by tag: {1})", typeof(T).Name, tag));
            }
        }
    }

    private void OnDestroy()
    {
        // Clear all services when game ends
        ServiceLocator.Clear();
        PerformanceUtils.Log("🧹 Services cleared on game shutdown");
    }

    /// <summary>
    /// Create a basic pause menu if none exists
    /// </summary>
    private UI.PauseMenu CreateBasicPauseMenu()
    {
        PerformanceUtils.Log("🎮 Creating basic pause menu...");

        // Find or create canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Create EventSystem if it doesn't exist
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }
        }

        // Create pause menu GameObject
        GameObject pauseMenuObj = new GameObject("PauseMenu");
        pauseMenuObj.transform.SetParent(canvas.transform, false);

        // Add UI.PauseMenu component
        UI.PauseMenu pauseMenuComponent = pauseMenuObj.AddComponent<UI.PauseMenu>();

        // Create basic pause panel
        GameObject panel = new GameObject("PausePanel");
        panel.transform.SetParent(pauseMenuObj.transform, false);

        // Add CanvasGroup for fade effects
        CanvasGroup canvasGroup = panel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f; // Start hidden

        // Add Image component for background
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f); // Semi-transparent black

        // Set panel to fill screen
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Create title text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(panel.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "PAUSED";
        titleText.fontSize = 48;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.7f);
        titleRect.anchorMax = new Vector2(0.5f, 0.7f);
        titleRect.sizeDelta = new Vector2(300, 60);
        titleRect.anchoredPosition = Vector2.zero;

        // Create Resume button
        GameObject resumeBtnObj = CreateButton("ResumeButton", "Resume", panel.transform);
        RectTransform resumeRect = resumeBtnObj.GetComponent<RectTransform>();
        resumeRect.anchorMin = new Vector2(0.5f, 0.5f);
        resumeRect.anchorMax = new Vector2(0.5f, 0.5f);
        resumeRect.sizeDelta = new Vector2(200, 50);
        resumeRect.anchoredPosition = new Vector2(0, 20);

        // Create Main Menu button
        GameObject menuBtnObj = CreateButton("MainMenuButton", "Main Menu", panel.transform);
        RectTransform menuRect = menuBtnObj.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.5f, 0.4f);
        menuRect.anchorMax = new Vector2(0.5f, 0.4f);
        menuRect.sizeDelta = new Vector2(200, 50);
        menuRect.anchoredPosition = new Vector2(0, -30);

        // Assign to pause menu component
        pauseMenuComponent.pauseMenuPanel = panel;
        pauseMenuComponent.resumeButton = resumeBtnObj.GetComponent<Button>();
        pauseMenuComponent.mainMenuButton = menuBtnObj.GetComponent<Button>();

        // Add button listeners
        if (pauseMenuComponent.resumeButton != null)
        {
            pauseMenuComponent.resumeButton.onClick.AddListener(() => pauseMenuComponent.ResumeGame());
        }

        if (pauseMenuComponent.mainMenuButton != null)
        {
            pauseMenuComponent.mainMenuButton.onClick.AddListener(() => pauseMenuComponent.GoToMainMenu());
        }

        PerformanceUtils.Log("🎮 Basic pause menu created successfully!");
        return pauseMenuComponent;
    }

    /// <summary>
    /// Helper method to create a button
    /// </summary>
    private GameObject CreateButton(string name, string text, Transform parent)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        // Add Button component
        Button button = buttonObj.AddComponent<Button>();

        // Add Image for background
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark gray

        // Create text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 24;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        // Set text rect to fill button
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return buttonObj;
    }
}
