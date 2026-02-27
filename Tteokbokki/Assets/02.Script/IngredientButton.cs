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

        // ✨ 에러 메시지에 띄울 번역된 재료 이름 미리 준비! (예: "떡" -> "Rice Cake")
        string transName = TextTranslator.GetIngredientName(ingredientName);

        // 1. 화구 선택 여부 확인
        if (!StoveManager.Instance.HasSelectedSlot())
        {
            TooltipManager.ShowFollowMouse(TooltipType.UI, TextTranslator.GetUIText("Warning_SelectStove_Key"), 1f);
            return;
        }

        // 2. 선택된 화구의 상태 확인
        StoveSlot selectedSlot = StoveManager.Instance.GetSelectedSlot();
        if (selectedSlot.IsCooking || selectedSlot.IsCooked)
        {
            TooltipManager.ShowFollowMouse(TooltipType.UI, TextTranslator.GetUIText("Warning_StoveInUse"), 1f);
            return;
        }

        // ✨ 3. 최대 개수 제한 체크
        if (!selectedSlot.CanAddIngredient(ingredientName))
        {
            TooltipManager.ShowFollowMouse(TooltipType.UI, TextTranslator.GetUIText("Warning_MaxIngredient", transName), 1f);
            return;
        }

        // 4. 재고 차감 시도
        bool success = IngredientStockManager.Instance.UseIngredient(ingredientName);

        if (success)
        {
            StoveManager.Instance.AddIngredientToSelectedSlot(ingredientName);
        }
        else
        {
            TooltipManager.ShowFollowMouse(TooltipType.UI, TextTranslator.GetUIText("Warning_OutOfStock", transName), 1f);
        }
    }

    // ✨ [수정] 외부에서 이미지까지 받아서 세팅
    public void Setup(string name, Sprite sprite)
    {
        // 🚨 중요: 내부 로직용 ID는 원본(한국어) 그대로 저장!!
        ingredientName = name;

        // ✨ UI에 보여줄 글자만 번역기로 돌려서 씌워줍니다!
        if (nameText != null) nameText.text = TextTranslator.GetIngredientName(name);

        if (iconImage != null)
        {
            if (sprite != null)
            {
                iconImage.sprite = sprite;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
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