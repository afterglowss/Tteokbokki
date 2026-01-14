using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerWokManager : MonoBehaviour
{
    public static PlayerWokManager Instance { get; private set; }
    public TextMeshProUGUI playerIngredientsText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ✨ [변경] 조리 버튼 클릭 시 -> StoveManager에게 위임
    public void OnCookButtonPressed()
    {
        if (!StoveManager.Instance.HasSelectedSlot())
        {
            Debug.LogWarning("선택된 화구가 없습니다!");
            TooltipManager.ShowFollowMouse(TooltipType.UI, "화구를 먼저 선택해주세요!", 1f);
            return;
        }

        // 선택된 화구의 조리를 시도함
        StoveManager.Instance.TryCookSelectedSlot();
    }

    // ✨ [변경] UI 업데이트 전용 함수 (데이터는 StoveSlot에서 받음)
    public void UpdateUI(Dictionary<string, int> ingredients)
    {
        if (playerIngredientsText == null) return;

        if (ingredients == null || ingredients.Count == 0)
        {
            playerIngredientsText.text = "선택된 화구: 비어있음";
            return;
        }

        string result = "현재 담은 재료:\n";
        foreach (var item in ingredients)
        {
            result += $"{item.Key} x{item.Value}\n";
        }
        playerIngredientsText.text = result;
    }

    // ✨ [변경] 검증 로직만 제공 (StoveSlot이 호출해서 사용)
    public bool CheckRecipe(Dictionary<string, int> pendingIngredients)
    {
        return ContainsBaseIngredients(pendingIngredients);
    }

    // 기존 검증 로직 유지
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

    // 기존의 AddIngredient, ClearWok, GetPlayerIngredients 등은 모두 삭제됨
}