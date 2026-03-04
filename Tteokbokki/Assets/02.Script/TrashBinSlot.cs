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

            // ✨ [핵심 수정] 화구 리셋 조건 강화
            // "화구 출신이면서(origin != null)" AND "현재 포장대에 있지 않을 때(currentSlot == null)"만 리셋
            if (food.originStoveSlot != null && food.currentSlot == null)
            {
                food.originStoveSlot.ResetSlot();
            }

            // 포장대에서 왔다면 포장대 리스트에서 제거
            if (food.currentSlot != null)
            {
                food.currentSlot.RemoveFood(food);
            }
            Destroy(food.gameObject);
            AddTrashCount();
            return;
        }

        // 2. 영수증 버리기 (기존 로직)
        var receiptItem = eventData.pointerDrag?.GetComponent<ReceiptLineItem>();
        if (receiptItem != null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(114);
            // ✨ [NEW] 영수증 거절 로그 기록
            if (GameDataLogger.Instance != null)
            {
                GameDataLogger.Instance.CountFail("Trash");
            }
            ReceiptLineManager.Instance.RecordCanceledReceipt(receiptItem.GetReceipt());
            ReceiptLineManager.Instance.RemoveReceipt(receiptItem);
            return;
        }

        // ✨ 3. [NEW] 화구의 Wok(재료 담는 중) 버리기
        var stoveSlot = eventData.pointerDrag?.GetComponent<StoveSlot>();
        // 만약 StoveSlot 자체가 아니라 Wok 아이콘에 스크립트가 있다면 GetComponentInParent 사용 등 조정 필요
        // 위 StoveSlot 코드에서는 StoveSlot 오브젝트 자체가 드래그 핸들러를 가짐.

        if (stoveSlot != null)
        {
            if (stoveSlot.IsCooking)
            {
                return; // 조리 중에는 못 버림
            }
            // A. 조리 완료된 상태라면? -> 화구 초기화 (음식 버리기)
            if (stoveSlot.IsCooked)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(114); // 버리는 소리
                Debug.Log($"[휴지통] {stoveSlot.name}의 완성된 요리를 버립니다.");

                stoveSlot.ResetSlot(); // ✨ 화구 초기화!
                AddTrashCount();
                return;
            }

            // B. 준비 중(재료 담는 중) 상태라면? -> 재료 비우기
            if (stoveSlot.HasPendingIngredients)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(114);
                Debug.Log($"[휴지통] {stoveSlot.name}의 재료를 비웁니다.");

                stoveSlot.ClearPending(); // ✨ 재료만 비우기
                AddTrashCount();
                return;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager.ShowFollowMouse(TooltipType.UI, TextTranslator.GetUIText("Tooltip_TrashBin"));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Hide(TooltipType.UI);
    }

    private void AddTrashCount()
    {
        int trashCount = PlayerPrefs.GetInt("TotalTrashedItems", 0);
        trashCount++;
        PlayerPrefs.SetInt("TotalTrashedItems", trashCount);
        PlayerPrefs.Save();

        if (trashCount == 20 && AchievementManager.Instance != null)
        {
            AchievementManager.Instance.Unlock(AchievementID.gordon_ramsays_nightmare);
        }
    }
}