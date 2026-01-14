using UnityEngine;
using UnityEngine.UI;

public class IngredientButton : MonoBehaviour
{
    // public PlayerWokManager playerWokManager; // 이제 필요 없음 (StoveManager 싱글톤 사용)

    public void OnButtonClick()
    {
        string ingredientName = this.name;

        // 1. 재고 확인 및 차감
        bool success = IngredientStockManager.Instance.UseIngredient(ingredientName);

        if (success)
        {
            // ✨ [변경] StoveManager를 통해 현재 선택된 화구에 추가
            StoveManager.Instance.AddIngredientToSelectedSlot(ingredientName);
        }
        else
        {
            TooltipManager.ShowFollowMouse(TooltipType.UI, $"{ingredientName} 재고가 부족합니다!", 1f);
            Debug.LogWarning($"'{ingredientName}' 재고 부족 - 추가 실패");
        }
    }
}