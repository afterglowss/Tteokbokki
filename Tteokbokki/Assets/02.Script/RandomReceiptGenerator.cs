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
    public float autoOrderIntervalMinutes = 3f;  // 게임시간 3분마다 자동주문

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
        CheckAutoOrder();
    }

    private void CheckAutoOrder()
    {
        DateTime currentGameTime = gameClock.GetCurrentGameTime();
        TimeSpan elapsed = currentGameTime - lastOrderTime;

        if (elapsed.TotalMinutes >= 3)
        {
            GenerateAndDisplayReceipt();
            lastOrderTime = currentGameTime;  // 다음 주문 시간 갱신
        }
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

        int menuCount = GetRandomWeightedValue(new int[] { 70, 20, 5, 5 }) + 1;

        for (int i = 0; i < menuCount; i++)
        {
            string menuName = GetRandomMenu();
            bool hasExtras = GetRandomWeightedValue(new int[] { 40, 60 }) == 1;

            Dictionary<string, int> extraCounts = new Dictionary<string, int>();
            if (hasExtras)
            {
                int extraCount = GetRandomWeightedValue(new int[] { 20, 20, 20, 20, 10, 10 }) + 1;
                extraCounts = GetRandomExtras(extraCount);
            }

            newReceipt.AddOrder(menuName, extraCounts);
        }

        receiptManager.AddReceipt(newReceipt);
        receiptText.text = newReceipt.GetReceiptText();

        //combinedIngredientManager.DisplayAllCombinedIngredients(newReceipt);  // 영수증 생성 후 바로 재료 합산리스트를 출력할 필요없음.
                                                                                // 영수증이 활성화된 상태에서만 재료 합산 리스트를 출력하면 됨.

        receiptLineManager.AddNewReceipt(newReceipt); // 신규 주문 들어옴
        //ReceiptStateManager.Instance.SetActiveReceipt(newReceipt);// 영수증의 생성과 활성화 여부는 다름
                                                                    // 영수증이 생성되었더라도, 플레이어가 영수증을 클릭하지 않는 이상 활성화되지 않음. 
        receiptUIManager.UpdateIsCookedDisplay(newReceipt);         // 새로 생성된 영수증의 조리 완료 여부 표시

        foreach (var order in newReceipt.GetOrders())
        {
            int cost = order.GetTotalCostWithExtras();
            int profit = order.GetProfitWithExtras();

            //Debug.Log($"[영수증 {newReceipt.OrderID}] {order.Menu.Name} - 판매가 {order.TotalPrice}원 / 원가 {cost}원 / 이윤 {profit}원");
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
                receiptUIManager.UpdateIsCookedDisplay(foundReceipt);       //주문 번호로 찾은 영수증의 조리 완료 여부 표시
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

    // 🔔 랜덤 추가재료 선택 (중복 허용, 개수 합산)
    private Dictionary<string, int> GetRandomExtras(int count)
    {
        var extras = new Dictionary<string, int>();
        var keys = new List<string>(IngredientDatabase.Ingredients.Keys);

        for (int i = 0; i < count; i++)
        {
            string extra = keys[UnityEngine.Random.Range(0, keys.Count - 2)];       // 마라 소스와 로제 크림은 제외
            if (extras.ContainsKey(extra))
                extras[extra]++;
            else
                extras[extra] = 1;
        }
        return extras;
    }
}
