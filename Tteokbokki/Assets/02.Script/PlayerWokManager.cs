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
            Debug.LogWarning("[Wok] ���õ� ȭ���� �����ϴ�!");
            return;
        }

        var ingredients = GetPlayerIngredients();

        if (!ContainsBaseIngredients(ingredients))
        {
            Debug.LogWarning("[Wok] �⺻ ��ᰡ �����Ͽ� ������ �� �����ϴ�!");
            // TODO: ���ϸ� UI�� ��� �޽��� ��µ� ����
            return;
        }

        stoveManager.StartCookingOnSelectedSlot(ingredients);
        ClearWok();  // ���� ���� �� wok �ʱ�ȭ
    }

    private bool ContainsBaseIngredients(Dictionary<string, int> wok)
    {
        var baseMenu = MenuDatabase.Menus["���� ������"];
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
            playerIngredientsText.text = "���� ���� ��� ����";
            return;
        }

        string result = "���� ���� ���:\n";
        foreach (var item in playerIngredients)
        {
            result += $"{item.Key} x{item.Value}\n";
        }
        playerIngredientsText.text = result;
    }
}
