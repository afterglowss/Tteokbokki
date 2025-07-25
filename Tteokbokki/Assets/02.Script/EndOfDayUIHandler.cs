using TMPro;
using UnityEngine;

public class EndOfDayUIHandler : MonoBehaviour
{
    [Header("성공/실패 영수증 텍스트")]
    [SerializeField] private TextMeshProUGUI successText;
    [SerializeField] private TextMeshProUGUI missedText;

    public void FillReceiptTexts()
    {
        if (ReceiptLineManager.Instance != null)
        {
            successText.text = ReceiptLineManager.Instance.GetTodaySuccessfulReceiptsText();
            missedText.text = ReceiptLineManager.Instance.GetTodayMissedReceiptsText();
        }
    }
}
