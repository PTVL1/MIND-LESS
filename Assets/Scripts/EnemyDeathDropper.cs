using UnityEngine;

namespace MindLess.Gameplay
{

public class EnemyDeathDropper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private EnemyPistol enemyPistol;
    [SerializeField] private WeaponItem droppedPistolPrefab;
    [SerializeField] private Transform dropPoint;

    private bool hasDropped;

    private void Awake()
    {
        if (healthSystem == null)
        {
            healthSystem = GetComponent<HealthSystem>();
        }

        if (enemyPistol == null)
        {
            enemyPistol = GetComponentInChildren<EnemyPistol>();
        }
    }

    private void OnEnable()
    {
        if (healthSystem != null)
        {
            healthSystem.Died += HandleEnemyDeath;
        }
    }

    private void OnDisable()
    {
        if (healthSystem != null)
        {
            healthSystem.Died -= HandleEnemyDeath;
        }
    }

    private void HandleEnemyDeath()
    {
        if (hasDropped || droppedPistolPrefab == null)
        {
            return;
        }

        hasDropped = true;
        DisableEnemyHeldWeapon();

        Vector3 spawnPosition = dropPoint != null
            ? dropPoint.position
            : transform.position + Vector3.up * 0.5f;

        WeaponItem dropped = Instantiate(droppedPistolPrefab, spawnPosition, Quaternion.identity);
        dropped.SetWorldState(true);

        int bulletsToDrop = enemyPistol != null ? enemyPistol.GetCurrentBullets() : 15;
        PistolWeapon pistol = dropped.GetComponent<PistolWeapon>();
        if (pistol != null)
        {
            pistol.SetCurrentBullets(bulletsToDrop);
        }

        Rigidbody rb = dropped.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce((transform.forward + Vector3.up) * 2f, ForceMode.VelocityChange);
        }
    }

    private void DisableEnemyHeldWeapon()
    {
        WeaponItem heldWeapon = enemyPistol != null ? enemyPistol.GetComponent<WeaponItem>() : null;
        if (heldWeapon == null)
        {
            return;
        }

        heldWeapon.SetWorldState(false);
        heldWeapon.gameObject.SetActive(false);
    }
}
}
