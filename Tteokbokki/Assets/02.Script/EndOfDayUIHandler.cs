using System.Security.Policy;
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


    [Header("성공/실패 영수증 텍스트")]
    [SerializeField] private TextMeshProUGUI successText;
    [SerializeField] private TextMeshProUGUI missedText;

    [Header("주문 필요재료 텍스트")]
    [SerializeField] private TextMeshProUGUI lowStockTextUI;

    [Header("정기 지출 텍스트")]
    [SerializeField] private TextMeshProUGUI taxText;

    private void Start()
    {
        // 버튼 클릭 이벤트 등록
        buttonSettlement.onClick.AddListener(OnClickShowSettlement);
        buttonShop.onClick.AddListener(OnClickShowShop);
        buttonClosing.onClick.AddListener(OnClickShowClosing);

        // 초기 화면 = 정산 화면
        OnClickShowSettlement();
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
            successText.text = ReceiptLineManager.Instance.GetTodaySuccessfulReceiptsText();
            missedText.text = ReceiptLineManager.Instance.GetTodayMissedReceiptsText();
        }
    }

    public void FillIngredientTexts()
    {
        // 주문 필요 재료 텍스트 업데이트
        IngredientStockManager.Instance.UpdateLowStockList(); // 목록 갱신
        lowStockTextUI.text = IngredientStockManager.Instance.GetLowStockText();
    }

    public void FillTaxText()
    {
        int tax = PlayerWalletManager.Instance.LastPaidTaxAmount;
        taxText.text = $"{tax:N0}원";    //정기 지출 내역
    }
}
