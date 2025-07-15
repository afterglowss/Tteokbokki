using System.Collections.Generic;
using UnityEngine;

public class PackagingAreaManager : MonoBehaviour
{
    public static PackagingAreaManager Instance { get; private set; }
    public Transform foodParent;  // 음식들을 담을 부모 (UI Grid)
    public GameObject foodPrefab; // CookedFoodUI 프리팹
    public int maxSlots = 4;

    private List<CookedFoodUI> currentFoods = new();

    public bool CanAddFood => currentFoods.Count < maxSlots;

    public List<PackagingSlot> slots;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void Start()
    {
        foreach (var slot in slots)
        {
            slot.Initialize(this); // 이 매니저를 전달
        }
    }

    public void AddCookedFood(Dictionary<string, int> ingredients)
    {
        if (!CanAddFood)
        {
            Debug.LogWarning("포장대가 가득 찼습니다!");
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

    public List<List<Dictionary<string, int>>> GetSlotWiseCookedFoods()
    {
        List<List<Dictionary<string, int>>> result = new();

        foreach (var slot in slots)
        {
            List<Dictionary<string, int>> stack = new();
            foreach (Transform child in slot.foodStackParent)
            {
                var food = child.GetComponent<CookedFoodUI>();
                if (food != null)
                    stack.Add(new Dictionary<string, int>(food.Ingredients));
            }
            result.Add(stack);
        }

        return result;
    }
    public void RestoreSlots(List<List<Dictionary<string, int>>> savedData)
    {
        ClearAllFoods(); // 기존 음식들 제거

        for (int i = 0; i < savedData.Count && i < slots.Count; i++)
        {
            var slot = slots[i];
            var stack = savedData[i];

            foreach (var ingredients in stack)
            {
                var obj = Instantiate(foodPrefab, slot.foodStackParent);
                var foodUI = obj.GetComponent<CookedFoodUI>();
                foodUI.Initialize(ingredients);
                foodUI.currentSlot = slot;

                slot.ForceAddFoodToStack(foodUI); // 자동 정렬
            }
        }
    }

}
