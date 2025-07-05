using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SaveData;

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

    private void Start()
    {
        Debug.Log("Saved to: " + Path.Combine(Application.persistentDataPath, SaveFilePath));
    }

    public void SaveGame()
    {
        GameSaveData data = new GameSaveData();

        data.gameTime = GameClock.gameTime.ToString("o");
        data.playerBalance = PlayerWalletManager.Instance.CurrentBalance;
        data.ingredientStocks = IngredientStockManager.Instance.GetCurrentStockCopy();
        data.playerWok = PlayerWokManager.Instance.GetPlayerIngredients();
        data.packagingArea = PackagingAreaManager.Instance.GetSlotWiseCookedFoods();

        // 저장: 스토브 상태
        var stoveList = new List<StoveSlotSaveData>();
        foreach (var slot in StoveManager.Instance.stoves)
        {
            StoveSlotSaveData slotData = new StoveSlotSaveData
            {
                isCooking = slot.IsCooking,
                isCooked = slot.IsCooked,
                cookTimeRemaining = slot.GetCookTimeRemaining(),
                currentIngredients = slot.GetRawIngredientsCopy()
            };
            stoveList.Add(slotData);
        }
        data.stoveStates = stoveList;

        // TODO: 아래 ID와 영수증은 ReceiptManager에서 받아오도록 처리
        data.lastReceiptID = ReceiptSystem.CurrentReceiptID;
        data.lastOrderItemID = ReceiptSystem.CurrentOrderItemID;

        data.missedReceipts = ReceiptSystem.GetMissedReceiptsData();
        data.successfulReceipts = ReceiptSystem.GetSuccessfulReceiptsData();

        File.WriteAllText(Path.Combine(Application.persistentDataPath, SaveFilePath), JsonUtility.ToJson(data, true));
        Debug.Log("Saved to: " + Path.Combine(Application.persistentDataPath, SaveFilePath));
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
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

        GameClock.SetGameTime(DateTime.Parse(data.gameTime));
        PlayerWalletManager.Instance.SetBalance(data.playerBalance);
        IngredientStockManager.Instance.RestoreStock(data.ingredientStocks);
        PlayerWokManager.Instance.RestoreWok(data.playerWok);
        PackagingAreaManager.Instance.RestoreSlots(data.packagingArea);

        for (int i = 0; i < data.stoveStates.Count; i++)
        {
            StoveManager.Instance.stoves[i].RestoreFromSave(data.stoveStates[i]);
        }

        ReceiptSystem.CurrentReceiptID = data.lastReceiptID;
        ReceiptSystem.CurrentOrderItemID = data.lastOrderItemID;

        ReceiptSystem.RestoreReceipts(data.missedReceipts, data.successfulReceipts);

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
