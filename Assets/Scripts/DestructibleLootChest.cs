using System;
using System.Collections;
using MindLess.Gameplay;
using UnityEngine;

[RequireComponent(typeof(HealthSystem))]
public class DestructibleLootChest : MonoBehaviour
{
    [Serializable]
    private class MaterialDrop
    {
        public CurrencyType materialType = CurrencyType.Wood;
        public CurrencyPickup pickupPrefab;
        [Min(0)] public int minimumQuantity = 1;
        [Min(0)] public int maximumQuantity = 3;
    }

    [Header("Loot")]
    [SerializeField] private CurrencyPickup materialPickupPrefab;
    [SerializeField] private MaterialDrop[] materialDrops;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float dropSpread = 0.7f;

    [Header("Hit Shake")]
    [SerializeField] private Transform shakeRoot;
    [SerializeField] private float shakeDuration = 0.18f;
    [SerializeField] private float shakeDistance = 0.06f;

    [Header("Destruction")]
    [SerializeField] private GameObject destroyedEffectPrefab;
    [SerializeField] private float destroyDelay = 0.1f;

    private HealthSystem healthSystem;
    private bool hasDroppedLoot;
    private int previousHealth;
    private Vector3 shakeBaseLocalPosition;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        previousHealth = healthSystem.CurrentHealth;

        if (shakeRoot == null)
        {
            shakeRoot = transform;
        }
        shakeBaseLocalPosition = shakeRoot.localPosition;

        // The chest owns its destruction so loot and optional effects always spawn first.
        healthSystem.SetDestroyOnDeath(false);
    }

    private void OnEnable()
    {
        if (healthSystem != null)
        {
            healthSystem.Died += HandleDestroyed;
            healthSystem.HealthChanged += HandleHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (healthSystem != null)
        {
            healthSystem.Died -= HandleDestroyed;
            healthSystem.HealthChanged -= HandleHealthChanged;
        }
    }

    private void HandleDestroyed()
    {
        if (hasDroppedLoot)
        {
            return;
        }

        hasDroppedLoot = true;
        Vector3 center = dropPoint != null ? dropPoint.position : transform.position;

        SpawnMaterialDrops(center);

        if (destroyedEffectPrefab != null)
        {
            Instantiate(destroyedEffectPrefab, center, Quaternion.identity);
        }

        DisableChestCollisions();
        Destroy(gameObject, Mathf.Max(Mathf.Max(0f, destroyDelay), shakeDuration));
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth < previousHealth)
        {
            PlayHitShake();
        }

        previousHealth = currentHealth;
    }

    private void PlayHitShake()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoot.localPosition = shakeBaseLocalPosition;
        }

        shakeRoutine = StartCoroutine(Shake());
    }

    private IEnumerator Shake()
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, shakeDuration);

        while (elapsed < safeDuration)
        {
            float strength = 1f - (elapsed / safeDuration);
            Vector2 offset = UnityEngine.Random.insideUnitCircle * (shakeDistance * strength);
            shakeRoot.localPosition = shakeBaseLocalPosition + new Vector3(offset.x, 0f, offset.y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeRoot.localPosition = shakeBaseLocalPosition;
        shakeRoutine = null;
    }

    private void SpawnMaterialDrops(Vector3 center)
    {
        if (materialDrops == null)
        {
            return;
        }

        for (int i = 0; i < materialDrops.Length; i++)
        {
            MaterialDrop drop = materialDrops[i];
            if (drop == null)
            {
                continue;
            }

            int safeMinimum = Mathf.Max(0, drop.minimumQuantity);
            int safeMaximum = Mathf.Max(safeMinimum, drop.maximumQuantity);
            int quantity = UnityEngine.Random.Range(safeMinimum, safeMaximum + 1);
            if (quantity <= 0)
            {
                continue;
            }

            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * dropSpread;
            Vector3 spawnPosition = center + new Vector3(randomOffset.x, 0.2f, randomOffset.y);
            CurrencyPickup selectedPrefab = drop.pickupPrefab != null
                ? drop.pickupPrefab
                : materialPickupPrefab;
            if (selectedPrefab == null)
            {
                continue;
            }

            CurrencyPickup pickup = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
            pickup.Configure(drop.materialType, quantity);
        }
    }

    private void DisableChestCollisions()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }
}
