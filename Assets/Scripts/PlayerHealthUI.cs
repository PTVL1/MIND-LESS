using MindLess.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace MindLess.UI
{
public class PlayerHealthUI : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private HealthSystem playerHealth;

    [Header("UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFill;
    [SerializeField] private Text healthText;

    [Header("Colors")]
    [SerializeField] private Color healthyColor = new Color(0.15f, 0.8f, 0.2f);
    [SerializeField] private Color lowHealthColor = new Color(0.9f, 0.1f, 0.1f);

    private void OnEnable()
    {
        if (playerHealth == null)
        {
            return;
        }

        playerHealth.HealthChanged += UpdateHealthUI;
        UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= UpdateHealthUI;
        }
    }

    private void UpdateHealthUI(int currentHealth, int maxHealth)
    {
        int safeMaxHealth = Mathf.Max(1, maxHealth);
        float normalizedHealth = Mathf.Clamp01((float)currentHealth / safeMaxHealth);

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = safeMaxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthFill != null)
        {
            healthFill.fillAmount = normalizedHealth;
            healthFill.color = Color.Lerp(lowHealthColor, healthyColor, normalizedHealth);
        }

        if (healthText != null)
        {
            healthText.text = currentHealth + " / " + safeMaxHealth;
        }
    }
}
}
