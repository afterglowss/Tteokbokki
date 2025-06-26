using UnityEngine;
using System.Collections.Generic;

public class ReceiptLineManager : MonoBehaviour
{
    public GameObject receiptPrefab;   // ������ UI ������
    public Transform receiptLineParent; // ���������� �Ŵ޸� �θ� (��)

    private List<ReceiptLineItem> activeReceipts = new();

    public float cookLimitMinutes = 30f;

    public ReceiptPopup receiptPopup;
    public CombinedIngredientManager combinedIngredientManager;

    public void AddNewReceipt(Receipt receipt)
    {
        var obj = Instantiate(receiptPrefab, receiptLineParent);
        var lineItem = obj.GetComponent<ReceiptLineItem>();

        lineItem.Setup(receipt, cookLimitMinutes, this, receiptPopup, combinedIngredientManager);
        activeReceipts.Add(lineItem);
    }

    public void RemoveReceiptByOrderID(int orderID)
    {
        var target = activeReceipts.Find(r => r.GetReceipt().OrderID == orderID);
        if (target != null)
        {
            RemoveReceipt(target);
        }
    }

    public void RemoveReceipt(ReceiptLineItem item)
    {
        if (ReceiptStateManager.Instance.ActiveReceipt == item.GetReceipt())
        {
            ReceiptStateManager.Instance.ClearActiveReceipt();
            receiptPopup.Close(); // �˾� �ݱ�
        }

        activeReceipts.Remove(item);
        Destroy(item.gameObject);
    }


    public void ClearAllReceipts()
    {
        foreach (var item in activeReceipts)
        {
            Destroy(item.gameObject);
        }
        activeReceipts.Clear();
    }
}
