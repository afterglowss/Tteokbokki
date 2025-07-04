using UnityEngine;
using UnityEngine.UI;

public class IngredientButton : MonoBehaviour
{
    public PlayerWokManager playerWokManager;

    public void OnButtonClick()
    {
        string ingredientName = this.name;

        bool success = IngredientStockManager.Instance.UseIngredient(ingredientName);

        if (success)
        {
            playerWokManager.AddIngredient(ingredientName);
        }
        else
        {
            TooltipManager.Instance?.Show($"{ingredientName} ��� �����մϴ�!");
            Debug.LogWarning($"'{ingredientName}' ��� ���� - �߰� ����");
        }
    }
}