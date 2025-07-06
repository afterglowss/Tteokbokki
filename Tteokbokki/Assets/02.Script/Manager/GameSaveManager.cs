using Newtonsoft.Json;
using SaveData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public string gameTime;
    public int playerBalance;
    public Dictionary<string, int> ingredientStocks;
    public Dictionary<string, int> playerWok;
    public List<List<Dictionary<string, int>>> packagingArea;
    public List<StoveSlotSaveData> stoveStates;
    public int lastReceiptID;
    public int lastOrderItemID;
    public List<ReceiptData> missedReceipts;
    public List<ReceiptData> successfulReceipts;
    public List<ReceiptSlotSaveData> receiptSlots;
    public List<ReceiptData> pendingReceipts;
}

[Serializable]
public class StoveSlotSaveData
{
    public bool isCooking;
    public bool isCooked;
    public float cookTimeRemaining;
    public Dictionary<string, int> currentIngredients;
}

public class GameSaveManager : MonoBehaviour
{
    private const string SaveFilePath = "SaveData.json";

    public void SaveGame()
    {
        GameSaveData data = new GameSaveData
        {
            gameTime = GameClock.gameTime.ToString("yyyy-MM-dd HH:mm"),
            playerBalance = PlayerWalletManager.Instance.CurrentBalance,
            ingredientStocks = IngredientStockManager.Instance.GetCurrentStockCopy(),
            playerWok = PlayerWokManager.Instance.GetPlayerIngredients(),
            packagingArea = PackagingAreaManager.Instance.GetSlotWiseCookedFoods(),
            stoveStates = new List<StoveSlotSaveData>()
        };

        foreach (var slot in StoveManager.Instance.stoves)
        {
            StoveSlotSaveData slotData = new StoveSlotSaveData
            {
                isCooking = slot.IsCooking,
                isCooked = slot.IsCooked,
                cookTimeRemaining = slot.GetCookTimeRemaining(),
                currentIngredients = slot.GetRawIngredientsCopy()
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
        IngredientStockManager.Instance.RestoreStock(data.ingredientStocks);
        PlayerWokManager.Instance.RestoreWok(data.playerWok);
        PackagingAreaManager.Instance.RestoreSlots(data.packagingArea);

        for (int i = 0; i < data.stoveStates.Count; i++)
        {
            StoveManager.Instance.stoves[i].RestoreFromSave(data.stoveStates[i]);
        }

        ReceiptLineManager.Instance.RestoreReceiptSlots(data.receiptSlots);

        ReceiptSystem.CurrentReceiptID = data.lastReceiptID;
        ReceiptSystem.CurrentOrderItemID = data.lastOrderItemID;
        ReceiptSystem.RestoreReceipts(data.missedReceipts, data.successfulReceipts);
        ReceiptLineManager.Instance.RestorePendingReceipts(data.pendingReceipts);

        Debug.Log("게임 불러오기 완료!");
    }

    public void OnSaveButtonPressed()
    {
        SaveGame();
    }

    public void OnLoadButtonPressed()
    {
        LoadGame();
    }
}
