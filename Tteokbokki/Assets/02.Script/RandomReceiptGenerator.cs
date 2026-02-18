using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Unity.VisualScripting;
using UnityEngine.Windows;
using System.Collections;
using SaveData;
using System.Linq;

//[Serializable]
//public class KeyValueStringInt
//{
//    public string Key;
//    public int Value;
//}

//[Serializable]
//public class ReceiptData
//{
//    public int OrderID;
//    public string OrderDateTime;
//    public List<OrderItemData> Orders;
//}

//[Serializable]
//public class OrderItemData
//{
//    public string MenuName;
//    public int BasePrice;
//    public List<KeyValueStringInt> Extras = new List<KeyValueStringInt>();
//}

[Serializable]
public class ReceiptsWrapper
{
    public List<ReceiptData> Receipts;
}



public class RandomReceiptGenerator : MonoBehaviour
{
    [SerializeField]
    private static readonly HashSet<string> excludedExtras = new HashSet<string>
{
    "마라 소스",
    "로제 소스",
    "크림 소스",
    "간장 소스",
    "카레 소스",
    "짜장 소스",
};
    // ✨ [NEW] 메뉴별 금지 재료 리스트 (여기 정의된 조합은 절대 나오지 않음)
    private readonly Dictionary<string, List<string>> menuSpecificForbiddenExtras = new Dictionary<string, List<string>>
    {
        { "크림 군자 떡볶이", new List<string> { "군자 소스" } },
        { "간장 군자 떡볶이", new List<string> { "군자 소스" } }
    };

    public TextMeshProUGUI receiptText;
    public TMP_InputField orderIDInput;
    public TMP_InputField dateInput;
    public Button generateButton;
    public Button searchByOrderIDButton;
    public Button searchByDateButton;

    public GameClock gameClock;  // 현재 게임 시간 가져오는 컴포넌트 (외부 연결)
    public CombinedIngredientManager combinedIngredientManager;  // RandomReceiptGenerator에 Inspector에서 연결
    public ReceiptUIManager receiptUIManager;
    public ReceiptLineManager receiptLineManager;

    private ReceiptManager receiptManager;

    private DateTime lastOrderTime;  // 마지막 주문 시간 저장

    void Start()
    {
        // 현재 게임 날짜 기반으로 오늘의 영수증 관리 객체 생성
        DateTime currentGameTime = gameClock.GetCurrentGameTime();
        receiptManager = new ReceiptManager(currentGameTime);

        // 게임 시작 시 영수증 자동 생성
        //GenerateAndDisplayReceipt();

        // 버튼 이벤트 연결
        generateButton.onClick.AddListener(GenerateAndDisplayReceipt);
        searchByOrderIDButton.onClick.AddListener(SearchReceiptByOrderID);
        searchByDateButton.onClick.AddListener(SearchReceipts);

        lastOrderTime = currentGameTime;  // 게임 시작과 동시에 초기화
    }

    void Update()
    {
        
    }

    public void ShowReceiptIngredients(int orderID)     //특정 주문 번호의 영수증의 재료 합산 출력
    {
        //Debug.Log($"ShowReceiptIngredients 호출됨 - 주문번호 {orderID}");

        var foundReceipt = receiptManager.FindReceiptByOrderID(orderID);
        if (foundReceipt == null)
        {
            combinedIngredientManager.combinedIngredientsText.text = $"주문번호 {orderID} 없음";
            return;
        }

        //Debug.Log("영수증 찾음");

        string result = $"주문번호 {foundReceipt.OrderID}의 메뉴별 재료 목록\n\n";

        foreach (var order in foundReceipt.GetOrders())
        {
            //Debug.Log($"메뉴: {order.Menu.Name}");

            var combined = CombinedIngredientManager.GetCombinedIngredients(order.Menu, order.GetExtras());

            result += $"[{order.ItemID}] {order.Menu.Name} 전체 재료 목록\n";
            result += CombinedIngredientManager.GetIngredientsText(combined);
            result += "\n";
        }

        combinedIngredientManager.combinedIngredientsText.text = result;
        //Debug.Log($"재료 목록 출력 완료:\n{result}");
    }
    // 🔔 랜덤 영수증 생성 + 화면 출력 + 저장
    public void GenerateAndDisplayReceipt()
    {
        DateTime orderTime = gameClock.GetCurrentGameTime();
        Receipt newReceipt = new Receipt(orderTime);

        // 1. 메뉴 필터링
        var availableMenus = GetAvailableMenus();
        if (availableMenus.Count == 0)
        {
            Debug.LogWarning("재고로 조리 가능한 메뉴가 없습니다.");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerEmergencyClose("재고로 조리 가능한 메뉴가 없습니다!");
            }
            return;
        }


