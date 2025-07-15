using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaveData
{
    [Serializable]
    public class KeyValueStringInt
    {
        public string Key;
        public int Value;
    }

    [Serializable]
    public class ReceiptData
    {
        public int OrderID;
        public string OrderDateTime;
        public List<OrderItemData> Orders;
    }

    [Serializable]
    public class OrderItemData
    {
        public string MenuName;
        public int BasePrice;
        public List<KeyValueStringInt> Extras = new List<KeyValueStringInt>();
    }
    [Serializable]
    public class ReceiptSlotSaveData
    {
        public ReceiptData receiptData;      // 주문 정보 (메뉴, 재료 등)
        public float remainingTime;          // 제한시간 타이머
        public float cookLimitTime;          // 전체 제한 시간
        public int slotIndex;                // 슬롯 위치 (0~N)
    }
}

public class ReceiptSaveDTO : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
