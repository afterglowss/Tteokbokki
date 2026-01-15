using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using Yarn.Unity;

public class GameManager : MonoBehaviour
{
    [Header("마감창 UI")]
    public GameObject endOfDayPanel;
    public EndOfDayUIHandler endOfDayUIHandler;

    public static GameManager Instance { get; private set; }

    public DialogueRunner dialogueRunner;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void StartOfDay()
    {
        GameClock.gameTime = GameClock.gameTime.AddDays(1);
        GameClock.Instance.SetToStartOfDay();
        GameClock.Instance.UpdateTimeAndDateDisplay();

        IngredientStockManager.Instance.ResetDailyOrderFlags();
        IngredientStockManager.Instance.AdvanceDayAndDecay();

        // 조리 상태 초기화
        StoveManager.Instance.ClearAllStoves();

        PackagingAreaManager.Instance.ClearAllFoods(); // 포장대 초기화

        ReceiptLineManager.Instance.ClearAllReceipts(); // 영수증 리스트 초기화
        ReceiptLineManager.Instance.ClearMissedReceipts(); // 실패 영수증 리스트 초기화
        ReceiptLineManager.Instance.ClearSuccessfulReceipts(); // 성공 영수증 리스트 초기화

        ReceiptSystem.CurrentReceiptID = 1; // 주문 번호 초기화
        ReceiptSystem.CurrentOrderItemID = 1; // 메뉴 번호 초기화

        DailyBonusManager.Instance.ApplyNewDayBonus();

        GameClock.Resume();

        OrderSpawner.Instance.RestartSpawning();

        PlayerWalletManager.Instance.ResetTodayEarnings();  // 하루 수익 초기화

        HideEndOfDayPanel();

        Debug.Log("[시작] 새로운 영업일이 시작되었습니다.");
    }

    public void EndOfDay()
    {
        GameClock.Pause();
        OrderSpawner.Instance.StopSpawning();

        GameSaveManager.Instance.SaveGame();

        PackagingAreaManager.Instance.ClearAllFoods();

        var missed = ReceiptLineManager.Instance.GetMissedReceipts();
        var successful = ReceiptLineManager.Instance.GetSuccessfulReceipts();
        DateTime today = GameClock.gameTime.Date;

        ReceiptManager.SaveMissedReceipts(missed, today);
        ReceiptManager.SaveSuccessfulReceipts(successful, today);

        // 판매 총액 계산
        int successTotal = successful.Sum(r => r.GetTotalPrice());

        // ❌ [삭제됨] 자동 세금 차감 로직 제거 (이제 UI에서 버튼 눌러서 납부함)
        // PlayerWalletManager.Instance.DeductDailyTaxes(successTotal); 

        // 손실 총액 계산
        int missedTotal = missed.Sum(r => r.GetOrders().Sum(o => o.TotalPrice));
        // 성공률 계산
        float successRate = successful.Count / (float)(successful.Count + missed.Count + 0.01f);
        OrderSpawner.Instance.SetPreviousDaySuccessRate(successRate); // 다음날 확률 반영용

        // 로그 출력
        Debug.Log($"[마감] 성공 주문 {successful.Count}건 / 총 판매금액: {successTotal:N0}원");
        Debug.Log($"[마감] 미완료 주문 {missed.Count}건 / 손실 금액: {missedTotal:N0}원");
        // 세금 로그도 여기서 띄우기 애매하므로 제거하거나 예상액으로 변경
        // Debug.Log($"[마감] 세금 {Mathf.RoundToInt(successTotal * PlayerWalletManager.Instance.taxRate):N0}원 납부");

        // 마감 UI 출력
        ShowEndOfDayPanel();

        IngredientStockManager.Instance.ResetDailyOrderFlags();

        IngredientStockManager.Instance.UpdateLowStockList();
        Debug.Log(IngredientStockManager.Instance.GetLowStockText());

        DailyBonusManager.Instance.SetTomorrowBonusIngredients();

        // Yarn 변수에 저장
        var bonusList = DailyBonusManager.Instance.GetTomorrowBonusIngredients().ToList();
        dialogueRunner.VariableStorage.SetValue("$bonus1", bonusList.Count > 0 ? bonusList[0] : "");
        dialogueRunner.VariableStorage.SetValue("$bonus2", bonusList.Count > 1 ? bonusList[1] : "");

        // Yarn 대화 시작
        dialogueRunner.StartDialogue("TomorrowBonusLine");

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
        // Panel_EndOfDay 활성화
        // ✨ [수정] 핸들러가 OnEnable에서 스스로 초기화하므로, 여기서는 켜기만 하면 됩니다.
        endOfDayPanel.SetActive(true);
    }

    private void HideEndOfDayPanel()
    {
        endOfDayPanel.SetActive(false);
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