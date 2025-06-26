using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;

public class StoveManager : MonoBehaviour
{
    public StoveSlot[] stoves;
    public TextMeshProUGUI resultText;
    public ReceiptLineManager receiptLineManager;

    public void TryStartCooking(OrderItem order, float cookTimeSeconds = 5 * 60)
    {
        var emptySlot = FindEmptyStove();
        if (emptySlot != null)
        {
            emptySlot.StartCooking(order, cookTimeSeconds, OnStoveFreed);

            var activeReceipt = ReceiptStateManager.Instance.ActiveReceipt; 
            if (AllMenusHandled(activeReceipt))     //메뉴를 올리는 순간 모든 메뉴가 조리 중이거나 완료 상태일 경우, 영수증 제거하기
            {
                Debug.Log($"[StoveManager] 모든 메뉴 처리됨 → 영수증 제거 시도: {activeReceipt.OrderID}");
                receiptLineManager.RemoveReceiptByOrderID(activeReceipt.OrderID);
            }
        }
        else
        {
            Debug.LogWarning("비어있는 화구가 없습니다.");
        }
    }
    private StoveSlot FindEmptyStove()
    {
        foreach (var slot in stoves)
        {
            if (slot.IsAvailable)
            {
                return slot;
            }
        }
        return null;  // 비어있는 슬롯이 없으면 null 반환
    }

    private void OnStoveFreed(OrderItem completedOrder)
    {
        if (resultText != null)
        {
            resultText.text = $"[{completedOrder.ItemID}] {completedOrder.Menu.Name} 조리 완료!";
        }

        var activeReceipt = ReceiptStateManager.Instance.ActiveReceipt;
        if (activeReceipt == null) return;

        // 특정 메뉴 완료 체크 후 영수증의 전체 완료 여부 체크
        // 여기서 오류가!!!!!!!!!
        //if (AllMenusCompleted(activeReceipt))
        //{
        //    FindObjectOfType<ReceiptLineManager>().RemoveReceiptByOrderID(activeReceipt.OrderID);
        //}
        
    }

    private bool AllMenusCompleted(Receipt receipt) //영수증의 모든 메뉴가 완료되었음을 확인
    {
        foreach (var order in receipt.GetOrders())
        {
            if (!order.IsCompleted)
            {
                return false;
            }
        }
        return true;
    }


    private bool AllMenusHandled(Receipt receipt) //영수증의 모든 메뉴가 조리 중이거나 조리 완료 되었음을 확인
    {
        foreach (var order in receipt.GetOrders())
        {
            if (!order.IsOnStove && !order.IsCompleted)
            {
                return false;
            }
        }
        return true;
    }

    public static bool AllMenusHandledStatic(Receipt receipt)
    {
        foreach (var order in receipt.GetOrders())
        {
            if (!order.IsOnStove && !order.IsCompleted)
                return false;
        }
        return true;
    }
}


