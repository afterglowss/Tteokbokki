using System.Security.Policy;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndOfDayUIHandler : MonoBehaviour
{
    [Header("ScrollView Panels")]
    [SerializeField] private GameObject scrollViewSettlement;
    [SerializeField] private GameObject scrollViewShop;
    [SerializeField] private GameObject scrollViewClosing;

    [Header("상단 탭 버튼")]
    [SerializeField] private Button buttonSettlement;
    [SerializeField] private Button buttonShop;
    [SerializeField] private Button buttonClosing;

    [Header("성공/실패 영수증 텍스트")]
    [SerializeField] private TextMeshProUGUI textSuccess;
    [SerializeField] private TextMeshProUGUI textmissed;
    [Header("주문 필요재료 텍스트")]
    [SerializeField] private TextMeshProUGUI lowStockTextUI;
    [Header("총 필요 재료 비용 텍스트")]
    [SerializeField] private TextMeshProUGUI textLowStockCost;
    [Header("정기 지출 텍스트")]
    [SerializeField] private TextMeshProUGUI textTax;
    [Header("하루 수익 텍스트")]
    [SerializeField] private TextMeshProUGUI textTodayEarnings;

    [SerializeField] private Button buttonPayTax;
    [SerializeField] private TextMeshProUGUI textPayTaxButton;

    [Header("마감 체크 토글 버튼")]
    [SerializeField] private Toggle checkReciptToggle;
    [SerializeField] private Toggle checkTaxToggle;
    [SerializeField] private Toggle checkIngredientToggle;
    [SerializeField] private Toggle checkAllToggle;

    private bool isUpdating = false;

    private void SetActiveTab(Button selected)
    {
        Button[] buttons = { buttonSettlement, buttonShop, buttonClosing };

        foreach (var btn in buttons)
        {
            var colors = btn.colors;

            if (btn == selected)
            {
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.white;
            }
            else
            {
                colors.normalColor = new Color(0.7f, 0.7f, 0.7f);
                colors.highlightedColor = new Color(0.7f, 0.7f, 0.7f);
            }

            btn.colors = colors;
        }
    }
    

    private void Start()
    {
        // 버튼 클릭 이벤트 등록
        buttonSettlement.onClick.AddListener(OnClickShowSettlement);
        buttonShop.onClick.AddListener(OnClickShowShop);
        buttonClosing.onClick.AddListener(OnClickShowClosing);
        buttonPayTax.onClick.AddListener(PayTax);

        // 초기 화면 = 정산 화면
        OnClickShowSettlement();
        InitializeTaxPayButton();

        checkAllToggle.onValueChanged.AddListener(OnToggleAllChanged);
        checkReciptToggle.onValueChanged.AddListener(_ => OnIndividualToggleChanged());
        checkTaxToggle.onValueChanged.AddListener(_ => OnIndividualToggleChanged());
        checkIngredientToggle.onValueChanged.AddListener(_ => OnIndividualToggleChanged());
    }

    private void OnToggleAllChanged(bool isOn)
    {
        if (isUpdating) return;
        isUpdating = true;

        checkReciptToggle.isOn = isOn;
        checkTaxToggle.isOn = isOn;
        checkIngredientToggle.isOn = isOn;

        isUpdating = false;
    }

    private void OnIndividualToggleChanged()
    {
        if (isUpdating) return;
        isUpdating = true;

        bool allOn = checkReciptToggle.isOn && checkTaxToggle.isOn && checkIngredientToggle.isOn;
        checkAllToggle.isOn = allOn;

        isUpdating = false;
    }

    private void ShowOnly(GameObject target)
    {
        scrollViewSettlement.SetActive(target == scrollViewSettlement);
        scrollViewShop.SetActive(target == scrollViewShop);
        scrollViewClosing.SetActive(target == scrollViewClosing);
    }
    public void OnClickShowSettlement()
    {
        ShowOnly(scrollViewSettlement);
        SetActiveTab(buttonSettlement);
    }

    public void OnClickShowShop()
    {
        ShowOnly(scrollViewShop);
        SetActiveTab(buttonShop);
    }

    public void OnClickShowClosing()
    {
        ShowOnly(scrollViewClosing);
        SetActiveTab(buttonClosing);
    }



    public void FillReceiptTexts()
    {
        if (ReceiptLineManager.Instance != null)
        {
            textSuccess.text = ReceiptLineManager.Instance.GetTodaySuccessfulReceiptsText();
            textmissed.text = ReceiptLineManager.Instance.GetTodayMissedReceiptsText();
        }
    }

    public void FillIngredientTexts()
    {
        // 주문 필요 재료 텍스트 업데이트
        IngredientStockManager.Instance.UpdateLowStockList(); // 목록 갱신
        lowStockTextUI.text = IngredientStockManager.Instance.GetLowStockText();
    }
    public void FillIngredientCostText()
    {
        string costText = IngredientStockManager.Instance.GetLowStockCostSummaryText();
        textLowStockCost.text = costText;
    }

    public void FillTaxText()
    {
        int tax = PlayerWalletManager.Instance.LastPaidTaxAmount;   //정기 지출 내역
        textTax.text = $"세금: {tax:N0}원";  
    }

    public void FillTodayEarningsText()
    {
        int earning = PlayerWalletManager.Instance.TodayEarnedAmount;   //하루 수익 내역
        textTodayEarnings.text = $"+ {earning:N0}원";
    }

    public void PayTax()
    {
        // 이미 납부했다면 (버튼이 비활성화)
        if (buttonPayTax == null || !buttonPayTax.interactable)
            return;

        int taxAmount = PlayerWalletManager.Instance.LastPaidTaxAmount;

        if (PlayerWalletManager.Instance.Spend(taxAmount))
        {
            Debug.Log($"[세금 납부] {taxAmount:N0}원 납부 완료");

            // 텍스트 변경 + 버튼 비활성화
            if (textPayTaxButton != null)
                textPayTaxButton.text = "납부 완료";
            buttonPayTax.interactable = false;
        }
        else
        {
            Debug.LogWarning("[세금 납부 실패] 잔고 부족");
        }
    }

    public void InitializeTaxPayButton()
    {
        if (textPayTaxButton != null)
            textPayTaxButton.text = "납부하기";

        if (buttonPayTax != null)
            buttonPayTax.interactable = true;
    }


}
