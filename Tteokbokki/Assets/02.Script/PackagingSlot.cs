using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PackagingSlot : MonoBehaviour, IDropHandler
{
    public Transform foodStackParent; // 음식이 쌓일 위치 (자신이거나 자식)
    public float stackYOffset = 30f;

    private List<CookedFoodUI> stackedFoods = new();
    private PackagingAreaManager packagingArea;

    public int maxStackSize = 4;

    public void Initialize(PackagingAreaManager area)
    {
        packagingArea = area;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var foodUI = eventData.pointerDrag?.GetComponent<CookedFoodUI>();
        if (foodUI == null) return;

        if (stackedFoods.Count >= maxStackSize) return;

        // 1. 슬롯에 쌓기
        AddFood(foodUI);

        // 2. 화구 상태 초기화
        if (foodUI.originStoveSlot != null)
        {
            foodUI.originStoveSlot.ResetSlot();
        }
    }

    private void AddFood(CookedFoodUI food)
    {
        if (stackedFoods.Contains(food))
            return;

        packagingArea.RemoveFoodFromAllSlots(food);  // 기존 슬롯에서 제거 (중복 방지)

        food.transform.SetParent(foodStackParent);
        food.transform.localPosition = new Vector3(0, stackedFoods.Count * stackYOffset, 0);
        stackedFoods.Add(food);

        food.currentSlot = this;  // 현재 슬롯 등록
    }

    public void RemoveFood(CookedFoodUI food)
    {
        if (stackedFoods.Remove(food))
        {
            RepositionFoods();
        }
    }

    private void RepositionFoods()
    {
        for (int i = 0; i < stackedFoods.Count; i++)
        {
            stackedFoods[i].transform.localPosition = new Vector3(0, i * stackYOffset, 0);
        }
    }

    public void HandleReceiptDrop(ReceiptLineItem receiptItem)
    {
        var receipt = receiptItem.GetReceipt();

        if (stackedFoods.Count == 0)
        {
            // 음식이 하나도 없는 슬롯 → 실패 아님, 드롭 무효
            Debug.Log("음식이 없는 슬롯입니다. 영수증 드롭 무시 + 복귀");

            // 복귀를 위해 원래 위치로 돌려놓기
            receiptItem.ReturnToOriginalPosition();
            return;
        }

        // 내부 음식 재료 목록 추출
        List<Dictionary<string, int>> cookedIngredients = new();
        foreach (var food in stackedFoods)
        {
            cookedIngredients.Add(food.Ingredients);
        }

        bool success = MatchAllMenusInReceipt(receipt, cookedIngredients);

        if (!success)
            ReceiptLineManager.Instance.RecordFailedReceipt(receipt);
        else
        {
            ReceiptLineManager.Instance.RecordSuccessfulReceipt(receipt);
            PlayerWalletManager.Instance.AddIncome(receipt.GetTotalPrice());
        }

        TooltipManager.Instance.Hide(); // 툴팁 숨기기
        foreach (var food in stackedFoods)
            Destroy(food.gameObject);

        stackedFoods.Clear();

        // 영수증 UI 제거
        Destroy(receiptItem.gameObject);

        ReceiptLineManager.Instance.RemoveReceipt(receiptItem);

        // (선택사항: 성공/실패 피드백)
        Debug.Log(success ? $"영수증 {receipt.OrderID} 처리 성공!" : $"영수증 {receipt.OrderID} 처리 실패 - 기록됨");
    }

    private bool MatchAllMenusInReceipt(Receipt receipt, List<Dictionary<string, int>> cookedFoods)
    {
        var unmatched = new List<Dictionary<string, int>>(cookedFoods);

        foreach (var order in receipt.GetOrders())
        {
            var required = CombinedIngredientManager.GetCombinedIngredients(order.Menu, order.GetExtras());

            bool matched = false;
            for (int i = 0; i < unmatched.Count; i++)
            {
                if (AreIngredientsEqual(required, unmatched[i]))
                {
                    unmatched.RemoveAt(i);
                    matched = true;
                    break;
                }
            }

            if (!matched) return false;
        }

        return true;
    }

    private bool AreIngredientsEqual(Dictionary<string, int> a, Dictionary<string, int> b)
    {
        if (a.Count != b.Count) return false;

        foreach (var kvp in a)
        {
            if (!b.TryGetValue(kvp.Key, out int val) || val != kvp.Value)
                return false;
        }

        return true;
    }

    public void ForceAddFoodToStack(CookedFoodUI food)
    {
        food.transform.SetParent(foodStackParent);
        food.transform.localPosition = new Vector3(0, stackedFoods.Count * stackYOffset, 0);
        stackedFoods.Add(food);
        food.currentSlot = this;
    }

    public bool IsTopOfStack(CookedFoodUI food)
    {
        return stackedFoods.Count > 0 && stackedFoods[stackedFoods.Count - 1] == food;
    }

    public int GetStackIndex(CookedFoodUI food)
    {
        return stackedFoods.IndexOf(food);
    }
    public bool HasAnyFood()
    {
        return stackedFoods != null && stackedFoods.Count > 0;
    }
}
