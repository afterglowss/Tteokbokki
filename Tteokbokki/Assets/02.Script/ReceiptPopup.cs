using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReceiptPopup : MonoBehaviour
{
    public TextMeshProUGUI receiptText; // 영수증 내용
    public Button closeButton;          // 팝업창 Close 버튼
    public ScrollRect scrollRect;       // ScrollView 본체 (스크롤 제어용)

    public event Action OnPopupClosed;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        gameObject.SetActive(false);
    }

    public void ShowReceiptText(string content)
    {
        receiptText.text = content;
        Canvas.ForceUpdateCanvases();  // 즉시 레이아웃 갱신
        scrollRect.verticalNormalizedPosition = 1f;  // 스크롤 최상단 이동
    }

    public void Show(Receipt receipt)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(receipt.GetReceiptText());

        foreach (var order in receipt.GetOrders())
        {
            if (order.IsOnStove)
            {
                sb.AppendLine($"[{order.ItemID}] {order.Menu.Name} - 현재 조리 중!");
            }
        }

        receiptText.text = sb.ToString();
        gameObject.SetActive(true);
    }


    public void Close()
    {
        gameObject.SetActive(false);
        //ReceiptStateManager.Instance.ClearActiveReceipt();      // 팝업창이 켜져 있어야만 조리 가능
        OnPopupClosed?.Invoke();        // UI 가 팝업창 닫혔다고 신호 보내기
    }
}
