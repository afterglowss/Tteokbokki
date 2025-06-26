using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

// 🟢 [1] 모든 재료를 저장하는 Ingredient 클래스

public class Ingredient
{
    public string Name { get; }
    public int Price { get; }

    public Ingredient(string name, int price)
    {
        Name = name;
        Price = price;
    }
}

// 🟢 [2] 재료 데이터베이스 (고정된 재료 목록)
public static class IngredientDatabase
{
    public static readonly Dictionary<string, Ingredient> Ingredients = new Dictionary<string, Ingredient>
    {
        { "떡", new Ingredient("떡", 1000) },
        { "오뎅", new Ingredient("오뎅", 1000) },
        { "파", new Ingredient("파", 0) },
        { "배추", new Ingredient("배추", 0) },
        { "군자 소스", new Ingredient("군자 소스", 500) },
        { "체다 치즈", new Ingredient("체다 치즈", 1000) },
        { "중국당면", new Ingredient("중국당면", 2000) },
        { "일반당면", new Ingredient("일반당면", 1000) },
        { "우삼겹", new Ingredient("우삼겹", 3000) },
        { "계란", new Ingredient("계란", 1500) },
        { "메추리알", new Ingredient("메추리알", 1500) },
        { "분모자", new Ingredient("분모자", 2000) },
        { "유부", new Ingredient("유부", 1000) },
        { "곱창", new Ingredient("곱창", 3000) },
        { "마라 소스", new Ingredient("마라 소스", 0) },
        { "로제 크림", new Ingredient("로제 크림", 0) }
    };
}


public class MenuItem
{
    public string Name { get; }
    public int BasePrice { get; }
    public Dictionary<string, int> DefaultIngredients { get; }

    public MenuItem(string name, int basePrice, Dictionary<string, int> ingredients)
    {
        Name = name;
        BasePrice = basePrice;
        DefaultIngredients = new Dictionary<string, int>(ingredients);
    }
}

// 🟢 [4] 메뉴 데이터베이스 (고정된 메뉴 목록)
public static class MenuDatabase
{
    public static readonly Dictionary<string, MenuItem> Menus = new Dictionary<string, MenuItem>
    {
        { "군자 떡볶이", new MenuItem("군자 떡볶이", 12000, new Dictionary<string, int>
            {
                { "떡", 2 },
                { "오뎅", 1 },
                { "파", 1 },
                { "배추", 1 },
                { "군자 소스", 2 }
            })
        },
        { "성인 군자 떡볶이", new MenuItem("성인 군자 떡볶이", 12000, new Dictionary<string, int>
            {
                { "떡", 2 },
                { "오뎅", 1 },
                { "파", 1 },
                { "배추", 1 },
                { "군자 소스", 4 }
            })
        },
        { "곱창 군자 떡볶이", new MenuItem("곱창 군자 떡볶이", 14500, new Dictionary<string, int>
            {
                { "떡", 2 },
                { "오뎅", 1 },
                { "파", 1 },
                { "배추", 1 },
                { "군자 소스", 2 },
                { "곱창", 1 }
            })
        },
        { "마라 군자 떡볶이", new MenuItem("마라 군자 떡볶이", 13000, new Dictionary<string, int>
            {
                { "떡", 2 },
                { "오뎅", 1 },
                { "파", 1 },
                { "배추", 1 },
                { "군자 소스", 2 },
                { "마라 소스", 1 }
            })
        },
        { "로제 군자 떡볶이", new MenuItem("로제 군자 떡볶이", 13000, new Dictionary<string, int>
            {
                { "떡", 2 },
                { "오뎅", 1 },
                { "파", 1 },
                { "배추", 1 },
                { "군자 소스", 2 },
                { "로제 크림", 1 }
            })
        }
    };
}


public class Receipt
{
    private static int OrderCounter = 1;

    public int OrderID { get; }
    public DateTime OrderDateTime { get; }
    private List<OrderItem> orders;

    public Receipt(DateTime orderTime, int? fixedOrderID = null)
    {
        OrderID = fixedOrderID ?? OrderCounter++;
        OrderDateTime = orderTime;
        orders = new List<OrderItem>();
    }

    public void AddOrder(string menuName, Dictionary<string, int> extras)
    {
        if (MenuDatabase.Menus.ContainsKey(menuName))
        {
            orders.Add(new OrderItem(MenuDatabase.Menus[menuName], extras));
        }
    }

    public string GetReceiptText()
    {
        string result = $"=== 주문번호: {OrderID} ===\n";
        result += $"주문일시: {OrderDateTime:yyyy-MM-dd HH:mm}\n";

        int total = 0;
        foreach (var order in orders)
        {
            result += order.GetOrderText();
            total += order.TotalPrice;
        }

        result += "=====================\n";
        result += $"총 주문 금액: {total}원\n";
        return result;
    }

    public List<OrderItem> GetOrders() => orders;

    public Dictionary<string, int> GetExtras(int orderIndex) => orders[orderIndex].GetExtras();
}

