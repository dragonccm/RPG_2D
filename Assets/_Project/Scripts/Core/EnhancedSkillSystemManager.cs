using UnityEngine;

/// <summary>
/// Enhanced Skill System Manager
/// Qu?n lý h? th?ng k? n?ng nâng cao v?i damage zone và cursor management
/// </summary>
public class EnhancedSkillSystemManager : MonoBehaviour
{
    [Header("Skill Settings")]
    [SerializeField] private SkillModule[] availableSkills;
    [SerializeField] private Transform skillZoneParent; // Parent cho các damage zone
    
    [Header("Damage Zone Prefabs")]
    [Tooltip("Optional custom damage zone prefab - if null, will auto-generate")]
    [SerializeField] private GameObject damageZonePrefab;
    
    [Header("Cursor Settings")]
    [SerializeField] private Texture2D normalCursor;
    [SerializeField] private Texture2D attackCursor;
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
    
    [Header("Visual Settings")]
    [SerializeField] private bool showSkillPreview = true;
    [SerializeField] private bool showRangeIndicator = true;
    [SerializeField] private LayerMask enemyLayerMask = 1 << 8; // Default Enemy layer
    
    // Private components
    private Character playerCharacter;
    private Camera mainCamera;
    private GameObject currentRangeIndicator;
    private GameObject currentDirectionIndicator;
    
    // Current skill data
    private SkillModule currentSkill;
    private ISkillExecutor currentExecutor;
    private bool isAiming = false;

    #region Initialization
    
    void Awake()
    {
        playerCharacter = GetComponent<Character>();
        mainCamera = Camera.main;
        
        if (mainCamera == null)
            mainCamera = FindFirstObjectByType<Camera>();
            
        // Create parent for skill zones if not assigned
        if (skillZoneParent == null)
        {
            GameObject parent = new GameObject("SkillZones");
            skillZoneParent = parent.transform;
        }
        
        // Set default cursor
        SetNormalCursor();
    }
    
    void Start()
    {
        ValidateComponents();
    }
    
    #endregion

    #region Skill Execution
    
    /// <summary>
    /// Th?c thi skill v?i lo?i và v? trí ch? ??nh
    /// </summary>
    /// <param name="skillModule">Skill c?n th?c thi</param>
    /// <param name="targetPosition">V? trí m?c tiêu</param>
    public bool ExecuteSkill(SkillModule skillModule, Vector2 targetPosition)
    {
        if (skillModule == null || playerCharacter == null)
        {
            Debug.LogWarning("?? Cannot execute skill: missing components");
            return false;
        }
        
        // Ki?m tra ?i?u ki?n th?c thi
        if (!skillModule.CanExecute(playerCharacter))
        {
            Debug.LogWarning($"?? Cannot execute {skillModule.skillName}: conditions not met");
            return false;
        }
        
        // T?o executor và th?c thi
        var executor = skillModule.CreateExecutor();
        if (executor != null)
        {
            Debug.Log($"?? Executing skill: {skillModule.skillName} at {targetPosition}");
            
            // Th?c thi skill
            executor.Execute(playerCharacter, targetPosition);
            
            // Hi?n th? damage zone theo lo?i skill
            ShowSkillDamageZone(skillModule, targetPosition);
            
            return true;
        }
        
        Debug.LogError($"? Failed to create executor for {skillModule.skillName}");
        return false;
    }
    
    /// <summary>
    /// B?t ??u aiming cho skill
    /// </summary>
    /// <param name="skillModule">Skill c?n aim</param>
    public void StartAiming(SkillModule skillModule)
    {
        if (skillModule == null) return;
        
        currentSkill = skillModule;
        currentExecutor = skillModule.CreateExecutor();
        isAiming = true;
        
        Debug.Log($"?? Started aiming with {skillModule.skillName}");
        
        // Hi?n th? range indicator cho Projectile và AoE
        if (skillModule.skillType == SkillType.Projectile || skillModule.skillType == SkillType.Area)
        {
            ShowRangeIndicator(skillModule.range);
        }
        
        // Thay ??i cursor
        SetAttackCursor();
    }
    
