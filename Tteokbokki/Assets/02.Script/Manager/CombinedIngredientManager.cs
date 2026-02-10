using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombinedIngredientManager : MonoBehaviour
{
    public TextMeshProUGUI combinedIngredientsText;  // UI 연결 (Inspector에서 할당)

    // ✨ [NEW] 텍스트가 포함된 ScrollView 오브젝트 (Inspector 연결)
    public GameObject recipeScrollView;

    public ScrollRect recipeScrollRect;

    private void Start()
    {
        // 시작할 때 안 보이게 숨김
        ClearIngredientsText();
    }

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
        if (recipeScrollView != null)
        {
            recipeScrollView.SetActive(true);

            // ✨ 코루틴 시작
            StartCoroutine(ResetScrollCoroutine());
        }

        string result = $"주문번호 {receipt.OrderID}의 메뉴별 재료\n\n";

        foreach (var order in receipt.GetOrders())
        {
            var combined = GetCombinedIngredients(order.Menu, order.GetExtras());
            result += $"[{order.ItemID}] {order.Menu.Name}\n";
            result += GetIngredientsText(combined);
            result += "\n";
        }

        combinedIngredientsText.text = result;
    }

    private IEnumerator ResetScrollCoroutine()
    {
        yield return null; // 한 프레임 대기

        if (recipeScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            recipeScrollRect.verticalNormalizedPosition = 1f;
        }
    }
    public void ClearIngredientsText()
    {
        // 1. 텍스트 비우기 (안전장치)
        if (combinedIngredientsText != null) combinedIngredientsText.text = "";

        // 2. 스크롤뷰 전체 비활성화
        if (recipeScrollView != null) recipeScrollView.SetActive(false);
    }
}
