using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Controller cho Pause Menu
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        // Tự động tìm các components nếu chưa được gán
        if (pausePanel == null)
            pausePanel = transform.Find("PausePanel")?.gameObject;

        if (resumeButton == null)
            resumeButton = transform.Find("PausePanel/ResumeButton")?.GetComponent<Button>();

        if (settingsButton == null)
            settingsButton = transform.Find("PausePanel/SettingsButton")?.GetComponent<Button>();

        if (quitButton == null)
            quitButton = transform.Find("PausePanel/QuitButton")?.GetComponent<Button>();
    }

    private void Start()
    {
        // Setup button listeners
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        // Ẩn panel khi bắt đầu
        HidePauseMenu();
    }

    public void ShowPauseMenu()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            Debug.Log("? Pause menu shown");
        }
    }

    public void HidePauseMenu()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            Debug.Log("? Pause menu hidden");
        }
    }

    private void OnResumeClicked()
    {
        if (PauseMenu.Instance != null)
        {
            PauseMenu.Instance.TogglePause();
        }
    }

    private void OnSettingsClicked()
    {
        Debug.Log("? Settings button clicked - Implement settings panel");
        // TODO: Implement settings panel
    }

    private void OnQuitClicked()
    {
        Debug.Log("? Quit button clicked");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // Context menu để test
    [ContextMenu("Test Show Pause Menu")]
    public void TestShow()
    {
        ShowPauseMenu();
    }

    [ContextMenu("Test Hide Pause Menu")]
    public void TestHide()
    {
        HidePauseMenu();
    }
}