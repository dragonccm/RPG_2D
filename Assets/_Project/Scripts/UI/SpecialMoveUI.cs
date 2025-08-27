using UnityEngine;
using UnityEngine.UI;

public class SpecialMoveUI : MonoBehaviour
{
    [Header("UI References")]
    public Image dashIcon;
    public Image teleportIcon;
    public Image chargeAttackIcon;
    
    public Image dashCooldownOverlay;
    public Image teleportCooldownOverlay;
    public Image chargeAttackCooldownOverlay;
    
    public Text dashKeyText;
    public Text teleportKeyText;
    public Text chargeAttackKeyText;

    private special_move specialMove;

    void Start()
    {
        specialMove = FindObjectOfType<special_move>();
        if (specialMove == null)
        {
            Debug.LogWarning("SpecialMoveUI: Không tìm thấy special_move component!");
            return;
        }

        UpdateKeyDisplay();
    }

    void Update()
    {
        if (specialMove == null) return;

        UpdateCooldownDisplay();
    }

    void UpdateCooldownDisplay()
    {
        if (dashCooldownOverlay != null)
        {
            dashCooldownOverlay.fillAmount = specialMove.GetDashCooldownProgress();
        }

        if (teleportCooldownOverlay != null)
        {
            teleportCooldownOverlay.fillAmount = specialMove.GetTeleportCooldownProgress();
        }

        if (chargeAttackCooldownOverlay != null)
        {
            chargeAttackCooldownOverlay.fillAmount = specialMove.GetChargeAttackCooldownProgress();
        }

        // Làm mờ icon khi đang cooldown
        if (dashIcon != null)
        {
            Color iconColor = dashIcon.color;
            iconColor.a = specialMove.GetDashCooldownProgress() > 0 ? 0.5f : 1f;
            dashIcon.color = iconColor;
        }

        if (teleportIcon != null)
        {
            Color iconColor = teleportIcon.color;
            iconColor.a = specialMove.GetTeleportCooldownProgress() > 0 ? 0.5f : 1f;
            teleportIcon.color = iconColor;
        }

        if (chargeAttackIcon != null)
        {
            Color iconColor = chargeAttackIcon.color;
            iconColor.a = specialMove.GetChargeAttackCooldownProgress() > 0 ? 0.5f : 1f;
            chargeAttackIcon.color = iconColor;
        }
    }

    void UpdateKeyDisplay()
    {
        if (specialMove == null) return;

        if (dashKeyText != null)
        {
            dashKeyText.text = specialMove.dashKey.ToString();
        }

        if (teleportKeyText != null)
        {
            teleportKeyText.text = specialMove.teleportKey.ToString();
        }

        if (chargeAttackKeyText != null)
        {
            chargeAttackKeyText.text = specialMove.chargeAttackKey.ToString();
        }
    }

    public void RefreshUI()
    {
        UpdateKeyDisplay();
        UpdateCooldownDisplay();
    }
}