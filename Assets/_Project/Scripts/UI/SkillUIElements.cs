using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class SkillSlotUIElement : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Components")]
    [SerializeField] private Image skillIcon;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TextMeshProUGUI hotkeyText;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private GameObject lockedOverlay;
    
    [Header("Visual Settings")]
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color equippedColor = Color.green;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private GameObject levelRequirementIcon;
    
    private int slotIndex;
    private SkillPanelUI parentUI; // Changed from SkillSlotsUI to SkillPanelUI
    private SkillSlot currentSlot;

    public void Initialize(int index, SkillPanelUI parent) // Changed parameter type
    {
        slotIndex = index;
        parentUI = parent;
        
        // ?? FIXED: Dynamic hotkey text generation - SUPPORTS UNLIMITED SLOTS
        string hotkeyDisplayText = GetHotkeyDisplayText(index);
        
        if (hotkeyText != null)
        {
            hotkeyText.text = hotkeyDisplayText;
        }

        // Initialize UI components if they're null
        if (cooldownOverlay == null)
        {
            cooldownOverlay = transform.Find("CooldownOverlay")?.GetComponent<Image>();
        }

        if (cooldownText == null)
        {
            cooldownText = transform.Find("CooldownText")?.GetComponent<TextMeshProUGUI>();
        }

        if (skillIcon == null)
        {
            skillIcon = transform.Find("SkillIcon")?.GetComponent<Image>();
        }

        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (hotkeyText == null)
        {
            hotkeyText = transform.Find("HotkeyText")?.GetComponent<TextMeshProUGUI>();
        }

        if (lockedOverlay == null)
        {
            lockedOverlay = transform.Find("LockedOverlay")?.gameObject;
        }
    }
    
    /// <summary>
    /// Get appropriate display text for hotkey based on slot index - UNLIMITED SUPPORT
    /// </summary>
    private string GetHotkeyDisplayText(int index)
    {
        // Numbers 1-9, 0 (slots 0-9)
        if (index < 9)
        {
            return (index + 1).ToString(); // 1,2,3,4,5,6,7,8,9
        }
        else if (index == 9)
        {
            return "0"; // Slot 9 shows "0"
        }
        // Function keys F1-F12 (slots 10-21)
        else if (index >= 10 && index <= 21)
        {
            return $"F{index - 9}"; // F1,F2,F3...F12
        }
        // Letter keys (slots 22+)
        else if (index >= 22 && index <= 31)
        {
            char[] letters = {'Q','W','E','R','T','Y','U','I','O','P'};
            int letterIndex = index - 22;
            if (letterIndex < letters.Length)
            {
                return letters[letterIndex].ToString();
            }
        }
        // Additional letters (slots 32+)
        else if (index >= 32 && index <= 40)
        {
            char[] extraLetters = {'A','S','D','F','G','H','J','K','L'};
            int letterIndex = index - 32;
            if (letterIndex < extraLetters.Length)
            {
                return extraLetters[letterIndex].ToString();
            }
        }
        // Mouse buttons (slots 41+)
        else if (index >= 41 && index <= 44)
        {
            return $"M{index - 40}"; // M1,M2,M3,M4 (Mouse3-6)
        }
        
        // Fallback for slots beyond available keys
        return $"#{index + 1}";
    }

    public void UpdateSlot(SkillSlot slot)
    {
        currentSlot = slot;
        
        if (slot == null)
        {
            SetEmptyState();
            return;
        }

        if (!slot.isUnlocked)
        {
            SetLockedState();
        }
        else if (slot.HasSkill())
        {
            SetEquippedState(slot.equippedSkill);
        }
        else
        {
            SetEmptyState();
        }
    }

    private void SetLockedState()
    {
        if (lockedOverlay != null)
            lockedOverlay.SetActive(true);
            
        if (backgroundImage != null)
            backgroundImage.color = lockedColor;
            
        if (skillIcon != null)
        {
            skillIcon.sprite = null;
            skillIcon.color = Color.clear;
        }
        
        if (cooldownOverlay != null)
            cooldownOverlay.gameObject.SetActive(false);
            
        if (cooldownText != null)
            cooldownText.gameObject.SetActive(false);
    }

    private void SetEmptyState()
    {
        if (lockedOverlay != null)
            lockedOverlay.SetActive(false);
            
        if (backgroundImage != null)
            backgroundImage.color = unlockedColor;
            
        if (skillIcon != null)
        {
            skillIcon.sprite = null;
            skillIcon.color = Color.clear;
        }
        
        if (cooldownOverlay != null)
            cooldownOverlay.gameObject.SetActive(false);
            
        if (cooldownText != null)
            cooldownText.gameObject.SetActive(false);
    }

    private void SetEquippedState(SkillModule skill)
    {
        if (lockedOverlay != null)
            lockedOverlay.SetActive(false);
            
        if (backgroundImage != null)
            backgroundImage.color = equippedColor;
            
        if (skillIcon != null && skill != null)
        {
            skillIcon.sprite = skill.skillIcon;
            skillIcon.color = Color.white;
        }
    }

    public void UpdateCooldown(float cooldownRemaining)
    {
        // Add null checks to prevent NullReferenceException
        if (currentSlot == null || !currentSlot.HasSkill()) 
        {
            // Hide cooldown UI when no skill or no cooldown
            if (cooldownOverlay != null)
                cooldownOverlay.gameObject.SetActive(false);
            if (cooldownText != null)
                cooldownText.gameObject.SetActive(false);
            return;
        }
        
        if (cooldownRemaining > 0)
        {
            // Show cooldown UI
            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(true);
                
                float totalCooldown = currentSlot.equippedSkill.cooldown;
                float progress = Mathf.Clamp01(cooldownRemaining / totalCooldown);
                cooldownOverlay.fillAmount = progress;
            }

            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(true);
                cooldownText.text = cooldownRemaining.ToString("F1");
            }
        }
        else
        {
            // Hide cooldown UI when not on cooldown
            if (cooldownOverlay != null)
                cooldownOverlay.gameObject.SetActive(false);
            if (cooldownText != null)
                cooldownText.gameObject.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (parentUI == null)
            {
                Debug.LogWarning("Parent UI is not assigned for SkillSlotUIElement.");
                return;
            }

            Debug.Log($"Skill slot {slotIndex} clicked");
            // Add functionality as needed for new system
        }
    }

    // Helper method to create missing UI components
    public void CreateMissingComponents()
    {
        // Create cooldown overlay if missing
        if (cooldownOverlay == null)
        {
            GameObject overlayObj = new GameObject("CooldownOverlay");
            overlayObj.transform.SetParent(transform, false);
            
            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            
            cooldownOverlay = overlayObj.AddComponent<Image>();
            cooldownOverlay.color = new Color(0, 0, 0, 0.7f);
            cooldownOverlay.type = Image.Type.Filled;
            cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
            cooldownOverlay.gameObject.SetActive(false);
        }

        // Create cooldown text if missing
        if (cooldownText == null)
        {
            GameObject textObj = new GameObject("CooldownText");
            textObj.transform.SetParent(transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            cooldownText = textObj.AddComponent<TextMeshProUGUI>();
            cooldownText.text = "";
            cooldownText.fontSize = 14;
            cooldownText.color = Color.white;
            cooldownText.alignment = TextAlignmentOptions.Center;
            cooldownText.gameObject.SetActive(false);
        }

        // Create skill icon if missing
        if (skillIcon == null)
        {
            GameObject iconObj = new GameObject("SkillIcon");
            iconObj.transform.SetParent(transform, false);
            
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            
            skillIcon = iconObj.AddComponent<Image>();
            skillIcon.color = Color.clear;
        }

        // Create hotkey text if missing
        if (hotkeyText == null)
        {
            GameObject hotkeyObj = new GameObject("HotkeyText");
            hotkeyObj.transform.SetParent(transform, false);
            
            RectTransform hotkeyRect = hotkeyObj.AddComponent<RectTransform>();
            hotkeyRect.anchorMin = new Vector2(0, 0);
            hotkeyRect.anchorMax = new Vector2(1, 0.3f);
            hotkeyRect.offsetMin = Vector2.zero;
            hotkeyRect.offsetMax = Vector2.zero;
            
            hotkeyText = hotkeyObj.AddComponent<TextMeshProUGUI>();
            hotkeyText.text = (slotIndex + 1).ToString();
            hotkeyText.fontSize = 12;
            hotkeyText.color = Color.white;
            hotkeyText.alignment = TextAlignmentOptions.Center;
        }

        Debug.Log($"Created missing UI components for slot {slotIndex}");
    }
}

