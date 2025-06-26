using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CombinedIngredientManager : MonoBehaviour
{
    public TextMeshProUGUI combinedIngredientsText;  // UI 연결 (Inspector에서 할당)

    // 메뉴 기본재료 + 추가재료 합산하는 함수
    public static Dictionary<string, int> GetCombinedIngredients(MenuItem menu, Dictionary<string, int> extras)
    {
        Dictionary<string, int> combined = new Dictionary<string, int>();

        // 기본 재료 추가
        foreach (var pair in menu.DefaultIngredients)
        {
            if (!combined.ContainsKey(pair.Key))
            {
                combined[pair.Key] = 0;
            }
            combined[pair.Key] += pair.Value;
        }

        // 추가 재료 추가
        foreach (var extra in extras)
        {
            if (!combined.ContainsKey(extra.Key))
            {
                combined[extra.Key] = 0;
            }
            combined[extra.Key] += extra.Value;
        }

        return combined;
    }

    // 합산된 재료 리스트를 텍스트로 변환하는 함수
    public static string GetIngredientsText(Dictionary<string, int> combinedIngredients)
    {
        string result = "";
        foreach (var pair in combinedIngredients)
        {
            result += $"{pair.Key} x{pair.Value}\n";
        }
        return result;
    }

    // 하나의 영수증에 있는 **모든 메뉴**의 재료 합산 결과를 출력하는 함수
    public void DisplayAllCombinedIngredients(Receipt receipt)
    {
        string result = $"주문번호 {receipt.OrderID}의 메뉴별 재료 목록\n\n";

        foreach (var order in receipt.GetOrders())
        {
            var combined = GetCombinedIngredients(order.Menu, order.GetExtras());
            result += $"[{order.ItemID}] {order.Menu.Name} 전체 재료 목록\n";
            result += GetIngredientsText(combined);
            result += "\n";
        }

        combinedIngredientsText.text = result;
    }
    public void ClearIngredientsText()
    {
        combinedIngredientsText.text = "활성화된 영수증이 없습니다.";
    }
}