        int menuCount = GetRandomWeightedValue(new int[] { 84, 15, 1}) + 1;

        for (int i = 0; i < menuCount; i++)
        {
            string menuName = availableMenus[UnityEngine.Random.Range(0, availableMenus.Count)];

            bool hasExtras = GetRandomWeightedValue(new int[] { 40, 60 }) == 1;

            Dictionary<string, int> extras = new();
            if (hasExtras)
            {
                HashSet<string> currentExclusions = new HashSet<string>(excludedExtras); // 기본 제외 목록 복사

                if (menuSpecificForbiddenExtras.TryGetValue(menuName, out var forbiddenList))
                {
                    foreach (var forbidden in forbiddenList)
                    {
                        currentExclusions.Add(forbidden);
                    }
                }

                var extrasStock = GetAvailableExtras(currentExclusions); // 현재 가능한 추가재료
                // 가능한 재료가 있을 때만 뽑기
                if (extrasStock.Count > 0)
                {
                    int extraCount = GetRandomWeightedValue(new int[] { 20, 20, 20, 20, 10, 10 }) + 1;
                    extras = GetRandomExtras(extraCount, extrasStock);
                }
            }

            var totalIngredients = CombinedIngredientManager.GetCombinedIngredients(MenuDatabase.Menus[menuName], extras);
            if (!HasEnoughStock(totalIngredients))
            {
                Debug.Log("[취소] 재고 부족으로 영수증 생성 안됨");
                return;
            }

            newReceipt.AddOrder(menuName, extras);
        }

        receiptManager.AddReceipt(newReceipt);
        receiptText.text = newReceipt.GetReceiptText();

        receiptLineManager.AddNewReceipt(newReceipt); 

