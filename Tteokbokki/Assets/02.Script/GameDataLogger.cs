using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using System.Linq;
using System;

public class GameDataLogger : MonoBehaviour
{
    public static GameDataLogger Instance { get; private set; }

    private string filePath;

    // === [일일 데이터 저장소] ===
    // 1. 경제
    public int DayStartBalance { get; private set; }
    public int ExpenseTax { get; private set; }
    public int ExpenseShopping { get; private set; }
    public int IncomeBonus { get; private set; }

    // 2. 게임플레이 (주문 실패 요인 카운트)
    public int FailTimeoutCount { get; private set; }
    public int FailMistakeCount { get; private set; }
    public int FailTrashCount { get; private set; } // 영수증 휴지통 거절

    public int DayStartUnlockedCount { get; private set; }

    // 3. 재료 통계 (Key: 재료명)
    public class IngredientDailyStat
    {
        public int Used;   // 냄비에 넣은 횟수 (총 사용량)
        public int Sold;   // 성공한 주문에 포함되어 나간 양
        public int Bought; // 오늘 구매한 양
        // Waste = Used - Sold 로 계산
    }
    private Dictionary<string, IngredientDailyStat> dailyIngredientStats = new Dictionary<string, IngredientDailyStat>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 파일 경로: C:/Users/사용자명/AppData/LocalLow/회사명/게임명/GamePlayLog.csv
        filePath = Path.Combine(Application.persistentDataPath, "GamePlayLog.csv");
        Debug.Log($"[Logger] 로그 파일 경로: {filePath}");
    }

    // 하루 시작 시 초기화 (GameManager.StartOfDay에서 호출)
    public void StartNewDayLog(int currentBalance)
    {
        DayStartBalance = currentBalance;
        ExpenseTax = 0;
        ExpenseShopping = 0;
        IncomeBonus = 0;

        FailTimeoutCount = 0;
        FailMistakeCount = 0;
        FailTrashCount = 0;

        dailyIngredientStats.Clear();

        if (IngredientStockManager.Instance != null)
        {
            DayStartUnlockedCount = IngredientStockManager.Instance.GetPurchasedIngredientCount();
        }
    }

    // === [데이터 수집용 함수들] ===
    public void AddTaxExpense(int amount) => ExpenseTax += amount;
    public void AddShoppingExpense(int amount) => ExpenseShopping += amount;
    public void AddBonusIncome(int amount) => IncomeBonus += amount;

    public void CountFail(string reason)
    {
        switch (reason)
        {
            case "Timeout": FailTimeoutCount++; break;
            case "Mistake": FailMistakeCount++; break;
            case "Trash": FailTrashCount++; break;
        }
    }

    // 재료 사용 시 (냄비 투입)
    public void LogIngredientUsed(string ingredientName, int count = 1)
    {
        GetStat(ingredientName).Used += count;
    }

    // 재료 판매 시 (주문 성공 시 영수증 내용 분석해서 호출)
    public void LogIngredientSold(string ingredientName, int count = 1)
    {
        GetStat(ingredientName).Sold += count;
    }

    // 재료 구매 시
    public void LogIngredientBought(string ingredientName, int count)
    {
        GetStat(ingredientName).Bought += count;
    }

    private IngredientDailyStat GetStat(string name)
    {
        if (!dailyIngredientStats.ContainsKey(name))
            dailyIngredientStats[name] = new IngredientDailyStat();
        return dailyIngredientStats[name];
    }

    // === [CSV 저장 핵심 로직] ===
    // GameManager.EndOfDay 마지막에 호출
    public void SaveDailyLog()
    {
        StringBuilder sb = new StringBuilder();

        List<string> allDbKeys = GetSortedIngredientKeys();

        // 1. 헤더(Header) 작성 (파일이 없거나 비어있을 때만)
        if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0)
        {
            sb.Append("Day,Date,"); // A. 기본
            sb.Append("Balance_Start,Income_Sales,Income_Bonus,Expense_Tax,Expense_Shopping,Balance_End,Opportunity_Loss,"); // B. 경제
            sb.Append("Order_Total,Order_Success,Order_Fail_Total,Order_Fail_Timeout,Order_Fail_Mistake,Order_Fail_Trash,Success_Rate,"); // C. 플레이
            sb.Append("Cumul_Success_Rate,Unlocked_Ing_Count,Consecutive_Zero_Days,"); // D. 엔딩

            // ✨ [수정] 수집된 재료가 아니라, '전체 DB 키'를 순서대로 순회하며 헤더 생성
            foreach (var ing in allDbKeys)
            {
                sb.Append($"{ing}_Used,{ing}_Sold,{ing}_Waste,{ing}_Bought,");
            }
            sb.AppendLine();
        }

        // 2. 값(Value) 채우기
        // [A] 기본 정보
        int day = GetDayCount();
        string date = GameClock.gameTime.ToString("yyyy-MM-dd");
        sb.Append($"{day},{date},");

        // [B] 경제
        int sales = ReceiptLineManager.Instance.GetTotalSuccessfulAmount();
        int balanceEnd = PlayerWalletManager.Instance.CurrentBalance;
        int oppLoss = ReceiptLineManager.Instance.GetTotalMissedAmount(); // 기회 비용

        sb.Append($"{DayStartBalance},{sales},{IncomeBonus},{ExpenseTax},{ExpenseShopping},{balanceEnd},{oppLoss},");

        // [C] 플레이
        int successCount = ReceiptLineManager.Instance.GetSuccessfulReceipts().Count;
        int failTotal = FailTimeoutCount + FailMistakeCount + FailTrashCount;
        int totalOrders = successCount + failTotal;
        float successRate = totalOrders > 0 ? (float)successCount / totalOrders * 100f : 0f;

        sb.Append($"{totalOrders},{successCount},{failTotal},{FailTimeoutCount},{FailMistakeCount},{FailTrashCount},{successRate:F1}%,");

        // [D] 엔딩
        int totalSuccess = GameManager.Instance.TotalSuccessCount;
        int totalMissed = GameManager.Instance.TotalMissedCount;
        float cumulRate = (totalSuccess + totalMissed) > 0
            ? (float)totalSuccess / (totalSuccess + totalMissed) * 100f
            : 0f;
        int unlockedCount = DayStartUnlockedCount;
        int zeroDays = GameManager.Instance.ConsecutiveZeroSuccessDays;

        sb.Append($"{cumulRate:F1}%,{unlockedCount},{zeroDays},");

        // [E] 재료 상세 (여기가 핵심!)
        // ✨ [수정] 수집된 stats가 아니라, 위에서 가져온 '전체 DB 키(allDbKeys)'를 기준으로 순회합니다.
        foreach (var key in allDbKeys)
        {
            // 만약 오늘 기록된 데이터가 있다면 그 값을 쓰고,
            if (dailyIngredientStats.ContainsKey(key))
            {
                var s = dailyIngredientStats[key];
                int waste = s.Used - s.Sold; // 낭비량 = 사용량 - 판매량
                sb.Append($"{s.Used},{s.Sold},{waste},{s.Bought},");
            }
            // ✨ 기록된 게 없다면(오늘 안 씀/안 삼) 0,0,0,0을 채워 넣어 칸을 유지합니다.
            else
            {
                sb.Append("0,0,0,0,");
            }
        }
        sb.AppendLine();

        // 3. 파일 쓰기
        try
        {
            File.AppendAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[Logger] 밸런싱 데이터 저장 완료: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Logger] 데이터 저장 실패: {e.Message}");
        }
    }

    private int GetDayCount()
    {
        TimeSpan span = GameClock.gameTime.Date - 
            new DateTime(GameClock.Instance.startYear, GameClock.Instance.startMonth, GameClock.Instance.startDay).Date;
        return (int)span.TotalDays + 1;
    }

    private List<string> GetSortedIngredientKeys()
    {
        return IngredientEconomyDatabase.SortOrder;
    }
}