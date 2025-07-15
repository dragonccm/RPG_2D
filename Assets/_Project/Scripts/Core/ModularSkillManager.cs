/// <summary>
/// File: ModularSkillManager.cs
/// Author: Unity 2D RPG Refactoring Agent
/// Description: Core skill management system with modular architecture and hotkey support
/// </summary>

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ModularSkillManager : MonoBehaviour
{
    [Header("Skill Slots Configuration")]
    [SerializeField] private int maxSkillSlots = 8;
    [SerializeField] private int levelsPerSlot = 5;
    
    [Header("Available Skills")]
    public List<SkillModule> availableSkills = new List<SkillModule>();
    
    [Header("Legacy System Settings")]
    [SerializeField] private bool enableLegacyHotkeys = false;
    [SerializeField] private List<SkillSlot> skillSlots = new List<SkillSlot>();
    
    private Character player;
    private Dictionary<ISkillExecutor, float> cooldownTimers = new Dictionary<ISkillExecutor, float>();
    
    // Events
    public System.Action<int> OnSlotUnlocked;
    public System.Action<int, SkillModule> OnSkillEquipped;
    public System.Action<int> OnSkillUnequipped;

    private bool isSkillActive = false;
    private bool isSkillHeld = false;
    private int activeSkillSlot = -1;
    private GameObject currentPreviewDamageArea = null;
    private GameObject currentProjectileDirectionLine = null;

    private void Awake()
    {
        player = GetComponent<Character>();
        InitializeSkillSlots();
    }

    private void Start()
    {
        UpdateUnlockedSlots();
    }

    private void Update()
    {
        UpdateCooldowns();
        
        if (enableLegacyHotkeys)
        {
            HandleSkillInput();
        }
    }

    private void InitializeSkillSlots()
    {
        skillSlots.Clear();
        
        KeyCode[] hotkeys = GenerateDynamicHotkeys(maxSkillSlots);
        
        for (int i = 0; i < maxSkillSlots; i++)
        {
            var slot = new SkillSlot(i, hotkeys[i]);
            skillSlots.Add(slot);
        }
    }
    
    private KeyCode[] GenerateDynamicHotkeys(int slotCount)
    {
        KeyCode[] hotkeys = new KeyCode[slotCount];
        
        KeyCode[] numberKeys = { 
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
        };
        
        KeyCode[] functionKeys = {
            KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6,
            KeyCode.F7, KeyCode.F8, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12
        };
        
        KeyCode[] letterKeys = {
            KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T,
            KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P
        };
        
        KeyCode[] extraLetterKeys = {
            KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G,
            KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L
        };
        
        KeyCode[] mouseKeys = {
            KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse5, KeyCode.Mouse6
        };
        
        System.Collections.Generic.List<KeyCode> allAvailableKeys = new System.Collections.Generic.List<KeyCode>();
        allAvailableKeys.AddRange(numberKeys);
        allAvailableKeys.AddRange(functionKeys);
        allAvailableKeys.AddRange(letterKeys);
        allAvailableKeys.AddRange(extraLetterKeys);
        allAvailableKeys.AddRange(mouseKeys);
        
        for (int i = 0; i < slotCount; i++)
        {
            if (i < allAvailableKeys.Count)
            {
                hotkeys[i] = allAvailableKeys[i];
            }
            else
            {
                hotkeys[i] = KeyCode.None;
            }
        }
        
        return hotkeys;
    }

    private void UpdateCooldowns()
    {
        var cooldownList = cooldownTimers.ToList();
        foreach (var pair in cooldownList)
        {
            cooldownTimers[pair.Key] = pair.Value - Time.deltaTime;
            if (cooldownTimers[pair.Key] <= 0)
            {
                cooldownTimers.Remove(pair.Key);
            }
        }
    }

    private void HandleSkillInput()
    {
        if (!enableLegacyHotkeys) return;

        for (int i = 0; i < skillSlots.Count; i++)
        {
            var slot = skillSlots[i];
            if (!slot.isUnlocked || !slot.HasSkill()) continue;

            if (Input.GetKeyDown(slot.hotkey))
            {
                // Phase 1: Start skill
                StartSkillPreview(i);
            }

            if (Input.GetKey(slot.hotkey))
            {
                // Phase 2: Hold skill
                HoldSkillPreview(i);
            }

            if (Input.GetKeyUp(slot.hotkey))
            {
                // Phase 3: Release skill
                ActivateSkill(i);
                EndSkillPreview(i);
            }
        }
    }

    private void StartSkillPreview(int slotIndex)
    {
        if (isSkillActive || slotIndex >= skillSlots.Count) return;

        var slot = skillSlots[slotIndex];
        if (!slot.isUnlocked || !slot.HasSkill()) return;

        var skill = slot.equippedSkill;
        if (skill == null || !skill.showDamageArea) return;

        // Hi?n th? vùng sát th??ng preview
        ShowSkillPreview(skill);

        isSkillActive = true;
        activeSkillSlot = slotIndex;
    }

    private void HoldSkillPreview(int slotIndex)
    {
        if (!isSkillActive || slotIndex != activeSkillSlot) return;

        var slot = skillSlots[slotIndex];
        if (!slot.isUnlocked || !slot.HasSkill()) return;

        var skill = slot.equippedSkill;
        if (skill == null || !skill.showDamageArea) return;

        // C?p nh?t vùng sát th??ng preview n?u c?n
        UpdateSkillPreview(skill);

        isSkillHeld = true;
    }

    private void EndSkillPreview(int slotIndex)
    {
        if (!isSkillActive || slotIndex != activeSkillSlot) return;

        // ?n vùng sát th??ng preview
        HideSkillPreview();

        isSkillActive = false;
        isSkillHeld = false;
        activeSkillSlot = -1;
    }

    private void ShowSkillPreview(SkillModule skill)
    {
        Vector2 playerPosition = transform.position;
        
        // C?p nh?t v? trí và kích th??c d?a trên lo?i k? n?ng
        switch (skill.skillType)
        {
            case SkillType.Melee:
                ShowMeleePreview(skill, playerPosition);
                break;
            case SkillType.Area:
                ShowAreaPreview(skill, playerPosition);
                break;
            case SkillType.Projectile:
                ShowProjectilePreview(skill, playerPosition);
                break;
            default:
                // Support không hi?n th? preview
                HideAllPreviews();
                return;
        }
    }

    private void ShowMeleePreview(SkillModule skill, Vector2 playerPosition)
    {
        // ?u tiên s? d?ng damageZonePrefab t? SkillModule
        if (skill.damageZonePrefab != null)
        {
            // S? d?ng custom prefab cho Melee
            if (currentPreviewDamageArea == null)
            {
                currentPreviewDamageArea = Object.Instantiate(skill.damageZonePrefab);
                currentPreviewDamageArea.name = "MeleePreviewArea_Custom";
            }
            
            currentPreviewDamageArea.transform.position = new Vector3(playerPosition.x, playerPosition.y, 0);
            currentPreviewDamageArea.transform.localScale = Vector3.one * skill.range * 2;
        }
        else
        {
            // Fallback: T?o vùng sát th??ng preview m?c ??nh cho Melee
            if (currentPreviewDamageArea == null)
            {
                currentPreviewDamageArea = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                currentPreviewDamageArea.name = "MeleePreviewArea";
                
                // Remove collider
                var collider = currentPreviewDamageArea.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }
            }

            currentPreviewDamageArea.transform.position = new Vector3(playerPosition.x, playerPosition.y, 0);
            currentPreviewDamageArea.transform.localScale = Vector3.one * skill.range * 2;
            
            SetPreviewMaterial(currentPreviewDamageArea, skill.damageAreaColor);
        }

        currentPreviewDamageArea.SetActive(true);
    }

    private void ShowAreaPreview(SkillModule skill, Vector2 playerPosition)
    {
        Vector2 mousePos = GetMouseWorldPosition();
        Vector2 validPos = GetValidTargetPosition(mousePos, playerPosition, skill);
        
        // ?u tiên s? d?ng damageZonePrefab t? SkillModule
        if (skill.damageZonePrefab != null)
        {
            // S? d?ng custom prefab cho Area
            if (currentPreviewDamageArea == null)
            {
                currentPreviewDamageArea = Object.Instantiate(skill.damageZonePrefab);
                currentPreviewDamageArea.name = "AreaPreviewArea_Custom";
            }
            
            currentPreviewDamageArea.transform.position = new Vector3(validPos.x, validPos.y, 0);
            currentPreviewDamageArea.transform.localScale = Vector3.one * skill.areaRadius * 2;
        }
        else
        {
            // Fallback: T?o vùng sát th??ng preview m?c ??nh cho Area
            if (currentPreviewDamageArea == null)
            {
                currentPreviewDamageArea = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                currentPreviewDamageArea.name = "AreaPreviewArea";
                
                // Remove collider
                var collider = currentPreviewDamageArea.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }
            }

            currentPreviewDamageArea.transform.position = new Vector3(validPos.x, validPos.y, 0);
            currentPreviewDamageArea.transform.localScale = Vector3.one * skill.areaRadius * 2;
            
            SetPreviewMaterial(currentPreviewDamageArea, skill.damageAreaColor);
        }

        currentPreviewDamageArea.SetActive(true);
    }

    private void ShowProjectilePreview(SkillModule skill, Vector2 playerPosition)
    {
        Vector2 mousePos = GetMouseWorldPosition();
        Vector2 direction = (mousePos - playerPosition).normalized;
        
        // ?u tiên s? d?ng damageZonePrefab thay vì LineRenderer cho Projectile
        if (skill.damageZonePrefab != null)
        {
            // S? d?ng custom prefab cho Projectile direction indicator
            if (currentProjectileDirectionLine == null)
            {
                currentProjectileDirectionLine = Object.Instantiate(skill.damageZonePrefab);
                currentProjectileDirectionLine.name = "ProjectileDirectionPrefab";
            }
            
            // ??t v? trí và h??ng cho custom prefab
            Vector3 startPos = new Vector3(playerPosition.x, playerPosition.y, 0);
            Vector3 endPos = startPos + (Vector3)(direction * skill.range);
            Vector3 midPos = (startPos + endPos) / 2f;
            
            currentProjectileDirectionLine.transform.position = midPos;
            
            // Xoay prefab theo h??ng
            if (direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                currentProjectileDirectionLine.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
            
            // Scale theo range
            float rangeScale = skill.range / 2f;
            currentProjectileDirectionLine.transform.localScale = new Vector3(rangeScale, 0.2f, 1f);
        }
        else
        {
            // Fallback: T?o ???ng th?ng LineRenderer nh? c?
            if (currentProjectileDirectionLine == null)
            {
                currentProjectileDirectionLine = new GameObject("ProjectileDirectionLine");
                var lineRendererComponent = currentProjectileDirectionLine.AddComponent<LineRenderer>();
                
                // Thi?t l?p LineRenderer
                lineRendererComponent.material = CreateLineMaterial(skill.skillColor);
                lineRendererComponent.startWidth = 0.1f;
                lineRendererComponent.endWidth = 0.05f;
                lineRendererComponent.positionCount = 2;
                lineRendererComponent.useWorldSpace = true;
            }

            var lineRenderer = currentProjectileDirectionLine.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                Vector3 startPos = new Vector3(playerPosition.x, playerPosition.y, 0);
                Vector3 endPos = startPos + (Vector3)(direction * skill.range);
                
                lineRenderer.SetPosition(0, startPos);
                lineRenderer.SetPosition(1, endPos);
                lineRenderer.material.color = skill.skillColor;
            }
        }

        currentProjectileDirectionLine.SetActive(true);
        
        // ?n damage area cho Projectile vì chúng ta ch? hi?n th? ???ng bay
        if (currentPreviewDamageArea != null)
        {
            currentPreviewDamageArea.SetActive(false);
        }
    }

    private void SetPreviewMaterial(GameObject previewObject, Color skillColor)
    {
        var renderer = previewObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            var previewColor = skillColor;
            previewColor.a = 0.15f; // Alpha th?p h?n cho preview
            material.color = previewColor;
            material.SetFloat("_Mode", 3); // Transparent mode
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            renderer.material = material;
        }
    }

    private Material CreateLineMaterial(Color color)
    {
        var material = new Material(Shader.Find("Sprites/Default"));
        material.color = color;
        return material;
    }

    private void HideAllPreviews()
    {
        if (currentPreviewDamageArea != null)
        {
            currentPreviewDamageArea.SetActive(false);
        }
        
        if (currentProjectileDirectionLine != null)
        {
            currentProjectileDirectionLine.SetActive(false);
        }
    }

    private void UpdateSkillPreview(SkillModule skill)
    {
        Vector2 playerPosition = transform.position;

        switch (skill.skillType)
        {
            case SkillType.Area:
                // C?p nh?t v? trí Area preview khi chu?t di chuy?n
                if (currentPreviewDamageArea != null)
                {
                    Vector2 mousePos = GetMouseWorldPosition();
                    Vector2 validPos = GetValidTargetPosition(mousePos, playerPosition, skill);
                    currentPreviewDamageArea.transform.position = new Vector3(validPos.x, validPos.y, 0);
                }
                break;
                
            case SkillType.Projectile:
                // C?p nh?t h??ng bay Projectile khi chu?t di chuy?n
                if (currentProjectileDirectionLine != null)
                {
                    Vector2 mousePos = GetMouseWorldPosition();
                    Vector2 direction = (mousePos - playerPosition).normalized;
                    
                    if (skill.damageZonePrefab != null)
                    {
                        // C?p nh?t custom prefab cho Projectile
                        Vector3 startPos = new Vector3(playerPosition.x, playerPosition.y, 0);
                        Vector3 endPos = startPos + (Vector3)(direction * skill.range);
                        Vector3 midPos = (startPos + endPos) / 2f;
                        
                        currentProjectileDirectionLine.transform.position = midPos;
                        
                        // Xoay prefab theo h??ng
                        if (direction != Vector2.zero)
                        {
                            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                            currentProjectileDirectionLine.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                        }
                    }
                    else
                    {
                        // C?p nh?t LineRenderer fallback
                        var lineRenderer = currentProjectileDirectionLine.GetComponent<LineRenderer>();
                        if (lineRenderer != null)
                        {
                            Vector3 startPos = new Vector3(playerPosition.x, playerPosition.y, 0);
                            Vector3 endPos = startPos + (Vector3)(direction * skill.range);
                            
                            lineRenderer.SetPosition(0, startPos);
                            lineRenderer.SetPosition(1, endPos);
                        }
                    }
                }
                break;
        }
    }

    private void HideSkillPreview()
    {
        HideAllPreviews();
    }

    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 10f;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        return new Vector2(mouseWorldPos.x, mouseWorldPos.y);
    }

    private Vector2 GetValidTargetPosition(Vector2 mousePos, Vector2 playerPos, SkillModule skill)
    {
        Vector2 direction = (mousePos - playerPos).normalized;
        float distance = Vector2.Distance(mousePos, playerPos);
        float maxRange = skill.range;

        if (distance <= maxRange)
        {
            return mousePos;
        }
        else
        {
            return playerPos + direction * maxRange;
        }
    }

    public void ActivateSkill(int slotIndex)
    {
        if (slotIndex >= skillSlots.Count) 
        {
            return;
        }
        
        var slot = skillSlots[slotIndex];
        if (!slot.isUnlocked || !slot.HasSkill()) 
        {
            return;
        }
        
        var executor = slot.executor;
        
        if (executor == null)
        {
            return;
        }
        
        if (cooldownTimers.ContainsKey(executor)) 
        {
            return;
        }
        
        if (!executor.CanExecute(player)) 
        {
            return;
        }
        
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 10f;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 targetPos = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
        
        executor.Execute(player, targetPos);
        cooldownTimers[executor] = executor.GetCooldown();
    }

    public bool EquipSkill(int slotIndex, SkillModule skill)
    {
        if (slotIndex >= skillSlots.Count) 
        {
            return false;
        }
        
        if (skill == null)
        {
            return false;
        }
        
        var slot = skillSlots[slotIndex];
        if (!slot.isUnlocked) 
        {
            return false;
        }
        
        if (GetPlayerLevel() < skill.requiredLevel)
        {
            return false;
        }

        // Remove reference to UnifiedSkillSystem as it's been removed
        
        bool success = slot.EquipSkill(skill);
        if (success)
        {
            OnSkillEquipped?.Invoke(slotIndex, skill);
        }
        
        return success;
    }

    public void UnequipSkill(int slotIndex)
    {
        if (slotIndex >= skillSlots.Count) return;
        
        var slot = skillSlots[slotIndex];
        var skillName = slot.HasSkill() ? slot.equippedSkill.skillName : "Unknown";
        slot.UnequipSkill();
        
        OnSkillUnequipped?.Invoke(slotIndex);
    }

    public void UpdateUnlockedSlots()
    {
        int playerLevel = GetPlayerLevel();
        int unlockedSlots = Mathf.Min((playerLevel / levelsPerSlot) + 1, maxSkillSlots);
        
        for (int i = 0; i < skillSlots.Count; i++)
        {
            bool wasUnlocked = skillSlots[i].isUnlocked;
            bool shouldBeUnlocked = i < unlockedSlots;
            
            if (!wasUnlocked && shouldBeUnlocked)
            {
                skillSlots[i].UnlockSlot();
                OnSlotUnlocked?.Invoke(i);
            }
        }
    }

    public int GetPlayerLevel()
    {
        return PlayerPrefs.GetInt("PlayerLevel", 1);
    }

    public void SetPlayerLevel(int level)
    {
        PlayerPrefs.SetInt("PlayerLevel", level);
        UpdateUnlockedSlots();
    }

    public List<SkillModule> GetAvailableSkills()
    {
        return availableSkills.Where(skill => GetPlayerLevel() >= skill.requiredLevel).ToList();
    }

    public List<SkillSlot> GetUnlockedSlots()
    {
        return skillSlots.Where(slot => slot.isUnlocked).ToList();
    }

    public SkillSlot GetSlot(int index)
    {
        if (index >= 0 && index < skillSlots.Count)
            return skillSlots[index];
        return null;
    }

    public float GetSkillCooldown(int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null || !slot.HasSkill()) return 0f;
        
        if (cooldownTimers.ContainsKey(slot.executor))
            return cooldownTimers[slot.executor];
        
        return 0f;
    }

    public bool IsSkillOnCooldown(int slotIndex)
    {
        return GetSkillCooldown(slotIndex) > 0f;
    }

    public bool IsSkillEquippedInLegacySystem(SkillModule skill)
    {
        if (skill == null) return false;
        
        return skillSlots.Any(slot => slot.HasSkill() && slot.equippedSkill == skill);
    }

    public int GetSkillSlotIndex(SkillModule skill)
    {
        if (skill == null) return -1;
        
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (skillSlots[i].HasSkill() && skillSlots[i].equippedSkill == skill)
                return i;
        }
        return -1;
    }

    public void SetLegacyHotkeysEnabled(bool enabled)
    {
        enableLegacyHotkeys = enabled;
    }

    public void AddAvailableSkill(SkillModule skill)
    {
        if (!availableSkills.Contains(skill))
        {
            availableSkills.Add(skill);
        }
    }

    public void RemoveAvailableSkill(SkillModule skill)
    {
        availableSkills.Remove(skill);
    }

    // Save/Load system
    [System.Serializable]
    public class SkillSaveData
    {
        public int[] equippedSkillIDs;
        public int playerLevel;
        public bool legacyHotkeysEnabled;
    }

    public void SaveSkillSetup()
    {
        var saveData = new SkillSaveData();
        saveData.playerLevel = GetPlayerLevel();
        saveData.legacyHotkeysEnabled = enableLegacyHotkeys;
        saveData.equippedSkillIDs = new int[skillSlots.Count];
        
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (skillSlots[i].HasSkill())
            {
                saveData.equippedSkillIDs[i] = availableSkills.IndexOf(skillSlots[i].equippedSkill);
            }
            else
            {
                saveData.equippedSkillIDs[i] = -1;
            }
        }
        
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString("SkillSetup", json);
    }

    public void LoadSkillSetup()
    {
        string json = PlayerPrefs.GetString("SkillSetup", "");
        if (string.IsNullOrEmpty(json)) return;
        
        var saveData = JsonUtility.FromJson<SkillSaveData>(json);
        SetPlayerLevel(saveData.playerLevel);
        SetLegacyHotkeysEnabled(saveData.legacyHotkeysEnabled);
        
        for (int i = 0; i < saveData.equippedSkillIDs.Length && i < skillSlots.Count; i++)
        {
            int skillID = saveData.equippedSkillIDs[i];
            if (skillID >= 0 && skillID < availableSkills.Count)
            {
                EquipSkill(i, availableSkills[skillID]);
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveSkillSetup();
        }
    }

    public bool UpdateSlotHotkey(int slotIndex, KeyCode newKey)
    {
        if (slotIndex < 0 || slotIndex >= skillSlots.Count)
        {
            return false;
        }
        
        var slot = skillSlots[slotIndex];
        KeyCode oldKey = slot.hotkey;
        
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (i != slotIndex && skillSlots[i].UsesHotkey(newKey))
            {
                return false;
            }
        }
        
        slot.UpdateHotkey(newKey);
        return true;
    }
    
    public SkillSlot GetSlotByHotkey(KeyCode key)
    {
        return skillSlots.FirstOrDefault(slot => slot.UsesHotkey(key));
    }
    
    public int GetSlotIndexByHotkey(KeyCode key)
    {
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (skillSlots[i].UsesHotkey(key))
                return i;
        }
        return -1;
    }
    
    public bool AssignSkillToHotkey(SkillModule skill, KeyCode key)
    {
        if (skill == null)
        {
            return false;
        }
        
        if (GetPlayerLevel() < skill.requiredLevel)
        {
            return false;
        }
        
        var existingSlot = GetSlotByHotkey(key);
        
        if (existingSlot != null && existingSlot.isUnlocked)
        {
            if (existingSlot.HasSkill())
            {
                existingSlot.UnequipSkill();
            }
            
            bool success = existingSlot.EquipSkill(skill);
            if (success)
            {
                OnSkillEquipped?.Invoke(existingSlot.slotIndex, skill);
            }
            return success;
        }
        
        var emptySlot = skillSlots.FirstOrDefault(s => s.isUnlocked && !s.HasSkill());
        if (emptySlot != null)
        {
            emptySlot.UpdateHotkey(key);
            bool success = emptySlot.EquipSkill(skill);
            if (success)
            {
                OnSkillEquipped?.Invoke(emptySlot.slotIndex, skill);
            }
            return success;
        }
        
        return false;
    }
    
    public int GetMaxSupportedSlots()
    {
        return 45;
    }
    
    public string GetHotkeyDisplayName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Alpha1: return "1";
            case KeyCode.Alpha2: return "2";
            case KeyCode.Alpha3: return "3";
            case KeyCode.Alpha4: return "4";
            case KeyCode.Alpha5: return "5";
            case KeyCode.Alpha6: return "6";
            case KeyCode.Alpha7: return "7";
            case KeyCode.Alpha8: return "8";
            case KeyCode.Alpha9: return "9";
            case KeyCode.Alpha0: return "0";
            case KeyCode.F1: return "F1";
            case KeyCode.F2: return "F2";
            case KeyCode.F3: return "F3";
            case KeyCode.F4: return "F4";
            case KeyCode.F5: return "F5";
            case KeyCode.F6: return "F6";
            case KeyCode.F7: return "F7";
            case KeyCode.F8: return "F8";
            case KeyCode.F9: return "F9";
            case KeyCode.F10: return "F10";
            case KeyCode.F11: return "F11";
            case KeyCode.F12: return "F12";
            case KeyCode.Mouse3: return "M1";
            case KeyCode.Mouse4: return "M2";
            case KeyCode.Mouse5: return "M3";
            case KeyCode.Mouse6: return "M4";
            case KeyCode.None: return "---";
            default: return key.ToString();
        }
    }
}