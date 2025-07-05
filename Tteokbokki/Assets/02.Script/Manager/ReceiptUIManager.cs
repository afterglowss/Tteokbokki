using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ReceiptUIManager : MonoBehaviour
{
    public TextMeshProUGUI receiptText;  // Content 안에 배치된 TextMeshProUGUI
    public TextMeshProUGUI isCookedOrNotText;    // 조리가 완료된 상태인지 확인할 수 있는 텍스트
    public ScrollRect scrollViewRecentReceipt;        // ScrollView 본체 (스크롤 제어용)

    public void ShowReceiptText(string content)
    {
        receiptText.text = content;
        Canvas.ForceUpdateCanvases();  // 즉시 레이아웃 갱신
        scrollViewRecentReceipt.verticalNormalizedPosition = 1f;  // 스크롤 최상단 이동
    }
    public void UpdateIsCookedDisplay(Receipt receipt)
    {
        if (receipt == null)
        {
            Debug.Log("없는 주문번호 입니다.");
            return;
        }
        string isCooked = $"=== 주문번호: {receipt.OrderID} ===\n";
        isCooked += $"주문일시: {receipt.OrderDateTime:yyyy-MM-dd HH:mm}\n\n";

        foreach (var order in receipt.GetOrders())
        {
            if (order.IsCompleted)
            {
                isCooked += $"<color=#888888><size=90%>[{order.ItemID}] {order.Menu.Name} - 조리 완료</size></color>\n";
            }
            else
            {
                isCooked += $"[{order.ItemID}] {order.Menu.Name} - 준비중\n";
            }
        }

        isCookedOrNotText.text = isCooked;
    }

}
