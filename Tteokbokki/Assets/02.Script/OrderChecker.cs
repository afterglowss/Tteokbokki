using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OrderChecker : MonoBehaviour
{
    public PlayerWokManager playerWokManager;
    public TextMeshProUGUI resultText;
    public StoveManager stoveManager;  // 화구 관리

    public void CheckOrder()
    {
        var activeReceipt = ReceiptStateManager.Instance.ActiveReceipt;

        if (activeReceipt == null)
        {
            resultText.text = "현재 활성화된 영수증이 없습니다!";
            return;
        }

        var playerIngredients = playerWokManager.GetPlayerIngredients();

        // 1️⃣ 모든 메뉴를 끝까지 체크하는 구조로 변경
        OrderItem targetOrder = null;

        foreach (var order in activeReceipt.GetOrders())
        {
            var combined = CombinedIngredientManager.GetCombinedIngredients(order.Menu, order.GetExtras());

            if (AreIngredientsEqual(playerIngredients, combined))
            {
                if (order.IsCompleted)
                {
                    // 이미 완료된 메뉴는 스킵, 하지만 '이미 완료' 안내는 표시
                    resultText.text = $"[{order.ItemID}] {order.Menu.Name}는 이미 조리 완료된 메뉴입니다!";
                    playerWokManager.ClearWok();
                    continue;  // 즉시 종료 (이미 완료된 메뉴가 우선)
                }

                if (order.IsOnStove)
                {
                    // 이미 조리 중인 메뉴도 스킵, 하지만 '이미 조리중' 안내는 표시
                    resultText.text = $"[{order.ItemID}] {order.Menu.Name}는 이미 화구에서 조리 중입니다!";
                    playerWokManager.ClearWok();
                    continue;  // 즉시 종료 (이미 조리중 메뉴가 우선)
                }

                // 미완료 + 미조리인 경우 일치하는 메뉴 발견
                targetOrder = order;
                break;  // 일치하는 메뉴 발견 시 반복 종료 (맨 앞에 있는 메뉴부터 처리)
            }
        }

        if (targetOrder != null)
        {
            // 2️⃣ 찾은 메뉴를 화구에 올리고 성공 처리
            resultText.text = $"성공! [{targetOrder.ItemID}] {targetOrder.Menu.Name} 조리 시작! 화구에 올렸습니다.";

            //stoveManager.TryStartCooking(targetOrder, 5 * 60);  // 5분 조리

            playerWokManager.ClearWok();
        }
        else
        {
            // 3️⃣ 끝까지 뒤져도 해당하는 메뉴가 없을 때
            resultText.text = "실패! 정확히 일치하는 미완료 메뉴가 없습니다.";
            playerWokManager.ClearWok();
        }
    }

    private bool AreIngredientsEqual(Dictionary<string, int> a, Dictionary<string, int> b)
    {
        if (a.Count != b.Count)
            return false;

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
