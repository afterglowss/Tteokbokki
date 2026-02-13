using Newtonsoft.Json;
using SaveData;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public string gameTime;
    public int playerBalance;
    public Dictionary<string, List<StockEntry>> ingredientStocks;
    public List<string> unlockedIngredients;
    public List<List<Dictionary<string, int>>> packagingArea;
    public List<StoveSlotSaveData> stoveStates;
    public int lastReceiptID;
    public int lastOrderItemID;
    public List<ReceiptData> missedReceipts;
    public List<ReceiptData> successfulReceipts;
    public List<ReceiptSlotSaveData> receiptSlots;
    public List<ReceiptData> pendingReceipts;
    public bool isTutorialCompleted;
    public List<string> tomorrowBonusCandidates;
    public int bonusCycleIndex;

    public int totalSuccessCount; // 2주간 총 성공 횟수
    public int totalMissedCount;  // 2주간 총 실패 횟수
    public int consecutiveZeroSuccessDays; // 연속 0건 성공 일수 (배드엔딩1 용)
}

[Serializable]
public class StoveSlotSaveData
{
    public bool isCooking;
    public bool isCooked;
    public float cookTimeRemaining;
    public Dictionary<string, int> currentIngredients; // 조리 중인 재료
    public Dictionary<string, int> pendingIngredients; // ✨ 추가됨: 담고 있는(대기 중) 재료
}

public class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance { get; private set; }
    public bool IsTutorialCompleted { get; private set; }
    private const string SaveFilePath = "SaveData.json";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SaveGame()
    {
        GameSaveData data = new GameSaveData
        {
            gameTime = GameClock.gameTime.ToString("yyyy-MM-dd HH:mm"),
            playerBalance = PlayerWalletManager.Instance.CurrentBalance,
            ingredientStocks = IngredientStockManager.Instance.GetCurrentStockForSave(),
            unlockedIngredients = IngredientStockManager.Instance.GetPurchasedHistoryForSave(),
            packagingArea = PackagingAreaManager.Instance.GetSlotWiseCookedFoods(),
            stoveStates = new List<StoveSlotSaveData>(),
            isTutorialCompleted = this.IsTutorialCompleted,
            tomorrowBonusCandidates = DailyBonusManager.Instance.GetTomorrowBonusForSave(),
            bonusCycleIndex = DailyBonusManager.Instance.CurrentBonusCycleIndex,

            totalSuccessCount = GameManager.Instance.TotalSuccessCount,
            totalMissedCount = GameManager.Instance.TotalMissedCount,
            consecutiveZeroSuccessDays = GameManager.Instance.ConsecutiveZeroSuccessDays
        };

        // 화구 상태 저장 (Pending 포함)
        foreach (var slot in StoveManager.Instance.stoves)
        {
            StoveSlotSaveData slotData = new StoveSlotSaveData
            {
                isCooking = slot.IsCooking,
                isCooked = slot.IsCooked,
                cookTimeRemaining = slot.GetCookTimeRemaining(),
                currentIngredients = slot.GetRawIngredientsCopy(), // 조리중 재료
                pendingIngredients = slot.GetPendingIngredientsCopy() // ✨ 대기중 재료 저장
            };
            data.stoveStates.Add(slotData);
        }

        data.lastReceiptID = ReceiptSystem.CurrentReceiptID;
        data.lastOrderItemID = ReceiptSystem.CurrentOrderItemID;
        data.receiptSlots = ReceiptLineManager.Instance.GetCurrentReceiptSlots();
        data.pendingReceipts = ReceiptLineManager.Instance.GetPendingReceiptsData();
        data.missedReceipts = ReceiptSystem.GetMissedReceiptsData();
        data.successfulReceipts = ReceiptSystem.GetSuccessfulReceiptsData();

        string json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, SaveFilePath), json);

        Debug.Log("게임 저장 완료!");
    }

    public void LoadGame()
    {
        string fullPath = Path.Combine(Application.persistentDataPath, SaveFilePath);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning("저장 파일이 존재하지 않습니다.");
            return;
        }

        string json = File.ReadAllText(fullPath);
        GameSaveData data = JsonConvert.DeserializeObject<GameSaveData>(json);

        GameClock.SetGameTime(DateTime.Parse(data.gameTime));
        PlayerWalletManager.Instance.SetBalance(data.playerBalance);
        // ✨ [NEW] 재고 복원 전에 해금 목록을 먼저 복원 (중요: 순서)
        // 그래야 재고가 0인 재료도 버튼을 생성할 수 있음
        IngredientStockManager.Instance.RestorePurchasedHistory(data.unlockedIngredients);
        IngredientStockManager.Instance.RestoreStock(data.ingredientStocks);
        PackagingAreaManager.Instance.RestoreSlots(data.packagingArea);
        this.IsTutorialCompleted = data.isTutorialCompleted;

        if (DailyBonusManager.Instance != null)
        {
            // ✨ [NEW] 보너스 후보 + 순서(Cycle) 복원
            DailyBonusManager.Instance.RestoreBonusData(data.tomorrowBonusCandidates, data.bonusCycleIndex);
        }

        // 화구 복원
        for (int i = 0; i < data.stoveStates.Count; i++)
        {
            StoveManager.Instance.stoves[i].RestoreFromSave(data.stoveStates[i]);
        }

        ReceiptLineManager.Instance.RestoreReceiptSlots(data.receiptSlots);

        ReceiptSystem.CurrentReceiptID = data.lastReceiptID;
        ReceiptSystem.CurrentOrderItemID = data.lastOrderItemID;
        ReceiptSystem.RestoreReceipts(data.missedReceipts, data.successfulReceipts);
        ReceiptLineManager.Instance.RestorePendingReceipts(data.pendingReceipts);

        GameManager.Instance.RestoreSessionData(
            data.totalSuccessCount,
            data.totalMissedCount,
            data.consecutiveZeroSuccessDays
        );

        Debug.Log("게임 불러오기 완료!");
    }

    // ... DeleteSaveFile 등 나머지 기존 코드 유지 ...
    public void DeleteSaveFile()
    {
        string fullPath = Path.Combine(Application.persistentDataPath, "SaveData.json");
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    public void SetTutorialComplete()
    {
        // 1. 클래스 내부의 프로퍼티 값을 true로 변경
        IsTutorialCompleted = true;
        // 2. 변경된 상태를 JSON 파일에 즉시 물리적으로 기록
        SaveGame();
        Debug.Log("<color=green>[시스템] 튜토리얼 완료 상태가 세이브 데이터에 기록되었습니다.</color>");
    }
}