using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

[System.Serializable]
public class IngredientStockEntry
{
    public string ingredientName;
    public TextMeshProUGUI textUI;
}

[System.Serializable]
public class StockEntry
{
    public int count;
    public int dayRemaining;
}


public class IngredientStockManager : MonoBehaviour
{
    public static IngredientStockManager Instance { get; private set; }

    // 현재 재고
    private Dictionary<string, List<StockEntry>> stock = new();

    public List<IngredientStockEntry> stockTextEntries;

    // UI 연결 (재료별 텍스트 표시)
    public Dictionary<string, TextMeshProUGUI> stockTexts = new();

    // 총 사용된 돈 기록 (Optional)
    public int TotalSpent { get; private set; } = 0;

    private HashSet<string> orderedToday = new();

    private const int ShelfLifeDays = 5; // 모든 재료 유통기한 5일

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
            stock[name] = new List<StockEntry>();
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
        if (!stock.ContainsKey(ingredientName) || stock[ingredientName].Count == 0)
        {
            Debug.LogWarning($"'{ingredientName}' 재고가 없습니다!");
            return false;
        }

        // 유통기한이 가장 짧은 항목부터 사용
        var entryList = stock[ingredientName]
            .OrderBy(e => e.dayRemaining)
            .ToList();

        foreach (var entry in entryList)
        {
            if (entry.count > 0)
            {
                entry.count--;

                if (entry.count == 0)
                {
                    stock[ingredientName].Remove(entry);
                }

                UpdateStockText(ingredientName);
                return true;
            }
        }

        Debug.LogWarning($"'{ingredientName}' 재고는 있으나 모두 폐기 상태입니다.");
        return false;
    }


    /// <summary>
    /// 고정된 주문량 기준으로 재료 보충
    /// </summary>
    public void OrderIngredient(string ingredientName)
    {
        //Debug.Log($"OrderIngredient 호출됨: {ingredientName}");

        if (orderedToday.Contains(ingredientName))
        {
            Debug.LogWarning($"'{ingredientName}'은(는) 이미 오늘 주문했습니다!");
            return;
        }

        if (!IngredientEconomyDatabase.Data.TryGetValue(ingredientName, out var meta))
        {
            Debug.LogWarning($"'{ingredientName}'은(는) 경제 정보에 없습니다.");
            return;
        }

        int servings = meta.ServingsPerOrder;
        int cost = meta.OrderCost;

        bool paid = PlayerWalletManager.Instance.Spend(cost);
        if (!paid)
        {
            Debug.LogWarning($"'{ingredientName}' 주문 실패 - 잔고 부족!");
            return;
        }

        // 유통기한이 있는 새로운 재고 항목 추가
        if (!stock.ContainsKey(ingredientName))
            stock[ingredientName] = new List<StockEntry>();

        stock[ingredientName].Add(new StockEntry
        {
            count = servings,
            dayRemaining = ShelfLifeDays
        });

        TotalSpent += cost;
        orderedToday.Add(ingredientName);

        Debug.Log($"'{ingredientName}' {servings}인분 (유통기한 {ShelfLifeDays}일) 주문 완료!");
        UpdateStockText(ingredientName);
    }


    public void ResetDailyOrderFlags()
    {
        orderedToday.Clear();
    }

    public bool HasOrderedToday(string name) => orderedToday.Contains(name);


    /// <summary>
    /// 현재 재고 반환
    /// </summary>
    public int GetStock(string ingredientName)
    {
        if (!stock.TryGetValue(ingredientName, out var entries))
            return 0;

        return entries.Sum(e => e.count);
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
        if (!stockTexts.TryGetValue(ingredientName, out var ui))
            return;

        if (!stock.ContainsKey(ingredientName) || stock[ingredientName].Count == 0)
        {
            ui.text = "재고: 없음";
            return;
        }

        // 유통기한별로 그룹화
        var grouped = stock[ingredientName]
            .GroupBy(e => e.dayRemaining)
            .OrderBy(g => g.Key)
            .Select(g => $"{g.Sum(e => e.count)}개({g.Key}일)");

        ui.text = "재고: " + string.Join(", ", grouped);
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

    public void AdvanceDayAndDecay()
    {
        foreach (var pair in stock)
        {
            string ingredientName = pair.Key;
            List<StockEntry> entries = pair.Value;

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                entries[i].dayRemaining--;

                if (entries[i].dayRemaining <= 0)
                {
                    Debug.Log($"'{ingredientName}' 재고 {entries[i].count}개 폐기됨 (유통기한 만료)");
                    entries.RemoveAt(i);
                }
            }
        }

        UpdateAllStockTexts(); // UI 갱신
    }


    public Dictionary<string, List<StockEntry>> GetCurrentStockForSave()
    {
        // 깊은 복사
        var result = new Dictionary<string, List<StockEntry>>();

        foreach (var kv in stock)
        {
            result[kv.Key] = kv.Value
                .Select(e => new StockEntry { count = e.count, dayRemaining = e.dayRemaining })
                .ToList();
        }

        return result;
    }

    public void RestoreStock(Dictionary<string, List<StockEntry>> savedStock)
    {
        stock.Clear();

        foreach (var pair in savedStock)
        {
            stock[pair.Key] = pair.Value
                .Select(e => new StockEntry { count = e.count, dayRemaining = e.dayRemaining })
                .ToList();
        }

        UpdateAllStockTexts();
    }
}
