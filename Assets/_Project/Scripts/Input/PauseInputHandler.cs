using UnityEngine;

/// <summary>
/// Xử lý input cho pause menu
/// </summary>
public class PauseInputHandler : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    
    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            HandlePauseInput();
        }
    }

    private void HandlePauseInput()
    {
        // Kiểm tra nếu có UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.TogglePauseMenu();
        }
        else if (PauseMenu.Instance != null)
        {
            // Fallback trực tiếp tới PauseMenu
            PauseMenu.Instance.TogglePause();
        }
        else
        {
            Debug.LogWarning("? Không tìm thấy UIManager hoặc PauseMenu để toggle pause!");
        }
    }

    // Context menu để test
    [ContextMenu("Test Pause Toggle")]
    public void TestPauseToggle()
    {
        HandlePauseInput();
    }
}