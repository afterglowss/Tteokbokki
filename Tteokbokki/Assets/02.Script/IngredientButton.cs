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

    public TextMeshProUGUI hotkeyText;

    private void Start()
    {
        IngredientStockManager.Instance.RegisterIngredientButton(this);
        IngredientStockManager.Instance.UpdateStockText(ingredientName);
    }

    public void UpdateStockDisplay(string text)
    {
        if (stockText != null) stockText.text = text;
    }

    // ✨ [핵심 수정] 버튼 클릭 로직 변경
    public void OnButtonClick()
    {
        if (string.IsNullOrEmpty(ingredientName)) return;

        // 1. 화구 선택 여부 확인
        if (!StoveManager.Instance.HasSelectedSlot())
        {
            TooltipManager.ShowFollowMouse(TooltipType.UI, "화구를 먼저 선택해주세요 (1~5)", 1f);
            return; // ❌ 화구가 없으면 재고 차감하지 않고 종료
        }

        // 2. 선택된 화구의 상태 확인 (조리 중이거나, 음식이 완료되어 올려진 상태인지)
        StoveSlot selectedSlot = StoveManager.Instance.GetSelectedSlot();
        if (selectedSlot.IsCooking || selectedSlot.IsCooked)
        {
            TooltipManager.ShowFollowMouse(TooltipType.UI, "조리 중이거나 음식이 있는 화구입니다!", 1f);
            return; // ❌ 이미 사용 중인 화구면 재고 차감하지 않고 종료
        }

        // ✨ [NEW] 화구 내 재료 최대 개수(10개) 제한 체크! (재고 차감 전에 해야 함)
        if (!selectedSlot.CanAddIngredient(ingredientName))
        {
            TooltipManager.ShowFollowMouse(TooltipType.UI, $"{ingredientName}은(는) 한 화구에 최대 10개까지만 넣을 수 있습니다!", 1f);
            return;
        }

        // 3. 위 조건을 모두 통과했을 때만 재고 차감 시도
        bool success = IngredientStockManager.Instance.UseIngredient(ingredientName);

        if (success)
        {
            // 4. 재고 차감 성공 시 화구에 투입
            StoveManager.Instance.AddIngredientToSelectedSlot(ingredientName);
        }
        else
        {
            // 재고 부족
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

    // ✨ [NEW] 단축키 표시 함수
    public void SetHotkeyDisplay(string key)
    {
        if (hotkeyText != null)
        {
            hotkeyText.text = key;
            hotkeyText.gameObject.SetActive(!string.IsNullOrEmpty(key));
        }
    }
}