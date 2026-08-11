using UnityEngine;

public class EnemyPistol : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int damagePerShot = 20;
    [SerializeField] private int maxBullets = 15;
    [SerializeField] private float fireRate = 0.3f;
    [SerializeField] private float range = 80f;

    [Header("References")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("FX")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;

    private float nextShootTime;
    private int currentBullets;

    public bool CanShoot => Time.time >= nextShootTime && currentBullets > 0;

    private void Awake()
    {
        currentBullets = maxBullets;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public bool TryShootAt(Vector3 targetPosition)
    {
        if (!CanShoot)
        {
            return false;
        }

        nextShootTime = Time.time + fireRate;
        currentBullets--;

        Vector3 origin = muzzlePoint != null ? muzzlePoint.position : transform.position;
        Vector3 direction = (targetPosition - origin).normalized;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = transform.forward;
        }

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            MindLess.Gameplay.HealthSystem health = hit.collider.GetComponentInParent<MindLess.Gameplay.HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(damagePerShot);
            }
        }

        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.Play(true);
        }

        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        return true;
    }

    public int GetCurrentBullets()
    {
        return currentBullets;
    }
}
