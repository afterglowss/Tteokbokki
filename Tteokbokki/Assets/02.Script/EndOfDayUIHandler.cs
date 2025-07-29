using System.Security.Policy;
using TMPro;
using UnityEngine;

public class EndOfDayUIHandler : MonoBehaviour
{
    [Header("성공/실패 영수증 텍스트")]
    [SerializeField] private TextMeshProUGUI successText;
    [SerializeField] private TextMeshProUGUI missedText;

    [Header("주문 필요재료 텍스트")]
    [SerializeField] private TextMeshProUGUI lowStockTextUI;

    [Header("정기 지출 텍스트")]
    [SerializeField] private TextMeshProUGUI taxText;


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
