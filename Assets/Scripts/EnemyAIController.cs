using UnityEngine;
using UnityEngine.AI;

namespace MindLess.Gameplay
{

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAIController : MonoBehaviour
{
    private enum EnemyState
    {
        Roam,
        Alert,
        Investigate,
        Combat
    }

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 16f;
    [SerializeField] private float chaseDelay = 1f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private LayerMask lineOfSightMask = ~0;

    [Header("Hearing")]
    [SerializeField] private float hearingSensitivityMultiplier = 1f;

    [Header("Combat")]
    [SerializeField] private EnemyPistol enemyPistol;
    [SerializeField] private float attackRange = 11f;
    [Header("Roaming")]
    [SerializeField] private float roamRadius = 12f;
    [SerializeField] private float roamWaitTime = 1f;
    [SerializeField] private float roamPointTolerance = 0.6f;
    [SerializeField] private int roamSampleAttempts = 8;
    [SerializeField] private float investigateStopDistance = 1.2f;

    private NavMeshAgent agent;
    private HealthSystem healthSystem;
    private EnemyState state;
    private Vector3 lastKnownPlayerPosition;
    private float roamWaitTimer;
    private float alertTimer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        healthSystem = GetComponent<HealthSystem>();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (enemyPistol == null)
        {
            enemyPistol = GetComponentInChildren<EnemyPistol>();
        }
    }

    private void OnEnable()
    {
        GunshotNoise.OnGunshot += HandleGunshot;
    }

    private void OnDisable()
    {
        GunshotNoise.OnGunshot -= HandleGunshot;
    }

    private void Start()
    {
        state = EnemyState.Roam;
        ChooseRoamDestination();
    }

    private void Update()
    {
        if (healthSystem != null && healthSystem.IsDead)
        {
            agent.isStopped = true;
            return;
        }

        bool playerDetected = CanDetectPlayer();
        if (playerDetected)
        {
            lastKnownPlayerPosition = player.position;
            if (state == EnemyState.Roam || state == EnemyState.Investigate)
            {
                BeginAlert();
            }
        }

        switch (state)
        {
            case EnemyState.Roam:
                UpdateRoam();
                break;
            case EnemyState.Alert:
                UpdateAlert(playerDetected);
                break;
            case EnemyState.Investigate:
                UpdateInvestigate();
                break;
            case EnemyState.Combat:
                UpdateCombat();
                break;
        }
    }

    private bool CanDetectPlayer()
    {
        if (player == null)
        {
            return false;
        }

        Vector3 eyePosition = transform.position + Vector3.up * 1.6f;
        Vector3 toPlayer = player.position - eyePosition;
        float distance = toPlayer.magnitude;

        if (distance > detectionRadius)
        {
            return false;
        }

        RaycastHit[] hits = Physics.RaycastAll(eyePosition, toPlayer.normalized, distance, lineOfSightMask, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0)
        {
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                continue;
            }

            return hitTransform == player || hitTransform.IsChildOf(player);
        }

        return false;
    }

    private void HandleGunshot(Vector3 position, float radius)
    {
        float effectiveRadius = radius * hearingSensitivityMultiplier;
        if (Vector3.Distance(transform.position, position) > effectiveRadius)
        {
            return;
        }

        if (state == EnemyState.Combat || state == EnemyState.Alert)
        {
            return;
        }

        lastKnownPlayerPosition = position;
        state = EnemyState.Investigate;
        agent.SetDestination(lastKnownPlayerPosition);
    }

    private void UpdateCombat()
    {
        if (player == null)
        {
            BeginRoaming();
            return;
        }

        if (CanDetectPlayer())
        {
            lastKnownPlayerPosition = player.position;

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer > attackRange)
            {
                if (HasPathTo(player.position))
                {
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                }
                else
                {
                    state = EnemyState.Investigate;
                    agent.isStopped = false;
                    agent.SetDestination(lastKnownPlayerPosition);
                }
            }
            else
            {
                agent.isStopped = true;
                Vector3 look = player.position - transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(look),
                        Time.deltaTime * turnSpeed);
                }

                if (enemyPistol != null)
                {
                    enemyPistol.TryShootAt(player.position);
                }
            }

            return;
        }

        agent.isStopped = false;
        state = EnemyState.Investigate;
        agent.SetDestination(lastKnownPlayerPosition);
    }

    private void UpdateInvestigate()
    {
        agent.isStopped = false;

        if (CanDetectPlayer())
        {
            BeginAlert();
            return;
        }

        agent.SetDestination(lastKnownPlayerPosition);

        if (!agent.pathPending && agent.remainingDistance <= investigateStopDistance)
        {
            BeginRoaming();
        }
    }

    private void UpdateAlert(bool playerDetected)
    {
        agent.isStopped = true;

        if (!playerDetected || player == null)
        {
            alertTimer = 0f;
            BeginRoaming();
            return;
        }

        lastKnownPlayerPosition = player.position;
        FacePosition(player.position);
        alertTimer += Time.deltaTime;

        if (alertTimer >= chaseDelay)
        {
            state = EnemyState.Combat;
            agent.isStopped = false;
            return;
        }
    }

    private void UpdateRoam()
    {
        agent.isStopped = false;

        if (agent.pathPending)
        {
            return;
        }

        if (!agent.hasPath || agent.remainingDistance <= roamPointTolerance)
        {
            roamWaitTimer += Time.deltaTime;
            if (roamWaitTimer >= roamWaitTime)
            {
                roamWaitTimer = 0f;
                ChooseRoamDestination();
            }
        }
    }


    private bool HasPathTo(Vector3 destination)
    {
        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(destination, path))
        {
            return false;
        }

        return path.status == NavMeshPathStatus.PathComplete;
    }

    private void BeginAlert()
    {
        state = EnemyState.Alert;
        alertTimer = 0f;
        agent.isStopped = true;
    }

    private void BeginRoaming()
    {
        state = EnemyState.Roam;
        alertTimer = 0f;
        roamWaitTimer = 0f;
        agent.isStopped = false;
        ChooseRoamDestination();
    }

    private void ChooseRoamDestination()
    {
        for (int i = 0; i < roamSampleAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * roamRadius;
            Vector3 candidate = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, roamRadius, agent.areaMask) &&
                HasPathTo(hit.position))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    private void FacePosition(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                turnSpeed * Time.deltaTime);
        }
    }
}
}
