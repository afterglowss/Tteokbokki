using UnityEngine;
using UnityEngine.EventSystems;

public class TrashBinSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // 1. 음식 카드 버리기 (기존 로직)
        var food = eventData.pointerDrag?.GetComponent<CookedFoodUI>();
        if (food != null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(114);

            // ... (기존 음식 버리기 코드 유지) ...
            if (food.originStoveSlot != null) food.originStoveSlot.ResetSlot();
            if (food.currentSlot != null) food.currentSlot.RemoveFood(food);
            Destroy(food.gameObject);
            return;
        }

        // 2. 영수증 버리기 (기존 로직)
        var receiptItem = eventData.pointerDrag?.GetComponent<ReceiptLineItem>();
        if (receiptItem != null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(114);

            // ... (기존 영수증 버리기 코드 유지) ...
            ReceiptLineManager.Instance.RecordFailedReceipt(receiptItem.GetReceipt());
            ReceiptLineManager.Instance.RemoveReceipt(receiptItem);
            return;
        }

        // ✨ 3. [NEW] 화구의 Wok(재료 담는 중) 버리기
        var stoveSlot = eventData.pointerDrag?.GetComponent<StoveSlot>();
        // 만약 StoveSlot 자체가 아니라 Wok 아이콘에 스크립트가 있다면 GetComponentInParent 사용 등 조정 필요
        // 위 StoveSlot 코드에서는 StoveSlot 오브젝트 자체가 드래그 핸들러를 가짐.

        if (stoveSlot != null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(114);

            Debug.Log($"[휴지통] {stoveSlot.name}의 재료를 비웁니다.");
            stoveSlot.ClearPending(); // 재료 비우기
            return;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager.ShowFollowMouse(TooltipType.UI, "휴지통에 버리기");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Hide(TooltipType.UI);
    }
}