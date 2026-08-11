using UnityEngine;
using UnityEngine.UI;

public class CurrencyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCurrencyWallet wallet;
    [SerializeField] private Text goldText;
    [SerializeField] private Text woodText;
    [SerializeField] private Text ironText;
    [SerializeField] private Text stoneText;
    [SerializeField] private Text woolText;

    [Header("Labels")]
    [SerializeField] private string goldLabel = "Gold: ";
    [SerializeField] private string woodLabel = "Wood: ";
    [SerializeField] private string ironLabel = "Iron: ";
    [SerializeField] private string stoneLabel = "Stone: ";
    [SerializeField] private string woolLabel = "Wool: ";

    private void Awake()
    {
        if (wallet == null)
        {
            wallet = FindObjectOfType<PlayerCurrencyWallet>();
        }
    }

    private void OnEnable()
    {
        if (wallet != null)
        {
            wallet.CurrencyChanged += HandleCurrencyChanged;
        }

        RefreshAll();
    }

    private void OnDisable()
    {
        if (wallet != null)
        {
            wallet.CurrencyChanged -= HandleCurrencyChanged;
        }
    }

    private void HandleCurrencyChanged(CurrencyType currencyType, int amount)
    {
        RefreshCurrency(currencyType, amount);
    }

    private void RefreshAll()
    {
        if (wallet == null)
        {
            return;
        }

        RefreshCurrency(CurrencyType.Gold, wallet.Gold);
        RefreshCurrency(CurrencyType.Wood, wallet.Wood);
        RefreshCurrency(CurrencyType.Iron, wallet.Iron);
        RefreshCurrency(CurrencyType.Stone, wallet.Stone);
        RefreshCurrency(CurrencyType.Wool, wallet.Wool);
    }

    private void RefreshCurrency(CurrencyType currencyType, int amount)
    {
        switch (currencyType)
        {
            case CurrencyType.Gold:
                SetText(goldText, goldLabel, amount);
                break;
            case CurrencyType.Wood:
                SetText(woodText, woodLabel, amount);
                break;
            case CurrencyType.Iron:
                SetText(ironText, ironLabel, amount);
                break;
            case CurrencyType.Stone:
                SetText(stoneText, stoneLabel, amount);
                break;
            case CurrencyType.Wool:
                SetText(woolText, woolLabel, amount);
                break;
        }
    }

    private void SetText(Text target, string label, int amount)
    {
        if (target != null)
        {
            target.text = label + amount;
        }
    }
}
