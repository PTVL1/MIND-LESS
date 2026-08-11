using UnityEngine;

[RequireComponent(typeof(WeaponItem))]
public class MeleeWeapon : MonoBehaviour
{
    [Header("Melee Stats")]
    [SerializeField] private float range = 2f;
    [SerializeField] private float hitRadius = 0.4f;
    [SerializeField] private float hitsPerSecond = 2f;
    [SerializeField] private int damage = 55;
    [Tooltip("Extra tree-hit progress per strike. A value of 2 makes one strike count as 3 punches.")]
    [SerializeField] [Min(0)] private int treeHitsReducedBy = 2;
    [SerializeField] [Min(1f)] private float walkSpeedMultiplier = 1.2f;

    [Header("References")]
    [SerializeField] private Transform hitOrigin;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private LowFpsCharacterAnimator characterAnimator;

    private WeaponItem weaponItem;
    private PlayerInventory inventory;
    private float nextHitTime;

    public float WalkSpeedMultiplier => Mathf.Max(1f, walkSpeedMultiplier);

    private void Awake()
    {
        weaponItem = GetComponent<WeaponItem>();
    }

    private void Update()
    {
        if (BuildingSystem.IsBuildingModeActive)
        {
            return;
        }

        if (inventory == null)
        {
            inventory = GetComponentInParent<PlayerInventory>();
        }

        if (inventory == null || !inventory.IsWeaponEquipped(weaponItem))
        {
            return;
        }

        if (!inventory.IsWeaponTriggerHeld(weaponItem) || Time.time < nextHitTime)
        {
            return;
        }

        PerformHit();
    }

    private void PerformHit()
    {
        nextHitTime = Time.time + (1f / Mathf.Max(0.01f, hitsPerSecond));

        if (characterAnimator == null)
        {
            characterAnimator = inventory.GetComponentInChildren<LowFpsCharacterAnimator>();
        }

        if (characterAnimator != null)
        {
            characterAnimator.PlayMeleeAttack(inventory.GetWeaponSlot(weaponItem) == 1);
        }

        Vector3 direction = inventory.transform.forward;
        Vector3 origin = hitOrigin != null
            ? hitOrigin.position
            : inventory.transform.position + Vector3.up;

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            hitRadius,
            direction,
            range,
            hitMask,
            QueryTriggerInteraction.Ignore);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform == inventory.transform || hitTransform.IsChildOf(inventory.transform))
            {
                continue;
            }

            IPunchable punchable = FindPunchable(hits[i].collider);
            if (punchable != null)
            {
                punchable.ReceivePunch(damage, hits[i].point, 1 + treeHitsReducedBy);
            }
            else
            {
                MindLess.Gameplay.HealthSystem health =
                    hits[i].collider.GetComponentInParent<MindLess.Gameplay.HealthSystem>();
                if (health != null)
                {
                    health.TakeDamage(damage);
                }
            }

            break;
        }
    }

    private IPunchable FindPunchable(Collider hitCollider)
    {
        MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IPunchable punchable)
            {
                return punchable;
            }
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Transform owner = inventory != null ? inventory.transform : transform.root;
        Vector3 origin = hitOrigin != null ? hitOrigin.position : owner.position + Vector3.up;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin + owner.forward * range, hitRadius);
    }
}
