using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombinedIngredientManager : MonoBehaviour
{
    public TextMeshProUGUI combinedIngredientsText;  // UI 연결 (Inspector에서 할당)
    public TextMeshProUGUI totalPriceText;

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
            // ✨ 1. 재료 이름 번역기 돌리기!
            string transIngName = TextTranslator.GetIngredientName(pair.Key);
            string line = $"{transIngName} x{pair.Value}";

            if (DailyBonusManager.Instance != null && DailyBonusManager.Instance.IsBonusIngredient(pair.Key))
            {
                // ✨ 2. "(보너스)" 텍스트도 번역기로 빼주기!
                string bonusText = TextTranslator.GetUIText("UI_BonusTag");
                line = $"<color=#D95400><font=\"KimjungchulMyungjo-Bold SDF\">{transIngName} x{pair.Value}</font> {bonusText}</color>";
            }

            result += line + "\n";
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

        // 1. 시간 포맷팅 (HH:mm -> 14:30 형식)
        // 🚨 만약 Receipt 스크립트의 시간 변수명이 OrderTime이 아니라면, 해당 변수명으로 바꿔주세요.
        string timeStr = receipt.OrderDateTime.ToString("HH:mm");

        // 2. 제목에 시간 추가
        // 예: "[14:30] 주문번호 1의 메뉴별 재료"
        string result = TextTranslator.GetUIText("UI_CombinedHeader", timeStr, receipt.OrderID);

        int totalBasePrice = receipt.GetTotalPrice();
        int totalBonusAmount = 0;

        foreach (var order in receipt.GetOrders())
        {
            var combined = GetCombinedIngredients(order.Menu, order.GetExtras());
            // ✨ 4. 메뉴 이름 번역!
            string transMenuName = TextTranslator.GetMenuName(order.Menu.Name);
            result += $"[{order.ItemID}] {transMenuName}\n";
            result += GetIngredientsText(combined);
            result += "\n";

            // 2. 보너스 재료 체크 및 합산
            foreach (var kv in combined)
            {
                string ingredientName = kv.Key;
                int count = kv.Value;

                // 오늘 보너스 재료라면?
                if (DailyBonusManager.Instance != null &&
                    DailyBonusManager.Instance.IsBonusIngredient(ingredientName))
                {
                    totalBonusAmount += (count * DailyBonusManager.bonusPerIngredient);
                }
            }
        }

        combinedIngredientsText.text = result;// ✨ [UI 갱신] 총 금액 표시
        if (totalPriceText != null)
        {
            int finalTotal = totalBasePrice + totalBonusAmount;

            // ✨ 5. 총 금액 텍스트 번역!
            if (totalBonusAmount > 0)
            {
                totalPriceText.text = TextTranslator.GetUIText("UI_CombinedTotal_WithBonus", finalTotal, totalBasePrice, totalBonusAmount);
            }
            else
            {
                totalPriceText.text = TextTranslator.GetUIText("UI_CombinedTotal_Normal", finalTotal);
            }
        }
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
        if (totalPriceText != null) totalPriceText.text = "";
    }
}
