using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class IngredientStockEntry
{
    public string ingredientName;
    public TextMeshProUGUI textUI;
}

public class IngredientStockManager : MonoBehaviour
{
    public static IngredientStockManager Instance { get; private set; }

    // 현재 재고
    private Dictionary<string, int> stock = new();

    public List<IngredientStockEntry> stockTextEntries;

    // UI 연결 (재료별 텍스트 표시)
    public Dictionary<string, TextMeshProUGUI> stockTexts = new();

    // 총 사용된 돈 기록 (Optional)
    public int TotalSpent { get; private set; } = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        foreach (var entry in stockTextEntries)
        {
            if (!stockTexts.ContainsKey(entry.ingredientName))
            {
                stockTexts.Add(entry.ingredientName, entry.textUI);
            }
        }

        InitializeStock();
    }

    void Start()
    {
        OrderAllIngredientsOnce();
    }

    /// <summary>
    /// 초기 재고 설정 (모든 재료 0개로 시작)
    /// </summary>
    private void InitializeStock()
    {
        foreach (var name in IngredientEconomyDatabase.Data.Keys)
        {
            stock[name] = 0;
        }

        UpdateAllStockTexts();
    }

    public void OrderAllIngredientsOnce()
    {
        foreach (var kv in IngredientEconomyDatabase.Data)
        {
            string ingredientName = kv.Key;
            OrderIngredient(ingredientName);
        }

        Debug.Log("모든 재료를 1회 주문량만큼 보충했습니다.");
    }

    /// <summary>
    /// 재료 1개 사용 (재고 차감)
    /// </summary>
    public bool UseIngredient(string ingredientName)
    {
        if (!stock.ContainsKey(ingredientName) || stock[ingredientName] <= 0)
        {
            Debug.LogWarning($"'{ingredientName}' 재고가 부족합니다!");
            return false;
        }

        stock[ingredientName]--;
        UpdateStockText(ingredientName);
        return true;
    }

    /// <summary>
    /// 고정된 주문량 기준으로 재료 보충
    /// </summary>
    public void OrderIngredient(string ingredientName)
    {
        if (!IngredientEconomyDatabase.Data.TryGetValue(ingredientName, out var meta))
        {
            Debug.LogWarning($"'{ingredientName}'은(는) 경제 정보에 등록되어 있지 않습니다.");
            return;
        }

        int servings = meta.ServingsPerOrder;
        int cost = meta.OrderCost;

        stock[ingredientName] += servings;
        TotalSpent += cost;

        Debug.Log($"'{ingredientName}' 재료를 {servings}인분만큼 보충했습니다. (비용: {cost}원)");
        UpdateStockText(ingredientName);
    }

    /// <summary>
    /// 현재 재고 반환
    /// </summary>
    public int GetStock(string ingredientName)
    {
        return stock.TryGetValue(ingredientName, out var value) ? value : 0;
    }

    /// <summary>
    /// 현재 총 지출 금액 반환
    /// </summary>
    public int GetTotalSpent()
    {
        return TotalSpent;
    }

    /// <summary>
    /// 특정 재료의 텍스트 갱신
    /// </summary>
    public void UpdateStockText(string ingredientName)
    {
        if (stockTexts.TryGetValue(ingredientName, out var ui))
        {
            ui.text = $"재고: {stock[ingredientName]}";
        }
    }

    /// <summary>
    /// 전체 재고 텍스트 갱신
    /// </summary>
    public void UpdateAllStockTexts()
    {
        foreach (var kv in stock)
        {
            UpdateStockText(kv.Key);
        }
    }
}
