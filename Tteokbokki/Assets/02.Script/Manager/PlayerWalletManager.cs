using TMPro;
using UnityEngine;
using DG.Tweening; // ✨ DOTween 추가

public class PlayerWalletManager : MonoBehaviour
{
    public static PlayerWalletManager Instance { get; private set; }
    public int TodayEarnedAmount { get; private set; } = 0;     //하루동안 얻은 수익

    [SerializeField]
    private int currentBalance = 250000;

    // 기존 코드들이 에러 나지 않도록 연결해주는 프로퍼티
    public int CurrentBalance
    {
        get { return currentBalance; }
        private set { currentBalance = value; }
    }
    public float taxRate = 0.25f; // 25% 세금

    public int LastPaidTaxAmount { get; private set; } = 0; //EndOfDayUIHandler <- 세금 기록용 프로퍼티

    public TextMeshProUGUI balanceText;
    public TextMeshProUGUI [] endOfDayBalanceText;

    // ✨ 애니메이션 중복 실행 방지용 트윈 변수
    private Tween balanceTween;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        UpdateUI(false); // 시작할 때는 애니메이션 없이 즉시 표시
    }

    public void AddIncome(int amount)
    {
        int prevBalance = CurrentBalance; // ✨ 변경 전 금액 저장

        CurrentBalance += amount;
        TodayEarnedAmount += amount;

        // ✨ 변경 전 금액부터 애니메이션 시작
        UpdateUI(true, prevBalance);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(111);
    }

    public void ResetTodayEarnings()
    {
        TodayEarnedAmount = 0;
    }

    public bool Spend(int amount)
    {
        if (CurrentBalance < amount)
        {
            Debug.LogWarning($"잔고 부족! 현재: {CurrentBalance}원, 필요: {amount}원");
            return false;
        }

        int prevBalance = CurrentBalance; // ✨ 변경 전 금액 저장

        CurrentBalance -= amount;

        // ✨ 변경 전 금액부터 애니메이션 시작
        UpdateUI(true, prevBalance);
        return true;
    }

    public void DeductDailyTaxes(int totalSales)
    {
        int taxAmount = Mathf.RoundToInt(totalSales * taxRate);
        LastPaidTaxAmount = taxAmount;

        int prevBalance = CurrentBalance; // ✨ 변경 전 금액 저장

        CurrentBalance -= taxAmount;
        Debug.Log($"세금 {taxAmount}원 납부됨 (세율 {taxRate * 100}%)");

        UpdateUI(true, prevBalance);
    }

    // ✨ UI 갱신 함수 수정 (애니메이션 여부, 시작 값)
    private void UpdateUI(bool animate = false, int startValue = 0)
    {
        // 텍스트 UI가 연결되지 않았으면 중단
        if (balanceText == null && endOfDayBalanceText == null) return;

        if (animate)
        {
            // 기존에 돌고 있던 카운팅이 있다면 중지 (숫자가 튀는 것 방지)
            balanceTween?.Kill();

            // startValue 부터 CurrentBalance 까지 0.5초 동안 변화
            balanceTween = DOVirtual.Float(startValue, CurrentBalance, 0.5f, (value) =>
            {
                // ✨ 1. 애니메이션 중 잔고 번역
                if (balanceText != null)
                    balanceText.text = TextTranslator.GetUIText("UI_Balance", (int)value);

                if (endOfDayBalanceText != null)
                {
                    for (int i = 0; i < endOfDayBalanceText.Length; i++)
                    {
                        // ✨ 2. 애니메이션 중 현재 자산 번역
                        endOfDayBalanceText[i].text = TextTranslator.GetUIText("UI_CurrentAsset", (int)value);
                    }
                }
            }).SetEase(Ease.OutExpo); // OutExpo: 빠르다가 끝에 부드럽게 멈춤 (돈 계산 느낌에 좋음)
        }
        else
        {
            // ✨ 3. 애니메이션 없이 즉시 갱신될 때의 잔고/자산 번역
            if (balanceText != null)
                balanceText.text = TextTranslator.GetUIText("UI_Balance", CurrentBalance);

            if (endOfDayBalanceText != null)
            {
                for (int i = 0; i < endOfDayBalanceText.Length; i++)
                {
                    endOfDayBalanceText[i].text = TextTranslator.GetUIText("UI_CurrentAsset", CurrentBalance);
                }
            }
        }
    }

    public void SetBalance(int newBalance)
    {
        CurrentBalance = newBalance;
        UpdateUI(false); // 로드 시에는 애니메이션 없이 갱신
    }
}