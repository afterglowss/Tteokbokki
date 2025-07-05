using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ����ī��(CookedFoodUI)�� ������(ReceiptLineItem)�� �巡���ؼ� ���� �� �ִ� ������ ����
/// </summary>
public class TrashBinSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // ����ī�� ������
        var food = eventData.pointerDrag?.GetComponent<CookedFoodUI>();
        if (food != null)
        {
            if (food.originStoveSlot != null)
            {
                food.originStoveSlot.ResetSlot();  // ȭ�� �ʱ�ȭ
            }

            if (food.currentSlot != null)
            {
                food.currentSlot.RemoveFood(food);  // ����Ʈ���� ���� + ����
            }

            TooltipManager.Instance.Hide();
            Destroy(food.gameObject);
            Debug.Log("[������] ����ī�尡 ���������ϴ�.");
            return;
        }

        // ������ ������ (�ֹ� ����)
        var receiptItem = eventData.pointerDrag?.GetComponent<ReceiptLineItem>();
        if (receiptItem != null)
        {
            var receipt = receiptItem.GetReceipt();
            ReceiptLineManager.Instance.RecordFailedReceipt(receipt);
            ReceiptLineManager.Instance.RemoveReceipt(receiptItem);  

            TooltipManager.Instance.Hide();
            Debug.Log($"[������] ������ {receipt.OrderID}���� ����Ǿ� ���� ��Ͽ� �߰��Ǿ����ϴ�.");
            return;
        }

        Debug.Log("[������] ��ȿ���� ���� �巡�� ����Դϴ�.");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager.Instance.Show("�����뿡 ������");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.Hide();
    }
}
