using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text;

public class IngredientShopUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject shopPanel;
    public GameObject shopItemPrefab;
    public ScrollRect shopScrollRect;

    // 상단 보너스 표시
    public TextMeshProUGUI bonusInfoText;

    [Header("Cart Popup UI")]
    public GameObject cartPopupPanel;
    public TextMeshProUGUI cartContentText;
    public Button cartButton;
    public TextMeshProUGUI cartCountBadgeText;

    [Header("Categorized Grids")]
    public GameObject reorderTitleObject;
    public Transform unlockedGridParent;
    public GameObject newArrivalTitleObject;
    public Transform lockedGridParent;

    [Header("Bottom UI")]
    public TextMeshProUGUI totalCostText;
    public TextMeshProUGUI cartInfoText;
    public TextMeshProUGUI warningText;

    public Button orderButton;
    public TextMeshProUGUI orderButtonText;

    [Header("Convenience Buttons")]
    // ✨ [NEW] 버튼 분리: 전체 1개씩 담기
    public Button selectAllButton;
    public TextMeshProUGUI selectAllButtonText;

    // ✨ [NEW] 버튼 분리: 부족한 것만 1개씩 담기
    public Button selectLowStockButton;
    public TextMeshProUGUI selectLowStockButtonText;

    // ✨ [NEW] 장바구니 비우기 (전체 해제)
    public Button clearCartButton;
    public TextMeshProUGUI clearCartButtonText;

    [Header("Tutorial Settings")]
    public bool isTutorialMode = false;
    public List<string> tutorialItems;

    // 내부 관리용
    private List<ShopItemUI> spawnedItems = new List<ShopItemUI>();
    public UnityEvent OnShopProcessFinished;

    // ✨ 최대 구매 수량 (ShopItemUI와 동일하게 맞춤)
    private const int MAX_QUANTITY = 3;

    // ✨ [NEW] 버튼 원래 텍스트 색상 저장용
    private Color originalButtonTextColor = Color.black;

    private void Awake()
    {
        if (OnShopProcessFinished == null) OnShopProcessFinished = new UnityEvent();
    }

    void Start()
    {
        // 1. 전체 담기 버튼 (SelectAll)
        if (selectAllButton != null)
        {
            selectAllButton.onClick.RemoveAllListeners();
            selectAllButton.onClick.AddListener(OnSelectAllClicked);
        }

        // 2. 부족분 담기 버튼 (LowStock)
        if (selectLowStockButton != null)
        {
            selectLowStockButton.onClick.RemoveAllListeners();
            selectLowStockButton.onClick.AddListener(OnSelectLowStockClicked);
        }

        // ✨ 3. 장바구니 비우기
        if (clearCartButton != null)
        {
            clearCartButton.onClick.RemoveAllListeners();
            clearCartButton.onClick.AddListener(OnClearCartClicked);
        }

        // 3. 주문 버튼
        if (orderButton != null)
        {
            orderButton.onClick.RemoveAllListeners();
            orderButton.onClick.AddListener(OnOrderButtonClicked);
            if (orderButtonText != null) originalButtonTextColor = orderButtonText.color;
        }

        // 4. 장바구니 버튼
        if (cartButton != null)
        {
            cartButton.onClick.RemoveAllListeners();
            cartButton.onClick.AddListener(ToggleCartPopup);
        }

        if (cartPopupPanel != null) cartPopupPanel.SetActive(false);
    }

    public void OpenShop()
    {
        gameObject.SetActive(true);
        if (shopPanel != null) shopPanel.SetActive(true);

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorial)
        {
            isTutorialMode = true;
        }

        PopulateShop();
        UpdateBonusText();
        UpdateTotalCostUI();

        if (gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(ResetScrollCoroutine());
        }
    }

    private void OnEnable()
    {
        if (shopScrollRect != null)
        {
            StopAllCoroutines();
            StartCoroutine(ResetScrollCoroutine());
        }
    }

    private void UpdateBonusText()
    {
        if (bonusInfoText == null) return;

        if (DailyBonusManager.Instance != null)
        {
            string bonusNames = DailyBonusManager.Instance.GetTodayBonusString();
            // ✨ [COLOR] 노란색 대신 진한 오렌지(#D95400)로 변경
            bonusInfoText.text = $"내일의 보너스: <color=#D95400>{bonusNames}</color> (수익 증가!)";
        }
        else
        {
            bonusInfoText.text = "보너스 정보 없음";
        }
    }

    void PopulateShop()
    {
        spawnedItems.Clear();
        ClearGrid(unlockedGridParent);
        ClearGrid(lockedGridParent);

        IngredientStockManager.Instance.UpdateLowStockList();
        List<string> lowStockList = IngredientStockManager.Instance.GetLowStockIngredients();
        HashSet<string> lowStockSet = new HashSet<string>(lowStockList);

        IEnumerable<string> itemsToShow;
        if (isTutorialMode && tutorialItems != null && tutorialItems.Count > 0)
        {
            itemsToShow = tutorialItems;
        }
        else
        {
            itemsToShow = IngredientEconomyDatabase.Data.Keys;
        }

        int unlockedCount = 0;
        int lockedCount = 0;

        foreach (var name in itemsToShow)
        {
            if (!IngredientEconomyDatabase.Data.TryGetValue(name, out var data)) continue;

            bool hasPurchased = IngredientStockManager.Instance.HasPurchasedBefore(name);
            int currentStock = IngredientStockManager.Instance.GetStock(name);

            Transform targetParent;
            if (isTutorialMode)
            {
                targetParent = lockedGridParent;
                lockedCount++;
            }
            else
            {
                targetParent = hasPurchased ? unlockedGridParent : lockedGridParent;
                if (hasPurchased) unlockedCount++; else lockedCount++;
            }

            GameObject obj = Instantiate(shopItemPrefab, targetParent);
            obj.name = name;

            ShopItemUI ui = obj.GetComponent<ShopItemUI>();
            if (ui == null) continue;

            spawnedItems.Add(ui);

            bool isLowStock = lowStockSet.Contains(name);

            ui.Setup(data, hasPurchased, currentStock, isLowStock, () =>
            {
                UpdateTotalCostUI();
            });
        }

        if (reorderTitleObject != null) reorderTitleObject.SetActive(unlockedCount > 0 && !isTutorialMode);
        if (newArrivalTitleObject != null) newArrivalTitleObject.SetActive(lockedCount > 0);

        // 버튼 텍스트 설정
        if (selectAllButtonText != null) selectAllButtonText.text = "전체 1개씩";
        if (selectLowStockButtonText != null) selectLowStockButtonText.text = "부족분 1개씩";
        // ✨ 비우기 버튼 텍스트 설정
        if (clearCartButtonText != null) clearCartButtonText.text = "비우기";
    }

    // ✨ [NEW] 장바구니 비우기 로직
    public void OnClearCartClicked()
    {
        bool anyChanged = false;

        foreach (var item in spawnedItems)
        {
            if (item.CurrentCount > 0)
            {
                item.SetCount(0); // 0개로 초기화
                anyChanged = true;
            }
        }

        if (anyChanged)
        {
            UpdateTotalCostUI();
            // 취소/삭제 느낌의 사운드 (예: 109번)
            //if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(107);
        }
        else
        {
            // 이미 비어있음
            TooltipManager.ShowFollowMouse(TooltipType.UI, "장바구니가 이미 비어있습니다.", 1f);
        }
    }

    // ✨ [NEW] 모든 재료를 최소 1개씩 담기
    public void OnSelectAllClicked()
    {
        bool anyChanged = false;

        foreach (var item in spawnedItems)
        {
            // 3개 미만인 것들만 1개 추가
            if (item.CurrentCount < MAX_QUANTITY)
            {
                item.SetCount(item.CurrentCount + 1);
                anyChanged = true;
            }
        }

        if (anyChanged)
        {
            UpdateTotalCostUI();
            //if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(107);
        }
        else
        {
            // 변경된 게 없다는 건 이미 모두 꽉 찼다는 뜻
            TooltipManager.ShowFollowMouse(TooltipType.UI, "모든 재료가 구매 제한(3개)에 도달했습니다.", 1f);
        }
    }

    // ✨ [NEW] 부족한 재료만 최소 1개씩 담기
    public void OnSelectLowStockClicked()
    {
        bool anyChanged = false;
        bool hasLowStockItems = false;

        foreach (var item in spawnedItems)
        {
            if (item.IsLowStock)
            {
                hasLowStockItems = true;
                // 부족한 재료 중 3개 미만인 것만 +1
                if (item.CurrentCount < MAX_QUANTITY)
                {
                    item.SetCount(item.CurrentCount + 1);
                    anyChanged = true;
                }
            }
        }

        if (anyChanged)
        {
            UpdateTotalCostUI();
            //if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(107);
        }
        else
        {
            if (!hasLowStockItems)
                TooltipManager.ShowFollowMouse(TooltipType.UI, "부족한 재료가 없습니다.", 1f);
            else
                TooltipManager.ShowFollowMouse(TooltipType.UI, "부족한 재료들이 이미 구매 제한(3개)에 도달했습니다.", 1f);
        }
    }

    public void ToggleCartPopup()
    {
        if (cartPopupPanel != null)
        {
            bool isActive = !cartPopupPanel.activeSelf;
            cartPopupPanel.SetActive(isActive);
            //if (isActive && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(107);
        }
    }

    void UpdateTotalCostUI()
    {
        int totalCost = 0;
        int totalItemsCount = 0;
        int totalQuantity = 0;

        StringBuilder cartSb = new StringBuilder();

        foreach (var item in spawnedItems)
        {
            if (item.CurrentCount > 0)
            {
                totalCost += item.Data.OrderCost * item.CurrentCount;
                totalItemsCount++;
                totalQuantity += item.CurrentCount;

                // ✨ [COLOR] 여기 수량 표시도 진한 오렌지로 변경
                cartSb.AppendLine($"{item.Data.Name} <color=#D95400>x{item.CurrentCount}</color>");
            }
        }

        if (cartContentText != null)
        {
            if (totalItemsCount > 0)
                cartContentText.text = cartSb.ToString();
            else
                cartContentText.text = "<color=#888888>장바구니가 비어있습니다.</color>";
        }

        // ✨ [COLOR] 총액 표시 색상 변경 (진한 오렌지)
        if (totalCostText != null)
            totalCostText.text = $"총 주문 금액: <color=#D95400>{totalCost:N0}원</color>";

        if (cartInfoText != null)
        {
            cartInfoText.text = totalItemsCount > 0
                ? $"<size=80%>(총 {totalQuantity}개 품목)</size>"
                : "";
        }

        if (cartCountBadgeText != null)
        {
            cartCountBadgeText.text = totalQuantity > 0 ? totalQuantity.ToString() : "";
            cartCountBadgeText.transform.parent.gameObject.SetActive(totalQuantity > 0);
        }

        bool canAfford = totalCost <= PlayerWalletManager.Instance.CurrentBalance;

        // 1. 장바구니가 비어있는 경우 -> "넘어가기" (항상 가능)
        if (totalItemsCount == 0)
        {
            if (orderButton != null) orderButton.interactable = true;
            if (orderButtonText != null)
            {
                orderButtonText.text = "넘어가기";
                orderButtonText.color = originalButtonTextColor;
            }
        }
        // 2. 장바구니에 내용물이 있는 경우
        else
        {
            if (canAfford)
            {
                // 돈이 충분함 -> "주문하기" (가능)
                if (orderButton != null) orderButton.interactable = true;
                if (orderButtonText != null)
                {
                    orderButtonText.text = "주문하기";
                    orderButtonText.color = originalButtonTextColor;
                }
            }
            else
            {
                // 돈이 부족함 -> "잔고 부족" (불가능 & 빨간색)
                if (orderButton != null) orderButton.interactable = false;
                if (orderButtonText != null)
                {
                    orderButtonText.text = "잔고 부족";
                    orderButtonText.color = Color.red; // 🔴 빨간색 강조
                }
            }
        }
    }

    public void OnOrderButtonClicked()
    {
        int purchaseCount = 0;
        int totalCost = 0;

        foreach (var item in spawnedItems)
        {
            if (item.CurrentCount > 0)
            {
                for (int i = 0; i < item.CurrentCount; i++)
                {
                    IngredientStockManager.Instance.OrderIngredient(item.Data.Name);
                }
                // ✨ [NEW] 재료별 구매 수량 기록
                if (GameDataLogger.Instance != null)
                {
                    GameDataLogger.Instance.LogIngredientBought(item.Data.Name, item.CurrentCount);
                }

                totalCost += item.Data.OrderCost * item.CurrentCount;
                purchaseCount++;
            }
        }

        // ✨ [NEW] 총 지출 금액 기록
        if (purchaseCount > 0 && GameDataLogger.Instance != null)
        {
            GameDataLogger.Instance.AddShoppingExpense(totalCost);
        }

        if (purchaseCount > 0)
        {
            PopulateShop();
            UpdateTotalCostUI();
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(110);
        }

        if (cartPopupPanel != null) cartPopupPanel.SetActive(false);

        OnShopProcessFinished?.Invoke();
    }

    private void ClearGrid(Transform grid)
    {
        if (grid == null) return;
        foreach (Transform child in grid) Destroy(child.gameObject);
    }

    private IEnumerator ResetScrollCoroutine()
    {
        yield return null;
        if (shopScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            shopScrollRect.verticalNormalizedPosition = 1f;
        }
    }
}