using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI amountText;

    [Header("Stock Info")]
    public TextMeshProUGUI currentStockText;

    [Header("Quantity Control")]
    public Button minusButton;
    public Button plusButton;
    public TextMeshProUGUI countText;

    [Header("Tutorial Compatibility")]
    public Toggle selectToggle;

    [Header("Visuals")]
    public Image ingredientIconImage;
    public Outline outline;
    public Color normalOutlineColor = Color.white;
    public Color lowStockOutlineColor = Color.red;

    // ✨ [NEW] 최대 구매 수량 상수
    private const int MAX_QUANTITY = 3;

    // 내부 데이터
    public int CurrentCount { get; private set; } = 0;
    public IngredientMetaData Data { get; private set; }
    public bool IsLowStock { get; private set; }

    private UnityAction onCountChanged;

    public void Setup(IngredientMetaData data, bool hasPurchased, int currentStock, bool isLowStock, UnityAction onChanged)
    {
        Data = data;
        IsLowStock = isLowStock;
        onCountChanged = onChanged;
        CurrentCount = 0;

        // 1. 텍스트 설정
        if (nameText != null) nameText.text = data.Name;
        if (priceText != null) priceText.text = $"{data.OrderCost:N0}원";
        if (amountText != null) amountText.text = $"(+{data.ServingsPerOrder}인분)";

        // 2. 재고 표시
        if (currentStockText != null)
        {
            if (!hasPurchased)
                currentStockText.text = "<color=#888888>미입고</color>";
            else
            {
                string colorHex = currentStock <= 0 ? "red" : "#00AA00";
                currentStockText.text = $"<color={colorHex}>{currentStock}</color>";
            }
        }

        // 3. 이미지 설정
        if (IngredientSpriteManager.Instance != null && ingredientIconImage != null)
        {
            ingredientIconImage.sprite = IngredientSpriteManager.Instance.GetSprite(data.Name);
        }

        // 4. 버튼 리스너 연결
        if (minusButton != null)
        {
            minusButton.onClick.RemoveAllListeners();
            minusButton.onClick.AddListener(OnMinusClick);
        }
        if (plusButton != null)
        {
            plusButton.onClick.RemoveAllListeners();
            plusButton.onClick.AddListener(OnPlusClick);
        }

        UpdateUI();
    }

    // 외부에서 수량 강제 설정 (버튼 기능용)
    public void SetCount(int count)
    {
        // ✨ 최대 수량 제한 적용
        CurrentCount = Mathf.Clamp(count, 0, MAX_QUANTITY);
        UpdateUI();
        onCountChanged?.Invoke();
    }

    private void OnPlusClick()
    {
        // ✨ [NEW] 3개 제한 체크
        if (CurrentCount >= MAX_QUANTITY)
        {
            TooltipManager.ShowFollowMouse(TooltipType.UI, "한 번에 최대 3개까지만 구매 가능합니다.", 1f);
            return;
        }

        CurrentCount++;
        UpdateUI();
        onCountChanged?.Invoke();
    }

    private void OnMinusClick()
    {
        if (CurrentCount > 0)
        {
            CurrentCount--;
            UpdateUI();
            onCountChanged?.Invoke();
        }
    }

    private void UpdateUI()
    {
        if (countText != null) countText.text = CurrentCount.ToString();

        // 마이너스 버튼은 0보다 클 때만 활성
        if (minusButton != null) minusButton.interactable = CurrentCount > 0;

        // ✨ 플러스 버튼은 최대 수량 미만일 때만 활성 (선택사항, 툴팁을 띄우려면 항상 켜둬야 함)
        // if (plusButton != null) plusButton.interactable = CurrentCount < MAX_QUANTITY;

        // 튜토리얼 호환성
        if (selectToggle != null)
        {
            selectToggle.SetIsOnWithoutNotify(CurrentCount > 0);
        }

        // 아웃라인 처리
        if (outline != null)
        {
            outline.enabled = true;
            if (CurrentCount > 0)
            {
                outline.effectColor = normalOutlineColor;
            }
            else if (IsLowStock)
            {
                outline.effectColor = lowStockOutlineColor;
            }
            else
            {
                outline.enabled = false;
            }
        }
    }
}