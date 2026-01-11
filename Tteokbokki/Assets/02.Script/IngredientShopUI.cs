using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IngredientShopUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject shopPanel;
    public GameObject shopItemPrefab;
    public Transform ingredientListParent;

    [Header("Bottom UI")]
    public TextMeshProUGUI totalCostText;
    public TextMeshProUGUI warningText;
    public Button orderButton;

    public Button selectAllButton;
    public TextMeshProUGUI selectAllButtonText;

    [Header("New Feature")]
    public Button selectLowStockButton; // "부족한 재료 담기" 버튼 연결

    private Dictionary<string, IngredientMetaData> selectedItems = new Dictionary<string, IngredientMetaData>();
    private List<ShopItemUI> spawnedItems = new List<ShopItemUI>();

    private bool isAllSelected = false;

    void Start()
    {
        selectAllButton.onClick.AddListener(OnSelectAllToggle);
        orderButton.onClick.AddListener(OnOrderButtonClicked);

        // 부족한 재료 선택 버튼 이벤트 연결
        if (selectLowStockButton != null)
            selectLowStockButton.onClick.AddListener(OnSelectLowStockButtonClicked);
    }

    public void OpenShop()
    {
        shopPanel.SetActive(true);
        PopulateShop();
        UpdateTotalCostUI();
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
    }

    void PopulateShop()
    {
        isAllSelected = false;
        selectAllButtonText.text = "모두 선택";
        selectedItems.Clear();
        spawnedItems.Clear();

        foreach (Transform child in ingredientListParent)
        {
            Destroy(child.gameObject);
        }

        // 1. 재고 부족 리스트 갱신 및 가져오기 (매니저 활용)
        IngredientStockManager.Instance.UpdateLowStockList();
        List<string> lowStockList = IngredientStockManager.Instance.GetLowStockIngredients();
        // 빠른 검색을 위해 HashSet으로 변환 (옵션)
        HashSet<string> lowStockSet = new HashSet<string>(lowStockList);

        foreach (var kv in IngredientEconomyDatabase.Data)
        {
            var data = kv.Value;

            GameObject obj = Instantiate(shopItemPrefab, ingredientListParent);
            ShopItemUI ui = obj.GetComponent<ShopItemUI>();

            if (ui == null) continue;

            spawnedItems.Add(ui);

            bool isOrdered = IngredientStockManager.Instance.HasOrderedToday(data.Name);

            // 2. 현재 재료가 부족한 상태인지 확인
            bool isLowStock = lowStockSet.Contains(data.Name);

            // 3. Setup 호출 시 isLowStock 전달
            ui.Setup(data, isOrdered, isLowStock, (isOn) =>
            {
                if (isOn)
                {
                    if (!selectedItems.ContainsKey(data.Name))
                        selectedItems.Add(data.Name, data);
                }
                else
                {
                    if (selectedItems.ContainsKey(data.Name))
                        selectedItems.Remove(data.Name);
                }
                CheckSelectAllButtonState();

                UpdateTotalCostUI();
            });
        }
    }

    // "부족한 재료 담기" 버튼 로직
    public void OnSelectLowStockButtonClicked()
    {
        // 1. 제어 대상 찾기 (주문 안 했고 & 재고가 부족한 아이템들)
        var targetItems = spawnedItems
            .Where(item => !item.IsOrdered && item.IsLowStock)
            .ToList();

        if (targetItems.Count == 0)
        {
            Debug.Log("선택할 부족한 재료가 없습니다.");
            return;
        }

        // 2. [판단 로직] 대상들이 "이미 전부 선택된 상태"인가?
        // All()은 리스트의 모든 요소가 조건을 만족하면 true입니다.
        // 즉, 하나라도 선택 안 된 게 있으면 false가 됩니다.
        bool areAllSelected = targetItems.All(item => item.selectToggle.isOn);

        // 3. 행동 결정
        // 이미 다 선택되어 있다면(true) -> 끈다(false)
        // 하나라도 안 켜져 있다면(false) -> 켠다(true)
        bool newToggleState = !areAllSelected;

        // 4. 적용
        foreach (var item in targetItems)
        {
            item.SetToggle(newToggleState);
        }

        // 로그 및 피드백 (옵션)
        Debug.Log(newToggleState ? "필요 재료 모두 선택" : "필요 재료 선택 해제");
    }

    // ... (UpdateTotalCostUI, OnOrderButtonClicked, OnSelectAllToggle은 기존과 동일)

    void UpdateTotalCostUI()
    {
        int totalCost = selectedItems.Values.Sum(i => i.OrderCost);
        totalCostText.text = $"총 주문 금액: {totalCost:N0}원";

        bool canAfford = totalCost <= PlayerWalletManager.Instance.CurrentBalance;

        if (selectedItems.Count > 0 && !canAfford)
        {
            warningText.gameObject.SetActive(true);
            warningText.text = "잔고 부족!";
            orderButton.interactable = false;
        }
        else if (selectedItems.Count == 0)
        {
            warningText.gameObject.SetActive(false);
            orderButton.interactable = false;
        }
        else
        {
            warningText.gameObject.SetActive(false);
            orderButton.interactable = true;
        }
    }

    public void OnOrderButtonClicked()
    {
        if (selectedItems.Count == 0) return;

        foreach (var entry in selectedItems)
        {
            IngredientStockManager.Instance.OrderIngredient(entry.Key);
        }

        // 주문 후 목록 갱신
        PopulateShop();
        UpdateTotalCostUI();
    }

    public void OnSelectAllToggle()
    {
        // 1. 제어 대상 찾기 (주문 안 한 모든 아이템)
        var targetItems = spawnedItems
            .Where(item => !item.IsOrdered)
            .ToList();

        if (targetItems.Count == 0) return;

        // 2. [판단 로직] 전부 선택된 상태인가?
        bool areAllSelected = targetItems.All(item => item.selectToggle.isOn);

        // 3. 행동 결정
        bool newToggleState = !areAllSelected;

        // 4. 적용
        foreach (var item in targetItems)
        {
            item.SetToggle(newToggleState);
        }

        // 5. 버튼 텍스트 갱신 (상태에 따라 직관적으로 변경)
        // 만약 이번에 켰으면(true) 다음엔 '해제'라고 보여주는 게 맞음
        selectAllButtonText.text = newToggleState ? "모두 해제" : "모두 선택";
    }

    private void CheckSelectAllButtonState()
    {
        var activeItems = spawnedItems.Where(i => !i.IsOrdered).ToList();
        if (activeItems.Count == 0) return;

        bool allOn = activeItems.All(i => i.selectToggle.isOn);
        selectAllButtonText.text = allOn ? "모두 해제" : "모두 선택";
    }
}