/// <summary>
/// File: SkillDetailUI.cs
/// Author: Unity 2D RPG Refactoring Agent
/// Description: Simplified skill detail UI with direct ModularSkillManager integration
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SkillDetailUI : MonoBehaviour
{
    [Header("Skill Information References")]
    [SerializeField] private Image skillIcon;
    [SerializeField] private TextMeshProUGUI skillName;
    [SerializeField] private TextMeshProUGUI skillDescription;

    [Header("Skill Stats References")]
    [SerializeField] private TextMeshProUGUI statDamage;
    [SerializeField] private TextMeshProUGUI statRange;
    [SerializeField] private TextMeshProUGUI statCooldown;
    [SerializeField] private TextMeshProUGUI statManaCost;
    [SerializeField] private TextMeshProUGUI statSpecialEffects;

    [Header("Key Binding System")]
    [SerializeField] private TextMeshProUGUI currentKeyText;
    [SerializeField] private Button assignKeyButton;
    [SerializeField] private Button clearKeyButton;
    [SerializeField] private Button closeButton;

    [Header("Settings")]
    [SerializeField] private float keyDetectionTimeout = 10f;
    [SerializeField] private bool autoFindComponents = true;

    private SkillModule currentSkill;
    private bool isKeyAssignmentMode = false;
    private bool waitingForKeyInput = false;
    private float keyDetectionStartTime;
    
    private SimpleHotkeyChanger hotkeyChanger;
    
    private static List<KeyCode> forbiddenKeys = new List<KeyCode>
    {
        KeyCode.Escape, KeyCode.Tab, KeyCode.Return, 
        KeyCode.LeftShift, KeyCode.RightShift,
        KeyCode.LeftControl, KeyCode.RightControl, 
        KeyCode.LeftAlt, KeyCode.RightAlt,
        KeyCode.LeftCommand, KeyCode.RightCommand
    };

    void Awake()
    {
        if (autoFindComponents)
        {
            AutoFindUIComponents();
        }
        SetupButtonEvents();
    }

    void Start()
    {
        hotkeyChanger = FindFirstObjectByType<SimpleHotkeyChanger>();
        if (hotkeyChanger == null)
        {
            var go = new GameObject("SimpleHotkeyChanger");
            hotkeyChanger = go.AddComponent<SimpleHotkeyChanger>();
        }
        
        SimpleHotkeyChanger.OnHotkeyChanged += OnHotkeyChangedCallback;
        
        gameObject.SetActive(false);
    }
    
    void OnDestroy()
    {
        if (SimpleHotkeyChanger.OnHotkeyChanged != null)
        {
            SimpleHotkeyChanger.OnHotkeyChanged -= OnHotkeyChangedCallback;
        }
    }
    
    private void OnHotkeyChangedCallback(SkillModule skill, KeyCode newKey)
    {
        if (currentSkill == skill)
        {
            UpdateCurrentKeyBinding();
            UpdateButtonStates();
        }
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isKeyAssignmentMode)
            {
                ExitKeyAssignmentMode();
            }
            else
            {
                ClosePanel();
            }
        }

        if (currentSkill != null)
        {
            UpdateCurrentKeyBinding();
        }

        if (isKeyAssignmentMode && waitingForKeyInput)
        {
            HandleKeyAssignmentInput();
        }
    }

    void AutoFindUIComponents()
    {
        skillIcon = skillIcon ?? transform.Find("SkillIcon")?.GetComponent<Image>();
        skillName = skillName ?? transform.Find("SkillName")?.GetComponent<TextMeshProUGUI>();
        skillDescription = skillDescription ?? transform.Find("SkillDescription")?.GetComponent<TextMeshProUGUI>();
        
        statDamage = statDamage ?? transform.Find("StatDamage")?.GetComponent<TextMeshProUGUI>();
        statRange = statRange ?? transform.Find("StatRange")?.GetComponent<TextMeshProUGUI>();
        statCooldown = statCooldown ?? transform.Find("StatCooldown")?.GetComponent<TextMeshProUGUI>();
        statManaCost = statManaCost ?? transform.Find("StatManaCost")?.GetComponent<TextMeshProUGUI>();
        statSpecialEffects = statSpecialEffects ?? transform.Find("StatSpecialEffects")?.GetComponent<TextMeshProUGUI>();
        
        currentKeyText = currentKeyText ?? transform.Find("CurrentKeyText")?.GetComponent<TextMeshProUGUI>();
        assignKeyButton = assignKeyButton ?? transform.Find("AssignKeyButton")?.GetComponent<Button>();
        clearKeyButton = clearKeyButton ?? transform.Find("ClearKeyButton")?.GetComponent<Button>();
        closeButton = closeButton ?? transform.Find("CloseButton")?.GetComponent<Button>();
    }

    void SetupButtonEvents()
    {
        if (assignKeyButton != null)
            assignKeyButton.onClick.AddListener(OnAssignKeyClicked);
        if (clearKeyButton != null)
            clearKeyButton.onClick.AddListener(OnClearKeyClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    public void ShowSkillDetail(SkillModule skill)
    {
        if (skill == null)
        {
            return;
        }

        currentSkill = skill;
        gameObject.SetActive(true);
        isKeyAssignmentMode = false;

        UpdateSkillDisplay();
        UpdateButtonStates();
    }

    void UpdateSkillDisplay()
    {
        if (currentSkill == null) return;

        if (skillIcon != null)
        {
            if (currentSkill.skillIcon != null)
            {
                skillIcon.sprite = currentSkill.skillIcon;
                skillIcon.color = Color.white;
            }
            else
            {
                skillIcon.color = currentSkill.skillColor;
            }
        }

        if (skillName != null)
            skillName.text = currentSkill.skillName;
        if (skillDescription != null)
            skillDescription.text = currentSkill.description;

        if (statDamage != null)
            statDamage.text = $"Damage: {currentSkill.damage}";
        if (statRange != null)
            statRange.text = $"Range: {currentSkill.range}";
        if (statCooldown != null)
            statCooldown.text = $"Cooldown: {currentSkill.cooldown}s";
        if (statManaCost != null)
            statManaCost.text = $"Mana Cost: {currentSkill.manaCost}";

        if (statSpecialEffects != null)
        {
            string effects = "";
            if (currentSkill.stunDuration > 0)
                effects += $"Stun: {currentSkill.stunDuration}s ";
            if (currentSkill.knockbackForce > 0)
                effects += $"Knockback: {currentSkill.knockbackForce} ";
            if (currentSkill.healAmount > 0)
                effects += $"Heal: {currentSkill.healAmount} ";
            if (currentSkill.areaRadius > 0)
                effects += $"Area: {currentSkill.areaRadius} ";
            statSpecialEffects.text = string.IsNullOrEmpty(effects) ? "None" : effects;
        }

        UpdateCurrentKeyBinding();
    }

    void UpdateCurrentKeyBinding()
    {
        if (currentKeyText == null || currentSkill == null || hotkeyChanger == null) return;

        KeyCode assignedKey = hotkeyChanger.GetSkillHotkey(currentSkill);
        
        if (assignedKey != KeyCode.None)
        {
            currentKeyText.text = $"Assigned Key: {GetKeyDisplayName(assignedKey)}";
        }
        else
        {
            currentKeyText.text = "No key assigned";
        }
    }

    void UpdateButtonStates()
    {
        if (currentSkill == null || hotkeyChanger == null) return;

        bool hasKey = hotkeyChanger.GetSkillHotkey(currentSkill) != KeyCode.None;
        
        if (clearKeyButton != null)
            clearKeyButton.gameObject.SetActive(hasKey);
    }

    void OnAssignKeyClicked()
    {
        if (currentSkill == null)
        {
            return;
        }

        if (isKeyAssignmentMode)
        {
            ExitKeyAssignmentMode();
        }
        else
        {
            EnterKeyAssignmentMode();
        }
    }

    void OnClearKeyClicked()
    {
        if (currentSkill == null || hotkeyChanger == null) return;

        bool success = hotkeyChanger.RemoveSkill(currentSkill);
        if (success)
        {
            UpdateCurrentKeyBinding();
            UpdateButtonStates();
        }
    }

    void EnterKeyAssignmentMode()
    {
        isKeyAssignmentMode = true;
        waitingForKeyInput = true;
        keyDetectionStartTime = Time.time;

        if (assignKeyButton != null)
        {
            var buttonText = assignKeyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = "Cancel";
        }

        if (currentKeyText != null)
            currentKeyText.text = "Press ANY key to assign (ESC to cancel)";
    }

    void ExitKeyAssignmentMode()
    {
        isKeyAssignmentMode = false;
        waitingForKeyInput = false;

        if (assignKeyButton != null)
        {
            var buttonText = assignKeyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = "Assign Key";
        }

        UpdateCurrentKeyBinding();
    }

    void HandleKeyAssignmentInput()
    {
        if (Time.time - keyDetectionStartTime > keyDetectionTimeout)
        {
            ExitKeyAssignmentMode();
            return;
        }

        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(key))
            {
                if (key == KeyCode.Escape)
                {
                    ExitKeyAssignmentMode();
                    return;
                }

                if (IsKeyUsable(key))
                {
                    bool success = hotkeyChanger.ChangeSkillHotkey(currentSkill, key);
                    ExitKeyAssignmentMode();
                }
                return;
            }
        }
    }

    bool IsKeyUsable(KeyCode key)
    {
        if (forbiddenKeys.Contains(key))
            return false;
        if (key == KeyCode.Mouse3 || key == KeyCode.Mouse4 || key == KeyCode.Mouse5 || key == KeyCode.Mouse6)
            return false;
        return true;
    }

    string GetKeyDisplayName(KeyCode key)
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
            case KeyCode.Space: return "Space";
            case KeyCode.Mouse0: return "Left Click";
            case KeyCode.Mouse1: return "Right Click";
            case KeyCode.Mouse2: return "Middle Click";
            default: return key.ToString();
        }
    }

    public void ClosePanel()
    {
        if (isKeyAssignmentMode)
        {
            ExitKeyAssignmentMode();
        }
        
        gameObject.SetActive(false);
        currentSkill = null;
    }

    public bool IsVisible() => gameObject.activeInHierarchy;
    public SkillModule GetCurrentSkill() => currentSkill;

    public void TestComponent()
    {
        // Component test method for compatibility
    }
}