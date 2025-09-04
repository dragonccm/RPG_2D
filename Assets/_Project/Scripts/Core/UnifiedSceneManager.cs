using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Unified scene management system to replace multiple scene managers
/// Consolidates SceneManager, LevelLoader, and transition controllers
/// </summary>
public class UnifiedSceneManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string gameScene = "Game";
    [SerializeField] private string loadingScene = "Loading";
    [SerializeField] private bool enableDebugLogging = true;

    [Header("Transition Settings")]
    [SerializeField] private GameObject fadePanel;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private AsyncOperation loadingOperation;
    private bool isLoading = false;
    private string targetScene;
    private System.Action onSceneLoaded;

    private void Awake()
    {
        ServiceLocator.RegisterService(this);

        // Ensure this manager persists across scenes
        DontDestroyOnLoad(gameObject);

        // Initialize fade panel
        if (fadePanel == null)
        {
            CreateFadePanel();
        }
    }

    private void CreateFadePanel()
    {
        GameObject canvas = new GameObject("FadeCanvas");
        canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        fadePanel = new GameObject("FadePanel");
        fadePanel.transform.SetParent(canvas.transform);
        var image = fadePanel.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.black;
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.sizeDelta = Vector2.zero;

        DontDestroyOnLoad(canvas);
    }

    /// <summary>
    /// Load scene synchronously
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (isLoading)
        {
            PerformanceUtils.LogWarning("⚠️ Already loading a scene");
            return;
        }

        StartCoroutine(LoadSceneCoroutine(sceneName, null));
    }

    /// <summary>
    /// Load scene synchronously with callback
    /// </summary>
    public void LoadScene(string sceneName, System.Action onLoaded)
    {
        if (isLoading)
        {
            PerformanceUtils.LogWarning("⚠️ Already loading a scene");
            return;
        }

        StartCoroutine(LoadSceneCoroutine(sceneName, onLoaded));
    }

    /// <summary>
    /// Load scene asynchronously
    /// </summary>
    public void LoadSceneAsync(string sceneName)
    {
        if (isLoading)
        {
            PerformanceUtils.LogWarning("⚠️ Already loading a scene");
            return;
        }

        StartCoroutine(LoadSceneAsyncCoroutine(sceneName, null));
    }

    /// <summary>
    /// Load scene asynchronously with callback
    /// </summary>
    public void LoadSceneAsync(string sceneName, System.Action onLoaded)
    {
        if (isLoading)
        {
            PerformanceUtils.LogWarning("⚠️ Already loading a scene");
            return;
        }

        StartCoroutine(LoadSceneAsyncCoroutine(sceneName, onLoaded));
    }

    /// <summary>
    /// Load main menu
    /// </summary>
    public void LoadMainMenu()
    {
        LoadScene(mainMenuScene);
    }

    /// <summary>
    /// Load game scene
    /// </summary>
    public void LoadGameScene()
    {
        LoadScene(gameScene);
    }

    /// <summary>
    /// Restart current scene
    /// </summary>
    public void RestartScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Load next scene in build order
    /// </summary>
    public void LoadNextScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            LoadScene(SceneManager.GetSceneByBuildIndex(nextIndex).name);
        }
        else
        {
            PerformanceUtils.LogWarning("⚠️ No next scene available");
        }
    }

    /// <summary>
    /// Load previous scene in build order
    /// </summary>
    public void LoadPreviousScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int prevIndex = currentIndex - 1;

        if (prevIndex >= 0)
        {
            LoadScene(SceneManager.GetSceneByBuildIndex(prevIndex).name);
        }
        else
        {
            PerformanceUtils.LogWarning("⚠️ No previous scene available");
        }
    }

    /// <summary>
    /// Synchronous scene loading coroutine
    /// </summary>
    private System.Collections.IEnumerator LoadSceneCoroutine(string sceneName, System.Action onLoaded)
    {
        isLoading = true;
        targetScene = sceneName;

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🔄 Loading scene: {0}", sceneName));
        }

        // Fade out
        yield return StartCoroutine(FadeOut());

        // Save current game state
        SaveCurrentState();

        // Load scene
        SceneManager.LoadScene(sceneName);

        // Wait for scene to load
        yield return null;

        // Initialize new scene
        InitializeScene();

        // Fade in
        yield return StartCoroutine(FadeIn());

        // Execute callback
        onLoaded?.Invoke();

        // Trigger scene loaded event
        GameEvents.OnSceneLoaded?.Invoke(sceneName);

        isLoading = false;

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("✅ Scene loaded: {0}", sceneName));
        }
    }

    /// <summary>
    /// Asynchronous scene loading coroutine
    /// </summary>
    private System.Collections.IEnumerator LoadSceneAsyncCoroutine(string sceneName, System.Action onLoaded)
    {
        isLoading = true;
        targetScene = sceneName;

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🔄 Loading scene async: {0}", sceneName));
        }

        // Fade out
        yield return StartCoroutine(FadeOut());

        // Save current game state
        SaveCurrentState();

        // Load loading scene first if available
        if (!string.IsNullOrEmpty(loadingScene) && SceneManager.GetSceneByName(loadingScene).IsValid())
        {
            SceneManager.LoadScene(loadingScene);
            yield return null;
        }

        // Start async loading
        loadingOperation = SceneManager.LoadSceneAsync(sceneName);
        loadingOperation.allowSceneActivation = false;

        // Wait for loading to complete
        while (loadingOperation.progress < 0.9f)
        {
            // Update loading progress UI here
            float progress = loadingOperation.progress / 0.9f;
            GameEvents.OnLoadingProgress?.Invoke(progress);

            yield return null;
        }

        // Activate the scene
        loadingOperation.allowSceneActivation = true;

        // Wait for scene activation
        while (!loadingOperation.isDone)
        {
            yield return null;
        }

        // Initialize new scene
        InitializeScene();

        // Fade in
        yield return StartCoroutine(FadeIn());

        // Execute callback
        onLoaded?.Invoke();

        // Trigger scene loaded event
        GameEvents.OnSceneLoaded?.Invoke(sceneName);

        isLoading = false;

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("✅ Scene loaded async: {0}", sceneName));
        }
    }

    /// <summary>
    /// Fade out screen
    /// </summary>
    private System.Collections.IEnumerator FadeOut()
    {
        if (fadePanel == null) yield break;

        var image = fadePanel.GetComponent<UnityEngine.UI.Image>();
        if (image == null) yield break;

        float elapsed = 0f;
        Color startColor = new Color(0f, 0f, 0f, 0f);
        Color endColor = new Color(0f, 0f, 0f, 1f);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeCurve.Evaluate(elapsed / fadeDuration);
            image.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        image.color = endColor;
    }

    /// <summary>
    /// Fade in screen
    /// </summary>
    private System.Collections.IEnumerator FadeIn()
    {
        if (fadePanel == null) yield break;

        var image = fadePanel.GetComponent<UnityEngine.UI.Image>();
        if (image == null) yield break;

        float elapsed = 0f;
        Color startColor = new Color(0f, 0f, 0f, 1f);
        Color endColor = new Color(0f, 0f, 0f, 0f);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeCurve.Evaluate(elapsed / fadeDuration);
            image.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        image.color = endColor;
    }

    /// <summary>
    /// Save current game state
    /// </summary>
    private void SaveCurrentState()
    {
        // Auto-save game state
        var saveLoad = ServiceLocator.GetService<UnifiedSaveLoad>();
        if (saveLoad != null)
        {
            saveLoad.SaveGame();
        }
    }

    /// <summary>
    /// Initialize newly loaded scene
    /// </summary>
    private void InitializeScene()
    {
        // Reinitialize services if needed
        ServiceLocator.InitializeServices();

        // Find and initialize scene-specific objects
        InitializeSceneObjects();
    }

    /// <summary>
    /// Initialize scene objects
    /// </summary>
    private void InitializeSceneObjects()
    {
        // Find player spawn points
        var spawnPoints = FindObjectsOfType<PlayerSpawnPoint>();
        if (spawnPoints.Length > 0)
        {
            var player = ServiceLocator.GetService<PlayerController>();
            if (player != null)
            {
                player.transform.position = spawnPoints[0].transform.position;
            }
        }

        // Initialize scene cameras
        var sceneCameras = FindObjectsOfType<SceneCamera>();
        foreach (var sceneCam in sceneCameras)
        {
            sceneCam.Initialize();
        }
    }

    /// <summary>
    /// Get current scene name
    /// </summary>
    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// Get current scene build index
    /// </summary>
    public int GetCurrentSceneIndex()
    {
        return SceneManager.GetActiveScene().buildIndex;
    }

    /// <summary>
    /// Check if scene exists in build settings
    /// </summary>
    public bool SceneExists(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var scene = SceneManager.GetSceneByBuildIndex(i);
            if (scene.name == sceneName)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Get all scenes in build settings
    /// </summary>
    public List<string> GetAllScenes()
    {
        List<string> scenes = new List<string>();
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            scenes.Add(SceneManager.GetSceneByBuildIndex(i).name);
        }
        return scenes;
    }

    /// <summary>
    /// Additively load a scene
    /// </summary>
    public void LoadSceneAdditive(string sceneName)
    {
        StartCoroutine(LoadSceneAdditiveCoroutine(sceneName));
    }

    private System.Collections.IEnumerator LoadSceneAdditiveCoroutine(string sceneName)
    {
        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🔄 Loading scene additive: {0}", sceneName));
        }

        var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        while (!operation.isDone)
        {
            yield return null;
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("✅ Scene loaded additive: {0}", sceneName));
        }
    }

    /// <summary>
    /// Unload a scene
    /// </summary>
    public void UnloadScene(string sceneName)
    {
        StartCoroutine(UnloadSceneCoroutine(sceneName));
    }

    private System.Collections.IEnumerator UnloadSceneCoroutine(string sceneName)
    {
        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🔄 Unloading scene: {0}", sceneName));
        }

        var operation = SceneManager.UnloadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            yield return null;
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("✅ Scene unloaded: {0}", sceneName));
        }
    }

    /// <summary>
    /// Check if currently loading
    /// </summary>
    public bool IsLoading()
    {
        return isLoading;
    }

    /// <summary>
    /// Get loading progress (0-1)
    /// </summary>
    public float GetLoadingProgress()
    {
        if (loadingOperation != null)
        {
            return loadingOperation.progress / 0.9f;
        }
        return 0f;
    }

    /// <summary>
    /// Set fade duration
    /// </summary>
    public void SetFadeDuration(float duration)
    {
        fadeDuration = duration;
    }

    /// <summary>
    /// Set fade curve
    /// </summary>
    public void SetFadeCurve(AnimationCurve curve)
    {
        fadeCurve = curve;
    }
}

/// <summary>
/// Player spawn point component
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId = "default";

    public string SpawnId => spawnId;
}

/// <summary>
/// Scene camera component
/// </summary>
public class SceneCamera : MonoBehaviour
{
    [SerializeField] private bool setAsMainCamera = true;

    public void Initialize()
    {
        if (setAsMainCamera)
        {
            var camera = GetComponent<Camera>();
            if (camera != null)
            {
                // Set as main camera if needed
                // Camera.main = camera;
            }
        }
    }
}
