using System.Collections;
using UnityEngine;

public class ChoppableTree : MonoBehaviour, IPunchable
{
    [Header("Chopping")]
    [SerializeField] [Min(1)] private int punchesRequired = 5;

    [Header("Shake")]
    [SerializeField] private Transform shakeRoot;
    [SerializeField] private float shakeDuration = 0.18f;
    [SerializeField] private float shakeDistance = 0.06f;

    [Header("Wood Drop")]
    [SerializeField] private CurrencyPickup woodPickupPrefab;
    [SerializeField] private Transform dropPoint;
    [SerializeField] [Min(1)] private int minimumWood = 2;
    [SerializeField] [Min(1)] private int maximumWood = 3;
    [SerializeField] private float dropSpread = 0.65f;

    private int punchesReceived;
    private bool isChopped;
    private Vector3 shakeBaseLocalPosition;
    private Coroutine shakeRoutine;

    public int PunchesReceived => punchesReceived;
    public int PunchesRequired => punchesRequired;

    private void Awake()
    {
        if (shakeRoot == null)
        {
            shakeRoot = transform;
        }

        shakeBaseLocalPosition = shakeRoot.localPosition;
    }

    public void ReceivePunch(int damage, Vector3 hitPoint, int hitStrength = 1)
    {
        if (isChopped)
        {
            return;
        }

        punchesReceived += Mathf.Max(1, hitStrength);

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoot.localPosition = shakeBaseLocalPosition;
        }

        shakeRoutine = StartCoroutine(ShakeAndCheckForChop());
    }

    private IEnumerator ShakeAndCheckForChop()
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, shakeDuration);

        while (elapsed < safeDuration)
        {
            float strength = 1f - (elapsed / safeDuration);
            Vector2 offset = Random.insideUnitCircle * (shakeDistance * strength);
            shakeRoot.localPosition = shakeBaseLocalPosition + new Vector3(offset.x, 0f, offset.y);

            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeRoot.localPosition = shakeBaseLocalPosition;
        shakeRoutine = null;

        if (punchesReceived >= punchesRequired)
        {
            ChopTree();
        }
    }

    private void ChopTree()
    {
        if (isChopped)
        {
            return;
        }

        isChopped = true;
        DropWood();
        Destroy(gameObject);
    }

    private void DropWood()
    {
        if (woodPickupPrefab == null)
        {
            return;
        }

        int safeMinimum = Mathf.Max(1, minimumWood);
        int safeMaximum = Mathf.Max(safeMinimum, maximumWood);
        int woodToDrop = Random.Range(safeMinimum, safeMaximum + 1);
        Vector3 center = dropPoint != null ? dropPoint.position : transform.position;

        for (int i = 0; i < woodToDrop; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * dropSpread;
            Vector3 spawnPosition = center + new Vector3(randomOffset.x, 0.15f, randomOffset.y);
            CurrencyPickup pickup = Instantiate(woodPickupPrefab, spawnPosition, Quaternion.identity);
            pickup.Configure(CurrencyType.Wood, 1);
        }
    }
}
