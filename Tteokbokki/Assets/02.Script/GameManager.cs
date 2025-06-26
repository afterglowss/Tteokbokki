using System;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public ReceiptLineManager receiptLineManager;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EndOfDay()
    {
        var missed = receiptLineManager.GetMissedReceipts();
        DateTime today = GameClock.gameTime.Date;

        ReceiptManager.SaveMissedReceipts(missed, today);

        int total = missed.Sum(r => r.GetOrders().Sum(o => o.TotalPrice));
        Debug.Log($"[마감] 미완료 주문 {missed.Count}건 / 총 {total}원 저장 완료");

        receiptLineManager.ClearMissedReceipts();
    }
}
