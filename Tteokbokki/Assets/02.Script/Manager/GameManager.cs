using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    public ReceiptLineManager receiptLineManager;

    [Header("마감창 UI")]
    public GameObject endOfDayPanel;
    public EndOfDayUIHandler endOfDayUIHandler;

    public static GameManager Instance { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartOfDay()
    {
        GameClock.gameTime = GameClock.gameTime.AddDays(1);
        GameClock.Instance.SetToStartOfDay();
        GameClock.Instance.UpdateTimeAndDateDisplay();

        IngredientStockManager.Instance.ResetDailyOrderFlags();
        IngredientStockManager.Instance.AdvanceDayAndDecay();

        StoveManager.Instance.ClearAllStoves(); // 조리 상태 초기화 (필요 시)
        PackagingAreaManager.Instance.ClearAllFoods(); // 포장대 초기화
        PlayerWokManager.Instance.ClearWok(); // 플레이어 웍 초기화
        ReceiptLineManager.Instance.ClearAllReceipts(); // 영수증 리스트 초기화
        ReceiptLineManager.Instance.ClearMissedReceipts(); // 실패 영수증 리스트 초기화
        ReceiptLineManager.Instance.ClearSuccessfulReceipts(); // 성공 영수증 리스트 초기화

        ReceiptSystem.CurrentReceiptID = 1; // 주문 번호 초기화
        ReceiptSystem.CurrentOrderItemID = 1; // 메뉴 번호 초기화

        DailyBonusManager.Instance.ApplyNewDayBonus();

        GameClock.Resume();

        OrderSpawner.Instance.RestartSpawning();

        // 로그 출력
        Debug.Log("[시작] 새로운 영업일이 시작되었습니다.");
    }

    public void EndOfDay()
    {
        GameClock.Pause();
        OrderSpawner.Instance.StopSpawning();

        PackagingAreaManager.Instance.ClearAllFoods();

        var missed = ReceiptLineManager.Instance.GetMissedReceipts();
        var successful = ReceiptLineManager.Instance.GetSuccessfulReceipts();
        DateTime today = GameClock.gameTime.Date;

        ReceiptManager.SaveMissedReceipts(missed, today);
        ReceiptManager.SaveSuccessfulReceipts(successful, today);

        // 판매 총액 계산
        int successTotal = successful.Sum(r => r.GetTotalPrice());
        // 세금 차감
        PlayerWalletManager.Instance.DeductDailyTaxes(successTotal);
        // 손실 총액 계산
        int missedTotal = missed.Sum(r => r.GetOrders().Sum(o => o.TotalPrice));
        // 성공률 계산
        float successRate = successful.Count / (float)(successful.Count + missed.Count + 0.01f);
        OrderSpawner.Instance.SetPreviousDaySuccessRate(successRate); // 다음날 확률 반영용

        // 로그 출력
        Debug.Log($"[마감] 성공 주문 {successful.Count}건 / 총 판매금액: {successTotal:N0}원");
        Debug.Log($"[마감] 미완료 주문 {missed.Count}건 / 손실 금액: {missedTotal:N0}원");
        Debug.Log($"[마감] 세금 {Mathf.RoundToInt(successTotal * PlayerWalletManager.Instance.taxRate):N0}원 납부");

        // 마감 UI 출력
        ShowEndOfDayPanel();

        // 초기화
        receiptLineManager.ClearMissedReceipts();
        receiptLineManager.ClearSuccessfulReceipts(); // 성공 기록도 초기화!

        IngredientStockManager.Instance.ResetDailyOrderFlags();

        IngredientStockManager.Instance.UpdateLowStockList();
        Debug.Log(IngredientStockManager.Instance.GetLowStockText());

        DailyBonusManager.Instance.SetTomorrowBonusIngredients();

        GameSaveManager.Instance.DeleteSaveFile();

        GameClock.SaveLastPlayedDate(today);
    }
    public void OnClosingTimeReached()
    {
        OrderSpawner.Instance.StopSpawning(); // 주문 생성 중단

        // 남은 영수증이 없다면 바로 마감
        if (ReceiptLineManager.Instance.GetReceiptSlots().Count == 0)
        {
            EndOfDay();
        }
        else
        {
            // 감시 루틴 시작
            StartCoroutine(WaitForReceiptClearAndEnd());
        }
    }

    private void ShowEndOfDayPanel()
    {
        //Panel_EndOfDay 활성화
        endOfDayPanel.SetActive(true);

        // 텍스트 채우기
        endOfDayUIHandler.FillReceiptTexts();
        endOfDayUIHandler.FillIngredientTexts();
        endOfDayUIHandler.FillTaxText();

        // 초기화
        receiptLineManager.ClearMissedReceipts();
        receiptLineManager.ClearSuccessfulReceipts();
    }
    private IEnumerator WaitForReceiptClearAndEnd()
    {
        Debug.Log("[마감 대기] 오후 9시 이후, 영수증 처리 완료 대기 중...");

        // 매 0.5초마다 확인
        while (ReceiptLineManager.Instance.GetReceiptSlots().Count > 0)
        {
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("[마감] 모든 영수증 처리 완료. EndOfDay 호출!");

        EndOfDay();
    }

}
