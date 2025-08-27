using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý pause/unpause game và các listeners
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("Pause Settings")]
    [SerializeField] private bool pauseOnStart = false;
    [SerializeField] private float timeScaleWhenPaused = 0f;
    [SerializeField] private bool lockCursorWhenResumed = true;

    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private PauseMenuUI pauseMenuUIController;

    private List<IPauseListener> pauseListeners = new List<IPauseListener>();
    private bool isPaused = false;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Don't destroy on load
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Auto-find UI components if not assigned
        if (pauseMenuUI == null)
            pauseMenuUI = transform.Find("PauseMenuUI")?.gameObject;

        if (pauseMenuUIController == null)
            pauseMenuUIController = GetComponentInChildren<PauseMenuUI>(true);

        // Hide pause menu on start
        SetPauseMenuUI(false);

        // Apply initial pause state
        if (pauseOnStart)
            Pause();
    }

    /// <summary>
    /// Toggle pause/unpause
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    /// <summary>
    /// Pause the game
    /// </summary>
    public void Pause()
    {
        if (isPaused) return;

        isPaused = true;
        Time.timeScale = timeScaleWhenPaused;

        // Show pause menu UI
        SetPauseMenuUI(true);

        // Notify all listeners
        foreach (var listener in pauseListeners)
        {
            listener.OnPause();
        }
    }

    /// <summary>
    /// Resume the game
    /// </summary>
    public void Resume()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;

        // Hide pause menu UI
        SetPauseMenuUI(false);

        // Notify all listeners
        foreach (var listener in pauseListeners)
        {
            listener.OnResume();
        }

        // Lock cursor if needed
        if (lockCursorWhenResumed)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Register a pause listener
    /// </summary>
    public void RegisterPauseListener(IPauseListener listener)
    {
        if (!pauseListeners.Contains(listener))
        {
            pauseListeners.Add(listener);
        }
    }

    /// <summary>
    /// Unregister a pause listener
    /// </summary>
    public void UnregisterPauseListener(IPauseListener listener)
    {
        if (pauseListeners.Contains(listener))
        {
            pauseListeners.Remove(listener);
        }
    }

    /// <summary>
    /// Show/hide pause menu UI
    /// </summary>
    private void SetPauseMenuUI(bool show)
    {
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(show);

        if (pauseMenuUIController != null)
        {
            if (show)
                pauseMenuUIController.ShowPauseMenu();
            else
                pauseMenuUIController.HidePauseMenu();
        }

        // Show cursor when paused
        if (show)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// Add a listener to the pause system
    /// </summary>
    public static void AddListener(IPauseListener listener)
    {
        if (Instance != null)
            Instance.RegisterPauseListener(listener);
    }

    /// <summary>
    /// Remove a listener from the pause system
    /// </summary>
    public static void RemoveListener(IPauseListener listener)
    {
        if (Instance != null)
            Instance.UnregisterPauseListener(listener);
    }

    private void OnDestroy()
    {
        // Clean up listeners
        pauseListeners.Clear();

        if (Instance == this)
            Instance = null;
    }

    // Context menu for testing
    [ContextMenu("Test Pause")]
    public void TestPause()
    {
        Pause();
    }

    [ContextMenu("Test Resume")]
    public void TestResume()
    {
        Resume();
    }

    [ContextMenu("Test Toggle")]
    public void TestToggle()
    {
        TogglePause();
    }
}

/// <summary>
/// Interface for pause listeners
/// </summary>
public interface IPauseListener
{
    void OnPause();
    void OnResume();
}