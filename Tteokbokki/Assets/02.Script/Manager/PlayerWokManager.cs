using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerWokManager : MonoBehaviour
{
    public static PlayerWokManager Instance { get; private set; }
    public TextMeshProUGUI playerIngredientsText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void OnCookButtonPressed()
    {
        if (!StoveManager.Instance.HasSelectedSlot())
        {
            Debug.LogWarning("선택된 화구가 없습니다!");
            TooltipManager.ShowFollowMouse(TooltipType.UI, "화구를 먼저 선택해주세요!", 1f);
            return;
        }

        StoveManager.Instance.TryCookSelectedSlot();
    }

    // ✨ [수정] 제목(statusTitle)을 받을 수 있도록 매개변수 추가 (기본값 설정)
    public void UpdateUI(Dictionary<string, int> ingredients, string statusTitle = "현재 담은 재료")
    {
        if (playerIngredientsText == null) return;

        if (ingredients == null || ingredients.Count == 0)
        {
            // 재료가 없을 때도 제목은 반영
            playerIngredientsText.text = $"{statusTitle}:\n없음";
            return;
        }

        string result = $"{statusTitle}:\n";
        foreach (var item in ingredients)
        {
            result += $"{item.Key} x{item.Value}\n";
        }
        playerIngredientsText.text = result;
    }

    public bool CheckRecipe(Dictionary<string, int> pendingIngredients)
    {
        return ContainsBaseIngredients(pendingIngredients);
    }

    private bool ContainsBaseIngredients(Dictionary<string, int> wok)
    {
        if (wok == null) return false;

        // 1. 필수 4대 재료 검사 (떡, 오뎅, 파, 양배추)
        var essentialIngredients = new Dictionary<string, int>
        {
            { "떡", 2 },
            { "오뎅", 2 },
            { "파", 1 },
            { "양배추", 1 }
        };

        foreach (var pair in essentialIngredients)
        {
            if (!wok.TryGetValue(pair.Key, out int amount) || amount < pair.Value)
            {
                // 디버깅용 로그 (필요시 주석 해제)
                // Debug.Log($"[조리 불가] 필수 재료 부족: {pair.Key} (필요: {pair.Value}, 현재: {amount})");
                return false;
            }
        }

        // 2. 소스 검사 (7종 중 하나라도 1개 이상 있어야 함)
        // IngredientEconomyDatabase에 있는 소스 목록을 기준으로 합니다.
        string[] sauces = new string[]
        {
            "군자 소스", "마라 소스", "로제 소스", "크림 소스",
            "간장 소스", "카레 소스", "짜장 소스"
        };

        bool hasSauce = false;
        foreach (var sauce in sauces)
        {
            if (wok.ContainsKey(sauce) && wok[sauce] > 0)
            {
                hasSauce = true;
                break; // 소스가 하나라도 발견되면 즉시 통과
            }
        }

        if (!hasSauce)
        {
            // Debug.Log("[조리 불가] 소스가 하나도 없습니다.");
            return false;
        }

        return true; // 모든 조건 통과
    }
}