    /// <summary>
    /// K?t thúc aiming
    /// </summary>
    public void StopAiming()
    {
        isAiming = false;
        currentSkill = null;
        currentExecutor = null;
        
        HideRangeIndicator();
        HideDirectionIndicator();
        SetNormalCursor();
        
        Debug.Log("?? Stopped aiming");
    }
    
    #endregion

    #region Damage Zone Management
    
    /// <summary>
    /// Hi?n th? damage zone theo lo?i skill
    /// </summary>
    /// <param name="skillModule">Skill module</param>
    /// <param name="position">V? trí hi?n th?</param>
    private void ShowSkillDamageZone(SkillModule skillModule, Vector2 position)
    {
        if (!skillModule.showDamageArea) return;
        
        switch (skillModule.skillType)
        {
            case SkillType.Melee:
                ShowMeleeDamageZone(skillModule);
                break;
                
            case SkillType.Projectile:
                // Projectile không c?n damage zone t?i start position
                break;
                
            case SkillType.Area:
                ShowAreaDamageZone(skillModule, position);
                break;
                
            case SkillType.Support:
                ShowSupportEffect(skillModule);
                break;
        }
    }
    
    /// <summary>
    /// Hi?n th? damage zone cho Melee skills
    /// </summary>
    private void ShowMeleeDamageZone(SkillModule skillModule)
    {
        Vector2 playerPos = playerCharacter.transform.position;
        
        GameObject damageZone = CreateDamageZone(
            playerPos, 
            skillModule.range, 
            skillModule.damageAreaColor,
            $"MeleeDamageZone_{skillModule.skillName}"
        );
        
        // Auto destroy
        if (damageZone != null)
            Destroy(damageZone, skillModule.damageAreaDisplayTime);
    }
    
    /// <summary>
    /// Hi?n th? damage zone cho Area skills
    /// </summary>
    private void ShowAreaDamageZone(SkillModule skillModule, Vector2 position)
    {
        GameObject damageZone = CreateDamageZone(
            position, 
            skillModule.areaRadius, 
            skillModule.damageAreaColor,
            $"AreaDamageZone_{skillModule.skillName}"
        );
        
        // Auto destroy
        if (damageZone != null)
            Destroy(damageZone, skillModule.damageAreaDisplayTime);
    }
    
    /// <summary>
    /// Hi?n th? effect cho Support skills
    /// </summary>
    private void ShowSupportEffect(SkillModule skillModule)
    {
        Vector2 playerPos = playerCharacter.transform.position;
        
        // T?o effect ??c bi?t cho support skills
        GameObject supportEffect = CreateSupportEffect(
            playerPos,
            skillModule.skillColor,
            $"SupportEffect_{skillModule.skillName}"
        );
        
        // Auto destroy
        if (supportEffect != null)
            Destroy(supportEffect, skillModule.damageAreaDisplayTime);
    }
    
    /// <summary>
    /// T?o damage zone object
    /// </summary>
    private GameObject CreateDamageZone(Vector2 position, float radius, Color color, string name)
    {
        GameObject zone;
        
        if (damageZonePrefab != null)
        {
            // S? d?ng custom prefab
            zone = Instantiate(damageZonePrefab, skillZoneParent);
        }
        else
        {
            // T?o auto-generated zone
            zone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            zone.transform.SetParent(skillZoneParent);
            
            // Remove collider
            var collider = zone.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }
        
        // Thi?t l?p zone
        zone.name = name;
        zone.transform.position = new Vector3(position.x, position.y, 0);
        zone.transform.localScale = Vector3.one * radius * 2;
        
        // Thi?t l?p material
        var renderer = zone.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = CreateTransparentMaterial(color);
            renderer.material = material;
        }
        
        Debug.Log($"?? Created damage zone: {name} at {position} with radius {radius}");
        
