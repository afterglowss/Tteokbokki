using UnityEngine;
using UnityEngine.EventSystems;

public class MissBinSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // 드래그된 객체에서 영수증 컴포넌트 가져오기
        var receiptItem = eventData.pointerDrag?.GetComponent<ReceiptLineItem>();

        if (receiptItem != null)
        {
            // (선택) 실패/터지는 느낌의 효과음이 있다면 번호를 바꿔주세요.
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(118);

            Receipt receipt = receiptItem.GetReceipt();

            Debug.Log($"[Debug] 영수증 {receipt.OrderID} 강제 실패(Miss) 처리 (Miss Bin)");

            // ✨ 데이터 로거에 타임아웃/실패로 카운트 (선택사항)
            if (GameDataLogger.Instance != null)
            {
                GameDataLogger.Instance.CountFail("Timeout");
            }

            // 1. 실패 내역에 기록 (ReceiptLineManager의 missedReceipts에 저장됨)
            ReceiptLineManager.Instance.RecordFailedReceipt(receipt);

            // 2. 영수증 목록에서 제거 및 UI 파괴
            ReceiptLineManager.Instance.RemoveReceipt(receiptItem);

            // 툴팁 숨기기
            TooltipManager.Hide(TooltipType.UI);
            return;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 마우스 올렸을 때 툴팁 표시
        TooltipManager.ShowFollowMouse(TooltipType.UI, "강제 실패/시간초과 처리 (디버그용)");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Hide(TooltipType.UI);
    }
}