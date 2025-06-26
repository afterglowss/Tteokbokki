using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReceiptPopup : MonoBehaviour
{
    public TextMeshProUGUI receiptText; // ������ ����
    public Button closeButton;          // �˾�â Close ��ư
    public ScrollRect scrollRect;       // ScrollView ��ü (��ũ�� �����)

    public event Action OnPopupClosed;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        gameObject.SetActive(false);
    }

    public void ShowReceiptText(string content)
    {
        receiptText.text = content;
        Canvas.ForceUpdateCanvases();  // ��� ���̾ƿ� ����
        scrollRect.verticalNormalizedPosition = 1f;  // ��ũ�� �ֻ�� �̵�
    }

    public void Show(Receipt receipt)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(receipt.GetReceiptText());

        foreach (var order in receipt.GetOrders())
        {
            if (order.IsOnStove)
            {
                sb.AppendLine($"[{order.ItemID}] {order.Menu.Name} - ���� ���� ��!");
            }
        }

        receiptText.text = sb.ToString();
        gameObject.SetActive(true);
    }


    public void Close()
    {
        gameObject.SetActive(false);
        //ReceiptStateManager.Instance.ClearActiveReceipt();      // �˾�â�� ���� �־�߸� ���� ����
        OnPopupClosed?.Invoke();        // UI �� �˾�â �����ٰ� ��ȣ ������
    }
}
