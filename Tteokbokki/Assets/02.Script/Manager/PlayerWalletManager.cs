using TMPro;
using UnityEngine;

public class PlayerWalletManager : MonoBehaviour
{
    public static PlayerWalletManager Instance { get; private set; }


    public int CurrentBalance { get; private set; } = 1000000; // 초기 잔고
    public float taxRate = 0.25f; // 25% 세금

    public int LastPaidTaxAmount { get; private set; } = 0; //EndOfDayUIHandler <- 세금 기록용 프로퍼티

    public TextMeshProUGUI balanceText;
    public TextMeshProUGUI endOfDayBalanceText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        UpdateUI();
    }

    public void AddIncome(int amount)
    {
        CurrentBalance += amount;
        UpdateUI();
    }

    public bool Spend(int amount)
    {
        if (CurrentBalance < amount)
        {
            Debug.LogWarning($"잔고 부족! 현재: {CurrentBalance}원, 필요: {amount}원");
            return false;
        }

        CurrentBalance -= amount;
        UpdateUI();
        return true;
    }

    public void DeductDailyTaxes(int totalSales)
    {
        int taxAmount = Mathf.RoundToInt(totalSales * taxRate);
        LastPaidTaxAmount = taxAmount;

        CurrentBalance -= taxAmount;
        Debug.Log($"세금 {taxAmount}원 납부됨 (세율 {taxRate * 100}%)");
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (balanceText != null)
            balanceText.text = $"잔고: {CurrentBalance:N0}원";
        if (endOfDayBalanceText != null)
            endOfDayBalanceText.text = $"현재 자산: {CurrentBalance:N0}원";
    }

    public void SetBalance(int newBalance)
    {
        CurrentBalance = newBalance;
        UpdateUI();
    }
}
