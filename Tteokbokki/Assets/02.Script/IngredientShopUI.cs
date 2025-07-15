using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class IngredientShopUI : MonoBehaviour
{
    public GameObject shopPanel;
    public GameObject shopItemPrefab;
    public Transform ingredientListParent;
    public TextMeshProUGUI totalCostText;
    public Button orderButton;
    public TextMeshProUGUI warningText;
    public Button selectAllButton;
    public TextMeshProUGUI selectAllButtonText;

    private bool isAllSelected = false;  // 현재 선택 상태

    private Dictionary<string, IngredientMetaData> selectedItems = new();

    void Start()
    {
        selectAllButton.onClick.AddListener(OnSelectAllToggle);

        PopulateShop();
        UpdateTotalCostUI();
    }

    void PopulateShop()
    {
        isAllSelected = false;
        selectAllButtonText.text = "모두 선택";

        foreach (Transform child in ingredientListParent)
            Destroy(child.gameObject);

        foreach (var kv in IngredientEconomyDatabase.Data)
        {
            var data = kv.Value;
            var obj = Instantiate(shopItemPrefab, ingredientListParent);

            obj.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = data.Name;
            obj.transform.Find("PriceText").GetComponent<TextMeshProUGUI>().text = $"{data.OrderCost:N0}원";
            obj.transform.Find("AmountText").GetComponent<TextMeshProUGUI>().text = $"{data.ServingsPerOrder}인분";

            Toggle toggle = obj.transform.Find("SelectToggle").GetComponent<Toggle>();
            TextMeshProUGUI statusText = obj.transform.Find("StatusText").GetComponent<TextMeshProUGUI>();

            if (IngredientStockManager.Instance.HasOrderedToday(data.Name))
            {
                toggle.interactable = false;
                statusText.text = "주문 완료";
                continue;
            }

            toggle.SetIsOnWithoutNotify(false);
            statusText.text = "";

            toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                    selectedItems[data.Name] = data;
                else
                    selectedItems.Remove(data.Name);

                UpdateTotalCostUI();
            });
        }
    }

    void UpdateTotalCostUI()
    {
        int totalCost = selectedItems.Values.Sum(i => i.OrderCost);
        totalCostText.text = $"총 주문 금액: {totalCost:N0}원";

        bool canAfford = totalCost <= PlayerWalletManager.Instance.CurrentBalance;
        warningText.gameObject.SetActive(!canAfford);
        warningText.text = "잔고가 부족합니다!";
        orderButton.interactable = canAfford && selectedItems.Count > 0;
    }

    public void OnOrderButtonClicked()
    {
        if (selectedItems.Count == 0)
        {
            Debug.Log("[주문 실패] 선택된 재료가 없습니다.");
            return;
        }

        foreach (var entry in selectedItems)
        {
            IngredientStockManager.Instance.OrderIngredient(entry.Key);
        }

        Debug.Log($"[주문 완료] {selectedItems.Count}종 주문됨");

        selectedItems.Clear();
        PopulateShop();         // 재설정
        UpdateTotalCostUI();
    }
    public void OnSelectAllToggle()
    {
        isAllSelected = !isAllSelected;
        selectedItems.Clear();
        Debug.Log($"[선택 상태 변경] isAllSelected: {isAllSelected}");

        foreach (Transform child in ingredientListParent)
        {
            var nameText = child.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var toggle = child.Find("SelectToggle")?.GetComponent<Toggle>();

            if (nameText == null || toggle == null) continue;

            string ingredientName = nameText.text;

            if (IngredientStockManager.Instance.HasOrderedToday(ingredientName))
            {
                toggle.interactable = false;
                continue;
            }
            if (isAllSelected)
            {
                toggle.SetIsOnWithoutNotify(true);
                selectedItems[ingredientName] = IngredientEconomyDatabase.Data[ingredientName];
            }
            else
            {
                toggle.SetIsOnWithoutNotify(false);
                selectedItems.Remove(ingredientName);
            }
        }

        selectAllButtonText.text = isAllSelected ? "모두 해제" : "모두 선택";
        UpdateTotalCostUI();
    }



    private void OnBuyButtonClicked(string ingredientName)
    {
        IngredientStockManager.Instance.OrderIngredient(ingredientName);
    }

    public void OpenShop()
    {
        shopPanel.SetActive(true);
        PopulateShop();
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
    }
}
