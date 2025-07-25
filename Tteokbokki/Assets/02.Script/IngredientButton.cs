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
            IngredientSpawner.Instance.SpawnIngredient(ingredientName); // 재료 사용 후 오브젝트 생성
        }
        else
        {
            TooltipManager.ShowFollowMouse(TooltipType.UI, $"{ingredientName} 재고가 부족합니다!", 1f);
            Debug.LogWarning($"'{ingredientName}' 재고 부족 - 추가 실패");
        }
    }
}