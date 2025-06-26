using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerWokManager : MonoBehaviour
{
    private Dictionary<string, int> playerIngredients = new Dictionary<string, int>();

    public TextMeshProUGUI playerIngredientsText;

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
