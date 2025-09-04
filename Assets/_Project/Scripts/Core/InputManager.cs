using UnityEngine;

/// <summary>
/// Centralized input management system
/// Eliminates duplicate input handling across multiple scripts
/// </summary>
public class InputManager : MonoBehaviour
{
    private static InputManager instance;
    public static InputManager Instance => instance;

    // Input action delegates
    public System.Action OnAttackPressed;
    public System.Action OnSkillPanelPressed;
    public System.Action OnPausePressed;
    public System.Action OnLevelUpPressed;
    public System.Action<KeyCode> OnSkillSlotPressed;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Attack input
        if (Input.GetKeyDown(KeyCode.J))
        {
            OnAttackPressed?.Invoke();
        }

        // Skill panel input
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            OnSkillPanelPressed?.Invoke();
        }

        // Pause input
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnPausePressed?.Invoke();
        }

        // Level up input
        if (Input.GetKeyDown(KeyCode.V))
        {
            OnLevelUpPressed?.Invoke();
        }

        // Skill slot inputs (1-8)
        for (int i = 0; i < 8; i++)
        {
            KeyCode key = KeyCode.Alpha1 + i;
            if (Input.GetKeyDown(key))
            {
                OnSkillSlotPressed?.Invoke(key);
            }
        }
    }

    /// <summary>
    /// Check if any menu is currently open
    /// </summary>
    public bool IsAnyMenuOpen()
    {
        var uiManager = ServiceLocator.Get<UIManager>();
        if (uiManager != null)
        {
            var skillPanel = ServiceLocator.Get<SkillPanelUI>();
            if (skillPanel != null && skillPanel.IsVisible())
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Get movement input vector
    /// </summary>
    public Vector2 GetMovementInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        return new Vector2(horizontal, vertical);
    }
}