        return zone;
    }
    
    /// <summary>
    /// T?o support effect
    /// </summary>
    private GameObject CreateSupportEffect(Vector2 position, Color color, string name)
    {
        GameObject effect = new GameObject(name);
        effect.transform.SetParent(skillZoneParent);
        effect.transform.position = new Vector3(position.x, position.y, 0);
        
        // Thêm particle system ho?c visual effect
        var particleSystem = effect.AddComponent<ParticleSystem>();
        var main = particleSystem.main;
        main.startColor = color;
        main.startLifetime = 2f;
        main.startSpeed = 1f;
        main.maxParticles = 20;
        
        Debug.Log($"? Created support effect: {name} at {position}");
        
        return effect;
    }
    
    #endregion

    #region Range & Direction Indicators
    
    /// <summary>
    /// Hi?n th? range indicator cho Projectile và AoE
    /// </summary>
    private void ShowRangeIndicator(float range)
    {
        if (!showRangeIndicator) return;
        
        HideRangeIndicator();
        
        currentRangeIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        currentRangeIndicator.name = "RangeIndicator";
        
        // Remove collider
        var collider = currentRangeIndicator.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        
        // Thi?t l?p position và scale
        currentRangeIndicator.transform.position = playerCharacter.transform.position;
        currentRangeIndicator.transform.localScale = Vector3.one * range * 2;
        
        // Thi?t l?p material
        var renderer = currentRangeIndicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = CreateTransparentMaterial(new Color(0, 1, 1, 0.2f)); // Cyan transparent
            renderer.material = material;
        }
    }
    
    /// <summary>
    /// ?n range indicator
    /// </summary>
    private void HideRangeIndicator()
    {
        if (currentRangeIndicator != null)
        {
            Destroy(currentRangeIndicator);
            currentRangeIndicator = null;
        }
    }
    
    /// <summary>
    /// Hi?n th? direction indicator cho Projectile
    /// </summary>
    private void ShowDirectionIndicator(Vector2 direction, float length)
    {
        if (!showSkillPreview) return;
        
        HideDirectionIndicator();
        
        // T?o line renderer cho direction
        currentDirectionIndicator = new GameObject("DirectionIndicator");
        var lineRenderer = currentDirectionIndicator.AddComponent<LineRenderer>();
        
        // Thi?t l?p line renderer
        lineRenderer.material = CreateLineMaterial(Color.yellow);
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.positionCount = 2;
        
        Vector3 startPos = playerCharacter.transform.position;
        Vector3 endPos = startPos + (Vector3)(direction.normalized * length);
        
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }
    
    /// <summary>
    /// ?n direction indicator
    /// </summary>
    private void HideDirectionIndicator()
    {
        if (currentDirectionIndicator != null)
        {
            Destroy(currentDirectionIndicator);
            currentDirectionIndicator = null;
        }
    }
    
    #endregion

    #region Cursor Management
    
    /// <summary>
    /// Thi?t l?p cursor bình th??ng
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
    }
    
    /// <summary>
    /// Thi?t l?p cursor t?n công
    /// </summary>
    public void SetAttackCursor()
    {
        if (attackCursor != null)
        {
            Cursor.SetCursor(attackCursor, cursorHotspot, CursorMode.Auto);
        }
    }
    
    #endregion

    #region Update & Input Handling
    
    void Update()
    {
        HandleCursorUpdate();
        HandleAimingUpdate();
    }
    
    /// <summary>
    /// C?p nh?t cursor d?a trên ??i t??ng hover
    /// </summary>
    private void HandleCursorUpdate()
    {
        if (isAiming) return; // ?ang aim thì không ??i cursor
        
        bool hoveredEnemy = IsHoveringAttackableTarget();
        
        if (hoveredEnemy)
            SetAttackCursor();
        else
            SetNormalCursor();
    }
    
    /// <summary>
    /// C?p nh?t aiming system
    /// </summary>
    private void HandleAimingUpdate()
    {
        if (!isAiming || currentSkill == null) return;
        
        Vector2 mousePos = GetMouseWorldPosition();
        Vector2 playerPos = playerCharacter.transform.position;
        Vector2 direction = (mousePos - playerPos).normalized;
        
        // C?p nh?t range indicator position
        if (currentRangeIndicator != null)
        {
            currentRangeIndicator.transform.position = playerPos;
        }
        
        // Hi?n th? direction indicator cho Projectile
        if (currentSkill.skillType == SkillType.Projectile)
        {
            ShowDirectionIndicator(direction, currentSkill.range);
        }
        
        // Th?c thi skill khi click
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 targetPos = GetValidTargetPosition(mousePos, playerPos);
            ExecuteSkill(currentSkill, targetPos);
            StopAiming();
        }
        
        // H?y aiming khi right click
        if (Input.GetMouseButtonDown(1))
        {
            StopAiming();
        }
    }
    
    #endregion

    #region Utility Methods
    
    /// <summary>
    /// Ki?m tra có ?ang hover enemy không
    /// </summary>
    private bool IsHoveringAttackableTarget()
    {
        Vector2 mousePos = GetMouseWorldPosition();
        
        // Ki?m tra IAttackable objects
        var attackableObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var obj in attackableObjects)
        {
            if (obj is IAttackable attackable && attackable.CanBeAttacked())
            {
                float distance = Vector2.Distance(mousePos, attackable.GetPosition());
                if (distance < 1f) // Hover range
                {
                    return true;
                }
            }
        }
        
        // Ki?m tra objects trong Enemy layer
        Collider2D hitCollider = Physics2D.OverlapPoint(mousePos, enemyLayerMask);
        if (hitCollider != null)
        {
            var character = hitCollider.GetComponent<Character>();
            if (character != null && character.GetComponent<PlayerController>() == null)
            {
                return true; // Là enemy
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// L?y v? trí chu?t trong world space
    /// </summary>
    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = mainCamera.nearClipPlane;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        return new Vector2(mouseWorldPos.x, mouseWorldPos.y);
    }
    
    /// <summary>
    /// L?y target position h?p l? trong ph?m vi skill
    /// </summary>
    private Vector2 GetValidTargetPosition(Vector2 mousePos, Vector2 playerPos)
    {
        Vector2 direction = (mousePos - playerPos).normalized;
        float distance = Vector2.Distance(mousePos, playerPos);
        float maxRange = currentSkill.range;
        
        if (distance <= maxRange)
        {
            return mousePos;
        }
        else
        {
            return playerPos + direction * maxRange;
        }
    }
    
    /// <summary>
    /// T?o transparent material
    /// </summary>
    private Material CreateTransparentMaterial(Color color)
    {
        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        material.SetFloat("_Mode", 3); // Transparent mode
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        return material;
    }
    
    /// <summary>
    /// T?o line material
    /// </summary>
    private Material CreateLineMaterial(Color color)
    {
        var material = new Material(Shader.Find("Sprites/Default"));
        material.color = color;
        return material;
    }
    
    /// <summary>
    /// Validate các components c?n thi?t
    /// </summary>
    private void ValidateComponents()
    {
        if (playerCharacter == null)
        {
            Debug.LogError("? EnhancedSkillSystemManager: Player Character component not found!");
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("? EnhancedSkillSystemManager: Main Camera not found!");
        }
        
        if (normalCursor == null)
        {
            Debug.LogWarning("?? EnhancedSkillSystemManager: Normal cursor texture not assigned!");
        }
        
        if (attackCursor == null)
        {
            Debug.LogWarning("?? EnhancedSkillSystemManager: Attack cursor texture not assigned!");
        }
    }
    
    #endregion

    #region Public API
    
    /// <summary>
    /// Thi?t l?p cursor textures t? code
    /// </summary>
    public void SetCursorTextures(Texture2D normal, Texture2D attack)
    {
        normalCursor = normal;
        attackCursor = attack;
        SetNormalCursor();
    }
    
    /// <summary>
    /// Thi?t l?p damage zone prefab
    /// </summary>
    public void SetDamageZonePrefab(GameObject prefab)
    {
        damageZonePrefab = prefab;
    }
    
    /// <summary>
    /// L?y skill hi?n t?i ?ang aim
    /// </summary>
    public SkillModule GetCurrentSkill()
    {
        return currentSkill;
    }
    
    /// <summary>
    /// Ki?m tra có ?ang aiming không
    /// </summary>
    public bool IsAiming()
    {
        return isAiming;
    }
    
    #endregion
}