public class SkillItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Components")]
    [SerializeField] private Image skillIcon;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI levelRequirementText;
    [SerializeField] private Image backgroundImage;
    
    [Header("Visual Settings")]
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private GameObject levelRequirementIcon;
    
    private SkillModule skill;
    private SkillPanelUI parentUI; // Changed from SkillSlotsUI to SkillPanelUI
    private bool isSelected;

    public void Initialize(SkillModule skillModule, SkillPanelUI parent) // Changed parameter type
    {
        skill = skillModule;
        parentUI = parent;
        
        if (skill == null)
        {
            Debug.LogWarning("SkillModule is null in SkillItemUI.Initialize");
            return;
        }

        // Initialize UI components if they're null
        if (skillIcon == null)
        {
            skillIcon = transform.Find("SkillIcon")?.GetComponent<Image>();
        }

        if (skillNameText == null)
        {
            skillNameText = transform.Find("SkillNameText")?.GetComponent<TextMeshProUGUI>();
        }

        if (levelRequirementText == null)
        {
            levelRequirementText = transform.Find("LevelRequirementText")?.GetComponent<TextMeshProUGUI>();
        }

        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }
        
        if (skillIcon != null && skill.skillIcon != null)
        {
            skillIcon.sprite = skill.skillIcon;
            skillIcon.color = skill.GetSkillTypeColor();
        }
        
        if (skillNameText != null)
            skillNameText.text = skill.skillName;
        
        if (levelRequirementText != null)
            levelRequirementText.text = $"Lv.{skill.requiredLevel}";
        
        // Check if skill is available
        var skillManager = FindFirstObjectByType<ModularSkillManager>();
        if (skillManager != null && backgroundImage != null)
        {
            bool isAvailable = skillManager.GetPlayerLevel() >= skill.requiredLevel;
            backgroundImage.color = isAvailable ? availableColor : lockedColor;
            
            // Show/hide level requirement indicator
            if (levelRequirementIcon != null)
            {
                levelRequirementIcon.SetActive(!isAvailable);
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (parentUI == null)
            {
                Debug.LogWarning("Parent UI is not assigned for SkillItemUI.");
                return;
            }

            if (skill == null)
            {
                Debug.LogWarning("Skill is not assigned to SkillItemUI.");
                return;
            }

            SetSelected(!isSelected);
            parentUI.OnSkillItemClicked(skill);
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (backgroundImage == null)
        {
            Debug.LogWarning("Background image is not assigned to SkillItemUI.");
            return;
        }

        if (isSelected)
        {
            backgroundImage.color = selectedColor;
        }
        else
        {
            var skillManager = FindFirstObjectByType<ModularSkillManager>();
            if (skillManager == null)
            {
                Debug.LogWarning("ModularSkillManager not found in the scene.");
                backgroundImage.color = lockedColor;
                return;
            }

            if (skill == null)
            {
                Debug.LogWarning("Skill is not assigned to SkillItemUI.");
                backgroundImage.color = lockedColor;
                return;
            }

            bool isAvailable = skillManager.GetPlayerLevel() >= skill.requiredLevel;
            backgroundImage.color = isAvailable ? availableColor : lockedColor;
            
            // Show/hide level requirement indicator
            if (levelRequirementIcon != null)
            {
                levelRequirementIcon.SetActive(!isAvailable);
            }
        }
    }

    // Helper method to create missing UI components
    public void CreateMissingComponents()
    {
        // Create skill icon if missing
        if (skillIcon == null)
        {
            GameObject iconObj = new GameObject("SkillIcon");
            iconObj.transform.SetParent(transform, false);
            
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.05f, 0.2f);
            iconRect.anchorMax = new Vector2(0.25f, 0.8f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            
            skillIcon = iconObj.AddComponent<Image>();
            skillIcon.color = Color.white;
        }

        // Create skill name text if missing
        if (skillNameText == null)
        {
            GameObject nameObj = new GameObject("SkillNameText");
            nameObj.transform.SetParent(transform, false);
            
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.3f, 0.5f);
            nameRect.anchorMax = new Vector2(0.8f, 0.8f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            
            skillNameText = nameObj.AddComponent<TextMeshProUGUI>();
            skillNameText.text = skill != null ? skill.skillName : "Unknown";
            skillNameText.fontSize = 14;
            skillNameText.color = Color.white;
            skillNameText.alignment = TextAlignmentOptions.Left;
        }

        // Create level requirement text if missing
        if (levelRequirementText == null)
        {
            GameObject levelObj = new GameObject("LevelRequirementText");
            levelObj.transform.SetParent(transform, false);
            
            RectTransform levelRect = levelObj.AddComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.8f, 0.2f);
            levelRect.anchorMax = new Vector2(1f, 0.8f);
            levelRect.offsetMin = Vector2.zero;
            levelRect.offsetMax = Vector2.zero;
            
            levelRequirementText = levelObj.AddComponent<TextMeshProUGUI>();
            levelRequirementText.text = skill != null ? $"Lv.{skill.requiredLevel}" : "Lv.1";
            levelRequirementText.fontSize = 12;
            levelRequirementText.color = Color.yellow;
            levelRequirementText.alignment = TextAlignmentOptions.Center;
        }

        Debug.Log($"Created missing UI components for skill item: {(skill != null ? skill.skillName : "Unknown")}");
    }
}