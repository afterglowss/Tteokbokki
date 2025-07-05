using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 음식카드(CookedFoodUI)나 영수증(ReceiptLineItem)을 드래그해서 버릴 수 있는 휴지통 슬롯
/// </summary>
public class TrashBinSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // 음식카드 버리기
        var food = eventData.pointerDrag?.GetComponent<CookedFoodUI>();
        if (food != null)
        {
            if (food.originStoveSlot != null)
            {
                food.originStoveSlot.ResetSlot();  // 화구 초기화
            }

            if (food.currentSlot != null)
            {
                food.currentSlot.RemoveFood(food);  // 리스트에서 제거 + 정렬
            }

            TooltipManager.Instance.Hide();
            Destroy(food.gameObject);
            Debug.Log("[휴지통] 음식카드가 버려졌습니다.");
            return;
        }

        // 영수증 버리기 (주문 포기)
        var receiptItem = eventData.pointerDrag?.GetComponent<ReceiptLineItem>();
        if (receiptItem != null)
        {
            var receipt = receiptItem.GetReceipt();
            ReceiptLineManager.Instance.RecordFailedReceipt(receipt);
            ReceiptLineManager.Instance.RemoveReceipt(receiptItem);  

            TooltipManager.Instance.Hide();
            Debug.Log($"[휴지통] 영수증 {receipt.OrderID}번이 포기되어 실패 목록에 추가되었습니다.");
            return;
        }

        Debug.Log("[휴지통] 유효하지 않은 드래그 대상입니다.");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager.Instance.Show("휴지통에 버리기");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.Hide();
    }
}
