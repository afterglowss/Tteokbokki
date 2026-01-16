using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IngredientButton : MonoBehaviour
{
    [Header("Data")]
    public string ingredientName;

    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI stockText;
    public Image iconImage; // ✨ [NEW] 재료 이미지를 표시할 Image 컴포넌트

    private void Start()
    {
        IngredientStockManager.Instance.RegisterIngredientButton(this);
        IngredientStockManager.Instance.UpdateStockText(ingredientName);
    }

    public void UpdateStockDisplay(string text)
    {
        if (stockText != null) stockText.text = text;
    }

    public void OnButtonClick()
    {
        if (string.IsNullOrEmpty(ingredientName)) return;

        bool success = IngredientStockManager.Instance.UseIngredient(ingredientName);

        if (success)
        {
            StoveManager.Instance.AddIngredientToSelectedSlot(ingredientName);
        }
        else
        {
            TooltipManager.ShowFollowMouse(TooltipType.UI, $"{ingredientName} 재고가 부족합니다!", 1f);
        }
    }

    // ✨ [수정] 외부에서 이미지까지 받아서 세팅
    public void Setup(string name, Sprite sprite)
    {
        ingredientName = name;

        if (nameText != null) nameText.text = name;

        if (iconImage != null)
        {
            if (sprite != null)
            {
                iconImage.sprite = sprite;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                // 이미지가 없으면 텍스트만 보여주기 위해 끔 (선택사항)
                iconImage.gameObject.SetActive(false);
            }
        }

        gameObject.name = $"{name}_Button";
    }
}