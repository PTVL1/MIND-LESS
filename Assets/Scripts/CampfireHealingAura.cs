using MindLess.Gameplay;
using UnityEngine;

public class CampfireHealingAura : MonoBehaviour
{
    [Header("Healing")]
    [SerializeField] private float healingRadius = 4f;
    [SerializeField] private float healthPerSecond = 2f;

    [Header("Player")]
    [SerializeField] private HealthSystem playerHealth;

    private float pendingHealing;

    private void Awake()
    {
        if (playerHealth != null)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponentInParent<HealthSystem>();
            if (playerHealth == null)
            {
                playerHealth = player.GetComponentInChildren<HealthSystem>();
            }
        }
    }

    private void Update()
    {
        if (playerHealth == null || playerHealth.IsDead)
        {
            pendingHealing = 0f;
            return;
        }

        float sqrDistance = (playerHealth.transform.position - transform.position).sqrMagnitude;
        if (sqrDistance > healingRadius * healingRadius)
        {
            pendingHealing = 0f;
            return;
        }

        if (playerHealth.CurrentHealth >= playerHealth.MaxHealth)
        {
            pendingHealing = 0f;
            return;
        }

        pendingHealing += Mathf.Max(0f, healthPerSecond) * Time.deltaTime;
        int wholeHealthPoints = Mathf.FloorToInt(pendingHealing);
        if (wholeHealthPoints <= 0)
        {
            return;
        }

        pendingHealing -= wholeHealthPoints;
        playerHealth.Heal(wholeHealthPoints);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.35f, 0.05f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, healingRadius);
    }
}
