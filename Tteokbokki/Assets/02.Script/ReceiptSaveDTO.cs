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
