using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OrderChecker : MonoBehaviour
{
    // public PlayerWokManager playerWokManager; // 더 이상 참조 안 함
    public TextMeshProUGUI resultText;
    public StoveManager stoveManager;

    public void CheckOrder()
    {
        // 1. 활성 영수증 확인
        var activeReceipt = ReceiptStateManager.Instance.ActiveReceipt;
        if (activeReceipt == null)
        {
            resultText.text = "현재 활성화된 영수증이 없습니다!";
            return;
        }

        // 2. 선택된 화구 확인
        if (!StoveManager.Instance.HasSelectedSlot())
        {
            resultText.text = "선택된 화구가 없습니다!";
            return;
        }

        // ✨ 수정됨: 선택된 화구의 대기 중인 재료를 가져옴 (GetPendingIngredientsCopy 메서드 필요 -> StoveSlot에 추가할 것)
        // 만약 StoveSlot에 해당 getter가 없다면 아래 StoveSlot 수정 코드 참고
        var currentStove = StoveManager.Instance.GetSelectedSlot();
        var pendingIngredients = currentStove.GetPendingIngredientsCopy();

        if (pendingIngredients.Count == 0)
        {
            resultText.text = "선택된 화구에 재료가 없습니다.";
            return;
        }

        // 3. 메뉴 매칭 로직
        OrderItem targetOrder = null;

        foreach (var order in activeReceipt.GetOrders())
        {
            var combined = CombinedIngredientManager.GetCombinedIngredients(order.Menu, order.GetExtras());

            if (AreIngredientsEqual(pendingIngredients, combined))
            {
                if (order.IsCompleted)
                {
                    resultText.text = $"[{order.ItemID}] {order.Menu.Name}는 이미 조리 완료된 메뉴입니다!";
                    // playerWokManager.ClearWok(); // 삭제됨 -> 화구 비우기는 수동 or 로직에 따름
                    continue;
                }

                if (order.IsOnStove)
                {
                    resultText.text = $"[{order.ItemID}] {order.Menu.Name}는 이미 화구에서 조리 중입니다!";
                    continue;
                }

                targetOrder = order;
                break;
            }
        }

        if (targetOrder != null)
        {
            resultText.text = $"성공! [{targetOrder.ItemID}] {targetOrder.Menu.Name} 조리 시작! 화구에 올렸습니다.";

            // ✨ 수정됨: 화구 스스로 조리 시작 시도
            currentStove.TryStartCooking();
        }
        else
        {
            resultText.text = "실패! 재료가 일치하는 미완료 메뉴가 없습니다.";
            // currentStove.ClearPending(); // 원하면 실패 시 비우기 가능
        }
    }

    private bool AreIngredientsEqual(Dictionary<string, int> a, Dictionary<string, int> b)
    {
        if (a.Count != b.Count) return false;

        foreach (var kvp in a)
        {
            if (!b.TryGetValue(kvp.Key, out int count) || count != kvp.Value)
            {
                return false;
            }
        }
        return true;
    }
}