public class OrderItem
{
    private static int OrderItemCounter = 1;
    public int ItemID { get; }
    public MenuItem Menu { get; }
    private Dictionary<string, int> Extras { get; }  // 추가 재료 이름과 개수 저장
    public bool IsCompleted { get; private set; }   //조리 성공 여부
    public bool IsOnStove { get; private set; } = false;  //  화구 올라간 여부 추가
    public int TotalPrice
    {
        get
        {
            int total = Menu.BasePrice;
            foreach (var extra in Extras)
            {
                total += IngredientDatabase.Ingredients[extra.Key].Price * extra.Value;
            }
            return total;
        }
    }

    public OrderItem(MenuItem menu, Dictionary<string, int> extraCounts)
    {
        ItemID = OrderItemCounter++;
        Menu = menu;
        Extras = new Dictionary<string, int>(extraCounts);  // 추가 재료 저장
        IsCompleted = false;  // 초기엔 조리 전 상태
    }
    public void MarkAsCompleted()
    {
        IsCompleted = true;
        IsOnStove = false;  // 화구에서 내려옴
    }
    public void PlaceOnStove()
    {
        IsOnStove = true;   // 화구에 올려진 상태로 변경
    }
    public string GetOrderText()
    {
        string result = $"[{ItemID}] {Menu.Name} - {Menu.BasePrice}원\n";   // [{ItemID}]
        if (Extras.Count > 0)
        {
            result += "  추가 재료:\n";
            foreach (var extra in Extras)
            {
                int price = IngredientDatabase.Ingredients[extra.Key].Price * extra.Value;
                result += $"  + {extra.Key} x{extra.Value} ({price}원)\n";
            }
        }
        result += $"  합계: {TotalPrice}원\n\n";
        return result;
    }

    public Dictionary<string, int> GetExtras()
    {
        return new Dictionary<string, int>(Extras);
    }
}


public class ReceiptManager
{
    private List<Receipt> receipts = new List<Receipt>();
    private string todayFilePath;
    private const string ReceiptsFolder = "Receipts";

    public ReceiptManager(DateTime today)
    {
        string folderPath = Path.Combine(Application.dataPath, ReceiptsFolder);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fileName = $"{today:yyyy-MM-dd}_ReceiptData.json";
        todayFilePath = Path.Combine(folderPath, fileName);

        if (File.Exists(todayFilePath))
        {
            File.Delete(todayFilePath);  // 새날짜 시작 시 초기화
        }
    }

    public void AddReceipt(Receipt receipt)
    {
        receipts.Add(receipt);
        SaveReceiptsForToday();
    }

    public Receipt FindReceiptByOrderID(int orderID)
    {
        return receipts.Find(r => r.OrderID == orderID);
    }
    
    public string GetReceiptsTextByDate(DateTime date)
    {
        string folderPath = Path.Combine(Application.dataPath, ReceiptsFolder);
        string fileName = $"{date:yyyy-MM-dd}_ReceiptData.json";
        string filePath = Path.Combine(folderPath, fileName);

        if (!File.Exists(filePath))
        {
            return $"{date:yyyy-MM-dd} 에 해당하는 데이터 파일이 없습니다.";
        }

        string json = File.ReadAllText(filePath);
        var wrapper = JsonUtility.FromJson<ReceiptsWrapper>(json);

        if (wrapper?.Receipts == null || wrapper.Receipts.Count == 0)
        {
            return $"{date:yyyy-MM-dd} 에 해당하는 영수증이 없습니다.";
        }

        string result = $"=== {date:yyyy-MM-dd} 영수증 목록 ===\n\n";
        int dayTotal = 0;

        foreach (var receiptData in wrapper.Receipts)
        {
            result += FormatReceiptData(receiptData, out int receiptTotal);
            dayTotal += receiptTotal;
        }

        result += $"{date:yyyy-MM-dd} 총 매출: {dayTotal}원\n";
        return result;
    }
    public string GetReceiptTextByDateAndOrderID(DateTime date, int orderID)
    {
        string folderPath = Path.Combine(Application.dataPath, ReceiptsFolder);
        string fileName = $"{date:yyyy-MM-dd}_ReceiptData.json";
        string filePath = Path.Combine(folderPath, fileName);

        if (!File.Exists(filePath))
        {
            return $"{date:yyyy-MM-dd} 에 해당하는 데이터 파일이 없습니다.";
        }

        string json = File.ReadAllText(filePath);
        var wrapper = JsonUtility.FromJson<ReceiptsWrapper>(json);

        if (wrapper?.Receipts == null || wrapper.Receipts.Count == 0)
        {
            return $"{date:yyyy-MM-dd} 에 해당하는 영수증이 없습니다.";
        }

        var receiptData = wrapper.Receipts.Find(r => r.OrderID == orderID);
        if (receiptData == null)
        {
            return $"{date:yyyy-MM-dd} 주문번호 {orderID}에 해당하는 영수증이 없습니다.";
        }

        return FormatReceiptData(receiptData);
    }