        foreach (var order in newReceipt.GetOrders())
        {
            int cost = order.GetTotalCostWithExtras();
            int profit = order.GetProfitWithExtras();

        }
    }

    // 🔔 주문번호로 검색 후 화면 출력
    public void SearchReceiptByOrderID()
    {
        if (int.TryParse(orderIDInput.text, out int orderID))
        {
            Receipt foundReceipt = receiptManager.FindReceiptByOrderID(orderID);
            receiptText.text = foundReceipt != null ? foundReceipt.GetReceiptText() : $"주문번호 {orderID} 없음";
            if(receiptText.text != null)
            {
                ReceiptStateManager.Instance.SetActiveReceipt(foundReceipt);    //주문 번호로 찾은 영수증을 활성화
                //receiptUIManager.UpdateIsCookedDisplay(foundReceipt);       //주문 번호로 찾은 영수증의 조리 완료 여부 표시
            }
        }
        else
        {
            receiptText.text = "올바른 주문번호를 입력하세요.";
        }
        ShowReceiptIngredients(orderID);
    }

    // 🔔 특정 날짜의 모든 영수증 검색 후 출력

    
    public void SearchReceipts()
    {
        string input = dateInput.text.Trim();

        string[] parts = input.Split('_');
        string datePart = parts[0];
        int? orderID = null;

        if (parts.Length > 1 && int.TryParse(parts[1], out int parsedOrderID))
        {
            orderID = parsedOrderID;
        }

        if (!TryParseDate(datePart, out DateTime searchDate))
        {
            receiptUIManager.ShowReceiptText("날짜 형식이 잘못되었습니다 (yyyy-MM-dd, yyyymmdd, yyyy-MM-dd_번호, yyyymmdd_번호)");
            return;
        }

        if (orderID.HasValue)
        {
            var foundReceipt = receiptManager.FindReceiptByDateAndOrderID(searchDate, orderID.Value);

            if (foundReceipt == null)
            {
                receiptText.text = $"{searchDate:yyyy-MM-dd} 주문번호 {orderID}에 해당하는 영수증이 없습니다.";
                ReceiptStateManager.Instance.ClearActiveReceipt();
            }
            else
            {
                receiptText.text = foundReceipt.GetReceiptText();

                // 반드시 검색 성공 시 activeReceipt 업데이트!
                ReceiptStateManager.Instance.SetActiveReceipt(foundReceipt);
            }
        }
        else
        {
            // 날짜 전체 조회 시에는 특정 영수증이 없으므로 activeReceipt 클리어
            receiptText.text = receiptManager.GetReceiptsTextByDate(searchDate);
            ReceiptStateManager.Instance.ClearActiveReceipt();
        }


    }

    private bool TryParseDate(string input, out DateTime parsedDate)
    {
        if (DateTime.TryParse(input, out parsedDate))
        {
            return true;  // yyyy-MM-dd 지원
        }

        if (input.Length == 8 && int.TryParse(input, out _))
        {
            string formatted = $"{input.Substring(0, 4)}-{input.Substring(4, 2)}-{input.Substring(6, 2)}";
            return DateTime.TryParse(formatted, out parsedDate);
        }

        parsedDate = default;
        return false;
    }


    // 🔔 가중치 랜덤 선택
    private int GetRandomWeightedValue(int[] weights)
    {
        int total = 0;
        foreach (int weight in weights) total += weight;

        int rand = UnityEngine.Random.Range(0, total);
        int cumulative = 0;

        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (rand < cumulative) return i;
        }
        return 0;
    }

    // 🔔 랜덤 메뉴 선택
    private string GetRandomMenu()
    {
        var menuNames = new List<string>(MenuDatabase.Menus.Keys);
        return menuNames[UnityEngine.Random.Range(0, menuNames.Count)];
    }
    

    // 랜덤 추가재료 선택 (중복 허용, 개수 합산)
    //private Dictionary<string, int> GetRandomExtras(int count)
    //{
    //    var extras = new Dictionary<string, int>();
    //    var keys = new List<string>(IngredientDatabase.Ingredients.Keys);

    //    for (int i = 0; i < count; i++)
    //    {
    //        string extra = keys[UnityEngine.Random.Range(0, keys.Count - 2)];       // 마라 소스와 로제 소스은 제외
    //        if (extras.ContainsKey(extra))
    //            extras[extra]++;
    //        else
    //            extras[extra] = 1;
    //    }
    //    return extras;
    //}

    // 재고 내에서 중복 허용하여 count개 뽑기
    private Dictionary<string, int> GetRandomExtras(int count, Dictionary<string, int> stockMap)
    {
        Dictionary<string, int> extras = new();

        for (int i = 0; i < count; i++)
        {
            if (stockMap.Count == 0) break;

            var keys = stockMap.Keys.ToList();
            string selected = keys[UnityEngine.Random.Range(0, keys.Count)];

            // 추가
            extras.TryAdd(selected, 0);
            extras[selected]++;

            stockMap[selected]--;
            if (stockMap[selected] <= 0)
                stockMap.Remove(selected);
        }

        return extras;
    }


    // 기본 재료가 모두 재고에 있는 메뉴만 남김
    private List<string> GetAvailableMenus()
    {
        return MenuDatabase.Menus
            .Where(pair => HasEnoughStock(pair.Value.DefaultIngredients))
            .Select(pair => pair.Key)
            .ToList();
    }

    private bool HasEnoughStock(Dictionary<string, int> ingredients)
    {
        foreach (var pair in ingredients)
        {
            int current = IngredientStockManager.Instance.GetStock(pair.Key);
            if (current < pair.Value)
                return false;
        }
        return true;
    }
    // 현재 재고 수량이 1개 이상인 추가재료만 추출
    private Dictionary<string, int> GetAvailableExtras(HashSet<string> currentExclusions)
    {
        return IngredientDatabase.Ingredients.Keys
            .Where(name =>
                IngredientStockManager.Instance.HasPurchasedBefore(name) &&   // 최소 1회 구매
                IngredientStockManager.Instance.GetStock(name) > 0 &&        // 재고 있음
                !currentExclusions.Contains(name))                           // ✨ 전달받은 금지 목록 체크
            .ToDictionary(name => name, name => IngredientStockManager.Instance.GetStock(name));
    }
}
