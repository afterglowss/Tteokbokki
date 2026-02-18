using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// ✨ [NEW] 주방용 버튼 이미지를 Inspector에서 연결하기 위한 데이터 구조
[System.Serializable]
public struct IngredientVisualData
{
    public string ingredientName;
    public Sprite kitchenButtonSprite; // 상점 이미지와 다른, 주방용 이미지
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

    [Header("UI Generation Settings")]
    public GameObject ingredientButtonPrefab;
    public GameObject sauceButtonPrefab;
    public Transform ingredientGridParent; // ✨ 일반 재료용 Grid (위쪽 선반)
    public Transform sauceGridParent;      // ✨ 소스용 Grid (아래쪽 선반)
    public bool autoGenerateButtons = true;

    [Header("Kitchen Visuals")]
    // ✨ 재료 이름과 주방용 스프라이트를 매핑할 리스트
    public List<IngredientVisualData> kitchenSprites;

    [Header("Debug / Settings")]
    public bool startWithBasicIngredients = true;
    public bool debugUnlockAllIngredients = false;
    public string[] basicIngredients;

    // 현재 재고 데이터
    private Dictionary<string, List<StockEntry>> stock = new();

    // ✨ [변경] 텍스트를 직접 관리하지 않고, 등록된 버튼들을 관리함
    private Dictionary<string, IngredientButton> registeredButtons = new();

    public int TotalSpent { get; private set; } = 0;
    private HashSet<string> orderedToday = new();
    private const int ShelfLifeDays = 5;
    private List<string> purchasedAtLeastOnce = new List<string>();
    // ✨ 오늘 주문했는지 여부 (상점 UI 표시용으로만 사용하고, 주문 차단용으로는 쓰지 않음)
    private HashSet<string> orderedIngredientsToday = new HashSet<string>();
    private List<string> lowStockIngredients = new();


    // ✨ [변경] KeyCode 대신 InputSystem의 'Key' 사용
    private readonly Key[] row1Keys = { Key.Q, Key.W, Key.E, Key.R, Key.T, Key.Y, Key.U };
    private readonly Key[] row2Keys = { Key.A, Key.S, Key.D, Key.F, Key.G, Key.H, Key.J };
    private readonly Key[] row3Keys = { Key.Z, Key.X, Key.C, Key.V, Key.B, Key.N, Key.M };

