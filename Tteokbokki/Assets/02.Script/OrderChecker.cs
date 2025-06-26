using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OrderChecker : MonoBehaviour
{
    public PlayerWokManager playerWokManager;
    public TextMeshProUGUI resultText;
    public StoveManager stoveManager;  // 화구 관리

    //public void CheckOrder()
    //{
    //    var activeReceipt = ReceiptStateManager.Instance.ActiveReceipt;

    //    if (activeReceipt == null)
    //    {
    //        resultText.text = "현재 활성화된 영수증이 없습니다!";
    //        return;
    //    }

    //    var playerIngredients = playerWokManager.GetPlayerIngredients();

    //    // [1] 이미 완료된 메뉴와 재료가 일치하는지 먼저 체크
    //    foreach (var order in activeReceipt.GetOrders())
    //    {
    //        if (!order.IsCompleted) continue;

    //        var combined = CombinedIngredientManager.GetCombinedIngredients(order.Menu, order.GetExtras());
    //        if (AreIngredientsEqual(playerIngredients, combined))
    //        {
    //            resultText.text = $"[{order.ItemID}] {order.Menu.Name}는 이미 조리 완료된 메뉴입니다!";
    //            playerWokManager.ClearWok();
    //            return;  // 이미 완료된 메뉴가 있으면 즉시 반환//
    //        }
    //    }

    //    // [2] 이미 조리 중인 메뉴와 재료가 일치하는지 체크
    //    //bool foundAlreadyCooking = false;
    //    foreach (var order in activeReceipt.GetOrders())
    //    {
    //        if (!order.IsOnStove) continue;

    //        var combined = CombinedIngredientManager.GetCombinedIngredients(order.Menu, order.GetExtras());
    //        if (AreIngredientsEqual(playerIngredients, combined))
    //        {
    //            resultText.text = $"[{order.ItemID}] {order.Menu.Name}는 이미 화구에서 조리 중입니다!";
    //            playerWokManager.ClearWok();
    //            return;  // 이미 조리 중이면 즉시 반환
    //        }
    //    }

    //    // [3] 미완료 + 미조리 메뉴 중에서 정확히 일치하는 메뉴 찾기
    //    bool matchFound = false;
    //    foreach (var order in activeReceipt.GetOrders())
    //    {
    //        if (order.IsCompleted || order.IsOnStove) continue;

    //        var combined = CombinedIngredientManager.GetCombinedIngredients(order.Menu, order.GetExtras());
    //        if (AreIngredientsEqual(playerIngredients, combined))
    //        {
    //            // 성공 처리
    //            resultText.text = $"성공! [{order.ItemID}] {order.Menu.Name} 조리 시작! 화구에 올렸습니다.";

    //            // 해당 메뉴를 실제 화구에 올림 (조리 시작)
    //            stoveManager.TryStartCooking(order, 5 * 60);  // 게임시간 기준 5분 조리시간

    //            playerWokManager.ClearWok();
    //            matchFound = true;
    //            break;  // 첫 번째 일치하는 메뉴 처리 후 종료 (같은 메뉴가 또 있어도 무시)
    //        }
    //    }

    //    // [4] 만약 같은 재료 구성의 메뉴가 이미 조리 중인 게 있었다면, 안내 출력 (미완료 메뉴가 없을 경우)
    //    if (!matchFound)
    //    {
    //        resultText.text = "실패! 정확히 일치하는 미완료 메뉴가 없습니다.";
    //        playerWokManager.ClearWok();
    //    }
    //}
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

            stoveManager.TryStartCooking(targetOrder, 5 * 60);  // 5분 조리

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
