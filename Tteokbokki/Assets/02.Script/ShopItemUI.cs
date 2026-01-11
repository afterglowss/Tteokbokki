using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI 연결")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI statusText;

    [Header("이미지 및 토글")]
    public Image ingredientIconImage;
    public Toggle selectToggle;
    public Button imageButton;

    [Header("강조 효과 (아웃라인)")]
    public Outline outline;

    // ✨ 색상 설정 변수 추가
    public Color normalOutlineColor = Color.white; // 평소 색상 (예: 흰색)
    public Color lowStockOutlineColor = Color.red; // 부족할 때 색상 (빨강)

    public bool IsLowStock { get; private set; }
    public bool IsOrdered { get; private set; }

    public void Setup(IngredientMetaData data, bool isOrdered, bool isLowStock, UnityAction<bool> onToggleChanged)
    {
        IsOrdered = isOrdered;
        IsLowStock = isLowStock;

        // 1. 텍스트 설정
        nameText.text = data.Name;
        priceText.text = $"{data.OrderCost:N0}원";
        amountText.text = $"{data.ServingsPerOrder}인분";

        // 2. 이미지 설정
        Sprite icon = IngredientSpriteManager.Instance.GetSprite(data.Name);
        if (icon != null)
        {
            ingredientIconImage.sprite = icon;
            ingredientIconImage.color = Color.white;
        }
        else
        {
            ingredientIconImage.color = Color.clear;
        }

        // 3. 이미 주문한 상태 처리
        if (isOrdered)
        {
            selectToggle.interactable = false;
            selectToggle.isOn = false;
            statusText.text = "주문 완료";
            if (imageButton != null) imageButton.interactable = false;

            // ✨ 주문 완료 시: 아웃라인을 평소 색상으로 돌리거나, 아예 끄고 싶으면 enabled = false 처리
            if (outline != null)
            {
                outline.enabled = true; // 항상 켜둠
                outline.effectColor = normalOutlineColor; // 평소 색상
            }
        }
        else
        {
            selectToggle.interactable = true;
            selectToggle.SetIsOnWithoutNotify(false);
            statusText.text = "";
            if (imageButton != null) imageButton.interactable = true;

            // ✨ 4. 아웃라인 색상 변경 로직 (핵심!)
            if (outline != null)
            {
                outline.enabled = true; // 항상 켜둠

                if (isLowStock)
                {
                    // 부족하면 설정한 경고 색상(빨강)
                    outline.effectColor = lowStockOutlineColor;
                }
                else
                {
                    // 부족하지 않으면 평소 색상(흰색 등)
                    outline.effectColor = normalOutlineColor;
                }
            }
        }

        // 5. 토글 이벤트 연결
        selectToggle.onValueChanged.RemoveAllListeners();
        selectToggle.onValueChanged.AddListener(onToggleChanged);

        // 6. 이미지 클릭 연결
        if (imageButton != null)
        {
            imageButton.onClick.RemoveAllListeners();
            imageButton.onClick.AddListener(() =>
            {
                selectToggle.isOn = !selectToggle.isOn;
            });
        }
    }

    public void SetToggle(bool isOn)
    {
        if (selectToggle.interactable)
        {
            selectToggle.isOn = isOn;
        }
    }
}