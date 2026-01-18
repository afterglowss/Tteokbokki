using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

public class IngredientShopUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject shopPanel;
    public GameObject shopItemPrefab;

    public ScrollRect shopScrollRect;

    // ✨ 기존 ingredientListParent는 더 이상 직접 쓰지 않고, 전체 구조의 부모 역할만 합니다.
    public Transform contentRoot;

    [Header("Categorized Grids & Titles")]
    public GameObject reorderTitleObject; // "재주문" 제목 오브젝트
    public Transform unlockedGridParent;  // "재주문" 그리드

    public GameObject newArrivalTitleObject; // "신규 입고" 제목 오브젝트
    public Transform lockedGridParent;       // "신규 입고" 그리드

    [Header("Bottom UI")]
    public TextMeshProUGUI totalCostText;
    public TextMeshProUGUI warningText;
    public Button orderButton;
    public TextMeshProUGUI orderButtonText;

    [Header("Select Buttons")]
    public Button selectAllButton;
    public TextMeshProUGUI selectAllButtonText;
    public Outline selectAllButtonOutline;

    [Header("New Feature")]
    public Button selectLowStockButton;
    public Outline selectLowStockButtonOutline;

    [Header("Outline Colors")]
    public Color defaultButtonOutlineColor = Color.white;
    public Color activeButtonOutlineColor = Color.red;

    public UnityEvent OnShopProcessFinished;

    private Dictionary<string, IngredientMetaData> selectedItems = new Dictionary<string, IngredientMetaData>();
    private List<ShopItemUI> spawnedItems = new List<ShopItemUI>();

    void Start()
    {
        selectAllButton.onClick.AddListener(OnSelectAllToggle);
        orderButton.onClick.AddListener(OnOrderButtonClicked);

        if (selectLowStockButton != null)
            selectLowStockButton.onClick.AddListener(OnSelectLowStockButtonClicked);

        if (OnShopProcessFinished == null) OnShopProcessFinished = new UnityEvent();
    }

    public void OpenShop()
    {
        shopPanel.SetActive(true);
        PopulateShop();
        UpdateTotalCostUI();
        UpdateButtonOutlines();

        StartCoroutine(ResetScrollCoroutine());
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
    }

    void PopulateShop()
    {
        selectAllButtonText.text = "모두 선택";
        selectedItems.Clear();
        spawnedItems.Clear();

        // 1. 그리드 청소
        ClearGrid(unlockedGridParent);
        ClearGrid(lockedGridParent);

        IngredientStockManager.Instance.UpdateLowStockList();
        List<string> lowStockList = IngredientStockManager.Instance.GetLowStockIngredients();
        HashSet<string> lowStockSet = new HashSet<string>(lowStockList);

        // 카테고리별 아이템 개수 카운트 (UI 끄기/켜기용)
        int unlockedCount = 0;
        int lockedCount = 0;

        foreach (var kv in IngredientEconomyDatabase.Data)
        {
            var data = kv.Value;
            string name = data.Name;

            // ✨ [구역 분리 로직]
            bool hasPurchased = IngredientStockManager.Instance.HasPurchasedBefore(name);
            Transform targetParent;

            if (hasPurchased)
            {
                targetParent = unlockedGridParent;
                unlockedCount++;
            }
            else
            {
                targetParent = lockedGridParent;
                lockedCount++;
            }

            // 부모가 연결 안 되어있으면 예외처리
            if (targetParent == null) continue;

            // ✨ [버그 수정] 부모를 targetParent로 지정해야 분류가 됩니다!
            GameObject obj = Instantiate(shopItemPrefab, targetParent);
            ShopItemUI ui = obj.GetComponent<ShopItemUI>();

            if (ui == null) continue;

            spawnedItems.Add(ui);

            bool isOrdered = IngredientStockManager.Instance.HasOrderedToday(name);
            bool isLowStock = lowStockSet.Contains(name);

            ui.Setup(data, isOrdered, isLowStock, (isOn) =>
            {
                if (isOn)
                {
                    if (!selectedItems.ContainsKey(name))
                        selectedItems.Add(name, data);
                }
                else
                {
                    if (selectedItems.ContainsKey(name))
                        selectedItems.Remove(name);
                }

                // 버튼 상태 갱신은 토글이 바뀔 때마다 즉시 반영
                CheckSelectAllButtonState();
                UpdateTotalCostUI();
                UpdateButtonOutlines();
            });
        }

        // ✨ [UI 정리] 아이템이 없는 카테고리는 제목과 그리드를 숨겨서 깔끔하게 만듦
        if (reorderTitleObject != null) reorderTitleObject.SetActive(unlockedCount > 0);
        if (unlockedGridParent != null) unlockedGridParent.gameObject.SetActive(unlockedCount > 0);

        if (newArrivalTitleObject != null) newArrivalTitleObject.SetActive(lockedCount > 0);
        if (lockedGridParent != null) lockedGridParent.gameObject.SetActive(lockedCount > 0);

        // 초기 버튼 상태 갱신
        UpdateButtonOutlines();
        StartCoroutine(ResetScrollCoroutine());
    }

    // ✨ 스크롤 초기화 헬퍼 함수
    private IEnumerator ResetScrollCoroutine()
    {
        // 1. 레이아웃이 갱신될 때까지 한 프레임 대기
        yield return null;

        // 2. 혹시 모르니 강제 업데이트 한 번 더 (선택사항이지만 안전함)
        if (shopScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            shopScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void UpdateButtonOutlines()
    {
        // 1. "모두 선택" 버튼
        var allActiveItems = spawnedItems.Where(item => !item.IsOrdered).ToList();
        if (allActiveItems.Count > 0 && selectAllButtonOutline != null)
        {
            bool allOn = allActiveItems.All(item => item.selectToggle.isOn);
            selectAllButtonOutline.effectColor = allOn ? activeButtonOutlineColor : defaultButtonOutlineColor;
            selectAllButtonText.text = allOn ? "모두 해제" : "모두 선택";
        }

        // 2. "부족한 재료" 버튼
        var lowStockItems = spawnedItems.Where(item => !item.IsOrdered && item.IsLowStock).ToList();
        if (lowStockItems.Count > 0 && selectLowStockButtonOutline != null)
        {
            bool allLowStockOn = lowStockItems.All(item => item.selectToggle.isOn);
            selectLowStockButtonOutline.effectColor = allLowStockOn ? activeButtonOutlineColor : defaultButtonOutlineColor;
        }
        else if (selectLowStockButtonOutline != null)
        {
            // 부족한 재료가 아예 없으면 기본 색상
            selectLowStockButtonOutline.effectColor = defaultButtonOutlineColor;
        }
    }

    public void OnSelectLowStockButtonClicked()
    {
        var targetItems = spawnedItems
            .Where(item => !item.IsOrdered && item.IsLowStock)
            .ToList();

        if (targetItems.Count == 0) return;

        bool areAllSelected = targetItems.All(item => item.selectToggle.isOn);
        bool newToggleState = !areAllSelected;

        foreach (var item in targetItems) item.SetToggle(newToggleState);

        UpdateButtonOutlines();
        UpdateTotalCostUI(); // 금액 갱신 추가
    }

    public void OnSelectAllToggle()
    {
        var targetItems = spawnedItems.Where(item => !item.IsOrdered).ToList();

        if (targetItems.Count == 0) return;

        bool areAllSelected = targetItems.All(item => item.selectToggle.isOn);
        bool newToggleState = !areAllSelected;

        foreach (var item in targetItems) item.SetToggle(newToggleState);

        UpdateButtonOutlines();
        UpdateTotalCostUI(); // 금액 갱신 추가
    }

    private void CheckSelectAllButtonState()
    {
        var activeItems = spawnedItems.Where(i => !i.IsOrdered).ToList();
        if (activeItems.Count == 0) return;
        bool allOn = activeItems.All(i => i.selectToggle.isOn);
        selectAllButtonText.text = allOn ? "모두 해제" : "모두 선택";
    }

    void UpdateTotalCostUI()
    {
        int totalCost = selectedItems.Values.Sum(i => i.OrderCost);
        totalCostText.text = $"총 주문 금액: {totalCost:N0}원";

        bool canAfford = totalCost <= PlayerWalletManager.Instance.CurrentBalance;

        if (selectedItems.Count == 0)
        {
            warningText.gameObject.SetActive(false);
            orderButton.interactable = true;
            if (orderButtonText != null) orderButtonText.text = "넘어가기";
        }
        else
        {
            if (orderButtonText != null) orderButtonText.text = "주문하기";

            if (!canAfford)
            {
                warningText.gameObject.SetActive(true);
                warningText.text = "잔고 부족!";
                orderButton.interactable = false;
            }
            else
            {
                warningText.gameObject.SetActive(false);
                orderButton.interactable = true;
            }
        }
    }

    private void ClearGrid(Transform grid)
    {
        if (grid == null) return;
        foreach (Transform child in grid) Destroy(child.gameObject);
    }

    public void OnOrderButtonClicked()
    {
        if (selectedItems.Count > 0)
        {
            foreach (var entry in selectedItems)
                IngredientStockManager.Instance.OrderIngredient(entry.Key);

            PopulateShop();
            UpdateTotalCostUI();
            UpdateButtonOutlines();
        }
        else
        {
            Debug.Log("[상점] 주문 없이 넘어갑니다.");
        }
        OnShopProcessFinished?.Invoke();
    }
}