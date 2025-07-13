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

    public void StartOfDay()
    {
        // 하루 시작 시 초기화
        receiptLineManager.ClearMissedReceipts();
        receiptLineManager.ClearSuccessfulReceipts();

        IngredientStockManager.Instance.ResetDailyOrderFlags();

        IngredientStockManager.Instance.AdvanceDayAndDecay();

        // 로그 출력
        Debug.Log("[시작] 새로운 영업일이 시작되었습니다.");
    }

    public void EndOfDay()
    {
        var missed = receiptLineManager.GetMissedReceipts();
        var successful = receiptLineManager.GetSuccessfulReceipts();
        DateTime today = GameClock.gameTime.Date;

        ReceiptManager.SaveMissedReceipts(missed, today);
        ReceiptManager.SaveSuccessfulReceipts(successful, today);

        // 판매 총액 계산
        int successTotal = successful.Sum(r => r.GetTotalPrice());
        int missedTotal = missed.Sum(r => r.GetOrders().Sum(o => o.TotalPrice));

        // 세금 차감
        PlayerWalletManager.Instance.DeductDailyTaxes(successTotal);

        // 로그 출력
        Debug.Log($"[마감] 성공 주문 {successful.Count}건 / 총 판매금액: {successTotal:N0}원");
        Debug.Log($"[마감] 미완료 주문 {missed.Count}건 / 손실 금액: {missedTotal:N0}원");
        Debug.Log($"[마감] 세금 {Mathf.RoundToInt(successTotal * PlayerWalletManager.Instance.taxRate):N0}원 납부");

        // 초기화
        receiptLineManager.ClearMissedReceipts();
        receiptLineManager.ClearSuccessfulReceipts(); // 성공 기록도 초기화!

        IngredientStockManager.Instance.ResetDailyOrderFlags();

        IngredientStockManager.Instance.AdvanceDayAndDecay();
        Debug.Log("[마감] 영업일이 종료되었습니다. 재고가 초기화되고 유통기한이 갱신되었습니다.");
    }
}
