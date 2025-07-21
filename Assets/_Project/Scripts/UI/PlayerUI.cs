using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("Player Health & Mana UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private TMPro.TextMeshProUGUI healthText;
    [SerializeField] private TMPro.TextMeshProUGUI manaText;

    private Character playerCharacter;

    void Start()
    {
        // T�m player character
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerCharacter = player.GetComponent<Character>();
            if (playerCharacter != null)
            {
                InitializeUI();
            }
            else
            {
                Debug.LogError("Player kh�ng c� Character component!");
            }
        }
        else
        {
            Debug.LogError("Kh�ng t�m th?y GameObject v?i tag 'Player'!");
        }
    }

    private void InitializeUI()
    {
        // Subscribe to health changes
        playerCharacter.health.OnValueChanged += UpdateHealthUI;
        playerCharacter.mana.OnValueChanged += UpdateManaUI;
        playerCharacter.OnDamageTaken += OnTakeDamage;


        // Initial update
        UpdateHealthUI(playerCharacter.health.currentValue, playerCharacter.health.maxValue);
        UpdateManaUI(playerCharacter.mana.currentValue, playerCharacter.mana.maxValue);
    }

    public void OnTakeDamage(float damage)
    {
        // This method can be used to trigger visual effects like screen shake, flashing, etc.
        Debug.Log($"Player has taken {damage} damage!");
    }

    private void UpdateHealthUI(float currentValue, float maxValue)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxValue;
            healthSlider.value = currentValue;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(currentValue)}/{Mathf.Ceil(maxValue)}";
        }
    }

    private void UpdateManaUI(float currentValue, float maxValue)
    {
        if (manaSlider != null)
        {
            manaSlider.maxValue = maxValue;
            manaSlider.value = currentValue;
        }

        if (manaText != null)
        {
            manaText.text = $"{Mathf.Ceil(currentValue)}/{Mathf.Ceil(maxValue)}";
        }
    }

    private void OnDestroy()
    {
        // Cleanup subscriptions
        if (playerCharacter != null)
        {
            playerCharacter.health.OnValueChanged -= UpdateHealthUI;
            playerCharacter.mana.OnValueChanged -= UpdateManaUI;
            playerCharacter.OnDamageTaken -= OnTakeDamage;
        }
    }
}
