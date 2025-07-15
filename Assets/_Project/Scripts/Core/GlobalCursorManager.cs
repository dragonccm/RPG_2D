using UnityEngine;

/// <summary>
/// Global Cursor Manager
/// Qu?n lý cursor system toàn c?c cho game
/// </summary>
public class GlobalCursorManager : MonoBehaviour
{
    [Header("Cursor Textures")]
    [SerializeField] private Texture2D normalCursor;
    [SerializeField] private Texture2D attackCursor;
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
    
    [Header("Detection Settings")]
    [SerializeField] private LayerMask enemyLayerMask = 1 << 8; // Default Enemy layer
    [SerializeField] private float detectionRadius = 1f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Singleton pattern
    public static GlobalCursorManager Instance { get; private set; }
    
    // Private variables
    private Camera mainCamera;
    private bool isAiming = false;
    private bool hoveredAttackableTarget = false;
    
    #region Singleton & Initialization
    
    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Initialize()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindFirstObjectByType<Camera>();
            
        SetNormalCursor();
        
        Debug.Log("??? GlobalCursorManager initialized");
    }
    
    #endregion
    
    #region Update Loop
    
    void Update()
    {
        if (!isAiming)
        {
            HandleHoverDetection();
        }
    }
    
    /// <summary>
    /// Detect attackable targets under cursor
    /// </summary>
    private void HandleHoverDetection()
    {
        bool currentlyHoveringAttackable = IsHoveringAttackableTarget();
        
        // Only change cursor if hover state changed
        if (currentlyHoveringAttackable != hoveredAttackableTarget)
        {
            hoveredAttackableTarget = currentlyHoveringAttackable;
            
            if (hoveredAttackableTarget)
            {
                SetAttackCursor();
                if (showDebugInfo)
                    Debug.Log("?? Hovering attackable target - switched to attack cursor");
            }
            else
            {
                SetNormalCursor();
                if (showDebugInfo)
                    Debug.Log("??? No attackable target - switched to normal cursor");
            }
        }
    }
    
    /// <summary>
    /// Check if cursor is hovering over attackable target
    /// </summary>
    private bool IsHoveringAttackableTarget()
    {
        Vector2 mousePos = GetMouseWorldPosition();
        
        // Method 1: Check IAttackable objects
        var attackableObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var obj in attackableObjects)
        {
            if (obj is IAttackable attackable && attackable.CanBeAttacked())
            {
                float distance = Vector2.Distance(mousePos, attackable.GetPosition());
                if (distance <= detectionRadius)
                {
                    if (showDebugInfo)
                        Debug.Log($"?? Found IAttackable: {attackable.GetName()} at distance {distance:F2}");
                    return true;
                }
            }
        }
        
        // Method 2: Check objects in Enemy layer
        Collider2D hitCollider = Physics2D.OverlapPoint(mousePos, enemyLayerMask);
        if (hitCollider != null)
        {
            var character = hitCollider.GetComponent<Character>();
            if (character != null)
            {
                // Check if it's not a player by looking for PlayerController component
                var playerController = character.GetComponent<MonoBehaviour>();
                bool isPlayer = false;
                if (playerController != null && playerController.GetType().Name == "PlayerController")
                {
                    isPlayer = true;
                }
                
                if (!isPlayer && (character.health == null || character.health.currentValue > 0))
                {
                    if (showDebugInfo)
                        Debug.Log($"?? Found enemy in layer: {character.name}");
                    return true;
                }
            }
        }
        
        return false;
    }
    
    #endregion
    
    #region Cursor Management
    
    /// <summary>
    /// Set normal cursor
    /// </summary>
    public void SetNormalCursor()
    {
        if (normalCursor != null)
        {
            Cursor.SetCursor(normalCursor, cursorHotspot, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        
        if (showDebugInfo)
            Debug.Log("??? Set normal cursor");
    }
    
    /// <summary>
    /// Set attack cursor
    /// </summary>
    public void SetAttackCursor()
    {
        if (attackCursor != null)
        {
            Cursor.SetCursor(attackCursor, cursorHotspot, CursorMode.Auto);
            
            if (showDebugInfo)
                Debug.Log("?? Set attack cursor");
        }
        else
        {
            // Fallback to normal cursor if attack cursor not set
            SetNormalCursor();
            Debug.LogWarning("?? Attack cursor texture not assigned, using normal cursor");
        }
    }
    
    /// <summary>
    /// Force set aiming mode
    /// </summary>
    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
        
        if (aiming)
        {
            SetAttackCursor();
            if (showDebugInfo)
                Debug.Log("?? Entered aiming mode");
        }
        else
        {
            // Return to hover detection
            hoveredAttackableTarget = false; // Reset to force update
            if (showDebugInfo)
                Debug.Log("??? Exited aiming mode");
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Set cursor textures from external scripts
    /// </summary>
    public void SetCursorTextures(Texture2D normal, Texture2D attack, Vector2 hotspot = default)
    {
        normalCursor = normal;
        attackCursor = attack;
        if (hotspot != default)
            cursorHotspot = hotspot;
            
        // Apply immediately
        if (!isAiming && !hoveredAttackableTarget)
            SetNormalCursor();
            
        Debug.Log("??? Cursor textures updated");
    }
    
    /// <summary>
    /// Get current mouse world position
    /// </summary>
    public Vector2 GetMouseWorldPosition()
    {
        if (mainCamera == null) return Vector2.zero;
        
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = mainCamera.nearClipPlane;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        return new Vector2(mouseWorldPos.x, mouseWorldPos.y);
    }
    
    /// <summary>
    /// Check if currently hovering attackable target
    /// </summary>
    public bool IsHoveringAttackable()
    {
        return hoveredAttackableTarget;
    }
    
    /// <summary>
    /// Check if currently in aiming mode
    /// </summary>
    public bool IsAiming()
    {
        return isAiming;
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Validate cursor textures and settings
    /// </summary>
    public bool ValidateCursorSettings()
    {
        bool isValid = true;
        
        if (normalCursor == null)
        {
            Debug.LogWarning("?? Normal cursor texture not assigned!");
            isValid = false;
        }
        
        if (attackCursor == null)
        {
            Debug.LogWarning("?? Attack cursor texture not assigned!");
            isValid = false;
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("? Main camera not found!");
            isValid = false;
        }
        
        return isValid;
    }
    
    /// <summary>
    /// Reset cursor system
    /// </summary>
    public void ResetCursorSystem()
    {
        isAiming = false;
        hoveredAttackableTarget = false;
        SetNormalCursor();
        
        Debug.Log("?? Cursor system reset");
    }
    
    #endregion
    
    #region Editor Helpers
    
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            ValidateCursorSettings();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (showDebugInfo && Application.isPlaying)
        {
            Vector2 mousePos = GetMouseWorldPosition();
            
            // Draw detection radius
            Gizmos.color = hoveredAttackableTarget ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(mousePos, detectionRadius);
            
            // Draw mouse position
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(mousePos, 0.1f);
        }
    }
    
    #endregion
}