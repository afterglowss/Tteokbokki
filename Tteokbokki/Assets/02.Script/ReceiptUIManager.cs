using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ReceiptUIManager : MonoBehaviour
{
    public TextMeshProUGUI receiptText;  // Content �ȿ� ��ġ�� TextMeshProUGUI
    public TextMeshProUGUI isCookedText;    // ������ �Ϸ�� �������� Ȯ���� �� �ִ� �ؽ�Ʈ
    public ScrollRect scrollRect;        // ScrollView ��ü (��ũ�� �����)

    public void ShowReceiptText(string content)
    {
        receiptText.text = content;
        Canvas.ForceUpdateCanvases();  // ��� ���̾ƿ� ����
        scrollRect.verticalNormalizedPosition = 1f;  // ��ũ�� �ֻ�� �̵�
    }
    public void UpdateIsCookedDisplay(Receipt receipt)
    {
        if (receipt == null)
        {
            Debug.Log("���� �ֹ���ȣ �Դϴ�.");
            return;
        }
        string isCooked = $"=== �ֹ���ȣ: {receipt.OrderID} ===\n";
        isCooked += $"�ֹ��Ͻ�: {receipt.OrderDateTime:yyyy-MM-dd HH:mm}\n\n";

        foreach (var order in receipt.GetOrders())
        {
            if (order.IsCompleted)
            {
                isCooked += $"<color=#888888><size=90%>[{order.ItemID}] {order.Menu.Name} - ���� �Ϸ�</size></color>\n";
            }
            else
            {
                isCooked += $"[{order.ItemID}] {order.Menu.Name} - �غ���\n";
            }
        }

        isCookedText.text = isCooked;
    }

}
