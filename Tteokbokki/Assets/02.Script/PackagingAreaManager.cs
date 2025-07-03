using System.Collections.Generic;
using UnityEngine;

public class PackagingAreaManager : MonoBehaviour
{
    public Transform foodParent;  // ���ĵ��� ���� �θ� (UI Grid)
    public GameObject foodPrefab; // CookedFoodUI ������
    public int maxSlots = 4;

    private List<CookedFoodUI> currentFoods = new();

    public bool CanAddFood => currentFoods.Count < maxSlots;

    public List<PackagingSlot> slots;
    private List<Receipt> failedReceipts = new();

    void Start()
    {
        foreach (var slot in slots)
        {
            slot.Initialize(this); // �� �Ŵ����� ����
        }
    }

    public void RecordFailedReceipt(Receipt receipt)
    {
        failedReceipts.Add(receipt);
        Debug.Log($"[���] ���� ������: {receipt.OrderID}");
    }

    public List<Receipt> GetFailedReceipts() => new List<Receipt>(failedReceipts);

    public void AddCookedFood(Dictionary<string, int> ingredients)
    {
        if (!CanAddFood)
        {
            Debug.LogWarning("����밡 ���� á���ϴ�!");
            return;
        }

        var obj = Instantiate(foodPrefab, foodParent);
        var ui = obj.GetComponent<CookedFoodUI>();
        ui.Initialize(ingredients);

        currentFoods.Add(ui);
    }

    public List<Dictionary<string, int>> GetAllCookedFoods()
    {
        List<Dictionary<string, int>> result = new();
        foreach (var food in currentFoods)
        {
            result.Add(new Dictionary<string, int>(food.Ingredients));
        }
        return result;
    }

    public void ClearAllFoods()
    {
        foreach (var food in currentFoods)
        {
            Destroy(food.gameObject);
        }
        currentFoods.Clear();
    }

    public void RemoveFoodFromAllSlots(CookedFoodUI food)
    {
        foreach (var slot in slots)
        {
            slot.RemoveFood(food);
        }
    }
}
