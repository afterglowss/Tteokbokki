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

        var baseMenu = MenuDatabase.Menus["군자 떡볶이"];
        var baseIngredients = baseMenu.DefaultIngredients;

        foreach (var pair in baseIngredients)
        {
            if (!wok.TryGetValue(pair.Key, out int amount) || amount < pair.Value)
            {
                return false;
            }
        }
        return true;
    }
}