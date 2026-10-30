/// <summary>
/// File: ModularSkillManager.cs
/// Author: Unity 2D RPG Refactoring Agent
/// Description: Core skill management system with modular architecture and hotkey support
/// </summary>

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public partial class ModularSkillManager : MonoBehaviour
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
        EnsureInitialized();
    }

    /// <summary>Dựng ô kỹ năng + đăng ký SkillBook (idempotent — test EditMode không chạy Awake/OnEnable).</summary>
    public void EnsureInitialized()
    {
        if (player == null) player = GetComponent<Character>();
        if (skillSlots.Count == 0) InitializeSkillSlots();
        SkillBook.Changed -= OnSkillBookChanged;
        SkillBook.Changed += OnSkillBookChanged;
    }

    private void Start()
    {
        UpdateUnlockedSlots();
    }

    private void OnEnable() { EnsureInitialized(); }
    private void OnDisable() { SkillBook.Changed -= OnSkillBookChanged; }

    /// <summary>Học kỹ năng mới → tự gắn vào ô trống đầu tiên đã mở khoá (người chơi đổi được sau).</summary>
    private void OnSkillBookChanged(string skillId)
    {
        if (skillId == null) return; // nạp save: ImportEquipped lo phần gắn ô
        var skill = FindSkill(skillId);
        if (skill == null || GetSlotIndexOfSkill(skill) >= 0) return;
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (skillSlots[i].isUnlocked && !skillSlots[i].HasSkill() && EquipSkill(i, skill)) return;
        }
    }

    /// <summary>Tra SkillModule theo id trong danh mục kỹ năng của nhân vật.</summary>
    public SkillModule FindSkill(string skillId)
    {
        foreach (var s in availableSkills)
            if (s != null && s.Id == skillId) return s;
        return null;
    }

    private int GetSlotIndexOfSkill(SkillModule skill)
    {
        for (int i = 0; i < skillSlots.Count; i++)
            if (skillSlots[i].HasSkill() && skillSlots[i].equippedSkill == skill) return i;
        return -1;
    }

    /// <summary>Danh sách "ô=skillId" để lưu — bền vững theo id, không theo thứ tự danh sách.</summary>
    public List<string> ExportEquipped()
    {
        var result = new List<string>();
        for (int i = 0; i < skillSlots.Count; i++)
            if (skillSlots[i].HasSkill()) result.Add($"{i}={skillSlots[i].equippedSkill.Id}");
        return result;
    }

    public void ImportEquipped(List<string> saved)
    {
        for (int i = 0; i < skillSlots.Count; i++)
            if (skillSlots[i].HasSkill()) UnequipSkill(i);
        if (saved == null) return;
        foreach (var entry in saved)
        {
            var kv = entry.Split('=');
            if (kv.Length != 2 || !int.TryParse(kv[0], out int slot)) continue;
            var skill = FindSkill(kv[1]);
            if (skill != null) EquipSkill(slot, skill);
        }
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
        // Enhanced cooldown system using SkillSlot's built-in cooldown management
        foreach (var slot in skillSlots)
        {
            if (slot.HasSkill())
            {
                slot.UpdateCooldown();
            }
        }
        
        // Legacy cooldown system (keep for backward compatibility)
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
        
        var skill = slot.equippedSkill;
        Vector2 targetPos = Vector2.zero;
        
        // Instant skills don't need mouse position
        if (skill.skillType != SkillType.Instant)
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = 10f;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            targetPos = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
        }
        else
        {
            // For instant skills, use player position as target
            targetPos = player.transform.position;
        }
        
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

        // Chưa học (cuộn / quest / boss) thì không gắn được
        if (!SkillBook.Has(skill))
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
        return availableSkills.Where(skill => SkillBook.Has(skill) && GetPlayerLevel() >= skill.requiredLevel).ToList();
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

}
