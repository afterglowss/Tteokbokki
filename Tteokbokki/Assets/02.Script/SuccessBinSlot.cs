using UnityEngine;
using UnityEngine.EventSystems;

public class SuccessBinSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // 드래그된 객체에서 영수증 컴포넌트 가져오기
        var receiptItem = eventData.pointerDrag?.GetComponent<ReceiptLineItem>();

        if (receiptItem != null)
        {
            Receipt receipt = receiptItem.GetReceipt();

            Debug.Log($"[Debug] 영수증 {receipt.OrderID} 강제 성공 처리 (Success Bin)");

            // 1. 성공 내역에 기록 (ReceiptLineManager)
            ReceiptLineManager.Instance.RecordSuccessfulReceipt(receipt);

            // 2. 수입 추가 (PlayerWalletManager)
            // 실제 조리된 재료 보너스는 계산할 수 없으므로, 영수증 표기 금액만 추가합니다.
            if (PlayerWalletManager.Instance != null)
            {
                PlayerWalletManager.Instance.AddIncome(receipt.GetTotalPrice());
            }

            // 3. 영수증 목록에서 제거 및 UI 파괴
            ReceiptLineManager.Instance.RemoveReceipt(receiptItem);

            // 툴팁 숨기기
            TooltipManager.Hide(TooltipType.UI);
            return;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 마우스 올렸을 때 툴팁 표시
        TooltipManager.ShowFollowMouse(TooltipType.UI, "강제 성공 처리 (디버그용)");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Hide(TooltipType.UI);
    }
}