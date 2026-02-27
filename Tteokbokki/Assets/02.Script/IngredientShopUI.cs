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
            // ✨ 1. 보너스 텍스트 번역
            bonusInfoText.text = TextTranslator.GetUIText("Shop_BonusInfo", bonusNames);
        }
        else
        {
            // ✨ 2. 보너스 없음 텍스트 번역
            bonusInfoText.text = TextTranslator.GetUIText("Shop_NoBonus");
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

        // ✨ 3. 버튼 텍스트 설정 번역
        if (selectAllButtonText != null) selectAllButtonText.text = TextTranslator.GetUIText("Shop_SelectAll");
        if (selectLowStockButtonText != null) selectLowStockButtonText.text = TextTranslator.GetUIText("Shop_SelectLowStock");
        if (clearCartButtonText != null) clearCartButtonText.text = TextTranslator.GetUIText("Shop_ClearCart");
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
        else TooltipManager.ShowFollowMouse(TooltipType.UI, TextTranslator.GetUIText("Shop_CartAlreadyEmpty"), 1f);
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
        else TooltipManager.ShowFollowMouse(TooltipType.UI, TextTranslator.GetUIText("Shop_AllItemsMaxed"), 1f);
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
                TooltipManager.ShowFollowMouse(TooltipType.UI, TextTranslator.GetUIText("Shop_NoLowStock"), 1f);
            else
                TooltipManager.ShowFollowMouse(TooltipType.UI, TextTranslator.GetUIText("Shop_LowStockMaxed"), 1f);
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

                string transName = TextTranslator.GetIngredientName(item.Data.Name);
                cartSb.AppendLine($"{transName} <color=#D95400>x{item.CurrentCount}</color>");
            }
        }

        if (cartContentText != null)
        {
            if (totalItemsCount > 0) cartContentText.text = cartSb.ToString();
            else cartContentText.text = TextTranslator.GetUIText("Shop_CartEmpty"); // ✨ 번역
        }

        if (totalCostText != null)
            totalCostText.text = TextTranslator.GetUIText("Shop_TotalCost", totalCost);

        if (cartInfoText != null)
        {
            cartInfoText.text = totalItemsCount > 0
                ? TextTranslator.GetUIText("Shop_TotalItems", totalQuantity)
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
                orderButtonText.text = TextTranslator.GetUIText("Shop_BtnSkip"); // ✨ 번역
                orderButtonText.color = originalButtonTextColor;
            }
        }
        // 2. 장바구니에 내용물이 있는 경우
        else
        {
            if (canAfford)
            {
                if (orderButton != null) orderButton.interactable = true;
                if (orderButtonText != null)
                {
                    orderButtonText.text = TextTranslator.GetUIText("Shop_BtnOrder"); // ✨ 번역
                    orderButtonText.color = originalButtonTextColor;
                }
            }
            else
            {
                if (orderButton != null) orderButton.interactable = false;
                if (orderButtonText != null)
                {
                    orderButtonText.text = TextTranslator.GetUIText("Shop_BtnNoMoney"); // ✨ 번역
                    orderButtonText.color = Color.red;
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
                //// ✨ [NEW] 재료별 구매 수량 기록
                //if (GameDataLogger.Instance != null)
                //{
                //    GameDataLogger.Instance.LogIngredientBought(item.Data.Name, item.CurrentCount);
                //}

                totalCost += item.Data.OrderCost * item.CurrentCount;
                purchaseCount++;
            }
        }

        //// ✨ [NEW] 총 지출 금액 기록
        //if (purchaseCount > 0 && GameDataLogger.Instance != null)
        //{
        //    GameDataLogger.Instance.AddShoppingExpense(totalCost);
        //}

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