    public Receipt FindReceiptByDateAndOrderID(DateTime date, int orderID)
    {
        string folderPath = Path.Combine(Application.dataPath, "Receipts");
        string fileName = $"{date:yyyy-MM-dd}_ReceiptData.json";
        string filePath = Path.Combine(folderPath, fileName);

        if (!File.Exists(filePath))
        {
            return null;
        }

        string json = File.ReadAllText(filePath);
        var wrapper = JsonUtility.FromJson<ReceiptsWrapper>(json);

        var data = wrapper.Receipts.Find(r => r.OrderID == orderID);
        if (data == null)
        {
            return null;
        }

        // ReceiptData → Receipt 변환 (재구성)
        var receipt = new Receipt(DateTime.Parse(data.OrderDateTime), data.OrderID);

        foreach (var orderData in data.Orders)
        {
            Dictionary<string, int> extras = new Dictionary<string, int>();
            foreach (var extra in orderData.Extras)
            {
                extras[extra.Key] = extra.Value;
            }
            receipt.AddOrder(orderData.MenuName, extras);
        }

        return receipt;
    }


    private string FormatReceiptData(ReceiptData receiptData)
    {
        return FormatReceiptData(receiptData, out _);  // 총합 필요 없으니 버림
    }

    private string FormatReceiptData(ReceiptData receiptData, out int receiptTotal)
    {
        string result = $"=== 주문번호: {receiptData.OrderID} ===\n";
        result += $"주문일시: {receiptData.OrderDateTime}\n\n";

        receiptTotal = 0;

        foreach (var order in receiptData.Orders)
        {
            int menuTotal = order.BasePrice;  // 메뉴 기본 가격 시작
            result += $"{order.MenuName} - {order.BasePrice}원\n";

            if (order.Extras != null && order.Extras.Count > 0)
            {
                result += "  추가 재료:\n";
                foreach (var extra in order.Extras)
                {
                    if (IngredientDatabase.Ingredients.TryGetValue(extra.Key, out var ingredient))
                    {
                        int extraPrice = ingredient.Price * extra.Value;
                        menuTotal += extraPrice;
                        result += $"  + {extra.Key} x{extra.Value} ({extraPrice}원)\n";
                    }
                    else
                    {
                        result += $"  + {extra.Key} x{extra.Value} (가격 정보 없음)\n";
                    }
                }
            }

            result += $"  메뉴 총 가격: {menuTotal}원\n\n";
            receiptTotal += menuTotal;  // 영수증 총합에 더함
        }

        result += $"영수증 총 금액: {receiptTotal}원\n";
        result += "------------------\n\n";
        return result;
    }


    private void SaveReceiptsForToday()
    {
        var wrapper = new ReceiptsWrapper { Receipts = new List<ReceiptData>() };

        foreach (var receipt in receipts)
        {
            var receiptData = new ReceiptData
            {
                OrderID = receipt.OrderID,
                OrderDateTime = receipt.OrderDateTime.ToString("yyyy-MM-dd HH:mm"),
                Orders = new List<OrderItemData>()
            };

            foreach (var order in receipt.GetOrders())
            {
                var orderData = new OrderItemData
                {
                    MenuName = order.Menu.Name,
                    BasePrice = order.Menu.BasePrice,
                    Extras = new List<KeyValueStringInt>()
                };

                foreach (var extra in order.GetExtras())
                {
                    orderData.Extras.Add(new KeyValueStringInt { Key = extra.Key, Value = extra.Value });
                }

                receiptData.Orders.Add(orderData);
            }

            wrapper.Receipts.Add(receiptData);
        }

        File.WriteAllText(todayFilePath, JsonUtility.ToJson(wrapper, true));
    }

    // 하루 동안 놓친 영수증 저장하기
    public static void SaveMissedReceipts(List<Receipt> missedReceipts, DateTime date)
    {
        if (missedReceipts == null || missedReceipts.Count == 0) return;

        string folderPath = Path.Combine(Application.dataPath, "Receipts/Missed");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string fileName = $"{date:yyyy-MM-dd}_MissedReceipts.json";
        string filePath = Path.Combine(folderPath, fileName);

        var wrapper = new ReceiptsWrapper { Receipts = new List<ReceiptData>() };

        foreach (var receipt in missedReceipts)
        {
            var receiptData = new ReceiptData
            {
                OrderID = receipt.OrderID,
                OrderDateTime = receipt.OrderDateTime.ToString("yyyy-MM-dd HH:mm"),
                Orders = new List<OrderItemData>()
            };

            foreach (var order in receipt.GetOrders())
            {
                var orderData = new OrderItemData
                {
                    MenuName = order.Menu.Name,
                    BasePrice = order.Menu.BasePrice,
                    Extras = new List<KeyValueStringInt>()
                };

                foreach (var extra in order.GetExtras())
                {
                    orderData.Extras.Add(new KeyValueStringInt { Key = extra.Key, Value = extra.Value });
                }

                receiptData.Orders.Add(orderData);
            }

            wrapper.Receipts.Add(receiptData);
        }

        File.WriteAllText(filePath, JsonUtility.ToJson(wrapper, true));
    }

}

public class ReceiptSystem : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
