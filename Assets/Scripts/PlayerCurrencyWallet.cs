using System;
using UnityEngine;

public class PlayerCurrencyWallet : MonoBehaviour
{
    [Header("Starting Currency")]
    [SerializeField] [Min(0)] private int startingGold;
    [SerializeField] [Min(0)] private int startingWood;
    [SerializeField] [Min(0)] private int startingIron;
    [SerializeField] [Min(0)] private int startingStone;
    [SerializeField] [Min(0)] private int startingWool;

    private int gold;
    private int wood;
    private int iron;
    private int stone;
    private int wool;

    public int Gold => gold;
    public int Wood => wood;
    public int Iron => iron;
    public int Stone => stone;
    public int Wool => wool;

    public event Action<CurrencyType, int> CurrencyChanged;

    private void Awake()
    {
        gold = Mathf.Max(0, startingGold);
        wood = Mathf.Max(0, startingWood);
        iron = Mathf.Max(0, startingIron);
        stone = Mathf.Max(0, startingStone);
        wool = Mathf.Max(0, startingWool);
    }

    private void Start()
    {
        NotifyAllCurrencies();
    }

    public int GetAmount(CurrencyType currencyType)
    {
        switch (currencyType)
        {
            case CurrencyType.Gold:
                return gold;
            case CurrencyType.Wood:
                return wood;
            case CurrencyType.Iron:
                return iron;
            case CurrencyType.Stone:
                return stone;
            case CurrencyType.Wool:
                return wool;
            default:
                return 0;
        }
    }

    public void Add(CurrencyType currencyType, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SetAmount(currencyType, GetAmount(currencyType) + amount);
    }

    public bool CanAfford(CurrencyType currencyType, int amount)
    {
        return amount >= 0 && GetAmount(currencyType) >= amount;
    }

    public bool TrySpend(CurrencyType currencyType, int amount)
    {
        if (!CanAfford(currencyType, amount))
        {
            return false;
        }

        SetAmount(currencyType, GetAmount(currencyType) - amount);
        return true;
    }

    public void SetAmount(CurrencyType currencyType, int amount)
    {
        int safeAmount = Mathf.Max(0, amount);

        switch (currencyType)
        {
            case CurrencyType.Gold:
                gold = safeAmount;
                break;
            case CurrencyType.Wood:
                wood = safeAmount;
                break;
            case CurrencyType.Iron:
                iron = safeAmount;
                break;
            case CurrencyType.Stone:
                stone = safeAmount;
                break;
            case CurrencyType.Wool:
                wool = safeAmount;
                break;
            default:
                return;
        }

        CurrencyChanged?.Invoke(currencyType, safeAmount);
    }

    private void NotifyAllCurrencies()
    {
        CurrencyChanged?.Invoke(CurrencyType.Gold, gold);
        CurrencyChanged?.Invoke(CurrencyType.Wood, wood);
        CurrencyChanged?.Invoke(CurrencyType.Iron, iron);
        CurrencyChanged?.Invoke(CurrencyType.Stone, stone);
        CurrencyChanged?.Invoke(CurrencyType.Wool, wool);
    }
}
