using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events; // ✨ 이벤트를 위해 추가

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
    // ✨ [NEW] 버튼 텍스트를 변경하기 위한 변수 추가
    public TextMeshProUGUI orderButtonText;

    public Button selectAllButton;
    public TextMeshProUGUI selectAllButtonText;

    [Header("New Feature")]
    public Button selectLowStockButton;

    // ✨ [NEW] 주문(또는 넘어가기) 완료 시 EndOfDayUIHandler에게 알릴 이벤트
    public UnityEvent OnShopProcessFinished;

    private Dictionary<string, IngredientMetaData> selectedItems = new Dictionary<string, IngredientMetaData>();
    private List<ShopItemUI> spawnedItems = new List<ShopItemUI>();

    private bool isAllSelected = false;

    void Start()
    {
        selectAllButton.onClick.AddListener(OnSelectAllToggle);
        orderButton.onClick.AddListener(OnOrderButtonClicked);

        if (selectLowStockButton != null)
            selectLowStockButton.onClick.AddListener(OnSelectLowStockButtonClicked);

        // ✨ 안전장치: Inspector에서 이벤트를 초기화하지 않았을 경우를 대비
        if (OnShopProcessFinished == null) OnShopProcessFinished = new UnityEvent();
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

        IngredientStockManager.Instance.UpdateLowStockList();
        List<string> lowStockList = IngredientStockManager.Instance.GetLowStockIngredients();
        HashSet<string> lowStockSet = new HashSet<string>(lowStockList);

        foreach (var kv in IngredientEconomyDatabase.Data)
        {
            var data = kv.Value;

            GameObject obj = Instantiate(shopItemPrefab, ingredientListParent);
            ShopItemUI ui = obj.GetComponent<ShopItemUI>();

            if (ui == null) continue;

            spawnedItems.Add(ui);

            bool isOrdered = IngredientStockManager.Instance.HasOrderedToday(data.Name);
            bool isLowStock = lowStockSet.Contains(data.Name);

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

    public void OnSelectLowStockButtonClicked()
    {
        var targetItems = spawnedItems
            .Where(item => !item.IsOrdered && item.IsLowStock)
            .ToList();

        if (targetItems.Count == 0) return;

        bool areAllSelected = targetItems.All(item => item.selectToggle.isOn);
        bool newToggleState = !areAllSelected;

        foreach (var item in targetItems)
        {
            item.SetToggle(newToggleState);
        }
    }

    // ✨ [수정] 주문 버튼 상태 및 텍스트 업데이트 로직 변경
    void UpdateTotalCostUI()
    {
        int totalCost = selectedItems.Values.Sum(i => i.OrderCost);
        totalCostText.text = $"총 주문 금액: {totalCost:N0}원";

        bool canAfford = totalCost <= PlayerWalletManager.Instance.CurrentBalance;

        // 1. 선택된 아이템이 없을 때 -> "넘어가기" 상태 (활성화)
        if (selectedItems.Count == 0)
        {
            warningText.gameObject.SetActive(false);
            orderButton.interactable = true;

            if (orderButtonText != null)
                orderButtonText.text = "넘어가기";
        }
        // 2. 선택된 아이템이 있을 때 -> "주문하기" 상태
        else
        {
            if (orderButtonText != null)
                orderButtonText.text = "주문하기";

            if (!canAfford)
            {
                warningText.gameObject.SetActive(true);
                warningText.text = "잔고 부족!";
                orderButton.interactable = false; // 돈 없으면 비활성화
            }
            else
            {
                warningText.gameObject.SetActive(false);
                orderButton.interactable = true; // 돈 있으면 활성화
            }
        }
    }

    // ✨ [수정] 주문 버튼 클릭 로직
    public void OnOrderButtonClicked()
    {
        // 1. 선택된 아이템이 있다면 주문 처리 (없으면 스킵)
        if (selectedItems.Count > 0)
        {
            foreach (var entry in selectedItems)
            {
                IngredientStockManager.Instance.OrderIngredient(entry.Key);
            }

            // 주문 처리 후 UI 갱신 (선택 해제 등)
            PopulateShop();
            UpdateTotalCostUI();
        }
        else
        {
            Debug.Log("[상점] 주문 없이 넘어갑니다.");
        }

        // 2. 주문을 했든, 그냥 넘어갔든 다음 단계(마감창)로 이동하라고 알림
        OnShopProcessFinished?.Invoke();
    }

    public void OnSelectAllToggle()
    {
        var targetItems = spawnedItems
            .Where(item => !item.IsOrdered)
            .ToList();

        if (targetItems.Count == 0) return;

        bool areAllSelected = targetItems.All(item => item.selectToggle.isOn);
        bool newToggleState = !areAllSelected;

        foreach (var item in targetItems)
        {
            item.SetToggle(newToggleState);
        }

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