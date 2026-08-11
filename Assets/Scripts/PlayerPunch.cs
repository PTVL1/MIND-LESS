using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerPunch : MonoBehaviour
{
    [Header("Punch")]
    [SerializeField] private int damage = 40;
    [SerializeField] private float range = 1.5f;
    [SerializeField] private float radius = 0.35f;
    [SerializeField] private float cooldown = 0.45f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("References")]
    [SerializeField] private Transform punchOrigin;
    [SerializeField] private LowFpsCharacterAnimator characterAnimator;

    private PlayerInventory inventory;
    private float nextPunchTime;
    private bool punchWithLeftHand = true;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();

        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<LowFpsCharacterAnimator>();
        }
    }

    private void Update()
    {
        if (BuildingSystem.IsBuildingModeActive)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0) || Time.time < nextPunchTime)
        {
            return;
        }

        if (inventory.HasAnyWeaponEquipped())
        {
            return;
        }

        Punch();
    }

    private void Punch()
    {
        nextPunchTime = Time.time + cooldown;

        if (characterAnimator != null)
        {
            characterAnimator.PlayPunch(punchWithLeftHand);
        }

        punchWithLeftHand = !punchWithLeftHand;

        Vector3 origin = punchOrigin != null
            ? punchOrigin.position
            : transform.position + Vector3.up;
        Vector3 direction = transform.forward;

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            radius,
            direction,
            range,
            hitMask,
            QueryTriggerInteraction.Ignore);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                continue;
            }

            IPunchable punchable = FindPunchable(hits[i].collider);
            if (punchable != null)
            {
                punchable.ReceivePunch(damage, hits[i].point, 1);
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

            // The first non-player collider blocks the punch, including walls.
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
        Vector3 origin = punchOrigin != null
            ? punchOrigin.position
            : transform.position + Vector3.up;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin + transform.forward * range, radius);
    }
}
