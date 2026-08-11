using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CurrencyPickup : MonoBehaviour
{
    [SerializeField] private CurrencyType currencyType = CurrencyType.Gold;
    [SerializeField] [Min(1)] private int amount = 1;

    private bool wasCollected;

    public void Configure(CurrencyType newCurrencyType, int newAmount)
    {
        currencyType = newCurrencyType;
        amount = Mathf.Max(1, newAmount);
    }

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Reset()
    {
        Collider pickupCollider = GetComponent<Collider>();
        pickupCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (wasCollected)
        {
            return;
        }

        PlayerCurrencyWallet wallet = other.GetComponentInParent<PlayerCurrencyWallet>();
        if (wallet == null)
        {
            return;
        }

        wasCollected = true;
        wallet.Add(currencyType, amount);
        Destroy(gameObject);
    }
}
