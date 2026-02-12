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

    private bool isEmergencyClosing = false;

    void Awake()
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
        //if (GameSaveManager.Instance.IsLoading)
        //{
        //    // A. 이어하기인 경우: 아무것도 안 함 (LoadGame이 RestoreStock을 부를 테니까)
        //}
        //else if (TutorialManager.Instance.IsTutorialJustFinished) // 튜토리얼 직후인지 확인하는 플래그 필요
        //{
        //    // B. 튜토리얼 끝내고 온 경우
        //    IngredientStockManager.Instance.ApplyTutorialAftermath();
        //    TutorialManager.Instance.IsTutorialJustFinished = false; // 플래그 초기화
        //}
        //else
        //{
        //    // C. 튜토리얼 스킵하고 바로 새 게임 (그냥 기본 재료만 지급)
        //    IngredientStockManager.Instance.OrderBasicIngredients(); // 기존 함수 public으로 변경 필요
        //}
    }

    public void StartOfDay()
    {
        isEmergencyClosing = false; // ✨ 플래그 초기화

        GameClock.gameTime = GameClock.gameTime.AddDays(1);
        GameClock.Instance.SetToStartOfDay();
        GameClock.Instance.UpdateTimeAndDateDisplay();

        IngredientStockManager.Instance.ResetDailyOrderFlags();
        IngredientStockManager.Instance.AdvanceDayAndDecay();

        // ✨ [NEW] 어제 구매 내역을 반영하여 주방 버튼(재료통) 새로고침!
        // (일반 재료와 소스도 이때 분리되어 생성됨)
        IngredientStockManager.Instance.GenerateIngredientButtons();

        // 조리 상태 초기화
        StoveManager.Instance.ClearAllStoves();

        PackagingAreaManager.Instance.ClearAllFoods(); // 포장대 초기화

        ReceiptLineManager.Instance.ClearAllReceipts(); // 영수증 리스트 초기화
        ReceiptLineManager.Instance.ClearMissedReceipts(); // 실패 영수증 리스트 초기화
        ReceiptLineManager.Instance.ClearSuccessfulReceipts(); // 성공 영수증 리스트 초기화

        ReceiptSystem.CurrentReceiptID = 1; // 주문 번호 초기화
        ReceiptSystem.CurrentOrderItemID = 1; // 메뉴 번호 초기화

        DailyBonusManager.Instance.ApplyNewDayBonus();

        //GameClock.Resume();

        //OrderSpawner.Instance.RestartSpawning();

        PlayerWalletManager.Instance.ResetTodayEarnings();  // 하루 수익 초기화

        //HideEndOfDayPanel();

        Debug.Log("[준비] 다음 영업일 데이터 설정 완료 (시간은 아직 정지 상태)");
    }

    public void StartDayGameplay()
    {
        GameClock.Resume(); // 시간 흐르기 시작
        OrderSpawner.Instance.RestartSpawning(); // 주문 생성 시작

        Debug.Log("[시작] 셔터가 열리고 영업이 시작되었습니다!");
    }

    public void EndOfDay()
    {
        GameClock.Pause();
        OrderSpawner.Instance.StopSpawning();

        // ✨ [NEW] 전화기 코드 뽑기 (전화 강제 종료)
        if (PhoneCallManager.Instance != null)
        {
            PhoneCallManager.Instance.ForceStopAllCalls();
        }

        // ✨ [핵심 추가] 정산 계산하기 전에, 현재 남아있는 영수증들을 전부 '실패'로 확정 짓습니다.
        if (ReceiptLineManager.Instance != null)
        {
            ReceiptLineManager.Instance.FailAllActiveReceipts();
        }

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

        // ✨ [UI 정리] 마감 창이 뜨기 전에 화구 정보와 영수증 정보창을 강제로 끕니다.
        // 1. 화구 정보창 끄기 & 화구 선택 해제 (PlayerWokManager 스크롤뷰 꺼짐)
        if (StoveManager.Instance != null)
        {
            StoveManager.Instance.DeselectCurrentSlot();
        }

        // 2. 영수증 재료 정보창 끄기 (CombinedIngredientManager 스크롤뷰 꺼짐)
        if (ReceiptLineManager.Instance != null && ReceiptLineManager.Instance.combinedIngredientManager != null)
        {
            ReceiptLineManager.Instance.combinedIngredientManager.ClearIngredientsText();
        }

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
        if(TutorialManager.Instance ==  null) {
            //dialogueRunner.StartDialogue("TomorrowBonusLine");
        };
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

    // ✨ [NEW] 재료 소진 시 강제 조기 마감
    public void TriggerEmergencyClose(string reason)
    {
        // 1. 이미 마감 절차가 진행 중이라면 무시
        if (isEmergencyClosing || GameClock.isPaused || endOfDayPanel.activeSelf) return;

        isEmergencyClosing = true; // ✨ 지금부터 마감 절차 시작함을 표시

        Debug.Log($"[긴급 마감] {reason} - 더 이상 조리가 불가능하여 영업을 종료합니다.");

        // 2. 플레이어에게 이유를 알려줌 (툴팁 등)
        TooltipManager.ShowFollowMouse(TooltipType.UI, $"{reason}\n잠시 후 영업을 조기 종료합니다...", 5f);

        // 3. 주문 생성 즉시 중단
        OrderSpawner.Instance.StopSpawning();

        StartCoroutine(DelayedEmergencyCloseRoutine());
    }
    private IEnumerator DelayedEmergencyCloseRoutine()
    {
        yield return new WaitForSeconds(5.0f); // 5초 대기 (플레이어가 툴팁 읽을 시간)

        EndOfDay(); // 마감 정산 시작
    }
}