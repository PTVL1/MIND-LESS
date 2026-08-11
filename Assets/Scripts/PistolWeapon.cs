using System.Collections;
using System;
using UnityEngine;

[RequireComponent(typeof(WeaponItem))]
public class PistolWeapon : MonoBehaviour, IAmmoWeapon
{
    [Header("Pistol Stats")]
    [SerializeField] private int maxBullets = 15;
    [SerializeField] private int damagePerShot = 35;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private float range = 120f;
    [SerializeField] private float gunshotNoiseRadius = 14f;

    [Header("References")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletVisualSpeed = 80f;
    [SerializeField] private float bulletVisualLifetime = 2f;

    [Header("Feedback")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private Vector3 muzzleFlashRotationOffset;
    [SerializeField] private bool useMuzzlePointRotation = true;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;

    [Header("Minimal Recoil")]
    [SerializeField] private float recoilKickBack = 0.03f;
    [SerializeField] private float recoilRecoverSpeed = 16f;
    [SerializeField] private Transform recoilPivot;

    private WeaponItem weaponItem;
    private PlayerInventory playerInventory;
    private int currentBullets;
    private float nextShootTime;
    private Vector3 recoilBaseLocalPosition;
    private Vector3 recoilOffset;

    public int CurrentAmmo => currentBullets;
    public int MaxAmmo => maxBullets;
    public string AmmoDisplayName => weaponItem != null ? weaponItem.WeaponName : "Pistol";
    public event Action<IAmmoWeapon> AmmoChanged;

    private void Awake()
    {
        weaponItem = GetComponent<WeaponItem>();
        currentBullets = maxBullets;

        if (recoilPivot != null)
        {
            recoilBaseLocalPosition = recoilPivot.localPosition;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    private void Update()
    {
        if (playerInventory == null)
        {
            playerInventory = GetComponentInParent<PlayerInventory>();
        }

        recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, recoilRecoverSpeed * Time.deltaTime);
        if (recoilPivot != null)
        {
            recoilPivot.localPosition = recoilBaseLocalPosition + recoilOffset;
        }

        if (!CanShoot())
        {
            return;
        }

        if (playerInventory.IsWeaponTriggerHeld(weaponItem))
        {
            Shoot();
        }
    }

    private bool CanShoot()
    {
        if (BuildingSystem.IsBuildingModeActive)
        {
            return false;
        }

        if (currentBullets <= 0)
        {
            return false;
        }

        if (Time.time < nextShootTime)
        {
            return false;
        }

        if (playerInventory == null)
        {
            return false;
        }

        return playerInventory.IsWeaponEquipped(weaponItem);
    }

    private void Shoot()
    {
        nextShootTime = Time.time + fireRate;
        currentBullets--;
        AmmoChanged?.Invoke(this);

        Vector3 origin = muzzlePoint != null ? muzzlePoint.position : transform.position;
        Vector3 direction = GetShootDirection();

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            MindLess.Gameplay.HealthSystem health = hit.collider.GetComponentInParent<MindLess.Gameplay.HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(damagePerShot);
            }
        }

        PlayShootFeedback(direction);
        SpawnBulletVisual(origin, direction);
        GunshotNoise.Raise(origin, gunshotNoiseRadius);

        if (recoilPivot != null)
        {
            recoilOffset += Vector3.back * recoilKickBack;
        }
    }

    private Vector3 GetShootDirection()
    {
        Vector3 direction;

        if (playerInventory != null)
        {
            direction = playerInventory.transform.forward;
        }
        else if (muzzlePoint != null)
        {
            direction = muzzlePoint.forward;
        }
        else
        {
            direction = transform.forward;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = transform.forward;
        }

        return direction.normalized;
    }

    private void PlayShootFeedback(Vector3 direction)
    {
        if (muzzleFlash == null)
        {
            muzzleFlash = GetComponentInChildren<ParticleSystem>(true);
        }

        if (muzzleFlash != null)
        {
            if (!muzzleFlash.gameObject.activeSelf)
            {
                muzzleFlash.gameObject.SetActive(true);
            }

            if (muzzlePoint != null)
            {
                Quaternion baseRotation = useMuzzlePointRotation
                    ? muzzlePoint.rotation
                    : Quaternion.LookRotation(direction, Vector3.up);

                Quaternion flashRotation = baseRotation * Quaternion.Euler(muzzleFlashRotationOffset);
                muzzleFlash.transform.SetPositionAndRotation(muzzlePoint.position, flashRotation);
            }

            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.Play(true);
            muzzleFlash.Emit(1);
        }

        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
    }

    private void SpawnBulletVisual(Vector3 origin, Vector3 direction)
    {
        if (bulletPrefab == null)
        {
            return;
        }

        GameObject bullet = Instantiate(bulletPrefab, origin, Quaternion.LookRotation(direction));

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.velocity = direction * bulletVisualSpeed;
            Destroy(bullet, bulletVisualLifetime);
            return;
        }

        StartCoroutine(MoveBulletWithoutRigidbody(bullet, direction));
    }

    private IEnumerator MoveBulletWithoutRigidbody(GameObject bullet, Vector3 direction)
    {
        float elapsed = 0f;

        while (bullet != null && elapsed < bulletVisualLifetime)
        {
            bullet.transform.position += direction * (bulletVisualSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (bullet != null)
        {
            Destroy(bullet);
        }
    }

    public int GetCurrentBullets()
    {
        return currentBullets;
    }

    public int GetMaxBullets()
    {
        return maxBullets;
    }

    public void SetCurrentBullets(int bullets)
    {
        currentBullets = Mathf.Clamp(bullets, 0, maxBullets);
        AmmoChanged?.Invoke(this);
    }
}
