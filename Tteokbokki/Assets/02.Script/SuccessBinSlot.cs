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
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(114);

            Receipt receipt = receiptItem.GetReceipt();

            Debug.Log($"[Debug] 영수증 {receipt.OrderID} 강제 성공 처리 (Success Bin)");

            // 1. 성공 내역에 기록 (ReceiptLineManager)
            ReceiptLineManager.Instance.RecordSuccessfulReceipt(receipt);

            // ✨ [NEW] 보너스 금액 계산하기
            int totalBonus = 0;
            foreach (var order in receipt.GetOrders())
            {
                // CombinedIngredientManager의 유용한 함수를 빌려와서 메뉴 기본재료+추가재료를 싹 합칩니다.
                var combined = CombinedIngredientManager.GetCombinedIngredients(order.Menu, order.GetExtras());

                // 합친 재료 중에 오늘 보너스 재료가 있는지 검사해서 금액을 구함
                if (DailyBonusManager.Instance != null)
                {
                    totalBonus += DailyBonusManager.Instance.CalculateBonusFromIngredients(combined);
                }
            }

            // ✨ [NEW] 매니저에 오늘치 보너스 수익 누적시키기 (마감 대시보드에 반영됨!)
            if (DailyBonusManager.Instance != null && totalBonus > 0)
            {
                DailyBonusManager.Instance.AddBonusIncome(totalBonus);
            }

            // 2. 수입 추가 (기본 영수증 금액 + 보너스 금액)
            int basePrice = receipt.GetTotalPrice();
            int finalPrice = basePrice + totalBonus;

            if (PlayerWalletManager.Instance != null)
            {
                // 지갑에 최종 금액(기본+보너스) 넣기
                PlayerWalletManager.Instance.AddIncome(finalPrice);
                EffectManager.Instance.ShowMoneyPopup(gameObject.transform.position, finalPrice);
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