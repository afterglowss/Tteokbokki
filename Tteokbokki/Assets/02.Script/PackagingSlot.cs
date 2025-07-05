using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PackagingSlot : MonoBehaviour, IDropHandler
{
    public Transform foodStackParent; // ������ ���� ��ġ (�ڽ��̰ų� �ڽ�)
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

        // 1. ���Կ� �ױ�
        AddFood(foodUI);

        // 2. ȭ�� ���� �ʱ�ȭ
        if (foodUI.originStoveSlot != null)
        {
            foodUI.originStoveSlot.ResetSlot();
        }
    }

    private void AddFood(CookedFoodUI food)
    {
        if (stackedFoods.Contains(food))
            return;

        packagingArea.RemoveFoodFromAllSlots(food);  // ���� ���Կ��� ���� (�ߺ� ����)

        food.transform.SetParent(foodStackParent);
        food.transform.localPosition = new Vector3(0, stackedFoods.Count * stackYOffset, 0);
        stackedFoods.Add(food);

        food.currentSlot = this;  // ���� ���� ���
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
            // ������ �ϳ��� ���� ���� �� ���� �ƴ�, ��� ��ȿ
            Debug.Log("������ ���� �����Դϴ�. ������ ��� ���� + ����");

            // ���͸� ���� ���� ��ġ�� ��������
            receiptItem.ReturnToOriginalPosition();
            return;
        }

        // ���� ���� ��� ��� ����
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

        TooltipManager.Instance.Hide(); // ���� �����
        foreach (var food in stackedFoods)
            Destroy(food.gameObject);

        stackedFoods.Clear();

        // ������ UI ����
        Destroy(receiptItem.gameObject);

        ReceiptLineManager.Instance.RemoveReceipt(receiptItem);

        // (���û���: ����/���� �ǵ��)
        Debug.Log(success ? $"������ {receipt.OrderID} ó�� ����!" : $"������ {receipt.OrderID} ó�� ���� - ��ϵ�");
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