    // ✨ 매핑 딕셔너리 타입 변경 (Key -> string)
    private Dictionary<Key, string> keyToIngredientMap = new Dictionary<Key, string>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeStock();
    }

    void Start()
    {
        if (debugUnlockAllIngredients) UnlockAllIngredientsHistory();
        // if (startWithBasicIngredients) OrderBasicIngredients();

        // (예시: 세이브 매니저가 로드할 데이터가 없다면 기본 재료 지급)
        // if (!GameSaveManager.Instance.HasSaveData) 
        // {
        //      OrderBasicIngredients();
        // }

        // ✨ 자동 생성 옵션이 켜져있다면 여기서 버튼을 쫙 만듭니다.
        if (autoGenerateButtons)
        {
            GenerateIngredientButtons();
        }
    }
    // ✨ [변경] 반환 타입 KeyCode -> Key
    public List<Key> GetAllRegisteredKeys()
    {
        return keyToIngredientMap.Keys.ToList();
    }

    // ✨ [변경] 매개변수 타입 KeyCode -> Key
    public string GetIngredientByKey(Key key)
    {
        if (keyToIngredientMap.TryGetValue(key, out string name))
        {
            return name;
        }
        return null;
    }

    public void GenerateIngredientButtons()
    {
        ClearGrid(ingredientGridParent);
        ClearGrid(sauceGridParent);
        registeredButtons.Clear();
        keyToIngredientMap.Clear();

        Dictionary<string, Sprite> spriteMap = new Dictionary<string, Sprite>();
        foreach (var data in kitchenSprites)
        {
            if (!spriteMap.ContainsKey(data.ingredientName))
                spriteMap.Add(data.ingredientName, data.kitchenButtonSprite);
        }

        List<string> sauceList = new List<string>();
        List<string> normalList = new List<string>();

        // ✨ [핵심 수정] DB 순서(IngredientEconomyDatabase.Data.Keys)가 아니라
        // 플레이어가 해금한 순서(purchasedAtLeastOnce)대로 순회합니다.
        foreach (var ingredientName in purchasedAtLeastOnce)
        {
            // 이미 구매한 목록을 도는 것이므로 Contains 체크는 불필요하지만 안전장치로 둡니다.
            if (string.IsNullOrEmpty(ingredientName)) continue;

            if (ingredientName.Contains("소스")) sauceList.Add(ingredientName);
            else normalList.Add(ingredientName);
        }

        CreateButtons(normalList, ingredientGridParent, ingredientButtonPrefab, spriteMap, false);
        CreateButtons(sauceList, sauceGridParent, sauceButtonPrefab, spriteMap, true);

        UpdateAllStockTexts();
    }

    private void CreateButtons(List<string> ingredients, Transform parent, GameObject prefab, Dictionary<string, Sprite> spriteMap, bool isSauceRow)
    {
        if (parent == null || prefab == null) return;

        int index = 0;
        foreach (var name in ingredients)
        {
            GameObject obj = Instantiate(prefab, parent);
            IngredientButton btn = obj.GetComponent<IngredientButton>();

            if (btn != null)
            {
                Sprite icon = null;
                if (spriteMap.TryGetValue(name, out Sprite s)) icon = s;
                btn.Setup(name, icon);

                // ✨ Key 할당 로직 (타입만 Key로 변경됨)
                Key assignedKey = Key.None;

                if (isSauceRow)
                {
                    if (index < row3Keys.Length) assignedKey = row3Keys[index];
                }
                else
                {
                    if (index < row1Keys.Length)
                        assignedKey = row1Keys[index];
                    else if (index - row1Keys.Length < row2Keys.Length)
                        assignedKey = row2Keys[index - row1Keys.Length];
                }

                if (assignedKey != Key.None)
                {
                    btn.SetHotkeyDisplay(assignedKey.ToString());
                    keyToIngredientMap[assignedKey] = name;
                }
                else
                {
                    btn.SetHotkeyDisplay("");
                }
            }
            index++;
        }
    }

    private void ClearGrid(Transform grid)
    {
        if (grid == null) return;
        foreach (Transform child in grid)
        {
            Destroy(child.gameObject);
        }
    }

    // ✨ [NEW] 버튼이 스스로를 등록할 때 호출하는 함수
    public void RegisterIngredientButton(IngredientButton btn)
    {
        if (string.IsNullOrEmpty(btn.ingredientName)) return;

        if (!registeredButtons.ContainsKey(btn.ingredientName))
        {
            registeredButtons.Add(btn.ingredientName, btn);
        }
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

        //UpdateAllStockTexts();
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

        /*
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
        */

        // ✨ [NEW] 단순 재고 관리 로직
        // 리스트의 첫 번째 뭉치에서 그냥 뺍니다.
        var entry = stock[ingredientName][0];
        if (entry.count > 0)
        {
            entry.count--;
            // 0개가 되어도 항목 자체를 지우진 않습니다 (단일 슬롯 유지)
            // 필요하다면 0일 때 Remove 해도 되지만, 
            // 단일 관리 체제에서는 entry를 남겨두고 count만 0으로 두는 게 관리하기 편합니다.

            UpdateStockText(ingredientName);

            CheckGameoverCondition();
            return true;
        }

        return false;
    }
    // ✨ [NEW] 필수 재료나 소스가 전멸했는지 검사하는 함수
    private void CheckGameoverCondition()
    {
        // 1. 4대 필수 재료 검사 (하나라도 0이면 게임 오버)
        string[] essentials = { "떡", "오뎅", "파", "양배추" };
        foreach (var item in essentials)
        {
            if (GetStock(item) <= 0)
            {
                GameManager.Instance.TriggerEmergencyClose($"필수 재료 '{item}' 소진!");
                return;
            }
        }

        // 2. 소스 전멸 검사 (모든 소스 합계가 0이면 게임 오버)
        string[] sauces = {
            "군자 소스", "마라 소스", "로제 소스", "크림 소스",
            "간장 소스", "카레 소스", "짜장 소스"
        };

        bool hasAnySauce = false;
        foreach (var sauce in sauces)
        {
            if (GetStock(sauce) > 0)
            {
                hasAnySauce = true;
                break; // 소스가 하나라도 있으면 생존
            }
        }

        if (!hasAnySauce)
        {
            GameManager.Instance.TriggerEmergencyClose("모든 소스 소진!");
        }
    }


    /// <summary>
    /// 고정된 주문량 기준으로 재료 보충
    /// </summary>
    public void OrderIngredient(string ingredientName)
    {
        if (!IngredientEconomyDatabase.Data.TryGetValue(ingredientName, out var meta)) return;
        // if (orderedToday.Contains(ingredientName)) return;

        int servings = meta.ServingsPerOrder;
        int cost = meta.OrderCost;

        bool paid = PlayerWalletManager.Instance.Spend(cost);
        if (!paid) return;

        if (!stock.ContainsKey(ingredientName)) stock[ingredientName] = new List<StockEntry>();
        /*
        stock[ingredientName].Add(new StockEntry { count = servings, dayRemaining = ShelfLifeDays });
        */

        // ✨ [NEW] 단일 재고 합산 로직
        // 리스트가 비어있으면 하나 만듭니다.
        if (stock[ingredientName].Count == 0)
        {
            stock[ingredientName].Add(new StockEntry { count = 0, dayRemaining = 999 }); // 999는 의미 없는 더미 값
        }

        // 첫 번째 항목에 수량을 더합니다.
        stock[ingredientName][0].count += servings;
        TotalSpent += cost;
        orderedToday.Add(ingredientName);

        // ✨ [중요] 구매 시 해금 목록에 추가 (중복 방지 필수)
        if (!purchasedAtLeastOnce.Contains(ingredientName))
        {
            purchasedAtLeastOnce.Add(ingredientName); // 리스트의 맨 끝에 추가됨 -> 버튼도 맨 뒤에 생김
        }

        UpdateStockText(ingredientName);

        Debug.Log($"[재료 주문] {ingredientName} {meta.ServingsPerOrder}인분 추가 완료. (현재 총 {GetStock(ingredientName)}인분)");
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
    // ✨ [수정] 텍스트 갱신 로직: 등록된 버튼에게 텍스트를 전달
    public void UpdateStockText(string ingredientName)
    {
        // 1. 해당 재료의 버튼이 등록되어 있는지 확인
        if (!registeredButtons.TryGetValue(ingredientName, out var btn))
            return;

        // 2. 텍스트 내용 계산
        string displayText;
        if (!stock.ContainsKey(ingredientName) || stock[ingredientName].Count == 0)
        {
            displayText = "0"; // 혹은 "재고: 없음"
        }
        else
        {
            // 유통기한별 그룹화 (기존 로직 유지)
            // 예: "10(5일), 3(2일)" 처럼 표시하거나 단순히 총합만 표시할 수도 있음
            // 여기서는 총합만 표시하는 걸로 간소화 예시 (원하시면 기존 그룹화 로직 쓰셔도 됨)

            // var totalCount = stock[ingredientName].Sum(e => e.count);
            // displayText = totalCount.ToString();

            // 기존처럼 상세 표시를 원한다면:
            /*
            var grouped = stock[ingredientName]
               .GroupBy(e => e.dayRemaining)
               .OrderBy(g => g.Key)
               .Select(g => $"{g.Sum(e => e.count)}({g.Key}일)");
            displayText = string.Join("\n", grouped); // 줄바꿈으로 구분
            */

            // ✨ [NEW] 단순 총합 표시
            int totalCount = stock[ingredientName].Sum(e => e.count);
            displayText = totalCount.ToString();
        }

        // 3. 버튼에게 전달
        btn.UpdateStockDisplay(displayText);
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

    public void UpdateLowStockList()
    {
        lowStockIngredients.Clear();

        foreach (var name in purchasedAtLeastOnce)
        {
            int stockCount = GetStock(name);

            if (IngredientEconomyDatabase.Data.TryGetValue(name, out var meta))
            {
                int threshold = meta.LowStockThreshold;

                if (stockCount <= threshold)
                {
                    lowStockIngredients.Add(name);
                }
            }
        }
    }

    public List<string> GetLowStockIngredients()
    {
        return new List<string>(lowStockIngredients);
    }

    public string GetLowStockText()
    {
        if (lowStockIngredients.Count == 0)
            return "추가 주문이 필요한 재료가 없습니다.";

        string result = "추가 주문 필요:\n";
        foreach (var name in lowStockIngredients)
        {
            result += $"- {name} (재고: {GetStock(name)}개)\n";
        }
        return result;
    }
    public int GetTotalCostForLowStockIngredients()
    {
        int totalCost = 0;

        foreach (var ingredientName in lowStockIngredients)
        {
            if (IngredientEconomyDatabase.Data.TryGetValue(ingredientName, out var meta))
            {
                totalCost += meta.OrderCost;
            }
            else
            {
                Debug.LogWarning($"[재고 경고] '{ingredientName}'의 경제 정보를 찾을 수 없습니다.");
            }
        }

        return totalCost;
    }
    public string GetLowStockCostSummaryText()
    {
        int total = GetTotalCostForLowStockIngredients();
        return $"추가 주문 총 비용: {total:N0}원";
    }
    public int GetPurchasedIngredientCount()
    {
        return purchasedAtLeastOnce.Count;
    }

    // ✨ [수정] 해금 여부 확인 (List에는 Contains 메서드가 있으므로 코드는 동일하지만 내부 동작이 바뀜)
    public bool HasPurchasedBefore(string ingredientName)
    {
        return purchasedAtLeastOnce.Contains(ingredientName);
    }

    public void AdvanceDayAndDecay()
    {
        /*
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
        */
        Debug.Log("[시스템] 재료 유통기한 시스템이 비활성화되어 재고가 유지됩니다.");

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
            // 일단 로드
            var loadedList = pair.Value.Select(e => new StockEntry { count = e.count, dayRemaining = e.dayRemaining }).ToList();

            // ✨ [NEW] 유통기한 기능이 꺼져있으므로, 로드한 데이터를 하나로 합칩니다.
            if (loadedList.Count > 1)
            {
                int totalCount = loadedList.Sum(e => e.count);
                loadedList.Clear();
                loadedList.Add(new StockEntry { count = totalCount, dayRemaining = 999 });
            }

            stock[pair.Key] = loadedList;
        }

        //foreach (var key in stock.Keys)
        //{
        //    if (stock[key].Count > 0) purchasedAtLeastOnce.Add(key);
        //}

        if (autoGenerateButtons)
        {
            GenerateIngredientButtons();
        }

        UpdateAllStockTexts();
    }

    public List<string> GetPurchasedHistoryForSave()
    {
        return new List<string>(purchasedAtLeastOnce);
    }

    // ✨ [NEW] 로드용: 해금된 재료 목록 복원 (저장된 순서 그대로 복원됨)
    public void RestorePurchasedHistory(List<string> savedHistory)
    {
        if (savedHistory == null) return;

        purchasedAtLeastOnce.Clear();
        foreach (var name in savedHistory)
        {
            // 중복 방지하며 순서대로 추가
            if (!purchasedAtLeastOnce.Contains(name))
            {
                purchasedAtLeastOnce.Add(name);
            }
        }
    }

    public void OrderBasicIngredients()
    {
        foreach (string ingredientName in basicIngredients)
        {
            if (!IngredientEconomyDatabase.Data.TryGetValue(ingredientName, out var meta)) continue;
            if (!stock.ContainsKey(ingredientName)) stock[ingredientName] = new List<StockEntry>();

            /*
            stock[ingredientName].Add(new StockEntry { count = meta.ServingsPerOrder, dayRemaining = 5 });
            */
            // ✨ [NEW] 단일 재고 합산
            if (stock[ingredientName].Count == 0)
            {
                stock[ingredientName].Add(new StockEntry { count = 0, dayRemaining = 999 });
            }
            stock[ingredientName][0].count += meta.ServingsPerOrder;

            // ✨ 리스트에 순서대로 추가
            if (!purchasedAtLeastOnce.Contains(ingredientName))
            {
                purchasedAtLeastOnce.Add(ingredientName);
            }
            UpdateStockText(ingredientName);
        }

        GenerateIngredientButtons();
    }
    private void UnlockAllIngredientsHistory()
    {
        foreach (var key in IngredientEconomyDatabase.Data.Keys)
        {
            purchasedAtLeastOnce.Add(key);
        }
        Debug.Log("[Debug] 모든 재료의 '구매 이력'이 해금되었습니다. (재고는 0일 수 있음)");
    }

    // ✨ [NEW] 튜토리얼 종료 후 메인 씬 진입 시 딱 한 번 호출할 함수
    public void ApplyTutorialAftermath()
    {
        // 1. 일단 기본 재료 꽉 채우기 (베이스)
        OrderBasicIngredients();

        // 2. 마라 소스 강제 해금 & 지급 (튜토리얼 보상)
        AddStockDirectly("마라 소스");

        // 3. [하드코딩] 튜토리얼에서 쓴 만큼 차감
        // (튜토리얼 진행 중 실제로 쓴 게 아니라, 메인 씬 오면서 '쓴 척' 하는 것)
        DecreaseStockDirectly("떡", 2);
        DecreaseStockDirectly("오뎅", 2);
        DecreaseStockDirectly("파", 1);
        DecreaseStockDirectly("양배추", 1);
        DecreaseStockDirectly("군자 소스", 1);

        GenerateIngredientButtons();

        Debug.Log("[System] 튜토리얼 결과 적용 완료 (마라소스 획득, 재료 차감)");
    }

    // 헬퍼: 돈 차감 없이 재고와 해금 목록에 추가 (마라 소스 보상용)
    private void AddStockDirectly(string name)
    {
        if (!IngredientEconomyDatabase.Data.TryGetValue(name, out var meta)) return;

        // ✨ 해금 목록 추가 (순서 보존)
        if (!purchasedAtLeastOnce.Contains(name)) purchasedAtLeastOnce.Add(name);

        // 재고 리스트 생성
        if (!stock.ContainsKey(name)) stock[name] = new List<StockEntry>();
        if (stock[name].Count == 0) stock[name].Add(new StockEntry { count = 0, dayRemaining = 999 });

        // 수량 추가
        stock[name][0].count += meta.ServingsPerOrder;
    }

    // 헬퍼: 재고 강제 차감 (튜토리얼 사용분 처리용)
    private void DecreaseStockDirectly(string name, int amount)
    {
        if (stock.ContainsKey(name) && stock[name].Count > 0)
        {
            stock[name][0].count -= amount;
            if (stock[name][0].count < 0) stock[name][0].count = 0; // 음수 방지
        }
    }
}
