using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerWokManager : MonoBehaviour
{
    private Dictionary<string, int> playerIngredients = new Dictionary<string, int>();

    public TextMeshProUGUI playerIngredientsText;

    public StoveManager stoveManager;

    public void OnCookButtonPressed()
    {
        if (!stoveManager.HasSelectedSlot())
        {
            Debug.LogWarning("[Wok] 선택된 화구가 없습니다!");
            return;
        }

        var ingredients = GetPlayerIngredients();

        if (!ContainsBaseIngredients(ingredients))
        {
            Debug.LogWarning("[Wok] 기본 재료가 부족하여 조리할 수 없습니다!");
            // TODO: 원하면 UI에 경고 메시지 출력도 가능
            return;
        }

        stoveManager.StartCookingOnSelectedSlot(ingredients);
        ClearWok();  // 조리 시작 후 wok 초기화
    }

    private bool ContainsBaseIngredients(Dictionary<string, int> wok)
    {
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
    public void AddIngredient(string ingredientName)
    {
        if (!playerIngredients.ContainsKey(ingredientName))
        {
            playerIngredients[ingredientName] = 0;
        }
        playerIngredients[ingredientName]++;
        UpdateIngredientText();
    }

    public Dictionary<string, int> GetPlayerIngredients()
    {
        return new Dictionary<string, int>(playerIngredients);
    }

    public void ClearWok()
    {
        playerIngredients.Clear();
        UpdateIngredientText();
    }

    private void UpdateIngredientText()
    {
        if (playerIngredientsText == null) return;

        if (playerIngredients.Count == 0)
        {
            playerIngredientsText.text = "현재 담은 재료 없음";
            return;
        }

        string result = "현재 담은 재료:\n";
        foreach (var item in playerIngredients)
        {
            result += $"{item.Key} x{item.Value}\n";
        }
        playerIngredientsText.text = result;
    }
}
